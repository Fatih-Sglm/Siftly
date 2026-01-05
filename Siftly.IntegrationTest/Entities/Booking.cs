namespace Siftly.IntegrationTest.Entities;

/// <summary>
/// Test entity demonstrating IFilterTransformable with FilterTransformationBuilder
/// Shows how to use builder pattern for transformations
/// </summary>
public class Booking : IFilterTransformable
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Notes { get; set; }

    /// <summary>
    /// Stored as Unix timestamp milliseconds (long) in DB
    /// Filters can send DateTime strings which will be converted to long
    /// </summary>
    public long BookingDateTimestamp { get; set; }

    /// <summary>
    /// Stored as enum int in DB, but can be filtered by string name
    /// </summary>
    public BookingStatus Status { get; set; }

    public decimal TotalAmount { get; set; }
    public bool IsConfirmed { get; set; }


    /// <summary>
    /// Transform filter conditions using the builder.
    /// </summary>
    public List<FilterCondition> GetTransformedFilters(FilterCondition condition)
        => new FilterTransformationBuilder()
        // DateTime string to Unix timestamp conversion
        .When("BookingDateTimestamp")
        .WithValueTransform(value =>
        {
            if (value is string dateTimeString &&
                DateTimeOffset.TryParse(dateTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTimeOffset))
            {
                return dateTimeOffset.ToUnixTimeMilliseconds();
            }
            return value;
        })
        .MapTo("BookingDateTimestamp")
        // Status string to enum int conversion
        .When("Status")
        .WithValueTransform(value =>
        {
            if (value is string statusString && Enum.TryParse<BookingStatus>(statusString, true, out var status))
            {
                return (int)status;
            }
            return value;
        })
        .MapTo("Status").Transform(condition);
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}
