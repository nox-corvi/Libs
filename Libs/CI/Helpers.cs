using Nox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Libs.CI
{
    public class Helpers
        : CIBase
    {
        public int PSExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMessage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(PSExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return _CI.GetProcessHandler.RunCliApplication("powershell.exe", $"-Command \"{Command}\"", Credential, out OutMessage, out ErrMessage);
        }

        public int UModExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(UModExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return _CI.GetProcessHandler.RunCliApplication("usermod.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int NetExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(NetExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return _CI.GetProcessHandler.RunCliApplication("net.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }


        public int IISExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(IISExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return _CI.GetProcessHandler.RunCliApplication(Environment.GetEnvironmentVariable("windir", EnvironmentVariableTarget.Machine).AddIfMiss("\\") +
                "\\SYSTEM32\\INETSRV\\appcmd.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int NetShellExec(string Command, ProcessCredential Credential, out string OutMessage, out string ErrMEssage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"{nameof(NetShellExec)} {Command}", Log4.Log4LevelEnum.Debug);

            return _CI.GetProcessHandler.RunCliApplication("netsh.exe", Command, Credential, out OutMessage, out ErrMEssage);
        }

        public int DISMExec(string Arguments, ProcessCredential Credential, out string OutMessage, out string ErrMessage)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Arguments, Credential);
            _logger?.LogMessage($"{nameof(DISMExec)} {Arguments}", Log4.Log4LevelEnum.Debug);

            return _CI.GetProcessHandler.RunCliApplication("dism.exe", Arguments, Credential, out OutMessage, out ErrMessage);
        }

        //private NVI_Xml ParseInstaller(string XmlValue)
        //{
        //    // Log
        //    _logger?.LogMethod(Log4.Log4LevelEnum.Trace, XmlValue);

        //    _logger?.LogMessage($"parse installer ..", Log4.Log4LevelEnum.Info);
        //    try
        //    {
        //        using (var reader = new StringReader(XmlValue))
        //        {
        //            var xDoc = XDocument.Load(reader);
        //            return NVI_Xml.FromXDocument(xDoc);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        string ErrMsg = "error: parsing installer failed";

        //        _CI.CancelWithMessage(ErrMsg);
        //        throw new ApplicationException(ErrMsg, e);
        //    }
        //}

        //private bool TargetMatch(string sTarget)
        //{
        //    // Log
        //    _logger?.LogMethod(Log4.Log4LevelEnum.Trace, sTarget);

        //    _logger?.LogMessage($"check target match {sTarget}", Log4.Log4LevelEnum.Trace);
        //    switch (sTarget.ToLower())
        //    {
        //        case "*":
        //        case "all":
        //            return true;
        //        case "broker":
        //            return (Target == NVITargetType.broker);
        //        case "printer":
        //            return (Target == NVITargetType.printer);
        //        default:
        //            return false;
        //    }
        //}

        //private string ReplaceVar(string Value)
        //{
        //    // Log
        //    _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Value);

        //    _logger?.LogMessage($"replace var {Value}", Log4.Log4LevelEnum.Trace);

        //    if (Installer != null)
        //    {
        //        var Result = Value;

        //        Result = Result.Replace("%ver%", Installer.Version.Replace(".", ""));
        //        Result = Result.Replace("%stage%", Stage.ToUpper());

        //        string Domain = Installer.Server.Domain;

        //        Result = Result.Replace("%domain%", Domain);
        //        if (!string.IsNullOrEmpty(Installer.Server.Suffix))
        //            Domain += $".{Installer.Server.Suffix}";

        //        Result = Result.Replace("%fq-domain%", Domain);


        //        return Result;
        //    }
        //    else
        //        return Value;
        //}

        public string MaskPass(string Pass) =>
            new string('.', Pass.Length);

        public Helpers(CI CI)
    :       base(CI) { }

        public Helpers(CI CI, Log4 logger)
            : base(CI, logger) { }
    }
}
