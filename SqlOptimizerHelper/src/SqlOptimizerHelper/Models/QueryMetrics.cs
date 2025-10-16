using System;
using System.Collections.Generic;

namespace SqlOptimizerHelper.Models
{
    /// <summary>
    /// Metrics collected for a single query execution
    /// </summary>
    public class QueryMetrics
    {
        /// <summary>
        /// The SQL command text
        /// </summary>
        public string CommandText { get; set; } = string.Empty;

        /// <summary>
        /// When the query started executing
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// When the query finished executing
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Parameters used in the query
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Table names involved in the query
        /// </summary>
        public List<string> TableNames { get; set; } = new List<string>();

        /// <summary>
        /// Column names involved in WHERE clauses
        /// </summary>
        public List<string> WhereColumns { get; set; } = new List<string>();

        /// <summary>
        /// Whether this is a SELECT query
        /// </summary>
        public bool IsSelectQuery { get; set; }

        /// <summary>
        /// Whether this is an INSERT query
        /// </summary>
        public bool IsInsertQuery { get; set; }

        /// <summary>
        /// Whether this is an UPDATE query
        /// </summary>
        public bool IsUpdateQuery { get; set; }

        /// <summary>
        /// Whether this is a DELETE query
        /// </summary>
        public bool IsDeleteQuery { get; set; }

        /// <summary>
        /// Request context identifier for grouping related queries
        /// </summary>
        public string RequestId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pattern information for N+1 query detection
    /// </summary>
    public class QueryPattern
    {
        /// <summary>
        /// Base query pattern (with parameters replaced by placeholders)
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Table name being queried
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// List of execution times for this pattern
        /// </summary>
        public List<DateTime> ExecutionTimes { get; set; } = new List<DateTime>();

        /// <summary>
        /// Number of times this pattern was executed
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Average execution time for this pattern
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Total execution time for this pattern
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
    }
}
