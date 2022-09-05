using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IO
{
    public class Share : CIDBase
    {
        public string Name { get; internal set; } = "";

        public Permissions Permissions { get; internal set; } = new Permissions();

        public static Share FromXElement(XElement source)
        {
            var Result = new Share()
            {
                Name = (string)source.Attribute("name") ?? "",
            };
            foreach (var item in source.Elements())
                Result.Permissions.Add(Permission.FromXElement(item));

            return Result;
        }
    }

}
