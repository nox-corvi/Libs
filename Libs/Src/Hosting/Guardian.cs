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

public abstract class Guardian
    : BackgroundService 
{
    protected readonly IConfiguration _configuration;
    protected readonly ILogger _logger;

    private int _ErrorCounter = 0;

    #region Properties
    public int ErrorCounter { get => _ErrorCounter; }
    public int ErrorCounterThreshold { get; set; } = 3;
    #endregion

    protected abstract void EnsureInstance();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    EnsureInstance();
                    _ErrorCounter = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Message}", ex.Message);
                    _ErrorCounter++;
                }

                if (_ErrorCounter > ErrorCounterThreshold)
                    // error counter exceeded, break and leave service ...
                    throw new TaskCanceledException("error counter threshold exceeded");

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
        catch (TaskCanceledException e)
        {           
            if (e.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"expected terminate ...");
                System.Environment.Exit(0);
            } else
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            System.Environment.Exit(1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            System.Environment.Exit(1);
        }
    }

    public Guardian(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
}