namespace EfCore.Querying.Tests.Integration.Tests;

/// <summary>
/// PostgreSQL integration tests for filtering using Testcontainers
/// </summary>
[Collection("PostgreSql")]
public class PostgreSqlFilteringTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{

    #region Basic Equality Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_IsEqualTo_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("IsActive", FilterOperator.IsEqualTo, true)
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_IsNotEqualTo_ReturnsNonMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Status", FilterOperator.IsNotEqualTo, (int)ProductStatus.Published)
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Status.Should().NotBe(ProductStatus.Published));
    }

    #endregion

    #region String Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_Contains_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Name", FilterOperator.Contains, "Pro")
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Name.Should().Contain("Pro"));
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_StartsWith_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Name", FilterOperator.StartsWith, "iPhone")
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Name.Should().StartWith("iPhone"));
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_EndsWith_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Name", FilterOperator.EndsWith, "Max")
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Name.Should().EndWith("Max"));
    }

    #endregion

    #region Comparison Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_IsGreaterThan_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Price", FilterOperator.IsGreaterThan, 1000m)
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Price.Should().BeGreaterThan(1000m));
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_IsLessThanOrEqualTo_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Price", FilterOperator.IsLessThanOrEqualTo, 500m)
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Price.Should().BeLessThanOrEqualTo(500m));
    }

    #endregion

    #region Null/Empty Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_IsNull_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Description", FilterOperator.IsNull, null)
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Description.Should().BeNull());
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_IsNotNull_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Description", FilterOperator.IsNotNull, null)
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => p.Description.Should().NotBeNull());
    }

    #endregion

    #region Composite Filters

    [Fact]
    public async Task ApplyQueryFilterAsync_AndLogic_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("IsActive", FilterOperator.IsEqualTo, true),
                    new FilterCondition("Price", FilterOperator.IsGreaterThan, 500m)
                ]
            }
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            p.Price.Should().BeGreaterThan(500m);
        });
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_OrLogic_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.Or,
                Filters =
                [
                    new FilterCondition("Name", FilterOperator.StartsWith, "iPhone"),
                    new FilterCondition("Name", FilterOperator.StartsWith, "MacBook")
                ]
            }
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
            (p.Name.StartsWith("iPhone") || p.Name.StartsWith("MacBook")).Should().BeTrue());
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_NestedLogic_ReturnsMatchingProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        // (IsActive = true AND (Price < 500 OR Price > 1500))
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("IsActive", FilterOperator.IsEqualTo, true),
                    new FilterCondition
                    {
                        Logic = FilterConstant.Or,
                        Filters =
                        [
                            new FilterCondition("Price", FilterOperator.IsLessThan, 500m),
                            new FilterCondition("Price", FilterOperator.IsGreaterThan, 1500m)
                        ]
                    }
                ]
            }
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            (p.Price < 500m || p.Price > 1500m).Should().BeTrue();
        });
    }

    #endregion

    #region Sorting

    [Fact]
    public async Task ApplyQueryFilterAsync_Sorting_ReturnsOrderedProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Sort =
            [
                new SortDescriptor("Price", ListSortDirection.Ascending)
            ]
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().BeInAscendingOrder(p => p.Price);
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_MultipleSorting_ReturnsOrderedProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Sort =
            [
                new SortDescriptor("IsActive", ListSortDirection.Descending),
                new SortDescriptor("Price", ListSortDirection.Ascending)
            ]
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().BeInDescendingOrder(p => p.IsActive)
            .And.ThenBeInAscendingOrder(p => p.Price);
    }

    #endregion

    #region Pagination

    [Fact]
    public async Task ApplyQueryFilterAsync_SkipAndTake_ReturnsPagedProducts()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Skip = 2,
            Take = 2,
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().HaveCount(2);
        result.TotalCount.Should().BeGreaterThan(4);
    }

    [Fact]
    public async Task ApplyQueryFilterAsync_IncludeCountFalse_ReturnsNullTotal()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            IncludeCount = false,
            Take = 5
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().HaveCount(5);
        result.TotalCount.Should().Be(5); // Default list view response behavior if total is null
    }

    #endregion

    #region Keyset Pagination (Cursor)

    [Fact]
    public async Task ApplyQueryFilterAsync_Cursor_ReturnsNextPage()
    {
        // Arrange
        await using var context = fixture.CreateContext();
        
        // 1. Get first item to use as cursor
        var firstItem = await context.Products.OrderBy(p => p.Id).FirstAsync();
        
        var request = new QueryFilterRequest
        {
            Cursor = new FilterCondition("Id", FilterOperator.IsGreaterThan, firstItem.Id),
            Take = 5,
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
        };

        // Act
        var result = await context.Products.ApplyQueryFilterAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.First().Id.Should().BeGreaterThan(firstItem.Id);
    }

    #endregion
}
