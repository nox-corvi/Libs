using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Nox.Hosting;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Win32.Hosting
{
    public class HostedService<T>
    : Nox.Hosting.HostedService<T> where T : class, IBetterBackgroundWorker
    {

        public HostedService(HostedConfiguration hostedConfig, IBetterBackgroundWorker betterBackgroundWorker, IConfiguration configuration, IHostApplicationLifetime hostLifetime, ILogger<T> logger)
            : base(hostedConfig, betterBackgroundWorker, configuration, hostLifetime, logger) { }


        public static void RunService(HostedConfiguration hostedConfig, IBetterBackgroundWorker betterBackgroundWorker) =>
            RunServiceAsync(hostedConfig, betterBackgroundWorker).Wait();

        public static async Task RunServiceAsync(HostedConfiguration hostedConfig, IBetterBackgroundWorker betterBackgroundWorker) =>
            await Host.CreateDefaultBuilder(hostedConfig?.CommandlineArgs)
            .ConfigureAppConfiguration((hostingContext, config) =>
                ApplyHostedConfig(hostedConfig, config))
                .ConfigureLogging(options => options.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddScoped<HostedConfiguration>(c => hostedConfig);
                    services.AddScoped<IBetterBackgroundWorker>(c => betterBackgroundWorker);
                    services.AddHostedService<HostedService<T>>()
                        .Configure<EventLogSettings>(config =>
                        {
                            config.LogName = "BitsServer";
                            config.SourceName = "BitsServer Source";
                        });
                }).UseWindowsService()
            .Build()
            .RunAsync();
    }
}
