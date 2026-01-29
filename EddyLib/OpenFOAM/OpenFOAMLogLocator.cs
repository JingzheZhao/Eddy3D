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
            FindLatestExisting(caseDir, SimulationCandidates);

        public static string FindLatestMeshingLog(string meshDir) =>
            FindLatestExisting(meshDir, MeshingCandidates);

        private static string FindLatestExisting(string dir, IEnumerable<string> candidates)
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

            return bestPath;
        }
    }
}
