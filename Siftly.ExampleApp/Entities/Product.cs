using Siftly.Core;

namespace Siftly.ExampleApp.Entities;

public class Product
{
    public Product()
    {
        Tags = [];
        ProductCategories = [];
    }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? StockCount { get; set; }
    public ProductStatus Status { get; set; }

    [FilterTransform("CategoryId", IsManyToMany = true, JoinProperty = "Category", ItemField = "Id")]
    public ICollection<ProductCategory> ProductCategories { get; set; }

    [FilterTransform("TagName", IsCollection = true, ItemField = "Name")]
    public ICollection<ProductTag> Tags { get; set; }
}

public enum ProductStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public class ProductTag
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
}

public class Category
{
    public Category()
    {
        ProductCategories = [];
    }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ProductCategory> ProductCategories { get; set; }
}

public class ProductCategory
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public Product Product { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
