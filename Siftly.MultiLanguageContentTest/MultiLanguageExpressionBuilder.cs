
namespace Siftly.IntegrationTest;

/// <summary>
/// Custom filter class for MultiLanguageContent
/// </summary>
public class MultiLangFilter : FilterCondition
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
