using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectiveAI.Finance.Plugins
{
    public class AssetAllocationPlugin
    {
        [KernelFunction, Description("Optimize asset allocation across asset classes")]
        public string OptimizeAssetAllocation(
            [Description("Risk tolerance (Conservative, Moderate, Aggressive)")] string riskTolerance,
            [Description("Investment horizon in years")] int horizon)
        {
            var allocations = new Dictionary<string, Dictionary<string, int>>
            {
                ["Conservative"] = new() { ["Stocks"] = 30, ["Bonds"] = 60, ["Alternatives"] = 10 },
                ["Moderate"] = new() { ["Stocks"] = 60, ["Bonds"] = 30, ["Alternatives"] = 10 },
                ["Aggressive"] = new() { ["Stocks"] = 80, ["Bonds"] = 10, ["Alternatives"] = 10 }
            };

            var allocation = allocations.GetValueOrDefault(riskTolerance, allocations["Moderate"]);
            return $"Optimal allocation for {riskTolerance} ({horizon}yr): Stocks {allocation["Stocks"]}%, Bonds {allocation["Bonds"]}%, Alts {allocation["Alternatives"]}%";
        }

        [KernelFunction, Description("Rebalancing recommendations based on current vs target allocation")]
        public string GetRebalancingRecommendations(
            [Description("Current allocation JSON")] string currentAllocation,
            [Description("Target allocation JSON")] string targetAllocation)
        {
            return "Rebalancing needed: Reduce Tech from 45% to 30% (-$3.2M), Increase Healthcare from 10% to 15% (+$1.1M), Add Energy position 5% (+$1.1M)";
        }

        [KernelFunction, Description("Calculate correlation matrix for portfolio diversification")]
        public string CalculateCorrelationMatrix(
            [Description("List of assets")] string assets)
        {
            return $"Correlation Matrix for {assets}: Highest correlation: AAPL-MSFT (0.85), Lowest: TLT-SPY (-0.42), Portfolio correlation: 0.65 (reduce for better diversification)";
        }
    }
}
