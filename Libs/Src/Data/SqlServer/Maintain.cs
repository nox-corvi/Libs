using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nox.Data.SqlServer
{
    public struct SqlColumn
    {
        public string TableCatalog;
        public string TableSchema;
        public string TableName;
        public string ColumnName;
        public int OrdinalPosition;
        public string ColumnDefault;
        public bool IsNullable;
        public string DataType;
        public string DataTypeAsComparable;
        public int MaxLength;
        public int OctetLength;
        public int NumericPrecision;
        public int NumericScale;
        public int DateTimePrecision;
    }
    public class SqlTable
    {
        public int ObjectId;
        public string Name;
    }
    public class SqlType
    {
        public string Type;
        public string Description;
    }

    public class Maintain
        : IDisposable
    {

        private SqlDbAccess _dba = null!;


        #region Info Queries

        public bool TableExists(string Table)
        {
            var qry =
                @"select 
	                object_id
                from 
	                sys.tables a 
                where 
	                is_ms_shipped = 0 and
	                [name] like @table";

            using (var r = _dba.GetReader(@qry,
                new SqlParameter("table", Table)))
                return r.Read();
        }

        public SqlTable[] Tables()
        {
            var qry =
                @"select 
	            a.object_id, b.name as [schema], a.name, a.create_date, a.modify_date
            from 
	            sys.tables a 
	            left join sys.schemas b 
		            on a.schema_id = b.schema_id 
	            left join sys.database_principals p
		            on a.principal_id = p.principal_id
            where is_ms_shipped = 0";

            var Result = new List<SqlTable>();
            using (var r = _dba.GetReader(@qry))
                Result.Add(new SqlTable()
                {
                    ObjectId = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("object_id")), "0")),
                    Name = Helpers.NZ(r.GetValue(r.GetOrdinal("name")))
                });

            return Result.ToArray();
        }

        public SqlColumn[] Columns(string Table)
        {
            var qry =
            @"select
	            table_catalog, 
	            table_schema, 
	            table_name, 
	            column_name, 
	            ordinal_position,
	            column_default, 
	            is_nullable, 
	            data_type, 
	            character_maximum_length,
	            character_octet_length,
	            numeric_precision, 
	            numeric_scale, 
	            datetime_precision
            from 
	            INFORMATION_SCHEMA.COLUMNS
            where
	            table_name = @table";

            var Result = new List<SqlColumn>();
            using (var r = _dba.GetReader(@qry,
                new SqlParameter("table", Table)))
                while (r.Read())
                {
                    var newItem = new SqlColumn()
                    {
                        TableCatalog = Helpers.NZ(r.GetValue(r.GetOrdinal("table_catalog"))),
                        TableSchema = Helpers.NZ(r.GetValue(r.GetOrdinal("table_schema"))),
                        TableName = Helpers.NZ(r.GetValue(r.GetOrdinal("table_name"))),
                        ColumnName = Helpers.NZ(r.GetValue(r.GetOrdinal("column_name"))),
                        OrdinalPosition = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("ordinal_position")), "0")),
                        ColumnDefault = Helpers.NZ(r.GetValue(r.GetOrdinal("column_default"))),
                        IsNullable = Helpers.NZ(r.GetValue(r.GetOrdinal("is_nullable")), "") == "YES",
                        DataType = Helpers.NZ(r.GetValue(r.GetOrdinal("data_type"))),
                        MaxLength = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("character_maximum_length")), "0")),
                        OctetLength = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("character_octet_length")), "0")),
                        NumericPrecision = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("numeric_precision")), "0")),
                        NumericScale = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("numeric_scale")), "0")),
                        DateTimePrecision = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("datetime_precision")), "0")),
                    };

                    switch (newItem.DataType)
                    {
                        case "char":
                        case "nchar":
                        case "nvarchar":
                        case "varchar":
                            string length = "max";
                            if (newItem.MaxLength > 0)
                            {
                                length = newItem.MaxLength.ToString();
                            }

                            newItem.DataTypeAsComparable = $"{newItem.DataType}({length})";
                            break;
                        case "decimal":
                            newItem.DataTypeAsComparable = $"{newItem.DataType}({newItem.NumericPrecision},{newItem.NumericScale})";
                            break;
                        default:
                            newItem.DataTypeAsComparable = $"{newItem.DataType}";
                            break;
                    }

                    Result.Add(newItem);
                }

            return Result.ToArray();
        }
        #endregion

        #region 
        #endregion

        public Maintain(SqlDbAccess dba)
            => _dba = dba;

        public void Dispose()
            => _dba?.Dispose();
    }
}