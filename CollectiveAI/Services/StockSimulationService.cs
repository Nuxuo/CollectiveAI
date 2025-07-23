using System.Text.Json;
using System.Globalization;

namespace CollectiveAI.Services;

public interface IStockSimulationService
{
    // Portfolio Management
    Task<decimal> GetCashBalanceAsync(string portfolioId);
    Task<Dictionary<string, Position>> GetPositionsAsync(string portfolioId);
    Task<PortfolioSummary> GetPortfolioSummaryAsync(string portfolioId);

    // Trading
    Task<TradeResult> ExecuteTradeAsync(string portfolioId, TradeOrder order);
    Task<List<Trade>> GetTradeHistoryAsync(string portfolioId, int days = 30);

    // Market Data - All from real APIs
    Task<StockQuote> GetStockQuoteAsync(string symbol);
    Task<List<StockQuote>> GetMultipleQuotesAsync(string[] symbols);
    Task<List<MarketNews>> GetMarketNewsAsync(int limit = 10);
    Task<List<string>> GetTrendingStocksAsync();
    Task<List<string>> SearchStocksAsync(string query);

    // Portfolio Analytics
    Task<PerformanceMetrics> CalculatePerformanceAsync(string portfolioId, int days = 30);

    // Initialization
    Task InitializePortfolioAsync(string portfolioId, decimal initialCash = 10_000);
}

