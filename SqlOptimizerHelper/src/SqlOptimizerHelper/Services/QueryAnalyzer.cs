using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlOptimizerHelper.Models;

namespace SqlOptimizerHelper.Services
{
    /// <summary>
    /// Main orchestrator for query analysis - coordinates all analysis services
    /// </summary>
    public class QueryAnalyzer
    {
        private readonly N1Detector _n1Detector;
        private readonly IndexAnalyzer _indexAnalyzer;
        private readonly PerformanceAnalyzer _performanceAnalyzer;
        private readonly SqlOptimizerConfig _config;

        public QueryAnalyzer(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _n1Detector = new N1Detector(config);
            _indexAnalyzer = new IndexAnalyzer(config);
            _performanceAnalyzer = new PerformanceAnalyzer(config);
        }

        /// <summary>
        /// Analyzes a query and returns optimization suggestions
        /// </summary>
        /// <param name="metrics">Query execution metrics</param>
        /// <returns>List of analysis results with suggestions</returns>
        public List<QueryAnalysisResult> AnalyzeQuery(QueryMetrics metrics)
        {
            var results = new List<QueryAnalysisResult>();

            // Only analyze SELECT queries for optimization opportunities
            if (!metrics.IsSelectQuery)
                return results;

            // Analyze performance (slow queries)
            if (_config.EnableSlowQueryDetection)
            {
                var performanceResult = _performanceAnalyzer.AnalyzePerformance(metrics);
                if (performanceResult != null)
                    results.Add(performanceResult);
            }

            // Analyze for missing indexes
            if (_config.EnableIndexAnalysis)
            {
                var indexResults = _indexAnalyzer.AnalyzeForMissingIndexes(metrics);
                results.AddRange(indexResults);
            }

            return results;
        }

        /// <summary>
        /// Analyzes query patterns for N+1 problems
        /// </summary>
        /// <param name="metrics">Query execution metrics</param>
        /// <returns>Analysis result if N+1 problem detected</returns>
        public QueryAnalysisResult? AnalyzeForN1Problem(QueryMetrics metrics)
        {
            if (!_config.EnableN1Detection || !metrics.IsSelectQuery)
                return null;

            return _n1Detector.DetectN1Problem(metrics);
        }

        /// <summary>
        /// Gets all detected query patterns for analysis
        /// </summary>
        /// <returns>Dictionary of query patterns</returns>
        public Dictionary<string, QueryPattern> GetQueryPatterns()
        {
            return _n1Detector.GetQueryPatterns();
        }

        /// <summary>
        /// Clears all stored analysis data
        /// </summary>
        public void ClearAnalysisData()
        {
            _n1Detector.ClearPatterns();
        }

        /// <summary>
        /// Generates a summary report of all analysis results
        /// </summary>
        /// <param name="results">List of analysis results</param>
        /// <returns>Optimization report</returns>
        public OptimizationReport GenerateReport(List<QueryAnalysisResult> results)
        {
            var report = new OptimizationReport
            {
                ApplicationName = _config.ApplicationName,
                Environment = _config.Environment,
                AnalysisResults = results
            };

            // Calculate summary statistics
            report.Summary.TotalQueries = results.Count;
            report.Summary.SlowQueries = results.Count(r => r.IssueType == "SlowQuery");
            report.Summary.N1Problems = results.Count(r => r.IssueType == "N+1");
            report.Summary.MissingIndexes = results.Count(r => r.IssueType == "MissingIndex");

            if (results.Any())
            {
                report.Summary.AverageExecutionTimeMs = results.Average(r => r.ExecutionTimeMs);
                report.Summary.SlowestQueryMs = results.Max(r => r.ExecutionTimeMs);
                report.Summary.TotalExecutionTimeMs = results.Sum(r => r.ExecutionTimeMs);
            }

            return report;
        }
    }
}
