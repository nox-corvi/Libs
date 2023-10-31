using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class Server : CIDBase
    {
        public string Domain { get; internal set; }
        public string Suffix { get; internal set; }

        public Application Application { get; internal set; } = null;

        public Requirements Requirements { get; internal set; } = null;

        public WinFeatures WinFeatures { get; internal set; } = null;

        public static Server FromXElement(XElement source)
        {
            var Result = new Server()
            {
                Domain = (string)source.Attribute("domain") ?? "",
                Suffix = (string)source.Attribute("suffix") ?? "",
            };

            foreach (var o in source.Elements())
                switch (o.Name.ToString().ToLower())
                {
                    case "application":
                        Result.Application = Application.FromXElement(o);
                        break;
                    case "requirements":
                        Result.Requirements = Requirements.FromXElement(o);
                        break;
                    case "features":
                        Result.WinFeatures = WinFeatures.FromXElement(o);
                        break;
                }

            return Result;
        }
    }
}
