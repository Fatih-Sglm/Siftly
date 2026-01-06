namespace Siftly.IntegrationTest.Entities;

/// <summary>
/// Custom transformer for BookingStatus enum
/// </summary>
public class BookingStatusTransformer : StringToEnumTransformer<BookingStatus>;

/// <summary>
/// Test entity demonstrating IFilterTransformable with FilterTransformAttribute.
/// Uses declarative attribute configuration instead of manual builder/switch implementation.
/// </summary>
public class Reservation
{
    public int Id { get; set; }

    [FilterTransform("search")]
    public string GuestName { get; set; } = string.Empty;

    [FilterTransform("search")]
    public string? Notes { get; set; }

    [FilterTransform("search")]
    public string RoomNumber { get; set; } = string.Empty;

    /// <summary>
    /// Stored as Unix timestamp milliseconds (long) in DB
    /// </summary>
    [FilterTransform<DateTimeToUnixTransformer>("ReservationDateTimestamp")]
    public long ReservationDateTimestamp { get; set; }

    [FilterTransform<ReservationStatusTransformer>("ReservationStatus")]
    public ReservationStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public int NumberOfGuests { get; set; }
}

public enum ReservationStatus
{
    Pending = 0,
    Confirmed = 1,
    CheckedIn = 2,
    CheckedOut = 3,
    Cancelled = 4
}

/// <summary>
/// Custom transformer for ReservationStatus enum
/// </summary>
public class ReservationStatusTransformer : StringToEnumTransformer<ReservationStatus>;
