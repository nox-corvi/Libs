using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Nox.Cli;
using Nox.IO;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
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
/* Structure of LogFile Entry
 * 
 * 
 * 
 * 
*/

public class Log4
{
    private const int __kb = 1024;
    private const int __mb = __kb << 10;

    private readonly ReaderWriterLockSlim locker = new();
    private ConPrint Con1 = new();

    #region Properties
    public string LogPath { get; protected set; } = null;
    public string LogFile { get; protected set; } = null;
    public string LogFileExtension { get; protected set; } = null!;

    public LogLevelEnum LogLevel { get; set; } = LogLevelEnum.Trace;

    public bool EchoEnabled { get; set; } = true;
    //public bool EchoSingleLine { get; set; } = true;
    public bool EchoColorized {  get; set; } = true;

    public virtual int LogWriterWaitTimeout { get; } = int.MaxValue;

    public virtual int LogWriterRetryCount { get; } = 3;

    /// <summary>
    /// size of the GZipBuffer in kilobytes
    /// </summary>
    public virtual int GZipBufferSize { get; } = 32;

    /// <summary>
    /// maximum size of an log file in mega bytes
    /// </summary>
    public virtual int MaxLogFileSize { get; } = 100;
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

    public virtual string BuildLogFilename()
    {
        string wcLogPath = (LogPath ?? AppContext.BaseDirectory).AddPS();
        string wcLogFile = LogFile;

        if (!wcLogFile.EndsWith(LogFileExtension, StringComparison.InvariantCultureIgnoreCase))
            wcLogFile += LogFileExtension;

        return wcLogPath + wcLogFile;
    }
    #endregion

    protected async Task ConsoleWriterAsync(LogLevelEnum LogLevel, DateTime Timestamp, 
        string Source, 
        string Text)
    {
        await Task.Run(() => { });
    }

    /// <summary>
    /// Komprimiert asynchron die angegebene Log-Datei im GZip-Format.
    /// </summary>
    /// <param name="filename">Der Pfad der zu komprimierenden Log-Datei.</param>
    /// <param name="deleteAfter">Gibt an, ob die Originaldatei nach der Kompression gelöscht werden soll.</param>
    /// <returns>
    /// Ein Task, der True zurückgibt, wenn die Datei erfolgreich komprimiert wurde, andernfalls False.
    /// </returns>
    /// <remarks>
    /// Die Methode prüft, ob die Datei bereits komprimiert oder versteckt ist, und führt in diesem Fall keine Kompression durch.
    /// Bei der Kompression wird eine neue Datei mit einem Timestamp im Namen erstellt, um Namenskonflikte zu vermeiden.
    /// Diese asynchrone Methode ermöglicht eine effizientere Nutzung der Ressourcen, insbesondere bei I/O-Operationen.
    /// Im Fehlerfall wird False zurückgegeben; es wird empfohlen, zusätzliches Logging für Fehlerbehandlung zu implementieren.
    /// </remarks>
    protected async Task<bool> CompressLogFileAsync(string filename, bool deleteAfter)
    {
        var fileInfo = new FileInfo(filename);
        if ((File.GetAttributes(filename) & FileAttributes.Hidden) == FileAttributes.Hidden ||
            fileInfo.Extension.Equals(".gz", StringComparison.OrdinalIgnoreCase))
        {
            // Already compressed or hidden
            return false;
        }

        try
        {
            byte[] buffer = new byte[GZipBufferSize * __kb]; // Assuming GZipBufferSize is defined elsewhere

            using (var inFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true))
            {
                string gzFilename = Path.Combine(fileInfo.DirectoryName,
                    $"{Path.GetFileNameWithoutExtension(filename)}_" +
                    $"{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(filename)}.gz");

                using (var outFileStream = new FileStream(gzFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length, true))
                using (var gzipStream = new GZipStream(outFileStream, CompressionMode.Compress))
                {
                    int read;
                    while ((read = await inFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await gzipStream.WriteAsync(buffer, 0, read);
                    }
                }
            }

            if (deleteAfter)
            {
                File.Delete(filename);
            }

            return true;
        }
        catch
        {
            // Consider logging the exception
            return false;
        }
    }

