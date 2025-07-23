// Services/ServiceLocator.cs
namespace CollectiveAI.Services;

/// <summary>
/// Simple service locator for accessing registered services from plugins
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize() first.");

        return _serviceProvider.GetRequiredService<T>();
    }

    public static T? GetService<T>()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize() first.");

        return _serviceProvider.GetService<T>();
    }
}