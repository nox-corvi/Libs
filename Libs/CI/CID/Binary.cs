using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class Binary : CIDBase
    {
        public string Key { get; internal set; } = "";
        public string Path { get; internal set; } = "";
        public string File { get; internal set; } = "";

        public static Binary FromXElement(XElement source) =>
            new Binary()
            {
                Key = (string)source.Attribute("key") ?? "",
                Path = (string)source.Attribute("path") ?? "",
                File = (string)source.Attribute("file") ?? "",
            };
    }

    public class Binaries : List<Binary>
    {
        public static Binaries FromXElement(XElement source)
        {
            var Result = new Binaries();

            foreach (var o in source.Elements())
                Result.Add(Binary.FromXElement(o));

            return Result;
        }
    }

}
