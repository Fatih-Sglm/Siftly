namespace Siftly.IntegrationTest.Tests;

// Basit test modeli
public class SimpleEmployee
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class SimpleDbContext(DbContextOptions<SimpleDbContext> options) : DbContext(options)
{
    public DbSet<SimpleEmployee> Employees => Set<SimpleEmployee>();
}

public class RequestPropertyNullTests
{
    private static async Task<SimpleDbContext> GetContext()
    {
        var options = new DbContextOptionsBuilder<SimpleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SimpleDbContext(options);

        // Test datas�
        for (int i = 1; i <= 50; i++)
        {
            context.Employees.Add(new SimpleEmployee { Name = $"Employee {i}" });
        }
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task RequestObjectExists_ButAllPropertiesAreNull_ShouldStillWork()
    {
        // ARRANGE
        var context = await GetContext();

        // G�rseldeki durum: Obje var ama i�indekiler null (veya default)
        var request = new QueryFilterRequest
        {
            Filter = null,
            Sort = null,
            Cursor = null,
            IncludeCount = true,
            Page = 0,
            PageSize = 20 // Default de�er
        };

        // ACT
        var result = await context.Employees.ToListViewResponseAsync(request);

        // ASSERT
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(50); // Count d�zg�n �al��mal�
        result.ListData.Should().HaveCount(20); // Take d�zg�n �al��mal�
        result.Skip.Should().Be(0);
        result.Take.Should().Be(20);
    }

    [Fact]
    public async Task RequestObjectExists_OnlyPaginationSet_ShouldWork()
    {
        // ARRANGE
        var context = await GetContext();

        var request = new QueryFilterRequest
        {
            Page = 10,
            PageSize = 5,
            IncludeCount = true
            // Filter ve Sort null
        };

        // ACT
        var result = await context.Employees.ToListViewResponseAsync(request);

        // ASSERT
        result.ListData.Should().HaveCount(5);
        result.TotalCount.Should().Be(50);
        result.ListData.First().Name.Should().Be("Employee 46");
    }
}
