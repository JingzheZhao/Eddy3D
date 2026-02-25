using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib.Helpers
{
    /// <summary>
    /// Utilities for directory and file system operations.
    /// </summary>
    public static class DirectoryHelpers
    {
        /// <summary>
        /// Recursively deletes a directory and all its contents.
        /// </summary>
        /// <param name="path">Path to the directory.</param>
        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                // Retry on IO exception
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                // Retry on access exception
                Directory.Delete(path, true);
            }
        }

        /// <summary>
        /// Recursively deletes a directory, clearing read-only attributes.
        /// </summary>
        /// <param name="baseDir">Directory to delete.</param>
        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
            {
                return;
            }

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }

            foreach (var file in baseDir.GetFiles())
            {
                file.IsReadOnly = false;
                file.Delete();
            }

            baseDir.Delete();
        }

        /// <summary>
        /// Ensures a path ends with a trailing directory separator.
        /// </summary>
        public static string EnsureTrailingBackslash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            string trimmed = path.Trim();
            if (trimmed.EndsWith("/") || trimmed.EndsWith("\\"))
            {
                return trimmed;
            }

            return trimmed + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Reformats a Windows path for use in Unix-style contexts (e.g., WSL, Docker).
        /// </summary>
        public static string ToUnixPath(string windowsPath)
        {
            var output = windowsPath.Replace(@"\", @"/");
            output = output.Replace(@":", @"/");
            output = "//" + output;
            output = output.Replace(@"//C//", @"//c//");
            return output;
        }

        /// <summary>
        /// Checks if a directory is empty.
        /// </summary>
        public static bool IsEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// Gets all directories matching a pattern.
        /// </summary>
        public static List<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return Directory.GetDirectories(path, searchPattern).ToList();
            }

            var directories = new List<string>(GetDirectoriesSafe(path, searchPattern));

            for (int i = 0; i < directories.Count; i++)
            {
                directories.AddRange(GetDirectoriesSafe(directories[i], searchPattern));
            }

            return directories;
        }

        private static List<string> GetDirectoriesSafe(string path, string searchPattern)
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

        /// <summary>
        /// Replaces double backslashes with single backslashes.
        /// </summary>
        public static string NormalizeBackslashes(string input)
        {
            var output = input.Replace(@"\\", @"\");
            output = output.Replace(@"\\", @"\");
            output = output.Replace(@"\\", @"\");
            return output;
        }

        /// <summary>
        /// Doubles backslashes for escaping.
        /// </summary>
        public static string EscapeBackslashes(string input)
        {
            var output = input.Replace(@"\", @"\\");
            output = output.Replace(@"\\\", @"\\");
            output = output.Replace(@"\\\\", @"\\");
            return output;
        }

        /// <summary>
        /// Cleans all files and subdirectories from a directory.
        /// </summary>
        public static void CleanDirectory(string path)
        {
            var di = new DirectoryInfo(path);

            foreach (var file in di.EnumerateFiles())
            {
                file.Delete();
            }

            foreach (var dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }
    }
}
