using Siftly.IntegrationTest.Fixtures;

namespace Siftly.IntegrationTest.Tests;

/// <summary>
/// Base tests for IFilterTransformable functionality.
/// Tests are shared between SQL Server and PostgreSQL.
/// Verifies that:
/// 1. DateTime strings are correctly converted to long timestamps for filtering
/// 2. Filters for non-transformable fields are NOT affected/lost during transformation
/// </summary>
public abstract class FilterTransformableTestsBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IDatabaseFixture
{
    protected readonly TFixture Fixture;

    protected FilterTransformableTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    #region DateTime to Long Timestamp Conversion Tests

    [Fact]
    public async Task ApplyQueryFilter_DateTimeStringFilter_ConvertsToLongAndFiltersCorrectly()
    {
        // Arrange
        // Event 1: 2024-03-15 10:00:00 UTC
        // Event 5: 2024-03-15 12:00:00 UTC
        // We're filtering for events created AFTER 2024-03-15 09:00:00 UTC
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThan, "2024-03-15T09:00:00Z"),
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert - Events after 2024-03-15 09:00:00 UTC: Event 1, 2, 4, 5
        result.ListData.Should().HaveCount(4);
        result.ListData.Select(e => e.Id).Should().BeEquivalentTo([1, 2, 4, 5]);
    }

    [Fact]
    public async Task ApplyQueryFilter_DateTimeStringFilter_LessThan_FiltersCorrectly()
    {
        // Arrange
        // Filter for events created BEFORE 2024-03-15 00:00:00 UTC
        // Only Event 3 (2024-01-10 09:00:00 UTC) should match
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("CreatedOnTimestamp", FilterOperator.IsLessThan, "2024-03-15T00:00:00Z")
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().HaveCount(1);
        result.ListData.First().Name.Should().Be("Art Exhibition");
    }

    [Fact]
    public async Task ApplyQueryFilter_DateTimeStringFilter_EqualTo_FiltersCorrectly()
    {
        // Arrange
        // Filter for exact timestamp match with Event 1: 2024-03-15 10:00:00 UTC
        await using var context = Fixture.CreateContext();
        var targetTimestamp = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("CreatedOnTimestamp", FilterOperator.IsEqualTo, targetTimestamp.ToString("O"))
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().HaveCount(1);
        result.ListData.First().Name.Should().Be("Tech Conference 2024");
    }

    [Fact]
    public async Task ApplyQueryFilter_DateTimeStringFilter_Between_FiltersCorrectly()
    {
        // Arrange
        // Filter for events between 2024-03-01 and 2024-04-01
        // Should match Event 1 (2024-03-15 10:00) and Event 5 (2024-03-15 12:00)
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThanOrEqualTo, "2024-03-01T00:00:00Z"),
                    new FilterCondition("CreatedOnTimestamp", FilterOperator.IsLessThan, "2024-04-01T00:00:00Z")
                ]
            },
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().HaveCount(2);
        result.ListData.Select(e => e.Name).Should().BeEquivalentTo(["Tech Conference 2024", "Food Fair"]);
    }

    #endregion

    #region Filter Preservation Tests - Ensure Non-Transformed Filters Are Not Lost

    [Fact]
    public async Task ApplyQueryFilter_MixedFilters_NonTransformableFieldsPreserved()
    {
        // Arrange
        // This test verifies that when both Name (no transform) and CreatedOnTimestamp (has transform) 
        // filters are applied, the Name filter is NOT lost or overwritten
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    // Name is NOT transformed - should be preserved
                    new FilterCondition("Name", FilterOperator.Contains, "Conference"),
                    // CreatedOnTimestamp IS transformed (DateTime -> long)
                    new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThan, "2024-01-01T00:00:00Z")
                ]
            }
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        // Only "Tech Conference 2024" should match both conditions
        result.ListData.Should().HaveCount(1);
        result.ListData.First().Name.Should().Be("Tech Conference 2024");
        result.ListData.First().Name.Should().Contain("Conference"); // Verify Name filter worked
    }

    [Fact]
    public async Task ApplyQueryFilter_MultipleNonTransformableFilters_AllPreserved()
    {
        // Arrange
        // Test with multiple non-transformed fields: Name, IsActive, Location
        // along with one transformed field: CreatedOnTimestamp
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("IsActive", FilterOperator.IsEqualTo, true),
                    new FilterCondition("Location", FilterOperator.IsEqualTo, "New York"),
                    new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThan, "2024-01-01T00:00:00Z")
                ]
            }
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().HaveCount(1);
        result.ListData.First().Name.Should().Be("Tech Conference 2024");
        result.ListData.First().IsActive.Should().BeTrue();
        result.ListData.First().Location.Should().Be("New York");
    }

    [Fact]
    public async Task ApplyQueryFilter_OnlyNonTransformableFilters_WorksNormally()
    {
        // Arrange
        // Test that filtering without transformed fields still works correctly on IFilterTransformable entity
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition("IsActive", FilterOperator.IsEqualTo, true),
                    new FilterCondition("Name", FilterOperator.Contains, "Festival")
                ]
            }
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().HaveCount(1);
        result.ListData.First().Name.Should().Be("Music Festival");
    }

    [Fact]
    public async Task ApplyQueryFilter_OrLogicWithMixedFilters_PreservesAllConditions()
    {
        // Arrange
        // Test OR logic with mixed transformed and non-transformed fields
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.Or,
                Filters =
                [
                    new FilterCondition("Name", FilterOperator.Contains, "Art"),
                    new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThan, "2024-08-01T00:00:00Z")
                ]
            },
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        // "Art Exhibition" (matches Name) OR Event 4 "Sports Championship" (2024-09-05, matches timestamp)
        result.ListData.Should().HaveCount(2);
        result.ListData.Select(e => e.Name).Should().BeEquivalentTo(["Art Exhibition", "Sports Championship"]);
    }

    [Fact]
    public async Task ApplyQueryFilter_NestedLogicWithMixedFilters_PreservesAllConditions()
    {
        // Arrange
        // Complex nested logic: (Name contains "Festival" OR Location = "Paris") AND CreatedOnTimestamp > 2024-01-01
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    new FilterCondition
                    {
                        Logic = FilterConstant.Or,
                        Filters =
                        [
                            new FilterCondition("Name", FilterOperator.Contains, "Festival"),
                            new FilterCondition("Location", FilterOperator.IsEqualTo, "Paris")
                        ]
                    },
                    new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThan, "2024-01-01T00:00:00Z")
                ]
            },
            Sort = [new SortDescriptor("Id", ListSortDirection.Ascending)]
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        // Matches: Music Festival (Name match, after Jan 2024) and Art Exhibition (Paris, after Jan 2024)
        result.ListData.Should().HaveCount(2);
        result.ListData.Select(e => e.Name).Should().BeEquivalentTo(["Music Festival", "Art Exhibition"]);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ApplyQueryFilter_TransformableFieldWithInvalidDateString_HandlesGracefully()
    {
        // Arrange
        // Invalid date string should be handled gracefully (original condition returned)
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThan, "invalid-date")
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        // Should return empty or all results depending on how invalid value is handled
        // The important thing is no exception is thrown
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyQueryFilter_LongValueDirectly_WorksWithoutTransformation()
    {
        // Arrange
        // If a long value is passed directly, it should work without transformation
        await using var context = Fixture.CreateContext();
        var targetTimestamp = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition("CreatedOnTimestamp", FilterOperator.IsEqualTo, targetTimestamp)
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        result.ListData.Should().HaveCount(1);
        result.ListData.First().Name.Should().Be("Tech Conference 2024");
    }

    #endregion

    #region Critical Test - Transformed and Non-Transformed Filters Together

    /// <summary>
    /// CRITICAL TEST: Verifies that when we have both:
    /// - A filter on a field WITH transformation (CreatedOnTimestamp returns List when DateTime string)
    /// - A filter on a field WITHOUT transformation (Name returns null)
    /// Both filters are correctly applied and neither is lost.
    /// 
    /// This test specifically validates that returning null from GetTransformedFilters
    /// correctly falls back to using the original condition.
    /// </summary>
    [Fact]
    public async Task ApplyQueryFilter_TransformedAndNonTransformedFilters_BothWork()
    {
        // Arrange
        // Data setup:
        // Event 1: Name="Tech Conference 2024", CreatedOnTimestamp=2024-03-15 10:00
        // Event 2: Name="Music Festival", CreatedOnTimestamp=2024-06-20 14:00
        // 
        // We want to filter:
        // - Name contains "Festival" (returns null from GetTransformedFilters - no transform)
        // - CreatedOnTimestamp > 2024-05-01 (returns List with transformed long value)
        //
        // Only Event 2 "Music Festival" matches BOTH conditions
        
        await using var context = Fixture.CreateContext();
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Logic = FilterConstant.And,
                Filters =
                [
                    // This field returns NULL from GetTransformedFilters - must use original condition
                    new FilterCondition("Name", FilterOperator.Contains, "Festival"),
                    // This field returns List<FilterCondition> - uses transformed condition  
                    new FilterCondition("CreatedOnTimestamp", FilterOperator.IsGreaterThan, "2024-05-01T00:00:00Z")
                ]
            }
        };

        // Act
        var result = await context.Events.ToListViewResponseAsync(request);

        // Assert
        // If the Name filter was lost, we'd get Event 2 AND Event 4 (Sports Championship 2024-09-05)
        // If the CreatedOnTimestamp filter was lost, we'd get Event 2 AND Event 5 (Food Fair - also has "Fair" but we filter "Festival")
        // Only Event 2 "Music Festival" (2024-06-20) matches BOTH conditions
        result.ListData.Should().HaveCount(1, "Both filters (Name and CreatedOnTimestamp) should be applied");
        result.ListData.First().Name.Should().Be("Music Festival", "Name filter should work (returns null - no transform)");
        
        // Verify the timestamp is actually after our filter date
        var eventTimestamp = result.ListData.First().CreatedOnTimestamp;
        var filterTimestamp = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        eventTimestamp.Should().BeGreaterThan(filterTimestamp, "CreatedOnTimestamp filter should work (returns transformed List)");
    }

    #endregion
}

/// <summary>
/// SQL Server tests for IFilterTransformable
/// </summary>
[Collection("SqlServer")]
public class SqlServerFilterTransformableTests(SqlServerFixture fixture) 
    : FilterTransformableTestsBase<SqlServerFixture>(fixture);

/// <summary>
/// PostgreSQL tests for IFilterTransformable
/// </summary>
[Collection("PostgreSql")]
public class PostgreSqlFilterTransformableTests(PostgreSqlFixture fixture) 
    : FilterTransformableTestsBase<PostgreSqlFixture>(fixture);
