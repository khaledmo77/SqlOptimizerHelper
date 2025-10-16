using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SqlOptimizerHelper.Models;
using SqlOptimizerHelper.Services;
using SqlOptimizerHelper.Logging;

namespace SqlOptimizerHelper.Interceptors
{
    /// <summary>
    /// Enhanced interceptor that captures SQL queries, measures execution time, and analyzes for optimization opportunities
    /// This interceptor specifically targets database queries (read operations).
    /// Every time EF Core executes a SQL query, it calls one of these methods:
    /// "Middleware for EF Core database operations."
    /// </summary>
    public class QueryInterceptor : DbCommandInterceptor, IDisposable
    {
        private readonly SqlOptimizerConfig _config;
        private readonly QueryAnalyzer _queryAnalyzer;
        private readonly ConsoleLogger _consoleLogger;
        private readonly ReportGenerator _reportGenerator;
        private readonly FileLogger _fileLogger;
        private readonly List<QueryAnalysisResult> _analysisResults = new();
        private readonly object _lockObject = new object();

        // Dictionary to track query execution start times
        private readonly Dictionary<string, Stopwatch> _executionTimers = new();

        public QueryInterceptor(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _queryAnalyzer = new QueryAnalyzer(config);
            _consoleLogger = new ConsoleLogger(config);
            _reportGenerator = new ReportGenerator(config);
            _fileLogger = new FileLogger(config);
            
            // Debug logging to confirm interceptor is created
            Console.WriteLine($"[SQL Optimizer] 🔥 QueryInterceptor created with config: SlowQueryThreshold={config.SlowQueryThresholdMs}ms, EnableN1Detection={config.EnableN1Detection}");
        }

        /// <summary>
        /// This method is called before a query is executed that returns a DbDataReader.
        /// We start timing the query execution here.
        /// </summary>
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, // Represents a SQL command to be executed against a database
            CommandEventData eventData, // Contains metadata about the command execution event
            InterceptionResult<DbDataReader> result) // Represents the result of the interception, allowing modification or short-circuiting of the operation
        {
            // Always log that the interceptor is working
            Console.WriteLine($"[SQL Optimizer] 🔥 ReaderExecuting called for: {command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length))}...");

            // Start timing the query execution
            var timerKey = GetTimerKey(command, eventData);
            var stopwatch = Stopwatch.StartNew();
            _executionTimers[timerKey] = stopwatch;

            // Log SQL execution if detailed logging is enabled
            if (_config.EnableDetailedSqlLogging)
            {
                _consoleLogger.LogInfo($"Executing SQL: {command.CommandText}");
            }

            return base.ReaderExecuting(command, eventData, result);
        }

        /// <summary>
        /// This method is called when a data reader is being disposed (sync version).
        /// We analyze the query here for optimization opportunities.
        /// </summary>
        public override InterceptionResult DataReaderDisposing(
            DbCommand command,
            DataReaderDisposingEventData eventData,
            InterceptionResult result)
        {
            // Always log that the interceptor is working
            Console.WriteLine($"[SQL Optimizer] 🔥 DataReaderDisposing called for: {command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length))}...");

            // Debug logging
            if (_config.EnableDetailedSqlLogging)
            {
                _consoleLogger.LogInfo($"DataReaderDisposing called for: {command.CommandText}");
            }

            // Analyze the completed query
            AnalyzeQueryExecutionFromDisposing(command, eventData);

            return base.DataReaderDisposing(command, eventData, result);
        }


        /// <summary>
        /// This method is called after a query is executed that returns a DbDataReader (sync version).
        /// We analyze the query here for optimization opportunities.
        /// </summary>
        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            // Always log that the interceptor is working
            Console.WriteLine($"[SQL Optimizer] 🔥 ReaderExecuted called for: {command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length))}...");

            // Analyze the completed query
            AnalyzeQueryExecution(command, eventData, result);

