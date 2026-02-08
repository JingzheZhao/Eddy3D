using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib
{
    public static partial class Utilities
    {
        public class Directories
        {
            public static void DeleteDirectory(string path)
            {
                foreach (string directory in Directory.GetDirectories(path))
                {
                    DeleteDirectory(directory);
                }

                try
                {
                    Directory.Delete(path, true);
                }
                catch (IOException)
                {
                    Directory.Delete(path, true);
                }
                catch (UnauthorizedAccessException)
                {
                    Directory.Delete(path, true);
                }
            }

            public static void RecursiveDelete(DirectoryInfo baseDir)
            {
                if (!baseDir.Exists)
                    return;

                foreach (var dir in baseDir.EnumerateDirectories())
                {
                    RecursiveDelete(dir);
                }
                var files = baseDir.GetFiles();
                foreach (var file in files)
                {
                    file.IsReadOnly = false;
                    file.Delete();
                }
                baseDir.Delete();
            }

            public static string FixDirectories(string dir)
            {
                var sep = Path.DirectorySeparatorChar.ToString();
                if (!dir.EndsWith(sep))
                {
                    dir = dir + sep;
                }
                return dir;
            }

            public static bool IsDirectoryEmpty(string path)
            {
                return !Directory.EnumerateFileSystemEntries(path).Any();
            }

            public static List<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
            {
                if (searchOption == SearchOption.TopDirectoryOnly)
                {
                    return Directory.GetDirectories(path, searchPattern).ToList();
                }

                List<string> directories = new List<string>(GetDirectories(path, searchPattern));

                for (int i = 0; i < directories.Count; i++)
                {
                    directories.AddRange(GetDirectories(directories[i], searchPattern));
                }

                return directories;
            }

            private static List<string> GetDirectories(string path, string searchPattern)
            {
                try
                {
                    return Directory.GetDirectories(path, searchPattern).ToList();
                }
                catch (UnauthorizedAccessException)
                {
                    return new List<string>();
                }
            }

            public static string ReplaceDoubleBackslashes(string input)
            {
                string output;
                output = input.Replace(@"\\", @"\");
                output = output.Replace(@"\\", @"\");
                output = output.Replace(@"\\", @"\");
                return output;
            }

            public static string InsertDoubleBackslashes(string input)
            {
                string output;

                output = input.Replace(@"\", @"\\");
                output = output.Replace(@"\\\", @"\\");
                output = output.Replace(@"\\\\", @"\\");
                return output;
            }
        }
    }
}
