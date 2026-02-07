using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace EddyLib
{
    public static partial class Utilities
    {
        public static string EnsureTrailingBackslash(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            var sep = Path.DirectorySeparatorChar.ToString();
            return path.EndsWith(sep) ? path : path + sep;
        }

        public static void CleanDirectory(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }

        public static string GetFileNameWithHighestEnumerator(string folder)
        {
            var path = Directory.GetFiles(folder, "*.dat").Select(fn => new FileInfo(fn)).OrderBy(f => f.Name).Last();
            return path.ToString();
        }

        public static List<string> FileReader(string filePath)
        {
            string line;
            List<string> lines = new List<string>();

            if (File.Exists(filePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new System.ArgumentException(e.Message);
                }
            }
            return lines;
        }

        public static void DownLoadFile(string URL, string FilePath)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFile(URL, FilePath);
        }
    }
}
