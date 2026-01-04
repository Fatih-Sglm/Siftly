namespace Siftly.Core;

/// <summary>
/// Sort descriptor for defining field and direction
/// </summary>
public class SortDescriptor
{
    /// <summary>
    /// Field name to sort by
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Sort direction
    /// </summary>
    public ListSortDirection Dir { get; set; } = ListSortDirection.Ascending;

    /// <summary>
    /// Culture/Language code for multi-language field sorting (e.g., "tr", "en")
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public SortDescriptor() { }

    /// <summary>
    /// Create a sort descriptor with field and direction
    /// </summary>
    public SortDescriptor(string field, ListSortDirection direction)
    {
        Field = field;
        Dir = direction;
    }
}
