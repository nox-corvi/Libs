using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nox.WebApi;


public enum StateEnum
{
    Success = 0,    // erfolgreich 
    Failure = 1,    // fehlgeschlagen

    Error = 4,      // es ist ein Fehler aufgetreten
}

public class KeyValue(string key = "", string value = "")
{
    public string Key { get; set; } = key;
    public string Value { get; set; } = value;

    public override string ToString()
        => $"{Key}:{Value ?? "<null>"}";
}

public interface IConfig
{
    ILogger Logger { get; }    

    string URL { get; }
}

public class Config
    : IConfig
{
    private readonly ILogger _Logger;
    private readonly string _URL;

    public ILogger Logger { get => _Logger; }
    public string URL { get => _URL; }

    public Config(ILogger Logger, string URL)
    {
        this._Logger = Logger;
        this._URL = URL;
    }
}

public class TokenConfig
    : Config
{
    private readonly KeyValue _Token;

    public KeyValue Token { get => _Token; }

    public TokenConfig(ILogger Logger, string URL)
        : base(Logger, URL) { }

    public TokenConfig(ILogger Logger, string URL, string Token)
        : base(Logger, URL) => this._Token = new KeyValue("Token", Token);
}

public class TokenSecretConfig
    : Nox.WebApi.TokenConfig
{
    private readonly KeyValue _Secret;

    public KeyValue Secret { get => _Secret; }

    public TokenSecretConfig(ILogger Logger, string URL)
        : base(Logger, URL) { }

    public TokenSecretConfig(ILogger Logger, string URL, string Token, string Secret)
        : base(Logger, URL, Token) => this._Secret = new KeyValue("Secret", Secret);
}


#region Interface
public interface IShell
{
    StateEnum State { get; set; }
    string Message { get; set; }

}

public interface IDataShell<T>
    : IShell
    where T : class, new()
{
    List<T> Data { get; }

    T First();
}

public interface IResponseShell
    : IShell
{
    // Spezifische Methoden und Eigenschaften für ResponseShell, falls erforderlich
}

public interface IPostShell
{
}

public interface ISingleDataResponseShell
    : IResponseShell
{
    string AdditionalData1 { get; }
}

public interface ISingleDataPostShell
    : IPostShell
{
    string Value1 { get; }
} 


public interface IDoubleDataResponseShell
    : ISingleDataResponseShell
{
    string AdditionalData2 { get; }
}

public interface IDoubleDataPostShell
    : ISingleDataPostShell
{
    string Value2 { get; }
}

public interface IQuadDataResponseShell
    : IDoubleDataResponseShell
{
    string AdditionalData3 { get; }
    string AdditionalData4 { get; }
}
public interface IQuadeDataPostShell
    : IDoubleDataResponseShell
{
    string Value3 { get; }
    string Value4 { get; }
}

#endregion

