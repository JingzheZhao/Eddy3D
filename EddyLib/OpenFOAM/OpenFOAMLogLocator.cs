using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Locates OpenFOAM log files in a case directory.
    /// </summary>
    public static class OpenFOAMLogLocator
    {
        public static IReadOnlyList<string> SimulationCandidates { get; } =
            new[] { "simpleFoam.log", "log.simpleFoam" };

        public static IReadOnlyList<string> MeshingCandidates { get; } =
            new[] { "snappyHexMesh.log", "log.snappyHexMesh" };

        public static string FindLatestSimulationLog(string caseDir) =>
            FindLatestExisting(caseDir, SimulationCandidates, IsSimulationLogName);

        public static string FindLatestMeshingLog(string meshDir) =>
            FindLatestExisting(meshDir, MeshingCandidates, IsMeshingLogName);

        private static string FindLatestExisting(
            string dir,
            IEnumerable<string> candidates,
            Func<string, bool> fallbackNameMatcher)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return null;

            string bestPath = null;
            DateTime bestWriteTime = DateTime.MinValue;

            foreach (var name in candidates ?? Enumerable.Empty<string>())
            {
                var path = Path.Combine(dir, name);
                if (!File.Exists(path))
                    continue;

                var writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime >= bestWriteTime)
                {
                    bestWriteTime = writeTime;
                    bestPath = path;
                }
            }

            if (fallbackNameMatcher == null)
                return bestPath;

            foreach (var path in Directory.EnumerateFiles(dir))
            {
                var name = Path.GetFileName(path);
                if (!fallbackNameMatcher(name))
                    continue;

                var writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime >= bestWriteTime)
                {
                    bestWriteTime = writeTime;
                    bestPath = path;
                }
            }

            return bestPath;
        }

        private static bool IsSimulationLogName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            return fileName.IndexOf("simpleFoam", StringComparison.OrdinalIgnoreCase) >= 0
                   && IsLikelyLogFileName(fileName);
        }

        private static bool IsMeshingLogName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            return fileName.IndexOf("snappyHexMesh", StringComparison.OrdinalIgnoreCase) >= 0
                   && IsLikelyLogFileName(fileName);
        }

        private static bool IsLikelyLogFileName(string fileName)
        {
            return fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
                   || fileName.StartsWith("log.", StringComparison.OrdinalIgnoreCase);
        }
    }
}
