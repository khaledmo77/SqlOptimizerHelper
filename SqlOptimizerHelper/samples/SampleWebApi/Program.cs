using Microsoft.EntityFrameworkCore;
using SampleWebApi.Data;
using SqlOptimizerHelper.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with SqlOptimizerHelper
// This demonstrates the main usage of the SqlOptimizerHelper library
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Configure SQL Server connection
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Debug logging
    Console.WriteLine("[SQL Optimizer] 🔥 Registering SqlOptimizerHelper with DbContext...");
    
    // Add SqlOptimizerHelper with custom configuration
    // This will enable SQL optimization analysis, N+1 detection, and performance monitoring
    options.AddSqlOptimizer(config =>
    {
        // Configure slow query threshold (queries taking longer than 100ms will be flagged)
        config.SlowQueryThresholdMs = 100;
        
        // Enable all optimization features
        config.EnableN1Detection = true;
        config.EnableIndexAnalysis = true;
        config.EnableSlowQueryDetection = true;
        
        // Configure logging
        config.EnableConsoleOutput = true;
        config.EnableJsonReports = true;
        config.LogPath = "./logs";
        
        // Configure N+1 detection
        config.N1DetectionThreshold = 3; // Flag when same query pattern is executed 3+ times
        config.N1DetectionTimeWindowSeconds = 30; // Within 30 seconds
        
        // Application identification
        config.ApplicationName = "E-Commerce Sample API";
        config.Environment = builder.Environment.EnvironmentName;
        
        // Enable detailed SQL logging for demonstration
        config.EnableDetailedSqlLogging = true;
        
        // Debug logging
        Console.WriteLine($"[SQL Optimizer] 🔥 Configuration set: SlowQueryThreshold={config.SlowQueryThresholdMs}ms, EnableN1Detection={config.EnableN1Detection}");
    });
    
    Console.WriteLine("[SQL Optimizer] 🔥 DbContext configuration completed!");
});

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Create database if it doesn't exist
    context.Database.EnsureCreated();
    
    // Seed data if database is empty
    if (!context.Products.Any())
    {
        // Data is seeded in the DbContext.OnModelCreating method
        Console.WriteLine("Database seeded with initial data.");
    }
}

// Add a simple endpoint to demonstrate the SqlOptimizerHelper
app.MapGet("/", () =>
{
    return new
    {
        Message = "E-Commerce Sample API with SqlOptimizerHelper",
        Description = "This API demonstrates SQL optimization analysis, N+1 query detection, and performance monitoring.",
        Endpoints = new
        {
            Products = "/api/products",
            Customers = "/api/customers", 
            Orders = "/api/orders",
            Swagger = "/swagger"
        },
        OptimizationFeatures = new
        {
            SlowQueryDetection = "Queries taking longer than 100ms are flagged",
            N1Detection = "Repeated query patterns are detected and reported",
            MissingIndexAnalysis = "WHERE clauses are analyzed for missing indexes",
            ConsoleLogging = "Real-time optimization suggestions in console",
            JsonReports = "Daily optimization reports saved to ./logs folder"
        }
    };
});

app.Run();
