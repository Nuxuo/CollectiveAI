using System.ComponentModel;
using Microsoft.SemanticKernel;
using CollectiveAI.Services;

namespace CollectiveAI.Plugins.Finance;

// Market Discovery and Data Plugin - All from real APIs
public class MarketDataPlugin(IStockSimulationService simulationService)
{
    [KernelFunction]
    [Description("Get current real-time stock quote for any symbol")]
    public async Task<string> GetStockQuote(
        [Description("Stock ticker symbol (e.g., AAPL, MSFT, TSLA)")] string symbol)
    {
        Console.WriteLine($"[MarketDataPlugin] Getting real-time quote for {symbol}");
        try
        {
            var quote = await simulationService.GetStockQuoteAsync(symbol.ToUpper());
            Console.WriteLine($"[MarketDataPlugin] Retrieved quote: {quote.Symbol} at ${quote.Price:F2}");
            return $"{quote.Symbol}: ${quote.Price:F2} ({quote.Change:+0.00;-0.00}) {quote.ChangePercent:+0.0;-0.0}% | Volume: {quote.Volume:N0}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MarketDataPlugin] ERROR getting quote for {symbol}: {ex.Message}");
            return $"Error getting quote for {symbol}: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get current trending stocks from the market - discover what's hot today")]
    public async Task<string> GetTrendingStocks()
    {
        Console.WriteLine("[MarketDataPlugin] Fetching trending stocks");
        try
        {
            var trending = await simulationService.GetTrendingStocksAsync();
            Console.WriteLine($"[MarketDataPlugin] Found {trending.Count()} trending stocks");

            if (!trending.Any())
            {
                Console.WriteLine("[MarketDataPlugin] No trending stocks available");
                return "No trending stocks available at the moment.";
            }

            var result = "📈 Currently Trending Stocks:\n";
            var quotes = await simulationService.GetMultipleQuotesAsync(trending.Take(8).ToArray());
            Console.WriteLine($"[MarketDataPlugin] Retrieved quotes for {quotes.Count()} trending stocks");

            foreach (var quote in quotes)
            {
                result += $"{quote.Symbol}: ${quote.Price:F2} ({quote.ChangePercent:+0.0;-0.0}%)\n";
            }

            Console.WriteLine("[MarketDataPlugin] Successfully compiled trending stocks result");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MarketDataPlugin] ERROR getting trending stocks: {ex.Message}");
            return $"Error getting trending stocks: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Search for stocks by company name or keyword to discover investment opportunities")]
    public async Task<string> SearchStocks(
        [Description("Search term (company name, industry, keyword)")] string query)
    {
        Console.WriteLine($"[MarketDataPlugin] Searching stocks with query: '{query}'");
        try
        {
            var symbols = await simulationService.SearchStocksAsync(query);
            Console.WriteLine($"[MarketDataPlugin] Found {symbols.Count()} symbols for query '{query}'");

            if (!symbols.Any())
            {
                Console.WriteLine($"[MarketDataPlugin] No stocks found for search term '{query}'");
                return $"No stocks found for search term '{query}'.";
            }

            var result = $"🔍 Search results for '{query}':\n";
            var quotes = await simulationService.GetMultipleQuotesAsync(symbols.Take(5).ToArray());
            Console.WriteLine($"[MarketDataPlugin] Retrieved quotes for {quotes.Count()} search results");

            foreach (var quote in quotes)
            {
                result += $"{quote.Symbol}: ${quote.Price:F2} ({quote.ChangePercent:+0.0;-0.0}%)\n";
            }

            Console.WriteLine("[MarketDataPlugin] Successfully compiled search results");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MarketDataPlugin] ERROR searching stocks: {ex.Message}");
            return $"Error searching stocks: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get current market news and trends to inform trading decisions")]
    public async Task<string> GetMarketNews()
    {
        Console.WriteLine("[MarketDataPlugin] Fetching market news");
        try
        {
            var news = await simulationService.GetMarketNewsAsync(5);
            Console.WriteLine($"[MarketDataPlugin] Retrieved {news.Count()} news items");

            if (!news.Any())
            {
                Console.WriteLine("[MarketDataPlugin] No market news available");
                return "No market news available at the moment.";
            }

            var result = "📰 Market News & Trends:\n";
            foreach (var item in news)
            {
                result += $"• {item.Title}\n";
            }

            Console.WriteLine("[MarketDataPlugin] Successfully compiled market news");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MarketDataPlugin] ERROR getting market news: {ex.Message}");
            return $"Error getting market news: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Compare multiple stocks side by side")]
    public async Task<string> CompareStocks(
        [Description("Comma-separated list of stock symbols to compare")] string symbols)
    {
        Console.WriteLine($"[MarketDataPlugin] Comparing stocks: {symbols}");
        try
        {
            var symbolArray = symbols.Split(',').Select(s => s.Trim().ToUpper()).ToArray();
            Console.WriteLine($"[MarketDataPlugin] Parsed {symbolArray.Length} symbols for comparison");

            var quotes = await simulationService.GetMultipleQuotesAsync(symbolArray);
            Console.WriteLine($"[MarketDataPlugin] Retrieved {quotes.Count()} quotes for comparison");

            if (!quotes.Any())
            {
                Console.WriteLine("[MarketDataPlugin] No valid quotes found for comparison");
                return "No valid quotes found for the provided symbols.";
            }

            var result = "📊 Stock Comparison:\n";
            foreach (var quote in quotes.OrderByDescending(q => q.ChangePercent))
            {
                result += $"{quote.Symbol}: ${quote.Price:F2} ({quote.ChangePercent:+0.0;-0.0}%) Vol: {quote.Volume:N0}\n";
            }

            Console.WriteLine("[MarketDataPlugin] Successfully compiled stock comparison");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MarketDataPlugin] ERROR comparing stocks: {ex.Message}");
            return $"Error comparing stocks: {ex.Message}";
        }
    }
}

