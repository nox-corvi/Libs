using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class NVXStrings : List<string>
    {
        public static NVXStrings FromXElement(XElement source, string element_name, string attribute_name)
        {
            var Result = new NVXStrings();
            foreach (var o in source.Elements(element_name))
                Result.Add((string)o.Attribute(attribute_name) ?? "");

            return Result;
        }
    }

}
