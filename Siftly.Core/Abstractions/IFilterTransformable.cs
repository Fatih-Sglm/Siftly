namespace Siftly.Core;

/// <summary>
/// Interface for entities that support custom filter transformations.
/// Implement this in your entity if you need to transform filter field names or values.
/// </summary>
public interface IFilterTransformable
{
    /// <summary>
    /// Transform a filter condition.
    /// Return null if no transformation is needed - the original condition will be used as-is.
    /// Return a list with transformed conditions if transformation is applied.
    /// Example: Transform DateTime string to Unix timestamp for a long field.
    /// </summary>
    /// <param name="condition">The filter condition to potentially transform</param>
    /// <returns>
    /// null - Use the original condition (no transformation or transformation failed)
    /// List - Use these transformed conditions instead
    /// </returns>
    List<FilterCondition> GetTransformedFilters(FilterCondition condition);
}
