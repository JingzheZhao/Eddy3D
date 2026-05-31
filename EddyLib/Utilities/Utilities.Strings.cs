using System;
using System.IO;
using System.Linq;

namespace EddyLib
{
    public static partial class Utilities
    {
        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static bool HasWhiteSpace(string input)
        {
            bool hasWhiteSpace = false;

            foreach (char ch in input)
            {
                if (Char.IsWhiteSpace(ch))
                {
                    hasWhiteSpace = true;
                }
            }
            return hasWhiteSpace;
        }

        public static bool IsValidProbeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            foreach (char ch in input)
            {
                if (!Char.IsLetterOrDigit(ch) && ch != '_' && ch != '-')
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Validates that a path is a safe relative path.
        /// </summary>
        public static void ValidateRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            ValidatePathForShell(path);

            if (Path.IsPathRooted(path) || path.Contains(".."))
            {
                throw new ArgumentException("Path must be relative and not contain '..': " + path);
            }
        }

        /// <summary>
        /// Validates that a path does not contain characters that could be used for shell injection.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the path contains shell metacharacters.</exception>
        public static void ValidatePathForShell(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            // Shell metacharacters that are dangerous on Windows and Unix-like systems.
            // On Windows, backslash is a path separator, but on Unix it is a metacharacter.
            char[] metachars;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                metachars = new[] { '&', '|', ';', '$', '`', '<', '>', '(', ')', '[', ']', '{', '}', '*', '?', '!', '\n', '\r', '\t', '\0', '%', '^', '\'', '"' };
            }
            else
            {
                metachars = new[] { '&', '|', ';', '$', '`', '<', '>', '(', ')', '[', ']', '{', '}', '*', '?', '!', '\n', '\r', '\t', '\0', '%', '^', '\\', '\'', '"' };
            }

            if (path.IndexOfAny(metachars) != -1)
            {
                throw new ArgumentException("Path contains invalid shell metacharacters: " + path);
            }
        }
    }
}
