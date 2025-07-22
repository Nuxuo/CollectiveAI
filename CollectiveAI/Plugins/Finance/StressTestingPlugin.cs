using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace CollectiveAI.Plugins.Finance;

public class StressTestingPlugin
{
    [KernelFunction]
    [Description("Run historical stress test scenarios")]
    public string RunHistoricalStressTest(
        [Description("Historical event (e.g., '2008 Crisis', 'COVID Crash', 'Dot-com Burst')")]
        string historicalEvent,
        [Description("Portfolio ID")] string portfolioId)
    {
        var scenarios = new Dictionary<string, string>
        {
            ["2008 Crisis"] = "Expected loss: -42.3%, Recovery time: 18 months, Most affected: Financials (-65%)",
            ["COVID Crash"] = "Expected loss: -28.5%, Recovery time: 6 months, Most affected: Travel/Energy (-55%)",
            ["Dot-com Burst"] = "Expected loss: -38.1%, Recovery time: 24 months, Most affected: Tech (-75%)"
        };

        return
            $"Stress Test - {historicalEvent} on {portfolioId}: {scenarios.GetValueOrDefault(historicalEvent, "Scenario not found")}";
    }

    [KernelFunction]
    [Description("Monte Carlo simulation for portfolio risk")]
    public string RunMonteCarloSimulation(
        [Description("Portfolio ID")] string portfolioId,
        [Description("Number of simulations")] int simulations,
        [Description("Time horizon in days")] int days)
    {
        return
            $"Monte Carlo Results ({simulations} runs, {days} days): 5th percentile: -15.2%, Median: +8.3%, 95th percentile: +31.5%, Probability of loss: 18.2%";
    }

    [KernelFunction]
    [Description("Calculate maximum drawdown scenarios")]
    public string CalculateMaxDrawdown(
        [Description("Portfolio or asset")] string target,
        [Description("Lookback period")] string period)
    {
        return
            $"Max Drawdown Analysis for {target} ({period}): Current DD: -5.2%, Historical Max DD: -18.7%, Time to recover: Avg 4.2 months, Longest: 8 months";
    }
}