using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    public class DatasetCuratorTests
    {
        private const double VGreat = -1.7976931e+307; // OpenFOAM sentinel for inside-wall probes

        // Regression: VGREAT in v or w was not caught — sqrt overflowed to inf.
        [RequiresPythonFact]
        public void AddMagU_VGreatSentinelInAnyComponent_NeverProducesInf()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-datacurator-vgreat");
            try
            {
                SetupAndRun(root, "TestCase", "0", new[]
                {
                    (VGreat, 1.0,   1.0),    // VGREAT in u
                    (1.0,   VGreat, 1.0),    // VGREAT in v — previously slipped through
                    (1.0,   1.0,   VGreat),  // VGREAT in w — previously slipped through
                    (VGreat, VGreat, VGreat) // all VGREAT
                });

                foreach (var magU in ReadMagU(root, "TestCase", "0"))
                {
                    Assert.False(
                        magU.IndexOf("inf", StringComparison.OrdinalIgnoreCase) >= 0,
                        $"mag_U must not be inf; got: '{magU}'");
                }
            }
            finally { TestFixtures.CleanupTestDirectory(root); }
        }

        // Regression: leading time token (e.g. "600") was consumed as the first velocity
        // component, misaligning all probes and producing huge/inf magnitudes.
        [RequiresPythonFact]
        public void AddMagU_ValidVelocity_ProbesAlignedAndMagnitudeCorrect()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-datacurator-alignment");
            try
            {
                SetupAndRun(root, "TestCase", "0", new[]
                {
                    (3.0, 4.0, 0.0),  // mag = sqrt(9+16)/5 = 1.0
                    (0.0, 0.0, 5.0),  // mag = sqrt(25)/5   = 1.0
                });

                var magnitudes = ReadMagU(root, "TestCase", "0")
                    .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                    .ToArray();

                Assert.Equal(2, magnitudes.Length);
                Assert.Equal(1.0, magnitudes[0], 6);
                Assert.Equal(1.0, magnitudes[1], 6);
            }
            finally { TestFixtures.CleanupTestDirectory(root); }
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static void SetupAndRun(string root, string caseName, string direction, (double u, double v, double w)[] probes)
        {
            // Write Python script from repo
            var scriptDir = Path.Combine(root, "Scripts");
            Directory.CreateDirectory(scriptDir);
            File.Copy(FindScript(), Path.Combine(scriptDir, "add_mag_u.py"));

            // Minimal CSV — SDF=50 (outside building, valid sensor point)
            var datasetDir = Path.Combine(root, "Dataset");
            Directory.CreateDirectory(datasetDir);
            var csv = new StringBuilder("X,Y,Z_relative,SDF,Bldg_height,U_over_Uref,dir_sin,dir_cos\n");
            for (int i = 0; i < probes.Length; i++)
                csv.AppendLine(FormattableString.Invariant($"{i},0,1.8,50.0,0,0.5,0,-1"));
            File.WriteAllText(Path.Combine(datasetDir, $"{caseName}_{direction}.csv"), csv.ToString(), new UTF8Encoding(false));

            // OpenFOAM probes U/k files: "time (u v w) (u v w) ..." and "time k0 k1 ..."
            var uDir = Path.Combine(root, direction, "postProcessing", "ttt", "600");
            Directory.CreateDirectory(uDir);
            var uLine = new StringBuilder("600");
            var kLine = new StringBuilder("600");
            foreach (var (u, v, w) in probes)
            {
                uLine.AppendFormat(CultureInfo.InvariantCulture, " ({0} {1} {2})", u, v, w);
                kLine.Append(" 0");
            }
            File.WriteAllLines(Path.Combine(uDir, "U"),
                new[] { "# Time\t0", uLine.ToString() },
                new UTF8Encoding(false));
            File.WriteAllLines(Path.Combine(uDir, "k"),
                new[] { "# Time\t0", kLine.ToString() },
                new UTF8Encoding(false));

            // Run script
            var psi = TestExecutionPolicy.CreatePythonProcessStartInfo(root);
            psi.ArgumentList.Add(Path.Combine(root, "Scripts", "add_mag_u.py"));
            psi.ArgumentList.Add(caseName);
            psi.ArgumentList.Add(root);

            using (var proc = Process.Start(psi))
            {
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                Assert.True(proc.ExitCode == 0,
                    $"Python script failed.\nstdout: {stdout}\nstderr: {stderr}");
            }
        }

        private static string[] ReadMagU(string root, string caseName, string direction)
        {
            var lines = File.ReadAllLines(Path.Combine(root, "Dataset", $"{caseName}_{direction}.csv"));
            var header = lines[0].Split(',');
            int magIdx = Array.IndexOf(header, "mag_U");
            Assert.True(magIdx >= 0, "mag_U column missing from output CSV.");
            return lines.Skip(1).Select(l => l.Split(',')[magIdx]).ToArray();
        }

        private static string FindScript()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && dir.Name != TestConstants.SolutionFolderName)
                dir = dir.Parent;
            if (dir == null)
                throw new DirectoryNotFoundException($"Could not find '{TestConstants.SolutionFolderName}' walking up from test output directory.");
            var path = Path.Combine(dir.FullName, "Scripts", "add_mag_u.py");
            if (!File.Exists(path))
                throw new FileNotFoundException($"add_mag_u.py not found at: {path}");
            return path;
        }
    }
}
