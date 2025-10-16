using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SqlOptimizerHelper.Services;
using SqlOptimizerHelper.Models;
using System;

namespace SqlOptimizerHelper.Extensions
{
    /// <summary>
    /// Extension methods for DbContextOptionsBuilder to add SqlOptimizerHelper functionality
    /// </summary>
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Adds SqlOptimizerHelper with default configuration
        /// The options object is an instance of DbContextOptionsBuilder, which EF Core uses to collect all the configurations you apply before creating the actual DbContext.
        /// </summary>
        /// <param name="optionsBuilder">DbContext options builder</param>
        /// <returns>DbContext options builder for chaining</returns>
        public static DbContextOptionsBuilder AddSqlOptimizer(this DbContextOptionsBuilder optionsBuilder)
        {
            return SqlOptimizerService.AddSqlOptimizer(optionsBuilder);
        }

        /// <summary>
        /// Adds SqlOptimizerHelper with custom configuration
        /// </summary>
        /// <param name="optionsBuilder">DbContext options builder</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>DbContext options builder for chaining</returns>
        public static DbContextOptionsBuilder AddSqlOptimizer(this DbContextOptionsBuilder optionsBuilder, Action<SqlOptimizerConfig> configure)
        {
            return SqlOptimizerService.AddSqlOptimizer(optionsBuilder, configure);
        }

        /// <summary>
        /// Adds SqlOptimizerHelper with configuration from IConfiguration
        /// </summary>
        /// <param name="optionsBuilder">DbContext options builder</param>
        /// <param name="configuration">Configuration source</param>
        /// <param name="sectionName">Configuration section name (default: "SqlOptimizer")</param>
        /// <returns>DbContext options builder for chaining</returns>
        public static DbContextOptionsBuilder AddSqlOptimizer(this DbContextOptionsBuilder optionsBuilder, IConfiguration configuration, string sectionName = "SqlOptimizer")
        {
            return SqlOptimizerService.AddSqlOptimizer(optionsBuilder, configuration, sectionName);
        }
    }
}
