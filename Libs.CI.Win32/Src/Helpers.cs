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
            _logger?.LogMessage(LogLevelEnum.Debug, $"{nameof(PSExec)} {Command}");

            return (_CI as CI).GetProcessHandler.RunCliApplication("powershell.exe", $"-Command \"{Command}\"", Credential, out OutMessage, out ErrMessage);
        }

        public int UModExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMessage(LogLevelEnum.Debug, $"{nameof(UModExec)} {Command}");

            return (_CI as CI).GetProcessHandler.RunCliApplication("umod.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int NetExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMessage(LogLevelEnum.Debug, $"{nameof(NetExec)} {Command}");

            return (_CI as CI).GetProcessHandler.RunCliApplication("net.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int IISExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMessage(LogLevelEnum.Debug, $"{nameof(IISExec)} {Command}");

            return (_CI as CI).GetProcessHandler.RunCliApplication(Environment.GetEnvironmentVariable("windir", EnvironmentVariableTarget.Machine).AddIfMiss("\\") +
                "\\SYSTEM32\\INETSRV\\appcmd.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int NetShellExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMessage(LogLevelEnum.Debug, $"{nameof(NetShellExec)} {Command}");

            return (_CI as CI).GetProcessHandler.RunCliApplication("netsh.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int DISMExec(string Arguments, ProcessCredential Credential, out string OutMessage, out string ErrMessage)
        {
            _logger?.LogMessage(LogLevelEnum.Debug, $"{nameof(DISMExec)} {Arguments}");

            return (_CI as Nox.Win32.CI.CI).GetProcessHandler.RunCliApplication("dism.exe", Arguments, Credential, out OutMessage, out ErrMessage);
        }

        public Helpers(Nox.CI.CI CI)
            : base(CI) { }

        public Helpers(Nox.CI.CI CI, Log4 logger)
            : base(CI, logger) { }
    }
}
