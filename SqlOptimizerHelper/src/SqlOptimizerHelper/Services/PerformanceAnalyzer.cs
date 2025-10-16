using System;
using System.Collections.Generic;
using SqlOptimizerHelper.Models;

namespace SqlOptimizerHelper.Services
{
    /// <summary>
    /// Analyzes query performance and detects slow queries
    /// </summary>
    public class PerformanceAnalyzer
    {
        private readonly SqlOptimizerConfig _config;

        public PerformanceAnalyzer(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Analyzes query performance and returns result if query is slow
        /// </summary>
        /// <param name="metrics">Query execution metrics</param>
        /// <returns>Analysis result if query is slow, null otherwise</returns>
        public QueryAnalysisResult? AnalyzePerformance(QueryMetrics metrics)
        {
            if (metrics.ExecutionTimeMs < _config.SlowQueryThresholdMs)
                return null;

            return new QueryAnalysisResult
            {
                Query = metrics.CommandText,
                ExecutionTimeMs = metrics.ExecutionTimeMs,
                IssueType = "SlowQuery",
                Severity = GetSeverityLevel(metrics.ExecutionTimeMs),
                TableName = ExtractTableName(metrics.CommandText),
                Warning = $"Slow query detected: {metrics.ExecutionTimeMs}ms (threshold: {_config.SlowQueryThresholdMs}ms)",
                Suggestion = GeneratePerformanceSuggestion(metrics),
                Context = $"Execution time: {metrics.ExecutionTimeMs}ms, Start: {metrics.StartTime:HH:mm:ss.fff}, End: {metrics.EndTime:HH:mm:ss.fff}"
            };
        }

        /// <summary>
        /// Determines severity level based on execution time
        /// </summary>
        /// <param name="executionTimeMs">Execution time in milliseconds</param>
        /// <returns>Severity level</returns>
        private string GetSeverityLevel(long executionTimeMs)
        {
            if (executionTimeMs >= _config.SlowQueryThresholdMs * 5)
                return "Critical";
            if (executionTimeMs >= _config.SlowQueryThresholdMs * 3)
                return "High";
            if (executionTimeMs >= _config.SlowQueryThresholdMs * 2)
                return "Medium";
            return "Low";
        }

        /// <summary>
        /// Generates performance optimization suggestions
        /// </summary>
        /// <param name="metrics">Query execution metrics</param>
        /// <returns>Performance optimization suggestion</returns>
        private string GeneratePerformanceSuggestion(QueryMetrics metrics)
        {
            var suggestions = new List<string>();

            // Check for SELECT * queries
            if (metrics.CommandText.ToUpper().Contains("SELECT *"))
            {
                suggestions.Add("Avoid SELECT * - specify only needed columns");
            }

            // Check for missing WHERE clause
            if (metrics.IsSelectQuery && !metrics.CommandText.ToUpper().Contains("WHERE"))
            {
                suggestions.Add("Consider adding WHERE clause to limit results");
            }

            // Check for ORDER BY without LIMIT
            if (metrics.CommandText.ToUpper().Contains("ORDER BY") && 
                !metrics.CommandText.ToUpper().Contains("TOP") && 
                !metrics.CommandText.ToUpper().Contains("LIMIT"))
            {
                suggestions.Add("Consider adding TOP/LIMIT clause with ORDER BY");
            }

            // Check for complex joins
            var joinCount = CountJoins(metrics.CommandText);
            if (joinCount > 3)
            {
                suggestions.Add($"Complex query with {joinCount} joins - consider breaking into smaller queries");
            }

            // General suggestions based on execution time
            if (metrics.ExecutionTimeMs > _config.SlowQueryThresholdMs * 2)
            {
                suggestions.Add("Consider adding indexes on frequently queried columns");
                suggestions.Add("Review query execution plan for optimization opportunities");
            }

            // Default suggestion if no specific issues found
            if (suggestions.Count == 0)
            {
                suggestions.Add("Review query execution plan and consider adding indexes");
            }

            return string.Join("; ", suggestions);
        }

        /// <summary>
        /// Counts the number of JOIN operations in a query
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>Number of joins</returns>
        private int CountJoins(string query)
        {
            if (string.IsNullOrEmpty(query))
                return 0;

            var upperQuery = query.ToUpper();
            var joinCount = 0;

            // Count different types of joins
            joinCount += CountOccurrences(upperQuery, "JOIN");
            joinCount += CountOccurrences(upperQuery, "INNER JOIN");
            joinCount += CountOccurrences(upperQuery, "LEFT JOIN");
            joinCount += CountOccurrences(upperQuery, "RIGHT JOIN");
            joinCount += CountOccurrences(upperQuery, "FULL JOIN");
            joinCount += CountOccurrences(upperQuery, "OUTER JOIN");

            return joinCount;
        }

        /// <summary>
        /// Counts occurrences of a substring in a string
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="substring">Substring to count</param>
        /// <returns>Number of occurrences</returns>
        private int CountOccurrences(string text, string substring)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(substring))
                return 0;

            var count = 0;
            var index = 0;

            while ((index = text.IndexOf(substring, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += substring.Length;
            }

            return count;
        }

        /// <summary>
        /// Extracts table name from SQL query
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>Table name or empty string</returns>
        private string ExtractTableName(string query)
        {
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            // Match FROM table_name pattern
            var match = System.Text.RegularExpressions.Regex.Match(query, @"FROM\s+(\w+)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
    }
}
