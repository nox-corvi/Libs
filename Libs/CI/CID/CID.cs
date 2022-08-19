using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Libs.CI.CID
{
    public sealed class CID 
    {
        #region Properties
        public string Version { get; internal set; } = "";

        public Binaries Binaries { get; internal set; } = null;

        public Server Server { get; internal set; } = null;

        public WinFeatures WinFreatures { get; internal set; } = null;

        public Credentials Credentials { get; internal set; } = null;
        #endregion

        public static CID FromXDocument(XDocument doc)
        {
            var e = doc.Elements("installer").First();

            var Result = new CID()
            {
                Version = (string)e.Attribute("ver") ?? "",
            };

            foreach (var o in e.Elements())
                switch ((string)o.Name.ToString() ?? "")
                {
                    case "binaries":
                        Result.Binaries = Binaries.FromXElement(o);
                        break;
                    case "server":
                        Result.Server = Server.FromXElement(o);
                        break;
                    case "credential":
                        Result.Credentials = Credentials.FromXElement(o);
                        break;
                }

            return Result;
        }

        #region CID Methods
        public ProcessCredential GetProcessCredentials(string Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Credential);

            _logger?.LogMessage($"get process credentials", Log4.Log4LevelEnum.Debug);
            try
            {
                var c = Installer.Credentials;
                var cbase = c.Where(f => f.Key.Equals(Credential)).FirstOrDefault();

                string PWD = DecodeString(Key, c.Salt, cbase.Pass);

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

        public string GetDomainFromCredential(string Credential)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Credential);

            _logger?.LogMessage($"get domain from credential", Log4.Log4LevelEnum.Debug);
            switch (Credential)
            {
                case "domain":  // check in the domain ..
                case "host":    // must be a domain account 
                    return Installer.Server.Application.Net.Domain;
                case "local":
                    return ".";
                default: // in doubt use . for local 
                    return ".";
            }
        }



        #endregion
    }

    public class CIDBase : IDisposable
    {
        public virtual void Dispose()
        {
          
        }
    }



}
