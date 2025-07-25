using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Redis.StackExchange;
using StackExchange.Redis;

namespace CollectiveAI.Configurations;

public static class HangfireConfiguration
{
    /// <summary>
    /// Configuration for Hangfire with Redis backend for the CollectiveAI finance system
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Get Redis connection details from configuration
        var redisEndpoint = configuration["Redis:Endpoint"];
        var redisPassword = configuration["Redis:Password"];

        // Create Redis connection for production/staging
        var redis = ConnectionMultiplexer.Connect($"{redisEndpoint},ssl=true,password={redisPassword},abortConnect=false");

        // Configure Hangfire with Redis
        services.AddHangfire(config =>
        {
            // Create Redis storage for distributed processing
            var redisStorage = new RedisStorage(redis, new RedisStorageOptions
            {
                Prefix = "CollecticeAI:",
                Db = 0
            });

            config.UseStorage(redisStorage);
            config.UseDashboardMetrics();
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
            options.Queues = new[] { "default", "heavy", "critical" };
            options.ServerName = $"CollecticeAI-{Environment.MachineName}-{Guid.NewGuid()}";
        });

        return services;
    }

    /// <summary>
    /// Add Hangfire dashboard with authentication for monitoring background jobs
    /// </summary>
    public static IApplicationBuilder UseHangfireDashboardWithAuth(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        var dashboardOptions = new DashboardOptions
        {
            AppPath = "/", // Return to main app from dashboard
            DashboardTitle = "CollectiveAI Finance Jobs",
            StatsPollingInterval = 5000, // 5 seconds for real-time monitoring
        };

        if (env.IsDevelopment())
        {
            // Allow all access in development
            dashboardOptions.Authorization = new[] { new AllowAllDashboardAuthorizationFilter() };
        }
        else
        {
            // In production, you might want to add proper authentication
            // dashboardOptions.Authorization = new[] { new MyAuthorizationFilter() };
        }

        app.UseHangfireDashboard("/hangfire", dashboardOptions);
        return app;
    }
}

/// <summary>
/// Allow all dashboard access for development environment
/// </summary>
public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true; // Allow all in development
    }
}