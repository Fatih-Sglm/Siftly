using Siftly.IntegrationTest.Fixtures;

namespace Siftly.IntegrationTest.Tests;

/// <summary>
/// Tests for IFilterTransformable using FilterTransformationBuilder pattern.
/// Booking entity uses the fluent builder API for transformations.
/// Separate from Event tests which use manual switch/case implementation.
/// </summary>
public abstract class FilterTransformableBuilderTestsBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IDatabaseFixture
{
    protected readonly TFixture Fixture;

    protected FilterTransformableBuilderTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    #region DateTime Transformation Tests

    [Fact]
    public async Task ApplyQueryFilter_DateTimeStringFilter_ConvertsToLongAndFiltersCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Filter by DateTime string - should be converted to Unix timestamp
        var targetDate = new DateTimeOffset(2024, 4, 10, 10, 0, 0, TimeSpan.Zero);
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition(
                "BookingDateTimestamp", 
                FilterOperator.IsLessThan, 
                targetDate.ToString("O")) // ISO 8601 format
        };

        // Act  
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        var targetTimestamp = targetDate.ToUnixTimeMilliseconds();
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b => 
            b.BookingDateTimestamp.Should().BeLessThan(targetTimestamp));
    }

    [Fact]
    public async Task ApplyQueryFilter_DateTimeGreaterThan_ReturnsBookingsAfterDate()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        var afterDate = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition(
                "BookingDateTimestamp", 
                FilterOperator.IsGreaterThan, 
                afterDate.ToString("O"))
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        var afterTimestamp = afterDate.ToUnixTimeMilliseconds();
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b => 
            b.BookingDateTimestamp.Should().BeGreaterThan(afterTimestamp));
    }

    [Fact]
    public async Task ApplyQueryFilter_LongValueDirectly_WorksWithoutTransform()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Use Unix timestamp directly (no transformation needed)
        var timestamp = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("BookingDateTimestamp", FilterOperator.IsGreaterThan, timestamp)
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b => 
            b.BookingDateTimestamp.Should().BeGreaterThan(timestamp));
    }

    #endregion

    #region Enum Transformation Tests

    [Fact]
    public async Task ApplyQueryFilter_StatusStringFilter_ConvertsToEnumAndFiltersCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Filter by status string - should be converted to enum int
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Status", FilterOperator.IsEqualTo, "Confirmed")
        };

        // Act  
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b => 
            b.Status.Should().Be(BookingStatus.Confirmed));
    }

    [Fact]
    public async Task ApplyQueryFilter_StatusPendingString_ReturnsOnlyPendingBookings()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Status", FilterOperator.IsEqualTo, "Pending")
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b => b.Status.Should().Be(BookingStatus.Pending));
    }

    [Fact]
    public async Task ApplyQueryFilter_StatusIntDirectly_WorksWithoutTransform()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Use int value directly (no transformation needed)
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Status", FilterOperator.IsEqualTo, (int)BookingStatus.Completed)
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b => b.Status.Should().Be(BookingStatus.Completed));
    }

    #endregion

    #region Combined Filter Tests

    [Fact]
    public async Task ApplyQueryFilter_DateAndStatus_BothTransformsWork()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        var afterDate = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("BookingDateTimestamp", FilterOperator.IsGreaterThan, afterDate.ToString("O")),
                    new FilterCondition("Status", FilterOperator.IsEqualTo, "Confirmed")
                ]
            }
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        var afterTimestamp = afterDate.ToUnixTimeMilliseconds();
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b =>
        {
            b.BookingDateTimestamp.Should().BeGreaterThan(afterTimestamp);
            b.Status.Should().Be(BookingStatus.Confirmed);
        });
    }

    [Fact]
    public async Task ApplyQueryFilter_TransformedAndNonTransformedFilters_BothWork()
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
                    new FilterCondition("CustomerName", FilterOperator.Contains, "Johnson"),
                    new FilterCondition("Status", FilterOperator.IsEqualTo, "Confirmed"),
                    new FilterCondition("IsConfirmed", FilterOperator.IsEqualTo, true)
                ]
            }
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b =>
        {
            b.CustomerName.Should().Contain("Johnson");
            b.Status.Should().Be(BookingStatus.Confirmed);
            b.IsConfirmed.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ApplyQueryFilter_StatusOrLogic_ReturnsMultipleStatuses()
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
                    new FilterCondition("Status", FilterOperator.IsEqualTo, "Confirmed"),
                    new FilterCondition("Status", FilterOperator.IsEqualTo, "Completed")
                ]
            }
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b =>
            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed).Should().BeTrue());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ApplyQueryFilter_InvalidDateString_TransformReturnsOriginalValue()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Invalid date string - builder will return original string value ("not-a-date")
        // This will cause a type mismatch when comparing to a long column
        // The behavior depends on database provider - SQL Server may return all or none
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("BookingDateTimestamp", FilterOperator.IsEqualTo, "not-a-date")
        };

        // Act - This should not throw, even with invalid input
        var act = async () => await context.Bookings.ToListViewResponseAsync(request);
        
        // Assert - No exception should be thrown
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyQueryFilter_InvalidStatusString_TransformReturnsOriginalValue()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Invalid status string - builder will return original string value
        // This will cause a type mismatch when comparing to an int column
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Status", FilterOperator.IsEqualTo, "InvalidStatus")
        };

        // Act - This should not throw, even with invalid input
        var act = async () => await context.Bookings.ToListViewResponseAsync(request);
        
        // Assert - No exception should be thrown
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyQueryFilter_CaseInsensitiveStatusString_WorksCorrectly()
    {
        // Arrange
        await using var context = Fixture.CreateContext();
        
        // Status string with different case
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("Status", FilterOperator.IsEqualTo, "confirmed") // lowercase
        };

        // Act
        var result = await context.Bookings.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().NotBeEmpty();
        result.ListData.Should().AllSatisfy(b => b.Status.Should().Be(BookingStatus.Confirmed));
    }

    #endregion
}

/// <summary>
/// SQL Server tests for FilterTransformationBuilder pattern
/// </summary>
[Collection("SqlServer")]
public class SqlServerFilterTransformableBuilderTests(SqlServerFixture fixture) 
    : FilterTransformableBuilderTestsBase<SqlServerFixture>(fixture);

/// <summary>
/// PostgreSQL tests for FilterTransformationBuilder pattern
/// </summary>
[Collection("PostgreSql")]
public class PostgreSqlFilterTransformableBuilderTests(PostgreSqlFixture fixture) 
    : FilterTransformableBuilderTestsBase<PostgreSqlFixture>(fixture);
