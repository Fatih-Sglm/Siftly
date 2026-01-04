namespace Siftly.Core;

/// <summary>
/// Static configuration for QueryFilter.
/// </summary>
public static class QueryFilter
{
    private static readonly QueryFilterOptions _options = new();

    /// <summary>
    /// Global configuration options
    /// </summary>
    public static QueryFilterOptions Options => _options;

    /// <summary>
    /// Configure the global options
    /// </summary>
    public static void Configure(Action<QueryFilterOptions> configure)
    {
        configure(_options);
    }
}

/// <summary>
/// Configuration options for QueryFilter. 
/// </summary>
public class QueryFilterOptions
{
    /// <summary>
    /// Maximum page size allowed to prevent over-fetching
    /// </summary>
    public int MaxPageSize { get; set; } = 1000;

    /// <summary>
    /// Registered type expression builders for filtering
    /// </summary>
    internal Dictionary<Type, ITypeExpressionBuilder> TypeBuilders { get; } = [];

    /// <summary>
    /// Registered sort expression builders for custom type sorting
    /// </summary>
    internal Dictionary<Type, object> SortBuilders { get; } = [];

    /// <summary>
    /// Register a custom type expression builder for filtering type T
    /// </summary>
    public QueryFilterOptions RegisterTypeBuilder<T>(ITypeExpressionBuilder<T> builder)
    {
        TypeBuilders[typeof(T)] = builder;
        return this;
    }

    /// <summary>
    /// Register a custom sort expression builder for sorting type T
    /// </summary>
    public QueryFilterOptions RegisterSortBuilder<T>(ISortExpressionBuilder<T> builder)
    {
        SortBuilders[typeof(T)] = builder;
        return this;
    }

    /// <summary>
    /// Get a type builder for filtering if registered
    /// </summary>
    public ITypeExpressionBuilder? GetTypeBuilder(Type type)
    {
        return TypeBuilders.TryGetValue(type, out var builder) ? builder : null;
    }

    /// <summary>
    /// Get a sort builder for a specific type if registered
    /// </summary>
    public ISortExpressionBuilder<T>? GetSortBuilder<T>()
    {
        return SortBuilders.TryGetValue(typeof(T), out var builder) ? builder as ISortExpressionBuilder<T> : null;
    }

    /// <summary>
    /// Get a sort builder for a specific type if registered (non-generic)
    /// </summary>
    public object? GetSortBuilder(Type type)
    {
        return SortBuilders.TryGetValue(type, out var builder) ? builder : null;
    }
}
