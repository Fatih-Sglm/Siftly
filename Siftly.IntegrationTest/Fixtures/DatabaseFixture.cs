namespace EfCore.Querying.Tests.Integration.Fixtures;

/// <summary>
/// Base database fixture interface for test database management
/// </summary>
public interface IDatabaseFixture : IAsyncLifetime
{
    TestDbContext CreateContext();
    string DatabaseProvider { get; }
}

/// <summary>
/// SQL Server LocalDB fixture for MSSQL integration tests
/// </summary>
public class SqlServerFixture : IDatabaseFixture
{
    private readonly string _connectionString;
    private DbContextOptions<TestDbContext>? _options;

    public SqlServerFixture()
    {
        var databaseName = $"EfCoreQuerying_Test_{Guid.NewGuid():N}";
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true";
    }

    public string DatabaseProvider => "SqlServer";

    public async Task InitializeAsync()
    {
        // Setup QueryFilter IoC
        var services = new ServiceCollection();
        services.AddQueryFilter();
        services.BuildServiceProvider();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        await using var context = new TestDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(context);
    }

    public async Task DisposeAsync()
    {
        if (_options != null)
        {
            await using var context = new TestDbContext(_options);
            await context.Database.EnsureDeletedAsync();
        }
    }

    public TestDbContext CreateContext()
    {
        return new TestDbContext(_options!);
    }

    private static async Task SeedTestDataAsync(TestDbContext context)
    {
        await SeedDataHelper.SeedAsync(context);
    }
}

/// <summary>
/// PostgreSQL fixture for PostgreSQL integration tests
/// Uses Testcontainers for Docker-based database
/// </summary>
public class PostgreSqlFixture : IDatabaseFixture
{
    private readonly Testcontainers.PostgreSql.PostgreSqlContainer _container;
    private DbContextOptions<TestDbContext>? _options;

