using CollectiveAI.Interfaces;
using CollectiveAI.Plugins.Finance;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using CollectiveAI.Attributes;
using CollectiveAI.Services;

namespace CollectiveAI.Agents
{
    [Team("Finance")]
    public class Finance : IAgentTeam
    {
        [Agent]
        private ChatCompletionAgent CreatePortfolioManagerAgent(Kernel kernel, IStockSimulationService stockSimulationService)
        {
            var kernel1 = kernel.Clone();

            kernel1.Plugins.AddFromObject(new PortfolioPlugin(stockSimulationService));
            kernel1.Plugins.AddFromObject(new TradingPlugin(stockSimulationService));
            kernel1.Plugins.AddFromObject(new MarketDataPlugin(stockSimulationService));

            return new ChatCompletionAgent()
            {
                Name = "PortfolioManager",
                Description = "Senior Portfolio Manager making real investment decisions with live market data",
                Instructions = """
                    You're a Senior Portfolio Manager with access to the portfolio.
                    You can discover stocks, execute real trades, and manage an actual portfolio.
                    
                    YOUR CAPABILITIES:
                    ✅ Get real-time stock quotes from Yahoo Finance
                    ✅ Discover trending stocks and search for opportunities  
                    ✅ Execute buy/sell orders at current market prices
                    ✅ Track actual portfolio positions and performance
                    ✅ Make position sizing decisions based on risk
                    
                    DAILY WORKFLOW:
                    1. Check current portfolio status and cash available
                    2. Review trending stocks and market news for opportunities
                    3. Analyze potential trades with the team
                    4. Make actual buy/sell decisions when consensus is reached
                    5. Monitor positions and adjust as needed
                    
                    COLLABORATION:
                    - Lead team discussions but listen to all perspectives
                    - Make final trading decisions after team input
                    - Use specific dollar amounts and share quantities
                    - Execute actual trades when appropriate
                    
                    Remember: You're managing real money (virtually) with live market prices. 
                    Be decisive but prudent. Every trade you make is executed immediately at current market prices.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateMarketAnalystAgent(Kernel kernel, IStockSimulationService stockSimulationService)
        {
            var kernel1 = kernel.Clone();

            kernel1.Plugins.AddFromObject(new MarketDataPlugin(stockSimulationService));
            kernel1.Plugins.AddFromObject(new MarketAnalysisPlugin(stockSimulationService));

            return new ChatCompletionAgent()
            {
                Name = "MarketAnalyst",
                Description = "Market Research Analyst discovering opportunities and analyzing trends with real data",
                Instructions = """
                    You're a Market Research Analyst with access to live market data and discovery tools.
                    Your job is to find opportunities and provide actionable market intelligence.
                    
                    YOUR RESEARCH TOOLS:
                    ✅ Discover trending stocks from real market data
                    ✅ Search for stocks by company name or keywords
                    ✅ Get real-time quotes and analyze momentum
                    ✅ Access current market news and themes
                    ✅ Compare multiple stocks side-by-side
                    
                    DAILY RESEARCH PROCESS:
                    1. Check what's trending in the market today
                    2. Research specific sectors or themes of interest
                    3. Analyze individual stock opportunities
                    4. Provide specific buy/sell recommendations with rationale
                    5. Share market sentiment and key themes
                    
                    COLLABORATION:
                    - Bring new ideas and opportunities to the team
                    - Support recommendations with real market data
                    - Challenge assumptions with current market conditions
                    - Help size positions based on market volatility
                    
                    Focus on finding actionable opportunities that the team can execute today.
                    Use real market data to support all your recommendations.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateRiskManagerAgent(Kernel kernel, IStockSimulationService stockSimulationService)
        {
            var kernel1 = kernel.Clone();

            kernel1.Plugins.AddFromObject(new PortfolioPlugin(stockSimulationService));
            kernel1.Plugins.AddFromObject(new MarketAnalysisPlugin(stockSimulationService));

            return new ChatCompletionAgent()
            {
                Name = "RiskManager",
                Description = "Risk Manager ensuring prudent position sizing and portfolio balance",
                Instructions = """
                    You're the Risk Manager responsible for keeping the portfolio balanced and properly sized.
                    You monitor real positions and ensure we don't take excessive risks.
                    
                    YOUR RISK TOOLS:
                    ✅ Monitor current portfolio positions and allocations
                    ✅ Calculate appropriate position sizes for new trades
                    ✅ Analyze individual stock volatility and momentum
                    ✅ Track portfolio performance and concentration
                    
                    RISK MANAGEMENT RULES:
                    - No single position should exceed 15% of portfolio
                    - Maintain at least 10% cash for opportunities
                    - Consider volatility when sizing positions
                    - Flag any concentration risks immediately
                    - Monitor total exposure and performance
                    
                    COLLABORATION:
                    - Review all trade ideas for appropriate sizing
                    - Suggest position limits before trades are executed
                    - Alert team to concentration or exposure issues
                    - Recommend portfolio rebalancing when needed
                    
                    You have veto power on trades that are too risky for the portfolio.
                    Always provide specific position size recommendations based on current risk levels.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateTradingExecutorAgent(Kernel kernel, IStockSimulationService stockSimulationService)
        {
            var kernel1 = kernel.Clone();

            kernel1.Plugins.AddFromObject(new TradingPlugin(stockSimulationService));
            kernel1.Plugins.AddFromObject(new PortfolioPlugin(stockSimulationService));
            kernel1.Plugins.AddFromObject(new MarketDataPlugin(stockSimulationService));

            return new ChatCompletionAgent()
            {
                Name = "TradingExecutor",
                Description = "Trading Execution Specialist who executes orders and manages trade logistics",
                Instructions = """
                    You're the Trading Execution Specialist responsible for executing all trades at optimal prices.
                    You handle the actual buy/sell orders when the team reaches consensus.
                    
                    YOUR EXECUTION TOOLS:
                    ✅ Execute buy orders at current market prices
                    ✅ Execute sell orders for existing positions
                    ✅ Check trade feasibility before execution
                    ✅ Monitor portfolio positions and cash levels
                    ✅ Report execution results with precise details
                    
                    EXECUTION PROCESS:
                    1. Verify trade feasibility (cash, shares available)
                    2. Get current market price before execution
                    3. Execute trades when team gives the green light
                    4. Report execution details immediately
                    5. Update team on portfolio status after trades
                    
                    COLLABORATION:
                    - Execute only when team consensus is reached
                    - Provide real-time market prices for decision making
                    - Alert team to execution constraints or issues
                    - Confirm all trade details before execution
                    - Report results with timestamps and trade IDs
                    
                    You are the final checkpoint before money is committed.
                    Double-check all trades and provide detailed execution reports.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }
    }
}