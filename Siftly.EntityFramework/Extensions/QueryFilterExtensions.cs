namespace Siftly.EntityFramework;

/// <summary>
/// Extension methods for applying QueryFilterRequest and dynamic LINQ operations to IQueryable.
/// </summary>
public static class QueryFilterExtensions
{
    #region Generic Extensions (Typed)

    /// <summary>
    /// Apply filters, sorting and pagination to the query.
    /// </summary>
    public static IQueryable<T> ApplyQueryFilter<T>(
        this IQueryable<T> query,
        QueryFilterRequest request) where T : class, new()
    {
        if (request == null) return query;

        return query
            .ApplyFilters(request)
            .ApplySortingAndPagination(request);
    }

    /// <summary>
    /// Apply filters and keyset cursor (if any) to the query.
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> query,
        QueryFilterRequest request) where T : class, new()
    {
        if (request == null) return query;
        var options = QueryFilter.Options;

        // 1. Logic Filters
        if (request.Filter != null)
        {
            query = FilterExpressionBuilder.ApplyFilters(query, request.Filter, options);
        }

        // 2. Keyset Pagination (Cursor)
        if (request.Cursor != null && !string.IsNullOrEmpty(request.Cursor.Field))
        {
            var parameter = Expression.Parameter(typeof(T), "c");
            var cursorExpression = FilterExpressionBuilder.BuildConditionExpression<T>(request.Cursor, parameter, options);

            if (cursorExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(cursorExpression, parameter);
                query = query.Where(lambda);
            }
        }

        return query;
    }

    /// <summary>
    /// Apply sorting and limit/offset pagination to the query.
    /// </summary>
    public static IQueryable<T> ApplySortingAndPagination<T>(
        this IQueryable<T> query,
        QueryFilterRequest request) where T : class, new()
    {
        if (request == null) return query;
        var options = QueryFilter.Options;

        // 3. Sorting
        if (request.Sort != null && request.Sort.Count > 0)
        {
            query = SortingExpressionBuilder.ApplySorting(query, request.Sort);
        }
        else
        {
            // Fallback sort for Split Query safety
            query = query.OrderBy(x => 0);
        }

        // 4. Pagination (Skip/Take)
        if (request.Cursor == null)
        {
            query = query.Skip(request.PageNumber);
        }

        var take = Math.Min(request.PageSize, options.MaxPageSize);
        return query.Take(take);
    }

    #endregion

    #region Non-Generic Extensions (Dynamic)

    /// <summary>
    /// Calls a Queryable method (Select, OrderBy, etc.) using reflection and expression trees.
    /// </summary>
    private static IQueryable CallQueryableMethod(this IQueryable source, string methodName, LambdaExpression selector)
    {
        return source.Provider.CreateQuery(Expression.Call(
            typeof(Queryable), 
            methodName, 
            [source.ElementType, selector.Body.Type], 
            source.Expression, 
            Expression.Quote(selector)));
    }

    public static IQueryable Page(this IQueryable source, int pageIndex, int pageSize)
    {
        IQueryable query = source;
        if (pageIndex > 0)
        {
            query = query.CallQueryableMethodWithInt("Skip", pageIndex * pageSize);
        }
        
        if (pageSize > 0)
        {
            query = query.CallQueryableMethodWithInt("Take", pageSize);
        }

        return query;
    }

    public static IQueryable Select(this IQueryable source, LambdaExpression selector)
    {
        return source.CallQueryableMethod("Select", selector);
    }

    public static IQueryable GroupBy(this IQueryable source, LambdaExpression keySelector)
    {
        return source.CallQueryableMethod("GroupBy", keySelector);
    }

    public static IQueryable OrderBy(this IQueryable source, LambdaExpression keySelector)
    {
        return source.CallQueryableMethod("OrderBy", keySelector);
    }

    public static IQueryable OrderByDescending(this IQueryable source, LambdaExpression keySelector)
    {
        return source.CallQueryableMethod("OrderByDescending", keySelector);
    }

    public static IQueryable OrderBy(this IQueryable source, LambdaExpression keySelector, ListSortDirection? sortDirection)
    {
        if (sortDirection.HasValue)
        {
            return sortDirection.Value == ListSortDirection.Ascending
                ? source.OrderBy(keySelector)
                : source.OrderByDescending(keySelector);
        }
        return source;
    }

    private static IQueryable CallQueryableMethodWithInt(this IQueryable source, string methodName, int value)
    {
        return source.Provider.CreateQuery(Expression.Call(
            typeof(Queryable),
            methodName,
            [source.ElementType],
            source.Expression,
            Expression.Constant(value)));
    }

    #endregion
}
