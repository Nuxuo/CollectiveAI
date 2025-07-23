// Services/AgentService.cs
using CollectiveAI.Agents;
using CollectiveAI.Managers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CollectiveAI.Services;

public interface IAgentService
{
    Task<string> DiscussAsync(string input, int maxRounds = 5, CancellationToken cancellationToken = default);
    ChatCompletionAgent[] GetAgents();
}

public class AgentService : IAgentService
{
    private readonly ChatCompletionAgent[] _agents;
    private readonly Kernel _kernel;
    private readonly ILogger<AgentService>? _logger;
    private readonly int _resultTimeoutInSeconds = 60;

    public AgentService(Kernel kernel, ILogger<AgentService>? logger = null)
    {
        _kernel = kernel;
        _logger = logger;
        _agents = FinanceAgents.CreateAllAgents(kernel);

        logger?.LogInformation("Initialized {AgentCount} finance agents: {AgentNames}",
            _agents.Length,
            string.Join(", ", _agents.Select(a => a.Name)));
    }

    public ChatCompletionAgent[] GetAgents()
    {
        return _agents;
    }

    public async Task<string> DiscussAsync(string input, int maxRounds = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Starting finance team discussion on: {Topic}", input);

            // Create orchestration
            var orchestration = new GroupChatOrchestration(
                new AiGroupChatManager(input, _kernel.GetRequiredService<IChatCompletionService>())
                {
                    MaximumInvocationCount = maxRounds
                },
                _agents);

            // Start the runtime
            InProcessRuntime runtime = new();
            await runtime.StartAsync(cancellationToken);

            // Run the orchestration
            var result = await orchestration.InvokeAsync(input, runtime, cancellationToken);
            var response = await result.GetValueAsync(TimeSpan.FromSeconds(_resultTimeoutInSeconds * 60), cancellationToken);

            await runtime.RunUntilIdleAsync();

            _logger?.LogInformation("Finance team discussion completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during finance team discussion: {Message}", ex.Message);
            throw;
        }
    }
}