            return base.ReaderExecuted(command, eventData, result);
        }

        /// <summary>
        /// Async counterpart of ReaderExecuted to ensure analysis also happens in async flows.
        /// </summary>
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            // Always log that the interceptor is working
            Console.WriteLine($"[SQL Optimizer] 🔥 ReaderExecutedAsync called for: {command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length))}...");

            // Analyze the completed query
            AnalyzeQueryExecution(command, eventData, result);

            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }


        /// <summary>
        /// This is the asynchronous version of the same hook.
        /// It's called whenever EF Core uses async query methods, allowing you to handle query execution in an async context.
        /// We start timing the query execution here.
        /// </summary>
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
         DbCommand command,
         CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
        {
            // Always log that the interceptor is working
            Console.WriteLine($"[SQL Optimizer] 🔥 ReaderExecutingAsync called for: {command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length))}...");

            // Start timing the query execution
            var timerKey = GetTimerKey(command, eventData);
            var stopwatch = Stopwatch.StartNew();
            _executionTimers[timerKey] = stopwatch;

            // Log SQL execution if detailed logging is enabled
            if (_config.EnableDetailedSqlLogging)
            {
                _consoleLogger.LogInfo($"Executing SQL Async: {command.CommandText}");
            }

            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }




        /// <summary>
        /// Analyzes the executed query for optimization opportunities from DataReaderDisposing event
        /// </summary>
        /// <param name="command">SQL command that was executed</param>
        /// <param name="eventData">Event data containing execution metadata</param>
        private void AnalyzeQueryExecutionFromDisposing(DbCommand command, DataReaderDisposingEventData eventData)
        {
            try
            {
                // Get execution timer
                var timerKey = GetTimerKeyForDisposing(command, eventData);
                if (!_executionTimers.TryGetValue(timerKey, out var stopwatch))
                    return;

                stopwatch.Stop();
                _executionTimers.Remove(timerKey);

                // Create query metrics
                var metrics = CreateQueryMetricsFromDisposing(command, eventData, stopwatch);

                // Debug logging
                if (_config.EnableDetailedSqlLogging)
                {
                    _consoleLogger.LogInfo($"Analyzing query: {metrics.CommandText} (Execution time: {metrics.ExecutionTimeMs}ms)");
                }

                // Analyze the query for optimization opportunities
                var analysisResults = _queryAnalyzer.AnalyzeQuery(metrics);

                // Check for N+1 problems
                var n1Result = _queryAnalyzer.AnalyzeForN1Problem(metrics);
                if (n1Result != null)
                {
                    analysisResults.Add(n1Result);
                }

                // Process analysis results
                if (analysisResults.Count > 0)
                {
                    ProcessAnalysisResults(analysisResults);
                }
                else if (_config.EnableDetailedSqlLogging)
                {
                    _consoleLogger.LogInfo($"No optimization issues found for query: {metrics.CommandText}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want to break the application
                _consoleLogger.LogWarning($"Error analyzing query: {ex.Message}", "Low");
            }
        }

        /// <summary>
        /// Analyzes the executed query for optimization opportunities
        /// </summary>
        /// <param name="command">SQL command that was executed</param>
        /// <param name="eventData">Event data containing execution metadata</param>
        /// <param name="result">Query result (can be null if query failed)</param>
        private void AnalyzeQueryExecution(DbCommand command, CommandEventData eventData, DbDataReader? result)
        {
            try
            {
                // Get execution timer
                var timerKey = GetTimerKey(command, eventData);
                if (!_executionTimers.TryGetValue(timerKey, out var stopwatch))
                    return;

                stopwatch.Stop();
                _executionTimers.Remove(timerKey);

                // Create query metrics
                var metrics = CreateQueryMetrics(command, eventData, stopwatch);

                // Debug logging
                if (_config.EnableDetailedSqlLogging)
                {
                    _consoleLogger.LogInfo($"Analyzing query: {metrics.CommandText} (Execution time: {metrics.ExecutionTimeMs}ms)");
                }

                // Analyze the query for optimization opportunities
                var analysisResults = _queryAnalyzer.AnalyzeQuery(metrics);

                // Check for N+1 problems
                var n1Result = _queryAnalyzer.AnalyzeForN1Problem(metrics);
                if (n1Result != null)
                {
                    analysisResults.Add(n1Result);
                }

                // Process analysis results
                if (analysisResults.Count > 0)
                {
                    ProcessAnalysisResults(analysisResults);
                }
                else if (_config.EnableDetailedSqlLogging)
                {
                    _consoleLogger.LogInfo($"No optimization issues found for query: {metrics.CommandText}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want to break the application
                _consoleLogger.LogWarning($"Error analyzing query: {ex.Message}", "Low");
            }
        }



        /// <summary>
        /// Creates query metrics from the executed command (from disposing event)
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <param name="eventData">Event data</param>
        /// <param name="stopwatch">Execution timer</param>
        /// <returns>Query metrics</returns>
        private QueryMetrics CreateQueryMetricsFromDisposing(DbCommand command, DataReaderDisposingEventData eventData, Stopwatch stopwatch)
        {
            var metrics = new QueryMetrics
            {
                CommandText = command.CommandText,
                StartTime = eventData.StartTime.DateTime,
                EndTime = eventData.StartTime.DateTime.AddMilliseconds(stopwatch.ElapsedMilliseconds),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                RequestId = eventData.Context?.ContextId.ToString() ?? Guid.NewGuid().ToString()
            };

            // Extract parameters
            foreach (DbParameter parameter in command.Parameters)
            {
                metrics.Parameters[parameter.ParameterName] = parameter.Value ?? DBNull.Value;
            }

            // Determine query type
            var upperQuery = command.CommandText.ToUpper().Trim();
            metrics.IsSelectQuery = upperQuery.StartsWith("SELECT");
            metrics.IsInsertQuery = upperQuery.StartsWith("INSERT");
            metrics.IsUpdateQuery = upperQuery.StartsWith("UPDATE");
            metrics.IsDeleteQuery = upperQuery.StartsWith("DELETE");

            // Extract table names
            metrics.TableNames = ExtractTableNames(command.CommandText);

            // Extract WHERE columns
            metrics.WhereColumns = ExtractWhereColumns(command.CommandText);

            return metrics;
        }

        /// <summary>
        /// Creates query metrics from the executed command
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <param name="eventData">Event data</param>
        /// <param name="stopwatch">Execution timer</param>
        /// <returns>Query metrics</returns>
        private QueryMetrics CreateQueryMetrics(DbCommand command, CommandEventData eventData, Stopwatch stopwatch)
        {
            var metrics = new QueryMetrics
            {
                CommandText = command.CommandText,
                StartTime = eventData.StartTime.DateTime,
                EndTime = eventData.StartTime.DateTime.AddMilliseconds(stopwatch.ElapsedMilliseconds),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                RequestId = eventData.Context?.ContextId.ToString() ?? Guid.NewGuid().ToString()
            };

            // Extract parameters
            foreach (DbParameter parameter in command.Parameters)
            {
                metrics.Parameters[parameter.ParameterName] = parameter.Value ?? DBNull.Value;
            }

            // Determine query type
            var upperQuery = command.CommandText.ToUpper().Trim();
            metrics.IsSelectQuery = upperQuery.StartsWith("SELECT");
            metrics.IsInsertQuery = upperQuery.StartsWith("INSERT");
            metrics.IsUpdateQuery = upperQuery.StartsWith("UPDATE");
            metrics.IsDeleteQuery = upperQuery.StartsWith("DELETE");

            // Extract table names
            metrics.TableNames = ExtractTableNames(command.CommandText);

            // Extract WHERE columns
            metrics.WhereColumns = ExtractWhereColumns(command.CommandText);

            return metrics;
        }

        /// <summary>
        /// Processes analysis results by logging and storing them
        /// </summary>
        /// <param name="results">Analysis results to process</param>
        private void ProcessAnalysisResults(List<QueryAnalysisResult> results)
        {
            lock (_lockObject)
            {
                // Add to stored results
                _analysisResults.AddRange(results);

                // Limit memory usage by removing old results
                if (_analysisResults.Count > _config.MaxResultsInMemory)
                {
                    var toRemove = _analysisResults.Count - _config.MaxResultsInMemory;
                    _analysisResults.RemoveRange(0, toRemove);
                }
            }

            // Log to console
            _consoleLogger.LogAnalysisResults(results);

            // Log to file asynchronously (don't await to avoid blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _fileLogger.AppendToDailyLogAsync(results);
                }
                catch (Exception ex)
                {
                    _consoleLogger.LogWarning($"Error writing to log file: {ex.Message}", "Low");
                }
            });

            // Generate reports asynchronously (don't await to avoid blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _reportGenerator.AppendToDailyReportAsync(results);
                }
                catch (Exception ex)
                {
                    _consoleLogger.LogWarning($"Error generating report: {ex.Message}", "Low");
                }
            });
        }

        /// <summary>
        /// Extracts table names from SQL query
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>List of table names</returns>
        private List<string> ExtractTableNames(string query)
        {
            var tableNames = new List<string>();

            if (string.IsNullOrEmpty(query))
                return tableNames;

            // Match FROM table_name patterns
            var fromMatches = Regex.Matches(query, @"FROM\s+(\w+)", RegexOptions.IgnoreCase);
            foreach (Match match in fromMatches)
            {
                tableNames.Add(match.Groups[1].Value);
            }

            // Match JOIN table_name patterns
            var joinMatches = Regex.Matches(query, @"JOIN\s+(\w+)", RegexOptions.IgnoreCase);
            foreach (Match match in joinMatches)
            {
                tableNames.Add(match.Groups[1].Value);
            }

            return tableNames.Distinct().ToList();
        }

        /// <summary>
        /// Extracts column names from WHERE clauses
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>List of column names</returns>
        private List<string> ExtractWhereColumns(string query)
        {
            var columns = new List<string>();

            if (string.IsNullOrEmpty(query))
                return columns;

            // Find WHERE clause
            var whereMatch = Regex.Match(query, @"WHERE\s+(.+?)(?:\s+ORDER\s+BY|\s+GROUP\s+BY|\s+HAVING|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!whereMatch.Success)
                return columns;

            var whereClause = whereMatch.Groups[1].Value;

            // Extract column names from conditions
            var columnMatches = Regex.Matches(whereClause, @"(\w+)\s*(?:=|LIKE|>|<|>=|<=|IN|BETWEEN|IS)", RegexOptions.IgnoreCase);
            foreach (Match match in columnMatches)
            {
                columns.Add(match.Groups[1].Value);
            }

            return columns.Distinct().ToList();
        }

        /// <summary>
        /// Generates a unique key for tracking query execution timers
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <param name="eventData">Event data</param>
        /// <returns>Unique timer key</returns>
        private string GetTimerKey(DbCommand command, CommandEventData eventData)
        {
            return $"{command.GetHashCode()}_{eventData.Context?.ContextId}_{eventData.StartTime.Ticks}";
        }

        /// <summary>
        /// Gets all stored analysis results
        /// </summary>
        /// <returns>List of analysis results</returns>
        public List<QueryAnalysisResult> GetAnalysisResults()
        {
            lock (_lockObject)
            {
                return new List<QueryAnalysisResult>(_analysisResults);
            }
        }

        /// <summary>
        /// Clears all stored analysis results
        /// </summary>
        public void ClearAnalysisResults()
        {
            lock (_lockObject)
            {
                _analysisResults.Clear();
            }
        }

        /// <summary>
        /// Generates a final report of all analysis results
        /// </summary>
        /// <returns>Optimization report</returns>
        public OptimizationReport GenerateFinalReport()
        {
            var results = GetAnalysisResults();
            return _queryAnalyzer.GenerateReport(results);
        }

        /// <summary>
        /// Generates a unique key for tracking query execution (from disposing event)
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <param name="eventData">Event data</param>
        /// <returns>Unique timer key</returns>
        private string GetTimerKeyForDisposing(DbCommand command, DataReaderDisposingEventData eventData)
        {
            // Use the same format/order as GetTimerKey to ensure we can find the stopwatch
            return $"{command.GetHashCode()}_{eventData.Context?.ContextId}_{eventData.StartTime.Ticks}";
        }

        /// <summary>
        /// Disposes the interceptor and cleans up resources
        /// </summary>
        public void Dispose()
        {
            // Clean up resources if needed
        }
    }
}
