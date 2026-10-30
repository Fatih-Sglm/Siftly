using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Siftly.Core;
using Siftly.EntityFramework;
using Testcontainers.PostgreSql;

namespace Siftly.Benchmark;

public class Program
{
    public static async Task Main(string[] args)
    {
        await using var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("siftly_benchmark")
            .Build();
        await container.StartAsync();
        Environment.SetEnvironmentVariable(FilterBenchmark.ConnectionStringVariable, container.GetConnectionString());

        await using (var context = FilterBenchmark.CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
            FilterBenchmark.Seed(context);
        }

        BenchmarkRunner.Run<FilterBenchmark>(args: ["--inProcess", .. args]);
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
    public const string ConnectionStringVariable = "SIFTLY_BENCHMARK_CONNECTION";

    private BenchmarkDbContext _context = null!;
    private long _interfaceTimestamp;

    public static BenchmarkDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? throw new InvalidOperationException($"{ConnectionStringVariable} is not set. Run the benchmark through Program.Main.");

        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseNpgsql(connectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return new BenchmarkDbContext(options);
    }

    public static void Seed(BenchmarkDbContext context)
    {
        context.Categories.AddRange(Enumerable.Range(1, 10).Select(i => new Category { Id = i, Name = $"Category {i}" }));

        context.Products.AddRange(Enumerable.Range(1, 1000).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Description = $"Description for product {i}",
            Price = i * 10,
            Tags = [new ProductTag { Id = i, Name = $"Tag {i % 10}" }],
            ProductCategories = [new ProductCategory { ProductId = i, CategoryId = (i % 10) + 1 }]
        }));

        context.Events.AddRange(Enumerable.Range(1, 1000).Select(i => new Event
        {
            Id = i,
            Name = $"Event {i}",
            CreatedOnTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));

        context.SaveChanges();
    }

    [GlobalSetup]
    public void Setup()
    {
        _context = CreateContext();
        _interfaceTimestamp = DateTimeOffset.Parse("2024-01-01T00:00:00Z").ToUnixTimeMilliseconds();

        (string Name, int Raw, int Siftly)[] pairs =
        [
            ("Attribute", RawEfCore_AttributeEquivalent().Count, Siftly_AttributeSearch().Count),
            ("Complex_Sensitive", RawEfCore_ComplexEquivalent_Sensitive().Count, Siftly_ComplexSearch_Sensitive().Count),
            ("Complex_Insensitive", RawEfCore_ComplexEquivalent_Insensitive().Count, Siftly_ComplexSearch_Insensitive().Count),
            ("Interface", RawEfCore_InterfaceEquivalent().Count, Siftly_InterfaceSearch().Count)
        ];
        foreach (var pair in pairs.Where(p => p.Raw != p.Siftly))
            throw new InvalidOperationException($"{pair.Name}: raw EF returned {pair.Raw} rows, Siftly returned {pair.Siftly}");
    }

    [GlobalCleanup]
    public void Cleanup() => _context.Dispose();

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
        return _context.Products.ApplyFilters(request).ToList();
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
        return _context.Products.ApplyFilters(request).ToList();
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
        return _context.Products.ApplyFilters(request).ToList();
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
        return _context.Events.ApplyFilters(request).ToList();
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
