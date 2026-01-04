namespace Siftly.MultiLanguageContentTest.Tests;

public class MultiLanguageFilteringTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private MultiLangDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Use IoC to register and configure QueryFilter
        var services = new ServiceCollection();
        services.AddQueryFilter(options => 
        {
            options.RegisterTypeBuilder(new MultiLanguageExpressionBuilder());
            options.MaxPageSize = 100;
        });
        
        var serviceProvider = services.BuildServiceProvider();

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
        var topicTech = new Topic();
        topicTech.Title.Add("tr", "Teknoloji");
        topicTech.Title.Add("en", "Technology");

        var topicAi = new Topic();
        topicAi.Title.Add("tr", "Yapay Zeka");
        topicAi.Title.Add("en", "Artificial Intelligence");

        var article1 = new Article();
        article1.Name.Add("tr", "EF Core 10 Yenilikleri");
        article1.Name.Add("en", "EF Core 10 Features");
        article1.Description = new MultiLanguageContent();
        article1.Description.Add("tr", "Büyük bir güncelleme geliyor");
        
        var tagDb = new ArticleTag();
        tagDb.TagName.Add("tr", "Veritabanı");
        tagDb.TagName.Add("en", "Database");
        article1.Tags.Add(tagDb);
        article1.ArticleTopics.Add(new ArticleTopic { Topic = topicTech });

        var article2 = new Article();
        article2.Name.Add("tr", "C# 14 Ön İzleme");
        article2.Name.Add("en", "C# 14 Preview");
        
        var tagLang = new ArticleTag();
        tagLang.TagName.Add("tr", "Programlama");
        tagLang.TagName.Add("en", "Programming");
        article2.Tags.Add(tagLang);
        article2.ArticleTopics.Add(new ArticleTopic { Topic = topicTech });
        article2.ArticleTopics.Add(new ArticleTopic { Topic = topicAi });

        _context.Articles.AddRange(article1, article2);
        _context.Topics.AddRange(topicTech, topicAi);
        await _context.SaveChangesAsync();
    }

    #region Basic String Operators

    [Fact]
    public async Task Root_StartsWith_ReturnsMatching()
    {
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Field = "Name",
                Operator = FilterOperator.StartsWith,
                Value = "EF Core"
            }
        };

        var result = await _context.Articles.ApplyQueryFilterAsync(request);
        result.ListData.Should().ContainSingle().Which.Name.Content.Any(c => c.Value.StartsWith("EF Core")).Should().BeTrue();
    }

    [Fact]
    public async Task Root_IsEqualTo_ReturnsMatching()
    {
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Field = "Name",
                Operator = FilterOperator.IsEqualTo,
                Value = "C# 14 Ön İzleme"
            }
        };

        var result = await _context.Articles.ApplyQueryFilterAsync(request);
        result.ListData.Should().ContainSingle().Which.Name.Content.Any(c => c.Value == "C# 14 Ön İzleme").Should().BeTrue();
    }

    #endregion

    #region Collection & Many-to-Many

    [Fact]
    public async Task OneToMany_Contains_ReturnsCorrectArticle()
    {
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Field = "_collection_:Tags:TagName",
                Operator = FilterOperator.Contains,
                Value = "Programlama"
            }
        };

        var result = await _context.Articles.Include(a => a.Tags).ApplyQueryFilterAsync(request);
        result.ListData.Should().ContainSingle().Which.Tags.Any(t => t.TagName.Content.Any(c => c.Value == "Programlama")).Should().BeTrue();
    }

    [Fact]
    public async Task ManyToMany_IsEqualTo_ReturnsMultipleArticles()
    {
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Field = "_m2m_:ArticleTopics:Topic:Title",
                Operator = FilterOperator.IsEqualTo,
                Value = "Teknoloji"
            }
        };

        var result = await _context.Articles.ApplyQueryFilterAsync(request);
        result.ListData.Should().HaveCount(2);
    }

    #endregion

    #region specialized Custom Filter (Inheritance)

    [Fact]
    public async Task EnglishSearch_WithSpecializedFilter_ReturnsResults()
    {
        var request = new QueryFilterRequest
        {
            Filter = new MultiLangFilter 
            { 
                Field = "Name", 
                Operator = FilterOperator.Contains, 
                Value = "Features",
                LanguageCode = "en"
            }
        };

        var result = await _context.Articles.ApplyQueryFilterAsync(request);
        
        // Should find Article 1 ("EF Core 10 Features") in English
        result.ListData.Should().ContainSingle();
        result.ListData.First().Name.Content.Any(c => c.Language == "en" && c.Value.Contains("Features")).Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Search_AcrossAllLanguages_WhenNoSpecializedFilterProvided()
    {
        var request = new QueryFilterRequest
        {
            Filter = new FilterCondition
            {
                Field = "Name",
                Operator = FilterOperator.Contains,
                Value = "Features"
            }
        };

        var result = await _context.Articles.ApplyQueryFilterAsync(request);
        result.ListData.Should().ContainSingle();
    }

    #endregion

    #region Transformation & Specialization

    [Fact]
    public async Task ToSpecialized_WithTemplate_ConvertsStandardFilterToMultiLang()
    {
        // 1. Gelen standart bir filtre (örneğin API'den gelen ham veri)
        var standardFilter = new FilterCondition
        {
            Field = "Name",
            Operator = FilterOperator.Contains,
            Value = "Features"
        };

        // 2. Template nesnemiz (Tüm filtre ağacına uygulanacak özel ayarlar)
        var template = new MultiLangFilter { LanguageCode = "en" };

        // 3. Dönüşüm: Standart -> Uzmanlaşmış (MultiLangFilter)
        var specializedFilter = standardFilter.ToSpecialized(template);

        // 4. Uygulama
        var request = new QueryFilterRequest { Filter = specializedFilter };
        var result = await _context.Articles.ApplyQueryFilterAsync(request);

        // Doğrulama
        specializedFilter.Should().BeOfType<MultiLangFilter>();
        specializedFilter.Field.Should().Be("Name"); // Orijinal alan korunmalı
        ((MultiLangFilter)specializedFilter).LanguageCode.Should().Be("en"); // Template'den özellik gelmeli
        
        result.ListData.Should().ContainSingle();
        result.ListData.First().Name.Content.Any(c => c.Language == "en" && c.Value.Contains("Features")).Should().BeTrue();
    }

    #endregion
}
