using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.EventLog;

namespace Nox.Hosting
{
    public class HostedService<T> : IHostedService
        where T : class, IBetterBackgroundWorker
    {
        public EventHandler<HostedEventArgs> Initialize;

        private IBetterBackgroundWorker _betterBackgroundWorker;

        private readonly ILogger<T> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _hostLifetime;

        private int? _exitCode;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var e = new HostedEventArgs(_configuration);
            Initialize?.Invoke(this, e);

            try
            {
                _betterBackgroundWorker.Run();

                while (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, cancellationToken);
                }
                _betterBackgroundWorker?.Cancel();

                _exitCode = 0;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("The job has been killed with CTRL+C");
                _betterBackgroundWorker?.Cancel();
                _exitCode = -1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred");
                _betterBackgroundWorker?.Cancel();
                _exitCode = 1;
            }
            finally
            {
                _hostLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _betterBackgroundWorker.Cancel();

            System.Environment.ExitCode = _exitCode.GetValueOrDefault(-1);

            _logger?.LogInformation("Shutting down the service with code {exitCode}", System.Environment.ExitCode);
            return Task.CompletedTask;
        }

        public HostedService(HostedConfiguration hostedConfig, IBetterBackgroundWorker betterBackgroundWorker, IConfiguration configuration, IHostApplicationLifetime hostLifetime, ILogger<T> logger)
        {
            Initialize += (sender, args) =>
                hostedConfig?.Initialize?.Invoke(sender, args);

            _betterBackgroundWorker = betterBackgroundWorker;

            // logger optional 
            _logger = logger;
            _configuration = configuration;

            _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
        }

        protected static void ApplyHostedConfig(HostedConfiguration hostedConfig, IConfigurationBuilder config)
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
        }
    }
}
