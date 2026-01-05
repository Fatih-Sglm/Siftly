using Siftly.IntegrationTest.Fixtures;

namespace Siftly.IntegrationTest.Tests;

/// <summary>
/// Base collection filtering tests (One-to-Many and Many-to-Many relationships)
/// Shared between SQL Server and PostgreSQL.
/// </summary>
public abstract class CollectionFilteringTestsBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IDatabaseFixture
{
    protected readonly TFixture Fixture;

    protected CollectionFilteringTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    #region One-to-Many Collection Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_CollectionFilter_TagName_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("_collection_:Tags:Name", FilterOperator.Contains, "Apple")
        };

        // Act
        var result = await context.Products
            .Include(p => p.Tags)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
            p.Tags.Any(t => t.Name.Contains("Apple")).Should().BeTrue());
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_CollectionFilter_TagEquals_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("_collection_:Tags:Name", FilterOperator.IsEqualTo, "Premium")
        };

        // Act
        var result = await context.Products
            .Include(p => p.Tags)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
            p.Tags.Any(t => t.Name == "Premium").Should().BeTrue());
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_CollectionFilter_MultipleConditions_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("IsActive", FilterOperator.IsEqualTo, true),
                    new FilterCondition("_collection_:Tags:Name", FilterOperator.Contains, "Smartphone")
                ]
            }
        };

        // Act
        var result = await context.Products
            .Include(p => p.Tags)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            p.Tags.Any(t => t.Name.Contains("Smartphone")).Should().BeTrue();
        });
    }

    #endregion

    #region Many-to-Many Collection Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_ManyToManyFilter_CategoryName_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("_m2m_:ProductCategories:Category:Name", FilterOperator.IsEqualTo, "Electronics")
        };

        // Act
        var result = await context.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
            p.ProductCategories.Any(pc => pc.Category.Name == "Electronics").Should().BeTrue());
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_ManyToManyFilter_CategoryContains_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("_m2m_:ProductCategories:Category:Name", FilterOperator.Contains, "Sport")
        };

        // Act
        var result = await context.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
            p.ProductCategories.Any(pc => pc.Category.Name.Contains("Sport")).Should().BeTrue());
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_ManyToManyFilter_WithOtherFilters_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("IsActive", FilterOperator.IsEqualTo, true),
                    new FilterCondition("Price", FilterOperator.IsGreaterThan, 100m),
                    new FilterCondition("_m2m_:ProductCategories:Category:Name", FilterOperator.IsEqualTo, "Electronics")
                ]
            }
        };

        // Act
        var result = await context.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            p.Price.Should().BeGreaterThan(100);
            p.ProductCategories.Any(pc => pc.Category.Name == "Electronics").Should().BeTrue();
        });
    }

    #endregion

    #region Or Logic with Collection Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_CollectionFilter_OrLogic_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.Or,
                Filters =
                [
                    new FilterCondition("_collection_:Tags:Name", FilterOperator.IsEqualTo, "Apple"),
                    new FilterCondition("_collection_:Tags:Name", FilterOperator.IsEqualTo, "Samsung")
                ]
            }
        };

        // Act
        var result = await context.Products
            .Include(p => p.Tags)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
            (p.Tags.Any(t => t.Name == "Apple") || p.Tags.Any(t => t.Name == "Samsung")).Should().BeTrue());
    }

    #endregion
}

/// <summary>
/// SQL Server collection filtering tests
/// </summary>
[Collection("SqlServer")]
public class SqlServerCollectionFilteringTests(SqlServerFixture fixture) 
    : CollectionFilteringTestsBase<SqlServerFixture>(fixture);

/// <summary>
/// PostgreSQL collection filtering tests
/// </summary>
[Collection("PostgreSql")]
public class PostgreSqlCollectionFilteringTests(PostgreSqlFixture fixture) 
    : CollectionFilteringTestsBase<PostgreSqlFixture>(fixture);
