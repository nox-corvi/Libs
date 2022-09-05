using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IIS
{
    public class Template : CIDBase
    {
        public string Key { get; internal set; } = "";
        public NVXStrings Items { get; internal set; } = new NVXStrings();

        public static Template FromXElement(XElement source) =>
            new Template()
            {
                Key = (string)source.Attribute("key") ?? "",
                Items = NVXStrings.FromXElement(source, "item", "value")
            };
    }

    public class Templates : List<Template>
    {
        public static Templates FromXElement(XElement source)
        {
            var Result = new Templates();

            foreach (var o in source.Elements("template"))
                Result.Add(Template.FromXElement(o));

            return Result;
        }
    }
}
