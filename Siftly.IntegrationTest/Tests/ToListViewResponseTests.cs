namespace Siftly.IntegrationTest.Tests;

[Collection("InMemory")]
public class ToListViewResponseTests(InMemoryFixture fixture)
{
    private readonly InMemoryFixture _fixture = fixture;

    [Fact]
    public async Task ToListViewResponseAsync_ShouldReturnCorrectDataAndTotalCount()
    {
        // ARRANGE
        using var context = _fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            IncludeCount = true,
            Page = 2,
            PageSize = 3
        };

        // ACT
        var result = await context.Products.ToListViewResponseAsync(request);

        // ASSERT
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(10); // SeedData'da 10 ürün var
        result.ListData.Should().HaveCount(3);
    }

    [Fact]
    public async Task ToListViewResponseAsync_WithProjection_ShouldWorkCorrectly()
    {
        // ARRANGE
        using var context = _fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            IncludeCount = true,
            PageSize = 5
        };

        // ACT - Projeksiyon (Select) ile DTO'ya çevirme
        var result = await context.Products.ToListViewResponseAsync(request, x => new 
        { 
            ProductId = x.Id, 
            ProductName = x.Name 
        });

        // ASSERT
        result.ListData.Should().HaveCount(5);
        result.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task ToListViewResponseAsync_WhenRequestIsNull_ShouldReturnAllData()
    {
        // ARRANGE
        using var context = _fixture.CreateContext();

        // ACT
        var result = await context.Products.ToListViewResponseAsync(null!);

        // ASSERT
        result.ListData.Should().HaveCount(10);
        result.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task ToListViewResponseAsync_WithFilter_ShouldFilterCorrectly()
    {
        // ARRANGE
        using var context = _fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            IncludeCount = true,
            Filter = new FilterCondition
            {
                Field = "IsActive",
                Operator = FilterOperator.IsEqualTo,
                Value = true
            }
        };

        // ACT
        var result = await context.Products.ToListViewResponseAsync(request);

        // ASSERT
        result.ListData.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
        result.TotalCount.Should().Be(9); // SeedData'da 9 aktif ürün var
    }
}
