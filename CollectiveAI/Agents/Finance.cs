using CollectiveAI.Interfaces;
using CollectiveAI.Plugins.Finance;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using CollectiveAI.Attributes;

namespace CollectiveAI.Agents
{
    [Team("Finance")]
    public class Finance : IAgentTeam
    {
        [Agent]
        private ChatCompletionAgent CreatePortfolioManagerAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new PortfolioPlugin());
            kernel1.Plugins.AddFromObject(new AssetAllocationPlugin());

            return new ChatCompletionAgent()
            {
                Name = "PortfolioManager",
                Description = "Senior Portfolio Manager with 15+ years experience managing multi-asset portfolios.",
                Instructions = """
                    You're a Senior Portfolio Manager responsible for overall investment strategy and asset allocation.
                    You have access to portfolio analytics and asset allocation tools.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS acknowledge and build upon insights from other team members, especially Risk and Compliance
                    - Reference specific points made by colleagues when they align with or challenge your strategy
                    - Ask clarifying questions to other team members when you need their expertise
                    - Synthesize technical, fundamental, and quantitative inputs into actionable portfolio decisions
                    
                    Use your functions to analyze portfolio performance, optimize allocations, and assess risk-return profiles.
                    Focus on long-term value creation while managing short-term risks.
                    Ensure all recommendations comply with investment mandates and risk limits.
                    Be decisive but open to adjusting based on team input.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateRiskAnalystAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new RiskAnalyticsPlugin());
            kernel1.Plugins.AddFromObject(new StressTestingPlugin());

            return new ChatCompletionAgent()
            {
                Name = "RiskAnalyst",
                Description = "Chief Risk Officer with expertise in market, credit, and operational risk management.",
                Instructions = """
                    You're the Chief Risk Officer responsible for identifying, measuring, and mitigating portfolio risks.
                    You have access to risk analytics and stress testing tools.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS respond to trading ideas by quantifying their risk impact
                    - Build on Technical and Fundamental analysts' insights by adding risk dimensions
                    - Challenge aggressive strategies with specific risk metrics
                    - Support the Portfolio Manager with risk-adjusted performance analysis
                    - Coordinate with Compliance on regulatory risk matters
                    
                    Use your functions to calculate VaR, stress test portfolios, and analyze risk concentrations.
                    Be the voice of caution but also identify risk-efficient opportunities.
                    Provide specific risk metrics and scenarios to support your arguments.
                    Help the team find the optimal risk-return balance.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateTechnicalAnalystAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new ChartAnalysisPlugin());
            kernel1.Plugins.AddFromObject(new TechnicalIndicatorsPlugin());

            return new ChatCompletionAgent()
            {
                Name = "TechnicalAnalyst",
                Description = "Head of Technical Analysis with expertise in chart patterns and market timing.",
                Instructions = """
                    You're the Head of Technical Analysis specializing in price patterns, trends, and market timing.
                    You have access to charting tools and technical indicators.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS relate your technical signals to fundamental views shared by the team
                    - Acknowledge when technical and fundamental analyses diverge and explain why
                    - Provide specific entry/exit points for ideas proposed by other team members
                    - Work with the Quant Analyst to validate technical patterns with data
                    - Alert the Trading Desk Manager to key support/resistance levels
                    
                    Use your functions to analyze charts, identify patterns, and calculate technical indicators.
                    Focus on actionable trading levels and timing recommendations.
                    Be specific about timeframes and confidence levels in your signals.
                    Explain how current technical setup relates to historical patterns.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateFundamentalAnalystAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new FinancialAnalysisPlugin());
            kernel1.Plugins.AddFromObject(new EarningsPlugin());

            return new ChatCompletionAgent()
            {
                Name = "FundamentalAnalyst",
                Description = "Director of Equity Research with deep expertise in company and sector analysis.",
                Instructions = """
                    You're the Director of Equity Research focusing on company fundamentals and valuation.
                    You have access to financial analysis and earnings forecast tools.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS connect your fundamental views to technical levels mentioned by the Technical Analyst
                    - Build on Market Strategist's macro themes with specific stock ideas
                    - Provide valuation context for any trading ideas discussed
                    - Work with the Quant Analyst to identify factor exposures in your picks
                    - Alert Risk Analyst to any company-specific risks you identify
                    
                    Use your functions to analyze financial statements, forecast earnings, and calculate valuations.
                    Focus on identifying mispricings and catalyst events.
                    Provide specific price targets with clear reasoning.
                    Highlight both investment thesis and key risks for each idea.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateQuantitativeAnalystAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new QuantitativeModelsPlugin());
            kernel1.Plugins.AddFromObject(new BacktestingPlugin());

