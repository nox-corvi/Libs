using System;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Nox.WebApi;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
    public string TableSource { get; set; }

    public TableAttribute(string TableSource)
        => this.TableSource = TableSource;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnAttribute : Attribute
{
    public string Source { get; set; }

    public SqlDbType Type { get; set; }

    public int Order { get; set; } = 0;

    //public bool Nullable { get; set; } = false;

    public ColumnAttribute(string Source, SqlDbType Type)
    {
        this.Source = Source;
        this.Type = Type;
    }

    public ColumnAttribute(string Source, SqlDbType Type, int Order)
        : this(Source, Type)
        => this.Order = Order;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnGuidAttribute : ColumnAttribute
{
    public ColumnGuidAttribute(string Source)
        : base(Source, SqlDbType.UniqueIdentifier) { }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnStringAttribute : ColumnAttribute
{
    public int MaxLength { get; set; } = -1;

    public bool Nullable { get; set; } = false;

    public ColumnStringAttribute(string Source, int MaxLength)
        : base(Source, SqlDbType.NVarChar) =>
        this.MaxLength = MaxLength;

    public ColumnStringAttribute(string Source, int MaxLength, bool Nullable)
        : base(Source, SqlDbType.NVarChar, MaxLength) =>
        this.Nullable = Nullable;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnIntAttribute : ColumnAttribute
{
    public ColumnIntAttribute(string Source)
        : base(Source, SqlDbType.Int) { }

}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ColumnDecimalAttribute : ColumnAttribute
{
    public int Precission { get; set; } = 18;
    public int Scale { get; set; } = 0;

    public ColumnDecimalAttribute(string Source)
        : base(Source, SqlDbType.Decimal) { }

    public ColumnDecimalAttribute(string Source, int Precission)
        : base(Source, SqlDbType.Decimal) =>
        this.Precission = Precission;

    public ColumnDecimalAttribute(string Source, int Precission, int Scale)
        : this(Source, Precission) => this.Scale = Scale;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnDateTimeAttribute : ColumnAttribute
{
    public bool WideRange { get; set; } = false;

    public ColumnDateTimeAttribute(string Source, bool WideRange)
        : base(Source, Helpers.OnXCond(() => WideRange, () => SqlDbType.DateTime2, () => SqlDbType.DateTime)) =>
        this.WideRange = WideRange;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PrimaryKeyAttribute : RequiredAttribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class IndexAttribute : Attribute
{
    public string Name { get; set; }

    public bool IsUnique { get; set; } = false;

    public IndexAttribute(string Name)
        => this.Name = Name;

    public IndexAttribute(string Name, bool IsUnique)
        : this(Name) =>
        this.IsUnique = IsUnique;
}