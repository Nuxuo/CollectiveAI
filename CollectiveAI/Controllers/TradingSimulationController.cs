using CollectiveAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CollectiveAI.Controllers;

[ApiController]
[Route("api/trading")]
public class TradingSimulationController(
    IStockSimulationService simulationService,
    ILogger<TradingSimulationController> logger)
    : ControllerBase
{
    /// <summary>
    /// Get current portfolio summary
    /// </summary>
    [HttpGet("portfolio")]
    public async Task<IActionResult> GetPortfolio()
    {
        try
        {
            var summary = await simulationService.GetPortfolioSummaryAsync();
            var positions = await simulationService.GetPositionsAsync();

            return Ok(new
            {
                Summary = summary,
                Positions = positions.Values.Where(p => p.Quantity > 0).ToList()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting portfolio");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get trade history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetTradeHistory(int days = 30)
    {
        try
        {
            var trades = await simulationService.GetTradeHistoryAsync(days);
            return Ok(trades);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting trade history for");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get portfolio performance metrics
    /// </summary>
    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance(int days = 30)
    {
        try
        {
            var performance = await simulationService.CalculatePerformanceAsync(days);

            return Ok(new
            {
                Performance = performance
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting performance");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}