[Serializable()]
public class Shell
    : IShell
{
    public static string OK { get; } = "ok";

    public static string NO_RESULT { get; } = "no result";
    public static string NOT_AUTHORIZED { get; } = "not authorized";

    #region Properties
    /// <summary>
    /// Gibt einen Status zurück der angibt ob die Aktion erfolgreich war
    /// </summary>
    public StateEnum State { get; set; } = StateEnum.Success;

    /// <summary>
    /// Gibt eine Statusnachricht zurück.
    /// </summary>
    public string Message { get; set; } = OK;
    #endregion

    public virtual TResult Pass<TResult>()
        where TResult : IShell, new()
        => new() { State = State, Message = Message };

    public static async Task<T> SwitchHandlerAsync<T, U>(U Shell,
        Func<U, Task<T>> OnSuccess, Func<U, Task<T>> OnFailure, Func<string, Task<T>> OnError)
        where T : IShell, new()
        where U : IShell
    {
        try
        {
            return Shell.State switch
            {
                StateEnum.Success => await OnSuccess(Shell),
                StateEnum.Failure => await OnFailure(Shell),
                _ => await OnError(Shell.Message),
            };
        }
        catch (Exception e)
        {
            return await OnError(Helpers.SerializeException(e));
        }
    }

    public static async Task<T> SwitchHandlerAsync<T, U>(U Shell,
        Func<U, Task<T>> OnSuccess, Func<U, Task<T>> OnFailure)
        where T : IShell, new()
        where U : IShell
        => await SwitchHandlerAsync(Shell, OnSuccess, OnFailure, (s) => Task.FromResult(new T() { State = StateEnum.Error, Message = s }));

    public static async Task<T> SwitchHandlerAsync<T, U>(Task<U> Shell,
        Func<U, Task<T>> OnSuccess, Func<U, Task<T>> OnFailure, Func<string, Task<T>> OnError)
        where T : IShell, new()
        where U : IShell
        => await SwitchHandlerAsync(Shell, OnSuccess, OnFailure, OnError);

    public static async Task<T> SwitchHandlerAsync<T, U>(Task<U> Shell,
        Func<U, Task<T>> OnSuccess, Func<U, Task<T>> OnFailure)
        where T : IShell, new()
        where U : IShell
        => await SwitchHandlerAsync(Shell, OnSuccess, OnFailure, (s) => Task.FromResult(new T() { State = StateEnum.Error, Message = s }));

    public static T SwitchHandler<T, U>(U Shell,
        Func<U, T> OnSuccess, Func<U, T> OnFailure, Func<string, T> OnError)
        where T : IShell, new()
        where U : IShell
    {
        try
        {
            return Shell.State switch
            {
                StateEnum.Success => OnSuccess(Shell),
                StateEnum.Failure => OnFailure(Shell),
                _ => OnError(Shell.Message),
            };
        }
        catch (Exception e)
        {
            return OnError(Helpers.SerializeException(e));
        }
    }

    public static T SwitchHandler<T, U>(U Shell,
        Func<U, T> OnSuccess, Func<U, T> OnFailure)
        where T : IShell, new()
        where U : IShell
        => SwitchHandler(Shell, OnSuccess, OnFailure, (s) => new T() { State = StateEnum.Error, Message = s });


    /// <summary>
    /// Verarbeitet eine Shell-Instanz und führt bei Erfolg eine gegebene Funktion aus.
    /// Bei Fehlschlag oder Fehler wird eine neue Shell-Instanz mit entsprechendem Status zurückgegeben.
    /// </summary>
    /// <typeparam name="T">Der Typ, der von Shell abgeleitet ist und zurückgegeben wird.</typeparam>
    /// <param name="shell">Die zu verarbeitende Shell-Instanz.</param>
    /// <param name="onSuccess">Die Funktion, die bei Erfolg ausgeführt wird.</param>
    /// <param name="failureMessage">Optionale Nachricht für den Fall eines Fehlschlags.</param>
    /// <param name="errorMessage">Optionale Nachricht für den Fall eines Fehlers.</param>
    /// <returns>Eine neue Instanz des Typs T, basierend auf dem Ergebnis der Verarbeitung.</returns>
    public static async Task<T> SuccessHandlerAsync<T, U>(U Shell,
        Func<U, Task<T>> OnSuccess,
        string FailureMessage = null, string ErrorMessage = null)
        where T : IShell, new()
        where U : IShell
        => await SwitchHandlerAsync(Shell, OnSuccess,
            OnFailure: (s) =>
            {
                //var q = await s;
                return Task.FromResult(new T()
                {
                    State = StateEnum.Failure,
                    Message = FailureMessage ?? s.Message
                });
            },
            OnError: (s) =>
                Task.FromResult(new T()
                {
                    State = StateEnum.Error,
                    Message = ErrorMessage ?? s
                }));

    public static T SuccessHandler<T, U>(U Shell, Func<U, T> OnSuccess,
        string FailureMessage = null, string ErrorMessage = null)
        where T : IShell, new()
        where U : IShell
        => SwitchHandler(Shell, OnSuccess,
            OnFailure: (s) => new T()
            {
                State = StateEnum.Failure,
                Message = FailureMessage ?? s.Message
            },
            OnError: (s) => new T()
            {
                State = StateEnum.Error,
                Message = ErrorMessage ?? s
            });

    public static async Task<T> FixFailureHandlerAsync<T, U>(Task<U> Shell,
        Func<U, Task<T>> OnSuccess, Func<U, Task<T>> OnFixFailure, string ErrorMessage = null!)
        where T : IShell, new()
        where U : IShell
        => await SwitchHandlerAsync(Shell, OnSuccess,
            OnFailure: async (s) => await SuccessHandlerAsync(await OnFixFailure(s), (q) => OnSuccess(s)),
            OnError: (s) =>
                Task.FromResult(new T()
                {
                    State = StateEnum.Error,
                    Message = ErrorMessage ?? s
                }));

    public static T FixFailureHandler<T, U>(U Shell, Func<U, T> OnSuccess, Func<U, T> OnFixFailure, string ErrorMessage = null!)
        where T : IShell, new()
        where U : IShell
        => SwitchHandler(Shell, OnSuccess,
            OnFailure: (s) => SuccessHandler(OnFixFailure(s), (q) => OnSuccess(s)),
            OnError: (s) =>
                new T()
                {
                    State = StateEnum.Error,
                    Message = ErrorMessage ?? s
                });

    public static async Task<T> NotFoundHandlerAsync<T, U>(Func<Task<U>> Shell,
        Func<U, Task<T>> OnSuccess, Func<Task<T>> OnNotFound, string ErrorMessage = null!)
        where T : IShell, new()
        where U : IShell
        => await SwitchHandlerAsync<T, U>(await Shell(), OnSuccess,
            OnFailure: async (s) =>
            {
                if (s.Message == NO_RESULT)
                    return await SuccessHandlerAsync(await OnNotFound(),
                            async (q) => await OnSuccess(await Shell()));
                else
                    return new T()
                    {
                        State = StateEnum.Failure,
                        Message = s.Message
                    };
            }, OnError: (s) =>
                Task.FromResult(new T()
                {
                    State = StateEnum.Error,
                    Message = ErrorMessage ?? s
                }));

    public static T NotFoundHandler<T, U>(Func<U> Shell,
        Func<U, T> OnSuccess, Func<T> OnNotFound, string ErrorMessage = null)
        where T : IShell, new()
        where U : IShell
        => SwitchHandler(Shell.Invoke(), OnSuccess,
            OnFailure: (s) =>
            {
                if (s.Message == NO_RESULT)
                    return SuccessHandler(OnNotFound(),
                        (s) => OnSuccess(Shell.Invoke()));
                else
                    return new T()
                    {
                        State = StateEnum.Failure,
                        Message = s.Message
                    };
            }, OnError: (s) =>
                new T()
                {
                    State = StateEnum.Error,
                    Message = ErrorMessage ?? s
                });

    public static async Task<T> NotErrorHandlerAsync<T, U>(Task<U> Shell,
        Func<U, Task<T>> OnNotError, string ErrorMessage = null!)
        where T : IShell, new()
        where U : IShell
        => await SwitchHandlerAsync<T, U>(Shell,
            OnSuccess: OnNotError,
            OnFailure: OnNotError,
            OnError: (s) =>
            {
                return Task.FromResult(new T()
                {
                    State = StateEnum.Error,
                    Message = ErrorMessage ?? s
                });
            });

    public static T NotErrorHandler<T, U>(U Shell,
        Func<U, T> OnNotError, string ErrorMessage = null)
        where T : IShell, new()
        where U : IShell
        => SwitchHandler(Shell,
            OnSuccess: OnNotError,
            OnFailure: OnNotError,
            OnError: (s) => new T()
            {
                State = Shell.State,
                Message = ErrorMessage ?? Shell.Message
            });

    public static async Task<T> AndHandlerAsync<T>(
        params Func<Task<T>>[] Shells)
        where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = await Shells[i]();
            switch (Result.State)
            {
                case StateEnum.Success:
                case StateEnum.Failure:
                    continue;
                default:
                    return await Task.FromResult(new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    });
            }
        }

        return new T() { State = StateEnum.Success };
    }
    public static T AndHandler<T, U>(
        params Func<T>[] Shells)
        where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = Shells[i].Invoke();
            switch (Result.State)
            {
                case StateEnum.Success:
                case StateEnum.Failure:
                    continue;
                default:
                    return new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    };
            }
        }

        return new T() { State = StateEnum.Success };
    }

    public static async Task<T> SuccessAndHandlerAsync<T>(
       params Func<Task<T>>[] Shells)
       where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = await Shells[i]();
            switch (Result.State)
            {
                case StateEnum.Success:
                    continue;
                default:
                    return await Task.FromResult(new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    });
            }
        }

        return new T() { State = StateEnum.Success };
    }
    public static T SuccessAndHandler<T, U>(
        params Func<T>[] Shells)
        where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = Shells[i].Invoke();
            switch (Result.State)
            {
                case StateEnum.Success:
                    continue;
                default:
                    return new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    };
            }
        }

        return new T() { State = StateEnum.Success };
    }

    public static async Task<T> OrHandlerAsync<T>(
        params Func<Task<T>>[] Shells)
       where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = await Shells[i].Invoke();
            switch (Result.State)
            {
                case StateEnum.Success:
                case StateEnum.Failure:
                    return await Task.FromResult(new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    });
                default:
                    continue;
            }
        }

        return new T() { State = StateEnum.Failure, Message = $"no success" };
    }

    public static T OrHandler<T>(
        params Func<T>[] Shells)
        where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = Shells[i].Invoke();
            switch (Result.State)
            {
                case StateEnum.Success:
                case StateEnum.Failure:
                    return new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    };
                default:
                    continue;
            }
        }

        return new T() { State = StateEnum.Failure, Message = $"no success" };
    }

    public static async Task<T> SuccessOrHandlerAsync<T>(
        params Func<Task<T>>[] Shells)
       where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = await Shells[i].Invoke();
            switch (Result.State)
            {
                case StateEnum.Success:
                    return await Task.FromResult(new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    });
                default:
                    continue;
            }
        }

        return new T() { State = StateEnum.Failure, Message = $"no success" };
    }

    public static T SuccessOrHandler<T>(
        params Func<T>[] Shells)
        where T : IShell, new()
    {
        for (int i = 0; i < Shells.Length; i++)
        {
            var Result = Shells[i].Invoke();
            switch (Result.State)
            {
                case StateEnum.Success:
                    return new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    };
                default:
                    continue;
            }
        }

        return new T() { State = StateEnum.Failure, Message = $"no success" };
    }

    public static async Task<T> ForHandlerAsync<T>(
        int Start, int Count, Func<int, Task<T>> For)
       where T : IShell, new()
    {
        for (int i = 0; i < Count; i++)
        {
            var Result = await For(i + Start);
            switch (Result.State)
            {
                case StateEnum.Success:
                case StateEnum.Failure:
                    continue;
                default:
                    return await Task.FromResult(new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    });
            }
        }

        return await Task.FromResult(new T() { State = StateEnum.Success });
    }

    public static T ForHandler<T>(
        int Start, int Count, Func<int, T> For)
        where T : IShell, new()
    {
        for (int i = 0; i < Count; i++)
        {
            var Result = For(i + Start);
            switch (Result.State)
            {
                case StateEnum.Success:
                case StateEnum.Failure:
                    continue;
                default:
                    return new T()
                    {
                        State = Result.State,
                        Message = Result.Message
                    };
            }
        }

        return new T() { State = StateEnum.Success };
    }


    public static async Task<T> IfHandlerAsync<T>(Func<bool> CheckIf, Func<Task<T>> OnTrue, Func<Task<T>> OnFalse)
       where T : IShell, new()
    {
        try
        {
            var Result = CheckIf.Invoke();

            if (CheckIf.Invoke())
                return await OnTrue.Invoke();
            else
                return await OnFalse();
        }
        catch (Exception e)
        {
            return (T)Activator.CreateInstance(typeof(T), StateEnum.Error, Helpers.SerializeException(e));
        }
    }

    public static T IfHandler<T>(Func<bool> CheckIf, Func<T> OnTrue, Func<T> OnFalse)
        where T : IShell, new()
    {
        try
        {
            var Result = CheckIf.Invoke();

            if (CheckIf.Invoke())
                return OnTrue.Invoke();
            else
                return OnFalse();
        }
        catch (Exception e)
        {
            return (T)Activator.CreateInstance(typeof(T), StateEnum.Error, Helpers.SerializeException(e));
        }
    }

    //public static void ErrorHandlerAsync<U>(U Shell,
    //    Action<U> Error)
    //    where U : IShell
    //{
    //    if (Shell.State == StateEnum.Error)
    //    {
    //        Error.Invoke(Shell);
    //    }
    //}

    public static void ErrorHandler<U>(U Shell,
        Action<U> Error)
        where U : IShell
    {
        if (Shell.State == StateEnum.Error)
        {
            Error.Invoke(Shell);
        }
    }

    public static T Success<T>(string Message = "")
        where T : IShell, new()
        => (T)Activator.CreateInstance(typeof(T), StateEnum.Success, Message);

    public static T Failure<T>(string Message = "")
        where T : IShell, new()
        => (T)Activator.CreateInstance(typeof(T), StateEnum.Failure, Message);

    public static T Error<T>(string Message = "")
        where T : IShell, new()
        => (T)Activator.CreateInstance(typeof(T), StateEnum.Error, Message);

    public static T NotAuthorized<T>()
        where T : IShell, new()
        => Error<T>(NOT_AUTHORIZED);

    public Shell() { }

    public Shell(StateEnum State)
        => this.State = State;

    public Shell(StateEnum State, string Message)
        : this(State)
        => this.Message = Message;
}

