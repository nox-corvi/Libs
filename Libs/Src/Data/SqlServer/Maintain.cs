using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Data.SqlServer
{
    internal class Maintain
        : IDisposable
    {
        public struct SqlObject
        {
            public int ObjectId;
            public string Name;
        }
        public struct TypeDesc
        {
            public string Type;
            public string Description;
        }

        private SqlDbAccess _dba = null!;


        #region Info Queries

        public List<SqlObject> Tables()
        {
            var Result = new List<SqlObject>();
            using (var r = _dba.GetReader("select * from sys.tables"))
                Result.Add(new SqlObject()
                {
                    ObjectId = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("object_id")), "0")),
                    Name = Helpers.NZ(r.GetValue(r.GetOrdinal("name")))
                });

            return Result;
        }

        public List<TypeDesc> Types()
        {
            var Result = new List<TypeDesc>();
            using (var r = _dba.GetReader("select distinct type, type_desc from sys.all_objects where is_ms_shipped = 0 order by type"))
                Result.Add(new TypeDesc()
                {
                    Type = Helpers.NZ(r.GetValue(r.GetOrdinal("type"))),
                    Description = Helpers.NZ(r.GetValue(r.GetOrdinal("type_desc")))
                });

            return Result;
        }

        public List<SqlObject> Objects(string Type)
        {
            var Result = new List<SqlObject>();
            using (var r = _dba.GetReader("select * from sys.all_objects where type = @type and is_ms_shipped = 0",
                new SqlParameter("@type", Type)))
                Result.Add(new SqlObject()
                {
                    ObjectId = int.Parse(Helpers.NZ(r.GetValue(r.GetOrdinal("object_id")), "0")),
                    Name = Helpers.NZ(r.GetValue(r.GetOrdinal("name")))

                });
            return Result;
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
