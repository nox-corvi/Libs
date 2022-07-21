using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Libs.Data.Babaj
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DatabaseTableAttribute : Attribute
    {
        public string Name { get; set; }
        public string PrimaryKey { get; set; }

        public DatabaseTableAttribute(string Name, string PrimaryKey)
        {
            this.Name = Name;
            this.PrimaryKey = PrimaryKey;

        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DatabaseColumnAttribute : Attribute
    {
        public string Name { get; set; }

        public int Length { get; set; } = 0;

        public bool AllowNull { get; set; } = false;

        public int Precision { get; set; } = -1;

        public int Scale { get; set; } = 0;

        public DatabaseColumnAttribute(string Name)
        {
            this.Name = Name;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DatabaseRelationAttribute : Attribute
    {
        public string Name { get; set; } = "";

        public Type RelatedDataModel { get; set; }
        public List<string> ForeignKeys { get; set; }
    }
}
