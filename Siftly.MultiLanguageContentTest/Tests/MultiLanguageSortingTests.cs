namespace Siftly.MultiLanguageContentTest.Tests;

public class MultiLanguageSortingTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private MultiLangDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Setup QueryFilter with both filter and sort builders
        var services = new ServiceCollection();
        services.AddQueryFilter(options => 
        {
            options.RegisterTypeBuilder(new MultiLanguageExpressionBuilder());
            options.RegisterSortBuilder(new MultiLanguageSortExpressionBuilder("en")); // Default fallback: English
            options.MaxPageSize = 100;
        });

        var dbOptions = new DbContextOptionsBuilder<MultiLangDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        _context = new MultiLangDbContext(dbOptions);
        await _context.Database.EnsureCreatedAsync();

        await SeedData();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    private async Task SeedData()
    {
        var article1 = new Article();
        article1.Name.Add("en", "Zebra Article");
        article1.Name.Add("tr", "Zebra Makale");

        var article2 = new Article();
        article2.Name.Add("en", "Apple Article");
        article2.Name.Add("tr", "Elma Makale");

        var article3 = new Article();
        article3.Name.Add("en", "Banana Article");
        article3.Name.Add("tr", "Muz Makale");

        _context.Articles.AddRange(article1, article2, article3);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Sort_ByMultiLanguageField_WithEnglish_ShouldSortByEnglishValue()
    {
        // ARRANGE - Using MultiLangSortDescriptor with explicit language
        var request = new QueryFilterRequest
        {
            Sort =
            [
                new MultiLangSortDescriptor { Field = "Name", Dir = ListSortDirection.Ascending, LanguageCode = "en" }
            ]
        };

        // ACT
        var result = await _context.Articles.ToListViewResponseAsync(request);

        // ASSERT
        result.ListData.Should().HaveCount(3);
        // Sorted by English: Apple, Banana, Zebra
        result.ListData[0].Name.Content.First(c => c.Language == "en").Value.Should().Be("Apple Article");
        result.ListData[1].Name.Content.First(c => c.Language == "en").Value.Should().Be("Banana Article");
        result.ListData[2].Name.Content.First(c => c.Language == "en").Value.Should().Be("Zebra Article");
    }

    [Fact]
    public async Task Sort_ByMultiLanguageField_WithTurkish_ShouldSortByTurkishValue()
    {
        // ARRANGE - Using MultiLangSortDescriptor with Turkish language
        var request = new QueryFilterRequest
        {
            Sort =
            [
                new MultiLangSortDescriptor { Field = "Name", Dir = ListSortDirection.Ascending, LanguageCode = "tr" }
            ]
        };

        // ACT
        var result = await _context.Articles.ToListViewResponseAsync(request);

        // ASSERT
        result.ListData.Should().HaveCount(3);
        // Sorted by Turkish: Elma (Apple), Muz (Banana), Zebra
        result.ListData[0].Name.Content.First(c => c.Language == "tr").Value.Should().Be("Elma Makale");
        result.ListData[1].Name.Content.First(c => c.Language == "tr").Value.Should().Be("Muz Makale");
        result.ListData[2].Name.Content.First(c => c.Language == "tr").Value.Should().Be("Zebra Makale");
    }

    [Fact]
    public async Task Sort_ByMultiLanguageField_Descending_ShouldSortDescending()
    {
        // ARRANGE
        var request = new QueryFilterRequest
        {
            Sort =
            [
                new MultiLangSortDescriptor { Field = "Name", Dir = ListSortDirection.Descending, LanguageCode = "en" }
            ]
        };

        // ACT
        var result = await _context.Articles.ToListViewResponseAsync(request);

        // ASSERT
        result.ListData.Should().HaveCount(3);
        // Sorted by English descending: Zebra, Banana, Apple
        result.ListData[0].Name.Content.First(c => c.Language == "en").Value.Should().Be("Zebra Article");
        result.ListData[1].Name.Content.First(c => c.Language == "en").Value.Should().Be("Banana Article");
        result.ListData[2].Name.Content.First(c => c.Language == "en").Value.Should().Be("Apple Article");
    }

    [Fact]
    public async Task Sort_WithFilterAndPagination_ShouldWorkTogether()
    {
        // ARRANGE
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Field = "Name",
                Operator = FilterOperator.Contains,
                Value = "Article"
            },
            Sort =
            [
                new MultiLangSortDescriptor { Field = "Name", Dir = ListSortDirection.Ascending, LanguageCode = "en" }
            ],
            PageSize = 2,
            IncludeCount = true
        };

        // ACT
        var result = await _context.Articles.ToListViewResponseAsync(request);

        // ASSERT
        result.TotalCount.Should().Be(3); // 3 articles match
        result.ListData.Should().HaveCount(2); // Only take 2
        result.ListData[0].Name.Content.First(c => c.Language == "en").Value.Should().Be("Apple Article");
        result.ListData[1].Name.Content.First(c => c.Language == "en").Value.Should().Be("Banana Article");
    }
}
