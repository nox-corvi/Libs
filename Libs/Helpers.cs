using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Runtime;
using System.Runtime.Serialization;
using System.Xml;
using System.Security;

namespace Nox
{
    public static class Helpers
    {
        public static Exception Oops() { return new Exception("Oops"); }

        public static void Nope() { return; }

        public static int intParse(string Value, string Fieldname)
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

        public static decimal decParse(string Value, string Fieldname)
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

        public static long lngParse(string Value, string Fieldname)
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

        public static void OnXParse<T>(string Value, Action<T> Success, Action<string> Error) where T: IComparable 
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


        public static T OnXCond<T>(Func<bool> Condition, Func<T> True, Func<T> False)
        {
            if (Condition())
                return True();
            else
                return False();
        }

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
            StringBuilder s = new StringBuilder();
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

        public static string XmlEncode(string Value) => System.Web.HttpUtility.HtmlEncode(Value);

        public static string XmlDecode(string Value) => System.Web.HttpUtility.HtmlDecode(Value);

        public static string FindUniqueXmlFilename(string Path, string Prefix, string Extension)
        {
            int Counter = 0;
            string Result = String.Empty;

            do
            {
                string CounterString = Counter == 0 ? String.Empty : String.Concat('_', Counter.ToString().PadRight(3, '0'));
                string UniqueFilename = String.Concat(Path, Prefix, Helpers.UTC, CounterString,Extension);

                if (!File.Exists(UniqueFilename))
                    Result = UniqueFilename;
                else
                    Counter++;

                if (Counter > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(System.Reflection.MethodBase.GetCurrentMethod().Name + "->Counter threshold exceeded");

            } while (Result.Equals(String.Empty));

            return Result;
        }
    }
}
