using CollectiveAI.Attributes;
using CollectiveAI.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Reflection;

namespace CollectiveAI.Factories
{
    public static class AgentBuilder
    {
        /// <summary>
        /// Creates all teams by discovering classes that implement IAgentTeam and have the Team attribute
        /// </summary>
        /// <param name="kernel">The semantic kernel instance</param>
        /// <param name="configuration">Configuration (for backward compatibility, can be null)</param>
        /// <returns>Dictionary of team name to agents</returns>
        public static Dictionary<string, Dictionary<string, ChatCompletionAgent>> CreateAllTeams(
            Kernel kernel,
            IConfiguration? configuration = null)
        {
            var teams = new Dictionary<string, Dictionary<string, ChatCompletionAgent>>();

            // Get all types that implement IAgentTeam
            var agentTeamTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IAgentTeam).IsAssignableFrom(type)
                              && !type.IsInterface
                              && !type.IsAbstract)
                .ToList();

            foreach (var teamType in agentTeamTypes)
            {
                var teamAttribute = teamType.GetCustomAttribute<TeamAttribute>();
                if (teamAttribute == null)
                {
                    throw new InvalidOperationException(
                        $"Agent team class {teamType.Name} must have a [Team] attribute");
                }

                var teamName = teamAttribute.Name;
                var agents = CreateTeamAgents(teamType, kernel);

                if (agents.Any())
                {
                    teams[teamName] = agents;
                }
            }

            return teams;
        }

        /// <summary>
        /// Creates all agents for a specific team by calling methods marked with [Agent] attribute
        /// </summary>
        /// <param name="teamType">The team class type</param>
        /// <param name="kernel">The semantic kernel instance</param>
        /// <returns>Dictionary of agent name to ChatCompletionAgent</returns>
        private static Dictionary<string, ChatCompletionAgent> CreateTeamAgents(Type teamType, Kernel kernel)
        {
            var agents = new Dictionary<string, ChatCompletionAgent>();

            var teamInstance = CreateTeamInstance(teamType, kernel);

            var agentMethods = teamType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<AgentAttribute>() != null)
                .ToList();

            foreach (var method in agentMethods)
            {
                try
                {
                    var agent = (ChatCompletionAgent)method.Invoke(teamInstance, new object[] { kernel })!;
                    agents[agent.Name!] = agent;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to create agent from method {teamType.Name}.{method.Name}: {ex.Message}", ex);
                }
            }

            return agents;
        }

        /// <summary>
        /// Creates an instance of the team class
        /// </summary>
        private static IAgentTeam CreateTeamInstance(Type teamType, Kernel kernel)
        {
            // Look for a parameterless constructor
            var defaultConstructor = teamType.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor != null)
            {
                return (IAgentTeam)Activator.CreateInstance(teamType)!;
            }

            // Look for a constructor that takes a Kernel parameter (for backward compatibility)
            var constructorWithKernel = teamType.GetConstructor(new[] { typeof(Kernel) });
            if (constructorWithKernel != null)
            {
                return (IAgentTeam)Activator.CreateInstance(teamType, kernel)!;
            }

            throw new InvalidOperationException(
                $"Team class {teamType.Name} must have either a parameterless constructor or a constructor that takes Kernel parameter");
        }
    }
}