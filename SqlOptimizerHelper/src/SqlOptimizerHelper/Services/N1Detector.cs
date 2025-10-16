using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlOptimizerHelper.Models;

namespace SqlOptimizerHelper.Services
{
    /// <summary>
    /// Detects N+1 query problems by analyzing query patterns
    /// </summary>
    public class N1Detector
    {
        private readonly SqlOptimizerConfig _config;
        private readonly Dictionary<string, QueryPattern> _queryPatterns = new();
        private readonly object _lockObject = new object();

        public N1Detector(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Detects N+1 query problems by tracking repeated query patterns
        /// </summary>
        /// <param name="metrics">Query execution metrics</param>
        /// <returns>Analysis result if N+1 problem detected</returns>
        public QueryAnalysisResult? DetectN1Problem(QueryMetrics metrics)
        {
            if (!metrics.IsSelectQuery)
                return null;

            // Create a normalized pattern by replacing parameters with placeholders
            var pattern = NormalizeQueryPattern(metrics.CommandText);
            var tableName = ExtractTableName(metrics.CommandText);

            lock (_lockObject)
            {
                // Get or create pattern tracking
                if (!_queryPatterns.ContainsKey(pattern))
                {
                    _queryPatterns[pattern] = new QueryPattern
                    {
                        Pattern = pattern,
                        TableName = tableName
                    };
                }

                var queryPattern = _queryPatterns[pattern];
                var now = DateTime.UtcNow;

                // Add execution time
                queryPattern.ExecutionTimes.Add(now);
                queryPattern.ExecutionCount++;
                queryPattern.TotalExecutionTimeMs += metrics.ExecutionTimeMs;
                queryPattern.AverageExecutionTimeMs = queryPattern.TotalExecutionTimeMs / queryPattern.ExecutionCount;

                // Remove old execution times outside the time window
                var cutoffTime = now.AddSeconds(-_config.N1DetectionTimeWindowSeconds);
                queryPattern.ExecutionTimes.RemoveAll(t => t < cutoffTime);

                // Check if this pattern exceeds the N+1 threshold
                if (queryPattern.ExecutionTimes.Count >= _config.N1DetectionThreshold)
                {
                    return new QueryAnalysisResult
                    {
                        Query = metrics.CommandText,
                        ExecutionTimeMs = metrics.ExecutionTimeMs,
                        IssueType = "N+1",
                        Severity = "High",
                        TableName = tableName,
                        Warning = $"N+1 Query Detected: {tableName} queried {queryPattern.ExecutionTimes.Count} times in {_config.N1DetectionTimeWindowSeconds} seconds.",
                        Suggestion = GenerateN1Suggestion(tableName, pattern),
                        Context = $"Pattern: {pattern}, Executions: {queryPattern.ExecutionTimes.Count}, Avg Time: {queryPattern.AverageExecutionTimeMs:F2}ms"
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all tracked query patterns
        /// </summary>
        /// <returns>Dictionary of query patterns</returns>
        public Dictionary<string, QueryPattern> GetQueryPatterns()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, QueryPattern>(_queryPatterns);
            }
        }

        /// <summary>
        /// Clears all stored query patterns
        /// </summary>
        public void ClearPatterns()
        {
            lock (_lockObject)
            {
                _queryPatterns.Clear();
            }
        }

        /// <summary>
        /// Normalizes a SQL query by replacing parameters with placeholders
        /// </summary>
        /// <param name="query">Original SQL query</param>
        /// <returns>Normalized query pattern</returns>
        private string NormalizeQueryPattern(string query)
        {
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            // Remove extra whitespace
            var normalized = Regex.Replace(query, @"\s+", " ").Trim();

            // Replace parameter values with placeholders
            // Pattern: @paramName = 'value' or @paramName = 123
            normalized = Regex.Replace(normalized, @"@\w+\s*=\s*'[^']*'", "@param = '?'");
            normalized = Regex.Replace(normalized, @"@\w+\s*=\s*\d+", "@param = ?");
            normalized = Regex.Replace(normalized, @"@\w+\s*=\s*NULL", "@param = NULL");

            // Replace IN clause values
            normalized = Regex.Replace(normalized, @"IN\s*\([^)]+\)", "IN (?)");

            return normalized;
        }

        /// <summary>
        /// Extracts the main table name from a SQL query
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>Table name or empty string</returns>
        private string ExtractTableName(string query)
        {
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            // Match FROM table_name or JOIN table_name patterns
            var fromMatch = Regex.Match(query, @"FROM\s+(\w+)", RegexOptions.IgnoreCase);
            if (fromMatch.Success)
                return fromMatch.Groups[1].Value;

            var joinMatch = Regex.Match(query, @"JOIN\s+(\w+)", RegexOptions.IgnoreCase);
            if (joinMatch.Success)
                return joinMatch.Groups[1].Value;

            return string.Empty;
        }

        /// <summary>
        /// Generates a suggestion for fixing N+1 query problems
        /// </summary>
        /// <param name="tableName">Table being queried</param>
        /// <param name="pattern">Query pattern</param>
        /// <returns>Optimization suggestion</returns>
        private string GenerateN1Suggestion(string tableName, string pattern)
        {
            // Check if this looks like a foreign key lookup
            if (pattern.Contains("WHERE") && pattern.Contains("="))
            {
                return $"Use 'Include()' to fetch related {tableName} data in one query. " +
                       $"Example: context.Orders.Include(o => o.{tableName}).ToListAsync()";
            }

            return $"Consider using batch loading or 'Include()' to reduce the number of queries to {tableName}";
        }
    }
}
