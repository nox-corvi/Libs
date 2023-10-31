using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IO
{
    public class Folder : CIDBase
    {

        public string Id { get; internal set; } = "";

        public string Parent { get; internal set; } = "";

        public string Name { get; internal set; } = "";

        public Share Share { get; internal set; }

        public Permissions Permissions { get; internal set; } = new Permissions();

        public static Folder FromXElement(XElement source)
        {
            var Result = new Folder()
            {
                Id = (string)source.Attribute("id") ?? "",
                Parent = (string)source.Attribute("parent") ?? "",
                Name = (string)source.Attribute("name") ?? "",
            };

            foreach (var item in source.Elements())
                switch (item.Name.ToString().ToLower())
                {
                    case "permission":
                        Result.Permissions.Add(Permission.FromXElement(item));
                        break;
                    case "share":
                        Result.Share = Share.FromXElement(item);
                        break;
                }

            return Result;
        }
    }

    public class Folders : List<Folder> { }

}