namespace Siftly.Core;

/// <summary>
/// Extensions to transform standard FilterCondition objects into specialized sub-types
/// </summary>
public static class FilterTransformExtensions
{
    /// <summary>
    /// Transforms a standard FilterCondition (and its nested tree) into a specialized sub-type.
    /// Existing property values (Field, Value, Operator, etc.) are preserved.
    /// </summary>
    /// <typeparam name="T">The specialized type to convert to (must inherit from FilterCondition)</typeparam>
    /// <param name="source">The source filter condition tree</param>
    /// <param name="configure">Optional action to configure the specialized properties (e.g., set LanguageCode)</param>
    /// <returns>A new instance of T with values copied from source</returns>
    public static T ToSpecialized<T>(this FilterCondition source, Action<T>? configure = null) where T : FilterCondition, new()
    {
        if (source == null) return null!;

        // 1. Create a new instance of the specialized type
        var specialized = new T
        {
            // 2. Copy base FilterDescriptorBase properties
            CaseSensitiveFilter = source.CaseSensitiveFilter,

            // 3. Copy standard FilterCondition properties
            Field = source.Field,
            Operator = source.Operator,
            Value = source.Value,
            Logic = source.Logic
        };

        // 4. Transform nested filters recursively
        if (source.Filters != null && source.Filters.Count > 0)
        {
            specialized.Filters = [.. source.Filters.Select(f => (FilterCondition)f.ToSpecialized<T>(configure))];
        }

        // 5. Apply custom specialized configuration
        configure?.Invoke(specialized);

        return specialized;
    }

    /// <summary>
    /// Transforms a standard FilterCondition tree into a specialized sub-type using a template object.
    /// Values from the source (Field, Value, etc.) will overwrite any values in the template.
    /// Note: This is a shallow copy for the template's custom properties.
    /// </summary>
    public static T ToSpecialized<T>(this FilterCondition source, T template) where T : FilterCondition, new()
    {
        return source.ToSpecialized<T>(specialized => 
        {
            // We use reflection to copy custom properties from template to each node if needed,
            // but usually specialized filters only have 1 or 2 extra props like LanguageCode.
            // For simplicity and performance, we can just copy public properties that are not in the base.
            
            var baseProps = typeof(FilterCondition).GetProperties().Select(p => p.Name).ToHashSet();
            var specializedProps = typeof(T).GetProperties();

            foreach (var prop in specializedProps)
            {
                if (!baseProps.Contains(prop.Name) && prop.CanWrite && prop.CanRead)
                {
                    var value = prop.GetValue(template);
                    prop.SetValue(specialized, value);
                }
            }
        });
    }

    /// <summary>
    /// Specializes the root filter of a QueryFilterRequest
    /// </summary>
    public static QueryFilterRequest SpecializeFilter<T>(this QueryFilterRequest request, Action<T>? configure = null) where T : FilterCondition, new()
    {
        if (request.Filter != null)
        {
            request.Filter = request.Filter.ToSpecialized<T>(configure);
        }
        return request;
    }
}
