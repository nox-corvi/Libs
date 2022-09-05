using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class Requirement : CIDBase
    {
        public string Key { get; internal set; } = "";

        public string Binary { get; internal set; } = "";

        public string Path { get; internal set; } = "";

        public string CRC { get; internal set; } = "";

        public string Target { get; internal set; } = "";

        public Install Install { get; internal set; } = null;

        public static Requirement FromXElement(XElement source)
        {
            var Result = new Requirement()
            {
                Key = (string)source.Attribute("key") ?? "",
                Binary = (string)source.Attribute("binary") ?? "",
                Path = (string)source.Attribute("path") ?? "",
                CRC = (string)source.Attribute("crc") ?? "",
                Target = (string)source.Attribute("target") ?? "",
            };

            foreach (var o in source.Elements())
                switch (o.Name.ToString().ToLower())
                {
                    case "install":
                        Result.Install = Install.FromXElement(o);
                        break;
                }

            return Result;
        }
    }

    public class Requirements : List<Requirement>
    {
        public string Key { get; internal set; } = "";
        public string Path { get; internal set; } = "";

        public static Requirements FromXElement(XElement source)
        {
            var Result = new Requirements()
            {
                Key = (string)source.Attribute("key") ?? "",
                Path = (string)source.Attribute("path") ?? "",
            };

            foreach (var o in source.Elements())
                Result.Add(Requirement.FromXElement(o));

            return Result;
        }
    }

}
