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
            _logger?.LogMessage(LogLevelEnum.Trace, "create process");

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
            _logger?.LogMessage(LogLevelEnum.Debug,
                $"run process ex {myProcess?.StartInfo?.FileName ?? "<null>"}");

            int Result = EXIT_UNKNOWN;

            try
            {
                if (standardOutput != null)
                {
                    _logger?.LogMessage(LogLevelEnum.Trace, 
                        $"redirect standard output");

                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        _logger?.LogMessage(LogLevelEnum.Trace, 
                            $"receive data on output-stream: {e.Data ?? ""}");

                        standardOutput.WriteLine(e.Data);
                    };
                }

                if (standardInput != null)
                {
                    _logger?.LogMessage(LogLevelEnum.Trace, 
                        $"redirect standard input");

                    myProcess.StartInfo.RedirectStandardInput = true;
                }

                if (standardError != null)
                {
                    _logger?.LogMessage(LogLevelEnum.Trace,
                        $"redirect standard error");

                    myProcess.StartInfo.RedirectStandardError = true;
                    myProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => 
                    {
                        _logger?.LogMessage(LogLevelEnum.Trace, 
                            $"receive data on error-stream: {e.Data ?? ""}");

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
                    _logger?.LogMessage(LogLevelEnum.Trace, 
                        $"start write standard output stream");

                    myProcess.StandardInput.Write(standardInput.ReadToEnd());
                    _logger?.LogMessage(LogLevelEnum.Trace, 
                        $"end write standard output stream");

                    myProcess.StandardInput.Dispose();
                }

                _logger?.LogMessage(LogLevelEnum.Trace, 
                    $"wait for exit");

                myProcess.WaitForExit();

                _logger?.LogMessage(LogLevelEnum.Trace, 
                    $"wait for exit done");

                standardOutput.Flush();

                _logger?.LogMessage(LogLevelEnum.Debug, 
                    $"done with {myProcess.ExitCode}");

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
            _logger?.LogMessage(LogLevelEnum.Trace, "");

            return RunProcessEx(CreateProcess(Filename, Arguments), standardOutput, standardInput, null);
        }

        public int RunProcess(string Filename, string Arguments, StreamWriter standardOutput, StreamReader standardInput, StreamWriter standardError)
        {
            _logger?.LogMessage(LogLevelEnum.Trace, "");

            return RunProcessEx(CreateProcess(Filename, Arguments), standardOutput, standardInput, standardError);
        }
        #endregion

        public ProcessHandler(CI CI) 
            : base(CI) { }
        
        public ProcessHandler(CI CI, Log4 logger) 
            : base(CI, logger) { }
    }
}
