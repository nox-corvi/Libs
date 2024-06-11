using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Nox.Cli;
using Nox.Data;
using Nox.IO;
using Nox.Net;
using Nox.WebApi;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox;

public enum LogLevelEnum
{
    Fatal,
    Error,
    Warning,
    Info,
    Debug,
    Trace,
}

public interface IXLog
{
    SingleDataResponseShell CreateLogApplication(string LogSource, int TTL, DateTime Expiration);

    ResponseShell Log(LogLevelEnum LogLevel, DateTime Timestamp, string Message, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);
    
    ResponseShell LogException(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);

    ResponseShell LogTrace(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);
    ResponseShell LogDebug(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);
    ResponseShell LogInfo(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);
    ResponseShell LogWarning(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);
    ResponseShell LogError(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);
    ResponseShell LogFatal(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0);
}

public class XLog
    : IXLog
{
    private ConPrint Con1 = new();

    private RestClient RestClient;
    private string _Token;
    private string _Source;

    private static readonly object _lock = new object();

    #region Properties
    public LogLevelEnum LogLevel { get; set; } = LogLevelEnum.Error;

    public bool EchoEnabled { get; set; } = true;

    public bool EchoColorized { get; set; } = true;

    public virtual int LogWriterWaitTimeout { get; private set; } = 30;
    #endregion

    #region Helpers
    public static string LogClassName(Type type) =>
        $"{type.Assembly.GetName().Name}::{type.Name}";

    private static int GetSeverity(LogLevelEnum LogLevel)
        // 'Syslog Message Severities' from https://tools.ietf.org/html/rfc5424.
        => LogLevel switch
        {
            LogLevelEnum.Trace => 7,
            LogLevelEnum.Debug => 7,
            LogLevelEnum.Info => 6,
            LogLevelEnum.Warning => 4,
            LogLevelEnum.Error => 3,
            LogLevelEnum.Fatal => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(LogLevel))
        };

    public static string GetLogLevelText(LogLevelEnum LogLevel)
        => LogLevel switch
        {
            LogLevelEnum.Fatal => "FATAL",
            LogLevelEnum.Error => "ERROR",
            LogLevelEnum.Warning => "WARN",
            LogLevelEnum.Info => "INFO",
            LogLevelEnum.Debug => "DEBUG",
            LogLevelEnum.Trace => "TRACE",
            _ => throw new ArgumentOutOfRangeException(nameof(LogLevel))
        };

    private ConsoleColors GetLogLevelConsoleColors(LogLevelEnum LogLevel)
        => LogLevel switch
        {
            LogLevelEnum.Fatal => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
            LogLevelEnum.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
            LogLevelEnum.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevelEnum.Info => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevelEnum.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevelEnum.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            _ => new ConsoleColors(null, null)
        };
    #endregion

    public async Task<SingleDataResponseShell> GetLogApplicationNameAsync()
        => await RestClient.RestGetAsync<SingleDataResponseShell>($"/XLog/GetLogApplicationName", new KeyValue("Token", _Token));
    public SingleDataResponseShell GetLogApplicationName()
        => RestClient.RestGet<SingleDataResponseShell>($"/XLog/GetLogApplicationName", new KeyValue("Token", _Token));

    public async Task<SingleDataResponseShell> CreateLogApplicationAsync(string Application, int TTL, DateTime Expiration)
        => await RestClient.RestGetAsync<SingleDataResponseShell>($"/XLog/CreateLogApplication?Application={Application}&TTL={TTL}&Expiration={Expiration:u}",
            new KeyValue("Token", _Token));
    public SingleDataResponseShell CreateLogApplication(string Application, int TTL, DateTime Expiration)
        => RestClient.RestGet<SingleDataResponseShell>($"/XLog/CreateLogApplication?Application={Application}&TTL={TTL}&Expiration={Expiration:u}",
            new KeyValue("Token", _Token));

    public async Task<SingleDataResponseShell> CreateLogApplicationAsync(string LogSource, int TTL)
        => await CreateLogApplicationAsync(LogSource, TTL, DateTime.Now.AddYears(3));
    public SingleDataResponseShell CreateLogApplication(string LogSource, int TTL)
        => CreateLogApplication(LogSource, TTL, DateTime.Now.AddYears(3));

    public async Task<SingleDataResponseShell> CreateLogApplicationAsync(string LogSource)
        => await CreateLogApplicationAsync(LogSource, 30);
    public SingleDataResponseShell CreateLogApplication(string LogSource)
        => CreateLogApplication(LogSource, 30);

    public async Task<ResponseShell> LogAsync(LogLevelEnum LogLevel, DateTime Timestamp, string Message, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
      {
        if (this.LogLevel < LogLevel)
            return Shell.Success<ResponseShell>("ok");

        string ts = $"{Timestamp:u}";
        string LogLevelText = GetLogLevelText(LogLevel);

        Echo(LogLevel, ts, Message, MemberName, SourceFile, SourceLine);
        string URL = $"/XLog/Log?LogLevel={LogLevel}&Timestamp={ts}&Message={Message}";
        if (MemberName != null)
        {
            URL += $"&MemberName={MemberName}";
        }
        if (SourceFile != null)
        {
            URL += $"&SourceFile={SourceFile}";
        }
        if (SourceLine != 0)
        {
            URL += $"&SourceLine={SourceLine}";
        }

        return await RestClient.RestGetAsync<ResponseShell>(URL, new KeyValue("Token", _Token));
    }
    public virtual ResponseShell Log(LogLevelEnum LogLevel, DateTime Timestamp, string Message, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => AsyncHelper.RunSync(() => LogAsync(LogLevel, Timestamp, Message, MemberName, SourceFile, SourceLine));

    public async Task<ResponseShell> LogExceptionAsync(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => await LogAsync(LogLevelEnum.Error, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);

    public ResponseShell LogException(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => Log(LogLevelEnum.Error, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);

    public async Task<ResponseShell> LogTraceAsync(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => await LogAsync(LogLevelEnum.Trace, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);
    public virtual ResponseShell LogTrace(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => Log(LogLevelEnum.Trace, Timestamp, Message, MemberName, SourceFile, SourceLine);

    public async Task<ResponseShell> LogDebugAsync(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => await LogAsync(LogLevelEnum.Debug, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);
    public virtual ResponseShell LogDebug(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => Log(LogLevelEnum.Debug, Timestamp, Message, MemberName, SourceFile, SourceLine);

    public async Task<ResponseShell> LogInfoAsync(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => await LogAsync(LogLevelEnum.Info, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);
    public virtual ResponseShell LogInfo(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => Log(LogLevelEnum.Info, Timestamp, Message, MemberName, SourceFile, SourceLine);

    public async Task<ResponseShell> LogWarningAsync(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => await LogAsync(LogLevelEnum.Warning, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);
    public virtual ResponseShell LogWarning(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => Log(LogLevelEnum.Warning, Timestamp, Message, MemberName, SourceFile, SourceLine);

    public async Task<ResponseShell> LogErrorAsync(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => await LogAsync(LogLevelEnum.Error, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);
    public virtual ResponseShell LogError(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => Log(LogLevelEnum.Error, Timestamp, Message, MemberName, SourceFile, SourceLine);

    public async Task<ResponseShell> LogFatalAsync(Exception ex, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => await LogAsync(LogLevelEnum.Fatal, Timestamp, Helpers.SerializeException(ex), MemberName, SourceFile, SourceLine);
    public virtual ResponseShell LogFatal(string Message, DateTime Timestamp, [CallerMemberName] string MemberName = "", [CallerFilePath] string SourceFile = "", [CallerLineNumber] int SourceLine = 0)
        => Log(LogLevelEnum.Fatal, Timestamp, Message, MemberName, SourceFile, SourceLine);

    protected async Task EchoAsync(LogLevelEnum LogLevel, DateTime Timestamp, string Message, string MemberName = null!, string SourceFile = null!, int SourceLine = 0)
        => await Task.Run(() => { Echo(LogLevel, Timestamp.ToString("u"), Message, MemberName, SourceFile, SourceLine); });
    public void Echo(LogLevelEnum LogLevel, string Timestamp, string Message, string MemberName = null!, string SourceFile = null!, int SourceLine = 0)
    {

        if (EchoEnabled)
        {
            lock (_lock)
            {

                Console.ResetColor();
                Console.Write(_Source);
                Console.Write(" ");

                Console.Write(Timestamp);

                Console.Write(" [ ");
                if (EchoColorized)
                    GetLogLevelConsoleColors(LogLevel).Set();

                Console.Write(GetLogLevelText(LogLevel));

                if (EchoColorized)
                    Console.ResetColor();

                Console.Write(" ] ");

                if (SourceFile != null)
                {
                    Console.Write($"{Message}\r\n\t:{SourceFile}");
                    if (SourceLine != 0)
                    {
                        Console.Write($" ({SourceLine}) ");
                    }
                    if (MemberName != null)
                    {
                        Console.Write($"::{MemberName} ");
                    }

                    Console.WriteLine();
                }
                else if (MemberName != null)
                {
                    Console.WriteLine($" ::{MemberName} {Message}");
                }
            }
        }
    }

    public Exception EchoException(LogLevelEnum LogLevel, Exception e, string MemberName = null!, string SourceFile = null!, int SourceLine = 0)
    {
        try
        {
            Echo(LogLevel, $"{DateTime.UtcNow:u}", Helpers.SerializeException(e), MemberName, SourceFile, SourceLine);
        }
        catch (Exception ex)
        {
            Echo(LogLevel, $"{DateTime.UtcNow:u}", Helpers.SerializeException(ex));
        }
        finally
        {
            //
        }

        return e;
    }

    private void ConfigureLogger(IConfiguration configuration)
    {
        RestClient = new(configuration["XLog:URL"]
            ?? throw EchoException(LogLevelEnum.Fatal, new ArgumentNullException("XLog:URL")), this);
        
        _Token = configuration["XLog:Token"]
            ?? throw EchoException(LogLevelEnum.Fatal, new ArgumentNullException("XLog:Token"));

        var LogSourceResult = GetLogApplicationName();
        switch (LogSourceResult.State)
        {
            case StateEnum.Success:
                _Source = LogSourceResult.AdditionalData1;
                break;
            default:
                throw EchoException(LogLevelEnum.Fatal, new Exception("unable to determin log source"));
        }

#if NETCOREAPP
        if (Enum.TryParse(typeof(LogLevelEnum), configuration["XLog:Level"], out object level))
#elif NETFRAMEWORK
        if (Enum.TryParse<LogLevelEnum>(configuration["XLog:Level"], out LogLevelEnum level))
#endif
        {
            LogLevel = (LogLevelEnum)level;
        }

        if (bool.TryParse(configuration["XLog:Echo:Enabled"], out bool EchoEnabled))
            this.EchoEnabled = EchoEnabled;

        if (bool.TryParse(configuration["XLog:Echo:Colorized"], out bool EchoColorized))
            this.EchoColorized = EchoColorized;

        if (int.TryParse(configuration["XLog:LogWriterWaitTimeout"], out int LogWriterWaitTimeout))
            this.LogWriterWaitTimeout = LogWriterWaitTimeout;
    }

    public XLog(IConfiguration configuration)
    {
        ConfigureLogger(configuration);
    }
}

public static class XLogExtension
{
    public static IServiceCollection AddXLog(this IServiceCollection services)
    {
        return AddXLog(services, builder => { });
    }

    public static IServiceCollection AddXLog(this IServiceCollection services, Action<ILoggingBuilder> configure)
    {
        //ThrowHelper.ThrowIfNull(services);

        //services.AddOptions();

        services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
        //services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        services.TryAdd(ServiceDescriptor.Singleton<IXLog, XLog>());

        //services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
        //    new DefaultLoggerLevelConfigureOptions(LogLevel.Information)));

        //configure(new LoggingBuilder(services));
        return services;
    }

}
