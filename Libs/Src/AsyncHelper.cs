
using System;
using System.Threading.Tasks;

namespace Nox;


public class AsyncHelper
{
    /// <summary>
    /// Führt eine asynchrone Methode synchron aus und gibt deren Ergebnis zurück.
    /// </summary>
    /// <typeparam name="TResult">Der Typ des Ergebnisses der asynchronen Methode.</typeparam>
    /// <param name="taskFactory">Eine Funktion, die die asynchrone Task<TResult>-Methode erzeugt.</param>
    /// <returns>Das Ergebnis der asynchronen Operation.</returns>
    /// <exception cref="ArgumentNullException">Wird geworfen, wenn taskFactory null ist.</exception>
    /// <exception cref="AggregateException">Wird geworfen, wenn die asynchrone Operation mit einem Fehler endet.</exception>
    public static TResult RunSync<TResult>(Func<Task<TResult>> taskFactory)
    {
        if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));

        // Vermeiden von Deadlocks durch Ausführen der Aufgabe im Standard-TaskScheduler (nicht im aktuellen Synchronisationskontext)
        var cultureUi = System.Globalization.CultureInfo.CurrentUICulture;
        var culture = System.Globalization.CultureInfo.CurrentCulture;

        return Task.Run(async () =>
        {
            // Kulturinformationen für den Fall, dass der Task in einem anderen Thread ausgeführt wird, in dem die Kultur unterscheidlich sein könnte.
            System.Globalization.CultureInfo.CurrentUICulture = cultureUi;
            System.Globalization.CultureInfo.CurrentCulture = culture;

            return await taskFactory().ConfigureAwait(false);
        }).GetAwaiter().GetResult();
    }
}