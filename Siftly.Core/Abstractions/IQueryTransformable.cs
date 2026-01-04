namespace Siftly.Core;
/// <summary>
/// Combined interface for entities that support both filter and sort transformations
/// </summary>
public interface IQueryTransformable : IFilterTransformable, ISortTransformable;
