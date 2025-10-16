# SqlOptimizerHelper

[![NuGet Version](https://img.shields.io/nuget/v/SqlOptimizerHelper.svg)](https://www.nuget.org/packages/SqlOptimizerHelper/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**SqlOptimizerHelper** is a powerful Entity Framework Core extension that automatically detects and analyzes SQL performance issues in your applications. It provides real-time monitoring, detailed analysis, and actionable suggestions to optimize your database queries.

## 🚀 Features

- **🔍 Slow Query Detection** - Automatically identifies queries exceeding performance thresholds
- **⚠️ N+1 Query Detection** - Detects and reports N+1 query problems with specific suggestions
- **📊 Missing Index Analysis** - Analyzes WHERE clauses and suggests missing indexes
- **📝 Real-time Console Logging** - Immediate feedback with colored warnings and suggestions
- **📈 JSON Report Generation** - Daily optimization reports with detailed statistics
- **⚙️ Highly Configurable** - Customizable thresholds, logging, and analysis settings
- **🔧 Easy Integration** - Simple one-line setup with your existing EF Core configuration

## 📦 Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package SqlOptimizerHelper
```

Or via Package Manager Console:

```powershell
Install-Package SqlOptimizerHelper
```

## 🎯 Quick Start

### Basic Usage

Add SqlOptimizerHelper to your EF Core configuration:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
           .AddSqlOptimizer()); // 👈 Your optimization helper
```

### Advanced Configuration

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
           .AddSqlOptimizer(config =>
           {
               // Configure slow query threshold (default: 1000ms)
               config.SlowQueryThresholdMs = 500;
               
               // Enable/disable specific features
               config.EnableN1Detection = true;
               config.EnableIndexAnalysis = true;
               config.EnableSlowQueryDetection = true;
               
               // Configure logging
               config.EnableConsoleOutput = true;
               config.EnableJsonReports = true;
               config.LogPath = "./logs";
               
               // N+1 detection settings
               config.N1DetectionThreshold = 5; // Flag after 5 similar queries
               config.N1DetectionTimeWindowSeconds = 30; // Within 30 seconds
               
               // Application identification
               config.ApplicationName = "My E-Commerce App";
               config.Environment = "Production";
           }));
```

### Configuration via appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=MyDb;..."
  },
  "SqlOptimizer": {
    "SlowQueryThresholdMs": 1000,
    "EnableN1Detection": true,
    "EnableIndexAnalysis": true,
    "EnableSlowQueryDetection": true,
    "EnableConsoleOutput": true,
    "EnableJsonReports": true,
    "LogPath": "./logs",
    "N1DetectionThreshold": 5,
    "N1DetectionTimeWindowSeconds": 30,
    "ApplicationName": "My Application",
    "Environment": "Production"
  }
}
```

Then use it in your configuration:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
           .AddSqlOptimizer(builder.Configuration));
```

## 🔎 Detection Examples

### Case 1: Slow Query Detection

**Your Code:**
```csharp
var products = await _context.Products
    .Where(p => p.Name.Contains("phone"))
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT * FROM Products WHERE Name LIKE '%phone%'
```

**SqlOptimizerHelper Output:**
```
[SQL Optimizer] ⚠️ Slow query detected (3.4s)
    Warning: Slow query detected: 3400ms (threshold: 1000ms)
    Suggestion: Consider adding indexes on frequently queried columns; Review query execution plan for optimization opportunities
```

### Case 2: N+1 Query Problem

**Your Code:**
```csharp
var orders = await _context.Orders.ToListAsync();

foreach (var order in orders)
{
    var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.Id == order.CustomerId);
}
```

**SqlOptimizerHelper Output:**
```
[SQL Optimizer] ⚠️ N+1 Query Detected
    Warning: N+1 Query Detected: Customers queried 101 times in 30 seconds.
    Suggestion: Use 'Include()' to fetch related Customers data in one query. Example: context.Orders.Include(o => o.Customer).ToListAsync()
```

### Case 3: Missing Index Detection

**Your Code:**
```csharp
var products = await _context.Products
    .Where(p => p.Category == "Electronics")
    .ToListAsync();
```

**SqlOptimizerHelper Output:**
```
[SQL Optimizer] ⚡ MissingIndex detected
    Warning: No index on 'Products.Category'. Consider creating an index.
    Suggestion: CREATE INDEX IX_Products_Category ON Products(Category)
