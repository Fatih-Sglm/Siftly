using Siftly.Core;

namespace Siftly.ExampleApp.Entities;

public class Booking : IFilterTransformable
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public long BookingDateTimestamp { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsConfirmed { get; set; }

    public List<FilterCondition> GetTransformedFilters(FilterCondition condition)
        => new FilterTransformationBuilder()
            .When("BookingDateTimestamp")
            .WithValueTransform(value =>
            {
                if (value is string dateTimeString &&
                    DateTimeOffset.TryParse(dateTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind,
                        out var dateTimeOffset))
                {
                    return dateTimeOffset.ToUnixTimeMilliseconds();
                }

                return value;
            })
            .MapTo("BookingDateTimestamp")
            .When("Status")
            .WithValueTransform(value =>
            {
                if (value is string statusString &&
                    Enum.TryParse<BookingStatus>(statusString, true, out var status))
                {
                    return (int)status;
                }

                return value;
            })
            .MapTo("Status")
            .Transform(condition);
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}