    public PostgreSqlFixture()
    {
        _container = new Testcontainers.PostgreSql.PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("efcore_querying_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public string DatabaseProvider => "PostgreSql";

    public async Task InitializeAsync()
    {
        // Setup QueryFilter IoC
        var services = new ServiceCollection();
        services.AddQueryFilter();
        services.BuildServiceProvider();

        await _container.StartAsync();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        await using var context = new TestDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        await SeedDataHelper.SeedAsync(context);
    }

    public async Task DisposeAsync()
    {
        if (_options != null)
        {
            await using var context = new TestDbContext(_options);
            await context.Database.EnsureDeletedAsync();
        }
        await _container.DisposeAsync();
    }

    public TestDbContext CreateContext()
    {
        return new TestDbContext(_options!);
    }
}

/// <summary>
/// In-Memory database fixture for quick tests (not real database tests)
/// </summary>
public class InMemoryFixture : IDatabaseFixture
{
    private DbContextOptions<TestDbContext>? _options;
    private readonly string _databaseName = $"InMemory_Test_{Guid.NewGuid():N}";

    public string DatabaseProvider => "InMemory";

    public async Task InitializeAsync()
    {
        // Setup QueryFilter IoC
        var services = new ServiceCollection();
        services.AddQueryFilter();
        services.BuildServiceProvider();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        await using var context = new TestDbContext(_options);
        await SeedDataHelper.SeedAsync(context);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public TestDbContext CreateContext()
    {
        return new TestDbContext(_options!);
    }
}

/// <summary>
/// Helper class for seeding test data
/// </summary>
public static class SeedDataHelper
{
    public static async Task SeedAsync(TestDbContext context)
    {
        // Categories
        var categories = new[]
        {
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Clothing" },
            new Category { Id = 3, Name = "Books" },
            new Category { Id = 4, Name = "Home & Garden" },
            new Category { Id = 5, Name = "Sports" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Products with various data for filtering tests
        var products = new[]
        {
            new Product
            {
                Id = 1,
                Name = "iPhone 15 Pro",
                Description = "Latest Apple smartphone with A17 chip",
                Price = 1199.99m,
                IsActive = true,
                CreatedAt = new DateTime(2024, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 50,
                Status = ProductStatus.Published
            },
            new Product
            {
                Id = 2,
                Name = "Samsung Galaxy S24",
                Description = "Android flagship phone",
                Price = 999.99m,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 75,
                Status = ProductStatus.Published
            },
            new Product
            {
                Id = 3,
                Name = "MacBook Pro 16",
                Description = "Professional laptop with M3 chip",
                Price = 2499.00m,
                IsActive = true,
                CreatedAt = new DateTime(2023, 11, 10, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 25,
                Status = ProductStatus.Published
            },
            new Product
            {
                Id = 4,
                Name = "Nike Air Max",
                Description = "Comfortable running shoes",
                Price = 149.99m,
                IsActive = true,
                CreatedAt = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 200,
                Status = ProductStatus.Published
            },
            new Product
            {
                Id = 5,
                Name = "Clean Code Book",
                Description = "A Handbook of Agile Software Craftsmanship",
                Price = 39.99m,
                IsActive = true,
                CreatedAt = new DateTime(2022, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 500,
                Status = ProductStatus.Published
            },
            new Product
            {
                Id = 6,
                Name = "Vintage T-Shirt",
                Description = "Retro style cotton t-shirt",
                Price = 29.99m,
                IsActive = false,
                CreatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 0,
                Status = ProductStatus.Archived
            },
            new Product
            {
                Id = 7,
                Name = "Garden Furniture Set",
                Description = null,
                Price = 899.00m,
                IsActive = true,
                CreatedAt = new DateTime(2024, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                StockCount = null,
                Status = ProductStatus.Draft
            },
            new Product
            {
                Id = 8,
                Name = "Sony WH-1000XM5",
                Description = "Premium noise-cancelling headphones",
                Price = 349.99m,
                IsActive = true,
                CreatedAt = new DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 100,
                Status = ProductStatus.Published
            },
            new Product
            {
                Id = 9,
                Name = "Dell XPS 15",
                Description = "Windows laptop for professionals",
                Price = 1799.00m,
                IsActive = true,
                CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 40,
                Status = ProductStatus.Published
            },
            new Product
            {
                Id = 10,
                Name = "Adidas Ultraboost",
                Description = "High-performance running shoes",
                Price = 179.99m,
                IsActive = true,
                CreatedAt = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                StockCount = 150,
                Status = ProductStatus.Published
            }
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Product Tags (One-to-Many)
        var productTags = new[]
        {
            new ProductTag { Id = 1, ProductId = 1, Name = "Smartphone" },
            new ProductTag { Id = 2, ProductId = 1, Name = "Apple" },
            new ProductTag { Id = 3, ProductId = 1, Name = "Premium" },
            new ProductTag { Id = 4, ProductId = 2, Name = "Smartphone" },
            new ProductTag { Id = 5, ProductId = 2, Name = "Samsung" },
            new ProductTag { Id = 6, ProductId = 3, Name = "Laptop" },
            new ProductTag { Id = 7, ProductId = 3, Name = "Apple" },
            new ProductTag { Id = 8, ProductId = 4, Name = "Shoes" },
            new ProductTag { Id = 9, ProductId = 4, Name = "Nike" },
            new ProductTag { Id = 10, ProductId = 5, Name = "Programming" },
            new ProductTag { Id = 11, ProductId = 5, Name = "BestSeller" },
            new ProductTag { Id = 12, ProductId = 8, Name = "Headphones" },
            new ProductTag { Id = 13, ProductId = 8, Name = "Sony" },
            new ProductTag { Id = 14, ProductId = 8, Name = "Premium" },
            new ProductTag { Id = 15, ProductId = 9, Name = "Laptop" },
            new ProductTag { Id = 16, ProductId = 10, Name = "Shoes" },
            new ProductTag { Id = 17, ProductId = 10, Name = "Adidas" }
        };
        context.ProductTags.AddRange(productTags);
        await context.SaveChangesAsync();

        // Product Categories (Many-to-Many)
        var productCategories = new[]
        {
            new ProductCategory { ProductId = 1, CategoryId = 1 }, // iPhone -> Electronics
            new ProductCategory { ProductId = 2, CategoryId = 1 }, // Samsung -> Electronics
            new ProductCategory { ProductId = 3, CategoryId = 1 }, // MacBook -> Electronics
            new ProductCategory { ProductId = 4, CategoryId = 2 }, // Nike -> Clothing
            new ProductCategory { ProductId = 4, CategoryId = 5 }, // Nike -> Sports
            new ProductCategory { ProductId = 5, CategoryId = 3 }, // Book -> Books
            new ProductCategory { ProductId = 6, CategoryId = 2 }, // T-Shirt -> Clothing
            new ProductCategory { ProductId = 7, CategoryId = 4 }, // Garden -> Home & Garden
            new ProductCategory { ProductId = 8, CategoryId = 1 }, // Sony -> Electronics
            new ProductCategory { ProductId = 9, CategoryId = 1 }, // Dell -> Electronics
            new ProductCategory { ProductId = 10, CategoryId = 2 }, // Adidas -> Clothing
            new ProductCategory { ProductId = 10, CategoryId = 5 }  // Adidas -> Sports
        };
        context.ProductCategories.AddRange(productCategories);
        await context.SaveChangesAsync();

        // Customers
        var customers = new[]
        {
            new Customer
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                IsVip = true,
                RegistrationDate = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc)
            },
            new Customer
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                IsVip = false,
                RegistrationDate = new DateTime(2021, 6, 20, 0, 0, 0, DateTimeKind.Utc)
            },
            new Customer
            {
                Id = 3,
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@example.com",
                IsVip = true,
                RegistrationDate = new DateTime(2023, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            }
        };
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Orders
        var orders = new[]
        {
            new Order
            {
                Id = 1,
                CustomerId = 1,
                TotalAmount = 1549.98m,
                OrderDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Delivered
            },
            new Order
            {
                Id = 2,
                CustomerId = 1,
                TotalAmount = 2499.00m,
                OrderDate = new DateTime(2024, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Delivered
            },
            new Order
            {
                Id = 3,
                CustomerId = 2,
                TotalAmount = 149.99m,
                OrderDate = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Shipped
            },
            new Order
            {
                Id = 4,
                CustomerId = 3,
                TotalAmount = 389.98m,
                OrderDate = new DateTime(2024, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Confirmed
            },
            new Order
            {
                Id = 5,
                CustomerId = 2,
                TotalAmount = 39.99m,
                OrderDate = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Cancelled
            }
        };
        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();
    }
}
