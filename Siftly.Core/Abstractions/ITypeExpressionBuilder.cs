namespace Siftly.Core;

/// <summary>
/// Base interface for all type expression builders.
/// Allows storing builders in a non-generic way while keeping method signatures consistent.
/// </summary>
public interface ITypeExpressionBuilder
{
    /// <summary>
    /// Build filter expression for a property
    /// </summary>
    /// <param name="propertyAccess">The expression accessing the property</param>
    /// <param name="condition">The filter condition being processed</param>
    /// <param name="options">Global configuration options</param>
    Expression? BuildExpression(
        Expression propertyAccess, 
        FilterCondition condition);
}

/// <summary>
/// Generic marker interface for type-specific expression builders.
/// </summary>
public interface ITypeExpressionBuilder<T> : ITypeExpressionBuilder
{
}
