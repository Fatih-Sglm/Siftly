namespace Siftly.Core;

/// <summary>
/// Interface for value transformers used with FilterTransformAttribute.
/// Implement this to create reusable value transformation logic.
/// </summary>
public interface IValueTransformer
{
    object? Transform(object? value);
}

/// <summary>
/// Generic interface for value transformers providing type safety.
/// </summary>
public interface IValueTransformer<in TSource, out TDestination> : IValueTransformer
{
    TDestination Transform(TSource value);

    // Default implementation to bridge the non-generic call to the generic one
    object? IValueTransformer.Transform(object? value)
    {
        if (value is TSource source)
            return Transform(source);

        if (value == null)
            return Transform(default!);

        return value;
    }
}