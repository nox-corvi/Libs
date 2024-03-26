using Microsoft.Extensions.Logging;
using Nox.Cli;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox
{
    public class Log4
        : ILogger
    {
        public enum Log4LevelEnum
        {
            Fatal,
            Error,
            Warning,
            Info,
            Debug,
            Trace,
        }

        private const int GZ_BUFFER_MAX = 2 << 14;
        public const string Extension = ".log";

        private readonly ReaderWriterLock locker = new();

        private Log4LevelEnum _LogLevel = Log4LevelEnum.Trace;
        private readonly string _LogFile = "";

        private readonly bool _Echo = false;

        #region Properties
        public string LogFile { get => _LogFile; }

        public Log4LevelEnum LogLevel { get => _LogLevel; set => _LogLevel = value; }

        public bool Echo => _Echo;
        #endregion


        #region Helpers
        public static string LogClassName(Type type) =>
            $"{type.Assembly.GetName().Name}->{type.Name}";

        public static string GetLogLevelText(Log4LevelEnum LogLevel) =>
            LogLevel switch
            {
                Log4LevelEnum.Fatal => "FATAL",
                Log4LevelEnum.Error => "ERROR",
                Log4LevelEnum.Warning => "WARN",
                Log4LevelEnum.Info => "INFO",
                Log4LevelEnum.Debug => "DEBUG",
                Log4LevelEnum.Trace => "TRACE",
                _ => "UNKNWN"
            };
        #endregion

        public bool CompressLogFile(string Filename, bool DeleteAfter = false)
        {
            var FI = new FileInfo(Filename);

            if (((File.GetAttributes(Filename) & FileAttributes.Hidden) != FileAttributes.Hidden) &&
                (FI.Extension != ".gz"))
            {
                try
                {
                    byte[] Buffer = new byte[GZ_BUFFER_MAX];
                    using (FileStream inFileStream = new(Filename, FileMode.Open))
                    {
                        string GZFilename = Path.Combine(FI.DirectoryName,
                            Path.GetFileNameWithoutExtension(Filename) + "_" +
                            DateTime.Now.ToString("yyyymmddHHMMss") + Path.GetExtension(Filename) + ".gz");

                        using FileStream outFileStream = new(GZFilename, FileMode.CreateNew);
                        using GZipStream GZip = new(outFileStream, CompressionMode.Compress);
                        int Read = inFileStream.Read(Buffer, 0, Buffer.Length);

                        while (Read > 0)
                        {
                            GZip.Write(Buffer, 0, Read);
                            Read = inFileStream.Read(Buffer, 0, Buffer.Length);
                        }
                    }
                    if (DeleteAfter) File.Delete(Filename);

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
                return false;
        }

        private bool AppendLogFile(string Text, string Filename)
        {
            bool HasError = false; int LoopCount = 0;

            do
            {
                try
                {
                    locker.AcquireWriterLock(int.MaxValue);

                    using (var OutFile = File.AppendText(Filename))
                        OutFile.Write(Text);

                    var F = new System.IO.FileInfo(Filename);
                    if (F.Length > (10 * (2 << 20)))
                        CompressLogFile(Filename, true);

                    HasError = false;
                }
                catch
                {
                    HasError = true;
                    LoopCount += 1;

                    Thread.Sleep((int)(2 * LoopCount));
                }
                finally
                {
                    locker.ReleaseWriterLock();
                }
            } while ((HasError) && (LoopCount < 3));

            return !HasError;
        }

        public bool WriteLog(string Message, string Filename) =>
            AppendLogFile($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{AppDomain.CurrentDomain.FriendlyName}\t{Message}\r\n", Filename);

        public string BuildLogFile()
        {
            string wcLogPath = AppContext.BaseDirectory; //Environment.CurrentDirectory;
            if (!wcLogPath.EndsWith(@"\"))
                wcLogPath += @"\";

            string wcLogFile = _LogFile;

            if (!wcLogFile.EndsWith(Extension, StringComparison.InvariantCultureIgnoreCase))
                wcLogFile += Extension;

            return wcLogPath + wcLogFile;
        }

        public static string BuildExceptionString(Exception ex)
        {
            var frame = new StackFrame(1, true);
            var method = frame.GetMethod();

            return $"Exception in {frame.GetFileName()}({frame.GetFileLineNumber()}):{method.Name} - {Helpers.XmlEncode(ex.ToString())}";
        }

        public bool LogException(Exception ex)
        {
            string LogFile = BuildLogFile();
            if (LogFile.Equals(string.Empty))
                return false;

            var frame = new StackFrame(1, true);
            var method = frame.GetMethod();
            string OriginInfo = $"{frame.GetFileName()}({frame.GetFileLineNumber()}):{method.Name}";

            return WriteLog($"{ex.GetType().Name}\t{OriginInfo}: {ex}", LogFile);
        }

        /// <summary>
        /// Schreibt den Methodenaufruf mit Parametern und eine Nachricht ins Log
        /// </summary>
        /// <param name="Frame">Die Anzahl an zu überspringenden Frames im StackFrame um die aufrufende Methode ermitteln zu können</param>
        /// <param name="message">Die Nachricht die ins Log geschrieben werden soll</param>
        /// <param name="LogLevel"></param>
        /// <returns>Wahr wenn erfolgreich ins Log geschrieben werden konnte, sonst falsch</returns>
        private bool LogMethodNative(int Frame, string message, Log4LevelEnum LogLevel)
        {
            try
            {
                if (LogLevel <= _LogLevel)
                {
                    string LogFile = BuildLogFile();
                    if (LogFile.Equals(string.Empty))
                        return false;

                    var frame = new StackFrame(Frame, true);
                    var method = frame.GetMethod();
                    string param = "";
                    foreach (var item in method.GetParameters())
                        param += item.ParameterType.ToString() + " " + item.Name + ", ";

                    if (param.EndsWith(", "))
                        param = param.Remove(param.Length - 2);

                    string OriginInfo = $"{frame.GetFileName()}({frame.GetFileLineNumber()})";
                    string Message = $"{Enum.GetName(typeof(Log4LevelEnum), LogLevel)}\t{OriginInfo}\t{method.Name}({param}): {message}";

                    return WriteLog(Message, LogFile);
                }
                else
                    return true;
            }
            // False im Fehlerfall, diese Methode darf keine Ausnahmen auslösen ...
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Schreibt den Methodenaufruf mit eine Nachricht ins Log
        /// </summary>
        /// <param name="Frame">Die Anzahl an zu überspringenden Frames im StackFrame um die aufrufende Methode ermitteln zu können</param>
        /// <param name="message">Die Nachricht die ins Log geschrieben werden soll</param>
        /// <param name="LogLevel"></param>
        /// <param name="WithParameters">Gibt an ob die Parameter mit ins Log aufgenommen werden sollen</param>
        /// <param name="WithValues">Gibt an ob die Werte mit aufgenommen werden sollen</param>
        /// <returns></returns>
        public bool LogMethodNative(int Frame, string message, Log4LevelEnum LogLevel, bool WithParameters = false, params object[] args)
        {
            try
            {
                if (LogLevel <= _LogLevel)
                {
                    string LogFile = BuildLogFile();

                    // Logdatei konnte nicht ermittelt werden, raus !
                    if (LogFile.Equals(string.Empty))
                        return false;

                    // Stackframe ermitteln
                    var frame = new StackFrame(Frame, true);

                    // Methode extrahieren
                    var method = frame.GetMethod();

                    var LogMessage = new StringBuilder();

                    // Loglevel
                    LogMessage.Append($"{Enum.GetName(typeof(Log4LevelEnum), LogLevel)}\t");

                    // Origin
                    LogMessage.Append($"{frame.GetFileName()}({frame.GetFileLineNumber()})\t");

                    // Methode
                    LogMessage.Append($"{method.Name}(");

                    if (WithParameters)
                    {
                        // Parameter
                        var Parameters = method.GetParameters();
                        for (int i = 0; i < Parameters.Length; i++)
                        {
                            var item = Parameters[i];
                            if (i != 0)
                                LogMessage.Append(", ");

                            LogMessage.Append($"{item.ParameterType.ToString()} {item.Name}");

                            if (i < args.Length)
                            {
                                if (args != null)
                                    try
                                    {
                                        var arg = args[i];
                                        if (arg != null)
                                            LogMessage.Append($" = {arg.ToString()}");
                                        else
                                            LogMessage.Append($" = <Null>");
                                    }
                                    catch (Exception)
                                    {
                                        // ignore
                                        LogMessage.Append($" -_-");
                                    }
                            }
                            else
                                LogMessage.Append($" -_-");
                        }
                    }

                    LogMessage.Append(")\t");

                    // noch die Nachricht und los gehts ... 
                    LogMessage.Append(message);

                    return WriteLog(LogMessage.ToString(), LogFile);
                }
                else
                    return true;
            }
            // False im Fehlerfall, diese Methode darf keine Ausnahmen auslösen ...
            catch (Exception)
            {
                return false;
            }
        }

        public bool LogMethod(string message, Log4LevelEnum LogLevel) =>
            LogMethodNative(2, message, LogLevel);

        public bool LogMethod(Log4LevelEnum LogLevel, params object[] args) =>
            LogMethodNative(2, "", LogLevel, true, args);

        public bool LogMethod3(string message, Log4LevelEnum LogLevel) =>
            LogMethodNative(3, message, LogLevel);

        public bool LogMethod3(Log4LevelEnum LogLevel, params object[] args) =>
            LogMethodNative(3, "", LogLevel, true, args);

        public bool LogMessageNative(int Frame, string message, Log4LevelEnum LogLevel)
        {
            string LogFile = BuildLogFile();
            if (LogFile.Equals(string.Empty))
                return false;

            if (LogLevel <= _LogLevel)
            {
                var frame = new StackFrame(Frame, true);
                var method = frame.GetMethod();
                string OriginInfo = $"{frame.GetFileName()}({frame.GetFileLineNumber()}):{method.Name}";
                string Message = $"{Enum.GetName(typeof(Log4LevelEnum), LogLevel)}\t{OriginInfo}: {message}";

                return WriteLog(Message, LogFile);
            }
            else
                return true;
        }

        public bool LogMessage(string message, Log4LevelEnum LogLevel) =>
            LogMessageNative(2, message, LogLevel);

        public bool LogMessage(string message, Log4LevelEnum LogLevel, params object[] args) =>
            LogMessageNative(2, string.Format(message, args), LogLevel);

        public bool LogFunc(Func<string> action, Log4LevelEnum LogLevel)
        {
            if (LogLevel <= _LogLevel)
            {
                string Message = action?.Invoke();

                return LogMessageNative(2, Message, LogLevel);
            }
            else
            {
                return true;
            }
        }

        public bool TestLogWriteable(string Filename) => AppendLogFile("", Filename);

        public static Log4 Create(ILogger logger = null, Log4LevelEnum LogLevel = Log4LevelEnum.Trace, int SkipFrames = 1) =>
            new($"{(new StackFrame(SkipFrames)).GetMethod().DeclaringType.FullName}.log");

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public Log4(string LogFile, bool Echo, Log4LevelEnum LogLevel = Log4LevelEnum.Trace)
            : this(LogFile, LogLevel)
            => this._Echo = Echo;

        public Log4(string LogFile, Log4LevelEnum LogLevel = Log4LevelEnum.Trace)
        {
            this._LogFile = LogFile;
            this._LogLevel = LogLevel;
        }
    }
}
