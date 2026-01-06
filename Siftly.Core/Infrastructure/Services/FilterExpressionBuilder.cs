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

    private static readonly ConcurrentDictionary<(Type, string), MethodInfo> _genericMethodCache = new();

    public static Expression? BuildFilterExpression<T>(FilterCondition filter, ParameterExpression parameter, QueryFilterOptions options) where T : class, new()
    {
        var filterTransformable = FilterTransformableCache<T>.Instance;

        if (filter.Filters == null || filter.Filters.Count == 0)
        {
            var transformed = GetTransformedFilters<T>(filter, filterTransformable);
            
            if (transformed.Count == 1)
                return BuildConditionExpression<T>(transformed[0], parameter, options);
            
            if (transformed.Count > 1)
            {
                Expression? orExpr = null;
                foreach (var cond in transformed)
                {
                    var expr = BuildConditionExpression<T>(cond, parameter, options);
                    if (expr != null)
                        orExpr = orExpr == null ? expr : Expression.OrElse(orExpr, expr);
                }
                return orExpr;
            }
            
            return null;
        }

        Expression? combinedExpression = null;
        var isAndLogic = (filter.Logic ?? FilterConstant.And).Equals(FilterConstant.And, StringComparison.OrdinalIgnoreCase);

        foreach (var condition in filter.Filters)
        {
            Expression? conditionExpression = null;

            if (condition.Filters != null && condition.Filters.Count > 0)
            {
                conditionExpression = BuildFilterExpression<T>(condition, parameter, options);
            }
            else
            {
                var transformedConditions = GetTransformedFilters<T>(condition, filterTransformable);

                if (transformedConditions.Count == 1)
                {
                    conditionExpression = BuildConditionExpression<T>(transformedConditions[0], parameter, options);
                }
                else if (transformedConditions.Count > 1)
                {
                    Expression? orExpression = null;
                    foreach (var transformedCondition in transformedConditions)
                    {
                        var expr = BuildConditionExpression<T>(transformedCondition, parameter, options);
                        if (expr != null)
                            orExpression = orExpression == null ? expr : Expression.OrElse(orExpression, expr);
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

    private static List<FilterCondition> GetTransformedFilters<T>(FilterCondition cond, IFilterTransformable? transformable) where T : class
    {
        if (transformable != null)
        {
            var result = transformable.GetTransformedFilters(cond);
            if (result.Count > 1 || (result.Count == 1 && (result[0].Field != cond.Field || result[0].Value != cond.Value)))
            {
                return result;
            }
        }
        
        return FilterTransformAttributeHelper.GetTransformedFilters<T>(cond);
    }

    public static Expression? BuildConditionExpression<T>(FilterCondition condition, ParameterExpression parameter, QueryFilterOptions options) where T : class, new()
    {
        try
        {
            if (string.IsNullOrEmpty(condition.Field))
                return null;

            // Flag-based Detection (Priority)
            if (condition.IsManyToMany && !string.IsNullOrEmpty(condition.JoinNavigationProperty) && !string.IsNullOrEmpty(condition.ItemField))
            {
                return BuildCollectionExpressionDirect<T>(condition.Field, $"{condition.JoinNavigationProperty}.{condition.ItemField}", condition, parameter, options);
            }

            if (condition.IsCollection && !string.IsNullOrEmpty(condition.ItemField))
            {
                return BuildCollectionExpressionDirect<T>(condition.Field, condition.ItemField, condition, parameter, options);
            }

            // Automatic Path Detection (Fallback)
            // If the field is "Tags.Name" and "Tags" is a collection, we handle it as a collection filter.
            if (condition.Field.Contains('.') && PropertyHelper.HasCollectionInPath(typeof(T), condition.Field))
            {
                var parts = condition.Field.Split('.');
                var currentPath = string.Empty;
                var currentType = typeof(T);

                for (int i = 0; i < parts.Length - 1; i++) // We still split once here to find the split point, but we know it's a collection
                {
                    currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : $"{currentPath}.{parts[i]}";
                    var prop = currentType.GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    
                    if (prop != null)
                    {
                        if (prop.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType))
                        {
                            var itemField = string.Join(".", parts.Skip(i + 1));
                            return BuildCollectionExpressionDirect<T>(currentPath, itemField, condition, parameter, options);
                        }
                        currentType = prop.PropertyType;
                    }
                }
            }

            var propertyAccess = parameter.GetPropertyAccess(condition.Field, out var property);
            if (propertyAccess == null || property == null) return null;

            var typeBuilder = options.GetTypeBuilder(property.PropertyType);
            if (typeBuilder != null)
            {
                return typeBuilder.BuildExpression(propertyAccess, condition);
            }

            return BuildStandardExpression(parameter, property, condition.Operator, condition.Value, options, condition.CaseSensitiveFilter, propertyAccess);
        }
        catch
        {
            return null;
        }
    }

    private static MethodCallExpression? BuildCollectionExpressionDirect<T>(string collectionPath, string itemField, FilterCondition condition, ParameterExpression parameter, QueryFilterOptions options)
    {
        var collectionAccess = parameter.GetPropertyAccess(collectionPath, out var collectionProperty);
        if (collectionAccess == null || collectionProperty == null) return null;

        var currentType = collectionProperty.PropertyType;
        var itemType = currentType.IsGenericType ? currentType.GetGenericArguments()[0] : currentType.GetElementType();
        if (itemType == null) return null;

        var itemParam = Expression.Parameter(itemType, "t");
        
        // Handle nested properties inside collection (e.g. "Tags.Category.Name")
        var itemPropertyAccess = itemParam.GetPropertyAccess(itemField, out var itemProperty);
        if (itemPropertyAccess == null || itemProperty == null) return null;

        Expression? predicateBody = null;
        var typeBuilder = options.GetTypeBuilder(itemProperty.PropertyType);
        if (typeBuilder != null)
        {
            predicateBody = typeBuilder.BuildExpression(itemPropertyAccess, condition);
        }

        predicateBody ??= BuildStandardExpression(itemParam, itemProperty, condition.Operator, condition.Value, options, condition.CaseSensitiveFilter, itemPropertyAccess);
        if (predicateBody == null) return null;

        var anyMethod = _genericMethodCache.GetOrAdd((itemType, "Enumerable.Any"), key => 
            typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(key.Item1));
        
        var lambda = Expression.Lambda(predicateBody, itemParam);
        return Expression.Call(anyMethod, collectionAccess, lambda);
    }

    private static readonly MethodInfo StringToLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
    private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo StringStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo StringEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
    private static readonly MethodInfo StringIsNullOrEmpty = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;

    private static Expression? BuildStandardExpression(ParameterExpression parameter, PropertyInfo property, FilterOperator operatorType, object? value, QueryFilterOptions options, bool caseSensitive = false, Expression? propertyAccess = null)
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

        if (isString && !caseSensitive && !options.DisableAutomaticToLower && convertedValue is string strVal)
        {
            propExpr = Expression.Call(propertyAccess, StringToLower);
            // Optimization: Lowercase the constant value once during expression building, not at runtime.
            valExpr = Expression.Constant(strVal.ToLower());
        }

        return operatorType switch
        {
            FilterOperator.IsEqualTo => Expression.Equal(propExpr, valExpr),
            FilterOperator.IsNotEqualTo => Expression.NotEqual(propExpr, valExpr),
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

internal static class FilterTransformableCache<T> where T : class, new()
{
    public static readonly IFilterTransformable? Instance;

    static FilterTransformableCache()
    {
        if (typeof(IFilterTransformable).IsAssignableFrom(typeof(T)))
        {
            Instance = Activator.CreateInstance<T>() as IFilterTransformable;
        }
    }
}
