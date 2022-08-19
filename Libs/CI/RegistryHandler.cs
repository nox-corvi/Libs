using Nox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libs.CI
{
    public class RegistryHandler 
        : CIBase
    { 

        #region Registry
        private bool RegValueExist(ProcessCredential Credential, string RegRoot, string Key, string Value, string Pattern)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Credential, RegRoot, Key, Value, Pattern);
            _logger?.LogMessage($"check reg value exists", Log4.Log4LevelEnum.Debug);

            string OutMessage, ErrMessage;
            try
            {
                var Result = _CI
                    .GetProcessHandler
                    .RunCliApplication("regquery.exe", $"query --root:{RegRoot} --key \"{Key}\" --value {Value} \"{Pattern}\"", Credential, out OutMessage, out ErrMessage);

                _logger?.LogMessage("RegValueExists->Out: " + OutMessage, Log4.Log4LevelEnum.Debug);
                _logger?.LogMessage("RegValueExists->Err: " + OutMessage, Log4.Log4LevelEnum.Debug);
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


        public RegistryHandler(CI CI) 
            : base(CI) { }

        public RegistryHandler(CI CI, Log4 logger) 
            : base(CI, logger) { }
    }
}
