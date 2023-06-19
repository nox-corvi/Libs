/*
 * Copyright (c) 2014-2020 Anrá aka Nox
 * 
 * This code is licensed under the MIT license (MIT) 
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal 
 * in the Software without restriction, including without limitation the rights 
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included 
 * in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 * 
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using Nox;
using Nox.Data;
using Nox.Data.SqlServer;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nox.Libs.Data.Babaj
{
    /// <summary>
    /// Stellt Werkzeuge zur Datenmanipulation zur Verfügung
    /// </summary>
    public class Operate<T> : SqlDbAccess where T : DataRow
    {
        private DataTable _dataTable;
        private DataColMapDescriptor[] _Attributes;
        private DataColMapDescriptor _PrimaryKeyAttribute;

        #region Properties
        public string DatabaseTableName =>
            _dataTable.DatabaseTableName;

        public string DatabasePrimaryKey =>
            _dataTable.DatabasePrimaryKeyField;
        #endregion

        #region Stmt
        private string CreateSchemaSelect =>
            $"SELECT TOP 0 * FROM {DatabaseTableName}";

        private string CreateKeySelect =>
            $"SELECT * FROM {DatabaseTableName} WHERE {DatabasePrimaryKey} = @{DatabasePrimaryKey}";

        private string CreateSelectStmt(string Where) =>
           $"SELECT * FROM {DatabaseTableName} WHERE {Where}";

        private string CreateSelectStmt(string Where, List<string> FieldList)
        {
            StringBuilder Fields = new StringBuilder();

            for (int i = 0; i < FieldList.Count; i++)
                Fields.Append(i > 0 ? ", " : "").Append(FieldList[i]);

            return $"SELECT {Fields.ToString()} FROM {DatabaseTableName} WHERE ";
        }

        private string CreateInsertStmt(List<string> FieldList)
        {
            StringBuilder Fields = new StringBuilder(), Values = new StringBuilder();

            for (int i = 0; i < FieldList.Count; i++)
            {
                Fields.Append(i > 0 ? ", " : "").Append(FieldList[i]);
                Values.Append(i > 0 ? ", " : "").Append("@" + FieldList[i]);
            }

            return $"INSERT INTO {DatabaseTableName}({Fields.ToString()}) VALUES({Values.ToString()})";
        }

        private string CreateUpdateStmt(List<string> FieldList)
        {
            StringBuilder FieldValuePair = new StringBuilder();

            for (int i = 0; i < FieldList.Count; i++)
                FieldValuePair.Append(i > 0 ? ", " : "").Append(FieldList[i] + " = @" + FieldList[i]);

            return $"UPDATE {DatabaseTableName} SET {FieldValuePair.ToString()} WHERE {DatabasePrimaryKey} = @{DatabasePrimaryKey}";
        }

        private string CreateDeleteStmt =>
            $"DELETE FROM {DatabaseTableName} WHERE {DatabasePrimaryKey} = @{DatabasePrimaryKey}";
        #endregion

        private Guid IdPropertyValue(T row) =>
            row.GetPropertyValue<Guid>(_Attributes.Where(f => f.Name.Equals(DatabasePrimaryKey, StringComparison.InvariantCultureIgnoreCase)).First().Property);

        public void Insert(T row)
        {
            var KeyFieldValue = row.GetPropertyValue<Guid>(_PrimaryKeyAttribute.Property);

            if (!Exists(CreateKeySelect, new SqlParameter($"@{DatabasePrimaryKey}", KeyFieldValue)))
            {
                var Fields = new List<string>();
                var Params = new List<SqlParameter>();

                // add key
                Fields.Add(_PrimaryKeyAttribute.Name);
                Params.Add(new SqlParameter($"@{_PrimaryKeyAttribute.Name}", KeyFieldValue));

                // add data
                for (int i = 0; i < _Attributes.Count(); i++)
                {
                    Fields.Add(_Attributes[i].Name);
                    Params.Add(new SqlParameter($"@{_Attributes[i].Name}", row.GetPropertyValue(_Attributes[i].Property)));
                }

                // and go ... 
                Execute(CreateInsertStmt(Fields), Params.ToArray());
            }
            else
                throw new Exception("row already found");
        }

        public void Update(T r)
        {
            // get primary key of row ...
            var KeyFieldValue = r.GetPropertyValue<Guid>(_PrimaryKeyAttribute.Property);

            // test is row exists ... 
            if (Exists(CreateKeySelect, new SqlParameter($"@{DatabasePrimaryKey}", KeyFieldValue)))
            {
                var Fields = new List<string>();
                var Params = new List<SqlParameter>();

                for (int i = 0; i < _Attributes.Count(); i++)
                {
                    Fields.Add(_Attributes[i].Name);
                    Params.Add(new SqlParameter($"@{_Attributes[i].Name}", r.GetPropertyValue(_Attributes[i].Property)));
                }

                // add parameter used in where-condition ... 
                Params.Add(new SqlParameter($"@{_PrimaryKeyAttribute.Name}", KeyFieldValue));

                // and go ... 
                Execute(CreateUpdateStmt(Fields), Params.ToArray());
            } else
                throw new Exception("row not found");
        }

        public void Delete(DataRow r)
        {
            var KeyFieldValue = r.GetPropertyValue<Guid>(_PrimaryKeyAttribute.Property);

            Execute(CreateDeleteStmt, new SqlParameter($"@{_PrimaryKeyAttribute.Name}", KeyFieldValue));
        }

        public void Schema()
        {
            EnsureConnectionEstablished();
            OpenDatabaseConnection();
        }

        public DataRowColl<T> Load(string WhereCondition, params SqlParameter[] Parameters)
        {
            var Result = (DataRowColl<T>)Activator.CreateInstance(typeof(DataRowColl<T>), this._dataTable);

            using (var r = GetReader(CreateSelectStmt(WhereCondition), Parameters))
                while (r.Read())
                {
                    var NewRow = Activator.CreateInstance<T>();

                    NewRow.dataTable = this._dataTable;

                    // add primary key ..
                    _PrimaryKeyAttribute.Property.SetValue(NewRow, (r.GetValue(r.GetOrdinal(_PrimaryKeyAttribute.Name))));

                    // add data ... 
                    foreach (var a in _Attributes)
                    {
                        var data = r.GetValue(r.GetOrdinal(a.Name));

                        // Test if DBNull, use null instead ...
                        if (!Convert.IsDBNull(data))
                            a.Property.SetValue(NewRow, data);
                        else
                            a.Property.SetValue(NewRow, null);
                    }

                    NewRow.AcceptChanges();

                    Result.Add(NewRow);
                }

            return Result;
        }

        public Operate(DataTable dataTable, string ConnectionString) :
            base(ConnectionString)
        {
            this._dataTable = dataTable;

            var AttributeList = new List<DataColMapDescriptor>();

            IEnumerable<PropertyInfo> SearchList;
#if NET35 
            SearchList = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<DatabaseColumnAttribute>() != null))
#endif
            foreach (var item in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<DatabaseColumnAttribute>() != null))
            {
                var r = item.GetCustomAttribute<DatabaseColumnAttribute>();

                if (r.Name.Equals(DatabasePrimaryKey, StringComparison.InvariantCultureIgnoreCase))
                    _PrimaryKeyAttribute = new DataColMapDescriptor() { Property = item, Name = r.Name };
                else
                    AttributeList.Add(new DataColMapDescriptor()
                    {
                        Property = item,
                        Name = r.Name,
                    });
            }
            this._Attributes = AttributeList.ToArray();
        }
    }
}
