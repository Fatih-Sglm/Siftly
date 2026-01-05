namespace Siftly.EntityFramework;

/// <summary>
/// Extension methods for materializing IQueryable into ListViewResponse.
/// </summary>
public static class ToListViewResponseExtensions
{
    /// <summary>
    /// Materializes the query into a ListViewResponse with optional total count.
    /// Filters, sorting and pagination are applied via QueryFilterRequest.
    /// </summary>
    public static async Task<ListViewResponse<T>> ToListViewResponseAsync<T>(
        this IQueryable<T> query,
        QueryFilterRequest request,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        if (request == null)
        {
            var data = await query.ToListAsync(cancellationToken);
            return new ListViewResponse<T>(data, data.Count, 0, data.Count);
        }

        // 1. Apply Filters & Keyset Cursor (Without sorting and pagination)
        var filteredQuery = query.ApplyFilters(request);

        // 2. Count (Optional) - Performed on filtered query before pagination
        long? totalCount = null;
        if (request.IncludeCount)
        {
            totalCount = await filteredQuery.LongCountAsync(cancellationToken);
        }

        // 3. Apply Sorting and Pagination
        var finalQuery = filteredQuery.ApplySortingAndPagination(request);

        // 4. Materialize
        var listData = await finalQuery.ToListAsync(cancellationToken);

        return new ListViewResponse<T>(listData, totalCount ?? listData.Count, request.Offset, request.PageSize);
    }

    /// <summary>
    /// Materializes the projected query into a ListViewResponse with optional total count.
    /// </summary>
    public static async Task<ListViewResponse<TResult>> ToListViewResponseAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        QueryFilterRequest request,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken = default) where TSource : class, new()
    {
        if (request == null)
        {
            var data = await query.Select(selector).Cast<TResult>().ToListAsync(cancellationToken);
            return new ListViewResponse<TResult>(data, data.Count, 0, data.Count);
        }

        // 1. Apply Filters & Keyset Cursor
        var filteredQuery = query.ApplyFilters(request);

        // 2. Count (Optional)
        long? totalCount = null;
        if (request.IncludeCount)
        {
            totalCount = await filteredQuery.LongCountAsync<TSource>(cancellationToken);
        }

        // 3. Apply Sorting and Pagination
        var sortedAndPagedQuery = filteredQuery.ApplySortingAndPagination(request);

        // 4. Project and Materialize
        var listData = await sortedAndPagedQuery.Select(selector).Cast<TResult>().ToListAsync(cancellationToken);

        return new ListViewResponse<TResult>(listData, totalCount ?? listData.Count, request.Offset, request.PageSize);
    }
}