public class StockSimulationService : IStockSimulationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockSimulationService> _logger;
    private readonly Dictionary<string, SimulatedPortfolio> _portfolios = new();

    public StockSimulationService(HttpClient httpClient, ILogger<StockSimulationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Initialize with a default portfolio
        InitializePortfolioAsync("default", 10_000).Wait();
    }

    public async Task<decimal> GetCashBalanceAsync(string portfolioId)
    {
        var portfolio = GetOrCreatePortfolio(portfolioId);
        return portfolio.CashBalance;
    }

    public async Task<Dictionary<string, Position>> GetPositionsAsync(string portfolioId)
    {
        var portfolio = GetOrCreatePortfolio(portfolioId);
        return portfolio.Positions.ToDictionary(p => p.Key, p => p.Value);
    }

    public async Task<PortfolioSummary> GetPortfolioSummaryAsync(string portfolioId)
    {
        var portfolio = GetOrCreatePortfolio(portfolioId);
        var positions = await GetPositionsAsync(portfolioId);

        decimal totalValue = portfolio.CashBalance;
        var positionValues = new Dictionary<string, decimal>();

        foreach (var position in positions.Values.Where(p => p.Quantity > 0))
        {
            try
            {
                var quote = await GetStockQuoteAsync(position.Symbol);
                var positionValue = position.Quantity * quote.Price;
                totalValue += positionValue;
                positionValues[position.Symbol] = positionValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get quote for {Symbol}", position.Symbol);
                // Use last known price if API fails
                var positionValue = position.Quantity * position.AveragePrice;
                totalValue += positionValue;
                positionValues[position.Symbol] = positionValue;
            }
        }

        return new PortfolioSummary
        {
            PortfolioId = portfolioId,
            TotalValue = totalValue,
            CashBalance = portfolio.CashBalance,
            InitialValue = portfolio.InitialCash,
            TotalReturn = totalValue - portfolio.InitialCash,
            TotalReturnPercent = ((totalValue - portfolio.InitialCash) / portfolio.InitialCash) * 100,
            PositionCount = positions.Values.Count(p => p.Quantity > 0),
            PositionValues = positionValues,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<TradeResult> ExecuteTradeAsync(string portfolioId, TradeOrder order)
    {
        var portfolio = GetOrCreatePortfolio(portfolioId);

        try
        {
            // Get current market price from Yahoo Finance
            var quote = await GetStockQuoteAsync(order.Symbol);
            var executionPrice = quote.Price;
            var totalCost = order.Quantity * executionPrice;

            if (order.OrderType == OrderType.Buy)
            {
                if (totalCost > portfolio.CashBalance)
                {
                    return new TradeResult
                    {
                        Success = false,
                        ErrorMessage = $"Insufficient funds. Required: ${totalCost:N2}, Available: ${portfolio.CashBalance:N2}",
                        ExecutedQuantity = 0
                    };
                }

                portfolio.CashBalance -= totalCost;

                if (portfolio.Positions.ContainsKey(order.Symbol))
                {
                    var existingPosition = portfolio.Positions[order.Symbol];
                    var newTotalQuantity = existingPosition.Quantity + order.Quantity;
                    var newTotalCost = (existingPosition.Quantity * existingPosition.AveragePrice) + totalCost;

                    portfolio.Positions[order.Symbol] = new Position
                    {
                        Symbol = order.Symbol,
                        Quantity = newTotalQuantity,
                        AveragePrice = newTotalCost / newTotalQuantity,
                        LastUpdated = DateTime.UtcNow
                    };
                }
                else
                {
                    portfolio.Positions[order.Symbol] = new Position
                    {
                        Symbol = order.Symbol,
                        Quantity = order.Quantity,
                        AveragePrice = executionPrice,
                        LastUpdated = DateTime.UtcNow
                    };
                }
            }
            else // Sell
            {
                if (!portfolio.Positions.ContainsKey(order.Symbol) ||
                    portfolio.Positions[order.Symbol].Quantity < order.Quantity)
                {
                    return new TradeResult
                    {
                        Success = false,
                        ErrorMessage = $"Insufficient shares. Requested: {order.Quantity}, Available: {portfolio.Positions.GetValueOrDefault(order.Symbol)?.Quantity ?? 0}",
                        ExecutedQuantity = 0
                    };
                }

                portfolio.CashBalance += totalCost;
                var position = portfolio.Positions[order.Symbol];
                position.Quantity -= order.Quantity;

                if (position.Quantity == 0)
                {
                    portfolio.Positions.Remove(order.Symbol);
                }
            }

            var trade = new Trade
            {
                TradeId = Guid.NewGuid().ToString(),
                PortfolioId = portfolioId,
                Symbol = order.Symbol,
                OrderType = order.OrderType,
                Quantity = order.Quantity,
                Price = executionPrice,
                TotalValue = totalCost,
                ExecutedAt = DateTime.UtcNow,
                Status = TradeStatus.Executed
            };

            portfolio.TradeHistory.Add(trade);

            return new TradeResult
            {
                Success = true,
                Trade = trade,
                ExecutedQuantity = order.Quantity,
                ExecutionPrice = executionPrice,
                TotalValue = totalCost
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing trade for portfolio {PortfolioId}", portfolioId);
            return new TradeResult
            {
                Success = false,
                ErrorMessage = $"Trade execution failed: {ex.Message}",
                ExecutedQuantity = 0
            };
        }
    }

    public async Task<List<Trade>> GetTradeHistoryAsync(string portfolioId, int days = 30)
    {
        var portfolio = GetOrCreatePortfolio(portfolioId);
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return portfolio.TradeHistory
            .Where(t => t.ExecutedAt >= cutoffDate)
            .OrderByDescending(t => t.ExecutedAt)
            .ToList();
    }

    public async Task<StockQuote> GetStockQuoteAsync(string symbol)
    {
        try
        {
            // Yahoo Finance API call
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}";
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<JsonElement>(response);

            var result = data.GetProperty("chart").GetProperty("result")[0];
            var meta = result.GetProperty("meta");
            var quote = result.GetProperty("indicators").GetProperty("quote")[0];

            var currentPrice = meta.GetProperty("regularMarketPrice").GetDecimal();
            var previousClose = meta.GetProperty("previousClose").GetDecimal();
            var volume = meta.GetProperty("regularMarketVolume").GetInt64();

            var change = currentPrice - previousClose;
            var changePercent = (change / previousClose) * 100;

            return new StockQuote
            {
                Symbol = symbol.ToUpper(),
                Price = currentPrice,
                Change = change,
                ChangePercent = changePercent,
                Volume = volume,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quote for {Symbol}", symbol);
            throw new InvalidOperationException($"Unable to get quote for {symbol}: {ex.Message}");
        }
    }

    public async Task<List<StockQuote>> GetMultipleQuotesAsync(string[] symbols)
    {
        var quotes = new List<StockQuote>();
        var tasks = symbols.Select(GetStockQuoteAsync);

        try
        {
            var results = await Task.WhenAll(tasks);
            quotes.AddRange(results);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Some quotes failed to load");
            // Return partial results
            foreach (var task in tasks)
            {
                try
                {
                    if (task.IsCompletedSuccessfully)
                        quotes.Add(await task);
                }
                catch { /* Skip failed quotes */ }
            }
        }

        return quotes;
    }

    public async Task<List<MarketNews>> GetMarketNewsAsync(int limit = 10)
    {
        try
        {
            // Yahoo Finance news endpoint
            var url = "https://query1.finance.yahoo.com/v1/finance/trending/US";
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<JsonElement>(response);

            var news = new List<MarketNews>();
            var quotes = data.GetProperty("finance").GetProperty("result")[0].GetProperty("quotes");

            foreach (var quote in quotes.EnumerateArray().Take(limit))
            {
                if (quote.TryGetProperty("symbol", out var symbolElement))
                {
                    news.Add(new MarketNews
                    {
                        Title = $"Trending: {symbolElement.GetString()}",
                        Summary = $"{symbolElement.GetString()} is currently trending in the market",
                        Source = "Yahoo Finance",
                        PublishedAt = DateTime.UtcNow,
                        Url = $"https://finance.yahoo.com/quote/{symbolElement.GetString()}"
                    });
                }
            }

            return news;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get market news");
            return new List<MarketNews>();
        }
    }

    public async Task<List<string>> GetTrendingStocksAsync()
    {
        try
        {
            var url = "https://query1.finance.yahoo.com/v1/finance/trending/US";
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<JsonElement>(response);

            var trendingStocks = new List<string>();
            var quotes = data.GetProperty("finance").GetProperty("result")[0].GetProperty("quotes");

            foreach (var quote in quotes.EnumerateArray())
            {
                if (quote.TryGetProperty("symbol", out var symbol))
                {
                    trendingStocks.Add(symbol.GetString()!);
                }
            }

            return trendingStocks.Take(20).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get trending stocks");
            return new List<string>();
        }
    }

    public async Task<List<string>> SearchStocksAsync(string query)
    {
        try
        {
            var url = $"https://query1.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<JsonElement>(response);

            var results = new List<string>();
            var quotes = data.GetProperty("quotes");

            foreach (var quote in quotes.EnumerateArray().Take(10))
            {
                if (quote.TryGetProperty("symbol", out var symbol))
                {
                    results.Add(symbol.GetString()!);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search stocks for query: {Query}", query);
            return new List<string>();
        }
    }

    public async Task<PerformanceMetrics> CalculatePerformanceAsync(string portfolioId, int days = 30)
    {
        var portfolio = GetOrCreatePortfolio(portfolioId);
        var trades = await GetTradeHistoryAsync(portfolioId, days);
        var summary = await GetPortfolioSummaryAsync(portfolioId);

        var totalTradingVolume = trades.Sum(t => t.TotalValue);
        var totalTrades = trades.Count;

        return new PerformanceMetrics
        {
            PortfolioId = portfolioId,
            Period = days,
            TotalReturn = summary.TotalReturn,
            TotalReturnPercent = summary.TotalReturnPercent,
            TotalTrades = totalTrades,
            TotalVolume = totalTradingVolume,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task InitializePortfolioAsync(string portfolioId, decimal initialCash = 10_000)
    {
        _portfolios[portfolioId] = new SimulatedPortfolio
        {
            PortfolioId = portfolioId,
            InitialCash = initialCash,
            CashBalance = initialCash,
            Positions = new Dictionary<string, Position>(),
            TradeHistory = new List<Trade>(),
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _logger.LogInformation("Initialized portfolio {PortfolioId} with ${InitialCash:N2}", portfolioId, initialCash);
    }

    private SimulatedPortfolio GetOrCreatePortfolio(string portfolioId)
    {
        if (!_portfolios.ContainsKey(portfolioId))
        {
            InitializePortfolioAsync(portfolioId).Wait();
        }
        return _portfolios[portfolioId];
    }
}

// Data Models
public class SimulatedPortfolio
{
    public string PortfolioId { get; set; } = string.Empty;
    public decimal InitialCash { get; set; }
    public decimal CashBalance { get; set; }
    public Dictionary<string, Position> Positions { get; set; } = new();
    public List<Trade> TradeHistory { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class Position
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class Trade
{
    public string TradeId { get; set; } = string.Empty;
    public string PortfolioId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime ExecutedAt { get; set; }
    public TradeStatus Status { get; set; }
}

public class TradeOrder
{
    public string Symbol { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public decimal Quantity { get; set; }
}

public class TradeResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Trade? Trade { get; set; }
    public decimal ExecutedQuantity { get; set; }
    public decimal ExecutionPrice { get; set; }
    public decimal TotalValue { get; set; }
}

public class StockQuote
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class MarketNews
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class PortfolioSummary
{
    public string PortfolioId { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal InitialValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int PositionCount { get; set; }
    public Dictionary<string, decimal> PositionValues { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class PerformanceMetrics
{
    public string PortfolioId { get; set; } = string.Empty;
    public int Period { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int TotalTrades { get; set; }
    public decimal TotalVolume { get; set; }
    public DateTime LastUpdated { get; set; }
}

public enum OrderType
{
    Buy,
    Sell
}

public enum TradeStatus
{
    Pending,
    Executed,
    Cancelled,
    Failed
}