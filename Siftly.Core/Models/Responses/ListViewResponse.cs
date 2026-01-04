namespace Siftly.Core;

/// <summary>
/// Generic list response model with pagination support
/// </summary>
/// <typeparam name="T">Type of data items</typeparam>
public class ListViewResponse<T>
{
    public ListViewResponse()
    {
        ListData = [];
        TotalCount = 0;
    }

    public ListViewResponse(List<T>? data, long? total = null, int skip = 0, int take = 20)
    {
        ListData = data ?? [];
        TotalCount = total ?? data?.Count ?? 0;
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// The list of data items
    /// </summary>
    public List<T> ListData { get; set; }

    /// <summary>
    /// Total count of all items (before pagination)
    /// </summary>
    public long? TotalCount { get; set; }

    /// <summary>
    /// Number of items skipped
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber => Take > 0 ? (Skip / Take) + 1 : 1;

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (Take > 0 && TotalCount.HasValue) ? (int)Math.Ceiling((double)TotalCount.Value / Take) : 1;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => TotalCount.HasValue ? PageNumber < TotalPages : ListData.Count >= Take;
}
