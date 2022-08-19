using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Libs.CI.CID
{
    public class Group : CIDBase
    {
        public string Key { get; private set; } = "";
        public string Name { get; private set; } = "";

        public GroupMembers GroupMembers { get; internal set; } = new GroupMembers();

        public static Group FromXElement(XElement source)
        {
            var Result = new Group()
            {
                Key = (string)source.Attribute("key") ?? "",
                Name = (string)source.Attribute("name") ?? "",
            };

            foreach (var o in source.Elements())
                Result.GroupMembers.Add(GroupMember.FromXElement(o));

            return Result;
        }

    }

    public class Groups : List<Group> { }

}
