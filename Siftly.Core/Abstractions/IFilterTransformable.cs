namespace Siftly.Core;

/// <summary>
/// Interface for entities that support custom filter transformations
/// Implement this in your entity if you need to transform filter field names or values
/// </summary>
public interface IFilterTransformable
{
    /// <summary>
    /// Transform a filter condition to one or more conditions
    /// Example: Transform "name" field to "__ML__Name" for multilanguage content
    /// </summary>
    List<FilterCondition> GetTransformedFilters(FilterCondition condition);
}
