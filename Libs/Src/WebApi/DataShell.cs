using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Nox.WebApi;

public class DataShell<T>
    : Shell, IDataShell<T>
    where T : DataRow, new()
{
    private readonly ILogger Logger;

    private DataModel _DataModel = null!;
    private Operate<T> _Operate = null!;

    internal DataModel DataModel
    {
        get => _DataModel;
        set => _Operate = new Operate<T>(_DataModel = value, Hosting.Hosting.CreateDefaultLogger<Operate>());
    }

    internal Operate<T> Operate
    {
        get => _Operate;
    }

    public List<T> Data { get; private set; } = [];

    public T First()
        => Data.First();

    public static ResponseShell ExecuteHandler(Func<int> Execute, string FailureMessage = null, string ErrorMessage = null)
    {
        try
        {
            var Result = Execute();
            return Result switch
            {
                0 => new ResponseShell(StateEnum.Failure, FailureMessage ?? NO_RESULT),
                _ => new ResponseShell(StateEnum.Success, "execute successfull"),
            };
        }
        catch (Exception e)
        {
            return new ResponseShell(StateEnum.Error, ErrorMessage
                ?? Helpers.SerializeException(e));
        }
    }
    
    public DataShell<T> GetWhereId(Guid Id) 
        => Select("id = @id", new SqlParameter("id", Id));

    public static DataShell<T> GetWhereId(DataModel dataModel, Guid Id)
        => ((DataShell<T>)Activator.CreateInstance(typeof(T), dataModel)).GetWhereId(Id);

    public static U GetWhereId<U>(DataModel dataModel, Guid Id)
        where U : DataShell<T>, new()
    {
        try
        {
            var Result = (U)Activator.CreateInstance(typeof(U), dataModel);
            Result.Data = Result.Operate.Load("ID = @ID", new SqlParameter("ID", Id));

            if (Result.Data.Count != 0)
                Result.State = StateEnum.Success;
            else
            {
                Result.State = StateEnum.Failure;
                Result.Message = NO_RESULT;
            }

            return Result;
        }
        catch (Exception ex)
        {
            var Result = (U)Activator.CreateInstance(typeof(U), dataModel);

            Result.State = StateEnum.Error;
            Result.Message = Helpers.SerializeException(ex);

            return Result;
        }
    }

    public List<T> Get()
        => Operate.Load("", null);

    public int Insert(T Data)
        => Operate.Insert(Data);

    public int Update(T Data)
        => Operate.Update(Data);

    public int Delete(T Data)
        => Operate.Delete(Data);

    public int Truncate()
        => Operate.Truncate();

    public DataShell<T> Select(string WhereCondition, params SqlParameter[] Parameters)
    {
        try
        {
            Logger.LogDebug(WhereCondition);
            var Result = new DataShell<T>(Logger)
            {
                Data = Operate.Load(WhereCondition, Parameters)
            };

            if (Result.Data.Count != 0)
                Result.State = StateEnum.Success;
            else
            {
                Result.State = StateEnum.Failure;
                Result.Message = NO_RESULT;
            }

            return Result;
        }
        catch (Exception ex)
        {
            return new DataShell<T>(Logger)
            {
                State = StateEnum.Error,
                Message = Helpers.SerializeException(ex)
            };
        }
    }

    public ResponseShell Exists(string WhereCondition, params SqlParameter[] Parameters)
        => ExecuteHandler(() => Select(WhereCondition, Parameters).Data.Count());

    public Shell<T> ToShell()
        => new()
        {
            State = State,
            Message = Message,
            Data = Data
        };

    public DataShell()
        : base() { }

    public DataShell(DataModel dataModel)
        : base()
    {
        this.DataModel = dataModel;
    }

    public DataShell(ILogger Logger)
        : base()
    {
        this.Logger = Logger ?? (ILogger)Hosting.Hosting.CreateDefaultLogger<DataShell<T>>();
    }

    // DI Constructor
    public DataShell(ILogger<DataShell<T>> Logger)
        : this((ILogger)Logger) { }

    public DataShell(ILogger Logger, DataModel dataModel)
    : this(Logger)
    {
        DataModel = dataModel;
    }

    // DI Constructor
    public DataShell(ILogger<DataShell<T>> Logger, DataModel dataModel)
        : this((ILogger)Logger, dataModel) { }
}