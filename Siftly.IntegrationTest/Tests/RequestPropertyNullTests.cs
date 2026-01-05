using Siftly.IntegrationTest.Fixtures;

namespace Siftly.IntegrationTest.Tests;

/// <summary>
/// Base tests for request with null properties.
/// Shared between SQL Server, PostgreSQL, and InMemory.
/// </summary>
public abstract class RequestPropertyNullTestsBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IDatabaseFixture
{
    protected readonly TFixture Fixture;

    protected RequestPropertyNullTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task RequestObjectExists_ButAllPropertiesAreNull_ShouldStillWork()
    {
        // ARRANGE
        await using var context = Fixture.CreateContext();

        // Request object exists but properties are null/default
        var request = new QueryFilterRequest
        {
            Filter = null,
            Sort = null,
            Cursor = null,
            IncludeCount = true,
            Page = 0,
            PageSize = 5
        };

        // ACT
        var result = await context.Products.ToListViewResponseAsync(request);

        // ASSERT
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(10); // SeedData'da 10 ürün var
        result.ListData.Should().HaveCount(5);
        result.Skip.Should().Be(0);
        result.Take.Should().Be(5);
    }

    [Fact]
    public async Task RequestObjectExists_OnlyPaginationSet_ShouldWork()
    {
        // ARRANGE
        await using var context = Fixture.CreateContext();

        var request = new QueryFilterRequest
        {
            Page = 2,
            PageSize = 3,
            IncludeCount = true,
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
            // Filter ve Cursor null
        };

        // ACT
        var result = await context.Products.ToListViewResponseAsync(request);

        // ASSERT
        result.ListData.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        // Page 2, PageSize 3 means skip 3, so we should get products with Id 4, 5, 6
        result.ListData.First().Id.Should().Be(4);
    }

    [Fact]
    public async Task RequestObjectExists_EmptyFiltersArray_ShouldWork()
    {
        // ARRANGE
        await using var context = Fixture.CreateContext();

        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters = [] // Empty filters array
            },
            IncludeCount = true,
            PageSize = 10
        };

        // ACT
        var result = await context.Products.ToListViewResponseAsync(request);

        // ASSERT
        result.Should().NotBeNull();
        result.ListData.Should().HaveCount(10);
        result.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task RequestObjectExists_OnlySortSet_ShouldWork()
    {
        // ARRANGE
        await using var context = Fixture.CreateContext();

        var request = new QueryFilterRequest
        {
            Sort = [new SortDescriptor("Price", ListSortDirection.Descending)],
            IncludeCount = true,
            PageSize = 5
        };

        // ACT
        var result = await context.Products.ToListViewResponseAsync(request);

        // ASSERT
        result.ListData.Should().HaveCount(5);
        result.ListData.Should().BeInDescendingOrder(p => p.Price);
    }
}

/// <summary>
/// SQL Server request property null tests
/// </summary>
[Collection("SqlServer")]
public class SqlServerRequestPropertyNullTests(SqlServerFixture fixture) 
    : RequestPropertyNullTestsBase<SqlServerFixture>(fixture);

/// <summary>
/// PostgreSQL request property null tests
/// </summary>
[Collection("PostgreSql")]
public class PostgreSqlRequestPropertyNullTests(PostgreSqlFixture fixture) 
    : RequestPropertyNullTestsBase<PostgreSqlFixture>(fixture);

/// <summary>
/// InMemory request property null tests
/// </summary>
[Collection("InMemory")]
public class InMemoryRequestPropertyNullTests(InMemoryFixture fixture) 
    : RequestPropertyNullTestsBase<InMemoryFixture>(fixture);
