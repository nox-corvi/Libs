using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nox.IO
{
    public static class Strings
    {
        private static string SEP = System.IO.Path.DirectorySeparatorChar.ToString();
        public static string GetDirectoryOnly(this string Filename)
        {
            if (!Filename.EndsWith(SEP))
            {
                if (!Filename.Contains(SEP))
                    return "";

                var Result = Filename;
                while (!Result.EndsWith(SEP))
                    if (Result.Length > 0)
                        Result = Result.Substring(0, Result.Length - 1);

                return Result;
            }
            else
                return Filename;
        }

        public static string DirectoryOnly(this string Filename)
        {
            if (!Filename.EndsWith(SEP))
            {
                if (!Filename.Contains(SEP))
                    return "";

                var Result = Filename;
                while (!Result.EndsWith(SEP))
                    if (Result.Length > 0)
                        Result = Result.Substring(0, Result.Length - 1);

                return Result;
            }
            else
                return Filename;
        }
        public static string FilenameOnly(this string Filename)
        {
            var Result = Filename; int i = 0;
            while ((i = Result.IndexOf(SEP)) > -1)
                Result = Result.Substring(i + 1);

            return Result;
        }
        public static string PathOnly(this string Filename)
        {
            var Result = Filename; int i = 0;
            while ((i = Result.IndexOf(SEP)) > -1)
                Result = Result.Substring(i + 1);

            return Filename.Substring(0, Filename.Length - Result.Length);
        }

        public static string ExtensionOnly(this string Filename)
        {
            var Result = FilenameOnly(Filename);
            int i = -1, j = -1;

            // find last .
            while ((j = Result.IndexOf('.', j + 1)) > i)
                i = j;

            if (i > -1) // return with .
                return Result.Substring(i);
            else
                return "";
        }

        public static string RemoveExtension(this string Filename)
        {
            string Result = Filename;
            int i = -1, j = -1;

            while ((j = Result.IndexOf('.')) > i)
                i = j;

            if (i > -1)
                return Filename.Substring(0, i);
            else
                return Filename;
        }
        public static string RemoveExtensions(this string Filename)
        {
            string Result = Filename, R2 = Filename;

            while ((R2 = RemoveExtension(Result)) != Result)
                Result = R2;

            return Result;
        }



        public static string AddPS(this string Path) =>
            Nox.Strings.AddIfMiss(Path, SEP);

        public static string FullPath(this string Path)
        {
            var Info = new DirectoryInfo(Path);
            return Info.FullName;
        }

    }
}
