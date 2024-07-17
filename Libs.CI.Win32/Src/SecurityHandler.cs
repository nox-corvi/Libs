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
using Nox.CI;
using Nox.CI.CID;
using System.DirectoryServices;
//using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Logging;

namespace Nox.Win32.CI
{
    public class SecurityHandler(CI CI, ILogger Logger)
        : Nox.CI.SecurityHandler(CI, Logger)
    {
        /// <summary>
        /// validate a given sid
        /// </summary>
        /// <param name="input">sid to validate</param>
        /// <returns>true if valid, false if not</returns>
        public bool ValidateSID(string input)
        {
            Logger?.LogDebug($"validate sid {input}");

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
            Logger?.LogDebug($"execute net.exe");

            try
            {
                string ErrMessage, OutMessage;
                int Result = (_CI as CI)
                    .GetHelper
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

        public ProcessCredential GetProcessCredentials(CID CID, string Key, string Credential)
        {
            Logger?.LogDebug($"get process credentials");
            try
            {
                var c = CID.Credentials;
                var cbase = c.Where(f => f.Key.Equals(Credential)).FirstOrDefault();

                string PWD = _CI
                    .GetIaCHandler
                    .DecodeString(Key, c.Salt, cbase.Pass);

                string SP = "@", User = "", Domain = "";
                if (cbase.User.Contains(SP))
                {
                    User = cbase.User.Before(SP);
                    Domain = cbase.User.After(SP);
                }

                SP = "\\";
                if (cbase.User.Contains(SP))
                {
                    Domain = cbase.User.Before(SP);
                    User = cbase.User.After(SP);
                }

                return new ProcessCredential(User, PWD, Domain);
            }
            catch (Exception e)
            {
                string ErrMsg = "error: eval process credentials failed";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }
        public string DetermineDomainFromCredential(Nox.CI.CID.CID CID, CredentialType Type)
        {
            Logger?.LogDebug($"get domain from credential");
            switch (Type)
            {
                case CredentialType._domain:  // check in the domain ..
                case CredentialType._host:    // must be a domain account 
                    return CID.Server.Application.Net.Domain;
                case CredentialType._local:
                    return ".";
                default: // in doubt use . for local 
                    return ".";
            }
        }

        public bool UserExists(CID CID, ProcessCredential CR, CredentialType Type, string User)
        {
            Logger?.LogDebug($"check if user {User ?? "<null>"} exists ");

            try
            {
                int Result;
                string Command = $"query user {User}",
                    ErrMessage, OutMessage;

                string ErrMsg;
                switch (Type)
                {
                    case CredentialType._domain:
                        Command += " --domain:" + DetermineDomainFromCredential(CID, Type);

                        ErrMsg = "error: could not check if domain user exists";
                        break;
                    case CredentialType._local:
                        ErrMsg = "error: could not check if local user exists";
                        break;
                    default:
                        ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }

                Logger?.LogTrace("umodexec with " + Command);
                if ((Result = (_CI as CI)
                    .GetHelper
                    .UModExec(Command, CR, out OutMessage, out ErrMessage)) != 0)
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

        public string GetUserSID(CID CID, ProcessCredential CR, CredentialType Type, string User)
        {
            Logger?.LogDebug($"get user {User ?? "<null>"} sid");
            try
            {
                //var CR = GetProcessCredentials(Credential);

                int Result;
                string Command = $"query user {User}",
                    ErrMessage, OutMessage;

                string ErrMsg;

                switch (Type)
                {
                    case CredentialType._domain:
                        Command += " --domain:" + DetermineDomainFromCredential(CID, Type);

                        ErrMsg = "error: could not evaluate domain user sid";
                        break;
                    case CredentialType._local:
                        ErrMsg = "error: could not evaluate local user sid"; ;
                        break;
                    default:
                        ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }

                if ((Result = (_CI as CI)
                    .GetHelper
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
            Logger?.LogDebug($"check if user {User ?? "<null>"} in group {Group ?? ""}");
            try
            {
                int Result;
                string Command = $"localgroup {Group}",
                    ErrMessage, OutMessage;


                string line;
                Result = (_CI as CI)
                    .GetHelper
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
            Logger?.LogDebug($"check if group {Group ?? "<null>"} exists");
            try
            {
                int Result;
                string Command = $"localgroup",
                    ErrMessage, OutMessage;

                string line;

                Result = (_CI as CI)
                    .GetHelper
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
            Logger?.LogDebug($"create group {Group ?? "<null>"}");
            try
            {
                int Result;
                string Command = $"localgroup {Group} /ADD",
                    ErrMessage, OutMessage;

                string line;

                Result = (_CI as CI)
                    .GetHelper
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
            Logger?.LogDebug($"add user {User ?? "<null>"} to local group {Group ?? "<null>"}");
            try
            {
                int Result;
                string Command = $"localgroup {Group} {User} /add",
                    ErrMessage, OutMessage;

                string line;

                Result = (_CI as CI)
                    .GetHelper
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

        public void ChangePass(CID CID, ProcessCredential CR, CredentialType Type, string User, string Pass, string Credential)
        {
            Logger?.LogDebug($"change pass of {User ?? "<null>"}");
            try
            {
                //var CR = GetProcessCredentials(Credential);

                int Result;
                string Command = $"alter user --set-pass {User} {Pass}",
                    ErrMessage, OutMessage;

                string ErrMsg;

                switch (Credential.ToLower())
                {
                    case "domain":
                        Command += " --domain:" + DetermineDomainFromCredential(CID, Type);

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

                if ((Result = (_CI as CI)
                    .GetHelper
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

        public bool ValidatePass(CID CID, ProcessCredential CR, CredentialType Type, string User, string Pass, string Credential)
        {
            Logger?.LogDebug($"validate pass of user {User ?? "<null>"}");
            try
            {
                switch (Credential.ToLower())
                {
                    case "local":
                        using (var pc = new PrincipalContext(ContextType.Machine, null))
                            return pc.ValidateCredentials(User, Pass);
                    case "domain":
                        var domain = DetermineDomainFromCredential(CID, Type);
                        using (var pc = new PrincipalContext(ContextType.Domain, domain))
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

        public void CreateUser(CID CID, ProcessCredential CR, CredentialType Type, string User, string DisplayName, string Pass, string OU)
        {
            Logger?.LogDebug($"create user {User ?? "<null>"} ({DisplayName ?? "<null>"})");
            try
            {
                int Result;
                string Command = $"create user {User} \"{DisplayName}\" --set-pass {Pass}",
                    ErrMessage, OutMessage;

                if (OU != "")
                    Command += $" --ou \"{OU}\"";

                string ErrMsg;

                switch (Type)
                {
                    case CredentialType._domain:
                        Command += " --domain:" + DetermineDomainFromCredential(CID, Type);

                        ErrMsg = "error: could not create domain user";
                        break;
                    case CredentialType._local:
                        ErrMsg = "error: could not create local user";
                        break;
                    default:
                        ErrMsg = "error: invalid credentials";

                        _CI.CancelWithMessage(ErrMsg);
                        throw new ApplicationException(ErrMsg);
                }

                if ((Result = (_CI as CI)
                    .GetHelper
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
            Logger?.LogDebug($"check if share {Share ?? "<null>"} exists");

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
            Logger?.LogDebug($"check if share {Share ?? "<null>"} matches destination {Destination ?? "<null>"}");

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
            Logger?.LogDebug($"check if share {Share ?? "<null>"} exists");

            string Command = $"share {Share}={Destination} {Grant} /REMARK:\"Created by NUVTY\"";

            return NetExecute(Command, Credential, (string Out) =>
            {
                string line;
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
            Logger?.LogDebug($"delete share {Share ?? "<null>"}");

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

        // DI-Constructor
        public SecurityHandler(CI CI, ILogger<SecurityHandler> Logger)
            : this(CI, (ILogger)Logger) { }
    }

    
}