public class Shell<T>
    : Shell, IDataShell<T>
    where T : class, new()
{
    public List<T> Data { get; set; } = [];

    //public override TResult Pass<TResult>()
    //{
    //    var Result = base.Pass<TResult>();
    //    (T)Result.Data = Data;

    //    return Result;
    //}

    public T First()
        => Data.First();

    //public Shell ToShell()
    //    => new Shell()
    //    {
    //        State = this.State,
    //        Message = this.Message,
    //    };


    public Shell()
        : base() { }

    public Shell(StateEnum State)
        : base(State, null) { }

    public Shell(StateEnum State, string Message)
        : base(State, Message) { }
}

public static class XLogExtension
{
    //public static T SuccessHandler<T, U>(U Shell, Func<U, T> OnSuccess,
    //    string FailureMessage = null, string ErrorMessage = null)
    //    where T : IShell, new()
    //    where U : IShell

    public static T LogShell<T>(this ILogger Logger, T Shell,
        string FailureMessage = null, string ErrorMessage = null, [CallerMemberName] string CallerMember = "")
        where T : IShell
    {
        string Message = $"{CallerMember}-Shell: {Shell.State} -> {Shell.Message}";
        switch (Shell.State)
        {
            case StateEnum.Success:
                Logger.LogDebug(Message);
                break;
            case StateEnum.Failure:
                Logger.LogWarning(FailureMessage ?? Shell.Message);
                break;
            case StateEnum.Error:
                Logger.LogError(ErrorMessage ?? Shell.Message);
                break;
        }

        return Shell;
    }

