using CollectiveAI.Extensions;
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
        Title = "CollectiveAI API",
        Version = "v1",
        Description = "AI Team Discussion Service with Function-Enabled AgentFactory"
    });
});

builder.Services.AddHttpClient("CollectiveAI", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "CollectiveAI/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<Kernel>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddOpenAIChatCompletion(
        configuration["OpenAI:ModelId"]!,
        configuration["OpenAI:ApiKey"]!
    );

    var kernel = kernelBuilder.Build();

    return kernel;
});

builder.Services.AddScoped<IAgentTeamService, AgentTeamService>();
builder.Services.AddCollectiveAI();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "CollectiveAI API v1"); });
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment()) app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();