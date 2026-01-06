namespace Siftly.IntegrationTest.Tests;

/// <summary>
/// Tests for IFilterTransformable using FilterTransformAttribute pattern.
/// Reservation entity uses attributes for transformations.
/// </summary>
public abstract class FilterTransformAttributeTestsBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IDatabaseFixture
{
    protected readonly TFixture Fixture;

    protected FilterTransformAttributeTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task ApplyQueryFilter_AttributeDateTimeTransform_WorksCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        var targetDate = new DateTimeOffset(2024, 8, 11, 0, 0, 0, TimeSpan.Zero);
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition(
                "ReservationDateTimestamp", 
                FilterOperator.IsLessThan, 
                targetDate.ToString("O"))
        };

        // Act  
        var result = await context.Reservations.ToListViewResponseAsync(request);

        // Assert
        var targetTimestamp = targetDate.ToUnixTimeMilliseconds();
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(r => 
            r.ReservationDateTimestamp.Should().BeLessThan(targetTimestamp));
    }

    [Fact]
    public async Task ApplyQueryFilter_AttributeStatusTransform_WorksCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("ReservationStatus", FilterOperator.IsEqualTo, "Confirmed")
        };

        // Act  
        var result = await context.Reservations.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(r => 
            r.Status.Should().Be(ReservationStatus.Confirmed));
    }

    [Fact]
    public async Task ApplyQueryFilter_AttributeMultiFieldSearch_WorksCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // "Search" maps to GuestName, Notes, or RoomNumber
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("search", FilterOperator.Contains, "101")
        };

        // Act  
        var result = await context.Reservations.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(r => 
            r.RoomNumber.Should().Be("101"));
    }

    [Fact]
    public async Task ApplyQueryFilter_AttributeCollectionTransform_WorksCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // "HasPremiumTag" maps to Tags collection items where Name contains the value
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("HasPremiumTag", FilterOperator.IsEqualTo, "Premium")
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
    public async Task ApplyQueryFilter_AttributeManyToManyTransform_WorksCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Find Electronics category Id
        var electronics = await context.Categories.FirstAsync(c => c.Name == "Electronics");

        // "CategoryId" maps to ProductCategories.Category.Id
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("CategoryId", FilterOperator.IsEqualTo, electronics.Id)
        };
 
        // Act  
        var result = await context.Products
            .Include(p => p.ProductCategories)
            .ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(p => 
            p.ProductCategories.Any(pc => pc.CategoryId == electronics.Id).Should().BeTrue());
    }
}

/// <summary>
/// SQL Server tests for FilterTransformAttribute pattern
/// </summary>
[Collection("SqlServer")]
public class SqlServerFilterTransformAttributeTests(SqlServerFixture fixture) 
    : FilterTransformAttributeTestsBase<SqlServerFixture>(fixture);

/// <summary>
/// PostgreSQL tests for FilterTransformAttribute pattern
/// </summary>
[Collection("PostgreSql")]
public class PostgreSqlFilterTransformAttributeTests(PostgreSqlFixture fixture) 
    : FilterTransformAttributeTestsBase<PostgreSqlFixture>(fixture);
