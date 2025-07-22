using CollectiveAI.Services;

namespace CollectiveAI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCollectiveAI(this IServiceCollection services)
    {
        // Register AgentStoreService as singleton - loads agents once at startup
        services.AddSingleton<IAgentStoreService, AgentStoreService>();

        // Register AITeamService as scoped - one per request/scope
        services.AddScoped<IAgentTeamService, AgentTeamService>();

        return services;
    }

    public static async Task<IServiceProvider> InitializeAgentsAsync(this IServiceProvider serviceProvider)
    {
        // Force initialization of the singleton AgentStoreService
        // This ensures agents are loaded during startup, not on first request
        var agentStore = serviceProvider.GetRequiredService<IAgentStoreService>();

        var logger = serviceProvider.GetService<ILogger<IServiceProvider>>();
        logger?.LogInformation("Agent store initialized with {TeamCount} teams", agentStore.GetTeamNames().Count());

        return serviceProvider;
    }
}