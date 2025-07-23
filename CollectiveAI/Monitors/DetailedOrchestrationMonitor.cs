using CollectiveAI.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration;

namespace CollectiveAI.Monitors;

public class DetailedOrchestrationMonitor
{
    public List<AgentResponse> Responses { get; } = [];

    public OrchestrationResponseCallback ResponseCallback => HandleChatMessageAsync;

    private ValueTask HandleChatMessageAsync(ChatMessageContent chatMessage)
    {
        var response = new AgentResponse
        {
            AgentName = chatMessage.AuthorName ?? "Unknown",
            Message = chatMessage.Content ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };

        Responses.Add(response);
        return ValueTask.CompletedTask;
    }

    public class AgentResponse
    {
        public string AgentName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}