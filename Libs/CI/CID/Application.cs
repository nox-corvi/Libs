using Nox.CI.CID.IIS;
using Nox.CI.CID.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class Application : CIDBase
    {
        public string Drive { get; internal set; } = "";

        public Net.Network Net { get; internal set; } = new Net.Network();

        public IO.Directories Directories { get; internal set; }

        public Config IISConfig { get; internal set; }

        public static Application FromXElement(XElement source)
        {
            var Result = new Application()
            {
                Drive = (string)source.Attribute("drive") ?? "",
            };

            foreach (var item in source.Elements())
                switch (item.Name.ToString().ToLower())
                {
                    case "net":
                        Result.Net = Network.FromXElement(item);
                        break;
                    case "directories":
                        Result.Directories = IO.Directories.FromXElement(item);
                        break;
                    case "iis":
                        Result.IISConfig = Config.FromXElement(item);
                        break;
                }

            return Result;
        }
    }
}
