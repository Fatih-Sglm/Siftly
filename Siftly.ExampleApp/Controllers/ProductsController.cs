using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siftly.Core;
using Siftly.EntityFramework;
using Siftly.ExampleApp.Data;

namespace Siftly.ExampleApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext context) : ControllerBase
{
    /// <summary>
    /// Simple paginated list with optional sorting.
    /// GET /api/products?page=1&amp;pageSize=10
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortField = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        var request = new QueryFilterRequest
        {
            Page = page,
            PageSize = pageSize
        };

        if (!string.IsNullOrEmpty(sortField))
        {
            request.Sort =
            [
                new SortDescriptor
                {
                    Field = sortField,
                    Dir = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending
                }
            ];
        }

        var result = await context.Products
            .Include(p => p.Tags)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .ToListViewResponseAsync(request, p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                StockCount = p.StockCount,
                Status = p.Status.ToString(),
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList()
            }, ct);

        return Ok(result);
    }

    /// <summary>
    /// Full query with filtering, sorting, and pagination.
    /// POST /api/products/query
    /// Supports all FilterOperator types, composite AND/OR, collection filtering, cursor pagination.
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> Query(
        [FromBody] QueryFilterRequest request,
        CancellationToken ct = default)
    {
        var result = await context.Products
            .Include(p => p.Tags)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .ToListViewResponseAsync(request, p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                StockCount = p.StockCount,
                Status = p.Status.ToString(),
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList()
            }, ct);

        return Ok(result);
    }

    /// <summary>
    /// Get a single product by ID.
    /// GET /api/products/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var product = await context.Products
            .Include(p => p.Tags)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                StockCount = p.StockCount,
                Status = p.Status.ToString(),
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (product is null) return NotFound();
        return Ok(product);
    }

    /// <summary>
    /// Query with minimal projection (lightweight DTO).
    /// POST /api/products/query-light
    /// </summary>
    [HttpPost("query-light")]
    public async Task<IActionResult> QueryLight(
        [FromBody] QueryFilterRequest request,
        CancellationToken ct = default)
    {
        var result = await context.Products
            .Include(p => p.Tags)
            .ToListViewResponseAsync(request, p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.IsActive,
                Tags = p.Tags.Select(t => t.Name)
            }, ct);

        return Ok(result);
    }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? StockCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<string> Categories { get; set; } = [];
}
