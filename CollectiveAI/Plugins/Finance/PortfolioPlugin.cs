using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace CollectiveAI.Plugins.Finance;

public class PortfolioPlugin
{
    [KernelFunction]
    [Description("Analyze portfolio performance metrics")]
    public string AnalyzePortfolioPerformance(
        [Description("Portfolio ID")] string portfolioId,
        [Description("Time period (e.g., YTD, 1Y, 3Y)")]
        string period)
    {
        // Simulated portfolio analysis
        return $"Portfolio {portfolioId} performance for {period}: Return: 12.5%, Sharpe: 1.2, Max Drawdown: -8.3%";
    }

    [KernelFunction]
    [Description("Calculate optimal position size based on Kelly Criterion")]
    public string CalculatePositionSize(
        [Description("Win probability")] double winProbability,
        [Description("Win/Loss ratio")] double winLossRatio,
        [Description("Portfolio value")] double portfolioValue)
    {
        var kellyPercentage = (winProbability * winLossRatio - (1 - winProbability)) / winLossRatio;
        var positionSize = portfolioValue * Math.Max(0, Math.Min(kellyPercentage, 0.25)); // Cap at 25%
        return $"Optimal position size: ${positionSize:N0} ({kellyPercentage * 100:F1}% Kelly, capped at 25%)";
    }
}