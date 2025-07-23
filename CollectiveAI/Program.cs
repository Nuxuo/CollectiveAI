using CollectiveAI.Services;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CollectiveAI Finance Trading API",
        Version = "v1",
        Description = "AI Finance Team with Real-Time Stock Trading"
    });
});

// Configure HttpClient for Yahoo Finance
builder.Services.AddHttpClient("YahooFinance", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "SE-KE-AGENTS");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Register Semantic Kernel
builder.Services.AddSingleton<Kernel>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddOpenAIChatCompletion(
        configuration["OpenAI:ModelId"] ?? "gpt-4",
        configuration["OpenAI:ApiKey"]!
    );

    var kernel = kernelBuilder.Build();
    return kernel;
});

// Register the Stock Trading Service as Singleton
builder.Services.AddSingleton<IStockSimulationService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("YahooFinance");
    var logger = sp.GetRequiredService<ILogger<StockSimulationService>>();

    return new StockSimulationService(httpClient, logger);
});

// Register the Agent Service as Singleton
builder.Services.AddSingleton<IAgentService, AgentService>();

builder.Services.AddMemoryCache();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddFilter("CollectiveAI", LogLevel.Information);
    logging.AddFilter("Microsoft.SemanticKernel", LogLevel.Warning);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize ServiceLocator
ServiceLocator.Initialize(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CollectiveAI Finance Trading API v1");
    });
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
    app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

// Initialize agents on startup
var agentService = app.Services.GetRequiredService<IAgentService>();
var agents = agentService.GetAgents();
Console.WriteLine($"Initialized {agents.Length} finance agents: {string.Join(", ", agents.Select(a => a.Name))}");

app.Run();