// Portfolio Management Plugin - Real positions and performance
public class PortfolioPlugin(IStockSimulationService simulationService)
{
    private const string DefaultPortfolioId = "default";

    [KernelFunction]
    [Description("Get current portfolio summary with all positions and real-time values")]
    public async Task<string> GetPortfolioSummary(
        [Description("Portfolio ID (default: 'default')")] string portfolioId = DefaultPortfolioId)
    {
        Console.WriteLine($"[PortfolioPlugin] Getting portfolio summary for: {portfolioId}");
        try
        {
            var summary = await simulationService.GetPortfolioSummaryAsync(portfolioId);
            Console.WriteLine($"[PortfolioPlugin] Retrieved summary - Total Value: ${summary.TotalValue:N2}, Positions: {summary.PositionCount}");

            var result = $"💼 Portfolio Summary:\n";
            result += $"Total Value: ${summary.TotalValue:N2}\n";
            result += $"Cash Available: ${summary.CashBalance:N2}\n";
            result += $"Total Return: ${summary.TotalReturn:N2} ({summary.TotalReturnPercent:+0.0;-0.0}%)\n";
            result += $"Number of Positions: {summary.PositionCount}\n\n";

            if (summary.PositionValues.Any())
            {
                Console.WriteLine($"[PortfolioPlugin] Processing {summary.PositionValues.Count} positions");
                result += "Current Holdings:\n";
                foreach (var position in summary.PositionValues.OrderByDescending(p => p.Value))
                {
                    var percentage = (position.Value / summary.TotalValue) * 100;
                    result += $"{position.Key}: ${position.Value:N0} ({percentage:F1}%)\n";
                }
            }
            else
            {
                Console.WriteLine("[PortfolioPlugin] No positions found - portfolio is all cash");
                result += "No current positions - portfolio is 100% cash.\n";
            }

            Console.WriteLine("[PortfolioPlugin] Successfully compiled portfolio summary");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortfolioPlugin] ERROR getting portfolio summary: {ex.Message}");
            return $"Error getting portfolio summary: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get detailed information about current stock positions with real-time P&L")]
    public async Task<string> GetPositionDetails(
        [Description("Portfolio ID (default: 'default')")] string portfolioId = DefaultPortfolioId)
    {
        Console.WriteLine($"[PortfolioPlugin] Getting position details for: {portfolioId}");
        try
        {
            var positions = await simulationService.GetPositionsAsync(portfolioId);
            var activePositions = positions.Values.Where(p => p.Quantity > 0);
            Console.WriteLine($"[PortfolioPlugin] Found {activePositions.Count()} active positions");

            if (!activePositions.Any())
            {
                Console.WriteLine("[PortfolioPlugin] No active positions found");
                return "No active positions in portfolio.";
            }

            var result = "📈 Position Details:\n";
            foreach (var position in activePositions.OrderBy(p => p.Symbol))
            {
                Console.WriteLine($"[PortfolioPlugin] Processing position: {position.Symbol} ({position.Quantity} shares)");
                var quote = await simulationService.GetStockQuoteAsync(position.Symbol);
                var currentValue = position.Quantity * quote.Price;
                var costBasis = position.Quantity * position.AveragePrice;
                var gainLoss = currentValue - costBasis;
                var gainLossPercent = (gainLoss / costBasis) * 100;

                result += $"\n{position.Symbol}:\n";
                result += $"  Shares: {position.Quantity:N0} @ ${position.AveragePrice:F2} avg cost\n";
                result += $"  Current: ${quote.Price:F2} | Market Value: ${currentValue:N2}\n";
                result += $"  P&L: ${gainLoss:+0,000;-0,000} ({gainLossPercent:+0.0;-0.0}%)\n";
            }

            Console.WriteLine("[PortfolioPlugin] Successfully compiled position details");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortfolioPlugin] ERROR getting position details: {ex.Message}");
            return $"Error getting position details: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get recent trading history and performance")]
    public async Task<string> GetTradingHistory(
        [Description("Number of days to look back")] int days = 7,
        [Description("Portfolio ID (default: 'default')")] string portfolioId = DefaultPortfolioId)
    {
        Console.WriteLine($"[PortfolioPlugin] Getting trading history for {days} days, portfolio: {portfolioId}");
        try
        {
            var trades = await simulationService.GetTradeHistoryAsync(portfolioId, days);
            var performance = await simulationService.CalculatePerformanceAsync(portfolioId, days);
            Console.WriteLine($"[PortfolioPlugin] Retrieved {trades.Count()} trades, performance calculated");

            if (!trades.Any())
            {
                Console.WriteLine($"[PortfolioPlugin] No trades found in the last {days} days");
                return $"No trades executed in the last {days} days.";
            }

            var result = $"📋 Trading History (Last {days} days):\n";
            result += $"Total Trades: {performance.TotalTrades} | Volume: ${performance.TotalVolume:N2}\n\n";

            foreach (var trade in trades.Take(10))
            {
                var action = trade.OrderType == OrderType.Buy ? "BOUGHT" : "SOLD";
                result += $"{trade.ExecutedAt:MMM dd HH:mm} - {action} {trade.Quantity:N0} {trade.Symbol} @ ${trade.Price:F2}\n";
            }

            Console.WriteLine("[PortfolioPlugin] Successfully compiled trading history");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortfolioPlugin] ERROR getting trading history: {ex.Message}");
            return $"Error getting trading history: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Calculate position size recommendation based on current portfolio and risk")]
    public async Task<string> CalculatePositionSize(
        [Description("Stock symbol to analyze")] string symbol,
        [Description("Risk percentage of portfolio to allocate (1-20)")] double riskPercent = 5.0,
        [Description("Portfolio ID (default: 'default')")] string portfolioId = DefaultPortfolioId)
    {
        Console.WriteLine($"[PortfolioPlugin] Calculating position size for {symbol} with {riskPercent}% risk");
        try
        {
            var summary = await simulationService.GetPortfolioSummaryAsync(portfolioId);
            var quote = await simulationService.GetStockQuoteAsync(symbol.ToUpper());
            Console.WriteLine($"[PortfolioPlugin] Portfolio value: ${summary.TotalValue:N2}, Stock price: ${quote.Price:F2}");

            var riskAmount = summary.TotalValue * (decimal)(riskPercent / 100);
            var maxShares = Math.Floor(riskAmount / quote.Price);
            var positionValue = maxShares * quote.Price;
            var portfolioPercent = (positionValue / summary.TotalValue) * 100;

            Console.WriteLine($"[PortfolioPlugin] Calculated max shares: {maxShares}, position value: ${positionValue:N2}");

            var result = $"📊 Position Sizing for {symbol.ToUpper()}:\n";
            result += $"Current Price: ${quote.Price:F2}\n";
            result += $"Available Cash: ${summary.CashBalance:N2}\n";
            result += $"Risk Budget ({riskPercent}%): ${riskAmount:N2}\n";
            result += $"Recommended Shares: {maxShares:N0}\n";
            result += $"Position Value: ${positionValue:N2} ({portfolioPercent:F1}% of portfolio)\n";

            if (positionValue > summary.CashBalance)
            {
                Console.WriteLine("[PortfolioPlugin] Insufficient cash for recommended position");
                result += $"⚠️ Insufficient cash - need ${positionValue - summary.CashBalance:N2} more";
            }
            else
            {
                Console.WriteLine("[PortfolioPlugin] Trade is feasible with current cash");
                result += $"✅ Trade feasible - ${summary.CashBalance - positionValue:N2} cash remaining";
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortfolioPlugin] ERROR calculating position size: {ex.Message}");
            return $"Error calculating position size: {ex.Message}";
        }
    }
}

