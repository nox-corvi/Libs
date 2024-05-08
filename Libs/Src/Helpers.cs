using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Xml.Serialization;

namespace Nox
{
    public static class Helpers
    {
        public static Exception Oops() { return new Exception("Oops"); }

        public static void Nope() { return; }

        public static int ParseInt(string Value, string Fieldname)
        {
            try
            {
                return int.Parse(Value);
            }
            catch (Exception e)
            {
                throw new ArgumentException("error parse " + Fieldname + " with Value: " + Value, e);
            }
        }

        public static decimal ParseDecimal(string Value, string Fieldname)
        {
            try
            {
                return (decimal)TypeDescriptor.GetConverter(typeof(decimal)).ConvertFromString(Value);
            }
            catch (Exception e)
            {
                throw new ArgumentException("error parse " + Fieldname + " with Value: " + Value, e);
            }
        }

        public static long ParseLong(string Value, string Fieldname)
        {
            try
            {
                return long.Parse(Value);
            }
            catch (Exception e)
            {
                throw new ArgumentException("error parse " + Fieldname + " with Value: " + Value, e);
            }
        }

        public static T XParse<T>(string Value, string Fieldname)
        {
            try
            {
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(Value);
            }
            catch (NotSupportedException e)
            {
                throw new ArgumentException("error parse " + Fieldname + " with value: " + Value + " to " + typeof(T).ToString(), e);
            }
        }

        public static void OnXParse<T>(string Value, Action<T> Success, Action<string> Error) where T : IComparable
        {
            T Result;

            try
            {
                Result = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(Value);
            }
            catch (Exception)
            {
                Error?.Invoke(Value);
                return;
            }

            Success?.Invoke(Result);
        }

        public static T OnXTry<T>(Func<T> exec, Func<Exception, T> exception)// where T : struct
        {
            try
            {
                return exec.Invoke();
            }
            catch (Exception e)
            {
                return exception.Invoke(e) ?? default;
            }
        }

        public static T OnXCond<T>(Func<bool> Condition, Func<T> True, Func<T> False)
        {
            if (Condition())
                return True();
            else
                return False();
        }

        public static void OnXCond(bool Condition, Action True, Action False = null)
        {
            if (Condition)
                True?.Invoke();
            else
                False?.Invoke();
        }

        public static void OnXCond(Func<bool> Condition, Action True, Action False = null)
            => OnXCond(Condition(), True, False);

        public static bool OnX(Action f)
        {
            try
            {
                f.Invoke();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool[] OnX(params Action[] fs)
        {
            var Result = new bool[fs.Length];
            for (int i = 0; i < fs.Length; i++)
                Result[i] = OnX(f: fs[i]);

            return Result;
        }

        public static T OnXFunc<T>(Func<T> f) => f.Invoke();


        public static int OnXExec(params Action[] Methods)
        {
            int Result = 0;
            for (int i = 0; i < Methods.Length; i++)
                try
                {
                    Methods[i].Invoke();
                    Result++;
                }
                catch
                {
                    //
                }

            return Result;
        }

        public static T OnResult<T>(Action f, T Result)
        {
            f.Invoke();
            return Result;
        }

        ///// <summary>
        ///// Ersetzt einen Wert bei Übereinstimmung mit einem Ersatzwert
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="Value"></param>
        ///// <param name="Match"></param>
        ///// <param name="ReplaceValue"></param>
        ///// <returns></returns>
        //public static T OnXReplace<T>(T Value, List<T> Match, T ReplaceValue)  where T : class
        //{
        //    foreach (var Item in Match)
        //        if (Value.Equals(Item))
        //            return ReplaceValue;

        //    return Value;
        //}

        //public static T OnXReplace<T>(T Value, T Match, T ReplaceValue) where T : class
        //{
        //    if (Value.Equals(Match))
        //        return ReplaceValue;
        //    else
        //    return Value;
        //}

        public static T N<T>(T Arg, T Default = default) where T : IComparable
        {
            if (Arg != null)
                if (Arg.CompareTo(default(T)) == 0)
                    return Default;
                else
                    return Arg;
            else
                return Default;
        }

        public static T To<T>(string value) where T : struct//, System.IFormattable
        {
            if (typeof(T) == typeof(double))
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value);
            else
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(value);
        }

        /// <summary>
        /// Method that forces a default-Value instead of null
        /// </summary>
        /// <param name="_default">der Text</param>
        /// <returns>argument or (if null) a default value</returns>
        public static string NZ(string Arg, string _default = "")
        {
            if (Arg != null)
                if (Arg == string.Empty)
                    return _default;
                else
                    return Arg;
            else
                return _default;
        }

        /// <summary>
        /// Method that forces a default-Value instead of null
        /// </summary>
        /// <param name="_default">der Text</param>
        /// <returns>argument or (if null) a default value</returns>
        public static string NZ(object Arg, string _default = "")
        {
            if (Arg != null)
                if (Arg.ToString() == string.Empty)
                    return _default;
                else
                    return Arg.ToString();
            else
                return _default;
        }

        public static T N<T>(object Arg, T Default = default)
        {
            if (Arg != null)
                if (!Convert.IsDBNull(Arg))
                {
                    /* error if try to convert double to float, use invariant cast from String!!!
                     * http://stackoverflow.com/questions/1667169/why-do-i-get-invalidcastexception-when-casting-a-double-to-decimal
                     */
                    if (typeof(T) == typeof(double))
                        return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(Arg.ToString());
                    else
                        return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(Arg.ToString());
                }
                else
                    return Default;
            else
                return Default;
        }

        public static byte GetHex(char x)
        {
            if ((x <= '9') && (x >= '0'))
                return (byte)(x - '0');
            else if ((x <= 'z') && (x >= 'a'))
                return (byte)(x - 'a' + 10);
            else if ((x <= 'Z') && (x >= 'A'))
                return (byte)(x - 'A' + 10);
            else
                return 0;
        }

        public static string EnHEX(byte[] bytes)
        {
            StringBuilder s = new();
            foreach (byte b in bytes)
                s.Append(b.ToString("x2"));
            return s.ToString();
        }

        public static byte[] DeHEX(string hex)
        {
            byte[] r = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length - 1; i += 2)
            {
                byte a = GetHex(hex[i]);
                byte b = GetHex(hex[i + 1]);
                r[i / 2] = (byte)(a << 4 + b);
            }
            return r;
        }

