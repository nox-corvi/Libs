using Nox;
using Nox.CI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Win32.CI
{
    public class Helpers
        : Nox.CI.Helpers
    {
        public int PSExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMessage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(PSExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return (_CI as CI).GetProcessHandler.RunCliApplication("powershell.exe", $"-Command \"{Command}\"", Credential, out OutMessage, out ErrMessage);
        }

        public int UModExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(UModExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return (_CI as CI).GetProcessHandler.RunCliApplication("umod.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int NetExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(NetExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return (_CI as CI).GetProcessHandler.RunCliApplication("net.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int IISExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(IISExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return (_CI as CI).GetProcessHandler.RunCliApplication(Environment.GetEnvironmentVariable("windir", EnvironmentVariableTarget.Machine).AddIfMiss("\\") +
                "\\SYSTEM32\\INETSRV\\appcmd.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int NetShellExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(NetShellExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return (_CI as CI).GetProcessHandler.RunCliApplication("netsh.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int DISMExec(string Arguments, ProcessCredential Credential, out string OutMessage, out string ErrMessage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Arguments, Credential);
            _logger?.LogMessage($"{nameof(DISMExec)} {Arguments}", Log4.Log4LevelEnum.Debug);

            return (_CI as CI).GetProcessHandler.RunCliApplication("dism.exe", Arguments, Credential, out OutMessage, out ErrMessage);
        }

        public Helpers(CI CI)
            : base(CI) { }

        public Helpers(CI CI, Log4 logger)
            : base(CI, logger) { }
    }
}

