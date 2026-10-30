using Siftly.ExampleApp.Entities;

namespace Siftly.ExampleApp.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        if (context.Products.Any()) return;

        // Categories
        var electronics = new Category { Id = 1, Name = "Electronics" };
        var clothing = new Category { Id = 2, Name = "Clothing" };
        var books = new Category { Id = 3, Name = "Books" };
        var sports = new Category { Id = 4, Name = "Sports" };

        context.Categories.AddRange(electronics, clothing, books, sports);

        // Products
        var products = new List<Product>
        {
            new()
            {
                Id = 1, Name = "Wireless Headphones", Description = "Premium noise-cancelling headphones",
                Price = 299.99m, IsActive = true, CreatedAt = new DateTime(2024, 1, 15),
                UpdatedAt = new DateTime(2024, 6, 1), StockCount = 150, Status = ProductStatus.Published
            },
            new()
            {
                Id = 2, Name = "Running Shoes", Description = "Lightweight running shoes for professionals",
                Price = 129.50m, IsActive = true, CreatedAt = new DateTime(2024, 2, 20),
                StockCount = 80, Status = ProductStatus.Published
            },
            new()
            {
                Id = 3, Name = "Programming Book", Description = "Advanced C# programming guide",
                Price = 49.99m, IsActive = true, CreatedAt = new DateTime(2024, 3, 10),
                StockCount = 200, Status = ProductStatus.Published
            },
            new()
            {
                Id = 4, Name = "Smart Watch", Description = null,
                Price = 399.00m, IsActive = true, CreatedAt = new DateTime(2024, 4, 5),
                UpdatedAt = new DateTime(2024, 7, 15), StockCount = 50, Status = ProductStatus.Published
            },
            new()
            {
                Id = 5, Name = "Cotton T-Shirt", Description = "Comfortable cotton t-shirt",
                Price = 24.99m, IsActive = true, CreatedAt = new DateTime(2024, 5, 1),
                StockCount = 500, Status = ProductStatus.Published
            },
            new()
            {
                Id = 6, Name = "Bluetooth Speaker", Description = "Portable waterproof speaker",
                Price = 79.99m, IsActive = false, CreatedAt = new DateTime(2024, 1, 25),
                StockCount = null, Status = ProductStatus.Archived
            },
            new()
            {
                Id = 7, Name = "Yoga Mat", Description = "Non-slip premium yoga mat",
                Price = 35.00m, IsActive = true, CreatedAt = new DateTime(2024, 6, 12),
                StockCount = 300, Status = ProductStatus.Published
            },
            new()
            {
                Id = 8, Name = "Draft Laptop", Description = "",
                Price = 999.99m, IsActive = false, CreatedAt = new DateTime(2024, 7, 1),
                StockCount = 0, Status = ProductStatus.Draft
            },
            new()
            {
                Id = 9, Name = "Winter Jacket", Description = "Warm winter jacket with hood",
                Price = 189.00m, IsActive = true, CreatedAt = new DateTime(2024, 8, 20),
                UpdatedAt = new DateTime(2024, 9, 1), StockCount = 75, Status = ProductStatus.Published
            },
            new()
            {
                Id = 10, Name = "Fitness Tracker", Description = "Advanced fitness tracking band",
                Price = 149.99m, IsActive = true, CreatedAt = new DateTime(2024, 9, 15),
                StockCount = 120, Status = ProductStatus.Published
            },
            new()
            {
                Id = 11, Name = "Archived Phone Case", Description = "Old phone case model",
                Price = 9.99m, IsActive = false, CreatedAt = new DateTime(2023, 12, 1),
                StockCount = 10, Status = ProductStatus.Archived
            },
            new()
            {
                Id = 12, Name = "Premium Backpack", Description = "Travel backpack with USB charging",
                Price = 89.99m, IsActive = true, CreatedAt = new DateTime(2024, 10, 5),
                StockCount = 60, Status = ProductStatus.Published
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();

        // Product Tags (One-to-Many)
        var tags = new List<ProductTag>
        {
            new() { Id = 1, ProductId = 1, Name = "Premium" },
            new() { Id = 2, ProductId = 1, Name = "Wireless" },
            new() { Id = 3, ProductId = 2, Name = "Premium" },
            new() { Id = 4, ProductId = 2, Name = "Sport" },
            new() { Id = 5, ProductId = 3, Name = "Education" },
            new() { Id = 6, ProductId = 4, Name = "Premium" },
            new() { Id = 7, ProductId = 4, Name = "Smart" },
            new() { Id = 8, ProductId = 5, Name = "Casual" },
            new() { Id = 9, ProductId = 7, Name = "Sport" },
            new() { Id = 10, ProductId = 7, Name = "Wellness" },
            new() { Id = 11, ProductId = 9, Name = "Winter" },
            new() { Id = 12, ProductId = 10, Name = "Smart" },
            new() { Id = 13, ProductId = 10, Name = "Sport" },
            new() { Id = 14, ProductId = 12, Name = "Travel" },
            new() { Id = 15, ProductId = 12, Name = "Premium" }
        };

        context.ProductTags.AddRange(tags);

        // Product-Category (Many-to-Many)
        var productCategories = new List<ProductCategory>
        {
            new() { ProductId = 1, CategoryId = 1 },  // Headphones -> Electronics
            new() { ProductId = 2, CategoryId = 4 },  // Running Shoes -> Sports
            new() { ProductId = 2, CategoryId = 2 },  // Running Shoes -> Clothing
            new() { ProductId = 3, CategoryId = 3 },  // Programming Book -> Books
            new() { ProductId = 4, CategoryId = 1 },  // Smart Watch -> Electronics
            new() { ProductId = 5, CategoryId = 2 },  // Cotton T-Shirt -> Clothing
            new() { ProductId = 6, CategoryId = 1 },  // Bluetooth Speaker -> Electronics
            new() { ProductId = 7, CategoryId = 4 },  // Yoga Mat -> Sports
            new() { ProductId = 8, CategoryId = 1 },  // Draft Laptop -> Electronics
            new() { ProductId = 9, CategoryId = 2 },  // Winter Jacket -> Clothing
            new() { ProductId = 10, CategoryId = 1 }, // Fitness Tracker -> Electronics
            new() { ProductId = 10, CategoryId = 4 }, // Fitness Tracker -> Sports
            new() { ProductId = 11, CategoryId = 1 }, // Phone Case -> Electronics
            new() { ProductId = 12, CategoryId = 2 }  // Backpack -> Clothing
        };

        context.ProductCategories.AddRange(productCategories);

        // Bookings (IFilterTransformable demo)
        var bookings = new List<Booking>
        {
            new()
            {
                Id = 1, CustomerName = "Ali Yilmaz", Notes = "Early check-in requested",
                BookingDateTimestamp = new DateTimeOffset(2024, 6, 15, 14, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = BookingStatus.Confirmed, TotalAmount = 450.00m, IsConfirmed = true
            },
            new()
            {
                Id = 2, CustomerName = "Ayse Demir", Notes = null,
                BookingDateTimestamp = new DateTimeOffset(2024, 7, 20, 10, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = BookingStatus.Pending, TotalAmount = 320.00m, IsConfirmed = false
            },
            new()
            {
                Id = 3, CustomerName = "Mehmet Kaya", Notes = "VIP customer",
                BookingDateTimestamp = new DateTimeOffset(2024, 8, 5, 16, 30, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = BookingStatus.Completed, TotalAmount = 890.00m, IsConfirmed = true
            },
            new()
            {
                Id = 4, CustomerName = "Fatma Ozturk", Notes = "Late check-out needed",
                BookingDateTimestamp = new DateTimeOffset(2024, 9, 10, 9, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = BookingStatus.Confirmed, TotalAmount = 550.00m, IsConfirmed = true
            },
            new()
            {
                Id = 5, CustomerName = "Emre Sahin", Notes = "Cancelled due to weather",
                BookingDateTimestamp = new DateTimeOffset(2024, 10, 1, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = BookingStatus.Cancelled, TotalAmount = 200.00m, IsConfirmed = false
            },
            new()
            {
                Id = 6, CustomerName = "Zeynep Arslan", Notes = "Business trip",
                BookingDateTimestamp = new DateTimeOffset(2024, 11, 15, 8, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = BookingStatus.Pending, TotalAmount = 750.00m, IsConfirmed = false
            }
        };

        context.Bookings.AddRange(bookings);

        // Reservations (FilterTransformAttribute demo)
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1, GuestName = "John Smith", Notes = "Honeymoon suite requested",
                RoomNumber = "101",
                ReservationDateTimestamp = new DateTimeOffset(2024, 6, 20, 14, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = ReservationStatus.Confirmed, TotalPrice = 1200.00m, NumberOfGuests = 2
            },
            new()
            {
                Id = 2, GuestName = "Emma Johnson", Notes = "Allergic to feathers",
                RoomNumber = "205",
                ReservationDateTimestamp = new DateTimeOffset(2024, 7, 10, 15, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = ReservationStatus.CheckedIn, TotalPrice = 800.00m, NumberOfGuests = 1
            },
            new()
            {
                Id = 3, GuestName = "Carlos Garcia", Notes = null,
                RoomNumber = "310",
                ReservationDateTimestamp = new DateTimeOffset(2024, 8, 1, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = ReservationStatus.CheckedOut, TotalPrice = 650.00m, NumberOfGuests = 3
            },
            new()
            {
                Id = 4, GuestName = "Sophie Martin", Notes = "VIP guest - extra amenities",
                RoomNumber = "401",
                ReservationDateTimestamp = new DateTimeOffset(2024, 9, 5, 14, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = ReservationStatus.Pending, TotalPrice = 2500.00m, NumberOfGuests = 2
            },
            new()
            {
                Id = 5, GuestName = "Hans Mueller", Notes = "Business conference attendee",
                RoomNumber = "102",
                ReservationDateTimestamp = new DateTimeOffset(2024, 10, 15, 10, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = ReservationStatus.Confirmed, TotalPrice = 450.00m, NumberOfGuests = 1
            },
            new()
            {
                Id = 6, GuestName = "Yuki Tanaka", Notes = "Cancelled - schedule conflict",
                RoomNumber = "203",
                ReservationDateTimestamp = new DateTimeOffset(2024, 11, 1, 16, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                Status = ReservationStatus.Cancelled, TotalPrice = 900.00m, NumberOfGuests = 4
            }
        };

        context.Reservations.AddRange(reservations);
        context.SaveChanges();
    }
}
