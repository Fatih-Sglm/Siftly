namespace EfCore.Querying.Tests.Integration.Entities;

/// <summary>
/// Test entity representing a product
/// </summary>
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

    // Navigation properties for collection filtering tests
    public ICollection<ProductTag> Tags { get; set; }
    public ICollection<ProductCategory> ProductCategories { get; set; }
}

/// <summary>
/// Product status enum for enum filtering tests
/// </summary>
public enum ProductStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

/// <summary>
/// Tag entity for one-to-many collection filtering tests
/// </summary>
public class ProductTag
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
}

/// <summary>
/// Category entity for many-to-many relationship tests
/// </summary>
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

/// <summary>
/// Join entity for many-to-many relationship between Product and Category
/// </summary>
public class ProductCategory
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public Product Product { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

/// <summary>
/// Customer entity for additional filter tests
/// </summary>
public class Customer
{
    public Customer()
    {
        Orders = [];
    }

    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public DateTime RegistrationDate { get; set; }
    public ICollection<Order> Orders { get; set; }
}

/// <summary>
/// Order entity for nested property filtering tests
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public Customer Customer { get; set; } = null!;
}

/// <summary>
/// Order status enum
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
