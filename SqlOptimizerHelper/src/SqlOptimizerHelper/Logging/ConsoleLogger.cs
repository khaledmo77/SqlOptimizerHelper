using System;
using System.Collections.Generic;
using System.Linq;
using SqlOptimizerHelper.Models;

namespace SqlOptimizerHelper.Logging
{
    /// <summary>
    /// Provides console output for SQL optimization warnings and suggestions
    /// </summary>
    public class ConsoleLogger
    {
        private readonly SqlOptimizerConfig _config;

        public ConsoleLogger(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Logs a single analysis result to console
        /// </summary>
        /// <param name="result">Analysis result to log</param>
        public void LogAnalysisResult(QueryAnalysisResult result)
        {
            if (!_config.EnableConsoleOutput || result == null)
                return;

            // Choose color based on severity
            var color = GetSeverityColor(result.Severity);
            var prefix = GetSeverityPrefix(result.Severity);

            Console.ForegroundColor = color;
            Console.WriteLine($"[SQL Optimizer] {prefix} {result.IssueType} detected");
            
            // Log warning message
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"    Warning: {result.Warning}");
            
            // Log suggestion
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    Suggestion: {result.Suggestion}");
            
            // Log context if available
            if (!string.IsNullOrEmpty(result.Context))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"    Context: {result.Context}");
            }

            // Reset color
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Logs multiple analysis results to console
        /// </summary>
        /// <param name="results">List of analysis results to log</param>
        public void LogAnalysisResults(List<QueryAnalysisResult> results)
        {
            if (!_config.EnableConsoleOutput || results == null || results.Count == 0)
                return;

            // Group by issue type for better organization
            var groupedResults = results.GroupBy(r => r.IssueType);

            foreach (var group in groupedResults)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[SQL Optimizer] {group.Count()} {group.Key} issue(s) found:");
                Console.ResetColor();

                foreach (var result in group)
                {
                    LogAnalysisResult(result);
                }
            }
        }

        /// <summary>
        /// Logs a summary of the optimization report
        /// </summary>
        /// <param name="report">Optimization report to summarize</param>
        public void LogReportSummary(OptimizationReport report)
        {
            if (!_config.EnableConsoleOutput || report == null)
                return;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("=== SQL Optimizer Report Summary ===");
            Console.ResetColor();

            Console.WriteLine($"Application: {report.ApplicationName}");
            Console.WriteLine($"Environment: {report.Environment}");
            Console.WriteLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();

            // Summary statistics
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Statistics:");
            Console.ResetColor();
            Console.WriteLine($"  Total Queries Analyzed: {report.Summary.TotalQueries}");
            Console.WriteLine($"  Slow Queries: {report.Summary.SlowQueries}");
            Console.WriteLine($"  N+1 Problems: {report.Summary.N1Problems}");
            Console.WriteLine($"  Missing Indexes: {report.Summary.MissingIndexes}");
            Console.WriteLine($"  Average Execution Time: {report.Summary.AverageExecutionTimeMs:F2}ms");
            Console.WriteLine($"  Slowest Query: {report.Summary.SlowestQueryMs}ms");
            Console.WriteLine($"  Total Execution Time: {report.Summary.TotalExecutionTimeMs}ms");
            Console.WriteLine();

            // Issue breakdown
            if (report.AnalysisResults.Any())
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Issues by Severity:");
                Console.ResetColor();

                var severityGroups = report.AnalysisResults.GroupBy(r => r.Severity);
                foreach (var group in severityGroups.OrderByDescending(g => GetSeverityOrder(g.Key)))
                {
                    var color = GetSeverityColor(group.Key);
                    Console.ForegroundColor = color;
                    Console.WriteLine($"  {group.Key}: {group.Count()}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Logs initialization message
        /// </summary>
        public void LogInitialization()
        {
            if (!_config.EnableConsoleOutput)
                return;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[SQL Optimizer] ⚡ Initialized EF Core Optimizer Interceptors...");
            Console.WriteLine($"[SQL Optimizer] 📊 Slow query threshold: {_config.SlowQueryThresholdMs}ms");
            Console.WriteLine($"[SQL Optimizer] 🔍 N+1 detection: {(_config.EnableN1Detection ? "Enabled" : "Disabled")}");
            Console.WriteLine($"[SQL Optimizer] 📈 Index analysis: {(_config.EnableIndexAnalysis ? "Enabled" : "Disabled")}");
            Console.WriteLine($"[SQL Optimizer] 📝 Reports: {(_config.EnableJsonReports ? "Enabled" : "Disabled")}");
            if (_config.EnableJsonReports)
            {
                Console.WriteLine($"[SQL Optimizer] 📁 Log path: {_config.LogPath}");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Logs a simple warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="severity">Severity level</param>
        public void LogWarning(string message, string severity = "Medium")
        {
            if (!_config.EnableConsoleOutput || string.IsNullOrEmpty(message))
                return;

            var color = GetSeverityColor(severity);
            var prefix = GetSeverityPrefix(severity);

            Console.ForegroundColor = color;
            Console.WriteLine($"[SQL Optimizer] {prefix} {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">Info message</param>
        public void LogInfo(string message)
        {
            if (!_config.EnableConsoleOutput || string.IsNullOrEmpty(message))
                return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[SQL Optimizer] ℹ️ {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Gets console color based on severity level
        /// </summary>
        /// <param name="severity">Severity level</param>
        /// <returns>Console color</returns>
        private ConsoleColor GetSeverityColor(string severity)
        {
            return severity.ToLower() switch
            {
                "critical" => ConsoleColor.Red,
                "high" => ConsoleColor.DarkRed,
                "medium" => ConsoleColor.Yellow,
                "low" => ConsoleColor.DarkYellow,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Gets prefix symbol based on severity level
        /// </summary>
        /// <param name="severity">Severity level</param>
        /// <returns>Prefix symbol</returns>
        private string GetSeverityPrefix(string severity)
        {
            return severity.ToLower() switch
            {
                "critical" => "🚨",
                "high" => "⚠️",
                "medium" => "⚡",
                "low" => "ℹ️",
                _ => "📊"
            };
        }

        /// <summary>
        /// Gets order value for severity sorting
        /// </summary>
        /// <param name="severity">Severity level</param>
        /// <returns>Order value (higher = more severe)</returns>
        private int GetSeverityOrder(string severity)
        {
            return severity.ToLower() switch
            {
                "critical" => 4,
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                _ => 0
            };
        }
    }
}
