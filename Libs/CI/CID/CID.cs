using Nox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public sealed class CID 
        : CIBase
    {
        protected const string ERR_UNHANDLED_EXCEPTION = "error: unhandled exception";

        #region Properties
        public string Version { get; internal set; } = "";

        public Binaries Binaries { get; internal set; } = null;

        public Server Server { get; internal set; } = null;

        public WinFeatures WinFreatures { get; internal set; } = null;

        public Credentials Credentials { get; internal set; } = null;
        #endregion

        public static CID FromXDocument(CI CI, XDocument doc)
        {
            var e = doc.Elements("installer").First();

            var Result = new CID(CI)
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

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public CID(CI CI)
            : base(CI) { }

        public CID(CI CI, Log4 logger)
            : base(CI, logger) { }
    }

    public class CIDBase : IDisposable
    {
        public virtual void Dispose()
        {
          
        }
    }
}