using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlOptimizerHelper.Services
{
    //this is the main entery point to register our service
    public static class SqlOptimizerService
    {
        public static bool _IsRegistered = false;
        //prevent duplicate registration
        public static void Register(DbContextOptionsBuilder options)
        {
            if (_IsRegistered)
                return;
            options.AddInterceptors(new Interceptors.QueryInterceptor());
            Console.WriteLine("[SqlOptimizerHelper] Initialized EF Core Optimizer Interceptors...");
            _IsRegistered = true;
        }

    }
}
