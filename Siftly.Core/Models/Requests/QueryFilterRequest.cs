namespace Siftly.Core;

/// <summary>
/// Main query filter request model that supports filtering, sorting, and pagination.
/// Page is 1-based (first page = 1). Use Offset for the actual skip value.
/// </summary>
public class QueryFilterRequest
{
    private int _page = 1;
    private int _pageSize;

    /// <summary>
    /// Page number (1-based). First page is 1. Values less than 1 are treated as 1.
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page. Values less than 1 use default from QueryFilter.Options.DefaultPageSize.
    /// </summary>
    public int PageSize
    {
        get => _pageSize < 1 ? QueryFilter.Options.DefaultPageSize : _pageSize;
        set => _pageSize = value < 1 ? QueryFilter.Options.DefaultPageSize : value;
    }

    /// <summary>
    /// Computed offset for database query: (Page - 1) * PageSize. Always >= 0.
    /// </summary>
    public int Offset => (Page - 1) * PageSize;

    /// <summary>
    /// Sorting configuration
    /// </summary>
    public List<SortDescriptor>? Sort { get; set; }

    /// <summary>
    /// Filter configuration (supports both simple and composite filters via FilterCondition)
    /// </summary>
    public FilterCondition? Filter { get; set; }

    /// <summary>
    /// Keyset pagination (Cursor) - Uses the same model as filters for consistency
    /// </summary>
    public FilterCondition? Cursor { get; set; }

    /// <summary>
    /// Whether to calculate the total count of items
    /// </summary>
    public bool IncludeCount { get; set; } = true;
}