        public static string UTC => System.DateTime.UtcNow.ToString("#yyyymmddHHMMss");

        public static string XmlEncode(string Value) => HttpUtility.HtmlEncode(Value);
        public static string XmlDecode(string Value) => HttpUtility.HtmlDecode(Value);

        public static string FindUniqueXmlFilename(string Path, string Prefix, string Extension)
        {
            int Counter = 0;
            string Result = String.Empty;

            do
            {
                string CounterString = Counter == 0 ? String.Empty : String.Concat('_', Counter.ToString().PadRight(3, '0'));
                string UniqueFilename = String.Concat(Path, Prefix, Helpers.UTC, CounterString, Extension);

                if (!File.Exists(UniqueFilename))
                    Result = UniqueFilename;
                else
                    Counter++;

                if (Counter > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(System.Reflection.MethodBase.GetCurrentMethod().Name + "->Counter threshold exceeded");

            } while (Result.Equals(String.Empty));

            return Result;
        }

        public static Guid ExtractGuid(byte[] data, int Offset)
        {
            byte[] buffer1 = new byte[16];
            Array.Copy(data, Offset, buffer1, 0, 16);

            return new Guid(buffer1);
        }

        public static byte[] ExtractArrayWithLength(byte[] data, int Offset, int length, out int read)
        {
            var l = length;

            //TODO:Verify
            if (Offset + l > length)
                l -= (Offset + l) - l + 1;

            byte[] buffer1 = new byte[l];
            Array.Copy(data, Offset, buffer1, 0, l);

            read = l;
            return buffer1;
        }

        public static byte[] ExtractArray(byte[] data, int Offset, out int read) =>
            ExtractArray(data, Offset, data.Length, out read);

        public static byte[] ExtractArray(byte[] data, int Offset, int length, out int read)
        {
            var l = length;

            //TODO:Verify
            if (Offset + l > length)
                l -= length - (Offset + l);

            byte[] buffer1 = new byte[l];
            Array.Copy(data, Offset, buffer1, 0, l);

            read = l;
            return buffer1;
        }

        public static byte[] ExtractArrayWithLength(byte[] data, int Offset, out int read)
        {
            int l = 0, i = Offset;
            l = BitConverter.ToInt32(data, i); i += sizeof(int);

            byte[] k = new byte[l];
            Array.Copy(data, i, k, 0, l);
            i += l;

            read = l + sizeof(int);
            return k;
        }

        public static string SerializeToXml<T>(T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                var ser = new XmlSerializer(typeof(T));
                ser.Serialize(memoryStream, obj);

                memoryStream.Position = 0;
                using (var r = new StreamReader(memoryStream))
                    return r.ReadToEnd();
            }
        }

        public static T DeserializeFromXml<T>(string xml)
        {
            T result;
            var ser = new XmlSerializer(typeof(T));
            using (var tr = new StringReader(xml))
            {
                result = (T)ser.Deserialize(tr);
            }
            return result;
        }

        public static string RandomString(int length)
        {
            var sb = new StringBuilder();
            var rnd = new Random((int)(DateTime.Now.Ticks & 0xFFFF));

            while (sb.Length < length)
                sb.Append(Helpers.EnHEX(new byte[] { (byte)rnd.Next(0xFF) }));

            return sb.ToString(0, length);
        }


        public static T GetDefaultValue<T>()
        {
            // We want an Func<T> which returns the default.
            // Create that expression here.
            Expression<Func<T>> e = Expression.Lambda<Func<T>>(
                // The default value, always get what the *code* tells us.
                Expression.Default(typeof(T))
            );

            // Compile and return the value.
            return e.Compile()();
        }

        public static object GetDefaultValue(this Type type)
        {
            // Validate parameters.
            if (type == null) throw new ArgumentNullException("type");

            // We want an Func<object> which returns the default.
            // Create that expression here.
            Expression<Func<object>> e = Expression.Lambda<Func<object>>(
                // Have to convert to object.
                Expression.Convert(
                    // The default value, always get what the *code* tells us.
                    Expression.Default(type), typeof(object)
                )
            );

            // Compile and return the value.
            return e.Compile().Invoke();
        }


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
        public static string SerializeException(Exception ex)
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

        public static IEnumerable<KeyValuePair<TKey, TValue>> ToEnumerable<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
        {
            // Erstelle eine Liste mit dem einzelnen KeyValuePair
            List<KeyValuePair<TKey, TValue>> list = new List<KeyValuePair<TKey, TValue>>();
            list.Add(pair);

            // Gib die Liste als IEnumerable zurück
            return list;
        }
    }
}
