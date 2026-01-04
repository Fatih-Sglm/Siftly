namespace Siftly.Core;

internal static class PropertyHelper
{
    internal static Expression? GetPropertyAccess(this Expression parameter, string fieldPath, out PropertyInfo? lastProperty)
    {
        lastProperty = null;
        if (string.IsNullOrEmpty(fieldPath)) return null;

        ReadOnlySpan<char> span = fieldPath.AsSpan();
        Expression current = parameter;
        Type currentType = parameter.Type;

        int index;
        while ((index = span.IndexOf('.')) != -1)
        {
            var part = span[..index].ToString();
            lastProperty = currentType.GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (lastProperty == null) return null;

            current = Expression.Property(current, lastProperty);
            currentType = lastProperty.PropertyType;
            span = span[(index + 1)..];
        }

        if (span.Length > 0)
        {
            lastProperty = currentType.GetProperty(span.ToString(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (lastProperty == null) return null;
            current = Expression.Property(current, lastProperty);
        }

        return current;
    }

}
