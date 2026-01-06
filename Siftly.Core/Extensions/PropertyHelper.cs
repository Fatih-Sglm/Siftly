using System.Collections.Concurrent;

namespace Siftly.Core;

internal static class PropertyHelper
{
    private static readonly ConcurrentDictionary<(Type Type, string Path), PropertyMetadata> _cache = new();

    internal static Expression? GetPropertyAccess(this Expression parameter, string fieldPath, out PropertyInfo? lastProperty)
    {
        lastProperty = null;
        if (string.IsNullOrEmpty(fieldPath)) return null;

        var metadata = _cache.GetOrAdd((parameter.Type, fieldPath), key => ComputeMetadata(key.Type, key.Path));

        if (metadata.Chain.Length == 0 || metadata.Property == null) return null;

        lastProperty = metadata.Property;
        Expression current = parameter;

        foreach (var prop in metadata.Chain)
        {
            current = Expression.Property(current, prop);
        }

        return current;
    }

    internal static bool HasCollectionInPath(Type type, string fieldPath)
    {
        if (string.IsNullOrEmpty(fieldPath)) return false;
        var metadata = _cache.GetOrAdd((type, fieldPath), key => ComputeMetadata(key.Type, key.Path));
        return metadata.HasCollection;
    }

    private static PropertyMetadata ComputeMetadata(Type type, string path)
    {
        var currentType = type;
        var pathSpan = path.AsSpan();
        
        int count = 1;
        for (int j = 0; j < pathSpan.Length; j++) if (pathSpan[j] == '.') count++;

        var chain = new List<PropertyInfo>(count);
        var hasCollection = false;
        int start = 0;
        int dotIndex;

        while ((dotIndex = pathSpan[start..].IndexOf('.')) != -1)
        {
            var part = pathSpan.Slice(start, dotIndex).ToString();
            var prop = currentType.GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return new PropertyMetadata(null, hasCollection, chain.ToArray());
            
            chain.Add(prop);
            if (prop.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType))
                hasCollection = true;
            
            currentType = prop.PropertyType;
            start += dotIndex + 1;
        }

        var lastPart = pathSpan[start..].ToString();
        var lastProp = currentType.GetProperty(lastPart, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        
        if (lastProp != null)
        {
            chain.Add(lastProp);
            if (lastProp.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(lastProp.PropertyType))
                hasCollection = true;
        }

        return new PropertyMetadata(lastProp, hasCollection, chain.ToArray());
    }

    private record PropertyMetadata(PropertyInfo? Property, bool HasCollection, PropertyInfo[] Chain);
}
