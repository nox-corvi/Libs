using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nox.CI.CID.IIS;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Hosting
{
    public class HostedService<T> : IHostedService
        where T : class, IReader
    {
        public EventHandler<HostedServiceEventArgs> Initialize;
        
        private readonly IReader _reader;
        private readonly ILogger<T> _logger;
        private readonly IConfiguration _configuration;

        private readonly IHostApplicationLifetime _hostLifetime;

        private int? _exitCode;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var e = new HostedServiceEventArgs(_configuration);
            Initialize?.Invoke(this, e);
            
            try
            {
                await _reader.Read(cancellationToken);

                _exitCode = 0;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("The job has been killed with CTRL+C");
                _exitCode = -1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred");
                _exitCode = 1;
            }
            finally
            {
                _hostLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            System.Environment.ExitCode = _exitCode.GetValueOrDefault(-1);

            _logger?.LogInformation("Shutting down the service with code {exitCode}", System.Environment.ExitCode);
            return Task.CompletedTask;
        }

        //public HostedConsole(HostedServiceConfiguration hostedConfig) =>
            

        public HostedService(HostedServiceConfiguration hostedConfig, IReader reader, IConfiguration configuration, IHostApplicationLifetime hostLifetime, ILogger<T> logger)
        {
            Initialize += (sender, args) => 
                hostedConfig?.Initialize?.Invoke(sender, args);

            // reader required
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            // logger optional 
            _logger = logger;
            _configuration = configuration;

            _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
        }

        public static void RunConsole(HostedServiceConfiguration hostedConfig, Func<IServiceProvider, T> implementationFactory) =>
            RunConsoleAsync(hostedConfig, implementationFactory).Wait();

        public static async Task RunConsoleAsync(HostedServiceConfiguration hostedConfig, Func<IServiceProvider, T> implementationFactory) =>
            await new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    if (hostedConfig != null)
                    {
                        if (hostedConfig.CommandlineArgs != null)
                            config.AddCommandLine(hostedConfig.CommandlineArgs);

                        if (hostedConfig.Config != null)
                        {
                            if (hostedConfig.Config.Filename != "")
                                config.AddJsonFile(hostedConfig.Config.Filename);

                            if (hostedConfig.Config.Path != "")
                                config.SetBasePath(hostedConfig.Config.Path);
                        }

                        if (hostedConfig.Environment != null)
                        {
                            if (hostedConfig.Environment.AddEnvironmentVariables)
                                config.AddEnvironmentVariables();

                        }
                    }
                })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();
                //services.AddDbContext<XAuthContext>(o => o.UseSqlServer(hostContext.Configuration.GetConnectionString("XAuth")));

                services.AddScoped<HostedServiceConfiguration>(c => hostedConfig);

                services.AddHostedService<HostedService<T>>();
                services.AddSingleton<IReader, T>(implementationFactory);
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            })
            .RunConsoleAsync();

        public static async Task RunConsoleAsync(Config config, Func<IServiceProvider, T> implementationFactory) =>
            await RunConsoleAsync(config, implementationFactory);
    }
}
