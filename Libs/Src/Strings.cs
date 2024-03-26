using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Nox
{
    public static class Strings
    {
        /// <summary>
        /// Methode die einen String auf eine definierte Länge beschränkt
        /// </summary>
        /// <param name="source">der Text</param>
        /// <param name="maxLength">The maximum limit of the string to return.</param>
        public static string LimitLength(this string source, int maxLength)
        {
            if (source != null)
            {
                if (source.Length <= maxLength)
                    return source;
                else
                    return source.Substring(0, maxLength);
            }
            else
                return string.Empty;
        }

        public static string CenterText(this string source, int Length)
        {
            int l_ges = source.Length;
            int l1 = (Length - source.Length) / 2;
            int l2 = (8 - Length - l1);

            return (new string(' ', l1) + source + new string(' ', Length)).Substring(0, Length);
        }

        public static string RemoveQMarks(this string source)
        {
            var Result = source;

            if (Result.StartsWith("\""))
                Result = Result.Substring(1, Result.Length - 1);

            if (Result.EndsWith("\""))
                Result = Result.Substring(0, Result.Length - 1);

            return Result;
        }

        /// <summary>
        /// Method that forces a default-Value instead of null
        /// </summary>
        /// <param name="_default">der Text</param>
        /// <returns>argument or (if null) a default value</returns>
        public static string NZ(this string Arg, string _default = "")
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
        /// Method that returns a string afore a first matching search-argument
        /// </summary>
        /// <param name="source"></param>
        /// <param name="match">der Text</param>
        /// <param name="_default"></param>
        /// <returns></returns>
        public static string Before(this string source, string match, string _default = "")
        {
            int Position = source.IndexOf(match);
            if (Position < 0)
                return _default;
            else
                return source.Substring(0, Position);
        }

        /// <summary>
        /// Method that returns a string past a first matching search-argument
        /// </summary>
        /// <param name="source">der Text</param>
        /// <param name="match"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        public static string After(this string source, string match, string _default = "")
        {
            int Position = source.IndexOf(match);
            if (Position < 0)
                return _default;
            else
                return source.Substring(Position + match.Length);
        }

        /// <summary>
        /// appends a string if source does not end with it
        /// </summary>
        /// <param name="source"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        public static string AddIfMiss(this string source, string add)
        {
            if (source.EndsWith(add))
                return source;
            else
                return source + add;
        }

        /// <summary>
        /// appends a string if source does not end with it
        /// </summary>
        /// <param name="source"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        public static string AddIfMiss(this string source, char add) =>
            source.AddIfMiss(add.ToString());


        // http://www.blackbeltcoder.com/Articles/net/implementing-vbs-like-operator-in-c
        public static bool IsLike(this string s, string pattern)
        {
            // Characters matched so far
            int matched = 0;

            // Loop through pattern string
            for (int i = 0; i < pattern.Length;)
            {
                // Check for end of string
                if (matched > s.Length)
                    return false;

                // Get next pattern character
                char c = pattern[i++];
                if (c == '[') // Character list
                {
                    // Test for exclude character
                    bool exclude = (i < pattern.Length && pattern[i] == '!');
                    if (exclude)
                        i++;
                    // Build character list
                    int j = pattern.IndexOf(']', i);
                    if (j < 0)
                        j = s.Length;
                    HashSet<char> charList = CharListToSet(pattern.Substring(i, j - i));
                    i = j + 1;

                    if (charList.Contains(s[matched]) == exclude)
                        return false;
                    matched++;
                }
                else if (c == '?') // Any single character
                {
                    matched++;
                }
                else if (c == '#') // Any single digit
                {
                    if (!Char.IsDigit(s[matched]))
                        return false;
                    matched++;
                }
                else if (c == '%') // Zero or more characters
                {
                    if (i < pattern.Length)
                    {
                        // Matches all characters until
                        // next character in pattern
                        char next = pattern[i];
                        int j = s.IndexOf(next, matched);
                        if (j < 0)
                            return false;
                        matched = j;
                    }
                    else
                    {
                        // Matches all remaining characters
                        matched = s.Length;
                        break;
                    }
                }
                else // Exact character
                {
                    if (matched >= s.Length || c != s[matched])
                        return false;
                    matched++;
                }
            }
            // Return true if all characters matched
            return (matched == s.Length);
        }


        //TODO: untested
        public static string ProperCase(this string s)
        {
            int i = 0;

            // find first non blank
            while (i < s.Length)
                if (!char.IsLetterOrDigit(s[i]))
                    i++;
                else
                    break;

            return s.Substring(0, i - 1) + s.Substring(i, 1).ToUpper() + s.Substring(i + 1);
        }

        /// <summary>
        /// Converts a string of characters to a HashSet of characters. If the string
        /// contains character ranges, such as A-Z, all characters in the range are
        /// also added to the returned set of characters.
        /// </summary>
        /// <param name="charList">Character list string</param>
        private static HashSet<char> CharListToSet(string charList)
        {
            HashSet<char> set = new HashSet<char>();

            for (int i = 0; i < charList.Length; i++)
            {
                if ((i + 1) < charList.Length && charList[i + 1] == '-')
                {
                    // Character range
                    char startChar = charList[i++];
                    i++; // Hyphen
                    char endChar = (char)0;
                    if (i < charList.Length)
                        endChar = charList[i++];
                    for (int j = startChar; j <= endChar; j++)
                        set.Add((char)j);
                }
                else set.Add(charList[i]);
            }
            return set;
        }

        [Obsolete]
        public static string RandomChars(int Length, bool Hex)
        {
            string HEX = "0123456789ABCDEF";
            var r = new Random(DateTime.Now.Millisecond);

            var Result = new StringBuilder();
            for (int i = 0; i < Length; i++)
                if (Hex)
                    Result.Append(HEX.Substring(r.Next(HEX.Length - 1)));
                else
                    Result.Append((char)r.Next(32, 127));

            return Result.ToString();
        }

        public static string RandomChars(int Length)
        {
            var r = new Random(DateTime.Now.Millisecond);

            var Result = new StringBuilder();
            for (int i = 0; i < Length; i++)
                Result.Append((char)r.Next(32, 127));

            return Result.ToString();
        }

        public static string RandomHex(int Length)
        {
            string HEX = "0123456789ABCDEF";
            var r = new Random(DateTime.Now.Millisecond);

            var Result = new StringBuilder();
            for (int i = 0; i < Length; i++)
                Result.Append(HEX.Substring(r.Next(0, HEX.Length - 1), 1));

            return Result.ToString();
        }

        public static string SQLJoin(string[] args, string seperator = ", ")
        {
            //return String.Join(",".ToString(), ValidationResult.Select(x => x.Trim(',')))));
            var Result = new StringBuilder();

            for (int i = 0; i < args.Length; i++)
            {
                if (Result.Length > 0)
                    Result.Append(seperator);
                Result.Append(args[i]);
            }

            return Result.ToString();
        }

        public static string ToBase64(this string s, Encoding e) =>
            Convert.ToBase64String(e.GetBytes(s));

        public static string FromBase64(this string s, Encoding e) =>
            e.GetString(Convert.FromBase64String(s));
    }
}
