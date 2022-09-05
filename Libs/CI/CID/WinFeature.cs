using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{

    public class WinFeature : CIDBase
    {
        public string Key { get; internal set; } = "";

        public string Target { get; internal set; } = "";

        public static WinFeature FromXElement(XElement source)
        {
            var Result = new WinFeature()
            {
                Key = (string)source.Attribute("key") ?? "",
                Target = (string)source.Attribute("target") ?? "",
            };

            return Result;
        }
    }

    public class WinFeatures : List<WinFeature>
    {
        public WinFeatures()
        {
        }

        public string Credential { get; internal set; } = "";

        public static WinFeatures FromXElement(XElement source)
        {
            var Result = new WinFeatures()
            {
                Credential = (string)source.Attribute("credential") ?? "",
            };

            foreach (var o in source.Elements())
                switch (o.Name.ToString().ToLower())
                {
                    case "item":
                        Result.Add(WinFeature.FromXElement(o));
                        break;
                }

            return Result;
        }
    }
}
