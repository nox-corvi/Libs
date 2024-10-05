using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Nox.WebApi;

public class Operate
    : IDisposable
{
    private readonly ILogger<Operate> Logger = Global.CreateLogger<Operate>();

    public string ConnectionString { get; set; } = "";
    public int SqlCommandTimeout { get; } = 300;

    protected SqlConnection _DatabaseConnection;
    protected SqlTransaction _Transaction;

    #region Database Operations
    /// <summary>
    /// ensure that the database connection object is created and has a valid state. the connection will not open
    /// </summary>
    protected void EnsureConnectionEstablished()
    {
        _DatabaseConnection ??= new SqlConnection(ConnectionString);

        switch (_DatabaseConnection.State)
        {
            case ConnectionState.Broken:
                // dispose and create new ...
                _DatabaseConnection.Dispose();
                _DatabaseConnection = new SqlConnection(ConnectionString);

                break;
            case ConnectionState.Open:
                // keep connection open .. 

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

    /// <summary>
    /// open the database connection 
    /// </summary>
    protected void OpenDatabaseConnection()
    {
        EnsureConnectionEstablished();

        if (_DatabaseConnection.State != ConnectionState.Open)
            _DatabaseConnection.Open();
    }

    /// <summary>
    /// close the database connection. the connection object will retain
    /// </summary>
    protected void CloseDatabaseConnection()
    {
        if (_DatabaseConnection != null)
            switch (_DatabaseConnection.State)
            {
                case ConnectionState.Broken:
                    // he's dead jim .. 

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
        else
            return;
    }

    /// <summary>
    /// ensures the database connection is established and starts a transaction
    /// </summary>
    public void BeginTransaction()
    {
        EnsureConnectionEstablished();
        OpenDatabaseConnection();

        _Transaction ??= _DatabaseConnection.BeginTransaction();
    }

    /// <summary>
    /// rolls back if a transaction is running. otherwise do nothing
    /// </summary>
    public void Rollback()
    {
        if (_Transaction != null)
        {
            _Transaction.Rollback();
            _Transaction = null;
        }
    }

    /// <summary>
    /// commits a transaction if running. otherwise do nothing
    /// </summary>
    public void Commit()
    {
        _Transaction?.Commit();
        _Transaction = null;
    }

    public SqlDataReader GetReader(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters)
    {
        EnsureConnectionEstablished();

        var CMD = new SqlCommand(SQL, _DatabaseConnection)
        {
            CommandType = commandType,
            CommandTimeout = SqlCommandTimeout,

            Transaction = _Transaction
        };

        OpenDatabaseConnection();

        if (Parameters != null)
            foreach (SqlParameter Param in Parameters)
                CMD.Parameters.Add(Param);

     
        DateTime dtStart = DateTime.Now;    
        SqlDataReader rReturn = CMD.ExecuteReader(CommandBehavior.CloseConnection);
        Logger.LogDebug("Execute Time (sec): " + (DateTime.Now - dtStart).TotalSeconds + "\n" + SQL);
        return rReturn;
    }

    public SqlDataReader GetReader(string SQL, params SqlParameter[] Parameters) =>
        GetReader(SQL, CommandType.Text, Parameters);

    public SqlDataReader GetReader(string SQL) =>
        GetReader(SQL, CommandType.Text, null);

    public long Execute(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters)
    {
        EnsureConnectionEstablished();

        using var CMD = new SqlCommand(SQL, _DatabaseConnection)
        {
            CommandType = commandType,
            CommandTimeout = SqlCommandTimeout,

            Transaction = _Transaction
        };

        OpenDatabaseConnection();

        if (Parameters != null)
            foreach (SqlParameter Param in Parameters)
                CMD.Parameters.Add(Param);

        var Result = CMD.ExecuteNonQuery();
        return Result;
    }

    public long Execute(string SQL, params SqlParameter[] Parameters) =>
        Execute(SQL, CommandType.Text, Parameters);

    public long Execute(string SQL) =>
        Execute(SQL, CommandType.Text, null);

    public K GetValue<K>(string SQL, CommandType commandType, SqlParameter[] Parameters, K DefaultValue = default) where K : IComparable
    {
        using (var Reader = GetReader(SQL, commandType, Parameters))
            if (Reader.Read())
            {
                try
                {
                    return Helpers.N<K>(Reader.GetFieldValue<K>(0), DefaultValue);
                }
                catch (Exception)
                {
                    return DefaultValue;
                }
            }

        return DefaultValue;
    }

    public K GetValue<K>(string SQL, SqlParameter[] Parameters, K DefaultValue = default) where K : IComparable =>
        GetValue<K>(SQL, CommandType.Text, Parameters, DefaultValue);

    public K GetValue<K>(string SQL, K DefaultValue = default) where K : IComparable =>
        GetValue<K>(SQL, CommandType.Text, null, DefaultValue);

    public K GetValue<K>(string SQL) where K : IComparable =>
        GetValue<K>(SQL, CommandType.Text, null, default);
    #endregion

    public Operate(string ConnectionString)
        : base()
    {
        Logger.LogDebug("test");
        this.ConnectionString = ConnectionString;
    }

    public Operate Clone(Operate operate)
        => new(operate.ConnectionString)
        {
            _DatabaseConnection = operate._DatabaseConnection,
            _Transaction = operate._Transaction,
        };

    public Operate(Operate operate)
    {
        ConnectionString = operate.ConnectionString;
        SqlCommandTimeout = operate.SqlCommandTimeout;

        _DatabaseConnection = operate._DatabaseConnection;
        _Transaction = operate._Transaction;
    }

    public virtual void Dispose()
    {
        Rollback();
        CloseDatabaseConnection();

        _DatabaseConnection?.Dispose();
    }
}

public abstract class OperateMods
{
    public abstract void BeforeUpdate(DataRow r, CancelEventArgs e);

    public abstract void BeforeInsert(DataRow r, CancelEventArgs e);

    public abstract void BeforeDelete(DataRow r, CancelEventArgs e);

}

//public class OperateNullMods
//    : OperateMods
//{
//    public override void BeforeInsert(DataRow r, CancelEventArgs e)
//    {

//    }

//    public override void BeforeUpdate(DataRow r, CancelEventArgs e)
//    {

//    }

//    public override void BeforeDelete(DataRow r, CancelEventArgs e)
//    {

//    }
//}


/// <summary>
/// Stellt Werkzeuge zur Datenmanipulation zur Verfügung
/// </summary>
public class Operate<T>
    : Operate//, ISeed 
    where T : DataRow
{
    private readonly ILogger<Operate<T>> Logger = Global.CreateLogger<Operate<T>>();

    //public Guid ObjectId { get; } = Guid.NewGuid();
    protected string _TableSource;
    protected string _PrimaryKeyField;

    protected TableDescriptor _TableDescriptor;
    private readonly PropertyDescriptor _PrimaryKeyPropertyDescriptor;

    protected DataRow dataRow;

    #region Properties
    public string TableSource { get => _TableSource; }
    public string PrimaryKey { get => _PrimaryKeyField; }

    public OperateMods OperateMods { get; set; }
    #endregion

    #region Stmt
    private string CreateSchemaSelect =>
        $"SELECT TOP 0 * FROM {_TableSource}";

    private string CreateKeySelect =>
        $"SELECT * FROM {TableSource} WHERE {PrimaryKey} = @{PrimaryKey}";

    private string CreateExistsSelect(string SubSelect) =>
        $"SELECT CASE WHEN EXISTS ({SubSelect}) THEN 1 ELSE 0 END";


    private string CreateSelectStmt(string WhereCondition)
    {
        return CreateSelectStmt(WhereCondition);
    }



    private string CreateSelectStmt(string WhereCondition, string order, int limit)
    {

        String TopTemplate = "";

        String sTemplate = ($"SELECT * FROM {TableSource} ");

        if (limit >= 0)
        {
            TopTemplate = " TOP " + limit.ToString() + " ";
            sTemplate = ($"SELECT {TopTemplate} * FROM {TableSource} ");
        }

        var stmt = new StringBuilder(sTemplate);
        //if (Suffix.StartsWith("LEFT JOIN", StringComparison.InvariantCultureIgnoreCase) ||
        //    Suffix.StartsWith("LEFT OUTER JOIN", StringComparison.InvariantCultureIgnoreCase) ||
        //    Suffix.StartsWith("RIGHT JOIN", StringComparison.InvariantCultureIgnoreCase) ||
        //    Suffix.StartsWith("RIGHT OUTER JOIN", StringComparison.InvariantCultureIgnoreCase) ||
        //    Suffix.StartsWith("INNER JOIN", StringComparison.InvariantCultureIgnoreCase))
        //{
        //    stmt.Append(Suffix);
        //}
        //else
        //    stmt.Append(" WHERE ").Append(Suffix);

        if (!WhereCondition.Trim().StartsWith("WHERE", StringComparison.InvariantCultureIgnoreCase))
            if (!string.IsNullOrEmpty(WhereCondition))
            {
                    stmt.Append(" WHERE ");            
            }
        stmt.Append(WhereCondition);


        if (!order.Trim().StartsWith("ORDER", StringComparison.InvariantCultureIgnoreCase))
            if (!string.IsNullOrEmpty(order))
            {  
                    stmt.Append(" ORDER "); 
            }

        stmt.Append(order);

        return stmt.ToString();
    }


    private string CreateSelectStmt(List<string> FieldList, string Suffix)
    {
        StringBuilder Fields = new();

        for (int i = 0; i < FieldList.Count; i++)
            Fields.Append(i > 0 ? ", " : "").Append(FieldList[i]);

        var stmt = new StringBuilder($"SELECT {Fields} FROM {TableSource}");

        if (!Suffix.StartsWith("WHERE", StringComparison.InvariantCultureIgnoreCase))
            stmt.Append(" WHERE ");

        stmt.Append(Suffix);

        return stmt.ToString();
    }

    private string CreateInsertStmt(List<string> FieldList)
    {
        StringBuilder Fields = new(), Values = new();

        for (int i = 0; i < FieldList.Count; i++)
        {
            Fields.Append(i > 0 ? ", " : "").Append(FieldList[i]);
            Values.Append(i > 0 ? ", " : "").Append("@" + FieldList[i]);
        }

        return $"INSERT INTO {TableSource}({Fields}) VALUES({Values})";
    }

    private string CreateUpdateStmt(List<string> FieldList)
    {
        var FieldValuePair = new StringBuilder();

        for (int i = 0; i < FieldList.Count; i++)
            FieldValuePair.Append(i > 0 ? ", " : "").Append(FieldList[i] + " = @" + FieldList[i]);

        return $"UPDATE {TableSource} SET {FieldValuePair} WHERE {PrimaryKey} = @{PrimaryKey}";
    }
    private string CreateDeleteStmt =>
        $"DELETE FROM {TableSource} WHERE {PrimaryKey} = @{PrimaryKey}";

    private string CreateTruncateStmt() =>
        $"TRUNCATE TABLE {TableSource}";
    #endregion

    #region Database Operations
    public bool Exists(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters) =>
        GetValue<int>(CreateExistsSelect(SQL), commandType, Parameters) == 1;

    public bool Exists(string SQL, params SqlParameter[] Parameters) =>
        Exists(SQL, CommandType.Text, Parameters);

    public bool Exists(string SQL) =>
        Exists(SQL, CommandType.Text, null);
    #endregion

    public int Insert(T row)
    {
        var e = new CancelEventArgs();
        OperateMods?.BeforeInsert(row, e);

        if (e.Cancel)
            return -1;


        var KeyFieldValue = row.GetPropertyValue<Guid>(_PrimaryKeyPropertyDescriptor.Property);

        if (!Exists(CreateKeySelect, new SqlParameter($"@{PrimaryKey}", KeyFieldValue)))
        {
            var Fields = new List<string>();
            var Params = new List<SqlParameter>();

            // add key
            Fields.Add(_PrimaryKeyPropertyDescriptor.Name);
            Params.Add(new SqlParameter($"@{_PrimaryKeyPropertyDescriptor.Source}", KeyFieldValue));

            // add data
            foreach (var pd in _TableDescriptor.Where(f => !f.IsPrimaryKey))
            {
                var md = pd.MappingDescriptor;
                var cd = md.CastDescriptor;

                Fields.Add(pd.Source);

                var sqlParam = new SqlParameter($"@{pd.Source}", cd.TargetType);

                var value = row.GetPropertyValue(pd.Property);
                if (value == null)
                    sqlParam.Value = DBNull.Value;
                else
                    sqlParam.Value = value;

                Params.Add(sqlParam);
            }

            // and go ... 
            return (int)Execute(CreateInsertStmt(Fields), [.. Params]);
        }
        else
            throw new Exception("row already exists");
    }

    public int Update(T row)
    {
        var e = new CancelEventArgs();
        OperateMods?.BeforeUpdate(row, e);

        if (e.Cancel)
            return -1;

        // get primary key of row ...
        var KeyFieldValue = row.GetPropertyValue<Guid>(_PrimaryKeyPropertyDescriptor.Property);

        // test is row exists ... 
        if (Exists(CreateKeySelect, new SqlParameter($"@{PrimaryKey}", KeyFieldValue)))
        {
            var Fields = new List<string>();
            var Params = new List<SqlParameter>();

            foreach (var pd in _TableDescriptor.Where(f => !f.IsPrimaryKey))
            {
                var md = pd.MappingDescriptor;
                var cd = md.CastDescriptor;

                Fields.Add(pd.Source);

                var sqlParam = new SqlParameter($"@{pd.Source}", cd.TargetType);

                var value = row.GetPropertyValue(pd.Property);
                if (value == null)
                    sqlParam.Value = DBNull.Value;
                else
                    sqlParam.Value = value;

                Params.Add(sqlParam);
            }

            // add parameter used in where-condition ... 
            Params.Add(new SqlParameter($"@{_PrimaryKeyPropertyDescriptor.Name}", KeyFieldValue));

            // and go ... 
            return (int)Execute(CreateUpdateStmt(Fields), [.. Params]);
        }
        else
            throw new Exception("row not found");
    }

    public int Delete(T row)
    {
        var e = new CancelEventArgs();
        OperateMods?.BeforeDelete(row, e);

        if (e.Cancel)
            return -1;


        var KeyFieldValue = row.GetPropertyValue<Guid>(_PrimaryKeyPropertyDescriptor.Property);
        return (int)Execute(CreateDeleteStmt, new SqlParameter($"@{_PrimaryKeyPropertyDescriptor.Name}", KeyFieldValue));
    }

    public int Truncate()
        => (int)Execute(CreateTruncateStmt());

    public void Schema()
    {
        EnsureConnectionEstablished();
        OpenDatabaseConnection();
    }

    public List<T> Load(string WhereCondition, params SqlParameter[] Parameters)
    {
        return Load(WhereCondition, "", -1, Parameters);
    }

    public List<T> Load(string WhereCondition, string Order, int limit, params SqlParameter[] Parameters)
    {
        var Result = new List<T>();

        Logger.LogDebug($"{nameof(Operate)}::Load({WhereCondition}{Order})");

        using (var r = GetReader(CreateSelectStmt(WhereCondition, Order, limit), Parameters))
            while (r.Read())
            {
                var NewRow = (T)Activator.CreateInstance(typeof(T));
                // add primary key ..
                _PrimaryKeyPropertyDescriptor.Property.SetValue(NewRow, (r.GetValue(r.GetOrdinal(_PrimaryKeyPropertyDescriptor.Name))));

                // add data ... 
                foreach (var md in _TableDescriptor.Where(f => !f.IsPrimaryKey))
                {
                    var data = r.GetValue(r.GetOrdinal(md.Source));

                    // Test if DBNull, use null instead ...
                    if (!Convert.IsDBNull(data))
                        md.Property.SetValue(NewRow, data);
                    else
                        md.Property.SetValue(NewRow, null);
                }
                //NewRow.AcceptChanges();

                Result.Add(NewRow);
            }

        return Result;
    }



    public List<T> LoadRAW(string SQL)
    {
        var Result = new List<T>();

        Logger.LogDebug($"{nameof(Operate)}::Load({SQL})");

        using (var r = GetReader(SQL))
            while (r.Read())
            {
                var NewRow = (T)Activator.CreateInstance(typeof(T));
                // add primary key ..
                _PrimaryKeyPropertyDescriptor.Property.SetValue(NewRow, (r.GetValue(r.GetOrdinal(_PrimaryKeyPropertyDescriptor.Name))));

                // add data ... 
                foreach (var md in _TableDescriptor.Where(f => !f.IsPrimaryKey))
                {
                    var data = r.GetValue(r.GetOrdinal(md.Source));

                    // Test if DBNull, use null instead ...
                    if (!Convert.IsDBNull(data))
                        md.Property.SetValue(NewRow, data);
                    else
                        md.Property.SetValue(NewRow, null);
                }
                //NewRow.AcceptChanges();

                Result.Add(NewRow);
            }

        return Result;
    }



    public Operate(DataModel dataModel)
        : base(dataModel.Operate)
    {
        var data = dataModel.GetType();

        _TableDescriptor = dataModel.GetTableDescriptor($"{typeof(T).Namespace}.{typeof(T).Name}");

        _TableSource = _TableDescriptor.TableSource;
        _PrimaryKeyField = _TableDescriptor.Where(f => f.IsPrimaryKey).FirstOrDefault()?.Source;

        _PrimaryKeyPropertyDescriptor = _TableDescriptor.Where(f => f.IsPrimaryKey).FirstOrDefault();
    }
}
