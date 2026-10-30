using Microsoft.AspNetCore.Mvc;
using Siftly.Core;
using Siftly.EntityFramework;
using Siftly.ExampleApp.Data;
using Siftly.ExampleApp.Entities;

namespace Siftly.ExampleApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(AppDbContext context) : ControllerBase
{
    /// <summary>
    /// Simple paginated list.
    /// GET /api/bookings?page=1&amp;pageSize=10
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = new QueryFilterRequest { Page = page, PageSize = pageSize };

        var result = await context.Bookings
            .ToListViewResponseAsync(request, b => new BookingDto
            {
                Id = b.Id,
                CustomerName = b.CustomerName,
                Notes = b.Notes,
                BookingDateTimestamp = b.BookingDateTimestamp,
                Status = b.Status.ToString(),
                TotalAmount = b.TotalAmount,
                IsConfirmed = b.IsConfirmed
            }, ct);

        return Ok(result);
    }

    /// <summary>
    /// Full query with IFilterTransformable support.
    /// POST /api/bookings/query
    ///
    /// Filter examples:
    /// - BookingDateTimestamp with DateTime string value (e.g. "2024-07-01T00:00:00Z") → auto-converted to Unix ms
    /// - Status with string value (e.g. "Confirmed") → auto-converted to enum int
    /// - Normal fields (CustomerName, TotalAmount, IsConfirmed) work as-is
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> Query(
        [FromBody] QueryFilterRequest request,
        CancellationToken ct = default)
    {
        var result = await context.Bookings
            .ToListViewResponseAsync(request, b => new BookingDto
            {
                Id = b.Id,
                CustomerName = b.CustomerName,
                Notes = b.Notes,
                BookingDateTimestamp = b.BookingDateTimestamp,
                Status = b.Status.ToString(),
                TotalAmount = b.TotalAmount,
                IsConfirmed = b.IsConfirmed
            }, ct);

        return Ok(result);
    }
}

public class BookingDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public long BookingDateTimestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsConfirmed { get; set; }
}
