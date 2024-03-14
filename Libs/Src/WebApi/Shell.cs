using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace Nox.WebApi;


[Flags]
public enum StateEnum
{
    Success = 0,    // erfolgreich 
    Failure = 1,    // nicht erfolgreich

    Error = 4,      // es ist ein fehler aufgetreten
}


#region Interface
public interface IShell
{
    StateEnum State { get; set; }
    string Message { get; set; }
}

public interface IShell<T> 
    : IShell
{
    List<T> Data { get; }

    T First();
}

public interface IResponseShell 
    : IShell
{
    // Spezifische Methoden und Eigenschaften für ResponseShell, falls erforderlich
}

public interface ISingleDataResponseShell 
    : IResponseShell
{
    string AdditionalData1 { get; }
}

public interface IDoubleDataResponseShell 
    : ISingleDataResponseShell
{
    string AdditionalData2 { get; }
}

public interface IQuadDataResponseShell 
    : IDoubleDataResponseShell
{
    string AdditionalData3 { get; }
    string AdditionalData4 { get; }
}
#endregion

[Serializable()]
public class Shell
    : IShell
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
    public static T DefaultHandler<T>(Shell shell, Func<T> OnSuccess, 
        string FailureMessage = null, string ErrorMessage = null) where T : IShell, new()
    {
        var result = new T();

        try
        {
            switch (shell.State)
            {
                case StateEnum.Success:
                    result = OnSuccess();
                    break;
                case StateEnum.Failure:
                    result.State = StateEnum.Failure;
                    result.Message = FailureMessage ?? shell.Message;
                    break;
                default:
                    result.State = StateEnum.Error;
                    result.Message = ErrorMessage ?? shell.Message;
                    break;
            }
        }
        catch (Exception e)
        {
            result.State = StateEnum.Error;
            result.Message = ErrorMessage ?? e.Message;
        }

        return result;
    }

    public Shell() { }

    public Shell(StateEnum State)
        => this.State = State;

    public Shell(StateEnum State, string Message)
        : this(State)
        => this.Message = Message;
}

[Serializable()]
public class Shell<T>
    : Shell, IShell<T>
{
    /// <summary>
    /// Gibt das Datenobjekt vom Typ T zurück, wenn erfolgreich, sonst null.
    /// </summary>
    public virtual List<T> Data { get; set; } = null!;

    #region Helpers
    /// <summary>
    /// Serialisiert eine Exception und ihre InnerException(s) in eine XML-Struktur.
    /// </summary>
    /// <param name="ex">Die zu serialisierende Exception.</param>
    /// <returns>
    /// Eine XML-String-Repräsentation der Exception. Beinhaltet Elemente für Quelle, Nachricht,
    /// Stacktrace und alle benutzerdefinierten Daten, die im Data-Property der Exception gespeichert sind.
    /// Für jede InnerException wird dieser Prozess rekursiv wiederholt, um eine vollständige Hierarchie der Fehlerursache darzustellen.
    /// Wenn das übergebene Exception-Objekt null ist, wird ein selbstschließendes <exception />-Element zurückgegeben.
    /// </returns>
    /// <remarks>
    /// Diese Methode ist nützlich für Logging-Zwecke oder zur Fehleranalyse, da sie eine detaillierte und strukturierte Darstellung
    /// von Fehlerinformationen bietet. Durch die Verwendung von XML als Format ist die Ausgabe leicht lesbar und kann
    /// für die Weiterverarbeitung oder Anzeige in verschiedenen Tools und Umgebungen verwendet werden.
    /// </remarks>
    protected static string SerializeException(Exception ex)
    {
        if (ex == null) return "<exception />";

        var sb = new StringBuilder();
        sb.Append("<exception>");
        sb.AppendFormat("<source>{0}</source>", ex.Source);
        sb.AppendFormat("<message>{0}</message>", ex.Message);
        sb.AppendFormat("<stacktrace>{0}</stacktrace>", ex.StackTrace);

        sb.Append("<data>");
        foreach (DictionaryEntry item in ex.Data)
        {
            sb.AppendFormat("<{0}>{1}</{0}>", item.Key, item.Value);
        }
        sb.Append("</data>");

        if (ex.InnerException != null)
        {
            sb.Append(SerializeException(ex.InnerException));
        }

        sb.Append("</exception>");
        return sb.ToString();
    }
    #endregion

    public T First()
        => Data.First();

    public Shell()
        : base() { }

    public Shell(StateEnum State)
       : base(State)
    {
    }

    public Shell(StateEnum State, string Message)
        : base(State, Message)
    {
    }
}

