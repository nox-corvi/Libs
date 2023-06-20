using Nox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.CI
{
    public class ProcessHandler 
        : CIBase
    {
        const int EXIT_SUCCESS = 0;
        const int EXIT_INVALID_ARGUMENT = 1;
        const int EXIT_ERROR = 2;
        const int EXIT_UNKNOWN = 3;

        #region Process Handling
        public Process CreateProcess(string Filename, string Arguments)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, Arguments);
            _logger?.LogMessage("create process", Log4.Log4LevelEnum.Trace);

            return new Process()
            {
                StartInfo = new ProcessStartInfo(Filename, Arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            };
        }

        public int RunProcessEx(Process myProcess, StreamWriter standardOutput, StreamReader standardInput, StreamWriter standardError)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, myProcess, standardOutput, standardInput, standardError);
            _logger?.LogMessage($"run process ex {myProcess?.StartInfo?.FileName ?? "<null>"}", Log4.Log4LevelEnum.Debug);

            int Result = EXIT_UNKNOWN;

            try
            {
                if (standardOutput != null)
                {
                    _logger?.LogMessage($"redirect standard output", Log4.Log4LevelEnum.Trace);
                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        _logger?.LogMessage($"receive data on output-stream: {e.Data ?? ""}", Log4.Log4LevelEnum.Trace);
                        standardOutput.WriteLine(e.Data);
                    };
                }

                if (standardInput != null)
                {
                    _logger?.LogMessage($"redirect standard input", Log4.Log4LevelEnum.Trace);
                    myProcess.StartInfo.RedirectStandardInput = true;
                }

                if (standardError != null)
                {
                    _logger?.LogMessage($"redirect standard error", Log4.Log4LevelEnum.Trace);
                    myProcess.StartInfo.RedirectStandardError = true;
                    myProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => 
                    {
                        _logger?.LogMessage($"receive data on error-stream: {e.Data ?? ""}", Log4.Log4LevelEnum.Trace);
                        standardError.WriteLine(e.Data);
                    };
                }

                myProcess.Start();

                // if redirected .. read 
                if (myProcess.StartInfo.RedirectStandardOutput)
                    myProcess.BeginOutputReadLine();

                // read always on errors
                myProcess.BeginErrorReadLine();

                if (myProcess.StartInfo.RedirectStandardInput)
                {
                    _logger?.LogMessage($"start write standard output stream", Log4.Log4LevelEnum.Trace);

                    myProcess.StandardInput.Write(standardInput.ReadToEnd());
                    _logger?.LogMessage($"end write standard output stream", Log4.Log4LevelEnum.Trace);

                    myProcess.StandardInput.Dispose();
                }

                _logger?.LogMessage($"wait for exit", Log4.Log4LevelEnum.Trace);
                myProcess.WaitForExit();

                _logger?.LogMessage($"wait for exit done", Log4.Log4LevelEnum.Trace);
                standardOutput.Flush();

                _logger?.LogMessage($"done with {myProcess.ExitCode}", Log4.Log4LevelEnum.Debug);
                Result = myProcess.ExitCode;
            }
            catch (Exception e)
            {
                string ErrMsg = "error: run process ex";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }

            return Result;
        }

        public int RunProcess(string Filename, string Arguments, StreamWriter standardOutput, StreamReader standardInput)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, Arguments, standardOutput, standardInput);

            return RunProcessEx(CreateProcess(Filename, Arguments), standardOutput, standardInput, null);
        }

        public int RunProcess(string Filename, string Arguments, StreamWriter standardOutput, StreamReader standardInput, StreamWriter standardError)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, Arguments, standardOutput, standardInput, standardError);

            return RunProcessEx(CreateProcess(Filename, Arguments), standardOutput, standardInput, standardError);
        }
        #endregion

        public ProcessHandler(CI CI) 
            : base(CI) { }
        
        public ProcessHandler(CI CI, Log4 logger) 
            : base(CI, logger) { }
    }
}
