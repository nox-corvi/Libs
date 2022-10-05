using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return File.Open(ArchiveFilename.GetDirectoryOnly() + Guid.NewGuid().ToString().Replace("-", ""), FileMode.Create);
        }

    }
}
