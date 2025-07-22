using System.Reflection;
using CollectiveAI.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CollectiveAI.Factories;

public class TeamConfiguration
{
    public string Name { get; set; } = string.Empty;
    public List<AgentConfiguration> Agents { get; set; } = [];
}

public static class AgentBuilder
{
    public static Dictionary<string, ChatCompletionAgent> CreateAgents(Kernel kernel, IConfiguration configuration,
        string teamName = null)
    {
        // Get teams configuration section
        var teamsConfig = configuration.GetSection("Teams");
        var teamConfigurations = new List<TeamConfiguration>();

        // Bind configuration to strongly typed objects
        teamsConfig.Bind(teamConfigurations);

        if (!teamConfigurations.Any()) throw new InvalidOperationException("No teams configured in app settings");

        // Get the specified team or the first team if no name provided
        TeamConfiguration teamConfig;
        if (string.IsNullOrEmpty(teamName))
        {
            teamConfig = teamConfigurations.First();
        }
        else
        {
            teamConfig =
                teamConfigurations.FirstOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));
            if (teamConfig == null)
                throw new InvalidOperationException($"Team '{teamName}' not found in configuration");
        }

        var agents = new Dictionary<string, ChatCompletionAgent>();

        foreach (var agentConfig in teamConfig.Agents)
        {
            // Clone kernel for each agent
            var agentKernel = kernel.Clone();

            // Add plugins based on configuration using reflection
            foreach (var pluginName in agentConfig.Plugins)
            {
                // Attempt to find and instantiate plugin class
                var assembly = Assembly.GetExecutingAssembly();
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name.Equals($"{pluginName}Plugin", StringComparison.OrdinalIgnoreCase) ||
                                         t.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));

                if (pluginType != null)
                {
                    var pluginInstance = Activator.CreateInstance(pluginType);
                    if (pluginInstance != null)
                        agentKernel.Plugins.AddFromObject(pluginInstance);
                    else
                        throw new InvalidOperationException($"Unable to instantiate plugin: {pluginName}");
                }
                else
                {
                    throw new InvalidOperationException($"Plugin type not found: {pluginName}");
                }
            }

            // Create the agent
            var agent = new ChatCompletionAgent
            {
                Name = agentConfig.Name,
                Description = agentConfig.Description,
                Instructions = agentConfig.Instructions,
                Kernel = agentKernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };

            // Add agent to dictionary
            agents[agentConfig.Name] = agent;
        }

        return agents;
    }

    public static Dictionary<string, Dictionary<string, ChatCompletionAgent>> CreateAllTeams(Kernel kernel,
        IConfiguration configuration)
    {
        // Get teams configuration section
        var teamsConfig = configuration.GetSection("Teams");
        var teamConfigurations = new List<TeamConfiguration>();

        // Bind configuration to strongly typed objects
        teamsConfig.Bind(teamConfigurations);

        if (!teamConfigurations.Any()) throw new InvalidOperationException("No teams configured in app settings");

        var allTeams = new Dictionary<string, Dictionary<string, ChatCompletionAgent>>();

        foreach (var teamConfig in teamConfigurations)
        {
            var teamAgents = CreateAgents(kernel, configuration, teamConfig.Name);
            allTeams[teamConfig.Name] = teamAgents;
        }

        return allTeams;
    }
}