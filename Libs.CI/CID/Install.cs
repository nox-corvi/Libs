using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class Install : CIDBase
    {
        public string Type { get; internal set; } = "";

        public string Check { get; internal set; } = "";

        public string Pattern { get; internal set; } = "";
        public string Argument { get; internal set; } = "";

        public string Credential { get; internal set; } = "";

        public static Install FromXElement(XElement source) =>
            new Install()
            {
                Type = (string)source.Attribute("type") ?? "",

                Check = (string)source.Attribute("check") ?? "",
                Pattern = (string)source.Attribute("pattern") ?? "",

                Argument = (string)source.Attribute("arg") ?? "",

                Credential = (string)source.Attribute("credential") ?? "",
            };
    }

}
