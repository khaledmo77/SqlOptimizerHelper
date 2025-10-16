using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SqlOptimizerHelper.Extensions;
using SqlOptimizerHelper.Models;
using SqlOptimizerHelper.Services;
using System.Data.Common;
using System.Diagnostics;

namespace SqlOptimizerHelper.Tests;

/// <summary>
/// Integration tests for SqlOptimizerHelper with real EF Core scenarios
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly SqlOptimizerConfig _config;

    public IntegrationTests()
    {
        _config = new SqlOptimizerConfig
        {
            SlowQueryThresholdMs = 50, // Low threshold for testing
            EnableN1Detection = true,
            EnableIndexAnalysis = true,
            EnableSlowQueryDetection = true,
            EnableConsoleOutput = false, // Disable for tests
            EnableJsonReports = false, // Disable for tests
            LogPath = "./test-logs",
            N1DetectionThreshold = 2, // Low threshold for testing
            N1DetectionTimeWindowSeconds = 10,
            ApplicationName = "IntegrationTestApp",
            Environment = "Test"
        };

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddSqlOptimizer(_config)
            .Options;

        _context = new TestDbContext(options);
        SeedTestData();
    }

    [Fact]
    public async Task QueryInterceptor_ShouldDetectSlowQueries()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute a query that should be flagged as slow
        var products = await _context.Products
            .Where(p => p.Name.Contains("Test"))
            .ToListAsync();

        // Simulate slow query by adding delay
        await Task.Delay(100); // Above threshold

        // Assert
        var results = interceptor.GetAnalysisResults();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.IssueType == "SlowQuery");
    }

    [Fact]
    public async Task QueryInterceptor_ShouldDetectMissingIndexes()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute queries that should trigger missing index detection
        var products = await _context.Products
            .Where(p => p.Name == "Test Product 1")
            .ToListAsync();

        var customers = await _context.Customers
            .Where(c => c.Email == "test1@example.com")
            .ToListAsync();

        // Assert
        var results = interceptor.GetAnalysisResults();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.IssueType == "MissingIndex");
        Assert.Contains(results, r => r.TableName == "Products");
        Assert.Contains(results, r => r.TableName == "Customers");
    }

    [Fact]
    public async Task QueryInterceptor_ShouldDetectN1Problems()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute queries that should trigger N+1 detection
        var orders = await _context.Orders.ToListAsync();

        // Simulate N+1 problem by querying customers individually
        foreach (var order in orders)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == order.CustomerId);
        }

        // Assert
        var results = interceptor.GetAnalysisResults();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.IssueType == "N+1");
        Assert.Contains(results, r => r.Warning.Contains("N+1 Query Detected"));
    }

    [Fact]
    public async Task QueryInterceptor_ShouldGenerateOptimizationReport()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute various queries to generate analysis results
        await _context.Products.Where(p => p.Name == "Test Product 1").ToListAsync();
        await _context.Customers.Where(c => c.Email == "test1@example.com").ToListAsync();
        await _context.Orders.Where(o => o.Status == "Pending").ToListAsync();

        // Assert
        var report = interceptor.GenerateFinalReport();
        Assert.NotNull(report);
        Assert.True(report.AnalysisResults.Count > 0);
        Assert.True(report.Summary.TotalQueries > 0);
        Assert.Equal("IntegrationTestApp", report.ApplicationName);
        Assert.Equal("Test", report.Environment);
    }

    [Fact]
    public async Task SqlOptimizerService_ShouldRegisterCorrectly()
    {
        // Arrange & Act
        var isRegistered = SqlOptimizerService.IsRegistered();
        var config = SqlOptimizerService.GetConfiguration();
        var interceptor = SqlOptimizerService.GetInterceptor();

        // Assert
        Assert.True(isRegistered);
        Assert.NotNull(config);
        Assert.NotNull(interceptor);
        Assert.Equal("IntegrationTestApp", config.ApplicationName);
        Assert.Equal("Test", config.Environment);
    }

    [Fact]
    public async Task QueryInterceptor_ShouldHandleMultipleQueryTypes()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute different types of queries
        var products = await _context.Products.ToListAsync(); // SELECT
        var newProduct = new TestProduct { Name = "New Product", Price = 99.99m };
        _context.Products.Add(newProduct);
        await _context.SaveChangesAsync(); // INSERT

        var product = await _context.Products.FirstAsync();
        product.Price = 199.99m;
        await _context.SaveChangesAsync(); // UPDATE

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(); // DELETE

        // Assert
        var results = interceptor.GetAnalysisResults();
        Assert.NotEmpty(results);
        // Should have analysis results for SELECT queries only
        Assert.All(results, r => Assert.True(r.Query.ToUpper().StartsWith("SELECT")));
    }

    [Fact]
    public async Task QueryInterceptor_ShouldExtractTableNamesCorrectly()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute queries with different table names
        await _context.Products.Where(p => p.Name == "Test").ToListAsync();
        await _context.Customers.Where(c => c.Email == "test").ToListAsync();
        await _context.Orders.Where(o => o.Status == "Test").ToListAsync();

        // Assert
        var results = interceptor.GetAnalysisResults();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.TableName == "Products");
        Assert.Contains(results, r => r.TableName == "Customers");
        Assert.Contains(results, r => r.TableName == "Orders");
    }

    [Fact]
    public async Task QueryInterceptor_ShouldExtractWhereColumnsCorrectly()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute queries with different WHERE columns
        await _context.Products.Where(p => p.Name == "Test").ToListAsync();
        await _context.Products.Where(p => p.Price > 100).ToListAsync();
        await _context.Customers.Where(c => c.Email == "test").ToListAsync();

        // Assert
        var results = interceptor.GetAnalysisResults();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ColumnName == "Name");
        Assert.Contains(results, r => r.ColumnName == "Price");
        Assert.Contains(results, r => r.ColumnName == "Email");
    }

    [Fact]
    public async Task QueryInterceptor_ShouldHandleComplexQueries()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute complex queries
        var complexQuery = await _context.Products
            .Where(p => p.Name.Contains("Test") && p.Price > 50 && p.Price < 200)
            .OrderBy(p => p.Name)
            .Take(10)
            .ToListAsync();

        // Assert
        var results = interceptor.GetAnalysisResults();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.IssueType == "MissingIndex");
    }

    [Fact]
    public async Task QueryInterceptor_ShouldClearResults()
    {
        // Arrange
        var interceptor = SqlOptimizerService.GetInterceptor();
        Assert.NotNull(interceptor);

        // Act - Execute some queries
        await _context.Products.Where(p => p.Name == "Test").ToListAsync();
        var resultsBefore = interceptor.GetAnalysisResults();
        Assert.NotEmpty(resultsBefore);

        // Clear results
        interceptor.ClearAnalysisResults();
        var resultsAfter = interceptor.GetAnalysisResults();

        // Assert
        Assert.Empty(resultsAfter);
    }

    private void SeedTestData()
    {
        // Seed Products
        var products = new List<TestProduct>
        {
            new() { Id = 1, Name = "Test Product 1", Price = 99.99m },
            new() { Id = 2, Name = "Test Product 2", Price = 199.99m },
            new() { Id = 3, Name = "Test Product 3", Price = 299.99m }
        };
        _context.Products.AddRange(products);

        // Seed Customers
        var customers = new List<TestCustomer>
        {
            new() { Id = 1, Name = "Test Customer 1", Email = "test1@example.com" },
            new() { Id = 2, Name = "Test Customer 2", Email = "test2@example.com" },
            new() { Id = 3, Name = "Test Customer 3", Email = "test3@example.com" }
        };
        _context.Customers.AddRange(customers);

        // Seed Orders
        var orders = new List<TestOrder>
        {
            new() { Id = 1, CustomerId = 1, Status = "Pending", TotalAmount = 99.99m },
            new() { Id = 2, CustomerId = 2, Status = "Shipped", TotalAmount = 199.99m },
            new() { Id = 3, CustomerId = 3, Status = "Delivered", TotalAmount = 299.99m }
        };
        _context.Orders.AddRange(orders);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        SqlOptimizerService.Reset();
    }
}

// Test entities for integration tests
public class TestProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class TestCustomer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class TestOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestProduct> Products { get; set; }
    public DbSet<TestCustomer> Customers { get; set; }
    public DbSet<TestOrder> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<TestCustomer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Email).IsRequired();
        });

        modelBuilder.Entity<TestOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });
    }
}