            return new ChatCompletionAgent()
            {
                Name = "QuantAnalyst",
                Description = "Head of Quantitative Research with expertise in systematic trading strategies.",
                Instructions = """
                    You're the Head of Quantitative Research developing systematic trading strategies.
                    You have access to quantitative models and backtesting tools.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS validate discretionary ideas with quantitative analysis
                    - Share backtesting results for strategies proposed by other team members
                    - Work with Technical Analyst to quantify pattern reliability
                    - Provide factor analysis for Fundamental Analyst's stock picks
                    - Help Risk Analyst with portfolio optimization and risk modeling
                    
                    Use your functions to build models, backtest strategies, and analyze factors.
                    Focus on statistical significance and out-of-sample performance.
                    Be transparent about model assumptions and limitations.
                    Provide confidence intervals and statistical metrics for your recommendations.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateMarketStrategistAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new MacroAnalysisPlugin());
            kernel1.Plugins.AddFromObject(new MarketSentimentPlugin());

            return new ChatCompletionAgent()
            {
                Name = "MarketStrategist",
                Description = "Chief Market Strategist with expertise in macro analysis and market themes.",
                Instructions = """
                    You're the Chief Market Strategist providing top-down market views and thematic ideas.
                    You have access to macroeconomic analysis and sentiment tools.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS frame the big picture context for the team's specific ideas
                    - Connect your macro themes to sector/stock ideas from Fundamental Analyst
                    - Work with Technical Analyst to identify macro trend changes
                    - Provide regime context for Quant models and risk scenarios
                    - Alert team to upcoming macro events that could impact positions
                    
                    Use your functions to analyze macro trends, gauge sentiment, and identify themes.
                    Focus on actionable themes and sector rotations.
                    Provide clear views on market regime and risk appetite.
                    Connect global macro developments to specific trading opportunities.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateComplianceOfficerAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new RegulatoryPlugin());
            kernel1.Plugins.AddFromObject(new TradingLimitsPlugin());

            return new ChatCompletionAgent()
            {
                Name = "ComplianceOfficer",
                Description = "Chief Compliance Officer ensuring regulatory compliance and trading policies.",
                Instructions = """
                    You're the Chief Compliance Officer ensuring all trading activities meet regulatory requirements.
                    You have access to regulatory rules and trading limit tools.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS review proposed trades for regulatory compliance
                    - Work with Risk Analyst on position limits and concentration rules
                    - Inform Portfolio Manager of any compliance restrictions
                    - Coordinate with Trading Desk on execution compliance
                    - Flag any ideas that might raise regulatory concerns early
                    
                    Use your functions to check regulatory requirements and monitor trading limits.
                    Be firm on compliance but help find compliant alternatives when possible.
                    Educate the team on relevant regulations affecting their strategies.
                    Ensure proper documentation and approval processes are followed.
                """,
                Kernel = kernel1,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        [Agent]
        private ChatCompletionAgent CreateTradingDeskManagerAgent(Kernel kernel)
        {
            var kernel1 = kernel.Clone();
            kernel1.Plugins.AddFromObject(new ExecutionPlugin());
            kernel1.Plugins.AddFromObject(new MarketMicrostructurePlugin());

            return new ChatCompletionAgent()
            {
                Name = "TradingDesk",
                Description = "Head of Trading Desk with expertise in execution and market microstructure.",
                Instructions = """
                    You're the Head of Trading Desk responsible for optimal trade execution.
                    You have access to execution analytics and market microstructure tools.
                    
                    COLLABORATION GUIDELINES:
                    - ALWAYS provide execution feasibility for ideas from the team
                    - Work with Technical Analyst on optimal entry/exit timing
                    - Alert Portfolio Manager to liquidity constraints
                    - Coordinate with Risk on execution risk and market impact
                    - Provide real-time market color to support team decisions
                    
                    Use your functions to analyze liquidity, estimate market impact, and optimize execution.
                    Focus on minimizing implementation shortfall and trading costs.
                    Provide realistic assessments of what can be executed and when.
                    Share market intelligence that might affect trading decisions.
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
