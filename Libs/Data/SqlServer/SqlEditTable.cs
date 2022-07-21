using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace Nox.Libs.Data.SqlServer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly")]
    [Serializable()]
	public class SqlEditTable : System.Data.DataTable
	{
		private SqlCommand          _Command;
		private SqlDataAdapter      _Adapt;
		private SqlCommandBuilder   _Scb;
		private SqlConnection       _Conn;

		private SqlTransaction      _Transaction;
		private bool                _InTransaction;

		protected bool              _IsSubQuery;

		private string  _ConnectionString = "";
		private int     _SqlCommandTimeout = 300;

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

		private string _LastError = "";
		/// <summary>
		/// Liefert den letzten Fehler zurück.
		/// </summary>
		public string LastError
		{
			get
			{
				var Result = _LastError;
				_LastError = "";

				return Result;
			}
			protected set
			{
				_LastError = value;
			}
		}

		/// <summary>
		/// Liefert zurück ob ein Feld geändert wurde oder legt es fest
		/// </summary>
		public virtual bool Dirty { get; protected set; }

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
				return _InTransaction;
			}
			private set
			{
				_InTransaction = value;
			}
		}

		public bool IsSubQuery
		{
			get
			{
				return _IsSubQuery;
			}
		}
        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL-Abfragen auf Sicherheitsrisiken überprüfen")]
        public void CreateDataTable(string Query)
		{
			if (_Conn != null)
			{
				_Conn = new SqlConnection(_ConnectionString);
				_Conn.Open();
			}

			_Command = new SqlCommand(Query, _Conn);



            if (_Transaction != null)
				_Command.Transaction = _Transaction;

			_Adapt = new SqlDataAdapter(_Command);
			_Scb = new SqlCommandBuilder(_Adapt);

			_Adapt.Fill(this);
		}

        //# Funktion:    Speichert sämtliche Änderungen in der Datenbank
        //# Geändert:    2012-10-15, Erweiterung um Behandlung von Dead-Lock Speicherfehlern.
        private bool UpdateTable(bool EndTransaction = false)
		{
			int RetryCount = 3; bool SqlSuccess = false;
			Random RND = new Random(DateTime.Now.Millisecond);

			while ((RetryCount >= 0) && (!SqlSuccess))
			{
				try
				{
					_Adapt.Update(this);
					SqlSuccess = true;
				}
				catch (SqlException e)
				{
					// sql-error 1205 deadlock occured
					if (e.Number == 1205)
					{
						RetryCount--;
						System.Threading.Thread.Sleep(RND.Next(20, 200));
					}
					else
					{
						if (EndTransaction)
							Rollback();

						return WithError(e.Message);
					}
				}
				catch (Exception e)
				{
					if (EndTransaction)
						Rollback();

					return WithError(e.Message);
				}
			}
			if (EndTransaction)
				Commit();

			return true;
		}

		private SqlEditTable SubQuery(string Query)
		{
			SqlEditTable Result;

			if (_Transaction != null)
				Result = new SqlEditTable(Query, Transaction);
			else
				Result = new SqlEditTable(Query);

			Result._IsSubQuery = true;

			return Result;
		}

		public void BeginTransaction()
		{
			if (!InTransaction)
			{
				_Transaction = _Conn.BeginTransaction();
				if (_Command.Transaction == null)
					_Command.Transaction = _Transaction;

				_InTransaction = true;
			}
		}

		public bool Rollback()
		{
			if ((_Transaction != null) && (InTransaction))
			{
				_Transaction.Rollback();
				_Transaction = null;

				_InTransaction = false;

				return true;
			}
			else
				return false;
		}

		public bool Commit()
		{
			if ((_Transaction != null) && (InTransaction))
			{
				_Transaction.Commit();
				_Transaction = null;

				_InTransaction = false;

				return true;
			}
			else
				return false;
		}

		public bool WithError(string Error)
		{
			LastError = Error;
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsSubQuery)
			{
				if (_Transaction != null)
					Rollback();

				if (_Conn.State == ConnectionState.Open)
					_Conn.Close();
			}
			base.Dispose(disposing);
		}

		public SqlEditTable(string Query, SqlTransaction Transaction)
			: base()
		{
			if (Transaction != null)
			{
				_Conn = _Transaction.Connection;
				if (_Conn.State == ConnectionState.Closed)
					_Conn.Open();
			}
			_Transaction = Transaction;

			CreateDataTable(Query);
		}

		public SqlEditTable(string Query)
			: base()
		{
			CreateDataTable(Query);
		}
	}
}
