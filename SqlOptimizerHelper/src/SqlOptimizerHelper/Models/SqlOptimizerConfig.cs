using System;

namespace SqlOptimizerHelper.Models
{
    /// <summary>
    /// Configuration options for the SqlOptimizerHelper
    /// </summary>
    public class SqlOptimizerConfig
    {
        /// <summary>
        /// Threshold in milliseconds for considering a query as slow (default: 1000ms)
        /// </summary>
        public long SlowQueryThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Enable N+1 query detection (default: true)
        /// </summary>
        public bool EnableN1Detection { get; set; } = true;

        /// <summary>
        /// Enable missing index detection (default: true)
        /// </summary>
        public bool EnableIndexAnalysis { get; set; } = true;

        /// <summary>
        /// Enable slow query detection (default: true)
        /// </summary>
        public bool EnableSlowQueryDetection { get; set; } = true;

        /// <summary>
        /// Path where log files will be written (default: "./logs")
        /// </summary>
        public string LogPath { get; set; } = "./logs";

        /// <summary>
        /// Enable console output for immediate feedback (default: true)
        /// </summary>
        public bool EnableConsoleOutput { get; set; } = true;

        /// <summary>
        /// Enable JSON report generation (default: true)
        /// </summary>
        public bool EnableJsonReports { get; set; } = true;

        /// <summary>
        /// Minimum number of similar queries to trigger N+1 detection (default: 5)
        /// </summary>
        public int N1DetectionThreshold { get; set; } = 5;

        /// <summary>
        /// Time window in seconds for grouping queries for N+1 detection (default: 30)
        /// </summary>
        public int N1DetectionTimeWindowSeconds { get; set; } = 30;

        /// <summary>
        /// Enable detailed SQL logging (default: false for security)
        /// </summary>
        public bool EnableDetailedSqlLogging { get; set; } = false;

        /// <summary>
        /// Maximum number of analysis results to keep in memory (default: 1000)
        /// </summary>
        public int MaxResultsInMemory { get; set; } = 1000;

        /// <summary>
        /// Application name for report identification
        /// </summary>
        public string ApplicationName { get; set; } = "Unknown";

        /// <summary>
        /// Environment name (Development, Production, etc.)
        /// </summary>
        public string Environment { get; set; } = "Development";
    }
}
