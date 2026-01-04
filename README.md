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
- ✅ **Extension Points** - Custom type expression builders for specialized scenarios
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
│   ├── Abstractions/                     # Interfaces and contracts
│   ├── Builders/                         # Fluent API builders
│   ├── Extensions/                       # Extension methods
│   ├── Infrastructure/                   # Core services
│   │   ├── Services/                     # Filter and sorting expression builders
│   │   ├── Converters/                   # JSON converters
│   │   └── Binders/                      # Model binders
│   └── Models/                           # Request/Response models
│       ├── Filters/                      # Filter models
│       ├── Sorting/                      # Sort descriptors
│       ├── Requests/                     # Query request models
│       └── Responses/                    # Response models
├── Siftly.EntityFramework/               # EF Core integration
└── Tests/                                # Test projects
    ├── Siftly.IntegrationTest/          # Integration tests
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
    options.DefaultPageSize = 20;
});
```

### 2. Basic Filtering

```csharp
using Siftly.Core;
using Siftly.EntityFramework;

public class ProductService
{
    private readonly DbContext _context;

    public async Task<ListViewResponse<Product>> GetProducts(QueryFilterRequest request)
    {
        return await _context.Products
            .ApplyQueryFilterAsync(request);
    }
}
```

### 3. Filter Request Examples

#### Simple Filter (Single Condition)

```json
{
  "filter": {
    "field": "Name",
    "operator": "Contains",
    "value": "Laptop"
  },
  "sort": [
    {
      "field": "Price",
      "dir": "Desc"
    }
  ],
  "take": 20,
  "skip": 0
}
```

#### Composite Filter (AND/OR Logic)

```json
{
  "filter": {
    "logic": "And",
    "filters": [
      {
        "field": "Category",
        "operator": "IsEqualTo",
        "value": "Electronics"
      },
      {
        "logic": "Or",
        "filters": [
          {
            "field": "Price",
            "operator": "IsLessThan",
            "value": 1000
          },
          {
            "field": "OnSale",
            "operator": "IsEqualTo",
            "value": true
          }
        ]
      }
    ]
  }
}
```

## 📚 Filter Operators

Siftly supports the following filter operators:

### Comparison Operators
- `IsEqualTo` - Equal to
- `IsNotEqualTo` - Not equal to
- `IsLessThan` - Less than
- `IsLessThanOrEqualTo` - Less than or equal to
- `IsGreaterThan` - Greater than
- `IsGreaterThanOrEqualTo` - Greater than or equal to

### String Operators
- `StartsWith` - String starts with
- `EndsWith` - String ends with
- `Contains` - String contains
- `DoesNotContain` - String does not contain

### Null/Empty Checks
- `IsNull` - Value is null
- `IsNotNull` - Value is not null
- `IsEmpty` - String is empty
- `IsNotEmpty` - String is not empty
- `IsNullOrEmpty` - String is null or empty
- `IsNotNullOrEmpty` - String is not null or empty

### Collection Operators
- `In` - Value is in collection
- `IsContainedIn` - Item is contained in collection

## 🔧 Advanced Features

### Projection (Select)

```csharp
public async Task<ListViewResponse<ProductDto>> GetProductsWithProjection(
    QueryFilterRequest request)
{
    return await _context.Products
        .ApplyQueryFilterAsync(
            request,
            p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            });
}
```

### Keyset Pagination (Cursor-based)

```csharp
{
  "cursor": {
    "field": "Id",
    "operator": "IsGreaterThan",
    "value": 100
  },
  "sort": [
    {
      "field": "Id",
      "dir": "Asc"
    }
  ],
  "take": 20
}
```

### Custom Type Expression Builders

For complex scenarios like multi-language content or custom value objects:

```csharp
public class MultiLanguageExpressionBuilder : ITypeExpressionBuilder
{
    public bool CanHandle(Type propertyType)
    {
        return propertyType == typeof(MultiLanguageContent);
    }

    public Expression BuildExpression(
        Expression property,
        FilterOperator op,
        object? value,
        QueryFilterConfiguration options)
    {
        // Custom expression building logic
    }
}

// Register in DI
services.AddQueryFilter(options =>
{
    options.RegisterTypeExpressionBuilder<MultiLanguageExpressionBuilder>();
});
```

## 🎨 ASP.NET Core Integration

### Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    [HttpPost("query")]
    public async Task<ActionResult<ListViewResponse<ProductDto>>> Query(
        [FromBody] QueryFilterRequest request)
    {
        var result = await _service.GetProductsAsync(request);
        return Ok(result);
    }
}
```

### Model Binding

Siftly includes custom model binders for query parameters:

```csharp
[HttpGet]
public async Task<ActionResult<ListViewResponse<Product>>> Get(
    [FromQuery] QueryFilterRequest request)
{
    // Automatically binds from query string
    return Ok(await _service.GetProductsAsync(request));
}
```

## 🧪 Testing

The project includes comprehensive test suites:

### Integration Tests
```bash
dotnet test Siftly.IntegrationTest/Siftly.IntegrationTest.csproj
```

### Multi-Language Content Tests
```bash
dotnet test Siftly.MultiLanguageContentTest/Siftly.MultiLanguageContentTest.csproj
```

## 🏗️ Build Configuration

Siftly uses **Central Package Management** (CPM) for consistent package versioning across all projects:

- `Directory.Build.props` - Shared MSBuild properties
- `Directory.Packages.props` - Centralized package versions
- Multi-target support: net8.0, net9.0, net10.0

### Build the Solution

```bash
dotnet restore
dotnet build
```

## 📖 Entity Framework Core Version Support

| Target Framework | EF Core Version |
|-----------------|----------------|
| .NET 8.0        | 8.0.22         |
| .NET 9.0        | 9.0.11         |
| .NET 10.0       | 10.0.1         |

## 🌍 Multi-Language Support

Siftly includes built-in support for multi-language content through custom expression builders. Perfect for applications with localized data stored as JSON in the database.

```csharp
public class MultiLanguageContent
{
    public List<LangContentDto> Content { get; set; } = [];
}

// EF Core 10+ uses ComplexProperty
// EF Core 8/9 uses OwnsOne with ToJson()
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

## 📞 Support

For issues, questions, or contributions, please open an issue on GitHub.

---

**Made with ❤️ for the .NET Community**