    /// <summary>
    /// Versucht, einen gegebenen Text in eine spezifizierte Log-Datei anzuhängen. 
    /// Sollte die Datei eine definierte Maximalgröße überschreiten, wird sie komprimiert.
    /// </summary>
    /// <param name="Text">Der in die Log-Datei zu schreibende Text.</param>
    /// <param name="Filename">Der Pfad der Log-Datei, in die der Text geschrieben wird.</param>
    /// <returns>True, wenn der Schreibvorgang erfolgreich war, andernfalls False.</returns>
    /// <remarks>
    /// Die Methode implementiert ein Wiederholungsschema mit exponentiellem Backoff für den Fall, dass Schreibfehler auftreten.
    /// Ein WriterLock wird verwendet, um Thread-Sicherheit beim Schreiben in die Datei zu gewährleisten.
    /// </remarks>
    protected bool CompressLogFile(string Filename, bool DeleteAfter)
        => AsyncHelper.RunSync(() => CompressLogFileAsync(Filename, DeleteAfter));

    /// <summary>
    /// Versucht, einen gegebenen Text in eine spezifizierte Log-Datei anzuhängen. 
    /// Sollte die Datei eine definierte Maximalgröße überschreiten, wird sie komprimiert.
    /// </summary>
    /// <param name="Text">Der in die Log-Datei zu schreibende Text.</param>
    /// <param name="Filename">Der Pfad der Log-Datei, in die der Text geschrieben wird.</param>
    /// <returns>True, wenn der Schreibvorgang erfolgreich war, andernfalls False.</returns>
    /// <remarks>
    /// Die Methode implementiert ein Wiederholungsschema mit exponentiellem Backoff für den Fall, dass Schreibfehler auftreten.
    /// Ein WriterLock wird verwendet, um Thread-Sicherheit beim Schreiben in die Datei zu gewährleisten.
    /// </remarks>
#if NETCOREAPP
    protected async Task<bool> AppendToLogFileAsync(string Filename, string Text)
#elif NETFRAMEWORK
    protected bool AppendToLogFile(string Filename, string Text)
#endif
    {
        int LoopCount = 0;
        bool HasError = false;

        do
        {
            try
            {
                locker.EnterWriteLock();

#if NETCOREAPP
                await File.AppendAllTextAsync(Filename, Text);
#elif NETFRAMEWORK
                File.AppendAllText(Filename, Text);
#endif

                if (new FileInfo(Filename).Length > (MaxLogFileSize * __mb))
                {
#if NETCOREAPP
                    await CompressLogFileAsync(Filename, true);
#elif NETFRAMEWORK
                    CompressLogFile(Filename, true);
#endif
                }

                HasError = false;
            }
            catch
            {
                try
                {
                    // for this special error ... write something to the console
                    Con1.PrintError("a write error occurred while accessing the log file. The file might be locked or access permissions may be insufficient");
                }
                finally
                {
                    HasError = true;
                    LoopCount += 1;

                    Thread.Sleep((int)(2 * LoopCount));
                }
            }
            finally
            {
                if (locker.IsWriteLockHeld)
                {
                    locker.ExitWriteLock();
                }
            }
        } while ((HasError) && (LoopCount < LogWriterRetryCount));

        return !HasError;
    }

#if NETCOREAPP
    /// <summary>
    /// Versucht, einen gegebenen Text in eine spezifizierte Log-Datei anzuhängen. 
    /// Sollte die Datei eine definierte Maximalgröße überschreiten, wird sie komprimiert.
    /// </summary>
    /// <param name="Text">Der in die Log-Datei zu schreibende Text.</param>
    /// <param name="Filename">Der Pfad der Log-Datei, in die der Text geschrieben wird.</param>
    /// <returns>True, wenn der Schreibvorgang erfolgreich war, andernfalls False.</returns>
    /// <remarks>
    /// Die Methode implementiert ein Wiederholungsschema mit exponentiellem Backoff für den Fall, dass Schreibfehler auftreten.
    /// Ein WriterLock wird verwendet, um Thread-Sicherheit beim Schreiben in die Datei zu gewährleisten.
    /// </remarks>
    protected bool AppendToLogFile(string Filename, string Text)
        => AsyncHelper.RunSync(() => AppendToLogFileAsync(Filename, Text));
#endif

#if NETCOREAPP
    /// <summary>
    /// Schreibt asynchron eine Log-Nachricht in die angegebene Datei.
    /// </summary>
    /// <param name="Message">Die zu loggende Nachricht.</param>
    /// <param name="Filename">Der vollständige Pfad der Datei, in die die Nachricht geschrieben werden soll.</param>
    /// <returns>
    /// Ein Task, der ein Boolean-Ergebnis zurückgibt. True, wenn die Nachricht erfolgreich geschrieben wurde, andernfalls False.
    /// </returns>
    /// <remarks>
    /// Die Methode formatiert die Log-Nachricht, indem sie den aktuellen Zeitstempel, den Namen der Anwendungsdomäne und die eigentliche Nachricht in einem vordefinierten Format anfügt.
    /// Das Format der Log-Nachricht ist 'Jahr-Monat-Tag Stunde:Minute:Sekunde <Tab> Anwendungsname <Tab> Nachricht <Neue Zeile>'.
    /// Diese Methode nutzt die 'AppendToLogFileAsync'-Methode, um die formatierte Nachricht in die angegebene Datei anzuhängen.
    /// Es wird empfohlen, diese Methode innerhalb eines Try-Catch-Blocks aufzurufen, um potenzielle Ausnahmen bei der Dateioperation zu behandeln.
    /// </remarks>

