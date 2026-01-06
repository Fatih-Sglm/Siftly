namespace Siftly.Core;

/// <summary>
/// Built-in transformer: DateTime string to Unix timestamp milliseconds
/// </summary>
public class DateTimeToUnixTransformer : IValueTransformer<string?, long>
{
    public long Transform(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;

        if (DateTimeOffset.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTimeOffset))
        {
            return dateTimeOffset.ToUnixTimeMilliseconds();
        }
        return 0;
    }
}