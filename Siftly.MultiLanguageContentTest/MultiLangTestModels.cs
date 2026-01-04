namespace Siftly.MultiLanguageContentTest;

// --- Models ---

public class LangContentDto
{
    public string Language { get; set; } = null!;
    public string Value { get; set; } = null!;
}

public class MultiLanguageContent
{
    public List<LangContentDto> Content { get; set; } = [];
    public void Add(string lang, string value) => Content.Add(new LangContentDto { Language = lang, Value = value });
}

public class Article
{
    public int Id { get; set; }
    public MultiLanguageContent Name { get; set; } = new();
    public MultiLanguageContent? Description { get; set; }

    public ICollection<ArticleTag> Tags { get; set; } = new List<ArticleTag>();
    public ICollection<ArticleTopic> ArticleTopics { get; set; } = new List<ArticleTopic>();
}

public class ArticleTag
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public MultiLanguageContent TagName { get; set; } = new();
}

public class Topic
{
    public int Id { get; set; }
    public MultiLanguageContent Title { get; set; } = new();
    public ICollection<ArticleTopic> ArticleTopics { get; set; } = new List<ArticleTopic>();
}

public class ArticleTopic
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}

// --- DbContext ---

public class MultiLangDbContext(DbContextOptions<MultiLangDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Topic> Topics => Set<Topic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.Id);
            ConfigureMultiLang(modelBuilder, nameof(Article.Name), typeof(Article));
            ConfigureMultiLang(modelBuilder, nameof(Article.Description), typeof(Article));
        });

        modelBuilder.Entity<ArticleTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            ConfigureMultiLang(modelBuilder, nameof(ArticleTag.TagName), typeof(ArticleTag));
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.Id);
            ConfigureMultiLang(modelBuilder, nameof(Topic.Title), typeof(Topic));
        });

        modelBuilder.Entity<ArticleTopic>(entity =>
        {
            entity.HasKey(et => new { et.ArticleId, et.TopicId });
        });
    }

    private void ConfigureMultiLang(ModelBuilder modelBuilder, string propertyName, Type entityType)
    {
        var entityBuilder = modelBuilder.Entity(entityType);

#if NET10_0_OR_GREATER
        // EF Core 10 - Use ComplexProperty
        // Note: For primitive collections like List<DTO> to work as JSON, 
        // EF 9+ handles it automatically via ComplexProperty.ToJson()
        entityBuilder.ComplexProperty(propertyName, cp => cp.ToJson());
#else
        // EF Core 8 - Use OwnsOne (Stable for LTS)
        var method = typeof(MultiLangDbContext).GetMethod(nameof(MapOwnsOne), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.MakeGenericMethod(entityType);

        method?.Invoke(null, [modelBuilder, propertyName]);
#endif
    }

    private static void MapOwnsOne<TEntity>(ModelBuilder modelBuilder, string propertyName) where TEntity : class
    {
        modelBuilder.Entity<TEntity>().OwnsOne<MultiLanguageContent>(propertyName, nav =>
        {
            nav.ToJson();
            nav.OwnsMany(x => x.Content);
        });
    }
}
