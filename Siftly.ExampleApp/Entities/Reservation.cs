using Siftly.Core;

namespace Siftly.ExampleApp.Entities;

public class Reservation
{
    public int Id { get; set; }

    [FilterTransform("search")]
    public string GuestName { get; set; } = string.Empty;

    [FilterTransform("search")]
    public string? Notes { get; set; }

    [FilterTransform("search")]
    public string RoomNumber { get; set; } = string.Empty;

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

public class ReservationStatusTransformer : StringToEnumTransformer<ReservationStatus>;
