namespace Siftly.Core;

/// <summary>
/// Extension methods for IServiceCollection to register QueryFilter services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configure the global QueryFilter options via DI.
    /// Note: This kütüphane now primarily uses a static configuration.
    /// </summary>
    public static IServiceCollection AddQueryFilter(this IServiceCollection services, Action<QueryFilterOptions>? configure = null)
    {
        if (configure != null)
        {
            QueryFilter.Configure(configure);
        }

        // Even though we use static configuration, we can still register 
        // the options in DI for services that might want to inject them.
        services.AddSingleton(QueryFilter.Options);

        return services;
    }
}
