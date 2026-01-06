# Siftly

[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Siftly.Core.svg)](https://www.nuget.org/packages/Siftly.Core/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Siftly** is a powerful, type-safe query filtering and dynamic querying library for .NET applications. It provides a clean, fluent API for building complex database queries with filtering, sorting, and pagination support, specifically designed for Entity Framework Core.

## 🚀 Features

- ✅ **Dynamic Filtering** - Build complex filters with multiple operators and conditions
- ✅ **Type-Safe Queries** - Strongly-typed query building with compile-time safety
- ✅ **Filter Transformations** - Map API fields to complex DB paths via Attributes or Interfaces
- ✅ **Automatic Collections** - Filter One-to-Many and Many-to-Many relationships automatically using dot notation
- ✅ **Keyset Pagination** - Cursor-based pagination for efficient large dataset handling
- ✅ **Composite Filters** - Support for AND/OR logic with nested conditions
- ✅ **Custom Type Builders** - Specialized builders for complex types (JSON, Multi-language)
- ✅ **HttpContext Extensions** - Zero-boilerplate query parameter parsing

## 🧩 Filter Transformations

Siftly provides two ways to transform simple API fields into complex database queries.

### 1. Declarative Mode (Attribute-based)
Perfect for simple mappings, field grouping, and value transformations without writing manual logic.

```csharp
public class Product
{
    public int Id { get; set; }

    // Maps API "search" field to both Name and Description (OR logic applied automatically)
    [FilterTransform("search")]
    public string Name { get; set; }

    [FilterTransform("search")]
    public string Description { get; set; }

    // Maps API "CategoryId" to a Many-to-Many relationship path
    [FilterTransform("CategoryId", IsManyToMany = true, JoinProperty = "Category", ItemField = "Id")]
    public ICollection<ProductCategory> ProductCategories { get; set; }

    // Maps API string to Enum with a value transformer
    [FilterTransform<StringToEnumTransformer<ProductStatus>>("Status")]
    public ProductStatus Status { get; set; }
}
```

### 2. Explicit Mode (Interface-based)
For scenarios requiring custom logic or runtime decisions, implement `IFilterTransformable`.

```csharp
public class Order : IFilterTransformable
{
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }

    public List<FilterCondition> GetTransformedFilters(FilterCondition condition)
    {
        if (condition.Field == "isRecent")
        {
            // Custom transformation: "isRecent" -> "OrderDate > Last 7 Days"
            return [new FilterCondition("OrderDate", FilterOperator.IsGreaterThan, DateTime.UtcNow.AddDays(-7))];
        }
        
        // Return original condition in a list to use standard filtering or Attribute-based mappings
        return [condition]; 
    }
}
```

## ⌨️ Keyset Pagination (Cursor)

Use Cursor-based pagination for high-performance infinite scroll or large data navigation. It avoids the performance pitfalls of `Skip`/`Take` on large datasets.

### Request Example (JSON):
```json
{
  "pageSize": 20,
  "sort": [{ "field": "Id", "dir": "Asc" }],
  "cursor": { "field": "Id", "operator": "gt", "value": 150 }
}
```

### Server Side Usage:
```csharp
// The ApplyQueryFilter method handles Cursor, Sort, Filter, and Pagination automatically
return await _context.Products.ToListViewResponseAsync(request);
```

## 🎯 Automatic Collection Filtering

You don't need any configuration to filter collections if you use standard dot notation in your field names. Siftly detects the `IEnumerable` property in the path and automatically builds `Any()` expressions.

- `Tags.Name` ➡️ `p.Tags.Any(t => t.Name == value)`
- `ProductCategories.Category.Id` ➡️ `p.ProductCategories.Any(pc => pc.Category.Id == value)`

### JSON Example:
```json
{
  "filter": {
    "field": "Tags.Name",
    "operator": "Contains",
    "value": "Premium"
  }
}
```

## 🛠️ Custom Type Builders

Extend Siftly for complex types like Multi-language JSON or specialized fields using `ITypeExpressionBuilder` and `ISortExpressionBuilder`.

### 1. Register Builders
```csharp
builder.Services.AddQueryFilter(options =>
{
    // Custom builder for MultiLanguageContent type
    options.RegisterTypeBuilder(new MultiLanguageExpressionBuilder());
    
    // Custom sorter for MultiLanguageContent (default to 'en')
    options.RegisterSortBuilder(new MultiLanguageSortExpressionBuilder("en"));
});
```

### 2. Implementation Example
```csharp
public class MultiLanguageExpressionBuilder : ITypeExpressionBuilder<MultiLanguageContent>
{
    public Expression BuildExpression(Expression propertyAccess, FilterCondition condition)
    {
        // Custom logic to build an expression that queries JSON property
        // e.g. x.Name.Translations.Any(t => t.Lang == "en" && t.Value.Contains("search"))
        return MyExpressionHelper.BuildJsonSearch(propertyAccess, condition);
    }
}
```

## 🎨 ASP.NET Core Integration

### 1. Configure Services
```csharp
builder.Services.AddQueryFilter(options =>
{
    options.DefaultPageSize = 25;
    options.MaxPageSize = 100;
});
```

### 2. Zero-Boilerplate Controller
```csharp
[HttpGet]
public async Task<ActionResult<ListViewResponse<Product>>> Get()
{
    // Automatically parses query strings: ?filter={"field":"Name","operator":"eq","value":"X"}&sort=Price:desc
    var request = HttpContext.GetQueryFilter();
    return Ok(await _context.Products.ToListViewResponseAsync(request));
}
```

## 📚 Filter Operators Reference

- **Equality**: `eq` (IsEqualTo), `neq` (IsNotEqualTo)
- **Comparison**: `lt`, `lte`, `gt`, `gte`
- **String**: `startswith`, `endswith`, `contains`, `doesnotcontain`
- **Null/Empty**: `isnull`, `isnotnull`, `isempty`, `isnotempty`, `isnullorempty`, `isnotnullorempty`
- **Collection**: `in`, `containedin`

## 📖 EF Core Version Support

| .NET Version | EF Core Version |
| :--- | :--- |
| .NET 8.0 | 8.0.x |
| .NET 9.0 | 9.0.x |
| .NET 10.0 | 10.0.x |

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

**Made with ❤️ for the .NET Community**
