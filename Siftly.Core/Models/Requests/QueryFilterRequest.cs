namespace Siftly.Core;

/// <summary>
/// Main query filter request model that supports filtering, sorting, and pagination.
/// Now uses a unified FilterCondition model for both filtering and cursor-based pagination.
/// </summary>
public class QueryFilterRequest
{
    /// <summary>
    /// Number of items to take (page size)
    /// </summary>
    public int Take { get; set; } = 20;

    /// <summary>
    /// Number of items to skip (for offset-based pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

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
