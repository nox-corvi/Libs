using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Hosting;

public static class Hosting
{
    //public static ILogger<T> CreateD1efaultLogger<T>(bool Debug = true, bool SingleLine = true, bool IncludeScopes = true)
    //    where T : class
    //    => LoggerFactory
    //        .Create(logging =>
    //        {
    //            if (Debug) { logging.AddDebug(); }
    //            logging.AddSimpleConsole(opt =>
    //            {
    //                opt.SingleLine = SingleLine;
    //                opt.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    //                opt.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
    //                opt.IncludeScopes = IncludeScopes;
    //            });
    //        })
    //        .CreateLogger<T>();

    //public static ILogger<T> CreateDefaultXLogger<T>()
    //    where T : class
    //    => LoggerFactory.Create(c => { 
            
    //    }).CreateLogger<T>();
}