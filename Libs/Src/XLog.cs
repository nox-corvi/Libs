using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Nox.Cli;
using Nox.WebApi;
using Org.BouncyCastle.Asn1.BC;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Nox;

[Flags]
public enum LogTargetEnum
{
    None = 0,
    Echo = 1,
    File = 2,
    WebApi = 4,
}

public class XLog
    : ILogger//, IXLogger
{
    private HttpClient _httpClient;

    private string _Token;

    private string _Application = null!;
    private string _CategoryName;

    private static readonly object _lock = new object();

    #region Properties
    public LogTargetEnum LogTarget { get; set; } = LogTargetEnum.Echo;

    public LogLevel LogLevel { get; set; } = LogLevel.Error;

    // Echo
    public bool EchoEnabled { get; set; } = true;

    public bool EchoColorized { get; set; } = true;

    // File
    public virtual int LogWriterWaitTimeout { get; private set; } = 30;


    #endregion

    #region Helpers
    protected static string LogClassName(Type type) =>
        $"{type.Assembly.GetName().Name}::{type.Name}";

    protected static int GetSeverity(LogLevel LogLevel)
        // 'Syslog Message Severities' from https://tools.ietf.org/html/rfc5424.
        => LogLevel switch
        {
            LogLevel.Trace => 7,
            LogLevel.Debug => 7,
            LogLevel.Information => 6,
            LogLevel.Warning => 4,
            LogLevel.Error => 3,
            LogLevel.Critical => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(LogLevel))
        };

    protected static string GetLogLevelText(LogLevel LogLevel)
        => LogLevel switch
        {
            LogLevel.Critical => "CRIT",
            LogLevel.Error => "ERROR",
            LogLevel.Warning => "WARN",
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DEBUG",
            LogLevel.Trace => "TRACE",
            _ => throw new ArgumentOutOfRangeException(nameof(LogLevel))
        };

    protected ConsoleColors GetLogLevelConsoleColors(LogLevel LogLevel)
        => LogLevel switch
        {
            LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            _ => new ConsoleColors(null, null)
        };
    #endregion

    #region Rest Methods
    protected async Task<T> RestGetAsync<T>(string Path, params KeyValue[] CustomHeaders)
        where T : IShell, new()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Path);

            foreach (var Item in CustomHeaders)
                request.Headers.Add(Item.Key, Item.Value);

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();

            return await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data)!);
        }
        catch (Exception e)
        {
            EchoException(LogLevel.Error, e);
            return Shell.Error<T>(e.Message);
        }
        finally
        {
            foreach (var Item in CustomHeaders)
                _httpClient.DefaultRequestHeaders.Remove(Item.Key);
        }
    }

    protected T RestGet<T>(string Path, params KeyValue[] CustomHeaders)
       where T : IShell, new()
        => AsyncHelper.RunSync<T>(async () => await RestGetAsync<T>(Path, CustomHeaders));
    #endregion

    public async Task<SingleDataResponseShell> GetLogApplicationNameAsync()
        => await RestGetAsync<SingleDataResponseShell>($"/Api/GetLogApplicationName", new KeyValue("Token", _Token));
    public SingleDataResponseShell GetLogApplicationName()
        => RestGet<SingleDataResponseShell>($"/Api/GetLogApplicationName", new KeyValue("Token", _Token));

    public async Task<SingleDataResponseShell> CreateLogApplicationAsync(string Application, int TTL, DateTime Expiration)
        => await RestGetAsync<SingleDataResponseShell>($"/Api/CreateLogApplication?Application={Application}&TTL={TTL}&Expiration={Expiration:u}",
            new KeyValue("Token", _Token));
    public SingleDataResponseShell CreateLogApplication(string Application, int TTL, DateTime Expiration)
        => RestGet<SingleDataResponseShell>($"/Api/CreateLogApplication?Application={Application}&TTL={TTL}&Expiration={Expiration:u}",
            new KeyValue("Token", _Token));

    public async Task<ResponseShell> LogAsync(LogLevel LogLevel, DateTime Timestamp, string Message)
    {
        ResponseShell Result = Shell.Success<ResponseShell>("Ok");

        if (LogLevel < this.LogLevel)
            return Result;

        string ts = $"{Timestamp:u}";

        if (LogTarget.HasFlag(LogTargetEnum.Echo))
            Echo(LogLevel, ts, Message);

        if (LogTarget.HasFlag(LogTargetEnum.WebApi))
        {
            //LogLevel LogLevel, DateTime Timestamp, string Message
            string URL = $"/Api/Log?LogLevel={LogLevel}&Timestamp={ts}&Message={Message}";
            Result = await RestGetAsync<ResponseShell>(URL, new KeyValue("Token", _Token));

            if (Result.State != StateEnum.Success)
            {
                if (LogTarget.HasFlag(LogTargetEnum.Echo))
                    Echo(LogLevel.Error, ts, Result.Message);
            }
        }

        return Result;
    }

    public ResponseShell Log(LogLevel LogLevel, DateTime Timestamp, string Message)
        => AsyncHelper.RunSync(() => LogAsync(LogLevel, Timestamp, Message));

    public void Echo(LogLevel LogLevel, string Timestamp, string Message)
    {
        if (EchoEnabled)
        {
            lock (_lock)
            {
                Console.Write(Timestamp);
                Console.Write(" ");

                Console.Write(_CategoryName);
                Console.Write(" ");

                if (LogTarget.HasFlag(LogTargetEnum.WebApi))
                {
                    if (EchoColorized)
                        Console.ForegroundColor = ConsoleColor.Blue;

                    Console.Write(_Application);
                    Console.Write(" ");

                    Console.ResetColor();
                }

                Console.Write(" [ ");
                if (EchoColorized)
                    GetLogLevelConsoleColors(LogLevel).Set();

                Console.Write(GetLogLevelText(LogLevel));

                if (EchoColorized)
                    Console.ResetColor();

                Console.Write(" ] ");

                Console.WriteLine($"{Message}");
            }
        }
    }

    public Exception EchoException(LogLevel LogLevel, Exception e)
    {
        try
        {
            Echo(LogLevel, $"{DateTime.Now:u}", Helpers.SerializeException(e));
        }
        catch (Exception ex)
        {
            Echo(LogLevel, $"{DateTime.Now:u}", Helpers.SerializeException(ex));
        }
        finally
        {
            //
        }

        return e;
    }

    private ResponseShell CompleteApplicationInit()
    {
        if (_Application != null)
            return Shell.Success<ResponseShell>("Ok");

        var LogSourceResult = GetLogApplicationName();
        switch (LogSourceResult.State)
        {
            case StateEnum.Success:
                _Application = LogSourceResult.AdditionalData1;
                break;
            default:
                //LogTarget &= ~LogTargetEnum.WebApi;
                break;
        }

        return LogSourceResult;
    }

    private void ConfigureLogger(IConfiguration configuration)
    {
        // no log if no target is specified
        LogTarget = Helpers.ParseEnum<LogTargetEnum>(configuration["XLog:Target"], LogTargetEnum.None);

        if (LogTarget.HasFlag(LogTargetEnum.Echo))
        {
            if (bool.TryParse(configuration["XLog:Echo:Enabled"], out bool EchoEnabled))
                this.EchoEnabled = EchoEnabled;

            if (bool.TryParse(configuration["XLog:Echo:Colorized"], out bool EchoColorized))
                this.EchoColorized = EchoColorized;
        }

        if (LogTarget.HasFlag(LogTargetEnum.File))
        {
            // not now

            if (int.TryParse(configuration["XLog:File:LogWriterWaitTimeout"], out int LogWriterWaitTimeout))
                this.LogWriterWaitTimeout = LogWriterWaitTimeout;
        }

        if (LogTarget.HasFlag(LogTargetEnum.WebApi))
        {
            // token must be set
            _Token = configuration["XLog:WebApi:Token"];

            // create client with 
            _httpClient = new()
            {
                BaseAddress = new Uri(configuration["XLog:WebApi:URL"]
                ?? throw EchoException(LogLevel.Critical, new ArgumentNullException("XLog:URL"))),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // parse vom string, use initial value if not set 
        LogLevel = Helpers.ParseEnum<LogLevel>(configuration["XLog:Level"], LogLevel);
    }

    #region ILogger:Interface 
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= (LogLevel)LogLevel;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        // No-op implementation
        return new NoOpDisposable();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        Log(logLevel, DateTime.UtcNow, formatter(state, exception));
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
    #endregion

    public XLog(IConfiguration configuration, string CategoryName)
    {
        ConfigureLogger(configuration);
        this._CategoryName = CategoryName;
    }
}

public class XLogProvider : ILoggerProvider
{
    private readonly IConfiguration _configuration;

    public XLogProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XLog(_configuration, categoryName);
    }

    public void Dispose()
    {
        // Dispose resources if needed
    }
}

