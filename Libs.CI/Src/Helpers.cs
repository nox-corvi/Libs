using Nox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI
{
    public class Helpers
        : CIBase
    {
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
