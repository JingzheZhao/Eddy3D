using System;
using System.IO;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Centralized file writing utilities for OpenFOAM dictionary files.
    /// </summary>
    public static class DictFileWriter
    {
        /// <summary>
        /// Writes content to an OpenFOAM dictionary file.
        /// </summary>
        /// <param name="path">Full path to the file.</param>
        /// <param name="content">Content to write.</param>
        public static void WriteDict(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Writes a dictionary file to a directory.
        /// </summary>
        /// <param name="directory">Directory path.</param>
        /// <param name="fileName">File name.</param>
        /// <param name="content">Content to write.</param>
        public static void WriteDictToDir(string directory, string fileName, string content)
        {
            WriteDict(Path.Combine(directory, fileName), content);
        }

        /// <summary>
        /// Creates an empty .foam file for ParaView.
        /// </summary>
        /// <param name="directory">Directory path.</param>
        /// <param name="name">Base name (without extension).</param>
        public static void WriteFoamFile(string directory, string name)
        {
            WriteDict(Path.Combine(directory, name + ".foam"), string.Empty);
        }

        /// <summary>
        /// Creates an empty .foam file using an integer name.
        /// </summary>
        public static void WriteFoamFile(string directory, int name)
        {
            WriteFoamFile(directory, name.ToString());
        }

        /// <summary>
        /// Writes boundary condition files to both 0 and 0.org directories.
        /// </summary>
        /// <param name="casePaths">Case paths containing boundary directories.</param>
        /// <param name="fileName">File name for the boundary condition.</param>
        /// <param name="content">Content to write.</param>
        public static void WriteBoundaryPair(CasePaths casePaths, string fileName, string content)
        {
            WriteDictToDir(casePaths.BoundaryDir, fileName, content);
            WriteDictToDir(casePaths.BoundaryDirTemp, fileName, content);
        }

        /// <summary>
        /// Writes a batch file.
        /// </summary>
        /// <param name="directory">Directory path.</param>
        /// <param name="fileName">Batch file name.</param>
        /// <param name="content">Batch file content.</param>
        public static void WriteBatchFile(string directory, string fileName, string content)
        {
            WriteDict(Path.Combine(directory, fileName), content);
        }
    }
}
