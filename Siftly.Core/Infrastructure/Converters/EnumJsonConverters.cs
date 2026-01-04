namespace Siftly.Core.Infrastructure.Converters;

/// <summary>
/// Helper for memory-efficient enum parsing using Spans
/// </summary>
internal static class EnumParser
{
    public static FilterOperator ParseOperator(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return FilterOperator.IsEqualTo;

        // Using span-based comparison for zero allocation
        if (span.Equals(FilterConstant.Operators.IsEqualTo.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsEqualTo;
        if (span.Equals(FilterConstant.Operators.IsNotEqualTo.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsNotEqualTo;
        if (span.Equals(FilterConstant.Operators.IsLessThan.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsLessThan;
        if (span.Equals(FilterConstant.Operators.IsLessThanOrEqualTo.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsLessThanOrEqualTo;
        if (span.Equals(FilterConstant.Operators.IsGreaterThan.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsGreaterThan;
        if (span.Equals(FilterConstant.Operators.IsGreaterThanOrEqualTo.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsGreaterThanOrEqualTo;
        if (span.Equals(FilterConstant.Operators.StartsWith.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.StartsWith;
        if (span.Equals(FilterConstant.Operators.EndsWith.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.EndsWith;
        if (span.Equals(FilterConstant.Operators.Contains.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.Contains;
        if (span.Equals(FilterConstant.Operators.IsContainedIn.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsContainedIn;
        if (span.Equals(FilterConstant.Operators.DoesNotContain.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.DoesNotContain;
        if (span.Equals(FilterConstant.Operators.IsNull.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsNull;
        if (span.Equals(FilterConstant.Operators.IsNotNull.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsNotNull;
        if (span.Equals(FilterConstant.Operators.IsEmpty.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsEmpty;
        if (span.Equals(FilterConstant.Operators.IsNotEmpty.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsNotEmpty;
        if (span.Equals(FilterConstant.Operators.IsNullOrEmpty.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsNullOrEmpty;
        if (span.Equals(FilterConstant.Operators.IsNotNullOrEmpty.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.IsNotNullOrEmpty;
        if (span.Equals(FilterConstant.Operators.In.AsSpan(), StringComparison.OrdinalIgnoreCase)) return FilterOperator.In;

        // Fallback to standard enum name parsing
        return Enum.TryParse<FilterOperator>(span, true, out var result) ? result : FilterOperator.IsEqualTo;
    }

    public static ListSortDirection ParseDirection(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return ListSortDirection.Ascending;

        if (span.Equals(FilterConstant.Directions.Asc.AsSpan(), StringComparison.OrdinalIgnoreCase)) return ListSortDirection.Ascending;
        if (span.Equals(FilterConstant.Directions.Desc.AsSpan(), StringComparison.OrdinalIgnoreCase)) return ListSortDirection.Descending;

        return Enum.TryParse<ListSortDirection>(span, true, out var result) ? result : ListSortDirection.Ascending;
    }
}

public class FilterOperatorJsonConverter : JsonConverter<FilterOperator>
{
    public override FilterOperator Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) return FilterOperator.IsEqualTo;
        
        // Utf8JsonReader.HasValueSequence is handled if needed, usually string values are contiguous
        return EnumParser.ParseOperator(reader.GetString().AsSpan());
    }

    public override void Write(Utf8JsonWriter writer, FilterOperator value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class ListSortDirectionJsonConverter : JsonConverter<ListSortDirection>
{
    public override ListSortDirection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) return ListSortDirection.Ascending;
        return EnumParser.ParseDirection(reader.GetString().AsSpan());
    }

    public override void Write(Utf8JsonWriter writer, ListSortDirection value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class FilterOperatorModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult != ValueProviderResult.None)
        {
            var value = valueProviderResult.FirstValue;
            bindingContext.Result = ModelBindingResult.Success(EnumParser.ParseOperator(value.AsSpan()));
        }
        return Task.CompletedTask;
    }
}

public class ListSortDirectionModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult != ValueProviderResult.None)
        {
            var value = valueProviderResult.FirstValue;
            bindingContext.Result = ModelBindingResult.Success(EnumParser.ParseDirection(value.AsSpan()));
        }
        return Task.CompletedTask;
    }
}
