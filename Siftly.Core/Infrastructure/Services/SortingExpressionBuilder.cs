namespace Siftly.Core;

public static class SortingExpressionBuilder
{
    public static IQueryable<T> ApplySorting<T>(IQueryable<T> query, List<SortDescriptor> sortDescriptors) where T : class, new()
    {
        if (sortDescriptors == null || sortDescriptors.Count == 0)
            return query;

        var sortTransformable = typeof(ISortTransformable).IsAssignableFrom(typeof(T))
            ? Activator.CreateInstance<T>() as ISortTransformable
            : null;

        var allSorts = new List<SortDescriptor>();
        foreach (var sort in sortDescriptors)
        {
            var transformed = sortTransformable?.GetTransformedSorts(sort);
            allSorts.AddRange(transformed ?? [sort]);
        }

        IOrderedQueryable<T>? orderedQuery = null;

        for (int i = 0; i < allSorts.Count; i++)
        {
            var sort = allSorts[i];
            var isDescending = sort.Dir == ListSortDirection.Descending;
            orderedQuery = ApplyOrderBy(orderedQuery ?? query, sort.Field, isDescending, i == 0);
        }

        return orderedQuery ?? query;
    }

    private static IOrderedQueryable<T> ApplyOrderBy<T>(IQueryable<T> query, string propertyName, bool descending, bool isFirst) where T : class
    {
        var options = QueryFilter.Options;
        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = parameter.GetPropertyAccess(propertyName, out var property);

        if (propertyAccess == null || property == null)
            return query as IOrderedQueryable<T> ?? (IOrderedQueryable<T>)query.OrderBy(x => 0);

        // Check if there's a custom sort builder for this property type
        var sortBuilder = options.GetSortBuilder(property.PropertyType);
        Expression sortKeyExpression;
        Type sortKeyType;

        if (sortBuilder != null)
        {
            // Use reflection to call BuildSortExpression on the builder
            var buildMethod = sortBuilder.GetType().GetMethod("BuildSortExpression");
            var sortDescriptor = new SortDescriptor { Field = propertyName, Dir = descending ? ListSortDirection.Descending : ListSortDirection.Ascending };
            var customExpression = buildMethod?.Invoke(sortBuilder, [propertyAccess, sortDescriptor]) as Expression;

            if (customExpression != null)
            {
                sortKeyExpression = customExpression;
                sortKeyType = customExpression.Type;
            }
            else
            {
                // Fallback to default behavior
                sortKeyExpression = propertyAccess;
                sortKeyType = property.PropertyType;
            }
        }
        else
        {
            sortKeyExpression = propertyAccess;
            sortKeyType = property.PropertyType;
        }

        var lambda = Expression.Lambda(sortKeyExpression, parameter);

        string methodName = isFirst
            ? (descending ? FilterConstant.OrderByDescending : FilterConstant.OrderBy)
            : (descending ? FilterConstant.ThenByDescending : FilterConstant.ThenBy);

        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), sortKeyType);

        return (IOrderedQueryable<T>)method.Invoke(null, [query, lambda])!;
    }
}
