using CollectiveAI.Factories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace CollectiveAI.Services;

public interface IAgentStoreService
{
    ChatCompletionAgent[] GetTeamAgents(string teamName);
    ChatCompletionAgent GetAgent(string teamName, string agentName);
    IEnumerable<string> GetTeamNames();
    IEnumerable<string> GetAgentNames(string teamName);
    bool TeamExists(string teamName);
    bool AgentExists(string teamName, string agentName);
}

public class AgentStoreService : IAgentStoreService
{
    private readonly Dictionary<string, Dictionary<string, ChatCompletionAgent>> _teams;

    public AgentStoreService(IServiceProvider serviceProvider, IConfiguration configuration,
        ILogger<AgentStoreService>? logger = null)
    {
        try
        {
            logger?.LogInformation("Loading agents from configuration...");

            // Create a scope to resolve the Kernel during construction
            using var scope = serviceProvider.CreateScope();
            var kernel = scope.ServiceProvider.GetRequiredService<Kernel>();

            // Load all teams and agents at startup
            _teams = AgentBuilder.CreateAllTeams(kernel, configuration);

            var teamCount = _teams.Count;
            var agentCount = _teams.SelectMany(t => t.Value).Count();

            logger?.LogInformation("Successfully loaded {AgentCount} agents across {TeamCount} teams", agentCount,
                teamCount);

            // Log team details
            foreach (var team in _teams)
                logger?.LogInformation("Team '{TeamName}' loaded with {AgentCount} agents: {AgentNames}",
                    team.Key,
                    team.Value.Count,
                    string.Join(", ", team.Value.Keys));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to load agents from configuration");
            throw;
        }
    }

    public ChatCompletionAgent[] GetTeamAgents(string teamName)
    {
        if (!_teams.TryGetValue(teamName, out var teamAgents))
            throw new InvalidOperationException($"Team '{teamName}' not found");

        return teamAgents.Values.ToArray();
    }

    public ChatCompletionAgent GetAgent(string teamName, string agentName)
    {
        if (!_teams.TryGetValue(teamName, out var teamAgents))
            throw new InvalidOperationException($"Team '{teamName}' not found");

        if (!teamAgents.TryGetValue(agentName, out var agent))
            throw new InvalidOperationException($"Agent '{agentName}' not found in team '{teamName}'");

        return agent;
    }

    public IEnumerable<string> GetTeamNames()
    {
        return _teams.Keys;
    }

    public IEnumerable<string> GetAgentNames(string teamName)
    {
        if (!_teams.TryGetValue(teamName, out var teamAgents))
            throw new InvalidOperationException($"Team '{teamName}' not found");

        return teamAgents.Keys;
    }

    public bool TeamExists(string teamName)
    {
        return _teams.ContainsKey(teamName);
    }

    public bool AgentExists(string teamName, string agentName)
    {
        return _teams.TryGetValue(teamName, out var teamAgents) &&
               teamAgents.ContainsKey(agentName);
    }
}