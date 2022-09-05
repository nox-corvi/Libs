using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IO
{
    public class Permission : CIDBase
    {
        public string Type { get; internal set; } = "";
        public string Name { get; internal set; } = "";
        public string ACL { get; internal set; } = "";

        public static Permission FromXElement(XElement source) =>
            new Permission()
            {
                Type = (string)source.Attribute("type") ?? "",
                Name = (string)source.Attribute("name") ?? "",
                ACL = (string)source.Attribute("acl") ?? "",
            };
    }
    public class Permissions : List<Permission>  { }
}