using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace Nox.Libs.Data.SqlServer
{
	public class SqlDbAccess : IDisposable
	{
		protected string  _ConnectionString = "";
        protected int _SqlCommandTimeout = 300;

		protected SqlConnection _DatabaseConnection;

        protected SqlTransaction _Transaction;

		#region Properties
		public string ConnectionString
		{
			get
			{
				return _ConnectionString;
			}
		}

		public int SqlCommandTimeout
		{
			get
			{
				return _SqlCommandTimeout;
			}
			set
			{
				_SqlCommandTimeout = value;
			}
		}

		public SqlTransaction Transaction
		{
			get
			{
				return _Transaction;
			}
		}
		public bool InTransaction
		{
			get
			{
				return _Transaction != null;
			}
		}
		#endregion

		#region Transaction
		public void BeginTransaction()
		{
			if (!InTransaction)
				_Transaction = _DatabaseConnection.BeginTransaction();
		}

		public bool Rollback()
		{
			if (InTransaction)
			{
				_Transaction.Rollback();
				_Transaction = null;

				return true;
			}
			else
				return false;
		}


		public bool Commit()
		{
			if (InTransaction)
			{
				_Transaction.Commit();
				_Transaction = null;

				return true;
			}
			else
				return false;
		}
        #endregion


        protected void EnsureConnectionEstablished()
        {
            if ((_DatabaseConnection == null) || _DatabaseConnection.State != ConnectionState.Open)
                _DatabaseConnection = new SqlConnection(_ConnectionString);

            switch (_DatabaseConnection.State)
            {
                case ConnectionState.Broken:
                    // dispose and create new ...
                    _DatabaseConnection.Dispose();
                    _DatabaseConnection = new SqlConnection(_ConnectionString);

                    break;
                case ConnectionState.Open:
                    _DatabaseConnection.Close();

                    break;
                case ConnectionState.Closed:
                    break;

                case ConnectionState.Connecting:
                case ConnectionState.Executing:
                case ConnectionState.Fetching:
                    // already in use, quit
                    
                    throw new InvalidOperationException("connection already in use");
            }
        }

        protected void OpenDatabaseConnection()
        {
            if (_DatabaseConnection.State != ConnectionState.Open)
                _DatabaseConnection.Open();
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL-Abfragen auf Sicherheitsrisiken überprüfen")]
        public SqlDataReader GetReader(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters)
		{
            EnsureConnectionEstablished();

            SqlCommand CMD = new SqlCommand(SQL, _DatabaseConnection)
			{
				CommandTimeout = SqlCommandTimeout,
				Transaction = _Transaction,
				CommandType = commandType
			};

            OpenDatabaseConnection();

			if (Parameters != null)
				foreach (SqlParameter Param in Parameters)
					CMD.Parameters.AddWithValue(Param.ParameterName, Param.Value);

			return CMD.ExecuteReader(CommandBehavior.CloseConnection);
		}

        public SqlDataReader GetReader(string SQL, params SqlParameter[] Parameters) =>
            GetReader(SQL, CommandType.Text, Parameters);

        public SqlDataReader GetReader(string SQL) =>
            GetReader(SQL, CommandType.Text, null);
		
        public bool Exists(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters)
        {
            using (var Reader = GetReader(SQL, commandType, Parameters))
                return (Reader.Read());
        }

        public bool Exists(string SQL, params SqlParameter[] Parameters)
        {
            using (var Reader = GetReader(SQL, CommandType.Text, Parameters))
                return (Reader.Read());
        }

        public bool Exists(string SQL)
        {
            using (var Reader = GetReader(SQL, CommandType.Text, null))
                return (Reader.Read());
        }

        public long Execute(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters)
		{
            EnsureConnectionEstablished();

            using (SqlCommand CMD = new SqlCommand(SQL, _DatabaseConnection)
			{
				CommandTimeout = SqlCommandTimeout,
                CommandType = commandType,
				Transaction = _Transaction
			})
			{
                OpenDatabaseConnection();

				if (Parameters != null)
					foreach (SqlParameter Param in Parameters)
						CMD.Parameters.AddWithValue(Param.ParameterName, Param.Value);

				var Result = CMD.ExecuteNonQuery();
				return Result;
			}
		}

        public long Execute(string SQL, params SqlParameter[] Parameters) =>
            Execute(SQL, CommandType.Text, Parameters);

        public long Execute(string SQL) =>
            Execute(SQL, CommandType.Text, null);

        //[System.Diagnostics.DebuggerStepThrough()]
        public T GetValue<T>(string SQL, SqlParameter[] Parameters, T Default = default) where T : IComparable
		{
			using (var Reader = GetReader(SQL, Parameters))
				if (Reader.Read())
				{
					try
					{
						return Helpers.N<T>(Reader.GetFieldValue<T>(0), Default);
					}
					catch (Exception)
					{
						return Default;
					}
				}

			return Default;
		}

		//[System.Diagnostics.DebuggerStepThrough()]
		public T GetValue<T>(string SQL, T Default = default) where T : IComparable
		{
			return GetValue<T>(SQL, null, Default);
		}

		//[System.Diagnostics.DebuggerStepThrough()]
		public List<T> GetValues<T>(string SQL, SqlParameter[] Parameters, T Default = default) where T : IComparable
		{
			var Result = new List<T>();

			using (var Reader = GetReader(SQL, Parameters))
				while (Reader.Read())
				{
					T Value = default;
					try
					{
						Value = Helpers.N<T>(Reader.GetFieldValue<T>(0), Default);
					}
					catch (Exception)
					{
						Value = Default;
					}
					finally
					{
						Result.Add(Value);
					}
				}

			return Result;
		}

		//[System.Diagnostics.DebuggerStepThrough()]
		public List<T> GetValues<T>(string SQL, T Default = default) where T : IComparable =>
			GetValues<T>(SQL, null, Default);

		public SqlDbAccess(string ConnectionString)
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
                    if (InTransaction)
                        Rollback();

                    if (_DatabaseConnection != null)
                        if (_DatabaseConnection.State != ConnectionState.Closed)
                            _DatabaseConnection.Close();

                    _DatabaseConnection?.Dispose();
                }
            }
            this.disposedValue = true;
        }

        public void Dispose() =>
            Dispose(true);
        #endregion
    }
}
