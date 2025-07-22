using CollectiveAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CollectiveAI.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(IAgentTeamService teamService, IAgentStoreService agentStore) : ControllerBase
{
    /// <summary>
    ///     Start a discussion with the AI team and get the final summary result
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
            if (string.IsNullOrWhiteSpace(request?.Message)) return BadRequest("Message cannot be empty");

            if (string.IsNullOrWhiteSpace(request?.Team)) return BadRequest("Team cannot be empty");

            if (!agentStore.TeamExists(request.Team)) return NotFound($"Team '{request.Team}' not found");

            var result =
                await teamService.DiscussTopicAsync(request.Team, request.Message, maxRounds, cancellationToken);
            return Ok(new ChatResponse
            {
                Result = result,
                Topic = request.Message,
                Team = request.Team,
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
    ///     Get all available teams
    /// </summary>
    /// <returns>List of available teams with their agents</returns>
    [HttpGet("teams")]
    public IActionResult GetTeams()
    {
        try
        {
            var teams = agentStore.GetTeamNames().Select(teamName => new TeamInfo
            {
                Name = teamName,
                Agents = agentStore.GetAgentNames(teamName).ToList(),
                AgentCount = agentStore.GetAgentNames(teamName).Count()
            }).ToList();

            return Ok(teams);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred retrieving teams", details = ex.Message });
        }
    }

    /// <summary>
    ///     Get agents for a specific team
    /// </summary>
    /// <param name="teamName">Name of the team</param>
    /// <returns>List of agents in the specified team</returns>
    [HttpGet("teams/{teamName}")]
    public IActionResult GetTeamAgents(string teamName)
    {
        try
        {
            if (!agentStore.TeamExists(teamName)) return NotFound($"Team '{teamName}' not found");

            var agents = agentStore.GetTeamAgents(teamName);
            var agentInfo = agents.Select(a => new AgentInfo
            {
                Name = a.Name,
                Description = a.Description
            }).ToList();

            return Ok(agentInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred retrieving team agents", details = ex.Message });
        }
    }
}

/// <summary>
///     Request model for chat discussions
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
}

/// <summary>
///     Response model for simple chat discussions
/// </summary>
public class ChatResponse
{
    public string Result { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public int MaxRounds { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
///     Information about a team
/// </summary>
public class TeamInfo
{
    public string Name { get; set; } = string.Empty;
    public List<string> Agents { get; set; } = [];
    public int AgentCount { get; set; }
}

/// <summary>
///     Information about an agent
/// </summary>
public class AgentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}