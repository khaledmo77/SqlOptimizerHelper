using System;
using System.Collections.Generic;

namespace SqlOptimizerHelper.Models
{
    /// <summary>
    /// Represents a collection of optimization suggestions and analysis results
    /// </summary>
    public class OptimizationReport
    {
        /// <summary>
        /// When the report was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of all query analysis results
        /// </summary>
        public List<QueryAnalysisResult> AnalysisResults { get; set; } = new List<QueryAnalysisResult>();

        /// <summary>
        /// Summary statistics
        /// </summary>
        public ReportSummary Summary { get; set; } = new ReportSummary();

        /// <summary>
        /// Application or context identifier
        /// </summary>
        public string ApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// Environment where the analysis was performed
        /// </summary>
        public string Environment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Summary statistics for the optimization report
    /// </summary>
    public class ReportSummary
    {
        /// <summary>
        /// Total number of queries analyzed
        /// </summary>
        public int TotalQueries { get; set; }

        /// <summary>
        /// Number of slow queries detected
        /// </summary>
        public int SlowQueries { get; set; }

        /// <summary>
        /// Number of N+1 query problems detected
        /// </summary>
        public int N1Problems { get; set; }

        /// <summary>
        /// Number of missing index suggestions
        /// </summary>
        public int MissingIndexes { get; set; }

        /// <summary>
        /// Average query execution time in milliseconds
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Slowest query execution time in milliseconds
        /// </summary>
        public long SlowestQueryMs { get; set; }

        /// <summary>
        /// Total time spent on all queries in milliseconds
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
    }
}
