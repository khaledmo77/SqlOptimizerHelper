using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SqlOptimizerHelper.Models;

namespace SqlOptimizerHelper.Logging
{
    /// <summary>
    /// Provides file-based logging for SQL optimization analysis
    /// </summary>
    public class FileLogger
    {
        private readonly SqlOptimizerConfig _config;
        private readonly object _lockObject = new object();

        public FileLogger(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Logs analysis results to a text file
        /// </summary>
        /// <param name="results">Analysis results to log</param>
        /// <returns>Path to the log file</returns>
        public async Task<string> LogAnalysisResultsAsync(List<QueryAnalysisResult> results)
        {
            if (results == null || results.Count == 0)
                return string.Empty;

            try
            {
                // Ensure log directory exists
                EnsureLogDirectoryExists();

                // Generate filename with timestamp
                var fileName = $"sql-optimizer-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.log";
                var filePath = Path.Combine(_config.LogPath, fileName);

                // Build log content
                var logContent = BuildLogContent(results);

                // Write to file
                await File.WriteAllTextAsync(filePath, logContent);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SqlOptimizer] Error writing log file: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Appends analysis results to the daily log file
        /// </summary>
        /// <param name="results">Analysis results to append</param>
        /// <returns>Path to the log file</returns>
        public async Task<string> AppendToDailyLogAsync(List<QueryAnalysisResult> results)
        {
            if (results == null || results.Count == 0)
                return string.Empty;

            try
            {
                // Ensure log directory exists
                EnsureLogDirectoryExists();

                // Generate daily filename
                var fileName = $"sql-optimizer-{DateTime.UtcNow:yyyy-MM-dd}.log";
                var filePath = Path.Combine(_config.LogPath, fileName);

                // Build log content
                var logContent = BuildLogContent(results);

                // Append to file
                lock (_lockObject)
                {
                    File.AppendAllText(filePath, logContent);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SqlOptimizer] Error appending to daily log: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Logs a single analysis result to file
        /// </summary>
        /// <param name="result">Analysis result to log</param>
        /// <returns>Path to the log file</returns>
        public async Task<string> LogSingleResultAsync(QueryAnalysisResult result)
        {
            if (result == null)
                return string.Empty;

            return await LogAnalysisResultsAsync(new List<QueryAnalysisResult> { result });
        }

        /// <summary>
        /// Logs system information and configuration
        /// </summary>
        /// <returns>Path to the log file</returns>
        public async Task<string> LogSystemInfoAsync()
        {
            try
            {
                // Ensure log directory exists
                EnsureLogDirectoryExists();

                // Generate filename
                var fileName = $"sql-optimizer-system-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.log";
                var filePath = Path.Combine(_config.LogPath, fileName);

                // Build system info content
                var systemInfo = BuildSystemInfoContent();

                // Write to file
                await File.WriteAllTextAsync(filePath, systemInfo);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SqlOptimizer] Error writing system info: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds log content from analysis results
        /// </summary>
        /// <param name="results">Analysis results</param>
        /// <returns>Formatted log content</returns>
        private string BuildLogContent(List<QueryAnalysisResult> results)
        {
            var sb = new StringBuilder();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

            // Header
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"SQL Optimizer Analysis Report - {timestamp}");
            sb.AppendLine($"Application: {_config.ApplicationName}");
            sb.AppendLine($"Environment: {_config.Environment}");
            sb.AppendLine($"Total Issues Found: {results.Count}");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            // Group by issue type
            var groupedResults = results.GroupBy(r => r.IssueType);

            foreach (var group in groupedResults)
            {
                sb.AppendLine($"## {group.Key} Issues ({group.Count()})");
                sb.AppendLine("-".PadRight(40, '-'));

                foreach (var result in group)
                {
                    sb.AppendLine($"Timestamp: {result.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                    sb.AppendLine($"Severity: {result.Severity}");
                    sb.AppendLine($"Execution Time: {result.ExecutionTimeMs}ms");
                    
                    if (!string.IsNullOrEmpty(result.TableName))
                        sb.AppendLine($"Table: {result.TableName}");
                    
                    if (!string.IsNullOrEmpty(result.ColumnName))
                        sb.AppendLine($"Column: {result.ColumnName}");

                    sb.AppendLine($"Warning: {result.Warning}");
                    sb.AppendLine($"Suggestion: {result.Suggestion}");
                    
                    if (!string.IsNullOrEmpty(result.Context))
                        sb.AppendLine($"Context: {result.Context}");

                    // Sanitize SQL for logging (remove sensitive data if configured)
                    var sanitizedQuery = SanitizeQuery(result.Query);
                    sb.AppendLine($"Query: {sanitizedQuery}");
                    
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            // Summary
            sb.AppendLine("## Summary");
            sb.AppendLine("-".PadRight(40, '-'));
            sb.AppendLine($"Total Queries: {results.Count}");
            sb.AppendLine($"Slow Queries: {results.Count(r => r.IssueType == "SlowQuery")}");
            sb.AppendLine($"N+1 Problems: {results.Count(r => r.IssueType == "N+1")}");
            sb.AppendLine($"Missing Indexes: {results.Count(r => r.IssueType == "MissingIndex")}");
            
            if (results.Count > 0)
            {
                sb.AppendLine($"Average Execution Time: {results.Average(r => r.ExecutionTimeMs):F2}ms");
                sb.AppendLine($"Slowest Query: {results.Max(r => r.ExecutionTimeMs)}ms");
                sb.AppendLine($"Total Execution Time: {results.Sum(r => r.ExecutionTimeMs)}ms");
            }

            sb.AppendLine();
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Builds system information content
        /// </summary>
        /// <returns>System information content</returns>
        private string BuildSystemInfoContent()
        {
            var sb = new StringBuilder();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"SQL Optimizer System Information - {timestamp}");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            // Configuration
            sb.AppendLine("## Configuration");
            sb.AppendLine($"Application Name: {_config.ApplicationName}");
            sb.AppendLine($"Environment: {_config.Environment}");
            sb.AppendLine($"Slow Query Threshold: {_config.SlowQueryThresholdMs}ms");
            sb.AppendLine($"N+1 Detection: {_config.EnableN1Detection}");
            sb.AppendLine($"Index Analysis: {_config.EnableIndexAnalysis}");
            sb.AppendLine($"Slow Query Detection: {_config.EnableSlowQueryDetection}");
            sb.AppendLine($"Console Output: {_config.EnableConsoleOutput}");
            sb.AppendLine($"JSON Reports: {_config.EnableJsonReports}");
            sb.AppendLine($"Log Path: {_config.LogPath}");
            sb.AppendLine($"N+1 Detection Threshold: {_config.N1DetectionThreshold}");
            sb.AppendLine($"N+1 Time Window: {_config.N1DetectionTimeWindowSeconds}s");
            sb.AppendLine($"Detailed SQL Logging: {_config.EnableDetailedSqlLogging}");
            sb.AppendLine($"Max Results in Memory: {_config.MaxResultsInMemory}");
            sb.AppendLine();

            // System Information
            sb.AppendLine("## System Information");
            sb.AppendLine($"Machine Name: {Environment.MachineName}");
            sb.AppendLine($"OS Version: {Environment.OSVersion}");
            sb.AppendLine($"Processor Count: {Environment.ProcessorCount}");
            sb.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
            sb.AppendLine($"CLR Version: {Environment.Version}");
            sb.AppendLine();

            // .NET Information
            sb.AppendLine("## .NET Information");
            sb.AppendLine($"Runtime Version: {Environment.Version}");
            sb.AppendLine($"Is 64-bit Process: {Environment.Is64BitProcess}");
            sb.AppendLine($"Is 64-bit Operating System: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine();

            sb.AppendLine("=".PadRight(80, '='));

            return sb.ToString();
        }

        /// <summary>
        /// Sanitizes SQL query for logging (removes sensitive data if configured)
        /// </summary>
        /// <param name="query">Original SQL query</param>
        /// <returns>Sanitized SQL query</returns>
        private string SanitizeQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            if (_config.EnableDetailedSqlLogging)
                return query;

            // Basic sanitization - replace parameter values with placeholders
            var sanitized = query;

            // Replace string parameters
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, 
                @"@\w+\s*=\s*'[^']*'", "@param = '***'");

            // Replace numeric parameters
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, 
                @"@\w+\s*=\s*\d+", "@param = ***");

            return sanitized;
        }

        /// <summary>
        /// Ensures the log directory exists
        /// </summary>
        private void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(_config.LogPath))
            {
                Directory.CreateDirectory(_config.LogPath);
            }
        }

        /// <summary>
        /// Gets the path to today's log file
        /// </summary>
        /// <returns>Path to today's log file</returns>
        public string GetTodayLogPath()
        {
            var fileName = $"sql-optimizer-{DateTime.UtcNow:yyyy-MM-dd}.log";
            return Path.Combine(_config.LogPath, fileName);
        }

        /// <summary>
        /// Checks if today's log file exists
        /// </summary>
        /// <returns>True if today's log exists</returns>
        public bool TodayLogExists()
        {
            return File.Exists(GetTodayLogPath());
        }
    }
}
