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
//using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.AccountManagement;
using Nox.CI;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Nox.Win32.CI
{
    public class RegistryHandler(Nox.CI.CI CI, ILogger Logger)
        : CIBase(CI, Logger)
    { 
        #region Registry
        private bool RegValueExist(ProcessCredential Credential, string RegRoot, string Key, string Value, string Pattern)
        {
            // Log
            Logger?.LogDebug($"check reg value exists");

            string OutMessage, ErrMessage;
            try
            {
                var Result = (_CI as CI)
                    .GetProcessHandler
                    .RunCliApplication("regquery.exe", $"query --root:{RegRoot} --key \"{Key}\" --value {Value} \"{Pattern}\"", Credential, out OutMessage, out ErrMessage);

                Logger?.LogDebug("RegValueExists->Out: " + OutMessage);
                Logger?.LogDebug("RegValueExists->Err: " + OutMessage);
                if (Result < 0)
                    return false;
                else if (Result == 0)
                    return true;
                else
                    throw new ApplicationException(ErrMessage);

            }
            catch (Exception e)
            {
                string ErrMsg = "error: file download failed";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }
        #endregion

        public RegistryHandler(Nox.CI.CI CI, ILogger<Nox.CI.CI> Logger) 
            : this((CI)CI, (ILogger)Logger) { }
    }
}