    public static async Task<T> LogShellAsync<T>(this ILogger Logger, T Shell,
        string FailureMessage = null, string ErrorMessage = null, [CallerMemberName] string CallerMember = "")
        where T : IShell
        => await Task.Run(() => LogShell(Logger, Shell, FailureMessage, ErrorMessage, CallerMember));
}


/// <summary>
/// Basisklasse für Antwortobjekte, die den Zustand und die Nachricht einer Antwort kapselt.
/// Kann als Grundlage für spezifischere Antworttypen mit zusätzlichen Daten dienen.
/// </summary>
public class ResponseShell
    : Shell, IResponseShell
{
    public ResponseShell()
        : base() { }

    public ResponseShell(StateEnum State)
        : base(State) { }

    public ResponseShell(StateEnum State, string Message)
        : base(State, Message) { }
}

public class PostShell
    : IPostShell
{
    public PostShell()
        : base() { }
}


/// <summary>
/// Erweiterung der ResponseShell für Antworten, die ein einzelnes zusätzliches Datenfeld enthalten.
/// Geeignet für einfache Antworten, die über den Zustand und die Nachricht hinaus eine zusätzliche Information zurückgeben.
/// </summary>
public class SingleDataResponseShell
    : ResponseShell, ISingleDataResponseShell
{
    /// <summary>
    /// Zusätzliches Datenfeld für die Antwort.
    /// </summary>
    public string AdditionalData1 { get; set; } = "";

    public static SingleDataResponseShell FromShell(IShell Shell, string additionalData1 = "")
        => new SingleDataResponseShell(Shell.State, Shell.Message, additionalData1);

    public SingleDataResponseShell()
        : base() { }

    public SingleDataResponseShell(StateEnum State)
        : base(State) { }

    public SingleDataResponseShell(StateEnum State, string Message)
        : base(State, Message) { }

    public SingleDataResponseShell(StateEnum state, string Message, string additionalData1)
        : base(state, Message)
    {
        AdditionalData1 = additionalData1;
    }
}