    public virtual async Task<bool> WriteLogMessageAsync(string Filename, string Message)
        => await AppendToLogFileAsync($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{AppDomain.CurrentDomain.FriendlyName}\t{Message}\r\n", Filename);
#endif

    /// <summary>
    /// Schreibt synchron eine Log-Nachricht in die angegebene Datei.
    /// </summary>
    /// <param name="Message">Die zu loggende Nachricht.</param>
    /// <param name="Filename">Der vollständige Pfad der Datei, in die die Nachricht geschrieben werden soll.</param>
    /// <returns>
    /// Ein Task, der ein Boolean-Ergebnis zurückgibt. True, wenn die Nachricht erfolgreich geschrieben wurde, andernfalls False.
    /// </returns>
    /// <remarks>
    /// Die Methode formatiert die Log-Nachricht, indem sie den aktuellen Zeitstempel, den Namen der Anwendungsdomäne und die eigentliche Nachricht in einem vordefinierten Format anfügt.
    /// Das Format der Log-Nachricht ist 'Jahr-Monat-Tag Stunde:Minute:Sekunde <Tab> Anwendungsname <Tab> Nachricht <Neue Zeile>'.
    /// Diese Methode nutzt die 'AppendToLogFileAsync'-Methode, um die formatierte Nachricht in die angegebene Datei anzuhängen.
    /// Es wird empfohlen, diese Methode innerhalb eines Try-Catch-Blocks aufzurufen, um potenzielle Ausnahmen bei der Dateioperation zu behandeln.
    /// </remarks>

    public virtual bool WriteLogMessage(string Filename, string Message)
        => AppendToLogFile(Filename, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{AppDomain.CurrentDomain.FriendlyName}\t{Message}\r\n");

    #region Log Methods
    public void Echo(LogLevelEnum LogLevel, string timestamp, string Message)
    {
        if (EchoEnabled)
        {
            Console.ResetColor();
            Console.Write(timestamp);

            Console.Write(" [");
            if (EchoColorized)
                GetLogLevelConsoleColors(LogLevel).Set();

            Console.Write(GetLogLevelText(LogLevel));

            if (EchoColorized)
                Console.ResetColor();

            Console.Write(" ] ");
            Console.Write(Message);
        }
    }

    //public bool LogMethod(LogLevelEnum LogLevel, object[] args,
    //    [CallerFilePath] string sourceFilePath = "",
    //    [CallerLineNumber] int sourceLineNumber = 0,
    //    [CallerMemberName] string memberName = "")
    //{
    //    try
    //    {
    //        if (this.LogLevel > LogLevel)
    //            return true;

    //        string LogFile = BuildLogFilename();

    //        string timestamp = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

    //        string LogLevelText = GetLogLevelText(LogLevel);
    //        string Origin = $"{Path.GetFileName(sourceFilePath)}({sourceLineNumber}):{memberName}";

    //        StringBuilder LogBuilder = new();
    //        LogBuilder.Append($"{timestamp}\t{LogLevelText}\t{Origin}");

    //        if (args != null)
    //        {
    //            LogBuilder.Append("[");
    //            for (int i = 0; i < args.Length; i++)
    //            {
    //                if (i != 0)
    //                    LogBuilder.Append(", ");

    //                try
    //                {
    //                    if (args[i] != null)
    //                        LogBuilder.Append($"{args[i].ToString()}");
    //                    else
    //                        LogBuilder.Append($"<Null>");
    //                }
    //                catch (Exception)
    //                {
    //                    // ignore
    //                    LogBuilder.Append("{..}");
    //                }
    //            }
    //            LogBuilder.Append("]");
    //        }

    //        if (Message != null)
    //            LogBuilder.Append($"\t{Message}");

    //        string LogMessage = LogBuilder.ToString();
    //        Echo(LogLevel, timestamp, Origin, LogMessage);
    //        return WriteLogMessage(LogFile, LogMessage.ToString());
    //    }
    //    // False im Fehlerfall, diese Methode darf keine Ausnahmen auslösen ...
    //    catch (Exception)
    //    {
    //        return false;
    //    }
    //}

    public bool LogMessage(LogLevelEnum LogLevel, string Message)
    {
        try
        {
            if (this.LogLevel > LogLevel)
                return true;

            string LogFile = BuildLogFilename();

            // Logdatei konnte nicht ermittelt werden, raus !
            if (string.IsNullOrWhiteSpace(LogFile))
                return false;

            string timestamp = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

            string LogLevelText = GetLogLevelText(LogLevel);
            
            string LogMessage = $"{timestamp}\t{LogLevelText}\t{Message}";
            
            Echo(LogLevel, timestamp, LogMessage);
            return WriteLogMessage(LogFile, LogMessage.ToString());
        }

        catch (Exception)
        {
            return false;
        }
    }

    //public bool LogMessage(LogLevelEnum LogLevel,
    //    [CallerFilePath] string sourceFilePath = "",
    //    [CallerLineNumber] int sourceLineNumber = 0,
    //    [CallerMemberName] string memberName = "") 
    //    => LogMessage(LogLevel, "", null, sourceFilePath, sourceLineNumber, memberName);

    public bool LogException(LogLevelEnum LogLevel, Exception ex)
        => LogMessage(LogLevel, Helpers.SerializeException(ex));
    #endregion

    private void ConfigureLogger(IConfiguration configuration)
    {
        LogFileExtension = configuration["Log4:Extension"] ?? "log";
        LogPath = configuration?["Log4:LogPath"]
            // assume current directory by default
            ?? LogPath;
        LogFile = configuration?["Log4:LogFile"]
            // try to get calling assembly from stacktrace if not set
            ?? $"{new StackFrame(1).GetMethod().DeclaringType.FullName}.log";

#if NETCOREAPP
        if (Enum.TryParse(typeof(LogLevelEnum), configuration["Log4:Level"], out object level))
#elif NETFRAMEWORK
        if (Enum.TryParse<LogLevelEnum>(configuration["Log4:Level"], out LogLevelEnum level))
#endif
        {
            LogLevel = (LogLevelEnum)level;
        }


    }


    public Log4(IConfiguration configuration)
    {
        
    }
}