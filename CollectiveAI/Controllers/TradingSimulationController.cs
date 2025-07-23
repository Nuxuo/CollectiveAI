using CollectiveAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CollectiveAI.Controllers;

[ApiController]
[Route("api/trading")]
public class TradingSimulationController : ControllerBase
{
    private readonly IStockSimulationService _simulationService;
    private readonly ILogger<TradingSimulationController> _logger;

    public TradingSimulationController(IStockSimulationService simulationService, ILogger<TradingSimulationController> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Get current portfolio summary
    /// </summary>
    [HttpGet("portfolio/{portfolioId?}")]
    public async Task<IActionResult> GetPortfolio(string portfolioId = "default")
    {
        try
        {
            var summary = await _simulationService.GetPortfolioSummaryAsync(portfolioId);
            var positions = await _simulationService.GetPositionsAsync(portfolioId);

            return Ok(new
            {
                Summary = summary,
                Positions = positions.Values.Where(p => p.Quantity > 0).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get trade history
    /// </summary>
    [HttpGet("history/{portfolioId?}")]
    public async Task<IActionResult> GetTradeHistory(string portfolioId = "default", int days = 30)
    {
        try
        {
            var trades = await _simulationService.GetTradeHistoryAsync(portfolioId, days);
            return Ok(trades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trade history for {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get portfolio performance metrics
    /// </summary>
    [HttpGet("performance/{portfolioId?}")]
    public async Task<IActionResult> GetPerformance(string portfolioId = "default", int days = 30)
    {
        try
        {
            var performance = await _simulationService.CalculatePerformanceAsync(portfolioId, days);

            return Ok(new
            {
                Performance = performance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance for {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}