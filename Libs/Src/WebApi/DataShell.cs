using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


namespace Nox.WebApi;

//public interface IDataShell<T>
//    : IShell<T>
//    where T : IDataRow
//{

//    T GetWhereId(Guid Id);
//    List<T> Get();

//    int Insert(T Data);
//    int Update(T Data);
//    int Delete(T Data);
//    int Truncate();

//    U Select<U>(string WhereCondition, params SqlParameter[] Parameters)
//        where U : IDataShell<T>;
//}


[Serializable()]
public class DataShell<T>
    : Shell<T>, IShell<T>
    where T : DataRow
{
    [Newtonsoft.Json.JsonIgnore]
    private DataModel _DataModel = null!;

    [Newtonsoft.Json.JsonIgnore]
    private Operate<T> _Operate = null!;

    [Newtonsoft.Json.JsonIgnore]
    public DataModel DataModel
    {
        get => _DataModel;
        set
        {
            _Operate = new Operate<T>(_DataModel = value);
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    public Operate<T> Operate
    {
        get => _Operate;
    }

    #region Helpers
    //public static ResponseShell UpdateOrInsert<U>(
    //    Func<DataShell<T>> Get, T data,
    //    string SuccessMessage = null, string FailureMessage = null, string ErrorMessage = null)
    //    where U : DataShell<T>
    //{
    //    try
    //    {
    //        var GetResult = Get();
    //        switch (GetResult.State)
    //        {
    //            case StateEnum.Success:
    //                return new ResponseShell(StateEnum.Success, SuccessMessage ?? "already exists");
    //            case StateEnum.Failure:
    //                var InsertResult = DataShell<T>.Insert<U>(;
    //                switch (InsertResult)
    //                {
    //                    case 0:
    //                        return new ResponseShell(StateEnum.Failure, FailureMessage ?? "no rows affected");
    //                    default:
    //                        return new ResponseShell(StateEnum.Success, "execute successfull");
    //                }
    //            default:
    //                return new ResponseShell(StateEnum.Error, ErrorMessage ?? "error");
    //        }

    //    }
    //    catch (Exception e)
    //    {
    //        return new ResponseShell(StateEnum.Error, ErrorMessage ?? SerializeException(e));
    //    }
    //}

    public static ResponseShell ExecuteHandler(Func<int> Execute, string FailureMessage = null, string ErrorMessage = null)
    {
        try
        {
            var Result = Execute();
            switch (Result)
            {
                case 0:
                    return new ResponseShell(StateEnum.Failure, FailureMessage ?? "no rows affected");
                default:
                    return new ResponseShell(StateEnum.Success, "execute successfull");
            }
        }
        catch (Exception e)
        {
            return new ResponseShell(StateEnum.Error, ErrorMessage ?? SerializeException(e));
        }
    }
    #endregion

    public T GetWhereId(Guid Id) =>
        Operate.Load("ID = @ID", new SqlParameter("ID", Id)).First();

    public List<T> Get() =>
        Operate.Load("", null);

    public int Insert(T Data)
        => Operate.Insert(Data);

    public static int Insert<U>(DataModel dataModel, T Data)
        where U : DataShell<T>, new()
        => ((U)Activator.CreateInstance(typeof(U), dataModel)).Insert(Data);

    public int Update(T Data)
        => Operate.Update(Data);
    public static int Update<U>(DataModel dataModel, T Data)
        where U : DataShell<T>, new()
        => ((U)Activator.CreateInstance(typeof(U), dataModel)).Update(Data);

    public int Delete(T Data)
        => Operate.Delete(Data);
    public static int Delete<U>(DataModel dataModel, T Data)
        where U : DataShell<T>, new()
        => ((U)Activator.CreateInstance(typeof(U), dataModel)).Delete(Data);

    public int Truncate()
        => Operate.Truncate();
    public static void Truncate<U>(DataModel dataModel)
        where U : DataShell<T>
        => ((U)Activator.CreateInstance(typeof(U), dataModel)).Truncate();

    public U Select<U>(string WhereCondition, params SqlParameter[] Parameters)
        where U : DataShell<T>, new()
        => Select<U>(DataModel, WhereCondition, Parameters);

    public static U Select<U>(DataModel dataModel, string WhereCondition, params SqlParameter[] Parameters)
        where U : DataShell<T>, new()
    {
        try
        {
            var Result = (U)Activator.CreateInstance(typeof(U), dataModel);
            Result.Data = Result.Operate.Load(WhereCondition, Parameters);

            if (Result.Data.Count != 0)
                Result.State = StateEnum.Success;
            else
            {
                Result.State = StateEnum.Failure;
                Result.Message = "no result";
            }

            return Result;
        }
        catch (Exception ex)
        {
            var Result = (U)Activator.CreateInstance(typeof(U), dataModel);

            Result.State = StateEnum.Error;
            Result.Message = SerializeException(ex);

            return Result;
        }
    }

    public DataShell(DataModel dataModel)
    : this()
    {
        DataModel = dataModel;
    }

    public DataShell()
        : base()
    {

    }
}
