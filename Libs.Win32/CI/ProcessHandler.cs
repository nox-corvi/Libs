using Nox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.AccountManagement;
using Nox.CI;
using System.Diagnostics;

namespace Nox.CI.Win32
{
    public class ProcessHandler
        : Nox.CI.ProcessHandler
    {
        public Process CreateForeignProcess(string Filename, string Arguments, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, Arguments, Credential);
            _logger?.LogMessage("create foreign process", Log4.Log4LevelEnum.Trace);

            var myProcess = CreateProcess(Filename, Arguments);

            var s = myProcess.StartInfo;

            s.Verb = "runas";
            s.UserName = Credential.Username;
            s.PasswordInClearText = Credential.Password;
            s.Domain = Credential.Domain;

            return myProcess;
        }

        public int RunProcess(string Filename, string Arguments, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, Arguments, Credential);

            return RunProcessEx(CreateForeignProcess(Filename, Arguments, Credential), null, null, null);
        }

        public int RunProcess(string Filename, string Arguments, ProcessCredential Credential, StreamWriter standardOutput, StreamReader standardInput)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, Arguments, Credential, standardOutput, standardInput);

            return RunProcessEx(CreateForeignProcess(Filename, Arguments, Credential), standardOutput, standardInput, null);
        }

        public int RunProcess(string Filename, string Arguments, ProcessCredential Credential, StreamWriter standardOutput, StreamReader standardInput, StreamWriter standardError)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Filename, Arguments, Credential, standardOutput, standardInput, standardError);

            return RunProcessEx(CreateForeignProcess(Filename, Arguments, Credential), standardOutput, standardInput, standardError);
        }

        public int RunCliApplication(string Application, ProcessCredential Credential, string InMessage, out string OutMessage, out string ErrMessage)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Debug, Application, Credential, InMessage);
            _logger?.LogMessage($"run cli application {Application}", Log4.Log4LevelEnum.Debug);

            _logger?.LogMessage($"read streams", Log4.Log4LevelEnum.Trace);
            OutMessage = ErrMessage = "";
            try
            {
                using (var memIn = new MemoryStream(Console.InputEncoding.GetBytes(InMessage)))
                using (var memInReader = new StreamReader(memIn))
                using (var memErr = new MemoryStream())
                using (var memErrWriter = new StreamWriter(memErr, Console.OutputEncoding) { AutoFlush = true })
                using (var memOut = new MemoryStream())
                using (var memOutWriter = new StreamWriter(memOut, Console.OutputEncoding) { AutoFlush = true })
                {
                    OutMessage = ErrMessage = "";

                    var result = RunProcess(Application, "", Credential, memOutWriter, memInReader, memErrWriter);

                    // read error stream ... 
                    memErr.Position = 0;
                    using (var memErrReader = new StreamReader(memErr, true))
                        ErrMessage = memErrReader.ReadToEnd();

                    // console out of msi exec
                    memOut.Position = 0;
                    using (var memOutReader = new StreamReader(memOut, true))
                        OutMessage = memOutReader.ReadToEnd();

                    return result;
                }
            }
            catch (Exception e)
            {
                string ErrMsg = "error: run cli application";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public int RunCliApplication(string Application, string Arguments, ProcessCredential Credential, out string OutMessage, out string ErrMessage)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Application, Arguments, Credential);
            _logger?.LogMessage($"run cli application {Application}", Log4.Log4LevelEnum.Debug);

            OutMessage = ErrMessage = "";
            try
            {
                using (var memErr = new MemoryStream())
                using (var memErrWriter = new StreamWriter(memErr, Console.OutputEncoding) { AutoFlush = true })
                using (var memOut = new MemoryStream())
                using (var memOutWriter = new StreamWriter(memOut, Console.OutputEncoding) { AutoFlush = true })
                {
                    OutMessage = ErrMessage = "";
                    var Result = RunProcess(Application, Arguments, Credential, memOutWriter, null, memErrWriter);

                    _logger?.LogMessage($"read streams", Log4.Log4LevelEnum.Trace);
                    // read error stream ... 
                    memErr.Position = 0;
                    using (var memErrReader = new StreamReader(memErr, true))
                        ErrMessage = memErrReader.ReadToEnd();

                    // console out of msi exec
                    memOut.Position = 0;
                    using (var memOutReader = new StreamReader(memOut, true))
                        OutMessage = memOutReader.ReadToEnd();

                    return Result;
                }
            }
            catch (Exception e)
            {
                string ErrMsg = "error: run cli application";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public ProcessHandler(CI CI)
            : base(CI) { }

        public ProcessHandler(CI CI, Log4 logger)
            : base(CI, logger) { }
    }
}
