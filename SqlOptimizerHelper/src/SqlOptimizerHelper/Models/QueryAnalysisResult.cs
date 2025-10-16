using System;

namespace SqlOptimizerHelper.Models
{
    /// <summary>
    /// Represents the result of analyzing a single SQL query for optimization opportunities
    /// </summary>
    public class QueryAnalysisResult
    {
        /// <summary>
        /// The SQL query that was analyzed
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Warning message describing the issue found
        /// </summary>
        public string Warning { get; set; } = string.Empty;

        /// <summary>
        /// Specific suggestion for optimization
        /// </summary>
        public string Suggestion { get; set; } = string.Empty;

        /// <summary>
        /// When the query was executed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Type of issue detected (SlowQuery, N+1, MissingIndex, etc.)
        /// </summary>
        public string IssueType { get; set; } = string.Empty;

        /// <summary>
        /// Table name affected by the query
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Column name that might need an index
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Severity level of the issue (Low, Medium, High, Critical)
        /// </summary>
        public string Severity { get; set; } = "Medium";

        /// <summary>
        /// Additional context about the query execution
        /// </summary>
        public string Context { get; set; } = string.Empty;
    }
}
