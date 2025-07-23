using CollectiveAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CollectiveAI.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(IAgentService agentService) : ControllerBase
{
    /// <summary>
    /// Start a discussion with the AI finance team and get the final summary result
    /// </summary>
    /// <param name="request">The topic or question to discuss</param>
    /// <param name="maxRounds">Maximum number of discussion rounds (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The final discussion summary</returns>
    [HttpPost("discuss")]
    public async Task<IActionResult> Discuss(
        [FromBody] ChatRequest request,
        [FromQuery] int maxRounds = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest("Message cannot be empty");

            var result = await agentService.DiscussAsync(request.Message, maxRounds, cancellationToken);

            return Ok(new ChatResponse
            {
                Result = result,
                Topic = request.Message,
                MaxRounds = maxRounds,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred during discussion", details = ex.Message });
        }
    }

    /// <summary>
    /// Get information about the finance team agents
    /// </summary>
    /// <returns>List of finance agents</returns>
    [HttpGet("agents")]
    public IActionResult GetAgents()
    {
        try
        {
            var agents = agentService.GetAgents();
            var agentInfo = agents.Select(a => new AgentInfo
            {
                Name = a.Name,
                Description = a.Description
            }).ToList();

            return Ok(new
            {
                AgentCount = agents.Length,
                Agents = agentInfo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred retrieving agents", details = ex.Message });
        }
    }
}

/// <summary>
///     Request model for chat discussions
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
///     Response model for simple chat discussions
/// </summary>
public class ChatResponse
{
    public string Result { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int MaxRounds { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
///     Information about an agent
/// </summary>
public class AgentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}