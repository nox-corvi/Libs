using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.CI.CID
{
    public class GroupMember : CIDBase
    {
        public string Name { get; private set; } = "";

        public static GroupMember FromXElement(XElement source) =>
          new GroupMember()
          {
              Name = (string)source.Attribute("name") ?? "",
          };
    }

    public class GroupMembers : List<GroupMember> { }

}
