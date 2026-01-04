namespace Siftly.Core.Infrastructure.Binders;

/// <summary>
/// Model binder to handle dynamic object values from query strings or JSON
/// </summary>
public class JsonObjectModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        var valueStr = valueProviderResult.FirstValue;
        if (string.IsNullOrEmpty(valueStr))
        {
            return Task.CompletedTask;
        }

        try
        {
            // Try to deserialize. If it's a plain string, it might fail if not quoted,
            // so we handle it gracefully.
            object? result;
            
            // Check if it's potentially a JSON string (starts with {, [, ", or is a number/bool)
            if (IsJsonFormat(valueStr))
            {
                result = JsonSerializer.Deserialize<object>(valueStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                // If it's just a plain string (like a date string or UUID), use it as is
                result = valueStr;
            }

            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch
        {
            // Fallback to plain string if deserialization fails
            bindingContext.Result = ModelBindingResult.Success(valueStr);
        }

        return Task.CompletedTask;
    }

    private static bool IsJsonFormat(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        
        var v = value.Trim();
        return (v.StartsWith("{") && v.EndsWith("}")) || 
               (v.StartsWith("[") && v.EndsWith("]")) ||
               (v.StartsWith("\"") && v.EndsWith("\"")) ||
               bool.TryParse(v, out _) ||
               double.TryParse(v, out _);
    }
}
