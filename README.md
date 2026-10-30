# Siftly

[![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)
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

## ⚡ Performance Tips

Siftly caches reflection and property metadata so that building a dynamic filter adds little on top of the equivalent hand-written LINQ query. See [Benchmarks](#-benchmarks) for measured numbers.

- **Case Sensitivity**: By default, `CaseSensitiveFilter` is set to `true`. This provides the best performance as it avoids injecting `.ToLower()` into queries, which can cause full table scans in some databases. 
- **SQL Server Support**: If you are using SQL Server with a case-insensitive collation, you should enable `DisableAutomaticToLower` in your configuration to preserve index performance even when `CaseSensitiveFilter` is false:
  ```csharp
  builder.Services.AddQueryFilter(options => {
      options.DisableAutomaticToLower = true;
  });
  ```
- **Caching**: Siftly automatically caches `PropertyInfo` chains and generic methods, ensuring that query building overhead is near-zero after the first execution.

## 📊 Benchmarks

Each Siftly filter is compared with the equivalent hand-written EF Core `Where` query. Both sides return the same rows (verified at setup). The numbers cover query building, SQL translation and execution against a real PostgreSQL 16 database (Testcontainers, 1,000 seeded rows, no-tracking queries) on .NET 10.

Regenerate this section with `./benchmark-readme.ps1` (requires Docker; pass `-BenchmarkArgs "--job","short"` for a quick run).

<!-- BENCHMARK:START -->
```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
AMD Ryzen 9 8945HX with Radeon Graphics 2.50GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Toolchain=InProcessEmitToolchain  

```
| Method                                  | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| RawEfCore_AttributeEquivalent           | 565.3 μs | 11.04 μs | 11.34 μs |  1.00 |    0.03 |       - |      - |   9.12 KB |        1.00 |
| Siftly_AttributeSearch                  | 562.1 μs | 11.12 μs | 14.06 μs |  0.99 |    0.03 |       - |      - |   7.26 KB |        0.80 |
| RawEfCore_ComplexEquivalent_Sensitive   | 729.9 μs | 13.81 μs | 13.57 μs |  1.29 |    0.03 |       - |      - |  13.58 KB |        1.49 |
| RawEfCore_ComplexEquivalent_Insensitive | 890.9 μs | 17.26 μs | 16.15 μs |  1.58 |    0.04 |  0.9766 |      - |  18.04 KB |        1.98 |
| Siftly_ComplexSearch_Sensitive          | 752.7 μs | 14.86 μs | 13.90 μs |  1.33 |    0.04 |       - |      - |   9.91 KB |        1.09 |
| Siftly_ComplexSearch_Insensitive        | 831.9 μs | 12.70 μs | 11.25 μs |  1.47 |    0.04 |       - |      - |  10.11 KB |        1.11 |
| RawEfCore_InterfaceEquivalent           | 699.2 μs |  6.95 μs |  6.50 μs |  1.24 |    0.03 | 17.5781 | 2.9297 |  287.8 KB |       31.56 |
| Siftly_InterfaceSearch                  | 723.2 μs | 14.40 μs | 20.65 μs |  1.28 |    0.04 | 16.6016 | 2.9297 | 286.62 KB |       31.43 |
<!-- BENCHMARK:END -->

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
| .NET 10.0 | 10.0.x |

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

**Made with ❤️ for the .NET Community**
