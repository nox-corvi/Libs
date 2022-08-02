using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Nox.IO
{
    public class IO
    {
        /// <summary>
		/// Sucht rekursiv in einem Verzeichnis nach Dateien und gibt das Ergebnis zurück.
		/// </summary>
		/// <param name="Path">Der Pfad in dem gesucht werden soll</param>
		/// <param name="Filter">Der Filter der auf die Suche angewendet werden soll</param>
		/// <returns>Das Ergebnis als string[]</returns>
		public static string[] SearchFiles(string Path, string Filter)
        {
            var SearchStack = new Stack<DirectoryInfo>();
            var Result = new List<string>();

            var Root = new System.IO.DirectoryInfo(Path);
            var RootPath = Root.FullName;

            int ErrCount = 0;

            SearchStack.Push(Root);
            while (SearchStack.Count() > 0)
            {
                try
                {
                    var Current = SearchStack.Pop();

                    var Folders = Current.GetDirectories();
                    int FolderCount = Folders.Length;
                    for (int i = 0; i < FolderCount; i++)
                    {
                        try
                        {
                            SearchStack.Push(Folders[i]);
                        }
                        catch (IOException)
                        {
                            // ignore
                            ErrCount++;
                        }
                    }

                    var Files = Current.GetFiles(Filter, SearchOption.TopDirectoryOnly);
                    int FileCount = Files.Length;
                    for (int i = 0; i < FileCount; i++)
                    {
                        try
                        {
                            Result.Add(Files[i].FullName.Substring(RootPath.Length + 1));
                        }
                        catch (IOException)
                        {
                            // ignore
                            ErrCount++;
                        }
                    }
                }
                catch (IOException)
                {
                    //IDXLogs.WriteException(IOe);
                    ErrCount++;
                }
            }
            if (ErrCount > 0)
                throw new Exception("some idx-archives could not read.");

            return Result.ToArray();
        }

        public static FileStream CreateTempFile(string ArchiveFilename)
        {
            return File.Open(GetDirectoryOnly(ArchiveFilename) + Guid.NewGuid().ToString().Replace("-", ""), FileMode.Create);
        }

        public static string GetDirectoryOnly(string Filename)
        {
            if (!Filename.EndsWith("\\"))
            {
                if (!Filename.Contains("\\"))
                    return "";

                var Result = Filename;
                while (!Result.EndsWith("\\"))
                    if (Result.Length > 0)
                        Result = Result.Substring(0, Result.Length - 1);

                return Result;
            }
            else
                return Filename;
        }

        public static string DirectoryOnly(string Filename)
        {
            if (!Filename.EndsWith("\\"))
            {
                if (!Filename.Contains("\\"))
                    return "";

                var Result = Filename;
                while (!Result.EndsWith("\\"))
                    if (Result.Length > 0)
                        Result = Result.Substring(0, Result.Length - 1);

                return Result;
            }
            else
                return Filename;
        }
        public static string FilenameOnly(string Filename)
        {
            var Result = Filename; int i = 0;
            while ((i = Result.IndexOf("\\")) > -1)
                Result = Result.Substring(i + 1);

            return Result;
        }
        public static string PathOnly(string Filename)
        {
            var Result = Filename; int i = 0;
            while ((i = Result.IndexOf("\\")) > -1)
                Result = Result.Substring(i + 1);

            return Filename.Substring(0, Filename.Length - Result.Length);
        }

        public static string ExtensionOnly(string Filename)
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

        public static string RemoveExtension(string Filename)
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
        public static string RemoveExtensions(string Filename)
        {
            string Result = Filename, R2 = Filename;

            while ((R2 = RemoveExtension(Result)) != Result)
                Result = R2;

            return Result;
        }

        public static string AddBS(string Path)
        {
            return string.Concat(Path, Path.EndsWith("\\") ? ' ' : '\\');
        }

        public static string FullPath(string Path)
        {
            var Info = new DirectoryInfo(Path);
            return Info.FullName;
        }

    }
}
