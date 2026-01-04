# Siftly

[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Siftly.Core.svg)](https://www.nuget.org/packages/Siftly.Core/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Siftly** is a powerful, type-safe query filtering and dynamic querying library for .NET applications. It provides a clean, fluent API for building complex database queries with filtering, sorting, and pagination support, specifically designed for Entity Framework Core.

## 🚀 Features

- ✅ **Dynamic Filtering** - Build complex filters with multiple operators and conditions
- ✅ **Type-Safe Queries** - Strongly-typed query building with compile-time safety
- ✅ **EF Core Integration** - Seamless integration with Entity Framework Core (8.0, 9.0, 10.0+)
- ✅ **Multi-Target Support** - Compatible with .NET 8.0, .NET 9.0, and .NET 10.0
- ✅ **Advanced Operators** - 17+ filter operators including string, numeric, and null checks
- ✅ **Keyset Pagination** - Cursor-based pagination for efficient large dataset handling
- ✅ **Composite Filters** - Support for AND/OR logic with nested conditions
- ✅ **Sorting** - Multi-column sorting with ascending/descending order
- ✅ **Custom Type Builders** - `ITypeExpressionBuilder<T>` for filtering and `ISortExpressionBuilder<T>` for sorting
- ✅ **Multi-Language Support** - Built-in support for multilingual content with runtime culture selection
- ✅ **HttpContext Extensions** - Easy query parameter parsing from HTTP requests
- ✅ **Central Package Management** - Modern MSBuild package versioning
- ✅ **Kendo UI Compatible** - Works seamlessly with Kendo UI DataSource

## 📦 Installation

### NuGet Package Manager

```bash
dotnet add package Siftly.Core
dotnet add package Siftly.EntityFramework
```

### Package Manager Console

```powershell
Install-Package Siftly.Core
Install-Package Siftly.EntityFramework
```

## 🏗️ Project Structure

```
Siftly/
├── Siftly.Core/                          # Core filtering library
│   ├── Configuration/                    # QueryFilter options and setup
│   ├── Extensions/                       # Extension methods (HttpContext, etc.)
│   ├── Infrastructure/Services/          # Filter and sorting expression builders
│   ├── Interfaces/                       # ITypeExpressionBuilder, ISortExpressionBuilder
│   └── Models/                           # Request/Response models
│       ├── Filters/                      # FilterCondition, FilterOperator
│       ├── Sorting/                      # SortDescriptor
│       ├── Requests/                     # QueryFilterRequest
│       └── Responses/                    # ListViewResponse
├── Siftly.EntityFramework/               # EF Core integration
│   └── Extensions/                       # QueryFilterExtensions, ToListViewResponseExtensions
└── Tests/                                # Test projects
    ├── Siftly.IntegrationTest/          # Integration tests (SQL Server, PostgreSQL, InMemory)
    └── Siftly.MultiLanguageContentTest/ # Multi-language support tests
```

## 🎯 Quick Start

### 1. Configure Services

```csharp
using Siftly.Core;

// In Program.cs or Startup.cs
builder.Services.AddQueryFilter(options =>
{
    options.MaxPageSize = 100;
    
    // Register custom type builders (optional)
    options.RegisterTypeBuilder(new MultiLanguageExpressionBuilder());
    options.RegisterSortBuilder(new MultiLanguageSortExpressionBuilder("tr"));
});
```

### 2. Basic Usage - ToListViewResponseAsync

```csharp
using Siftly.Core;
using Siftly.EntityFramework;

public class ProductService
{
    private readonly DbContext _context;

    public async Task<ListViewResponse<Product>> GetProducts(QueryFilterRequest request)
    {
        return await _context.Products.ToListViewResponseAsync(request);
    }
}
```

### 3. With Projection (Select)

```csharp
public async Task<ListViewResponse<ProductDto>> GetProductsWithProjection(QueryFilterRequest request)
{
    return await _context.Products.ToListViewResponseAsync(
        request,
        p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        });
}
```

### 4. Modular Query Building

For more control, use separate methods:

```csharp
// Step 1: Apply filters only (returns IQueryable)
var filteredQuery = _context.Products.ApplyFilters(request);

// Step 2: Apply sorting and pagination
var pagedQuery = filteredQuery.ApplySortingAndPagination(request);

// Step 3: Materialize
var result = await pagedQuery.ToListAsync();

// Or all-in-one (returns IQueryable):
var query = _context.Products.ApplyQueryFilter(request);
```

## 📝 Request Model

### QueryFilterRequest

```csharp
public class QueryFilterRequest
{
    public int PageSize { get; set; } = 20;        // Items per page
    public int PageNumber { get; set; } = 0;       // Page offset (0-based)
    public List<SortDescriptor>? Sort { get; set; } 
    public FilterCondition? Filter { get; set; }
    public FilterCondition? Cursor { get; set; }   // For keyset pagination
    public bool IncludeCount { get; set; } = true;
}
```

### JSON Request Examples

#### Simple Filter

```json
{
  "filter": {
    "field": "Name",
    "operator": "Contains",
    "value": "Laptop"
  },
  "sort": [
    { "field": "Price", "dir": "Desc" }
  ],
  "pageSize": 20,
  "pageNumber": 0
}
```

#### Composite Filter (AND/OR Logic)

```json
{
  "filter": {
    "logic": "And",
    "filters": [
      { "field": "Category", "operator": "IsEqualTo", "value": "Electronics" },
      {
        "logic": "Or",
        "filters": [
          { "field": "Price", "operator": "IsLessThan", "value": 1000 },
          { "field": "OnSale", "operator": "IsEqualTo", "value": true }
        ]
      }
    ]
  }
}
```

## 📚 Filter Operators

### Comparison Operators
- `IsEqualTo`, `IsNotEqualTo`
- `IsLessThan`, `IsLessThanOrEqualTo`
- `IsGreaterThan`, `IsGreaterThanOrEqualTo`

### String Operators
- `StartsWith`, `EndsWith`, `Contains`, `DoesNotContain`

### Null/Empty Checks
- `IsNull`, `IsNotNull`
- `IsEmpty`, `IsNotEmpty`
- `IsNullOrEmpty`, `IsNotNullOrEmpty`

### Collection Operators
- `In`, `IsContainedIn`

## 🎨 ASP.NET Core Integration

### HttpContext Extensions

Extract `QueryFilterRequest` from query string parameters:

```csharp
[HttpGet]
public async Task<ActionResult<ListViewResponse<Product>>> Get()
{
    var request = HttpContext.GetQueryFilter(
        defaultPageSize: 20,
        maxPageSize: 100
    );
    
    return Ok(await _service.GetProductsAsync(request));
}
```

Supported query parameters:
- `pageSize` or `take` - Items per page
- `pageNumber` or `skip` - Page offset
- `sort` - JSON array or `field:dir` format
- `filter` - JSON filter object
- `includeCount` - Whether to include total count

### From Request Body (POST)

```csharp
[HttpPost("query")]
public async Task<ActionResult<ListViewResponse<ProductDto>>> Query()
{
    var request = await HttpContext.GetQueryFilterFromBodyAsync();
    return Ok(await _service.GetProductsAsync(request));
}
```

## 🌍 Multi-Language Content Support

Siftly provides custom expression builders for multi-language content stored as JSON.

### Custom Filter and Sort Descriptors

```csharp
// Filter with specific language
var filter = new MultiLangFilter
{
    Field = "Name",
    Operator = FilterOperator.Contains,
    Value = "Laptop",
    LanguageCode = "tr"  // Filter by Turkish content
};

// Sort with specific language
var sort = new MultiLangSortDescriptor
{
    Field = "Name",
    Dir = ListSortDirection.Ascending,
    LanguageCode = "en"  // Sort by English content
};
```

### Register Custom Builders

```csharp
services.AddQueryFilter(options =>
{
    // For filtering MultiLanguageContent
    options.RegisterTypeBuilder(new MultiLanguageExpressionBuilder());
    
    // For sorting MultiLanguageContent (with default fallback language)
    options.RegisterSortBuilder(new MultiLanguageSortExpressionBuilder("tr"));
});
```

### Implementing Custom Builders

```csharp
// Filter builder
public class MultiLanguageExpressionBuilder : ITypeExpressionBuilder<MultiLanguageContent>
{
    public Expression? BuildExpression(Expression propertyAccess, FilterCondition condition)
    {
        string? languageCode = condition is MultiLangFilter mlf ? mlf.LanguageCode : null;
        // Build expression for filtering by specific language...
    }
}

// Sort builder
public class MultiLanguageSortExpressionBuilder : ISortExpressionBuilder<MultiLanguageContent>
{
    public Expression? BuildSortExpression(Expression propertyAccess, SortDescriptor sortDescriptor)
    {
        string languageCode = sortDescriptor is MultiLangSortDescriptor mlsd 
            ? mlsd.LanguageCode 
            : _defaultLanguage;
        // Build expression for sorting by specific language...
    }
}
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Integration tests (SQL Server, PostgreSQL, InMemory)
dotnet test Siftly.IntegrationTest/Siftly.IntegrationTest.csproj

# Multi-language content tests
dotnet test Siftly.MultiLanguageContentTest/Siftly.MultiLanguageContentTest.csproj

# Specific framework
dotnet test --framework net8.0
```

## 📖 Entity Framework Core Version Support

| Target Framework | EF Core Version |
|-----------------|----------------|
| .NET 8.0        | 8.0.22         |
| .NET 9.0        | 9.0.11         |
| .NET 10.0       | 10.0.1         |

## 🔧 Local Development

Build and pack local NuGet packages:

```bash
./repack-local.ps1 -Version 1.0.0
```

This script:
1. Builds release packages
2. Clears NuGet cache
3. Outputs packages to `./nupkgs`

Then restore in your test project:
```bash
dotnet restore --force-evaluate
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by Kendo UI DataSource filtering capabilities
- Built with modern .NET best practices
- Designed for high-performance query operations

---

**Made with ❤️ for the .NET Community**
