using Nox.CI.CID.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IIS
{
    public class Webs : List<Web>
    {
        public static Webs FromXElement(XElement source)
        {
            var Result = new Webs();

            foreach (var o in source.Elements("web"))
                Result.Add(Web.FromXElement(o));

            return Result;
        }
    }

    public class Web : CIDBase
    {
        public string Key { get; internal set; } = "";
        public string Path { get; internal set; } = "";
        public string AppPool { get; internal set; } = "";

        public Bindings Bindings { get; internal set; }

        public static Web FromXElement(XElement source) =>
            new Web()
            {
                Key = (string)source.Attribute("key") ?? "",
                Path = (string)source.Attribute("path") ?? "",
                AppPool = (string)source.Attribute("app_pool") ?? "",
                Bindings = Bindings.FromXElement(source)
            };
    }
}
