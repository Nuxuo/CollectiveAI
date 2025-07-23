using CollectiveAI.Managers;
using CollectiveAI.Monitors;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CollectiveAI.Services;

public interface IAgentTeamService
{
    Task<string> DiscussTopicAsync(string teamName, string input, int maxRounds = 5,
        CancellationToken cancellationToken = default);
}

public class AgentResponse
{
    public string AgentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class AgentTeamService(Kernel kernel, IAgentStoreService agentStore, ILogger<AgentTeamService>? logger = null)
    : IAgentTeamService
{
    private readonly int _resultTimeoutInSeconds = 60;

    public async Task<string> DiscussTopicAsync(string teamName, string input, int maxRounds = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate team exists
            if (!agentStore.TeamExists(teamName)) throw new InvalidOperationException($"Team '{teamName}' not found");

            // Create orchestration using stored agents
            var orchestration = CreateOrchestration(teamName, input, maxRounds);

            // Start the runtime
            InProcessRuntime runtime = new();
            await runtime.StartAsync(cancellationToken);

            logger?.LogInformation("Starting {TeamName} team discussion on: {Topic}", teamName, input);

            // Run the orchestration
            var result = await orchestration.InvokeAsync(input, runtime, cancellationToken);
            var response =
                await result.GetValueAsync(TimeSpan.FromSeconds(_resultTimeoutInSeconds * 60), cancellationToken);

            await runtime.RunUntilIdleAsync();

            logger?.LogInformation("{TeamName} team discussion completed successfully", teamName);
            return response;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during {TeamName} team discussion: {Message}", teamName, ex.Message);
            throw;
        }
    }

    private GroupChatOrchestration CreateOrchestration(string teamName, string topic, int maxRounds,
        DetailedOrchestrationMonitor? monitor = null)
    {
        // Get preloaded agents from store
        var agents = agentStore.GetTeamAgents(teamName);

        logger?.LogDebug("Creating orchestration for team '{TeamName}' with {AgentCount} agents: {AgentNames}",
            teamName,
            agents.Length,
            string.Join(", ", agents.Select(a => a.Name)));

        // Create orchestration with ResponseCallback set during initialization
        var orchestration = new GroupChatOrchestration(
            new AiGroupChatManager(topic, kernel.GetRequiredService<IChatCompletionService>())
            {
                MaximumInvocationCount = maxRounds
            },
            agents)
        {
            ResponseCallback = monitor?.ResponseCallback
        };

        return orchestration;
    }
}