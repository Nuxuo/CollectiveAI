using CollectiveAI.Services;

namespace CollectiveAI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCollectiveAI(this IServiceCollection services)
    {
        services.AddSingleton<IAgentStoreService, AgentStoreService>();
        services.AddScoped<IAgentTeamService, AgentTeamService>();
        services.AddScoped<IAgentTeamService, AgentTeamService>();
        return services;
    }

    public static async Task<IServiceProvider> InitializeAgentsAsync(this IServiceProvider serviceProvider)
    {
        var agentStore = serviceProvider.GetRequiredService<IAgentStoreService>();

        var logger = serviceProvider.GetService<ILogger<IServiceProvider>>();
        logger?.LogInformation("Initialized {TeamCount} agent teams with real trading capabilities",
            agentStore.GetTeamNames().Count());

        return serviceProvider;
    }
}