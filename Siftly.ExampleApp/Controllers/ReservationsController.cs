using Microsoft.AspNetCore.Mvc;
using Siftly.Core;
using Siftly.EntityFramework;
using Siftly.ExampleApp.Data;
using Siftly.ExampleApp.Entities;

namespace Siftly.ExampleApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(AppDbContext context) : ControllerBase
{
    /// <summary>
    /// Simple paginated list.
    /// GET /api/reservations?page=1&amp;pageSize=10
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = new QueryFilterRequest { Page = page, PageSize = pageSize };

        var result = await context.Reservations
            .ToListViewResponseAsync(request, r => new ReservationDto
            {
                Id = r.Id,
                GuestName = r.GuestName,
                Notes = r.Notes,
                RoomNumber = r.RoomNumber,
                ReservationDateTimestamp = r.ReservationDateTimestamp,
                Status = r.Status.ToString(),
                TotalPrice = r.TotalPrice,
                NumberOfGuests = r.NumberOfGuests
            }, ct);

        return Ok(result);
    }

    /// <summary>
    /// Full query with FilterTransformAttribute support.
    /// POST /api/reservations/query
    ///
    /// Filter examples:
    /// - "search" field → searches across GuestName, Notes, RoomNumber (multi-field search via OR)
    /// - "ReservationDateTimestamp" with DateTime string → auto-converted to Unix ms via DateTimeToUnixTransformer
    /// - "ReservationStatus" with string (e.g. "Confirmed") → auto-converted to enum int via ReservationStatusTransformer
    /// - Normal fields (TotalPrice, NumberOfGuests) work as-is
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> Query(
        [FromBody] QueryFilterRequest request,
        CancellationToken ct = default)
    {
        var result = await context.Reservations
            .ToListViewResponseAsync(request, r => new ReservationDto
            {
                Id = r.Id,
                GuestName = r.GuestName,
                Notes = r.Notes,
                RoomNumber = r.RoomNumber,
                ReservationDateTimestamp = r.ReservationDateTimestamp,
                Status = r.Status.ToString(),
                TotalPrice = r.TotalPrice,
                NumberOfGuests = r.NumberOfGuests
            }, ct);

        return Ok(result);
    }
}

public class ReservationDto
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public long ReservationDateTimestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int NumberOfGuests { get; set; }
}
