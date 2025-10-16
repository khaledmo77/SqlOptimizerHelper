using Microsoft.EntityFrameworkCore;
using SampleWebApi.Models;

namespace SampleWebApi.Data
{
    /// <summary>
    /// Entity Framework DbContext for the e-commerce application
    /// This context demonstrates various SQL optimization scenarios
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Products table - will be used to demonstrate missing index detection
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// Customers table - will be used to demonstrate N+1 query problems
        /// </summary>
        public DbSet<Customer> Customers { get; set; }

        /// <summary>
        /// Orders table - will be used to demonstrate N+1 query problems
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        /// <summary>
        /// Order items table - will be used to demonstrate complex queries
        /// </summary>
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Note: We intentionally don't add indexes here to demonstrate missing index detection
                // In a real application, you would add indexes like:
                // entity.HasIndex(e => e.Name).HasDatabaseName("IX_Products_Name");
                // entity.HasIndex(e => e.Category).HasDatabaseName("IX_Products_Category");
                // entity.HasIndex(e => e.Price).HasDatabaseName("IX_Products_Price");
            });

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Note: We intentionally don't add indexes here to demonstrate missing index detection
                // In a real application, you would add indexes like:
                // entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_Customers_Email");
                // entity.HasIndex(e => e.City).HasDatabaseName("IX_Customers_City");
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OrderDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.ShippingAddress).HasMaxLength(500);
                entity.Property(e => e.Notes).HasMaxLength(1000);

                // Configure relationship with Customer
                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Orders)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Note: We intentionally don't add indexes here to demonstrate missing index detection
                // In a real application, you would add indexes like:
                // entity.HasIndex(e => e.CustomerId).HasDatabaseName("IX_Orders_CustomerId");
                // entity.HasIndex(e => e.OrderDate).HasDatabaseName("IX_Orders_OrderDate");
                // entity.HasIndex(e => e.Status).HasDatabaseName("IX_Orders_Status");
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");

                // Configure relationship with Order
                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship with Product
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Note: We intentionally don't add indexes here to demonstrate missing index detection
                // In a real application, you would add indexes like:
                // entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_OrderItems_OrderId");
                // entity.HasIndex(e => e.ProductId).HasDatabaseName("IX_OrderItems_ProductId");
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Seeds the database with initial data for testing
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Products
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "iPhone 15 Pro", Description = "Latest iPhone with advanced features", Price = 999.99m, Category = "Electronics", StockQuantity = 50 },
                new Product { Id = 2, Name = "Samsung Galaxy S24", Description = "Premium Android smartphone", Price = 899.99m, Category = "Electronics", StockQuantity = 30 },
                new Product { Id = 3, Name = "MacBook Pro 16", Description = "Professional laptop for developers", Price = 2499.99m, Category = "Computers", StockQuantity = 20 },
                new Product { Id = 4, Name = "Dell XPS 13", Description = "Ultrabook for business users", Price = 1299.99m, Category = "Computers", StockQuantity = 25 },
                new Product { Id = 5, Name = "Nike Air Max", Description = "Comfortable running shoes", Price = 129.99m, Category = "Shoes", StockQuantity = 100 },
                new Product { Id = 6, Name = "Adidas Ultraboost", Description = "High-performance running shoes", Price = 149.99m, Category = "Shoes", StockQuantity = 80 },
                new Product { Id = 7, Name = "Levi's 501 Jeans", Description = "Classic denim jeans", Price = 79.99m, Category = "Clothing", StockQuantity = 150 },
                new Product { Id = 8, Name = "Uniqlo T-Shirt", Description = "Comfortable cotton t-shirt", Price = 19.99m, Category = "Clothing", StockQuantity = 200 }
            };

            modelBuilder.Entity<Product>().HasData(products);

            // Seed Customers
            var customers = new List<Customer>
            {
                new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@email.com", Phone = "+1-555-0101", City = "New York", Country = "USA" },
                new Customer { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@email.com", Phone = "+1-555-0102", City = "Los Angeles", Country = "USA" },
                new Customer { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@email.com", Phone = "+1-555-0103", City = "Chicago", Country = "USA" },
                new Customer { Id = 4, FirstName = "Alice", LastName = "Brown", Email = "alice.brown@email.com", Phone = "+1-555-0104", City = "Houston", Country = "USA" },
                new Customer { Id = 5, FirstName = "Charlie", LastName = "Wilson", Email = "charlie.wilson@email.com", Phone = "+1-555-0105", City = "Phoenix", Country = "USA" }
            };

            modelBuilder.Entity<Customer>().HasData(customers);

            // Seed Orders
            var orders = new List<Order>
            {
                new Order { Id = 1, CustomerId = 1, OrderNumber = "ORD-001", Status = "Delivered", TotalAmount = 1079.98m, OrderDate = DateTime.UtcNow.AddDays(-10) },
                new Order { Id = 2, CustomerId = 2, OrderNumber = "ORD-002", Status = "Shipped", TotalAmount = 2499.99m, OrderDate = DateTime.UtcNow.AddDays(-5) },
                new Order { Id = 3, CustomerId = 1, OrderNumber = "ORD-003", Status = "Pending", TotalAmount = 199.98m, OrderDate = DateTime.UtcNow.AddDays(-2) },
                new Order { Id = 4, CustomerId = 3, OrderNumber = "ORD-004", Status = "Delivered", TotalAmount = 79.99m, OrderDate = DateTime.UtcNow.AddDays(-15) },
                new Order { Id = 5, CustomerId = 4, OrderNumber = "ORD-005", Status = "Processing", TotalAmount = 1299.99m, OrderDate = DateTime.UtcNow.AddDays(-1) }
            };

            modelBuilder.Entity<Order>().HasData(orders);

            // Seed OrderItems
            var orderItems = new List<OrderItem>
            {
                new OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 999.99m, TotalPrice = 999.99m },
                new OrderItem { Id = 2, OrderId = 1, ProductId = 5, Quantity = 1, UnitPrice = 79.99m, TotalPrice = 79.99m },
                new OrderItem { Id = 3, OrderId = 2, ProductId = 3, Quantity = 1, UnitPrice = 2499.99m, TotalPrice = 2499.99m },
                new OrderItem { Id = 4, OrderId = 3, ProductId = 5, Quantity = 1, UnitPrice = 129.99m, TotalPrice = 129.99m },
                new OrderItem { Id = 5, OrderId = 3, ProductId = 6, Quantity = 1, UnitPrice = 69.99m, TotalPrice = 69.99m },
                new OrderItem { Id = 6, OrderId = 4, ProductId = 7, Quantity = 1, UnitPrice = 79.99m, TotalPrice = 79.99m },
                new OrderItem { Id = 7, OrderId = 5, ProductId = 4, Quantity = 1, UnitPrice = 1299.99m, TotalPrice = 1299.99m }
            };

            modelBuilder.Entity<OrderItem>().HasData(orderItems);
        }
    }
}
