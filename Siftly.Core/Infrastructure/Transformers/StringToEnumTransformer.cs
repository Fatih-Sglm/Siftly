namespace Siftly.Core;

/// <summary>
/// Built-in transformer: String to Enum (generic)
/// Usage: Create a derived class for your specific enum type
/// </summary>
public class StringToEnumTransformer<TEnum> : IValueTransformer<string?, int>
    where TEnum : struct, Enum
{
    public int Transform(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;

        if (Enum.TryParse<TEnum>(value, true, out var enumValue))
        {
            return Convert.ToInt32(enumValue);
        }
        return 0;
    }
}
