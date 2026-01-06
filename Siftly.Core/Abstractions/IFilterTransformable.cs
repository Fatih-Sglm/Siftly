namespace Siftly.Core;

/// <summary>
/// Interface for entities that support custom filter transformations.
/// Implement this in your entity if you need to transform filter field names or values.
/// </summary>
public interface IFilterTransformable
{
    /// <summary>
    /// Transform a filter condition.
    /// Return the original condition in a list if no transformation is needed - it will be used as-is.
    /// Return a list with transformed conditions if transformation is applied.
    /// Example: Transform DateTime string to Unix timestamp for a long field.
    /// </summary>
    /// <param name="condition">The filter condition to potentially transform</param>
    /// <returns>
    /// List containing [condition] - Use the original condition (no transformation)
    /// List with new items - Use these transformed conditions instead
    /// </returns>
    List<FilterCondition> GetTransformedFilters(FilterCondition condition);
}
