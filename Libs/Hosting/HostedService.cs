using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Hosting
{
    public class HostedService<T> : IHostedService
    {
        private readonly IReader _reader;
        private readonly ILogger<T> _logger;

        private readonly IHostApplicationLifetime _hostLifetime;


        private int? _exitCode;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Read {key} from settings", _configKey);

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
            Environment.ExitCode = _exitCode.GetValueOrDefault(-1);

            _logger?.LogInformation("Shutting down the service with code {exitCode}", Environment.ExitCode);
            return Task.CompletedTask;
        }

        public HostedService(IReader reader, IConfiguration configuration, IHostApplicationLifetime hostLifetime, ILogger<T> logger)
        {
            // reader required
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));    

            // logger optional 
            _logger = logger;

            _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
        }
    }
}
