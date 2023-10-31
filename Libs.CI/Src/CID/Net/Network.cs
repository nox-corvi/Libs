using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.Net
{
    public class Network : CIDBase
    {
        public string Domain { get; internal set; } = "";

        public PortRanges PortRanges { get; internal set; } = new PortRanges();

        public static Network FromXElement(XElement source)
        {
            var Result = new Network()
            {
                Domain = (string)source.Attribute("domain") ?? "",
            };

            Result.PortRanges = PortRanges.FromXElement(source);

            return Result;
        }
    }
}
