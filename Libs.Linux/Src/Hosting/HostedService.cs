using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;
using Nox.Hosting;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Linux.Hosting
{
    public class HostedService<T>
        : Nox.Hosting.HostedService<T>  where T : class, IBetterBackgroundWorker
    {

        public HostedService(HostedConfiguration hostedConfig, IBetterBackgroundWorker betterBackgroundWorker, IConfiguration configuration, IHostApplicationLifetime hostLifetime, ILogger<T> logger)
            : base(hostedConfig, betterBackgroundWorker, configuration, hostLifetime, logger) { }

        public static void RunService(HostedConfiguration hostedConfig, IBetterBackgroundWorker betterBackgroundWorker) =>
            RunServiceAsync(hostedConfig, betterBackgroundWorker).Wait();

        public static async Task RunServiceAsync(HostedConfiguration hostedConfig, IBetterBackgroundWorker betterBackgroundWorker) =>
           await Host.CreateDefaultBuilder()
            .UseSystemd()
            .ConfigureAppConfiguration((hostingContext, config) =>
                    ApplyHostedConfig(hostedConfig, config))

           .ConfigureLogging((hostingContext, logging) =>
           {
               logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
               logging.AddConsole();
           })
           .ConfigureServices((hostContext, services) =>
           {
               services.AddOptions();
               //services.AddDbContext<XAuthContext>(o => o.UseSqlServer(hostContext.Configuration.GetConnectionString("XAuth")));
               services.AddScoped<HostedConfiguration>(c => hostedConfig);
               services.AddScoped<IBetterBackgroundWorker>(c => betterBackgroundWorker);

               services.AddHostedService<HostedService<T>>();
           }).Build().RunAsync();
    }
}