public class SingleDataPostShell
    : PostShell, ISingleDataPostShell
{
    public string Value1 { get; set; } = "";

    public SingleDataPostShell()
        : base() { }

    public SingleDataPostShell(string Value1)
        : this() => this.Value1 = Value1;
}

/// <summary>
/// Erweiterung der SingleDataResponseShell für Antworten, die zwei zusätzliche Datenfelder enthalten.
/// Geeignet für Antworten, die eine moderate Menge an zusätzlichen Informationen zurückgeben.
/// </summary>
public class DoubleDataResponseShell
    : SingleDataResponseShell, IDoubleDataResponseShell
{
    /// <summary>
    /// Zweites zusätzliches Datenfeld für die Antwort.
    /// </summary>
    public string AdditionalData2 { get; set; } = "";

    public static DoubleDataResponseShell FromShell(IShell Shell, string additionalData1 = "", string additionalData2 = "")
        => new DoubleDataResponseShell(Shell.State, Shell.Message, additionalData1, additionalData2);

    public DoubleDataResponseShell()
        : base() { }

    public DoubleDataResponseShell(StateEnum State)
        : base(State) { }

    public DoubleDataResponseShell(StateEnum State, string Message)
        : base(State, Message) { }

    public DoubleDataResponseShell(StateEnum state, string Message, string additionalData1)
        : base(state, Message, additionalData1)
    {
        AdditionalData1 = additionalData1;
    }

    public DoubleDataResponseShell(StateEnum state, string Message, string additionalData1, string additionalData2)
        : base(state, Message, additionalData1)
    {
        AdditionalData2 = additionalData2;
    }
}

