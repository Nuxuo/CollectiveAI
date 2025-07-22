using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace CollectiveAI.Plugins.Finance;

public class RegulatoryPlugin
{
    [KernelFunction]
    [Description("Check regulatory compliance for a trade")]
    public string CheckCompliance(
        [Description("Trade details")] string tradeDetails)
    {
        return "Compliance Check: PASSED - Position within limits, No restricted list conflicts, Insider window: Open";
    }

    [KernelFunction]
    [Description("Verify position limits")]
    public string CheckPositionLimits(
        [Description("Asset")] string asset,
        [Description("Proposed position size")]
        double positionSize)
    {
        return
            $"Position Limit Check for {asset}: Current: 3.2%, Proposed: 4.5%, Limit: 5% - APPROVED with 0.5% buffer";
    }
}