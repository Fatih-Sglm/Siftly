using System.Linq.Expressions;

namespace Siftly.Core;

/// <summary>
/// Interface for building custom sort expressions for specific types.
/// Implement this interface to provide custom sorting logic for complex types like MultiLanguageContent.
/// </summary>
/// <typeparam name="T">The type this builder handles</typeparam>
public interface ISortExpressionBuilder<T>
{
    /// <summary>
    /// Builds a sort key expression for the given property access.
    /// </summary>
    /// <param name="propertyAccess">The expression representing access to the property of type T</param>
    /// <param name="sortDescriptor">The sort descriptor containing field and direction info</param>
    /// <returns>An expression that can be used as a sort key, or null if this builder cannot handle the request</returns>
    Expression? BuildSortExpression(Expression propertyAccess, SortDescriptor sortDescriptor);
}