public class DoubleDataPostShell
    : SingleDataPostShell, IDoubleDataPostShell
{
    public string Value2 { get; set; } = "";

    public DoubleDataPostShell()
        : base() { }

    public DoubleDataPostShell(string Value1)
        : this() => this.Value1 = Value1;

    public DoubleDataPostShell(string Value1, string Value2)
        : this(Value1) => this.Value2 = Value2;
}


/// <summary>
/// Erweiterung der DoubleDataResponseShell für Antworten, die vier zusätzliche Datenfelder enthalten.
/// Geeignet für komplexe Antworten, die eine Vielzahl von zusätzlichen Informationen zurückgeben.
/// </summary>
public class QuadDataResponseShell
    : DoubleDataResponseShell, IQuadDataResponseShell
{
    /// <summary>
    /// Drittes zusätzliches Datenfeld für die Antwort.
    /// </summary>
    public string AdditionalData3 { get; set; } = "";

    /// <summary>
    /// Viertes zusätzliches Datenfeld für die Antwort.
    /// </summary>
    public string AdditionalData4 { get; set; } = "";

    public static QuadDataResponseShell FromShell(IShell Shell, string additionalData1 = "", string additionalData2 = "", string additionalData3 = "", string additionalData4 = "")
        => new QuadDataResponseShell(Shell.State, Shell.Message, additionalData1, additionalData2, additionalData3, additionalData4);

    public QuadDataResponseShell()
        : base() { }

    public QuadDataResponseShell(StateEnum State)
        : base(State) { }

    public QuadDataResponseShell(StateEnum State, string Message)
        : base(State, Message) { }

    public QuadDataResponseShell(StateEnum state, string Message, string additionalData1)
        : base(state, Message, additionalData1)
    {
        AdditionalData1 = additionalData1;
    }

    public QuadDataResponseShell(StateEnum state, string Message, string additionalData1, string additionalData2)
        : base(state, Message, additionalData1)
    {
        AdditionalData2 = additionalData2;
    }

    public QuadDataResponseShell(StateEnum state, string Message, string additionalData1, string additionalData2, string additionalData3, string additionalData4)
        : base(state, Message, additionalData1, additionalData2)
    {
        AdditionalData3 = additionalData3;
        AdditionalData4 = additionalData4;
    }
}


public class QuadDataPostShell
    : DoubleDataPostShell, IPostShell
{
    public string Value3 { get; set; } = "";

    public string Value4 { get; set; } = "";

    public QuadDataPostShell()
        : base() { }

    public QuadDataPostShell(string Value1)
        : this() => this.Value1 = Value1;

    public QuadDataPostShell(string Value1, string Value2)
        : this(Value1) => this.Value2 = Value2;

    public QuadDataPostShell(string Value1, string Value2, string Value3)
        : this(Value1, Value2) => this.Value3 = Value3;
    public QuadDataPostShell(string Value1, string Value2, string Value3, string Value4)
        : this(Value1, Value2, Value3) => this.Value4 = Value4;
}