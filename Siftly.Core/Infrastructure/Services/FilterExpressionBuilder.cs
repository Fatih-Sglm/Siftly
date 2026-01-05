namespace Siftly.Core;

public static class FilterExpressionBuilder
{
    public static IQueryable<T> ApplyFilters<T>(IQueryable<T> query, FilterCondition filter, QueryFilterOptions options) where T : class, new()
    {
        if (filter == null) return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var expression = BuildFilterExpression<T>(filter, parameter, options);

        if (expression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    public static Expression? BuildFilterExpression<T>(FilterCondition filter, ParameterExpression parameter, QueryFilterOptions options) where T : class, new()
    {
        if (filter.Filters == null || filter.Filters.Count == 0)
        {
            return BuildConditionExpression<T>(filter, parameter, options);
        }

        Expression? combinedExpression = null;
        var isAndLogic = (filter.Logic ?? FilterConstant.And).Equals(FilterConstant.And, StringComparison.OrdinalIgnoreCase);

        var filterTransformable = typeof(IFilterTransformable).IsAssignableFrom(typeof(T))
            ? Activator.CreateInstance<T>() as IFilterTransformable
            : null;

        foreach (var condition in filter.Filters)
        {
            Expression? conditionExpression = null;

            if (condition.Filters != null && condition.Filters.Count > 0)
            {
                conditionExpression = BuildFilterExpression<T>(condition, parameter, options);
            }
            else
            {
                // Get transformed conditions from IFilterTransformable
                // Entity returns [condition] unchanged if no transformation needed
                var transformedConditions = filterTransformable!.GetTransformedFilters(condition);

                if (transformedConditions.Count == 1)
                {
                    // Single condition (most common case)
                    conditionExpression = BuildConditionExpression<T>(transformedConditions[0], parameter, options);
                }
                else if (transformedConditions.Count > 1)
                {
                    // Multiple conditions - combine with OR (e.g., MapToMany)
                    Expression? orExpression = null;
                    foreach (var transformedCondition in transformedConditions)
                    {
                        var expr = BuildConditionExpression<T>(transformedCondition, parameter, options);
                        if (expr != null)
                        {
                            orExpression = orExpression == null
                                ? expr
                                : Expression.OrElse(orExpression, expr);
                        }
                    }
                    conditionExpression = orExpression;
                }
            }

            if (conditionExpression == null)
                continue;

            combinedExpression = combinedExpression == null
                ? conditionExpression
                : isAndLogic
                    ? Expression.AndAlso(combinedExpression, conditionExpression)
                    : Expression.OrElse(combinedExpression, conditionExpression);
        }

        return combinedExpression;
    }

    public static Expression? BuildConditionExpression<T>(FilterCondition condition, ParameterExpression parameter, QueryFilterOptions options) where T : class, new()
    {
        try
        {
            if (string.IsNullOrEmpty(condition.Field))
                return null;

            if (condition.Field.StartsWith(FilterConstant.Prefixes.Collection))
            {
                return BuildCollectionExpression<T>(condition, parameter, options);
            }

            if (condition.Field.StartsWith(FilterConstant.Prefixes.ManyToMany))
            {
                return BuildManyToManyExpression<T>(condition, parameter, options);
            }

            var propertyAccess = parameter.GetPropertyAccess(condition.Field, out var property);
            if (propertyAccess == null || property == null) return null;

            var typeBuilder = options.GetTypeBuilder(property.PropertyType);
            if (typeBuilder != null)
            {
                var customExpression = typeBuilder.BuildExpression(propertyAccess, condition);
                if (customExpression != null)
                    return customExpression;
            }

            return BuildStandardExpression(parameter, property, condition.Operator, condition.Value, condition.CaseSensitiveFilter, propertyAccess);
        }
        catch
        {
            return null;
        }
    }

    private static MethodCallExpression? BuildCollectionExpression<T>(FilterCondition condition, ParameterExpression parameter, QueryFilterOptions options)
    {
        ReadOnlySpan<char> fieldSpan = condition.Field.AsSpan();
        int firstColon = fieldSpan.IndexOf(':');
        int lastColon = fieldSpan.LastIndexOf(':');

        if (firstColon == -1 || lastColon == -1 || firstColon == lastColon) return null;

        var collectionPath = fieldSpan.Slice(firstColon + 1, lastColon - firstColon - 1).ToString();
        var itemField = fieldSpan[(lastColon + 1)..].ToString();

        var collectionAccess = parameter.GetPropertyAccess(collectionPath, out var collectionProperty);
        if (collectionAccess == null || collectionProperty == null) return null;

        var currentType = collectionProperty.PropertyType;
        var itemType = currentType.IsGenericType ? currentType.GetGenericArguments()[0] : currentType.GetElementType();
        if (itemType == null) return null;

        var itemProperty = itemType.GetProperty(itemField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (itemProperty == null) return null;

        var itemParameter = Expression.Parameter(itemType, "t");
        var itemPropertyAccess = Expression.Property(itemParameter, itemProperty);

        Expression? predicateBody = null;
        var typeBuilder = options.GetTypeBuilder(itemProperty.PropertyType);
        if (typeBuilder != null)
        {
            predicateBody = typeBuilder.BuildExpression(itemPropertyAccess, condition);
        }

        predicateBody ??= BuildStandardExpression(itemParameter, itemProperty, condition.Operator, condition.Value, condition.CaseSensitiveFilter, itemPropertyAccess);
        if (predicateBody == null) return null;

        var predicateLambda = Expression.Lambda(predicateBody, itemParameter);
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == FilterConstant.Any && m.GetParameters().Length == 2)
            .MakeGenericMethod(itemType);

        return Expression.Call(anyMethod, collectionAccess, predicateLambda);
    }

    private static MethodCallExpression? BuildManyToManyExpression<T>(FilterCondition condition, ParameterExpression parameter, QueryFilterOptions options)
    {
        ReadOnlySpan<char> fieldSpan = condition.Field.AsSpan();
        int firstColon = fieldSpan.IndexOf(':');
        if (firstColon == -1) return null;

        var remaining = fieldSpan[(firstColon + 1)..];
        int secondColon = remaining.IndexOf(':');
        if (secondColon == -1) return null;

        var joinCollectionPath = remaining[..secondColon].ToString();
        remaining = remaining[(secondColon + 1)..];
        int thirdColon = remaining.IndexOf(':');
        if (thirdColon == -1) return null;

        var navigationProperty = remaining[..thirdColon].ToString();
        var itemField = remaining[(thirdColon + 1)..].ToString();

        var collectionAccess = parameter.GetPropertyAccess(joinCollectionPath, out var collectionProperty);
        if (collectionAccess == null || collectionProperty == null) return null;

        var currentType = collectionProperty.PropertyType;
        var joinType = currentType.IsGenericType ? currentType.GetGenericArguments()[0] : currentType.GetElementType();
        if (joinType == null) return null;

        var navProperty = joinType.GetProperty(navigationProperty, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (navProperty == null) return null;

        var relatedType = navProperty.PropertyType;
        var targetProperty = relatedType.GetProperty(itemField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (targetProperty == null) return null;

        var joinParameter = Expression.Parameter(joinType, "pc");
        var navAccess = Expression.Property(joinParameter, navProperty);
        var targetAccess = Expression.Property(navAccess, targetProperty);

        Expression? predicateBody = null;
        var typeBuilder = options.GetTypeBuilder(targetProperty.PropertyType);
        if (typeBuilder != null)
        {
            predicateBody = typeBuilder.BuildExpression(targetAccess, condition);
        }

        predicateBody ??= BuildStandardExpression(joinParameter, targetProperty, condition.Operator, condition.Value, condition.CaseSensitiveFilter, targetAccess);
        if (predicateBody == null) return null;

        var predicateLambda = Expression.Lambda(predicateBody, joinParameter);
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == FilterConstant.Any && m.GetParameters().Length == 2)
            .MakeGenericMethod(joinType);

        return Expression.Call(anyMethod, collectionAccess, predicateLambda);
    }

    private static readonly MethodInfo StringToLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
    private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo StringStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo StringEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
    private static readonly MethodInfo StringIsNullOrEmpty = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;

    private static Expression? BuildStandardExpression(ParameterExpression parameter, PropertyInfo property, FilterOperator operatorType, object? value, bool caseSensitive = false, Expression? propertyAccess = null)
    {
        propertyAccess ??= Expression.Property(parameter, property);
        var isString = property.PropertyType == typeof(string);
        var isNullable = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null;

        switch (operatorType)
        {
            case FilterOperator.IsNull:
                return isNullable ? Expression.Equal(propertyAccess, Expression.Constant(null, property.PropertyType)) : Expression.Constant(false);
            case FilterOperator.IsNotNull:
                return isNullable ? Expression.NotEqual(propertyAccess, Expression.Constant(null, property.PropertyType)) : Expression.Constant(true);
            case FilterOperator.IsEmpty:
                return isString ? Expression.Equal(propertyAccess, Expression.Constant(string.Empty)) : null;
            case FilterOperator.IsNotEmpty:
                return isString ? Expression.NotEqual(propertyAccess, Expression.Constant(string.Empty)) : null;
            case FilterOperator.IsNullOrEmpty:
                return isString ? Expression.Call(StringIsNullOrEmpty, propertyAccess) : null;
            case FilterOperator.IsNotNullOrEmpty:
                return isString ? Expression.Not(Expression.Call(StringIsNullOrEmpty, propertyAccess)) : null;
        }

        object? convertedValue;
        try { convertedValue = ConvertValue(value, property.PropertyType); } catch { return null; }

        var valueExpression = Expression.Constant(convertedValue, property.PropertyType);
        Expression propExpr = propertyAccess;
        Expression valExpr = valueExpression;

        if (isString && !caseSensitive)
        {
            propExpr = Expression.Call(propertyAccess, StringToLower);
            if (convertedValue is string strVal)
                valExpr = Expression.Constant(strVal.ToLower());
        }

        return operatorType switch
        {
            FilterOperator.IsEqualTo => Expression.Equal(isString && !caseSensitive ? propExpr : propertyAccess, isString && !caseSensitive ? valExpr : valueExpression),
            FilterOperator.IsNotEqualTo => Expression.NotEqual(isString && !caseSensitive ? propExpr : propertyAccess, isString && !caseSensitive ? valExpr : valueExpression),
            FilterOperator.IsLessThan => Expression.LessThan(propertyAccess, valueExpression),
            FilterOperator.IsLessThanOrEqualTo => Expression.LessThanOrEqual(propertyAccess, valueExpression),
            FilterOperator.IsGreaterThan => Expression.GreaterThan(propertyAccess, valueExpression),
            FilterOperator.IsGreaterThanOrEqualTo => Expression.GreaterThanOrEqual(propertyAccess, valueExpression),
            FilterOperator.Contains when isString => Expression.Call(propExpr, StringContains, valExpr),
            FilterOperator.DoesNotContain when isString => Expression.Not(Expression.Call(propExpr, StringContains, valExpr)),
            FilterOperator.StartsWith when isString => Expression.Call(propExpr, StringStartsWith, valExpr),
            FilterOperator.EndsWith when isString => Expression.Call(propExpr, StringEndsWith, valExpr),
            _ => null
        };
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            value = jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
                System.Text.Json.JsonValueKind.Number => jsonElement.GetRawText(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Null => null,
                _ => jsonElement.GetRawText()
            };
        }
        if (value == null) return null;
        if (underlyingType.IsAssignableFrom(value.GetType())) return value;
        var strValue = value.ToString();
        if (string.IsNullOrEmpty(strValue)) return null;
        if (underlyingType == typeof(Guid)) return Guid.Parse(strValue);
        if (underlyingType.IsEnum) return Enum.Parse(underlyingType, strValue, true);
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
        {
            if (DateTime.TryParse(strValue, out var dt))
                return underlyingType == typeof(DateTime) ? dt : (object)new DateTimeOffset(dt);
        }
        if (underlyingType == typeof(long))
        {
            if (DateTime.TryParse(strValue, out var dt)) return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
            if (long.TryParse(strValue, out var lv)) return lv;
        }
        return Convert.ChangeType(strValue, underlyingType);
    }
}
