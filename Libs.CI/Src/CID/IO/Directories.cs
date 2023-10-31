using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID.IO
{
    public class Directories : CIDBase
    {
        public Folders Folders { get; internal set; } = new Folders();

        public static Directories FromXElement(XElement source)
        {
            var Result = new Directories();

            foreach (var item in source.Elements())
                Result.Folders.Add(Folder.FromXElement(item));

            return Result;
        }
    }

}
