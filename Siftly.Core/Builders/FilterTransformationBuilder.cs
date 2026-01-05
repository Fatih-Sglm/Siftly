namespace Siftly.Core;

/// <summary>
/// Builder for defining filter field transformations in a fluent way
/// </summary>
public class FilterTransformationBuilder
{
    private readonly List<FilterTransformationRule> _rules = [];

    /// <summary>
    /// Define a field mapping rule
    /// </summary>
    /// <param name="sourceField">The incoming field name (case insensitive)</param>
    /// <returns>Rule builder for further configuration</returns>
    public FilterTransformationRuleBuilder When(string sourceField)
    {
        var rule = new FilterTransformationRule { SourceField = sourceField };
        _rules.Add(rule);
        return new FilterTransformationRuleBuilder(this, rule);
    }

    /// <summary>
    /// Apply the defined transformations to a filter condition.
    /// Returns the original condition in a list if no matching rule is found.
    /// Returns transformed condition(s) if a rule matches.
    /// </summary>
    /// <param name="condition">The condition to transform</param>
    /// <returns>List of transformed conditions (or original if no rule matched)</returns>
    public List<FilterCondition> Transform(FilterCondition condition)
    {
        if (string.IsNullOrEmpty(condition.Field))
            return [condition];

        foreach (var rule in _rules)
        {
            if (!condition.Field.Equals(rule.SourceField, StringComparison.OrdinalIgnoreCase))
                continue;

            // Apply value transformation first
            var transformedValue = rule.ValueTransformer != null 
                ? rule.ValueTransformer(condition.Value) 
                : condition.Value;

            // Apply field transformation
            if (rule.TargetFields.Count > 0)
            {
                // Multiple target fields - return list with OR logic handled by parent
                var results = new List<FilterCondition>();
                foreach (var targetField in rule.TargetFields)
                {
                    results.Add(new FilterCondition
                    {
                        Field = targetField,
                        Operator = condition.Operator,
                        Value = transformedValue,
                        CaseSensitiveFilter = condition.CaseSensitiveFilter
                    });
                }
                return results;
            }
            else if (rule.IsCollectionFilter)
            {
                // Collection filter (one-to-many)
                return [new FilterCondition
                {
                    Field = $"_collection_:{rule.CollectionPath}:{rule.CollectionItemField}",
                    Operator = condition.Operator,
                    Value = transformedValue,
                    CaseSensitiveFilter = condition.CaseSensitiveFilter
                }];
            }
            else if (rule.IsManyToManyFilter)
            {
                // Many-to-many filter with join entity
                return [new FilterCondition
                {
                    Field = $"_m2m_:{rule.CollectionPath}:{rule.JoinNavigationProperty}:{rule.CollectionItemField}",
                    Operator = condition.Operator,
                    Value = transformedValue,
                    CaseSensitiveFilter = condition.CaseSensitiveFilter
                }];
            }
            else if (!string.IsNullOrEmpty(rule.TargetField))
            {
                // Single target field mapping
                return [new FilterCondition
                {
                    Field = rule.TargetField,
                    Operator = condition.Operator,
                    Value = transformedValue,
                    CaseSensitiveFilter = condition.CaseSensitiveFilter
                }];
            }
            else if (rule.ValueTransformer != null)
            {
                // Value-only transformation - same field, transformed value
                return [new FilterCondition
                {
                    Field = condition.Field,
                    Operator = condition.Operator,
                    Value = transformedValue,
                    CaseSensitiveFilter = condition.CaseSensitiveFilter
                }];
            }
        }

        // No matching rule - return original condition unchanged
        return [condition];
    }

    /// <summary>
    /// Get rule for a field (used by expression builder)
    /// </summary>
    internal FilterTransformationRule? GetRule(string field)
    {
        return _rules.FirstOrDefault(r => r.SourceField.Equals(field, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Rule builder for fluent configuration
/// </summary>
public class FilterTransformationRuleBuilder
{
    private readonly FilterTransformationBuilder _parent;
    private readonly FilterTransformationRule _rule;

    internal FilterTransformationRuleBuilder(FilterTransformationBuilder parent, FilterTransformationRule rule)
    {
        _parent = parent;
        _rule = rule;
    }

    /// <summary>
    /// Map to a single target field
    /// </summary>
    public FilterTransformationBuilder MapTo(string targetField)
    {
        _rule.TargetField = targetField;
        return _parent;
    }

    /// <summary>
    /// Map to multiple target fields (will create OR conditions)
    /// Example: .When("search").MapToMany("Name", "Description", "Code")
    /// This will search in all three fields with OR logic
    /// </summary>
    public FilterTransformationBuilder MapToMany(params string[] targetFields)
    {
        _rule.TargetFields.AddRange(targetFields);
        return _parent;
    }

    /// <summary>
    /// Map to a collection property with Any() semantics (One-to-Many)
    /// Example: .When("tagName").MapToCollection("Tags", "Name")
    /// This creates: entity.Tags.Any(t => t.Name.Contains("value"))
    /// </summary>
    /// <param name="collectionPath">Path to the collection property (e.g., "Tags")</param>
    /// <param name="itemField">Field on the collection item to filter (e.g., "Name")</param>
    public FilterTransformationBuilder MapToCollection(string collectionPath, string itemField)
    {
        _rule.IsCollectionFilter = true;
        _rule.CollectionPath = collectionPath;
        _rule.CollectionItemField = itemField;
        return _parent;
    }

    /// <summary>
    /// Map to a many-to-many relationship through a join entity
    /// Example: .When("categoryName").MapToManyToMany("ProductCategories", "Category", "Name")
    /// This creates: entity.ProductCategories.Any(pc => pc.Category.Name.Contains("value"))
    /// </summary>
    /// <param name="joinCollectionPath">Path to the join collection (e.g., "ProductCategories")</param>
    /// <param name="navigationProperty">Navigation property on join entity to the related entity (e.g., "Category")</param>
    /// <param name="itemField">Field on the related entity to filter (e.g., "Name")</param>
    public FilterTransformationBuilder MapToManyToMany(string joinCollectionPath, string navigationProperty, string itemField)
    {
        _rule.IsManyToManyFilter = true;
        _rule.CollectionPath = joinCollectionPath;
        _rule.JoinNavigationProperty = navigationProperty;
        _rule.CollectionItemField = itemField;
        return _parent;
    }

    /// <summary>
    /// Transform the value before filtering
    /// Example: .When("status").WithValueTransform(v => Enum.Parse&lt;Status&gt;(v.ToString())).MapTo("StatusId")
    /// </summary>
    public FilterTransformationRuleBuilder WithValueTransform(Func<object?, object?> transformer)
    {
        _rule.ValueTransformer = transformer;
        return this;
    }
}

/// <summary>
/// Internal rule definition
/// </summary>
internal class FilterTransformationRule
{
    public string SourceField { get; set; } = string.Empty;
    public string? TargetField { get; set; }
    public List<string> TargetFields { get; set; } = [];
    public Func<object?, object?>? ValueTransformer { get; set; }
    
    // Collection filtering (One-to-Many)
    public bool IsCollectionFilter { get; set; }
    public string? CollectionPath { get; set; }
    public string? CollectionItemField { get; set; }
    
    // Many-to-Many filtering
    public bool IsManyToManyFilter { get; set; }
    public string? JoinNavigationProperty { get; set; }
}
