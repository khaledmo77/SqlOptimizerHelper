using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SqlOptimizerHelper.Models;

namespace SqlOptimizerHelper.Logging
{
    /// <summary>
    /// Generates JSON reports for SQL optimization analysis
    /// </summary>
    public class ReportGenerator
    {
        private readonly SqlOptimizerConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReportGenerator(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true, // Pretty print JSON
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Generates and saves a daily optimization report
        /// </summary>
        /// <param name="report">Optimization report to save</param>
        /// <returns>Path to the saved report file</returns>
        public async Task<string> GenerateDailyReportAsync(OptimizationReport report)
        {
            if (!_config.EnableJsonReports)
                return string.Empty;

            try
            {
                // Ensure log directory exists
                EnsureLogDirectoryExists();

                // Generate filename with timestamp
                var fileName = $"sql-optimizer-report-{DateTime.UtcNow:yyyy-MM-dd}.json";
                var filePath = Path.Combine(_config.LogPath, fileName);

                // Serialize report to JSON
                var jsonContent = JsonSerializer.Serialize(report, _jsonOptions);

                // Write to file
                await File.WriteAllTextAsync(filePath, jsonContent);

                return filePath;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want to break the application
                Console.WriteLine($"[SqlOptimizer] Error generating report: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates and saves a summary report with only the most important findings
        /// </summary>
        /// <param name="results">List of analysis results</param>
        /// <returns>Path to the saved summary file</returns>
        public async Task<string> GenerateSummaryReportAsync(List<QueryAnalysisResult> results)
        {
            if (!_config.EnableJsonReports || results == null || results.Count == 0)
                return string.Empty;

            try
            {
                // Filter to only high and critical severity issues
                var criticalIssues = results.FindAll(r => 
                    r.Severity == "Critical" || r.Severity == "High");

                if (criticalIssues.Count == 0)
                    return string.Empty;

                // Create summary report
                var summaryReport = new OptimizationReport
                {
                    ApplicationName = _config.ApplicationName,
                    Environment = _config.Environment,
                    AnalysisResults = criticalIssues
                };

                // Calculate summary statistics
                summaryReport.Summary.TotalQueries = criticalIssues.Count;
                summaryReport.Summary.SlowQueries = criticalIssues.Count(r => r.IssueType == "SlowQuery");
                summaryReport.Summary.N1Problems = criticalIssues.Count(r => r.IssueType == "N+1");
                summaryReport.Summary.MissingIndexes = criticalIssues.Count(r => r.IssueType == "MissingIndex");

                if (criticalIssues.Count > 0)
                {
                    summaryReport.Summary.AverageExecutionTimeMs = criticalIssues.Average(r => r.ExecutionTimeMs);
                    summaryReport.Summary.SlowestQueryMs = criticalIssues.Max(r => r.ExecutionTimeMs);
                    summaryReport.Summary.TotalExecutionTimeMs = criticalIssues.Sum(r => r.ExecutionTimeMs);
                }

                // Ensure log directory exists
                EnsureLogDirectoryExists();

                // Generate filename
                var fileName = $"sql-optimizer-summary-{DateTime.UtcNow:yyyy-MM-dd-HH-mm}.json";
                var filePath = Path.Combine(_config.LogPath, fileName);

                // Serialize and save
                var jsonContent = JsonSerializer.Serialize(summaryReport, _jsonOptions);
                await File.WriteAllTextAsync(filePath, jsonContent);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SqlOptimizer] Error generating summary report: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Appends analysis results to an existing daily report
        /// </summary>
        /// <param name="newResults">New analysis results to append</param>
        /// <returns>Path to the updated report file</returns>
        public async Task<string> AppendToDailyReportAsync(List<QueryAnalysisResult> newResults)
        {
            if (!_config.EnableJsonReports || newResults == null || newResults.Count == 0)
                return string.Empty;

            try
            {
                var fileName = $"sql-optimizer-report-{DateTime.UtcNow:yyyy-MM-dd}.json";
                var filePath = Path.Combine(_config.LogPath, fileName);

                OptimizationReport existingReport;

                // Load existing report if it exists
                if (File.Exists(filePath))
                {
                    var existingContent = await File.ReadAllTextAsync(filePath);
                    existingReport = JsonSerializer.Deserialize<OptimizationReport>(existingContent, _jsonOptions) 
                        ?? new OptimizationReport();
                }
                else
                {
                    existingReport = new OptimizationReport
                    {
                        ApplicationName = _config.ApplicationName,
                        Environment = _config.Environment
                    };
                }

                // Add new results
                existingReport.AnalysisResults.AddRange(newResults);

                // Update summary statistics
                UpdateReportSummary(existingReport);

                // Save updated report
                var jsonContent = JsonSerializer.Serialize(existingReport, _jsonOptions);
                await File.WriteAllTextAsync(filePath, jsonContent);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SqlOptimizer] Error appending to daily report: {ex.Message}");
                return string.Empty;
            }
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
        /// Updates the summary statistics in a report
        /// </summary>
        /// <param name="report">Report to update</param>
        private void UpdateReportSummary(OptimizationReport report)
        {
            if (report.AnalysisResults == null || report.AnalysisResults.Count == 0)
                return;

            report.Summary.TotalQueries = report.AnalysisResults.Count;
            report.Summary.SlowQueries = report.AnalysisResults.Count(r => r.IssueType == "SlowQuery");
            report.Summary.N1Problems = report.AnalysisResults.Count(r => r.IssueType == "N+1");
            report.Summary.MissingIndexes = report.AnalysisResults.Count(r => r.IssueType == "MissingIndex");

            report.Summary.AverageExecutionTimeMs = report.AnalysisResults.Average(r => r.ExecutionTimeMs);
            report.Summary.SlowestQueryMs = report.AnalysisResults.Max(r => r.ExecutionTimeMs);
            report.Summary.TotalExecutionTimeMs = report.AnalysisResults.Sum(r => r.ExecutionTimeMs);
        }

        /// <summary>
        /// Gets the path to today's report file
        /// </summary>
        /// <returns>Path to today's report file</returns>
        public string GetTodayReportPath()
        {
            var fileName = $"sql-optimizer-report-{DateTime.UtcNow:yyyy-MM-dd}.json";
            return Path.Combine(_config.LogPath, fileName);
        }

        /// <summary>
        /// Checks if today's report file exists
        /// </summary>
        /// <returns>True if today's report exists</returns>
        public bool TodayReportExists()
        {
            return File.Exists(GetTodayReportPath());
        }
    }
}
