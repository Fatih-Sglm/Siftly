using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Siftly.Core;
using Siftly.EntityFramework;
using Xunit;

namespace Siftly.MultiLanguageContentTest.Tests;

// Basit test modeli
public class SimpleEmployee
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class SimpleDbContext : DbContext
{
    public SimpleDbContext(DbContextOptions<SimpleDbContext> options) : base(options) { }
    public DbSet<SimpleEmployee> Employees => Set<SimpleEmployee>();
}

public class RequestPropertyNullTests
{
    private async Task<SimpleDbContext> GetContext()
    {
        var options = new DbContextOptionsBuilder<SimpleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SimpleDbContext(options);

        // Test datasý
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

        // Görseldeki durum: Obje var ama içindekiler null (veya default)
        var request = new QueryFilterRequest
        {
            Filter = null,
            Sort = null,
            Cursor = null,
            IncludeCount = true,
            Skip = 0,
            Take = 20 // Default deðer
        };

        // ACT
        var result = await context.Employees.ApplyQueryFilterAsync(request);

        // ASSERT
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(50); // Count düzgün çalýþmalý
        result.ListData.Should().HaveCount(20); // Take düzgün çalýþmalý
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
            Skip = 10,
            Take = 5,
            IncludeCount = true
            // Filter ve Sort null
        };

        // ACT
        var result = await context.Employees.ApplyQueryFilterAsync(request);

        // ASSERT
        result.ListData.Should().HaveCount(5);
        result.TotalCount.Should().Be(50);
        result.ListData.First().Name.Should().Be("Employee 11");
    }
}