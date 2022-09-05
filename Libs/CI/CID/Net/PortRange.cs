using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.Net
{
    public class PortRange : CIDBase
    {
        public string Stage { get; internal set; } = "";
        public string From { get; internal set; } = "";
        public string To { get; internal set; } = "";

        public static PortRange FromXElement(XElement source) =>
            new PortRange()
            {
                Stage = (string)source.Attribute("stage") ?? "",
                From = (string)source.Attribute("from") ?? "",
                To = (string)source.Attribute("to") ?? "",
            };
    }

    public class PortRanges : List<PortRange>
    {
        public const string PortRange = "port_range";

        public static PortRanges FromXElement(XElement source)
        {
            var Result = new PortRanges();

            foreach (var o in source.Elements(PortRange))
                Result.Add(Net.PortRange.FromXElement(o));

            return Result;
        }
    }
}
