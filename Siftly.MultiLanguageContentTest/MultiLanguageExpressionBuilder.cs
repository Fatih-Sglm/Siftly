
namespace Siftly.IntegrationTest;

/// <summary>
/// Custom filter class for MultiLanguageContent
/// </summary>
public class MultiLangFilter : FilterCondition
{
    public string LanguageCode { get; set; } = "tr";
}

/// <summary>
/// Custom sort descriptor for MultiLanguageContent with language code
/// </summary>
public class MultiLangSortDescriptor : SortDescriptor
{
    public string LanguageCode { get; set; } = "tr";
}

public class MultiLanguageExpressionBuilder : ITypeExpressionBuilder<MultiLanguageContent>
{
    public Expression? BuildExpression(Expression propertyAccess, FilterCondition condition)
    {
        var value = condition.Value;
        if (value == null) return null;
        var strValue = value.ToString();
        if (string.IsNullOrEmpty(strValue)) return null;

        // Try to get language code from the specialized filter class if it is one
        // Otherwise, it might be a standard FilterCondition
        string? languageCode = condition is MultiLangFilter mlf ? mlf.LanguageCode : null;
        
        var translationsProperty = Expression.Property(propertyAccess, nameof(MultiLanguageContent.Content));

        return condition.Operator switch
        {
            FilterOperator.Contains => BuildContainsExpression(translationsProperty, languageCode, strValue, condition.CaseSensitiveFilter),
            FilterOperator.IsEqualTo => BuildEqualsExpression(translationsProperty, languageCode, strValue, condition.CaseSensitiveFilter),
            FilterOperator.StartsWith => BuildStartsWithExpression(translationsProperty, languageCode, strValue, condition.CaseSensitiveFilter),
            FilterOperator.EndsWith => BuildEndsWithExpression(translationsProperty, languageCode, strValue, condition.CaseSensitiveFilter),
            _ => null
        };
    }

    private static MethodCallExpression BuildContainsExpression(Expression translationsProperty, string? languageCode, string searchValue, bool caseSensitive)
        => BuildStringMethodExpression(translationsProperty, languageCode, searchValue, caseSensitive, nameof(string.Contains));

    private static MethodCallExpression BuildEqualsExpression(Expression translationsProperty, string? languageCode, string searchValue, bool caseSensitive)
    {
        var itemParam = Expression.Parameter(typeof(LangContentDto), "t");
        var valueProperty = Expression.Property(itemParam, nameof(LangContentDto.Value));

        Expression condition;
        var valueCheck = caseSensitive 
            ? Expression.Equal(valueProperty, Expression.Constant(searchValue))
            : Expression.Equal(Expression.Call(valueProperty, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!), Expression.Constant(searchValue.ToLower()));

        if (!string.IsNullOrEmpty(languageCode))
        {
            var langKeyProperty = Expression.Property(itemParam, nameof(LangContentDto.Language));
            var langKeyEquals = Expression.Equal(langKeyProperty, Expression.Constant(languageCode));
            condition = Expression.AndAlso(langKeyEquals, valueCheck);
        }
        else
        {
            condition = valueCheck;
        }

        var predicate = Expression.Lambda<Func<LangContentDto, bool>>(condition, itemParam);
        var anyMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 2).MakeGenericMethod(typeof(LangContentDto));
        return Expression.Call(anyMethod, translationsProperty, predicate);
    }

    private static MethodCallExpression BuildStringMethodExpression(Expression translationsProperty, string? languageCode, string searchValue, bool caseSensitive, string methodName)
    {
        var itemParam = Expression.Parameter(typeof(LangContentDto), "t");
        var valueProperty = Expression.Property(itemParam, nameof(LangContentDto.Value));

        var valueCheck = Expression.Call(
            caseSensitive ? valueProperty : Expression.Call(valueProperty, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!),
            typeof(string).GetMethod(methodName, [typeof(string)])!,
            Expression.Constant(caseSensitive ? searchValue : searchValue.ToLower()));

        Expression condition;
        if (!string.IsNullOrEmpty(languageCode))
        {
            var langKeyProperty = Expression.Property(itemParam, nameof(LangContentDto.Language));
            var langKeyEquals = Expression.Equal(langKeyProperty, Expression.Constant(languageCode));
            condition = Expression.AndAlso(langKeyEquals, valueCheck);
        }
        else
        {
            condition = valueCheck;
        }

        var predicate = Expression.Lambda<Func<LangContentDto, bool>>(condition, itemParam);
        var anyMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 2).MakeGenericMethod(typeof(LangContentDto));
        return Expression.Call(anyMethod, translationsProperty, predicate);
    }

    private static MethodCallExpression BuildStartsWithExpression(Expression translationsProperty, string? languageCode, string searchValue, bool caseSensitive)
        => BuildStringMethodExpression(translationsProperty, languageCode, searchValue, caseSensitive, nameof(string.StartsWith));

    private static MethodCallExpression BuildEndsWithExpression(Expression translationsProperty, string? languageCode, string searchValue, bool caseSensitive)
        => BuildStringMethodExpression(translationsProperty, languageCode, searchValue, caseSensitive, nameof(string.EndsWith));
}

/// <summary>
/// Custom sort expression builder for MultiLanguageContent.
/// Extracts a sortable string value from the MultiLanguageContent for ordering.
/// </summary>
public class MultiLanguageSortExpressionBuilder(string defaultLanguage = "tr") : ISortExpressionBuilder<MultiLanguageContent>
{
    public Expression? BuildSortExpression(Expression propertyAccess, SortDescriptor sortDescriptor)
    {
        // Get language code from MultiLangSortDescriptor if available, otherwise use default
        string languageCode = sortDescriptor is MultiLangSortDescriptor mlsd 
            ? mlsd.LanguageCode 
            : defaultLanguage;

        // Build: x.Name.Content.Where(c => c.Language == "tr").Select(c => c.Value).FirstOrDefault()
        // This pattern is EF Core translatable (scalar projection before FirstOrDefault)
        var contentProperty = Expression.Property(propertyAccess, nameof(MultiLanguageContent.Content));
        
        // Parameter for lambda expressions
        var itemParam = Expression.Parameter(typeof(LangContentDto), "c");
        
        // 1. Where(c => c.Language == "tr")
        var langProperty = Expression.Property(itemParam, nameof(LangContentDto.Language));
        var langEquals = Expression.Equal(langProperty, Expression.Constant(languageCode));
        var wherePredicate = Expression.Lambda<Func<LangContentDto, bool>>(langEquals, itemParam);
        
        var whereMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters().Length == 2 
                && m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2)
            .MakeGenericMethod(typeof(LangContentDto));
        
        var whereCall = Expression.Call(whereMethod, contentProperty, wherePredicate);
        
        // 2. Select(c => c.Value)
        var selectParam = Expression.Parameter(typeof(LangContentDto), "c");
        var valueProperty = Expression.Property(selectParam, nameof(LangContentDto.Value));
        var selectLambda = Expression.Lambda<Func<LangContentDto, string>>(valueProperty, selectParam);
        
        var selectMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2)
            .MakeGenericMethod(typeof(LangContentDto), typeof(string));
        
        var selectCall = Expression.Call(selectMethod, whereCall, selectLambda);
        
        // 3. FirstOrDefault()
        var firstOrDefaultMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(string));
        
        var firstOrDefaultCall = Expression.Call(firstOrDefaultMethod, selectCall);
        
        return firstOrDefaultCall;
    }
}

