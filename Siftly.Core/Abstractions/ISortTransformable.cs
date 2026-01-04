namespace Siftly.Core;

/// <summary>
/// Interface for entities that support custom sort transformations
/// Implement this in your entity if you need to transform sort field names
/// </summary>
public interface ISortTransformable
{
    /// <summary>
    /// Transform a sort descriptor to one or more descriptors
    /// Example: Transform "createdOnValue" to "createdOn"
    /// </summary>
    List<SortDescriptor> GetTransformedSorts(SortDescriptor sortDescriptor);
}
