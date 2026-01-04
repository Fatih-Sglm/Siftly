namespace Siftly.EntityFramework;

/// <summary>
/// Extension methods for applying QueryFilterRequest to IQueryable.
/// </summary>
public static class QueryFilterExtensions
{
    /// <summary>
    /// Apply complete query filter including filters, sorting, and pagination.
    /// </summary>
    public static async Task<ListViewResponse<T>> ApplyQueryFilterAsync<T>(
        this IQueryable<T> query,
        QueryFilterRequest request,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        var (filteredQuery, total) = await ApplyFilterAndCountInternalAsync(query, request, cancellationToken);
        var data = await filteredQuery.ToListAsync(cancellationToken);
        return new ListViewResponse<T>(data, total ?? data.Count, request.Skip, request.Take);
    }

    /// <summary>
    /// Apply complete query filter with Select expression (executed in database).
    /// </summary>
    public static async Task<ListViewResponse<TResult>> ApplyQueryFilterAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        QueryFilterRequest request,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken = default) where TSource : class, new()
    {
        var (filteredQuery, total) = await ApplyFilterAndCountInternalAsync(query, request, cancellationToken);
        var data = await filteredQuery.Select(selector).ToListAsync(cancellationToken);
        return new ListViewResponse<TResult>(data, total ?? data.Count, request.Skip, request.Take);
    }

    private static async Task<(IQueryable<T> Query, int? Total)> ApplyFilterAndCountInternalAsync<T>(
        IQueryable<T> query,
        QueryFilterRequest request,
        CancellationToken cancellationToken) where T : class, new()
    {
        var options = QueryFilter.Options;

        // 1. Apply Filters
        if (request.Filter != null)
        {
            query = FilterExpressionBuilder.ApplyFilters(query, request.Filter, options);
        }

        // 2. Keyset Pagination (Cursor)
        if (request.Cursor != null && !string.IsNullOrEmpty(request.Cursor.Field))
        {
            var parameter = Expression.Parameter(typeof(T), "c");
            var cursorExpression = FilterExpressionBuilder.BuildConditionExpression<T>(request.Cursor, parameter, options);

            if (cursorExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(cursorExpression, parameter);
                query = query.Where(lambda);
            }
        }

        // 3. Count (Optional)
        int? total = null;
        if (request.IncludeCount)
        {
            total = await query.CountAsync(cancellationToken);
        }

        // 4. Sorting
        if (request.Sort != null && request.Sort.Count > 0)
        {
            query = SortingExpressionBuilder.ApplySorting(query, request.Sort);
        }

        // 5. Pagination
        if (request.Cursor == null)
        {
            query = query.Skip(request.Skip);
        }

        var take = Math.Min(request.Take, options.MaxPageSize);
        query = query.Take(take);

        return (query, total);
    }
}
