using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Siftly.Core;
using Siftly.EntityFramework;

namespace Siftly.Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<FilterBenchmark>();
    }
}

#region Entities

public class Product
{
    public int Id { get; set; }

    [FilterTransform("search")]
    public string Name { get; set; } = string.Empty;

    [FilterTransform("search")]
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public List<ProductTag> Tags { get; set; } = new();
    public List<ProductCategory> ProductCategories { get; set; } = new();
}

public class ProductTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProductCategory
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public class Event : IFilterTransformable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long CreatedOnTimestamp { get; set; }

    public List<FilterCondition> GetTransformedFilters(FilterCondition condition)
    {
        if (condition.Field == "CreatedOn" && condition.Value is string dateStr)
        {
            if (DateTimeOffset.TryParse(dateStr, out var dto))
            {
                return [new FilterCondition("CreatedOnTimestamp", condition.Operator, dto.ToUnixTimeMilliseconds())];
            }
        }
        return [condition];
    }
}

#endregion

[MemoryDiagnoser]
public class FilterBenchmark
{
    private BenchmarkDbContext _context = null!;
    private long _interfaceTimestamp;

    [GlobalSetup]
    public void Setup()
    {
        var optionsBuilder = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        _context = new BenchmarkDbContext(optionsBuilder.Options);

        // Seed
        var categories = Enumerable.Range(1, 10).Select(i => new Category { Id = i, Name = $"Category {i}" }).ToList();
        _context.Categories.AddRange(categories);

        var products = Enumerable.Range(1, 1000).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Description = $"Description for product {i}",
            Price = i * 10,
            Tags = [new ProductTag { Id = i, Name = $"Tag {i % 10}" }],
            ProductCategories = [new ProductCategory { ProductId = i, CategoryId = (i % 10) + 1 }]
        }).ToList();
        _context.Products.AddRange(products);

        var events = Enumerable.Range(1, 1000).Select(i => new Event
        {
            Id = i,
            Name = $"Event {i}",
            CreatedOnTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }).ToList();
        _context.Events.AddRange(events);

        _context.SaveChanges();

        // Pre-parse for raw equivalent baseline
        _interfaceTimestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z").ToUnixTimeMilliseconds();
    }

    #region Normal Search (Attribute Based)

    [Benchmark(Baseline = true)]
    public List<Product> RawEfCore_AttributeEquivalent()
    {
        var searchQuery = "500";
        return _context.Products
            .Where(p => p.Name.Contains(searchQuery) || p.Description.Contains(searchQuery))
            .ToList();
    }

    [Benchmark]
    public List<Product> Siftly_AttributeSearch()
    {
        var request = new QueryFilterRequest 
        { 
            Filter = new FilterCondition("search", FilterOperator.Contains, "500") { CaseSensitiveFilter = true } 
        };
        return _context.Products.ApplyQueryFilter(request).ToList();
    }

    #endregion

    #region Complex Search (Multiple Filters)

    [Benchmark]
    public List<Product> RawEfCore_ComplexEquivalent_Sensitive()
    {
        var searchQuery = "500";
        var priceLimit = 5000m;
        var tagName = "Tag 5";

        return _context.Products
            .Where(p => (p.Name.Contains(searchQuery) || p.Description.Contains(searchQuery)) 
                        && p.Price > priceLimit 
                        && p.Tags.Any(t => t.Name == tagName))
            .ToList();
    }

    [Benchmark]
    public List<Product> RawEfCore_ComplexEquivalent_Insensitive()
    {
        var searchQuery = "500";
        var priceLimit = 5000m;
        var tagName = "Tag 5";

        return _context.Products
            .Where(p => (p.Name.ToLower().Contains(searchQuery.ToLower()) || p.Description.ToLower().Contains(searchQuery.ToLower())) 
                        && p.Price > priceLimit 
                        && p.Tags.Any(t => t.Name.ToLower() == tagName.ToLower()))
            .ToList();
    }

    [Benchmark]
    public List<Product> Siftly_ComplexSearch_Sensitive()
    {
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("search", FilterOperator.Contains, "500") { CaseSensitiveFilter = true },
                    new FilterCondition("Price", FilterOperator.IsGreaterThan, 5000m) { CaseSensitiveFilter = true },
                    new FilterCondition("Tags.Name", FilterOperator.IsEqualTo, "Tag 5") { CaseSensitiveFilter = true }
                ]
            }
        };
        return _context.Products.ApplyQueryFilter(request).ToList();
    }

    [Benchmark]
    public List<Product> Siftly_ComplexSearch_Insensitive()
    {
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("search", FilterOperator.Contains, "500") { CaseSensitiveFilter = false },
                    new FilterCondition("Price", FilterOperator.IsGreaterThan, 5000m) { CaseSensitiveFilter = false },
                    new FilterCondition("Tags.Name", FilterOperator.IsEqualTo, "Tag 5") { CaseSensitiveFilter = false }
                ]
            }
        };
        return _context.Products.ApplyQueryFilter(request).ToList();
    }

    #endregion

    #region Interface Search

    [Benchmark]
    public List<Event> RawEfCore_InterfaceEquivalent()
    {
        return _context.Events.Where(e => e.CreatedOnTimestamp > _interfaceTimestamp).ToList();
    }

    [Benchmark]
    public List<Event> Siftly_InterfaceSearch()
    {
        var request = new QueryFilterRequest 
        { 
            Filter = new FilterCondition("CreatedOn", FilterOperator.IsGreaterThan, "2024-01-01T00:00:00Z") 
        };
        return _context.Events.ApplyQueryFilter(request).ToList();
    }

    #endregion
}

public class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasKey(p => p.Id);
        modelBuilder.Entity<Category>().HasKey(c => c.Id);
        modelBuilder.Entity<Event>().HasKey(e => e.Id);
        modelBuilder.Entity<ProductCategory>().HasKey(pc => new { pc.ProductId, pc.CategoryId });
    }
}
