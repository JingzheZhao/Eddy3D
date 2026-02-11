using EddyLib;
using System;
using System.IO;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    public class Test_EngineInstallStatusCache
    {
        [Fact]
        public void EngineInstallStatusCache_WriteRead_RoundTrips()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D_TestCache", Guid.NewGuid().ToString("N"));
            string cachePath = Path.Combine(tempDir, "engine_status.json");

            try
            {
                var snapshot = EngineInstallStatusCache.CreateSnapshot("Eddy3D", "Windows");
                EngineInstallStatusCache.SetEngineStatus(snapshot, "Docker", false, "Docker executable not found.");
                EngineInstallStatusCache.SetEngineStatus(snapshot, "BlueCFD", true, "blueCFD found.");

                Assert.True(EngineInstallStatusCache.TryWrite(cachePath, snapshot, out string writeError), writeError);
                Assert.True(File.Exists(cachePath));

                Assert.True(EngineInstallStatusCache.TryRead(cachePath, out var loaded, out string readError), readError);
                Assert.NotNull(loaded);

                Assert.True(EngineInstallStatusCache.TryGetEngineStatus(loaded, "Docker", out bool dockerInstalled, out string dockerDetails));
                Assert.False(dockerInstalled);
                Assert.Contains("Docker", dockerDetails, StringComparison.OrdinalIgnoreCase);

                Assert.True(EngineInstallStatusCache.TryGetEngineStatus(loaded, "BlueCFD", out bool blueCfdInstalled, out string _));
                Assert.True(blueCfdInstalled);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