public static class XLogExtension
{
    public static IServiceCollection AddXLog(this IServiceCollection services)
    {
        return AddXLogger(services, builder => { });
    }

    public static IServiceCollection AddXLogger(this IServiceCollection services, Action<ILoggingBuilder> configure)
    {
        //ThrowHelper.ThrowIfNull(services);

        //services.AddOptions();

        services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
        //services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        services.TryAdd(ServiceDescriptor.Singleton<ILogger, XLog>());

        //services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
        //    new DefaultLoggerLevelConfigureOptions(LogLevel.Information)));

        //configure(new LoggingBuilder(services));
        return services;
    }

#nullable enable
    public static void LogException(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => logger.Log(LogLevel.Error, exception, message, args);

    public static async Task LogExceptionAsync(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Error, exception, message, args));

    public static async Task LogAsync(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(logLevel, message, args));
    public static async Task LogAsync(this ILogger logger, LogLevel logLevel, EventId eventId, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(logLevel, eventId, message, args));
    public static async Task LogAsync(this ILogger logger, LogLevel logLevel, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(logLevel, exception, message, args));
    public static async Task LogAsync(this ILogger logger, LogLevel logLevel, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(logLevel, message, args));

    public static async Task LogCriticalAsync(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Critical, eventId, exception, message, args));
    public static async Task LogCriticalAsync(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Critical, eventId, message, args));
    public static async Task LogCriticalAsync(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Critical, exception, message, args));
    public static async Task LogCriticalAsync(this ILogger logger, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Critical, message, args));


    public static async Task LogDebugAsync(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Debug, eventId, exception, message, args));
    public static async Task LogDebugAsync(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Debug, eventId, message, args));
    public static async Task LogDebugAsync(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Debug, exception, message, args));
    public static async Task LogDebugAsync(this ILogger logger, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Debug, message, args));
    public static async Task LogErrorAsync(this ILogger logger, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Debug, null, message, args));

    public static async Task LogErrorAsync(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Error, exception, message, args));
    public static async Task LogErrorAsync(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Error, eventId, message, args));
    public static async Task LogErrorAsync(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Error, eventId, exception, message, args));

    public static async Task LogInformationAsync(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Information, eventId, exception, message, args));
    public static async Task LogInformationAsync(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Information, message, args));
    public static async Task LogInformationAsync(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Information, exception, message, args));
    public static async Task LogInformationAsync(this ILogger logger, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Information, message, args));

    public static async Task LogTraceAsync(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Trace, eventId, exception, message, args));
    public static async Task LogTraceAsync(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Trace, eventId, message, args));
    public static async Task LogTraceAsync(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Trace, exception, message, args));
    public static async Task LogTraceAsync(this ILogger logger, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Trace, message, args));

    public static async Task LogWarningAsync(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Warning, eventId, exception, message, args));
    public static async Task LogWarningAsync(this ILogger logger, EventId eventId, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Warning, eventId, message, args));
    public static async Task LogWarningAsync(this ILogger logger, Exception? exception, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Warning, exception, message, args));
    public static async Task LogWarningAsync(this ILogger logger, string? message, params object?[] args)
        => await Task.Run(() => logger.Log(LogLevel.Warning, message, args));
#nullable restore
}
