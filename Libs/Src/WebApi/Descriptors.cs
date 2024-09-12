using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;


namespace Nox.WebApi;

public class PropertyDescriptor
{
    private bool _IsPrimaryKey = default;

    #region Properties
    public PropertyInfo Property { get; }

    public string Name { get => Property.Name; }

    public string Source { get; set; }

    public int Order { get; set; } = 0;

    public bool Nullable { get; set; } = false;

    public ColumnMappingDescriptor MappingDescriptor { get; set; }


    public bool IsPrimaryKey { get => _IsPrimaryKey; set => _IsPrimaryKey = value; }
    #endregion

    public override string ToString() =>
            $"{Name}#{Property.PropertyType.Name}";

    public PropertyDescriptor(PropertyInfo Property) =>
        this.Property = Property;
}

public class ColumnMappingDescriptor
{
    #region Properties
    public PropertyDescriptor PropertyDescriptor { get; set; }

    public ColumnCastDescriptor CastDescriptor { get; set; }
    #endregion

    public ColumnMappingDescriptor(PropertyDescriptor propertyDescriptor)
    {
        this.PropertyDescriptor = propertyDescriptor;
        CastDescriptor = ColumnCastDescriptor.From(propertyDescriptor);
    }
}

public class ColumnCastDescriptor
{
    // statics
    public static readonly ColumnCastDescriptor String60;
    public static readonly ColumnCastDescriptor String60Nullable;
    public static readonly ColumnCastDescriptor Numeric;
    public static readonly ColumnCastDescriptor NumericNullable;
    public static readonly ColumnCastDescriptor Decimal;
    public static readonly ColumnCastDescriptor DecimalNullable;

    #region Properties
    public SqlDbType TargetType { get; private set; }

    public bool AllowNull { get; set; } = false;

    public int Length { get; set; } = -1;

    public int Precision { get; set; } = -1;

    public int Scale { get; set; } = -1;
    #endregion

