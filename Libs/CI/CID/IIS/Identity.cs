using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IIS
{
    public class Identity : CIDBase
    {
        public string Type { get; internal set; } = "";
        public string Name { get; internal set; } = "";

        public static Identity FromXElement(XElement source) =>
            new Identity()
            {
                Type = (string)source.Attribute("type") ?? "",
                Name = (string)source.Attribute("name") ?? "",
            };
    }

    public class Identities : List<Identity> { }
}
