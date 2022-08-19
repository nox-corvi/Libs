using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Libs.CI.CID
{

    public class Credential : CIDBase
    {
        public string Origin { get; internal set; } = "";

        /// <summary>
        /// true if credential is local, host or domain 
        /// </summary>
        public string Key { get; internal set; } = "";

        public string Description { get; internal set; } = "";


        public string User { get; internal set; } = "";

        public string Pass { get; internal set; } = "";

        public string Type { get; internal set; } = "";

        public NVXStrings Membership { get; internal set; } = new NVXStrings();

        public Suffixes Suffixes { get; internal set; } = new Suffixes();

        public static Credential FromXElement(XElement source, string Origin)
        {
            var Result = new Credential()
            {
                Origin = Origin,
                Key = (string)source.Attribute("key") ?? "",
                Description = (string)source.Attribute("key") ?? "",

                Type = (string)source.Attribute("type") ?? "",

                User = (string)source.Attribute("user") ?? "",
                Pass = (string)source.Attribute("pass") ?? "",
            };

            Result.Membership = NVXStrings.FromXElement(source, "membership", "key");
            Result.Suffixes = Suffixes.FromXElement(source);

            return Result;
        }
    }

    public class Credentials : List<Credential>
    {
        public string Salt { get; internal set; } = "";

        public int GenPassLength { get; internal set; } = 16;

        public string Domain { get; internal set; } = "";

        public Groups Groups { get; internal set; } = new Groups();

        public static Credentials FromXElement(XElement source)
        {
            var Result = new Credentials()
            {
                Salt = (string)source.Attribute("salt") ?? "",
                GenPassLength = int.Parse((string)source.Attribute("genpasslength") ?? "16"),
                Domain = (string)source.Attribute("domain") ?? "",
            };

            foreach (var o in source.Elements())
            {
                switch (o.Name.ToString().ToLower())
                {
                    case "required":
                        Result.Add(Credential.FromXElement(o, "required"));
                        break;
                    case "application":
                        Result.Add(Credential.FromXElement(o, "application"));
                        break;
                    case "group":
                        Result.Groups.Add(Group.FromXElement(o));
                        break;
                }
            }
            return Result;
        }
    }
}