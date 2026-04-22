using EddyLib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class ProbingTests
    {
        [RequiresGrasshopperFact]
        public void Probing_Scalar_ParsesAndWritesBinary()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-scalar");
            try
            {
                const int iter = 100;
                var probeName = "probe-scalar";
                var fieldName = "p";

                Directory.CreateDirectory(Path.Combine(root, iter.ToString()));
                WriteProbeFile(root, probeName, iter, fieldName, "0 1.2 3.4\n100 5.6 7.8\n");

                var points = new List<Point3d> { new Point3d(0, 0, 0), new Point3d(1, 0, 0) };
                var ofField = new OFField(fieldName, probeName, 1);
                var res = new OFResult(null, new OFRunSettings(endTime: iter), null, root);

                var probing = new Probing(points, root, root, ofField, res, rerun: true, currWindDir: "0");

                Assert.Equal(5.6, probing.ResultScalar[0].Value, 3);
                Assert.Equal(7.8, probing.ResultScalar[1].Value, 3);

                var binPath = Path.Combine(root, "postProcessing", "0_probe-scalar_p.bin");
                Assert.True(File.Exists(binPath), "Expected scalar probe binary file to be written.");

                var loaded = RadianceFiles.loadBinScalars(binPath);
                Assert.Equal(5.6, loaded[0], 3);
                Assert.Equal(7.8, loaded[1], 3);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [RequiresGrasshopperFact]
        public void ProbingNew_Vector_ParsesAndWritesBinary()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-vector");
            try
            {
                const int iter = 100;
                var probeName = "probe-vector";
                var fieldName = "U";

                Directory.CreateDirectory(Path.Combine(root, iter.ToString()));
                WriteProbeFile(root, probeName, iter, fieldName, "0 (1 2 3) (4 5 6)\n100 (7 8 9) (10 11 12)\n");

                var points = new List<Point3d> { new Point3d(0, 0, 0), new Point3d(1, 0, 0) };
                var ofField = new OFFieldNew(probeName, field.U, 1);
                var res = new OFResult(null, new OFRunSettings(endTime: iter), null, root);

                var probing = new ProbingNew(points, root, root, ofField, 0, res);

                Assert.Equal(7, probing.ResultVec[0].Value.X, 3);
                Assert.Equal(8, probing.ResultVec[0].Value.Y, 3);
                Assert.Equal(9, probing.ResultVec[0].Value.Z, 3);
                Assert.Equal(10, probing.ResultVec[1].Value.X, 3);
                Assert.Equal(11, probing.ResultVec[1].Value.Y, 3);
                Assert.Equal(12, probing.ResultVec[1].Value.Z, 3);

                var binPath = Path.Combine(root, "postProcessing", "0_probe-vector_U.bin");
                Assert.True(File.Exists(binPath), "Expected vector probe binary file to be written.");

                var loaded = RadianceFiles.loadBinVectors(binPath);
                Assert.Equal(7, loaded[0].X, 3);
                Assert.Equal(8, loaded[0].Y, 3);
                Assert.Equal(9, loaded[0].Z, 3);
                Assert.Equal(10, loaded[1].X, 3);
                Assert.Equal(11, loaded[1].Y, 3);
                Assert.Equal(12, loaded[1].Z, 3);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [NotWindowsServerFact]
        public void Probing_CommandLineParser_WritesExpectedOutput()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-cli");
            try
            {
                const int iter = 100;
                var probeName = "probe-cli";
                var fieldName = "U";

                const int probeCount = 12;
                const int lastTime = 1500;
                var content = BuildTypicalProbeFile(probeCount, lastTime, vector: true);

                WriteProbeFile(root, probeName, iter, fieldName, content);

                var probePath = Path.Combine(root, "postProcessing", probeName, iter.ToString(), fieldName);
                var outputPath = Path.Combine(root, "parsed.txt");
                var scriptPath = Path.Combine(root, "parse_probe.ps1");
                var script = BuildProbeParseScript(probePath, outputPath);
                File.WriteAllText(scriptPath, script);

                var result = RunPowerShellScript(root, scriptPath);
                Assert.True(result.ExitCode == 0, $"PowerShell failed: {result.Error}");

                Assert.True(File.Exists(outputPath), "Expected parsed output file to be written.");

                var output = File.ReadAllText(outputPath).Trim();
                var values = output.Split(',');
                Assert.Equal(probeCount * 3, values.Length);

                Assert.Equal(1.1, double.Parse(values[0], CultureInfo.InvariantCulture), 6);
                Assert.Equal(1.2, double.Parse(values[1], CultureInfo.InvariantCulture), 6);
                Assert.Equal(1.3, double.Parse(values[2], CultureInfo.InvariantCulture), 6);

                var lastIndex = values.Length - 3;
                var lastProbeBase = probeCount - 1 + 1.1;
                Assert.Equal(lastProbeBase, double.Parse(values[lastIndex], CultureInfo.InvariantCulture), 6);
                Assert.Equal(lastProbeBase + 0.1, double.Parse(values[lastIndex + 1], CultureInfo.InvariantCulture), 6);
                Assert.Equal(lastProbeBase + 0.2, double.Parse(values[lastIndex + 2], CultureInfo.InvariantCulture), 6);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [RequiresGrasshopperFact]
        public void Probing_Vector_ParsesTypicalFileWithHeaders()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-vector-headers");
            try
            {
                const int iter = 100;
                var probeName = "probe-vector-headers";
                var fieldName = "U";
                const int probeCount = 3;
                const int lastTime = 1500;

                Directory.CreateDirectory(Path.Combine(root, iter.ToString()));
                var content = BuildTypicalProbeFile(probeCount, lastTime, vector: true);
                WriteProbeFile(root, probeName, iter, fieldName, content);

                var points = new List<Point3d>
                {
                    new Point3d(0, 0, 0),
                    new Point3d(1, 0, 0),
                    new Point3d(2, 0, 0)
                };
                var ofField = new OFField(fieldName, probeName, 1);
                var res = new OFResult(null, new OFRunSettings(endTime: iter), null, root);

                var probing = new Probing(points, root, root, ofField, res, rerun: true, currWindDir: "0");

                Assert.Equal(probeCount, probing.ResultVec.Length);
                Assert.Equal(1.1, probing.ResultVec[0].Value.X, 3);
                Assert.Equal(1.2, probing.ResultVec[0].Value.Y, 3);
                Assert.Equal(1.3, probing.ResultVec[0].Value.Z, 3);

                Assert.Equal(3.1, probing.ResultVec[2].Value.X, 3);
                Assert.Equal(3.2, probing.ResultVec[2].Value.Y, 3);
                Assert.Equal(3.3, probing.ResultVec[2].Value.Z, 3);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [RequiresGrasshopperFact]
        public void Probing_RefreshesWhenOpenFoamProbeFileIsNewerThanCache()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-cache-refresh");
            try
            {
                const int iter = 100;
                var probeName = "probe-cache-refresh";
                var fieldName = "U";

                Directory.CreateDirectory(Path.Combine(root, iter.ToString()));
                WriteProbeFile(root, probeName, iter, fieldName, "0 (1 2 3) (4 5 6)\n100 (7 8 9) (10 11 12)\n");

                var points = new List<Point3d> { new Point3d(0, 0, 0), new Point3d(1, 0, 0) };
                var ofField = new OFField(fieldName, probeName, 1);
                var res = new OFResult(null, new OFRunSettings(endTime: iter), null, root);

                // Initial parse writes binary cache.
                var first = new Probing(points, root, root, ofField, res, rerun: true, currWindDir: "0");
                Assert.Equal(7, first.ResultVec[0].Value.X, 3);

                var probePath = Path.Combine(root, "postProcessing", probeName, iter.ToString(), fieldName);
                var binPath = Path.Combine(root, "postProcessing", "0_probe-cache-refresh_U.bin");
                Assert.True(File.Exists(binPath), "Expected vector probe binary file to be written.");

                // Make cache stale and source newer with different values.
                File.SetLastWriteTimeUtc(binPath, new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc));
                WriteProbeFile(root, probeName, iter, fieldName, "0 (11 12 13) (14 15 16)\n100 (17 18 19) (20 21 22)\n");
                File.SetLastWriteTimeUtc(probePath, new DateTime(2026, 2, 10, 12, 5, 0, DateTimeKind.Utc));

                // rerun:false should still parse from source because source is newer than cache.
                var second = new Probing(points, root, root, ofField, res, rerun: false, currWindDir: "0");
                Assert.Equal(17, second.ResultVec[0].Value.X, 3);
                Assert.Equal(18, second.ResultVec[0].Value.Y, 3);
                Assert.Equal(19, second.ResultVec[0].Value.Z, 3);
                Assert.Equal(20, second.ResultVec[1].Value.X, 3);
                Assert.Equal(21, second.ResultVec[1].Value.Y, 3);
                Assert.Equal(22, second.ResultVec[1].Value.Z, 3);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [RequiresGrasshopperFact]
        public void Probing_UsesCacheWhenCacheIsNewerThanOpenFoamProbeFile()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-cache-prefer-cache");
            try
            {
                const int iter = 100;
                var probeName = "probe-cache-prefer-cache";
                var fieldName = "U";

                Directory.CreateDirectory(Path.Combine(root, iter.ToString()));
                WriteProbeFile(root, probeName, iter, fieldName, "0 (1 2 3) (4 5 6)\n100 (7 8 9) (10 11 12)\n");

                var points = new List<Point3d> { new Point3d(0, 0, 0), new Point3d(1, 0, 0) };
                var ofField = new OFField(fieldName, probeName, 1);
                var res = new OFResult(null, new OFRunSettings(endTime: iter), null, root);

                // Initial parse writes binary cache with values 7..12.
                var first = new Probing(points, root, root, ofField, res, rerun: true, currWindDir: "0");
                Assert.Equal(7, first.ResultVec[0].Value.X, 3);

                var probePath = Path.Combine(root, "postProcessing", probeName, iter.ToString(), fieldName);
                var binPath = Path.Combine(root, "postProcessing", "0_probe-cache-prefer-cache_U.bin");
                Assert.True(File.Exists(binPath), "Expected vector probe binary file to be written.");

                // Change source content but force it to look older than cache.
                WriteProbeFile(root, probeName, iter, fieldName, "0 (11 12 13) (14 15 16)\n100 (17 18 19) (20 21 22)\n");
                File.SetLastWriteTimeUtc(probePath, new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(binPath, new DateTime(2026, 2, 10, 12, 5, 0, DateTimeKind.Utc));

                // rerun:false should keep using cache because cache is newer than source.
                var second = new Probing(points, root, root, ofField, res, rerun: false, currWindDir: "0");
                Assert.Equal(7, second.ResultVec[0].Value.X, 3);
                Assert.Equal(8, second.ResultVec[0].Value.Y, 3);
                Assert.Equal(9, second.ResultVec[0].Value.Z, 3);
                Assert.Equal(10, second.ResultVec[1].Value.X, 3);
                Assert.Equal(11, second.ResultVec[1].Value.Y, 3);
                Assert.Equal(12, second.ResultVec[1].Value.Z, 3);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [RequiresGrasshopperFact]
        public void Probing_Scalar_ParsesTypicalFileWithHeaders()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-scalar-headers");
            try
            {
                const int iter = 100;
                var probeName = "probe-scalar-headers";
                var fieldName = "p";
                const int probeCount = 3;
                const int lastTime = 1500;

                Directory.CreateDirectory(Path.Combine(root, iter.ToString()));
                var content = BuildTypicalProbeFile(probeCount, lastTime, vector: false);
                WriteProbeFile(root, probeName, iter, fieldName, content);

                var points = new List<Point3d>
                {
                    new Point3d(0, 0, 0),
                    new Point3d(1, 0, 0),
                    new Point3d(2, 0, 0)
                };
                var ofField = new OFField(fieldName, probeName, 1);
                var res = new OFResult(null, new OFRunSettings(endTime: iter), null, root);

                var probing = new Probing(points, root, root, ofField, res, rerun: true, currWindDir: "0");

                Assert.Equal(probeCount, probing.ResultScalar.Length);
                Assert.Equal(1.1, probing.ResultScalar[0].Value, 3);
                Assert.Equal(2.1, probing.ResultScalar[1].Value, 3);
                Assert.Equal(3.1, probing.ResultScalar[2].Value, 3);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [RhinoRequiredFact]
        public void OFField_ReformatOFFields_MapsExpectedNames()
        {
            Assert.Equal("U", OFField.ReformatOFFields(0));
            Assert.Equal("total(p)_coeff", OFField.ReformatOFFields(1));
            Assert.Equal("p", OFField.ReformatOFFields(2));
            Assert.Equal("epsilon", OFField.ReformatOFFields(3));
            Assert.Equal("omega", OFField.ReformatOFFields(4));
            Assert.Equal("k", OFField.ReformatOFFields(5));
            Assert.Equal("nut", OFField.ReformatOFFields(6));
            Assert.Equal("phi", OFField.ReformatOFFields(7));
            Assert.Equal("aoa", OFField.ReformatOFFields(8));
            Assert.Equal("covid19", OFField.ReformatOFFields(9));
        }

        [RhinoRequiredFact]
        public void Probing_DeduplicateProbePointsForOpenFoam_RemovesDuplicatesAndKeepsOrder()
        {
            var input = new List<Point3d>
            {
                new Point3d(0.0, 0.0, 0.0),
                new Point3d(1.0, 2.0, 3.0),
                new Point3d(1.0004, 2.0004, 3.0004), // Same as previous after OpenFOAM 0.### formatting
                new Point3d(4.0, 5.0, 6.0),
                new Point3d(0.0, 0.0, 0.0)
            };

            var deduplicated = Probing.DeduplicateProbePointsForOpenFoam(input, out int removed);

            Assert.Equal(2, removed);
            Assert.Equal(3, deduplicated.Count);
            Assert.Equal(new Point3d(0.0, 0.0, 0.0), deduplicated[0]);
            Assert.Equal(new Point3d(1.0, 2.0, 3.0), deduplicated[1]);
            Assert.Equal(new Point3d(4.0, 5.0, 6.0), deduplicated[2]);
        }

        [RhinoRequiredFact]
        public void Probing_GetLatestTime_IgnoresNonNumericCaseFolders()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-latest-time");
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "constant"));
                Directory.CreateDirectory(Path.Combine(root, "system"));
                Directory.CreateDirectory(Path.Combine(root, "postProcessing"));
                Directory.CreateDirectory(Path.Combine(root, "50"));
                Directory.CreateDirectory(Path.Combine(root, "150"));

                var field = new OFField("U", "probe-latest", 1);
                var result = new OFResult(null, new OFRunSettings(), null, root);

                int latest = Probing.GetLatestTime(root, result, field);
                Assert.Equal(150, latest);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        [WindowsOnlyFact]
        public void Probing_BatchLaunch_WorksWhenInstallAndCasePathsContainSpaces()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-batch-launch");
            var originalHome = Environment.GetEnvironmentVariable("HOME");

            try
            {
                var fakeHome = Path.Combine(root, "Fake Home");
                var fakeInstallDir = Path.Combine(root, "blueCFD Install");
                var fakeBinDir = Path.Combine(root, "Fake OpenFOAM Bin");
                var caseDir = Path.Combine(root, "Case With Spaces");
                var markerPath = Path.Combine(caseDir, "cwd.txt");

                Directory.CreateDirectory(fakeHome);
                Directory.CreateDirectory(fakeInstallDir);
                Directory.CreateDirectory(fakeBinDir);
                Directory.CreateDirectory(caseDir);

                Environment.SetEnvironmentVariable("HOME", fakeHome);

                File.WriteAllText(
                    Path.Combine(fakeInstallDir, "setvars_OF8.bat"),
                    "@echo off\r\n" +
                    $"set \"PATH={fakeBinDir};%PATH%\"\r\n");

                File.WriteAllText(
                    Path.Combine(fakeBinDir, "reconstructParMesh.bat"),
                    "@echo off\r\n" +
                    $"echo %CD%>\"{markerPath}\"\r\n");

                var script = EddyLib.Strings.BatFiles.BlueCfdScriptBuilder.BuildBlueCfdBatch(
                    new[] { "reconstructParMesh" },
                    caseDir,
                    EddyLib.Strings.RunMode.Canvas,
                    fakeInstallDir);

                Utilities.StartProcess.StartBatchScriptCMDNT(
                    script,
                    createnowindow: true,
                    waitforexit: true,
                    close: true,
                    startInNewThread: false);

                Assert.True(File.Exists(markerPath), "Expected fake reconstructParMesh command to run.");
                Assert.Equal(caseDir, File.ReadAllText(markerPath).Trim());
            }
            finally
            {
                Environment.SetEnvironmentVariable("HOME", originalHome);
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        private static void WriteProbeFile(string root, string probeName, int iter, string fieldName, string content)
        {
            var dir = Path.Combine(root, "postProcessing", probeName, iter.ToString());
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, fieldName), content);
        }

        private static string BuildTypicalProbeFile(int probeCount, int lastTime, bool vector)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < probeCount; i++)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "# Probe {0} ({1} {2} {3})", i, i, i + 1, 2);
                sb.AppendLine();
            }

            sb.Append("# Time");
            for (int i = 0; i < probeCount; i++)
            {
                sb.Append(" ");
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
            }
            sb.AppendLine();

            if (vector)
            {
                AppendVectorDataLine(sb, 0, probeCount, 0.1);
                AppendVectorDataLine(sb, lastTime, probeCount, 1.1);
            }
            else
            {
                AppendScalarDataLine(sb, 0, probeCount, 0.1);
                AppendScalarDataLine(sb, lastTime, probeCount, 1.1);
            }

            return sb.ToString();
        }

        private static void AppendVectorDataLine(StringBuilder sb, int time, int probeCount, double baseOffset)
        {
            sb.Append(time.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < probeCount; i++)
            {
                var x = i + baseOffset;
                var y = i + baseOffset + 0.1;
                var z = i + baseOffset + 0.2;
                sb.AppendFormat(CultureInfo.InvariantCulture, " ({0} {1} {2})", x, y, z);
            }
            sb.AppendLine();
        }

        private static void AppendScalarDataLine(StringBuilder sb, int time, int probeCount, double baseOffset)
        {
            sb.Append(time.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < probeCount; i++)
            {
                var value = i + baseOffset;
                sb.Append(" ");
                sb.Append(value.ToString(CultureInfo.InvariantCulture));
            }
            sb.AppendLine();
        }

        private static void RunBatchFileInteractive(string workingDir, string batchFileName)
        {
            var batchFilePath = Path.Combine(workingDir, batchFileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = false
            };
            startInfo.ArgumentList.Add("/C");
            startInfo.ArgumentList.Add(batchFilePath);

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
            }
        }

        private static string BuildProbeParseScript(string probePath, string outputPath)
        {
            var probe = EscapePowerShellSingleQuoted(probePath);
            var output = EscapePowerShellSingleQuoted(outputPath);

            return
                "$ProgressPreference = 'SilentlyContinue'\n" +
                $"$line = Get-Content -Path '{probe}' | Where-Object {{ ($_ -match '\\S') -and ($_ -notmatch '^\\s*#') -and ($_ -notmatch '^\\s*Time\\b') }} | Select-Object -Last 1\n" +
                "$line = $line -replace '[()]',''\n" +
                "$parts = $line -split '\\s+'\n" +
                $"if ($parts.Length -gt 1) {{ ($parts[1..($parts.Length-1)] -join ',') | Set-Content -Path '{output}' }}\n";
        }

        private static string EscapePowerShellSingleQuoted(string value)
        {
            return value?.Replace("'", "''") ?? string.Empty;
        }

        private static (int ExitCode, string Output, string Error) RunPowerShellScript(string workingDir, string scriptPath)
        {
            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            if (!File.Exists(powershell))
            {
                powershell = "powershell.exe";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = powershell,
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(startInfo))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return (process.ExitCode, output, error);
            }
        }
    }
}
