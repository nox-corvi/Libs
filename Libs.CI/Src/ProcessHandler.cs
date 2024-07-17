using Microsoft.Extensions.Logging;
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
    public class ProcessHandler(CI CI, ILogger Logger)
        : CIBase(CI, Logger)
    {
        const int EXIT_SUCCESS = 0;
        const int EXIT_INVALID_ARGUMENT = 1;
        const int EXIT_ERROR = 2;
        const int EXIT_UNKNOWN = 3;

        #region Process Handling
        public Process CreateProcess(string Filename, string Arguments)
        {
            Logger?.LogTrace($"create process {Filename}");

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
            Logger?.LogDebug($"run process ex {myProcess?.StartInfo?.FileName ?? "<null>"}");

            int Result = EXIT_UNKNOWN;

            try
            {
                if (standardOutput != null)
                {
                    Logger?.LogTrace($"redirect standard output");

                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        Logger?.LogTrace($"receive data on output-stream: {e.Data ?? ""}");

                        standardOutput.WriteLine(e.Data);
                    };
                }

                if (standardInput != null)
                {
                    Logger?.LogTrace($"redirect standard input");

                    myProcess.StartInfo.RedirectStandardInput = true;
                }

                if (standardError != null)
                {
                    Logger.LogTrace($"redirect standard error");

                    myProcess.StartInfo.RedirectStandardError = true;
                    myProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => 
                    {
                        Logger?.LogTrace($"receive data on error-stream: {e.Data ?? ""}");

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
                    Logger?.LogTrace($"start write standard output stream");

                    myProcess.StandardInput.Write(standardInput.ReadToEnd());
                    Logger?.LogTrace($"end write standard output stream");

                    myProcess.StandardInput.Dispose();
                }

                Logger?.LogTrace($"wait for exit");

                myProcess.WaitForExit();

                Logger?.LogTrace($"wait for exit done");

                standardOutput.Flush();

                Logger?.LogDebug($"done with {myProcess.ExitCode}");

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
            Logger?.LogTrace($"run process {Filename}");

            return RunProcessEx(CreateProcess(Filename, Arguments), standardOutput, standardInput, null);
        }

        public int RunProcess(string Filename, string Arguments, StreamWriter standardOutput, StreamReader standardInput, StreamWriter standardError)
        {
            Logger?.LogTrace($"run process {Filename}");

            return RunProcessEx(CreateProcess(Filename, Arguments), standardOutput, standardInput, standardError);
        }
        #endregion

        // DI-Constructor
        public ProcessHandler(CI CI, ILogger<CI> Logger) 
            : this(CI, (ILogger)Logger) { }
    }
}
