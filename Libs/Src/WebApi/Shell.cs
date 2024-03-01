using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace Nox.WebApi;

[Flags]
public enum StateEnum
{
    Success = 0,    // erfolgreich 
    Failure = 1,     // nicht erfolgreich

    Error = 4,      // es ist ein fehler aufgetreten
}

[Serializable()]
public class Shell
{
    #region Properties
    /// <summary>
    /// Gibt einen Status zurück der angibt ob die Aktion erfolgreich war
    /// </summary>
    public StateEnum State { get; set; } = StateEnum.Success;

    /// <summary>
    /// Gibt eine Statusnachricht zurück.
    /// </summary>
    public string Message { get; set; } = "";
    #endregion

    public Shell() { }
}

[Serializable()]
public abstract class Shell<T>
       : Shell
{
    /// <summary>
    /// Gibt das Datenobjekt vom Typ ISeed zurück wenn erfolgreich, sonst null
    /// </summary>
    public abstract List<T> Data { get; set; }

    #region Helpers
    protected static string SerializeException(Exception ex)
    {
        var Result = new StringBuilder();

        if (ex != null)
        {
            Result.Append($"<exception>");
            Result.Append($"<source>{ex.Source}</source>");
            Result.Append($"<message>{ex.Message}</message>");
            Result.Append($"<stacktrace>{ex.StackTrace}</stacktrace>");
            Result.Append($"<data>");
            foreach (var Item in ex.Data.Keys)
                Result.Append($"<{Item}>{ex.Data[Item].ToString()}</{Item}>");

            if (ex.InnerException != null)
                Result.Append(SerializeException(ex.InnerException));

            Result.Append($"</data>");

            Result.Append($"</exception>");
        }
        else
            Result.Append($"<exception />");

        return Result.ToString();
    }
    #endregion

    public string SerializeData()
    {
        XmlSerializer writer = new XmlSerializer(typeof(T));
        using (StringWriter file = new StringWriter())
        {
            writer.Serialize(file, Data);
            return file.ToString();
        }
    }

    public Shell()
        : base() { }
}

public class ResponseShell
    : Shell
{
    //public bool Response { get; set; } = default;

    public string AdditionalData1 { get; set; } = "";
    public string AdditionalData2 { get; set; } = "";
    public string AdditionalData3 { get; set; } = "";
    public string AdditionalData4 { get; set; } = "";

    #region Helpers
    public static ResponseShell DefaultHandler(Shell Shell, Func<ResponseShell> OnSuccess, 
        string FailureMessage = null, string ErrorMessage = null)
    {
        switch (Shell.State)
        {
            case StateEnum.Success:
                return Helpers.OnXTry(OnSuccess, (e) => new ResponseShell(StateEnum.Error, e.ToString()));
            case StateEnum.Failure:
                return new ResponseShell(StateEnum.Failure, FailureMessage ?? Shell.Message);
            default:
                return new ResponseShell(StateEnum.Error, ErrorMessage ?? Shell.Message);
        }
    }
    public static ResponseShell FromShell(Shell Shell)
    {
        switch (Shell.State)
        {
            case StateEnum.Success:
                return new ResponseShell(StateEnum.Success); //, true);
            case StateEnum.Failure:
                return new ResponseShell(StateEnum.Failure, Shell.Message);
            default:
                return new ResponseShell(StateEnum.Error, Shell.Message);
        }
    }
    #endregion

    public ResponseShell()
    {
    }

    public ResponseShell(StateEnum State)//, bool Response)
    {
        this.State = State;
        //this.Response = Response;
    }

    public ResponseShell(StateEnum State, string Message)
        : this(State) //, Response)
    {
        this.Message = Message;
    }
}

//public class ResponseShell
//    : Shell
//{
//    //public bool Response { get; set; } = default;
//    public string ResponseMessage { get; set; } = null;

//    public string AdditionalData1 { get; set; } = "";
//    public string AdditionalData2 { get; set; } = "";
//    public string AdditionalData3 { get; set; } = "";
//    public string AdditionalData4 { get; set; } = "";

//    #region Helpers
//    //public static ResponseShell DefaultHandler(Shell Shell, Func<ResponseShell> OnSuccess)
//    //{
//    //    switch (Shell.State)
//    //    {
//    //        case StateEnum.Success:
//    //            return Helpers.OnXTry(OnSuccess, (e) => new ResponseShell(StateEnum.Error, false, e.ToString()));
//    //        case StateEnum.NoResult:
//    //            return new ResponseShell(StateEnum.NoResult, false, Shell.Message);
//    //        default:
//    //            return new ResponseShell(StateEnum.Error, false, Shell.Message);
//    //    }
//    //}

//    public static ResponseShell WithSuccess(string Message)
//        => new ResponseShell(StateEnum.Success, true, Message);

//    public static ResponseShell WithNoResult(string Message)
//        => new ResponseShell(StateEnum.NoResult, false, Message);

//    public static ResponseShell WithError(string Message)
//        => new ResponseShell(StateEnum.Error, false, Message);


//    public static ResponseShell FromShell(Shell Shell, string NoResultMessage)
//    {
//        switch (Shell.State)
//        {
//            case StateEnum.Success:
//                return new ResponseShell(StateEnum.Success, true, "");
//            case StateEnum.NoResult:
//                return new ResponseShell(StateEnum.NoResult, false, NoResultMessage);
//            default:
//                return new ResponseShell(StateEnum.Error, false, Shell.Message);
//        }
//    }
//    #endregion

//    public ResponseShell()
//    {
//    }

//    public ResponseShell(StateEnum state)
//        => this.State = state;

//    public ResponseShell(bool Response, string ResponseMessage)
//    {
//        this.Response = Response;
//        this.ResponseMessage = ResponseMessage;
//    }

//    public ResponseShell(StateEnum state, bool Response, string ResponseMessage)
//        : this(Response, ResponseMessage)
//        => this.State = state;
//}
