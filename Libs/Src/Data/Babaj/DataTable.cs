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

    public abstract class DataTable : IDisposable
    {
        protected string _ConnectionString;

        protected string _DatabaseTableName = "";
        protected string _DatabasePrimaryKeyField = "";

        public string Name { get; }

        #region Properties
        public string DatabaseTableName =>
            _DatabaseTableName;
        public string DatabasePrimaryKeyField =>
            _DatabasePrimaryKeyField;
        #endregion  

        public virtual void Initialize()
        {
            var t = this.GetType();

            _DatabaseTableName = ((DatabaseTableAttribute)(t.GetCustomAttributes(typeof(DatabaseTableAttribute)).First())).Name;
            _DatabasePrimaryKeyField = ((DatabaseTableAttribute)(t.GetCustomAttributes(typeof(DatabaseTableAttribute)).First())).PrimaryKey;
        }

        public DataTable(string ConnectionString)
        {
            _ConnectionString = ConnectionString;
        }

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }
 
        public void Dispose() =>
            Dispose(true);

        #endregion
    }

    public abstract class DataTable<T> : DataTable where T : DataRow
    {
        protected Operate<T> _Operate;

        public DataRowColl<T> Get(string Where, params KeyValuePair<string,string>[] Parameters)
        {
            var Result = _Operate.Load(Where, Parameters.Select(f => new SqlParameter(f.Key, f.Value)).ToArray());

            return Result;
        }

        public T GetWhereId(Guid Id) => Get("id = @id",
            new KeyValuePair("id", Id.ToString())).FirstOrDefault();

        public void Update(T r)
        {
            if (r.IsAdded)
                

            if (r.IsModified)
                _Operate.Update(r);

        }

        public void Update(DataRowColl<T> rc)
        {
            _Operate.BeginTransaction();

            try
            {
                for (int i = 0; i < rc.Count; i++)
                    Update(rc[i]);

                _Operate.Commit();
            }
            catch (Exception)
            {
                //Log.LogException(e);
                _Operate.Rollback();
            }
        }

        public void Delete(T r)
        {
            if (!r.IsDeleted)
                _Operate.Delete(r);
        }

        public void Delete(DataRowColl<T> rc)
        {
            _Operate.BeginTransaction();

            try
            {
                for (int i = 0; i < rc.Count; i++)
                    Delete(rc[i]);

                _Operate.Commit();
            }
            catch (Exception)
            {
                //Log.LogException(e);
                _Operate.Rollback();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _Operate = new Operate<T>((DataTable)this, _ConnectionString);
        }

        public DataTable(string ConnectionString) :
            base(ConnectionString)
        {

        }
    }
}
