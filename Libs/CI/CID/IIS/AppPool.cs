using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IIS
{
    public class AppPool : CIDBase
    {
        public string Key { get; internal set; } = "";
        public string Template { get; internal set; } = "";
        public Identity Identity { get; internal set; }

        public static AppPool FromXElement(XElement source)
        {
            var Result = new AppPool()
            {
                Key = (string)source.Attribute("key") ?? "",
                Template = (string)source.Attribute("template") ?? "",
            };
            foreach (var item in source.Elements())
                Result.Identity = Identity.FromXElement(item);

            return Result;
        }
    }

    public class AppPools : List<AppPool>
    {
        public static AppPools FromXElement(XElement source)
        {
            var Result = new AppPools();

            foreach (var o in source.Elements("app_pool"))
                Result.Add(AppPool.FromXElement(o));

            return Result;
        }
    }
}
