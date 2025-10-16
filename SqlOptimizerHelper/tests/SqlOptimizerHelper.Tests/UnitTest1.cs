using SqlOptimizerHelper.Models;
using SqlOptimizerHelper.Services;
using SqlOptimizerHelper.Logging;
using SqlOptimizerHelper.Interceptors;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;
using Moq;

namespace SqlOptimizerHelper.Tests;

/// <summary>
/// Unit tests for SqlOptimizerHelper functionality
/// </summary>
public class SqlOptimizerHelperTests
{
    private readonly SqlOptimizerConfig _defaultConfig;

    public SqlOptimizerHelperTests()
    {
        _defaultConfig = new SqlOptimizerConfig
        {
            SlowQueryThresholdMs = 1000,
            EnableN1Detection = true,
            EnableIndexAnalysis = true,
            EnableSlowQueryDetection = true,
            EnableConsoleOutput = false, // Disable for tests
            EnableJsonReports = false, // Disable for tests
            LogPath = "./test-logs",
            N1DetectionThreshold = 3,
            N1DetectionTimeWindowSeconds = 30,
            ApplicationName = "TestApp",
            Environment = "Test"
        };
    }

    [Fact]
    public void SqlOptimizerConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new SqlOptimizerConfig();

        // Assert
        Assert.Equal(1000, config.SlowQueryThresholdMs);
        Assert.True(config.EnableN1Detection);
        Assert.True(config.EnableIndexAnalysis);
        Assert.True(config.EnableSlowQueryDetection);
        Assert.True(config.EnableConsoleOutput);
        Assert.True(config.EnableJsonReports);
        Assert.Equal("./logs", config.LogPath);
        Assert.Equal(5, config.N1DetectionThreshold);
        Assert.Equal(30, config.N1DetectionTimeWindowSeconds);
        Assert.Equal("Unknown", config.ApplicationName);
        Assert.Equal("Development", config.Environment);
    }

    [Fact]
    public void QueryAnalysisResult_Properties_ShouldBeSetCorrectly()
    {
        // Arrange
        var result = new QueryAnalysisResult
        {
            Query = "SELECT * FROM Products WHERE Name = 'test'",
            ExecutionTimeMs = 1500,
            Warning = "Slow query detected",
            Suggestion = "Add index on Name column",
            IssueType = "SlowQuery",
            TableName = "Products",
            ColumnName = "Name",
            Severity = "High"
        };

        // Assert
        Assert.Equal("SELECT * FROM Products WHERE Name = 'test'", result.Query);
        Assert.Equal(1500, result.ExecutionTimeMs);
        Assert.Equal("Slow query detected", result.Warning);
        Assert.Equal("Add index on Name column", result.Suggestion);
        Assert.Equal("SlowQuery", result.IssueType);
        Assert.Equal("Products", result.TableName);
        Assert.Equal("Name", result.ColumnName);
        Assert.Equal("High", result.Severity);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void PerformanceAnalyzer_AnalyzePerformance_SlowQuery_ShouldReturnResult()
    {
        // Arrange
        var analyzer = new PerformanceAnalyzer(_defaultConfig);
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Products",
            ExecutionTimeMs = 1500, // Above threshold
            IsSelectQuery = true
        };

        // Act
        var result = analyzer.AnalyzePerformance(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SlowQuery", result.IssueType);
        Assert.Equal("High", result.Severity);
        Assert.Equal(1500, result.ExecutionTimeMs);
        Assert.Contains("Slow query detected", result.Warning);
    }

    [Fact]
    public void PerformanceAnalyzer_AnalyzePerformance_FastQuery_ShouldReturnNull()
    {
        // Arrange
        var analyzer = new PerformanceAnalyzer(_defaultConfig);
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Products",
            ExecutionTimeMs = 100, // Below threshold
            IsSelectQuery = true
        };

        // Act
        var result = analyzer.AnalyzePerformance(metrics);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IndexAnalyzer_AnalyzeForMissingIndexes_EqualityCondition_ShouldReturnSuggestion()
    {
        // Arrange
        var analyzer = new IndexAnalyzer(_defaultConfig);
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Products WHERE Name = 'iPhone'",
            ExecutionTimeMs = 500,
            IsSelectQuery = true,
            WhereColumns = new List<string> { "Name" }
        };

        // Act
        var results = analyzer.AnalyzeForMissingIndexes(metrics);

        // Assert
        Assert.NotEmpty(results);
        var result = results.First();
        Assert.Equal("MissingIndex", result.IssueType);
        Assert.Equal("Products", result.TableName);
        Assert.Equal("Name", result.ColumnName);
        Assert.Contains("CREATE INDEX", result.Suggestion);
    }

    [Fact]
    public void IndexAnalyzer_AnalyzeForMissingIndexes_LikeCondition_ShouldReturnSuggestion()
    {
        // Arrange
        var analyzer = new IndexAnalyzer(_defaultConfig);
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Products WHERE Name LIKE '%phone%'",
            ExecutionTimeMs = 500,
            IsSelectQuery = true,
            WhereColumns = new List<string> { "Name" }
        };

        // Act
        var results = analyzer.AnalyzeForMissingIndexes(metrics);

        // Assert
        Assert.NotEmpty(results);
        var result = results.First();
        Assert.Equal("MissingIndex", result.IssueType);
        Assert.Contains("CREATE INDEX", result.Suggestion);
    }

    [Fact]
    public void N1Detector_DetectN1Problem_RepeatedQueries_ShouldDetectN1()
    {
        // Arrange
        var detector = new N1Detector(_defaultConfig);
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Customers WHERE Id = @id",
            ExecutionTimeMs = 50,
            IsSelectQuery = true,
            TableNames = new List<string> { "Customers" }
        };

        // Act - Execute the same query pattern multiple times
        QueryAnalysisResult? result = null;
        for (int i = 0; i < 5; i++)
        {
            result = detector.DetectN1Problem(metrics);
        }

        // Assert
        Assert.NotNull(result);
        Assert.Equal("N+1", result.IssueType);
        Assert.Equal("High", result.Severity);
        Assert.Contains("N+1 Query Detected", result.Warning);
        Assert.Contains("Include", result.Suggestion);
    }

    [Fact]
    public void N1Detector_DetectN1Problem_FewQueries_ShouldNotDetectN1()
    {
        // Arrange
        var detector = new N1Detector(_defaultConfig);
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Customers WHERE Id = @id",
            ExecutionTimeMs = 50,
            IsSelectQuery = true,
            TableNames = new List<string> { "Customers" }
        };

        // Act - Execute the query pattern only twice (below threshold)
        QueryAnalysisResult? result = null;
        for (int i = 0; i < 2; i++)
        {
            result = detector.DetectN1Problem(metrics);
        }

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void QueryAnalyzer_AnalyzeQuery_ShouldReturnMultipleResults()
    {
        // Arrange
        var analyzer = new QueryAnalyzer(_defaultConfig);
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Products WHERE Name = 'iPhone'",
            ExecutionTimeMs = 1500, // Slow query
            IsSelectQuery = true,
            WhereColumns = new List<string> { "Name" }
        };

        // Act
        var results = analyzer.AnalyzeQuery(metrics);

        // Assert
        Assert.NotEmpty(results);
        Assert.True(results.Count >= 2); // Should have both slow query and missing index results
        Assert.Contains(results, r => r.IssueType == "SlowQuery");
        Assert.Contains(results, r => r.IssueType == "MissingIndex");
    }

    [Fact]
    public void QueryAnalyzer_GenerateReport_ShouldCreateValidReport()
    {
        // Arrange
        var analyzer = new QueryAnalyzer(_defaultConfig);
        var results = new List<QueryAnalysisResult>
        {
            new() { IssueType = "SlowQuery", ExecutionTimeMs = 1500 },
            new() { IssueType = "N+1", ExecutionTimeMs = 200 },
            new() { IssueType = "MissingIndex", ExecutionTimeMs = 100 }
        };

        // Act
        var report = analyzer.GenerateReport(results);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(3, report.Summary.TotalQueries);
        Assert.Equal(1, report.Summary.SlowQueries);
        Assert.Equal(1, report.Summary.N1Problems);
        Assert.Equal(1, report.Summary.MissingIndexes);
        Assert.Equal(600, report.Summary.AverageExecutionTimeMs);
        Assert.Equal(1500, report.Summary.SlowestQueryMs);
        Assert.Equal(1800, report.Summary.TotalExecutionTimeMs);
    }

    [Fact]
    public void ConsoleLogger_LogAnalysisResult_ShouldNotThrow()
    {
        // Arrange
        var logger = new ConsoleLogger(_defaultConfig);
        var result = new QueryAnalysisResult
        {
            IssueType = "SlowQuery",
            Warning = "Test warning",
            Suggestion = "Test suggestion",
            Severity = "High"
        };

        // Act & Assert - Should not throw
        logger.LogAnalysisResult(result);
        logger.LogAnalysisResults(new List<QueryAnalysisResult> { result });
    }

    [Fact]
    public void ReportGenerator_GenerateDailyReport_ShouldNotThrow()
    {
        // Arrange
        var generator = new ReportGenerator(_defaultConfig);
        var report = new OptimizationReport
        {
            AnalysisResults = new List<QueryAnalysisResult>
            {
                new() { IssueType = "SlowQuery", ExecutionTimeMs = 1500 }
            }
        };

        // Act & Assert - Should not throw
        var task = generator.GenerateDailyReportAsync(report);
        Assert.NotNull(task);
    }

    [Fact]
    public void QueryMetrics_Properties_ShouldBeSetCorrectly()
    {
        // Arrange
        var metrics = new QueryMetrics
        {
            CommandText = "SELECT * FROM Products WHERE Id = @id",
            StartTime = DateTime.UtcNow.AddMilliseconds(-100),
            EndTime = DateTime.UtcNow,
            ExecutionTimeMs = 100,
            IsSelectQuery = true,
            TableNames = new List<string> { "Products" },
            WhereColumns = new List<string> { "Id" }
        };

        // Assert
        Assert.Equal("SELECT * FROM Products WHERE Id = @id", metrics.CommandText);
        Assert.Equal(100, metrics.ExecutionTimeMs);
        Assert.True(metrics.IsSelectQuery);
        Assert.False(metrics.IsInsertQuery);
        Assert.False(metrics.IsUpdateQuery);
        Assert.False(metrics.IsDeleteQuery);
        Assert.Contains("Products", metrics.TableNames);
        Assert.Contains("Id", metrics.WhereColumns);
    }

    [Fact]
    public void QueryPattern_Properties_ShouldBeSetCorrectly()
    {
        // Arrange
        var pattern = new QueryPattern
        {
            Pattern = "SELECT * FROM Products WHERE Id = ?",
            TableName = "Products",
            ExecutionCount = 5,
            AverageExecutionTimeMs = 50.5,
            TotalExecutionTimeMs = 252
        };

        // Assert
        Assert.Equal("SELECT * FROM Products WHERE Id = ?", pattern.Pattern);
        Assert.Equal("Products", pattern.TableName);
        Assert.Equal(5, pattern.ExecutionCount);
        Assert.Equal(50.5, pattern.AverageExecutionTimeMs);
        Assert.Equal(252, pattern.TotalExecutionTimeMs);
    }

    [Fact]
    public void OptimizationReport_Summary_ShouldCalculateCorrectly()
    {
        // Arrange
        var report = new OptimizationReport
        {
            AnalysisResults = new List<QueryAnalysisResult>
            {
                new() { IssueType = "SlowQuery", ExecutionTimeMs = 1000 },
                new() { IssueType = "SlowQuery", ExecutionTimeMs = 2000 },
                new() { IssueType = "N+1", ExecutionTimeMs = 100 },
                new() { IssueType = "MissingIndex", ExecutionTimeMs = 50 }
            }
        };

        // Act
        report.Summary.TotalQueries = report.AnalysisResults.Count;
        report.Summary.SlowQueries = report.AnalysisResults.Count(r => r.IssueType == "SlowQuery");
        report.Summary.N1Problems = report.AnalysisResults.Count(r => r.IssueType == "N+1");
        report.Summary.MissingIndexes = report.AnalysisResults.Count(r => r.IssueType == "MissingIndex");
        report.Summary.AverageExecutionTimeMs = report.AnalysisResults.Average(r => r.ExecutionTimeMs);
        report.Summary.SlowestQueryMs = report.AnalysisResults.Max(r => r.ExecutionTimeMs);
        report.Summary.TotalExecutionTimeMs = report.AnalysisResults.Sum(r => r.ExecutionTimeMs);

        // Assert
        Assert.Equal(4, report.Summary.TotalQueries);
        Assert.Equal(2, report.Summary.SlowQueries);
        Assert.Equal(1, report.Summary.N1Problems);
        Assert.Equal(1, report.Summary.MissingIndexes);
        Assert.Equal(787.5, report.Summary.AverageExecutionTimeMs);
        Assert.Equal(2000, report.Summary.SlowestQueryMs);
        Assert.Equal(3150, report.Summary.TotalExecutionTimeMs);
    }
}
