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


/* View Permission
 * 
 * 
 * SELECT  
    [UserName] = CASE princ.[type] 
                    WHEN 'S' THEN princ.[name]
                    WHEN 'U' THEN ulogin.[name] COLLATE Latin1_General_CI_AI
                 END,
    [UserType] = CASE princ.[type]
                    WHEN 'S' THEN 'SQL User'
                    WHEN 'U' THEN 'Windows User'
                 END,  
    [DatabaseUserName] = princ.[name],       
    [Role] = null,      
    [PermissionType] = perm.[permission_name],       
    [PermissionState] = perm.[state_desc],       
    [ObjectType] = obj.type_desc,--perm.[class_desc],       
    [ObjectName] = OBJECT_NAME(perm.major_id),
    [ColumnName] = col.[name]
FROM    
    --database user
    sys.database_principals princ  
LEFT JOIN
    --Login accounts
    sys.login_token ulogin on princ.[sid] = ulogin.[sid]
LEFT JOIN        
    --Permissions
    sys.database_permissions perm ON perm.[grantee_principal_id] = princ.[principal_id]
LEFT JOIN
    --Table columns
    sys.columns col ON col.[object_id] = perm.major_id 
                    AND col.[column_id] = perm.[minor_id]
LEFT JOIN
    sys.objects obj ON perm.[major_id] = obj.[object_id]
WHERE 
    princ.[type] in ('S','U')
UNION
--List all access provisioned to a sql user or windows user/group through a database or application role
SELECT  
    [UserName] = CASE memberprinc.[type] 
                    WHEN 'S' THEN memberprinc.[name]
                    WHEN 'U' THEN ulogin.[name] COLLATE Latin1_General_CI_AI
                 END,
    [UserType] = CASE memberprinc.[type]
                    WHEN 'S' THEN 'SQL User'
                    WHEN 'U' THEN 'Windows User'
                 END, 
    [DatabaseUserName] = memberprinc.[name],   
    [Role] = roleprinc.[name],      
    [PermissionType] = perm.[permission_name],       
    [PermissionState] = perm.[state_desc],       
    [ObjectType] = obj.type_desc,--perm.[class_desc],   
    [ObjectName] = OBJECT_NAME(perm.major_id),
    [ColumnName] = col.[name]
FROM    
    --Role/member associations
    sys.database_role_members members
JOIN
    --Roles
    sys.database_principals roleprinc ON roleprinc.[principal_id] = members.[role_principal_id]
JOIN
    --Role members (database users)
    sys.database_principals memberprinc ON memberprinc.[principal_id] = members.[member_principal_id]
LEFT JOIN
    --Login accounts
    sys.login_token ulogin on memberprinc.[sid] = ulogin.[sid]
LEFT JOIN        
    --Permissions
    sys.database_permissions perm ON perm.[grantee_principal_id] = roleprinc.[principal_id]
LEFT JOIN
    --Table columns
    sys.columns col on col.[object_id] = perm.major_id 
                    AND col.[column_id] = perm.[minor_id]
LEFT JOIN
    sys.objects obj ON perm.[major_id] = obj.[object_id]
UNION
--List all access provisioned to the public role, which everyone gets by default
SELECT  
    [UserName] = '{All Users}',
    [UserType] = '{All Users}', 
    [DatabaseUserName] = '{All Users}',       
    [Role] = roleprinc.[name],      
    [PermissionType] = perm.[permission_name],       
    [PermissionState] = perm.[state_desc],       
    [ObjectType] = obj.type_desc,--perm.[class_desc],  
    [ObjectName] = OBJECT_NAME(perm.major_id),
    [ColumnName] = col.[name]
FROM    
    --Roles
    sys.database_principals roleprinc
LEFT JOIN        
    --Role permissions
    sys.database_permissions perm ON perm.[grantee_principal_id] = roleprinc.[principal_id]
LEFT JOIN
    --Table columns
    sys.columns col on col.[object_id] = perm.major_id 
                    AND col.[column_id] = perm.[minor_id]                   
JOIN 
    --All objects   
    sys.objects obj ON obj.[object_id] = perm.[major_id]
WHERE
    --Only roles
    roleprinc.[type] = 'R' AND
    --Only public role
    roleprinc.[name] = 'public' AND
    --Only objects of ours, not the MS objects
    obj.is_ms_shipped = 0
ORDER BY
    princ.[Name],
    OBJECT_NAME(perm.major_id),
    col.[name],
    perm.[permission_name],
    perm.[state_desc],
    obj.type_desc--perm.[class_desc] 

*/