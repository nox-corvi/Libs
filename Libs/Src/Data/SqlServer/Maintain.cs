using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Data.SqlServer
{
    public struct SqlColumn
    {
        public string Name;
        public int OrdinalNumber;
        public string TypeName;
        public int MaxLength;
        public int Precision;
        public int Scale;
        public bool IsNullable;
        public bool IsIdentity;
        public int DefaultObjectId;
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
	            c.name as Name,
	            c.column_id as OrdinalNumber, 
	            ty.name as TypeName,
	            c.max_length as MaxLength,
	            c.precision as Precision, 
	            c.scale as Scale, 
	            c.is_nullable as IsNullable, 
	            c.is_identity as IsIdentity, 
	            c.default_object_id as DefaultObjectId
            from 
	            sys.all_columns c
	            left join sys.tables ta on c.object_id = ta.object_id
	            left join sys.types ty on c.system_type_id = ty.system_type_id and c.user_type_id = ty.user_type_id

            where
	            ta.name like @table 
                and ta.is_ms_shipped = 0";

            var Result = new List<SqlColumn>();
            using (var r = _dba.GetReader(@qry,
                new SqlParameter("table", Table)))
                while (r.Read())
                    Result.Add(new SqlColumn()
                    {
                        Name = Helpers.NZ(r.GetValue(r.GetOrdinal("name"))),
                        OrdinalNumber = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("OrdinalNumber")), "0")),
                        TypeName = Helpers.NZ(r.GetValue(r.GetOrdinal("TypeName"))),
                        MaxLength = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("MaxLength")), "0")),
                        Precision = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("Precision")), "0")),
                        Scale = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("Scale")), "0")),
                        IsNullable = r.GetBoolean(r.GetOrdinal("IsNullable")),
                        IsIdentity = r.GetBoolean(r.GetOrdinal("IsIdentity")),
                        DefaultObjectId = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("DefaultObjectId")), "0")),
                    });

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