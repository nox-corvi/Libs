using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IIS
{
    public class Config : CIDBase
    {
        public Templates Templates { get; internal set; }

        public AppPools AppPools { get; internal set; }

        public Webs Webs { get; internal set; }

        public static Config FromXElement(XElement source)
        {
            var Result = new Config();

            foreach (var item in source.Elements())
                switch (item.Name.ToString().ToLower())
                {
                    case "templates":
                        Result.Templates = Templates.FromXElement(item);
                        break;
                    case "app_pools":
                        Result.AppPools = AppPools.FromXElement(item);
                        break;
                    case "webs":
                        Result.Webs = Webs.FromXElement(item);
                        break;

                }

            return Result;
        }
    }

}
