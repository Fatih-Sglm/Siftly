namespace Siftly.Core;

/// <summary>
/// Individual filter condition or a nested composite filter group.
/// Can be inherited to add custom properties (e.g., MultiLangFilter).
/// </summary>
public class FilterCondition : FilterDescriptorBase
{
    /// <summary>
    /// Field name to filter (Member in Kendo terminology)
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Filter operator
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.IsEqualTo;

    /// <summary>
    /// Value to filter by. Supports dynamic types via JsonObjectModelBinder
    /// </summary>
    [ModelBinder(BinderType = typeof(JsonObjectModelBinder))]
    public object? Value { get; set; }

    /// <summary>
    /// Logic operator for nested filters: "and" or "or"
    /// </summary>
    public string? Logic { get; set; }

    /// <summary>
    /// Nested filters for complex filtering scenarios
    /// </summary>
    public List<FilterCondition>? Filters { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public FilterCondition() { }

    /// <summary>
    /// Create a filter condition with field, operator, and value
    /// </summary>
    public FilterCondition(string field, FilterOperator filterOperator, object? value)
    {
        Field = field;
        Operator = filterOperator;
        Value = value;
    }
}