```

## 📊 Report Generation

SqlOptimizerHelper automatically generates daily JSON reports in your specified log directory:

**File:** `./logs/sql-optimizer-report-2024-01-15.json`

```json
{
  "generatedAt": "2024-01-15T10:30:00Z",
  "applicationName": "My E-Commerce App",
  "environment": "Production",
  "analysisResults": [
    {
      "query": "SELECT * FROM Products WHERE Name LIKE '%phone%'",
      "executionTimeMs": 3400,
      "issueType": "SlowQuery",
      "severity": "High",
      "tableName": "Products",
      "columnName": "Name",
      "warning": "Slow query detected: 3400ms (threshold: 1000ms)",
      "suggestion": "Consider adding indexes on frequently queried columns",
      "timestamp": "2024-01-15T10:25:00Z"
    },
    {
      "issueType": "N+1",
      "severity": "High",
      "tableName": "Customers",
      "warning": "N+1 Query Detected: Customers queried 101 times in 30 seconds.",
      "suggestion": "Use 'Include()' to fetch related Customers data in one query"
    }
  ],
  "summary": {
    "totalQueries": 2,
    "slowQueries": 1,
    "n1Problems": 1,
    "missingIndexes": 0,
    "averageExecutionTimeMs": 1800,
    "slowestQueryMs": 3400,
    "totalExecutionTimeMs": 3600
  }
}
```

## ⚙️ Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SlowQueryThresholdMs` | long | 1000 | Threshold in milliseconds for slow query detection |
| `EnableN1Detection` | bool | true | Enable N+1 query problem detection |
| `EnableIndexAnalysis` | bool | true | Enable missing index analysis |
| `EnableSlowQueryDetection` | bool | true | Enable slow query detection |
| `EnableConsoleOutput` | bool | true | Enable real-time console logging |
| `EnableJsonReports` | bool | true | Enable JSON report generation |
| `LogPath` | string | "./logs" | Directory for log files and reports |
| `N1DetectionThreshold` | int | 5 | Minimum queries to trigger N+1 detection |
| `N1DetectionTimeWindowSeconds` | int | 30 | Time window for N+1 detection |
| `EnableDetailedSqlLogging` | bool | false | Log full SQL queries (security consideration) |
| `MaxResultsInMemory` | int | 1000 | Maximum analysis results to keep in memory |
| `ApplicationName` | string | "Unknown" | Application name for reports |
| `Environment` | string | "Development" | Environment name for reports |

## 🛠️ Advanced Usage

### Programmatic Access

```csharp
// Get current analysis results
var results = SqlOptimizerService.GetAnalysisResults();

// Generate a report
var report = SqlOptimizerService.GenerateReport();

// Clear stored results
SqlOptimizerService.ClearResults();

// Check if service is registered
var isRegistered = SqlOptimizerService.IsRegistered();
```

### Custom Analysis

```csharp
// Access the interceptor directly
var interceptor = SqlOptimizerService.GetInterceptor();
if (interceptor != null)
{
    var results = interceptor.GetAnalysisResults();
    var report = interceptor.GenerateFinalReport();
}
```

## 🧪 Testing

The library includes comprehensive unit and integration tests. Run tests using:

```bash
dotnet test
```

## 📈 Performance Impact

SqlOptimizerHelper is designed to have minimal performance impact:

- **Overhead**: < 1ms per query in most cases
- **Memory Usage**: Configurable limits prevent memory leaks
- **Async Operations**: Logging and reporting are non-blocking
- **Production Ready**: Safe to use in production environments

## 🔒 Security Considerations

- **SQL Logging**: Disabled by default to prevent sensitive data exposure
- **Parameter Sanitization**: Query parameters are sanitized in logs
- **File Permissions**: Ensure proper permissions on log directories
- **Network Security**: Reports are stored locally by default

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/SqlOptimizerHelper/issues)
- **Documentation**: [Wiki](https://github.com/yourusername/SqlOptimizerHelper/wiki)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/SqlOptimizerHelper/discussions)

## 🙏 Acknowledgments

- Entity Framework Core team for the excellent interceptor infrastructure
- .NET community for feedback and suggestions
- Contributors and users who help improve this library

---

**Made with ❤️ for the .NET community**