/// <summary>
/// Basisklasse für Antwortobjekte, die den Zustand und die Nachricht einer Antwort kapselt.
/// Kann als Grundlage für spezifischere Antworttypen mit zusätzlichen Daten dienen.
/// </summary>
public class ResponseShell
    : Shell, IResponseShell
{
    /// <summary>
    /// Behandelt eine ResponseShell basierend auf ihrem Zustand und führt bei Erfolg eine gegebene Funktion aus.
    /// </summary>
    /// <param name="shell">Die zu verarbeitende ResponseShell-Instanz.</param>
    /// <param name="onSuccess">Die Funktion, die bei Erfolg ausgeführt wird und eine ResponseShell zurückgibt.</param>
    /// <param name="failureMessage">Optionale Nachricht für den Fall eines Fehlschlags.</param>
    /// <param name="errorMessage">Optionale Nachricht für den Fall eines Fehlers.</param>
    /// <returns>Eine neue ResponseShell-Instanz basierend auf dem Ergebnis der Verarbeitung.</returns>
    public static ResponseShell DefaultHandler(ResponseShell shell, Func<ResponseShell> OnSuccess,
        string FailureMessage = null, string ErrorMessage = null)
        => Shell.DefaultHandler(shell, OnSuccess, FailureMessage, ErrorMessage);

    public ResponseShell()
    {
    }

    public ResponseShell(StateEnum State)
        : base(State)
    {
        //this.Response = Response;
    }

    public ResponseShell(StateEnum State, string Message)
        : base(State, Message)
    {
    }
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

    public SingleDataResponseShell() { }

    /// <summary>
    /// Konstruktor zur Initialisierung der Antwort mit einem Zustand und einem zusätzlichen Datenfeld.
    /// </summary>
    /// <param name="state">Der Zustand der Antwort.</param>
    /// <param name="additionalData1">Das zusätzliche Datenfeld.</param>
    public SingleDataResponseShell(StateEnum state, string additionalData1 = "") : base(state)
    {
        AdditionalData1 = additionalData1;
    }
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

    public DoubleDataResponseShell() { }

    /// <summary>
    /// Konstruktor zur Initialisierung der Antwort mit einem Zustand und zwei zusätzlichen Datenfeldern.
    /// </summary>
    /// <param name="state">Der Zustand der Antwort.</param>
    /// <param name="additionalData1">Das erste zusätzliche Datenfeld.</param>
    /// <param name="additionalData2">Das zweite zusätzliche Datenfeld.</param>
    public DoubleDataResponseShell(StateEnum state, string additionalData1 = "", string additionalData2 = "") : base(state, additionalData1)
    {
        AdditionalData2 = additionalData2;
    }
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

    public QuadDataResponseShell() { }

    /// <summary>
    /// Konstruktor zur Initialisierung der Antwort mit einem Zustand und vier zusätzlichen Datenfeldern.
    /// </summary>
    /// <param name="state">Der Zustand der Antwort.</param>
    /// <param name="additionalData1">Das erste zusätzliche Datenfeld.</param>
    /// <param name="additionalData2">Das zweite zusätzliche Datenfeld.</param>
    /// <param name="additionalData3">Das dritte zusätzliche Datenfeld.</param>
    /// <param name="additionalData4">Das vierte zusätzliche Datenfeld.</param>
    public QuadDataResponseShell(StateEnum state, string additionalData1 = "", string additionalData2 = "", string additionalData3 = "", string additionalData4 = "") : base(state, additionalData1, additionalData2)
    {
        AdditionalData3 = additionalData3;
        AdditionalData4 = additionalData4;
    }
}
