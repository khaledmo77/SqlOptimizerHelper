using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics; //This namespace gives you access to EF Core diagnostic tools, including:DbCommandInterceptor CommandEventData InterceptionResult
namespace SqlOptimizerHelper.Interceptors
{
    //This interceptor specifically targets database queries (read operations).
    //Every time EF Core executes a SQL query, it calls one of these methods:
    //“Middleware for EF Core database operations.”
    public class QueryInterceptor : DbCommandInterceptor
    {
        // This method is called before a query is executed that returns a DbDataReader.
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, //represent a SQL command to be executed against a database.
            CommandEventData eventData,//contains metadata about the command execution event.
            InterceptionResult<DbDataReader> result)//represents the result of the interception, allowing modification or short-circuiting of the operation.
        {
            Console.WriteLine($"[SqlOptimizer] Executing SQL: {command.CommandText}");
            return base.ReaderExecuting(command, eventData, result);
        }
        //This is the asynchronous version of the same hook.
        //It’s called whenever EF Core uses async query methods, It allows you to handle query execution in an async context.

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
         DbCommand command,
         CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[SqlOptimizer] Executing SQL Async: {command.CommandText}");
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
 

}
