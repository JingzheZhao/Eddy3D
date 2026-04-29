using EddyLib.OpenFOAM;
using System;
using System.IO;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "OpenFOAM")]
    public class Test_OpenFOAMLogLocator
    {
        [Fact]
        public void FindLatestSimulationLog_ReturnsNewestMatchingLogAcrossCandidatesAndFallbacks()
        {
            var tempDir = CreateTempDir();
            try
            {
                var candidate = Path.Combine(tempDir, "foamRun.log");
                var fallback = Path.Combine(tempDir, "foamRun.custom.log");
                File.WriteAllText(candidate, "candidate");
                File.WriteAllText(fallback, "fallback");

                File.SetLastWriteTimeUtc(candidate, DateTime.UtcNow.AddMinutes(-2));
                File.SetLastWriteTimeUtc(fallback, DateTime.UtcNow.AddMinutes(-1));

                var latest = OpenFOAMLogLocator.FindLatestSimulationLog(tempDir);
                Assert.Equal(fallback, latest);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void FindLatestMeshingLog_FindsSnappyHexMeshFallbackLog()
        {
            var tempDir = CreateTempDir();
            try
            {
                var fallback = Path.Combine(tempDir, "snappyHexMesh-run-2026.log");
                File.WriteAllText(fallback, "mesh");

                var latest = OpenFOAMLogLocator.FindLatestMeshingLog(tempDir);
                Assert.Equal(fallback, latest);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void FindLatestSimulationLog_IgnoresNonLogFiles()
        {
            var tempDir = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "foamRun.command"), "not a log");
                File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "not a log");

                var latest = OpenFOAMLogLocator.FindLatestSimulationLog(tempDir);
                Assert.Null(latest);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        private static string CreateTempDir()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "Eddy3D-Tests",
                "OpenFOAMLogLocator",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
