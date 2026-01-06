namespace Siftly.Core;

/// <summary>
/// Provides filter transformation using attributes with caching for optimal performance.
/// This is an alternative to implementing IFilterTransformable manually.
/// </summary>
public static class FilterTransformAttributeHelper
{
    private static readonly ConcurrentDictionary<Type, List<CompiledTransformRule>> _cache = new();
    private static readonly ConcurrentDictionary<Type, IValueTransformer> _transformerCache = new();

    /// <summary>
    /// Get transformed filter conditions for an entity type using attribute-based configuration.
    /// Uses caching to minimize reflection overhead - first call per type pays the cost,
    /// subsequent calls are O(n) where n is the number of rules.
    /// </summary>
    /// <typeparam name="T">The entity type decorated with FilterTransformAttribute</typeparam>
    /// <param name="condition">The filter condition to transform</param>
    /// <returns>List of transformed conditions, or original condition if no transformation matches</returns>
    public static List<FilterCondition> GetTransformedFilters<T>(FilterCondition condition) where T : class
    {
        return GetTransformedFilters(typeof(T), condition);
    }

    /// <summary>
    /// Get transformed filter conditions for an entity type using attribute-based configuration.
    /// </summary>
    public static List<FilterCondition> GetTransformedFilters(Type entityType, FilterCondition condition)
    {
        if (string.IsNullOrEmpty(condition.Field))
            return [condition];

        var rules = GetOrCreateRules(entityType);
        
        foreach (var rule in rules)
        {
            if (!condition.Field.Equals(rule.SourceField, StringComparison.OrdinalIgnoreCase))
                continue;

            // Apply value transformation
            var transformedValue = rule.ValueTransformer?.Transform(condition.Value) ?? condition.Value;

            // Apply field transformation based on rule type
            if (rule.TargetFields.Count > 0)
            {
                var results = new List<FilterCondition>();
                foreach (var targetField in rule.TargetFields)
                {
                    results.Add(new FilterCondition
                    {
                        Field = targetField,
                        Operator = condition.Operator,
                        Value = transformedValue,
                        CaseSensitiveFilter = condition.CaseSensitiveFilter,
                        IsCollection = rule.IsCollection,
                        IsManyToMany = rule.IsManyToMany,
                        ItemField = rule.ItemField,
                        JoinNavigationProperty = rule.JoinNavigationProperty
                    });
                }
                return results;
            }
            else if (!string.IsNullOrEmpty(rule.TargetField))
            {
                return [new FilterCondition
                {
                    Field = rule.TargetField,
                    Operator = condition.Operator,
                    Value = transformedValue,
                    CaseSensitiveFilter = condition.CaseSensitiveFilter,
                    IsCollection = rule.IsCollection,
                    IsManyToMany = rule.IsManyToMany,
                    ItemField = rule.ItemField,
                    JoinNavigationProperty = rule.JoinNavigationProperty
                }];
            }
            else if (rule.ValueTransformer != null)
            {
                return [new FilterCondition
                {
                    Field = condition.Field,
                    Operator = condition.Operator,
                    Value = transformedValue,
                    CaseSensitiveFilter = condition.CaseSensitiveFilter
                }];
            }
        }

        return [condition];
    }

    private static List<CompiledTransformRule> GetOrCreateRules(Type entityType)
    {
        return _cache.GetOrAdd(entityType, type =>
        {
            var rulesMap = new Dictionary<string, CompiledTransformRule>(StringComparer.OrdinalIgnoreCase);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var attributes = prop.GetCustomAttributes<FilterTransformAttribute>();

                foreach (var attr in attributes)
                {
                    if (!rulesMap.TryGetValue(attr.SourceField, out var rule))
                    {
                        rule = new CompiledTransformRule 
                        { 
                            SourceField = attr.SourceField,
                            IsCollection = attr.IsCollection,
                            IsManyToMany = attr.IsManyToMany,
                            ItemField = attr.ItemField,
                            JoinNavigationProperty = attr.JoinProperty
                        };
                        
                        // Initialize value transformer
                        if (attr.ValueTransformerType != null && typeof(IValueTransformer).IsAssignableFrom(attr.ValueTransformerType))
                        {
                            rule.ValueTransformer = _transformerCache.GetOrAdd(attr.ValueTransformerType, t =>
                                (IValueTransformer)Activator.CreateInstance(t)!);
                        }
                        rulesMap[attr.SourceField] = rule;
                    }

                    // Construct target path
                    string targetPath = prop.Name;
                    
                    // If it's NOT a flag-based special case, we can use dot notation for nested properties
                    // But for collections/m2m we keep targetPath as the source collection name
                    if (!attr.IsManyToMany && !attr.IsCollection && !string.IsNullOrEmpty(attr.ItemField))
                    {
                        targetPath = $"{prop.Name}.{attr.ItemField}";
                    }

                    if (!rule.TargetFields.Contains(targetPath))
                        rule.TargetFields.Add(targetPath);

                    if (!string.IsNullOrEmpty(attr.TargetFields))
                    {
                        foreach (var tf in attr.TargetFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            if (!rule.TargetFields.Contains(tf)) rule.TargetFields.Add(tf);
                        }
                    }
                }
            }

            var finalRules = rulesMap.Values.ToList();
            foreach (var rule in finalRules)
            {
                if (rule.TargetFields.Count == 1)
                {
                    rule.TargetField = rule.TargetFields[0];
                    rule.TargetFields.Clear();
                }
            }
            return finalRules;
        });
    }

    /// <summary>
    /// Compiled transformation rule for fast lookup
    /// </summary>
    private class CompiledTransformRule
    {
        public string SourceField { get; set; } = string.Empty;
        public string? TargetField { get; set; }
        public List<string> TargetFields { get; set; } = [];
        public IValueTransformer? ValueTransformer { get; set; }
        public bool IsCollection { get; set; }
        public bool IsManyToMany { get; set; }
        public string? ItemField { get; set; }
        public string? JoinNavigationProperty { get; set; }
    }
}
