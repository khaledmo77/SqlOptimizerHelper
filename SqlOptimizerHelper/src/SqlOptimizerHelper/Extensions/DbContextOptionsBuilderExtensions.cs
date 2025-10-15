using Microsoft.EntityFrameworkCore;
using SqlOptimizerHelper.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlOptimizerHelper.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        // options object is an instance of DbContextOptionsBuilder, which EF Core uses to collect all the configurations you apply before creating the actual DbContext.
        public static DbContextOptionsBuilder AddSqlOptimizer(this DbContextOptionsBuilder optionsBuilder) //Extension method for DbContextOptionsBuilder
        {
            SqlOptimizerService.Register(optionsBuilder);
            Console.WriteLine("[SqlOptimizerHelper] Initialized EF Core Optimizer Interceptors...");

            return optionsBuilder;

        }
    }
}
