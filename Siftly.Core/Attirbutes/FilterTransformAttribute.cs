namespace Siftly.Core;

/// <summary>
/// Attribute for declarative filter transformation configuration.
/// Apply to entity class to define filter transformations without implementing IFilterTransformable manually.
/// Uses reflection with caching for optimal performance.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class FilterTransformAttribute : Attribute
{
    public FilterTransformAttribute(string sourceField)
    {
        SourceField = sourceField;
    }

    public string SourceField { get; }
    
    /// <summary>
    /// If the target property is a collection (One-to-Many).
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    /// If the target property is part of a Many-to-Many relationship.
    /// </summary>
    public bool IsManyToMany { get; set; }

    /// <summary>
    /// If target is collection/m2m, specify the field inside the item (e.g. "Name").
    /// </summary>
    public string? ItemField { get; set; }

    /// <summary>
    /// For Many-to-Many, the navigation property on the join entity (e.g. "Category").
    /// </summary>
    public string? JoinProperty { get; set; }

    public string? TargetFields { get; set; }
    public Type? ValueTransformerType { get; protected set; }
}

/// <summary>
/// Generic version of FilterTransformAttribute for type-safe value transformation.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class FilterTransformAttribute<TTransformer> : FilterTransformAttribute 
    where TTransformer : IValueTransformer, new()
{
    public FilterTransformAttribute(string sourceField) : base(sourceField)
    {
        ValueTransformerType = typeof(TTransformer);
    }
}








