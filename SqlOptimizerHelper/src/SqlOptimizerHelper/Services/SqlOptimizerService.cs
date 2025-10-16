using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlOptimizerHelper.Models;
using SqlOptimizerHelper.Interceptors;
using SqlOptimizerHelper.Logging;

namespace SqlOptimizerHelper.Services
{
    /// <summary>
    /// Main entry point to register the SqlOptimizerHelper service
    /// This service provides SQL optimization analysis for EF Core applications
    /// </summary>
    public static class SqlOptimizerService
    {
        private static bool _isRegistered = false;
        private static SqlOptimizerConfig? _config;
        private static QueryInterceptor? _interceptor;

        /// <summary>
        /// Registers the SqlOptimizerHelper with default configuration
        /// </summary>
        /// <param name="options">DbContext options builder</param>
        /// <returns>DbContext options builder for chaining</returns>
        public static DbContextOptionsBuilder AddSqlOptimizer(this DbContextOptionsBuilder options)
        {
            return AddSqlOptimizer(options, null);
        }

        /// <summary>
        /// Registers the SqlOptimizerHelper with custom configuration
        /// </summary>
        /// <param name="options">DbContext options builder</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>DbContext options builder for chaining</returns>
        public static DbContextOptionsBuilder AddSqlOptimizer(this DbContextOptionsBuilder options, Action<SqlOptimizerConfig>? configure)
        {
            // Create configuration
            var config = new SqlOptimizerConfig();
            configure?.Invoke(config);

            // Create and register interceptor (always register for each DbContext instance)
            var interceptor = new QueryInterceptor(config);
            options.AddInterceptors(interceptor);

            // Store the first registration for reporting
            if (!_isRegistered)
            {
                _config = config;
                _interceptor = interceptor;
                
                // Log initialization
                var consoleLogger = new ConsoleLogger(config);
                consoleLogger.LogInitialization();
                
                _isRegistered = true;
            }

            return options;
        }

        /// <summary>
        /// Registers the SqlOptimizerHelper with configuration from IConfiguration
        /// </summary>
        /// <param name="options">DbContext options builder</param>
        /// <param name="configuration">Configuration source</param>
        /// <param name="sectionName">Configuration section name (default: "SqlOptimizer")</param>
        /// <returns>DbContext options builder for chaining</returns>
        public static DbContextOptionsBuilder AddSqlOptimizer(this DbContextOptionsBuilder options, IConfiguration configuration, string sectionName = "SqlOptimizer")
        {
            // Create configuration from IConfiguration
            var config = new SqlOptimizerConfig();
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
            {
                config.SlowQueryThresholdMs = section.GetValue<long>(nameof(config.SlowQueryThresholdMs), config.SlowQueryThresholdMs);
                config.EnableN1Detection = section.GetValue<bool>(nameof(config.EnableN1Detection), config.EnableN1Detection);
                config.EnableIndexAnalysis = section.GetValue<bool>(nameof(config.EnableIndexAnalysis), config.EnableIndexAnalysis);
                config.EnableSlowQueryDetection = section.GetValue<bool>(nameof(config.EnableSlowQueryDetection), config.EnableSlowQueryDetection);
                config.EnableConsoleOutput = section.GetValue<bool>(nameof(config.EnableConsoleOutput), config.EnableConsoleOutput);
                config.EnableJsonReports = section.GetValue<bool>(nameof(config.EnableJsonReports), config.EnableJsonReports);
                config.LogPath = section.GetValue<string>(nameof(config.LogPath), config.LogPath);
                config.N1DetectionThreshold = section.GetValue<int>(nameof(config.N1DetectionThreshold), config.N1DetectionThreshold);
                config.N1DetectionTimeWindowSeconds = section.GetValue<int>(nameof(config.N1DetectionTimeWindowSeconds), config.N1DetectionTimeWindowSeconds);
                config.EnableDetailedSqlLogging = section.GetValue<bool>(nameof(config.EnableDetailedSqlLogging), config.EnableDetailedSqlLogging);
                config.MaxResultsInMemory = section.GetValue<int>(nameof(config.MaxResultsInMemory), config.MaxResultsInMemory);
                config.ApplicationName = section.GetValue<string>(nameof(config.ApplicationName), config.ApplicationName);
                config.Environment = section.GetValue<string>(nameof(config.Environment), config.Environment);
            }

            // Create and register interceptor (always register for each DbContext instance)
            var interceptor = new QueryInterceptor(config);
            options.AddInterceptors(interceptor);

            // Store the first registration for reporting
            if (!_isRegistered)
            {
                _config = config;
                _interceptor = interceptor;
                
                // Log initialization
                var consoleLogger = new ConsoleLogger(config);
                consoleLogger.LogInitialization();
                
                _isRegistered = true;
            }
            return options;
        }

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        /// <returns>Current configuration or null if not registered</returns>
        public static SqlOptimizerConfig? GetConfiguration()
        {
            return _config;
        }

        /// <summary>
        /// Gets the current interceptor instance
        /// </summary>
        /// <returns>Current interceptor or null if not registered</returns>
        public static QueryInterceptor? GetInterceptor()
        {
            return _interceptor;
        }

        /// <summary>
        /// Gets all analysis results from the current interceptor
        /// </summary>
        /// <returns>List of analysis results</returns>
        public static List<QueryAnalysisResult> GetAnalysisResults()
        {
            return _interceptor?.GetAnalysisResults() ?? new List<QueryAnalysisResult>();
        }

        /// <summary>
        /// Generates a final optimization report
        /// </summary>
        /// <returns>Optimization report</returns>
        public static OptimizationReport? GenerateReport()
        {
            return _interceptor?.GenerateFinalReport();
        }

        /// <summary>
        /// Clears all stored analysis results
        /// </summary>
        public static void ClearResults()
        {
            _interceptor?.ClearAnalysisResults();
        }

        /// <summary>
        /// Checks if the service is registered
        /// </summary>
        /// <returns>True if registered</returns>
        public static bool IsRegistered()
        {
            return _isRegistered;
        }

        /// <summary>
        /// Resets the registration state (for testing purposes)
        /// </summary>
        internal static void Reset()
        {
            _isRegistered = false;
            _config = null;
            _interceptor = null;
        }
    }
}
