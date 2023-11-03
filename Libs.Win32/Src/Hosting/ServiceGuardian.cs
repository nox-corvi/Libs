using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Win32.Hosting
{


    //public class ServiceGuardian<T>
    //    : BackgroundService 
    //    where T : IRunner
    //{
    //    private readonly IConfiguration _configuration;
    //    private readonly ILogger _logger;

    //    //private NetServer _server = null!;

    //    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //    {
    //        try
    //        {
    //            while (!stoppingToken.IsCancellationRequested)
    //            {
    //                //if (!(_server?.StillListen ?? false)) 
    //                //{
    //                //    _server?.Dispose();

    //                //    try
    //                //    {
    //                //        string IP = _configuration["bind:ip"] ?? "127.0.0.1";
    //                //        int Port = int.Parse(_configuration["bind:port"] ?? "7855");

    //                //        _logger.LogInformation($"bind to {IP}:{Port}");
    //                //        (_server = new(_logger)).Bind(IP, Port);
    //                //    }
    //                //    catch (Exception ex)
    //                //    {
    //                //        // retry
    //                //        _logger.LogCritical(ex.ToString());
    //                //        _server?.Dispose();
    //                //    }
    //                //}

    //                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    //            }
    //        }
    //        catch (TaskCanceledException)
    //        {

    //            // When the stopping token is canceled, for example, a call made from services.msc,
    //            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
    //            Environment.Exit(1);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "{Message}", ex.Message);

    //            // Terminates this process and returns an exit code to the operating system.
    //            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
    //            // performs one of two scenarios:
    //            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
    //            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
    //            //
    //            // In order for the Windows Service Management system to leverage configured
    //            // recovery options, we need to terminate the process with a non-zero exit code.
    //            Environment.Exit(1);
    //        }
    //    }

    //    public ServiceGuardian(IConfiguration configuration, ILogger<ServiceGuardian> logger)
    //    {
    //        _configuration = configuration;
    //        _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
    //    }
    //}
}