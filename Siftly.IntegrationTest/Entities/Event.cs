namespace Siftly.IntegrationTest.Entities;

/// <summary>
/// Test entity demonstrating IFilterTransformable with timestamp conversion
/// DB stores CreatedOnTimestamp as long (Unix milliseconds), but filters can send DateTime strings
/// </summary>
public class Event : IFilterTransformable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Stored as Unix timestamp milliseconds (long) in DB
    /// Filters can send DateTime strings which will be converted to long
    /// </summary>
    public long CreatedOnTimestamp { get; set; }

    public bool IsActive { get; set; }
    public string? Location { get; set; }

    public List<FilterCondition> GetTransformedFilters(FilterCondition condition)
    {
        return condition.Field switch
        {
            "CreatedOnTimestamp" => condition.Value switch
            {
                string dateTimeString
                    when DateTimeOffset.TryParse(
                        dateTimeString,
                        null,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out var dateTimeOffset)
                    =>
                    [
                        new FilterCondition(
                        condition.Field,
                        condition.Operator,
                        dateTimeOffset.ToUnixTimeMilliseconds()
                    )
                    ],

                _ => [condition]
            },

            _ => [condition]
        };
    }

}