    /// <summary>
    /// Create a ColumnCastDescriptor from the <cref>PropertyInfo</cref> Type
    /// </summary>
    /// <param name="Property"><cref>PropertyInfo</cref> to read from</param>
    /// <returns>a new ColumnCastDescriptor</returns>
    /// <exception cref="NotSupportedException">thrown if properties' data type is not supported</exception>
    public static ColumnCastDescriptor From(PropertyDescriptor propertyDescriptor)
    {
        Func<ColumnCastDescriptor> NotSupported(string TypeName) => throw new NotSupportedException($"{TypeName} is not supported for database mapping");
        Func<ColumnCastDescriptor> AttributeMismatch(string RequiredAttributeName, Exception innerException)
            => throw new Exception($"{nameof(String)} requires a {nameof(ColumnDecimalAttribute)}", innerException);

        // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/sql-clr-type-mapping
        // https://docs.microsoft.com/en-us/dotnet/api/system.data.dbtype?view=netcore-3.1

        bool IsDerivedNullable = false;
        var Type = propertyDescriptor.Property.PropertyType;
        do
        {
            switch (Type.Name)
            {
                case nameof(System.Object):
                    if (Attribute.IsDefined(Type, typeof(SerializableAttribute)))
                        return new ColumnCastDescriptor()
                        {
                            TargetType = SqlDbType.NText,
                            AllowNull = IsDerivedNullable
                        };
                    else
                        return NotSupported(Type.FullName).Invoke();
                case nameof(Boolean):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.Bit, AllowNull = IsDerivedNullable };
                case nameof(System.Byte):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.TinyInt, AllowNull = IsDerivedNullable };
                case nameof(System.Int16):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.SmallInt, AllowNull = IsDerivedNullable };
                case nameof(System.Int32):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.Int, AllowNull = IsDerivedNullable };
                case nameof(System.Int64):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.BigInt, AllowNull = IsDerivedNullable };
                case nameof(System.Single):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.Real, AllowNull = IsDerivedNullable };
                case nameof(System.Double):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.Float, AllowNull = IsDerivedNullable };
                case nameof(System.Decimal):
                    try
                    {
                        var CDA = propertyDescriptor.Property.GetCustomAttributes<ColumnDecimalAttribute>(true).First();
                        return new ColumnCastDescriptor()
                        {
                            TargetType = SqlDbType.Decimal,
                            AllowNull = IsDerivedNullable,
                            Precision = CDA.Precission,
                            Scale = CDA.Scale,
                        };
                    }
                    catch (Exception ex) { return AttributeMismatch(nameof(ColumnDecimalAttribute), ex).Invoke(); }
                case nameof(System.Char):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.NChar, AllowNull = IsDerivedNullable };
                case nameof(System.String):
                    try
                    {
                        var CSA = propertyDescriptor.Property.GetCustomAttributes<ColumnStringAttribute>(true).First();
                        return new ColumnCastDescriptor()
                        {
                            TargetType = SqlDbType.NVarChar,
                            AllowNull = propertyDescriptor.Nullable,
                            Length = CSA.MaxLength
                        };
                    }
                    catch (Exception ex) { return AttributeMismatch(nameof(ColumnStringAttribute), ex).Invoke(); }
                case nameof(System.Guid):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.UniqueIdentifier, AllowNull = IsDerivedNullable };
                case nameof(DateTime):
                    return new ColumnCastDescriptor() { TargetType = SqlDbType.DateTime2, AllowNull = IsDerivedNullable };
                default:
                    if (Type.IsEnum)
                        // cast as int
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.Int, AllowNull = IsDerivedNullable };
                    else if (Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        IsDerivedNullable = true;
                        Type = Type.GenericTypeArguments.First();

                        break; // next round
                    }
                    else
                        return NotSupported(Type.FullName).Invoke();
            }
        } while (true);
    }

    public string SqlDbTypeText()
    {
        switch (TargetType)
        {
            case SqlDbType.NText:
                return "ntext";
            case SqlDbType.Bit:
                return "bit";
            case SqlDbType.TinyInt:
                return "byte";
            case SqlDbType.SmallInt:
                return "smallint";
            case SqlDbType.Int:
                return $"int";
            case SqlDbType.BigInt:
                return $"bigint";
            case SqlDbType.Real:
                return $"real";
            case SqlDbType.Float:
                return $"float";
            case SqlDbType.Decimal:
                return $"decimal({Scale}, {Precision})";
            case SqlDbType.NChar:
                return $"nchar({Length})";
            case SqlDbType.NVarChar:
                if (Length > 0)
                    return $"nvarchar({Length})";
                else
                    return $"nvarchar(max)";
            case SqlDbType.UniqueIdentifier:
                return $"uniqueidentifier";
            case SqlDbType.DateTime:
                return $"datetime";
            case SqlDbType.DateTime2:
                return $"datetime2";
            default:
                throw new NotSupportedException($"{nameof(TargetType)} not supported");
        }
    }

    public Type ToType()
    {
        //Func<ColumnCastDescriptor> NotSupported(string TypeName) => throw new NotSupportedException($"{TypeName} is not supported for database mapping");
        //Func<ColumnCastDescriptor> AttributeMismatch(string RequiredAttributeName, Exception innerException)
        //    => throw new Exception($"{nameof(String)} requires a {nameof(ColumnDecimalAttribute)}", innerException);

        // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/sql-clr-type-mapping
        // https://docs.microsoft.com/en-us/dotnet/api/system.data.dbtype?view=netcore-3.1


        switch (TargetType)
        {
            case SqlDbType.NText:
                return typeof(Object);
            case SqlDbType.Bit:
                return typeof(bool);
            case SqlDbType.TinyInt:
                return typeof(byte);
            case SqlDbType.SmallInt:
                return typeof(short);
            case SqlDbType.Int:
                return typeof(int);
            case SqlDbType.BigInt:
                return typeof(long);
            case SqlDbType.Real:
                return typeof(float);
            case SqlDbType.Float:
                return typeof(double);
            case SqlDbType.Decimal:
                return typeof(decimal);
            case SqlDbType.NChar:
                return typeof(char);
            case SqlDbType.NVarChar:
                return typeof(string);
            case SqlDbType.UniqueIdentifier:
                return typeof(Guid);
            case SqlDbType.DateTime:
                return typeof(DateTime);
            case SqlDbType.DateTime2:
                return typeof(DateTime);
            default:
                throw new NotSupportedException($"{nameof(TargetType)} not supported");
        }
    }

    static ColumnCastDescriptor()
    {
        String60 = new ColumnCastDescriptor() { AllowNull = false, Length = 60 };
        String60Nullable = new ColumnCastDescriptor() { AllowNull = true, Length = 60 };
        Numeric = new ColumnCastDescriptor() { AllowNull = false, Length = 18, Precision = 0 };
        NumericNullable = new ColumnCastDescriptor() { AllowNull = true, Length = 18, Precision = 0 };
        Decimal = new ColumnCastDescriptor() { AllowNull = false, Length = 18, Precision = 0 };
        DecimalNullable = new ColumnCastDescriptor() { AllowNull = true, Length = 18, Precision = 0 };
    }
}

public class TableDescriptor : List<PropertyDescriptor>
{
    #region Property
    public string Key { get; }

    public string TableSource { get; set; }
    #endregion

    public TableDescriptor(string Key) =>
        this.Key = Key;
}

