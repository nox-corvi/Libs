using System;
using System.Xml.Linq;

namespace Libs.CI.CID.Net
{
    public class Binding : CIDBase
    {
        public string Name { get; internal set; } = "";
        public string Port { get; internal set; } = "";

        public static Binding FromXElement(XElement source) =>
            new Binding()
            {
                Name = (string)source.Attribute("name") ?? "",
                Port = (string)source.Attribute("port") ?? "",
            };
    }

    public class Bindings : List<Binding>
    {
        public static Bindings FromXElement(XElement source)
        {
            var Result = new Bindings();

            foreach (var o in source.Elements("binding"))
                Result.Add(Binding.FromXElement(o));

            return Result;
        }
    }
}