// Trading Execution Plugin - Real buy/sell orders
public class TradingPlugin(IStockSimulationService simulationService)
{
    private const string DefaultPortfolioId = "default";

    [KernelFunction]
    [Description("Execute a buy order for a stock at current market price")]
    public async Task<string> BuyStock(
        [Description("Stock ticker symbol")] string symbol,
        [Description("Number of shares to buy")] decimal quantity,
        [Description("Portfolio ID (default: 'default')")] string portfolioId = DefaultPortfolioId)
    {
        Console.WriteLine($"[TradingPlugin] Executing BUY order: {quantity} shares of {symbol}");
        try
        {
            var order = new TradeOrder
            {
                Symbol = symbol.ToUpper(),
                OrderType = OrderType.Buy,
                Quantity = quantity
            };

            var result = await simulationService.ExecuteTradeAsync(portfolioId, order);
            Console.WriteLine($"[TradingPlugin] Buy order result - Success: {result.Success}");

            if (result.Success)
            {
                Console.WriteLine($"[TradingPlugin] BUY executed: {result.ExecutedQuantity} shares @ ${result.ExecutionPrice:F2}");
                return $"✅ BUY ORDER EXECUTED:\n" +
                       $"Symbol: {symbol.ToUpper()}\n" +
                       $"Quantity: {result.ExecutedQuantity:N0} shares\n" +
                       $"Execution Price: ${result.ExecutionPrice:F2}\n" +
                       $"Total Cost: ${result.TotalValue:N2}\n" +
                       $"Trade ID: {result.Trade!.TradeId[..8]}\n" +
                       $"Executed at: {result.Trade.ExecutedAt:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                Console.WriteLine($"[TradingPlugin] BUY order failed: {result.ErrorMessage}");
                return $"❌ BUY ORDER FAILED: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TradingPlugin] ERROR executing buy order: {ex.Message}");
            return $"❌ Error executing buy order: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Execute a sell order for a stock at current market price")]
    public async Task<string> SellStock(
        [Description("Stock ticker symbol")] string symbol,
        [Description("Number of shares to sell")] decimal quantity,
        [Description("Portfolio ID (default: 'default')")] string portfolioId = DefaultPortfolioId)
    {
        Console.WriteLine($"[TradingPlugin] Executing SELL order: {quantity} shares of {symbol}");
        try
        {
            var order = new TradeOrder
            {
                Symbol = symbol.ToUpper(),
                OrderType = OrderType.Sell,
                Quantity = quantity
            };

            var result = await simulationService.ExecuteTradeAsync(portfolioId, order);
            Console.WriteLine($"[TradingPlugin] Sell order result - Success: {result.Success}");

            if (result.Success)
            {
                Console.WriteLine($"[TradingPlugin] SELL executed: {result.ExecutedQuantity} shares @ ${result.ExecutionPrice:F2}");
                return $"✅ SELL ORDER EXECUTED:\n" +
                       $"Symbol: {symbol.ToUpper()}\n" +
                       $"Quantity: {result.ExecutedQuantity:N0} shares\n" +
                       $"Execution Price: ${result.ExecutionPrice:F2}\n" +
                       $"Total Proceeds: ${result.TotalValue:N2}\n" +
                       $"Trade ID: {result.Trade!.TradeId[..8]}\n" +
                       $"Executed at: {result.Trade.ExecutedAt:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                Console.WriteLine($"[TradingPlugin] SELL order failed: {result.ErrorMessage}");
                return $"❌ SELL ORDER FAILED: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TradingPlugin] ERROR executing sell order: {ex.Message}");
            return $"❌ Error executing sell order: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Check if we can execute a trade before actually doing it")]
    public async Task<string> CheckTradefeasibility(
        [Description("Stock symbol")] string symbol,
        [Description("Action: 'buy' or 'sell'")] string action,
        [Description("Number of shares")] decimal quantity,
        [Description("Portfolio ID (default: 'default')")] string portfolioId = DefaultPortfolioId)
    {
        Console.WriteLine($"[TradingPlugin] Checking trade feasibility: {action.ToUpper()} {quantity} shares of {symbol}");
        try
        {
            var summary = await simulationService.GetPortfolioSummaryAsync(portfolioId);
            var quote = await simulationService.GetStockQuoteAsync(symbol.ToUpper());
            var totalValue = quantity * quote.Price;
            Console.WriteLine($"[TradingPlugin] Trade value: ${totalValue:N2}, Available cash: ${summary.CashBalance:N2}");

            if (action.ToLower() == "buy")
            {
                var result = $"💰 BUY Feasibility Check for {symbol.ToUpper()}:\n";
                result += $"Current Price: ${quote.Price:F2}\n";
                result += $"Shares Requested: {quantity:N0}\n";
                result += $"Total Cost: ${totalValue:N2}\n";
                result += $"Available Cash: ${summary.CashBalance:N2}\n";

                if (totalValue <= summary.CashBalance)
                {
                    Console.WriteLine("[TradingPlugin] BUY trade is feasible");
                    result += $"✅ FEASIBLE - ${summary.CashBalance - totalValue:N2} cash remaining\n";
                    result += $"Portfolio Impact: {(totalValue / summary.TotalValue) * 100:F1}% allocation";
                }
                else
                {
                    Console.WriteLine("[TradingPlugin] BUY trade requires more cash");
                    result += $"❌ INSUFFICIENT FUNDS - Need ${totalValue - summary.CashBalance:N2} more";
                }

                return result;
            }
            else // sell
            {
                var positions = await simulationService.GetPositionsAsync(portfolioId);
                var availableShares = positions.GetValueOrDefault(symbol.ToUpper())?.Quantity ?? 0;
                Console.WriteLine($"[TradingPlugin] Available shares for {symbol}: {availableShares}");

                var result = $"📈 SELL Feasibility Check for {symbol.ToUpper()}:\n";
                result += $"Current Price: ${quote.Price:F2}\n";
                result += $"Shares to Sell: {quantity:N0}\n";
                result += $"Available Shares: {availableShares:N0}\n";
                result += $"Potential Proceeds: ${totalValue:N2}\n";

                if (quantity <= availableShares)
                {
                    Console.WriteLine("[TradingPlugin] SELL trade is feasible");
                    result += $"✅ FEASIBLE - Can sell {quantity:N0} shares";
                }
                else
                {
                    Console.WriteLine("[TradingPlugin] SELL trade requires more shares");
                    result += $"❌ INSUFFICIENT SHARES - Have {availableShares:N0}, need {quantity:N0}";
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TradingPlugin] ERROR checking trade feasibility: {ex.Message}");
            return $"Error checking trade feasibility: {ex.Message}";
        }
    }
}

// Market Analysis Plugin - Simple analysis tools
public class MarketAnalysisPlugin(IStockSimulationService simulationService)
{
    [KernelFunction]
    [Description("Analyze a stock's current performance and momentum")]
    public async Task<string> AnalyzeStock(
        [Description("Stock ticker symbol")] string symbol)
    {
        Console.WriteLine($"[MarketAnalysisPlugin] Analyzing stock: {symbol}");
        try
        {
            var quote = await simulationService.GetStockQuoteAsync(symbol.ToUpper());
            Console.WriteLine($"[MarketAnalysisPlugin] Retrieved quote for analysis: {quote.Symbol} @ ${quote.Price:F2}");

            var momentum = Math.Abs(quote.ChangePercent) > 3 ? "High" :
                          Math.Abs(quote.ChangePercent) > 1 ? "Medium" : "Low";

            var direction = quote.ChangePercent > 0 ? "Bullish" : quote.ChangePercent < 0 ? "Bearish" : "Neutral";

            var volumeStatus = quote.Volume > 10000000 ? "High" :
                              quote.Volume > 1000000 ? "Medium" : "Low";

            Console.WriteLine($"[MarketAnalysisPlugin] Analysis metrics - Direction: {direction}, Momentum: {momentum}, Volume: {volumeStatus}");

            var result = $"📊 Analysis for {quote.Symbol}:\n";
            result += $"Current Price: ${quote.Price:F2}\n";
            result += $"Daily Change: ${quote.Change:+0.00;-0.00} ({quote.ChangePercent:+0.0;-0.0}%)\n";
            result += $"Direction: {direction}\n";
            result += $"Momentum: {momentum}\n";
            result += $"Volume: {quote.Volume:N0} ({volumeStatus})\n";

            // Simple signal based on price movement and volume
            string signal;
            if (quote.ChangePercent > 2 && quote.Volume > 5000000)
            {
                signal = "📈 Signal: STRONG BUY momentum with high volume";
                Console.WriteLine("[MarketAnalysisPlugin] Generated STRONG BUY signal");
            }
            else if (quote.ChangePercent < -2 && quote.Volume > 5000000)
            {
                signal = "📉 Signal: STRONG SELL pressure with high volume";
                Console.WriteLine("[MarketAnalysisPlugin] Generated STRONG SELL signal");
            }
            else if (Math.Abs(quote.ChangePercent) < 1)
            {
                signal = "➡️ Signal: CONSOLIDATING - monitor for breakout";
                Console.WriteLine("[MarketAnalysisPlugin] Generated CONSOLIDATING signal");
            }
            else
            {
                signal = "⚖️ Signal: MIXED - wait for clearer direction";
                Console.WriteLine("[MarketAnalysisPlugin] Generated MIXED signal");
            }

            result += signal;

            Console.WriteLine("[MarketAnalysisPlugin] Successfully completed stock analysis");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MarketAnalysisPlugin] ERROR analyzing stock: {ex.Message}");
            return $"Error analyzing stock: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get a quick market overview and sentiment")]
    public async Task<string> GetMarketOverview()
    {
        Console.WriteLine("[MarketAnalysisPlugin] Getting market overview");
        try
        {
            var trending = await simulationService.GetTrendingStocksAsync();
            var news = await simulationService.GetMarketNewsAsync(3);
            Console.WriteLine($"[MarketAnalysisPlugin] Retrieved {trending.Count()} trending stocks and {news.Count()} news items");

            if (!trending.Any())
            {
                Console.WriteLine("[MarketAnalysisPlugin] No trending stocks available for market overview");
                return "Unable to get market data at this time.";
            }

            var quotes = await simulationService.GetMultipleQuotesAsync(trending.Take(5).ToArray());
            Console.WriteLine($"[MarketAnalysisPlugin] Analyzing {quotes.Count()} quotes for market sentiment");

            var gainers = quotes.Count(q => q.ChangePercent > 0);
            var losers = quotes.Count(q => q.ChangePercent < 0);
            var avgChange = quotes.Average(q => q.ChangePercent);

            var sentiment = avgChange > 1 ? "Bullish" : avgChange < -1 ? "Bearish" : "Mixed";
            Console.WriteLine($"[MarketAnalysisPlugin] Market sentiment: {sentiment} (avg change: {avgChange:F1}%)");

            var result = $"🌍 Market Overview:\n";
            result += $"Market Sentiment: {sentiment}\n";
            result += $"Trending Stocks: {gainers} up, {losers} down\n";
            result += $"Average Change: {avgChange:+0.0;-0.0}%\n\n";

            result += "Top Movers:\n";
            foreach (var quote in quotes.OrderByDescending(q => Math.Abs(q.ChangePercent)).Take(3))
            {
                result += $"{quote.Symbol}: {quote.ChangePercent:+0.0;-0.0}%\n";
            }

            if (news.Any())
            {
                Console.WriteLine($"[MarketAnalysisPlugin] Adding {news.Count()} market themes to overview");
                result += $"\nMarket Themes:\n";
                foreach (var item in news)
                {
                    result += $"• {item.Title}\n";
                }
            }

            Console.WriteLine("[MarketAnalysisPlugin] Successfully compiled market overview");
            return result.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MarketAnalysisPlugin] ERROR getting market overview: {ex.Message}");
            return $"Error getting market overview: {ex.Message}";
        }
    }
}