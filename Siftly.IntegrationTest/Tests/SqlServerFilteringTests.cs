namespace EfCore.Querying.Tests.Integration.Tests;

/// <summary>
/// SQL Server integration tests for filtering
/// </summary>
[Collection("SqlServer")]
public class SqlServerFilteringTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

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
        var result = await context.Products.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p =>
            (p.Name.StartsWith("iPhone") || p.Name.StartsWith("MacBook")).Should().BeTrue());
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
        var result = await context.Products.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().BeInAscendingOrder(p => p.Price);
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
            PageNumber = 2,
            PageSize = 2,
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
        };

        // Act
        var result = await context.Products.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().HaveCount(2);
    }

    #endregion

    #region Complex Logic

    [Fact]
    public async Task ApplyQueryFilterAsync_ComplexNestedLogic_ReturnsMatchingProducts()
    {
        // Arrange
        // Query: ((IsActive == true AND Price > 100) OR (Status == Draft)) 
        //        AND (Name.Contains("Samsung") OR Name.Contains("iPhone"))
        await using var context = fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    // Group 1: ((IsActive == true AND Price > 100) OR (Status == Draft))
                    new FilterCondition
                    {
                        Logic = FilterConstant.Or,
                        Filters =
                        [
                            new FilterCondition
                            {
                                Logic = FilterConstant.And,
                                Filters =
                                [
                                    new FilterCondition("IsActive", FilterOperator.IsEqualTo, true),
                                    new FilterCondition("Price", FilterOperator.IsGreaterThan, 100m)
                                ]
                            },
                            new FilterCondition("Status", FilterOperator.IsEqualTo, (int)ProductStatus.Draft)
                        ]
                    },
                    // Group 2: (Name.Contains("Samsung") OR Name.Contains("iPhone"))
                    new FilterCondition
                    {
                        Logic = FilterConstant.Or,
                        Filters =
                        [
                            new FilterCondition("Name", FilterOperator.Contains, "Samsung"),
                            new FilterCondition("Name", FilterOperator.Contains, "iPhone")
                        ]
                    }
                ]
            }
        };

        // Act
        var result = await context.Products.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().HaveCount(2); 
        result.ListData.Should().AllSatisfy(p =>
        {
            // Verify logic
            var part1 = (p.IsActive && p.Price > 100) || p.Status == ProductStatus.Draft;
            var part2 = p.Name.Contains("Samsung") || p.Name.Contains("iPhone");
            (part1 && part2).Should().BeTrue();
        });
    }

    #endregion
}
