using Microsoft.AspNetCore.Authentication;
using Nox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Libs.CI
{
    public class SecurityHandler 
        : CIBase
    {
        const string ERR_UNHANDLED_EXCEPTION = "error: unhandled exception";

        /// <summary>
        /// create a password with specified length
        /// </summary>
        /// <param name="Length">length of the password</param>
        /// <returns>password string</returns>
        public string PassGen(int Length)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Length);
            _logger?.LogMessage($"pass gen using length {Length}", Log4.Log4LevelEnum.Debug);

            var r = new Random((int)(DateTime.Now.Ticks & 0xFFFF));

            var chars = "BCDFGHJKMPQRTVWXY2346789";
            var result = new StringBuilder();

            for (int i = 0; i < Length; i++)
            {
                int p = r.Next(0, chars.Length - 1),
                    q = r.Next(0, 99);

                // never start with a digit
                if (i == 0)
                    while (char.IsDigit(chars[p]))
                        p = r.Next(0, chars.Length - 1);

                if (char.IsDigit(chars[p]))
                    result.Append(chars[p]);
                else
                {
                    switch (q & 0x1)
                    {
                        case 0:
                            result.Append(chars[p].ToString().ToLower());
                            break;
                        case 1:
                            result.Append(chars[p].ToString().ToUpper());
                            break;
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// validate a given sid
        /// </summary>
        /// <param name="input">sid to validate</param>
        /// <returns>true if valid, false if not</returns>
        public bool ValidateSID(string input)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, input);
            _logger?.LogMessage($"validate sid {input}", Log4.Log4LevelEnum.Debug);

            return Regex.IsMatch(input, @"^S-\d-\d+-(\d+-){1,14}\d+$");
        }

        /// <summary>
        /// execute net command
        /// </summary>
        /// <param name="Command">net arguments to execute</param>
        /// <param name="Credential">process credentials</param>
        /// <param name="ParseResult">method to parse results</param>
        /// <param name="EvaluateExitCode">method to evaluate exit codes</param>
        /// <returns>true if valid, false if not</returns>
        /// <exception cref="ApplicationException"></exception>
        public bool NetExecute(string Command, ProcessCredential Credential, Func<string, bool> ParseResult, Func<int, string, bool> EvaluateExitCode)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Command, Credential);
            _logger?.LogMessage($"execute net.exe", Log4.Log4LevelEnum.Debug);

            try
            {
                string ErrMessage, OutMessage;
                int Result = _CI
                    .GetHelpers
                    .NetExec(Command, Credential, out OutMessage, out ErrMessage);

                switch (Result)
                {
                    // net.exe returns 0
                    case 0:
                        return ParseResult.Invoke(OutMessage);
                    default:
                        return EvaluateExitCode(Result, ErrMessage);
                }
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool UserExists(string User, string Credential)
        {
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, User, Credential);
            _logger?.LogMessage($"check if user {User ?? "<null>"} exists ", Log4.Log4LevelEnum.Debug);
         
            try
            {
                var CR = GetProcessCredentials(Credential);

                int Result;
                string Command = $"query user {User}",
                    ErrMessage, OutMessage;

                string ErrMsg;
                switch (Credential.ToLower())
                {
                    case "domain":
                        Command += " --domain:" + GetDomainFromCredential(Credential);

                        ErrMsg = "error: could not check if domain user exists";
                        break;
                    case "local":
                        ErrMsg = "error: could not check if local user exists";
                        break;
                    default:
                        ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }

                _logger?.LogMessage("umodexec with " + Command, Log4.Log4LevelEnum.Trace);
                if ((Result =_CI.GetHelpers.UModExec(Command, CR, out OutMessage, out ErrMessage)) != 0)
                {
                    switch (Result)
                    {
                        case -1:
                            return false;
                        default:
                            _CI.CancelWithMessage(ErrMsg);
                            throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public string GetUserSID(string User, string Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, User, Credential);

            _logger?.LogMessage($"get user {User ?? "<null>"} sid", Log4.Log4LevelEnum.Debug);
            try
            {
                var CR = GetProcessCredentials(Credential);

                int Result;
                string Command = $"query user {User}",
                    ErrMessage, OutMessage;

                string ErrMsg;

                switch (Credential.ToLower())
                {
                    case "domain":
                        Command += " --domain:" + GetDomainFromCredential(Credential);

                        ErrMsg = "error: could not evaluate domain user sid";
                        break;
                    case "local":
                        ErrMsg = "error: could not evaluate local user sid"; ;
                        break;
                    default:
                        ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }

                if ((Result = _CI
                    .GetHelpers
                    .UModExec(Command, CR, out OutMessage, out ErrMessage)) != 0)
                {
                    _CI.CancelWithMessage(ErrMsg);
                    throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
                }

                string Name = "", SID = "", line;
                using (var reader = new StringReader(OutMessage))
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("SAM:"))
                            Name = line.After(":").Trim();
                        if (line.StartsWith("SID:"))
                            SID = line.After(":").Trim();
                    }

                if (!Name.Equals(User, StringComparison.InvariantCultureIgnoreCase))
                {
                    ErrMsg = "error: unexpected value";

                    _CI.CancelWithMessage(ErrMsg);
                    throw new ApplicationException(ErrMsg);
                }

                if (!ValidateSID(SID))
                {
                    ErrMsg = "error: invalid sid";

                    _CI.CancelWithMessage(ErrMsg);
                    throw new ApplicationException(ErrMsg);
                }

                // name and sid seems to be ok ..
                return SID;
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool UserInGroup(string User, string Group, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, User, Group, Credential);

            _logger?.LogMessage($"check if user {User ?? "<null>"} in group {Group ?? ""}", Log4.Log4LevelEnum.Debug);
            try
            {
                int Result;
                string Command = $"localgroup {Group}",
                    ErrMessage, OutMessage;


                string line;
                Result = _CI
                    .GetHelpers
                    .NetExec(Command, Credential, out OutMessage, out ErrMessage);

                switch (Result)
                {
                    case 0:
                        using (var reader = new StringReader(OutMessage))
                            while ((line = reader.ReadLine()) != null)
                                if (line.StartsWith("----"))
                                    // enter group list mode
                                    while ((line = reader.ReadLine()) != null)
                                        if (line.Trim().Equals(User, StringComparison.InvariantCultureIgnoreCase))
                                            return true;
                        break;
                    default:
                        string ErrMsg = "error: win32";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));

                }

                return false;
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool GroupExists(string Group, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Group, Credential);

            _logger?.LogMessage($"check if group {Group ?? "<null>"} exists", Log4.Log4LevelEnum.Debug);
            try
            {
                int Result;
                string Command = $"localgroup",
                    ErrMessage, OutMessage;

                string line;

                Result = _CI
                    .GetHelpers
                    .NetExec(Command, Credential, out OutMessage, out ErrMessage);
                switch (Result)
                {
                    case 0:
                        using (var reader = new StringReader(OutMessage))
                            while ((line = reader.ReadLine()) != null)
                                if (line.StartsWith("----"))
                                    // enter group list mode
                                    while ((line = reader.ReadLine()) != null)
                                        if (line.Trim().Replace("*", "").Equals(Group, StringComparison.InvariantCultureIgnoreCase))
                                            return true;
                        break;
                    default:
                        string ErrMsg = "error: win32";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));

                }

                return false;
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool CreateGroup(string Group, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Group, Credential);

            _logger?.LogMessage($"create group {Group ?? "<null>"}", Log4.Log4LevelEnum.Debug);
            try
            {
                int Result;
                string Command = $"localgroup {Group} /ADD",
                    ErrMessage, OutMessage;

                string line;

                Result = _CI
                    .GetHelpers
                    .NetExec(Command, Credential, out OutMessage, out ErrMessage);
                switch (Result)
                {
                    case 0:
                        return true;
                    default:
                        var Already = "Systemfehler 1379";
                        using (var reader = new StringReader(ErrMessage))
                            while ((line = reader.ReadLine()) != null)
                                if (line.Trim().StartsWith(Already, StringComparison.InvariantCultureIgnoreCase))
                                    return true;

                        string ErrMsg = "error: win32";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
                }
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool AddUserLocalGroup(string User, string Group, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, User, Group, Credential);

            _logger?.LogMessage($"add user {User ?? "<null>"} to local group {Group ?? "<null>"}", Log4.Log4LevelEnum.Debug);
            try
            {
                int Result;
                string Command = $"localgroup {Group} {User} /add",
                    ErrMessage, OutMessage;

                string line;

                Result = _CI
                    .GetHelpers
                    .NetExec(Command, Credential, out OutMessage, out ErrMessage);
                switch (Result)
                {
                    case 0:
                        return true;

                    default:
                        var Already = "Systemfehler 1378";
                        using (var reader = new StringReader(ErrMessage))
                            while ((line = reader.ReadLine()) != null)
                                if (line.Trim().StartsWith(Already, StringComparison.InvariantCultureIgnoreCase))
                                    return true;

                        string ErrMsg = "error: win32";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
                }
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public void ChangePass(string User, string Pass, string Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, User, MaskPass(Pass), Credential);

            _logger?.LogMessage($"change pass of {User ?? "<null>"}", Log4.Log4LevelEnum.Debug);
            try
            {
                var CR = GetProcessCredentials(Credential);

                int Result;
                string Command = $"alter user --set-pass {User} {Pass}",
                    ErrMessage, OutMessage;

                string ErrMsg;

                switch (Credential.ToLower())
                {
                    case "domain":
                        Command += " --domain:" + GetDomainFromCredential(Credential);

                        ErrMsg = "error: could not change pass of domain user";
                        break;
                    case "local":
                        ErrMsg = "error: could not change pass of local user";
                        break;
                    default:
                        ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }

                if ((Result = _CI
                    .GetHelpers
                    .UModExec(Command, CR, out OutMessage, out ErrMessage)) != 0)
                {
                    _CI.CancelWithMessage(ErrMsg);
                    throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
                }
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool ValidatePass(string User, string Pass, string Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, User, MaskPass(Pass), Credential);

            _logger?.LogMessage($"validate pass of user {User ?? "<null>"}", Log4.Log4LevelEnum.Debug);
            try
            {
                switch (Credential.ToLower())
                {
                    case "local":
                        using (var pc = new PrincipalContext(ContextType.Machine, null))
                            return pc.ValidateCredentials(User, Pass);
                    case "domain":
                        using (var pc = new PrincipalContext(ContextType.Domain, Installer.Credentials.Domain))
                            return pc.ValidateCredentials(User, Pass);
                    default:
                        string ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public void CreateUser(string User, string DisplayName, string Pass, string OU, string Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, User, DisplayName, MaskPass(Pass), OU, Credential);

            _logger?.LogMessage($"create user {User ?? "<null>"} ({DisplayName ?? "<null>"})", Log4.Log4LevelEnum.Debug);
            try
            {
                var CR = GetProcessCredentials(Credential);

                int Result;
                string Command = $"create user {User} \"{DisplayName}\" --set-pass {Pass}",
                    ErrMessage, OutMessage;

                if (OU != "")
                    Command += $" --ou \"{OU}\"";

                string ErrMsg;

                switch (Credential.ToLower())
                {
                    case "domain":
                        Command += " --domain:" + GetDomainFromCredential(Credential);

                        ErrMsg = "error: could not create domain user";
                        break;
                    case "local":
                        ErrMsg = "error: could not create local user";
                        break;
                    default:
                        ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }

                if ((Result = _CI
                    .GetHelpers
                    .UModExec(Command, CR, out OutMessage, out ErrMessage)) != 0)
                {
                    _CI.CancelWithMessage(ErrMsg);
                    throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
                }
            }
            catch (Exception e)
            {
                string ErrMsg = ERR_UNHANDLED_EXCEPTION;

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public bool ShareExists(string Share, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Share, Credential);

            _logger?.LogMessage($"check if share {Share ?? "<null>"} exists", Log4.Log4LevelEnum.Debug);

            return NetExecute($"share {Share}", Credential, (string Out) =>
            {
                string line;
                using (var reader = new StringReader(Out))
                    while ((line = reader.ReadLine()) != null)
                        if (line.StartsWith("Freigabename"))
                            return Share.Equals(line.After(" ").Trim(), StringComparison.InvariantCultureIgnoreCase);

                return false;
            }, (int Result, string ErrMessage) =>
            {
                string line;
                using (var reader = new StringReader(ErrMessage ?? ""))
                    while ((line = reader.ReadLine()) != null)
                        if (line.Contains("freigegebene Ressource existiert nicht"))
                            return false;

                string ErrMsg = "error: win32";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
            });
        }

        public bool ShareMatch(string Share, string Destination, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Share, Destination, Credential);

            _logger?.LogMessage($"check if share {Share ?? "<null>"} matches destination {Destination ?? "<null>"}", Log4.Log4LevelEnum.Debug);

            return NetExecute($"share {Share}", Credential, (string Out) =>
            {
                string line, s = null, p = null;
                using (var reader = new StringReader(Out))
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Freigabename"))
                            s = line.After(" ").Trim();

                        if (line.StartsWith("Pfad"))
                        {
                            p = line.After(" ").Trim();

                            if (!string.IsNullOrEmpty(s))
                                return (s.Equals(Share, StringComparison.InvariantCultureIgnoreCase) &
                                    p.Equals(Destination, StringComparison.InvariantCultureIgnoreCase));
                        }
                    }
                return false;
            }, (int Result, string ErrMessage) =>
            {
                string ErrMsg = "error: win32";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
            });
        }

        public bool CreateShare(string Share, string Destination, string Grant, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Share, Destination, Credential);
            _logger?.LogMessage($"check if share {Share ?? "<null>"} exists", Log4.Log4LevelEnum.Debug);

            string Command = $"share {Share}={Destination} {Grant} /REMARK:\"Created by NUVTY\"";

            return NetExecute(Command, Credential, (string Out) =>
            {
                string line, s = null, p = null;
                using (var reader = new StringReader(Out))
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("wurde erfolgreich freigegeben"))
                            return true;
                    }
                return false;
            }, (int Result, string ErrMessage) =>
            {
                string ErrMsg = "error: win32";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
            });
        }

        public bool DeleteShare(string Share, ProcessCredential Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Share, Credential);
            _logger?.LogMessage($"delete share {Share ?? "<null>"}", Log4.Log4LevelEnum.Debug);

            return NetExecute($"share {Share} /delete /yes", Credential, (string Out) =>
            {
                string line;
                using (var reader = new StringReader(Out))
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("wurde erfolgreich gelöscht"))
                            return true;
                    }
                return false;
            }, (int Result, string ErrMessage) =>
            {
                string ErrMsg = "error: win32";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, new Win32Exception(Result, ErrMessage));
            });
        }
        #endregion

        public SecurityHandler(CI CI)
    : base(CI) { }

        public SecurityHandler(CI CI, Log4 logger)
            : base(CI, logger) { }
    }
}
