using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class Suffix : CIDBase
    {
        public string Key { get; internal set; } = "";
        public string Value { get; internal set; } = "";

        public static Suffix FromXElement(XElement source) =>
            new Suffix()
            {
                Key = (string)source.Attribute("key") ?? "",
                Value = (string)source.Attribute("value") ?? "",
            };
    }

    public class Suffixes : List<Suffix>
    {
        public static Suffixes FromXElement(XElement source)
        {
            var Result = new Suffixes();

            foreach (var o in source.Elements("suffix"))
                Result.Add(Suffix.FromXElement(o));

            return Result;
        }
    }

}
