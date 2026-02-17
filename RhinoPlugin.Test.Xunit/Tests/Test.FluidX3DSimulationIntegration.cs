using EddyLib;
using EddyLib.FluidX3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    internal static class FluidX3DTestEnvironment
    {
        public static bool TryResolveSourceDirectory(out string sourceRoot, out string reason)
        {
            List<string> candidates = new List<string>();
            string env = Environment.GetEnvironmentVariable("EDDY_FLUIDX3D_SOURCE");
            if (!string.IsNullOrWhiteSpace(env))
            {
                candidates.Add(Path.GetFullPath(env.Trim()));
            }

            foreach (string localCandidate in EnumerateLocalRepositoryCandidates())
            {
                candidates.Add(localCandidate);
            }

            candidates.Add(Path.GetFullPath(FluidX3DAblWorkflow.GetDefaultSourceDirectory()));

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < candidates.Count; i++)
            {
                string candidate = candidates[i];
                if (!seen.Add(candidate))
                {
                    continue;
                }

                if (Directory.Exists(candidate) && FluidX3DAblWorkflow.IsValidSourceDirectory(candidate))
                {
                    sourceRoot = candidate;
                    reason = null;
                    return true;
                }
            }

            sourceRoot = null;
            reason = "FluidX3D source not found. Set EDDY_FLUIDX3D_SOURCE to a valid FluidX3D clone, "
                + "or install it under " + FluidX3DAblWorkflow.GetDefaultSourceDirectory() + ".";
            return false;
        }

        private static IEnumerable<string> EnumerateLocalRepositoryCandidates()
        {
            List<string> roots = new List<string>();

            // Typical invocation root during local dotnet test.
            string cwd = Directory.GetCurrentDirectory();
            if (!string.IsNullOrWhiteSpace(cwd))
            {
                roots.Add(Path.GetFullPath(cwd));
            }

            // Fallback: test assembly output root.
            string baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                roots.Add(Path.GetFullPath(baseDir));
            }

            // Walk upwards and look for a solution root marker.
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in roots)
            {
                DirectoryInfo current = new DirectoryInfo(root);
                while (current != null)
                {
                    if (!seen.Add(current.FullName))
                    {
                        break;
                    }

                    bool looksLikeRepoRoot =
                        File.Exists(Path.Combine(current.FullName, "Eddy.sln"))
                        || Directory.Exists(Path.Combine(current.FullName, ".git"));

                    if (looksLikeRepoRoot)
                    {
                        yield return Path.Combine(current.FullName, "FluidX3D");
                        yield return Path.Combine(current.FullName, "zen-margulis", "FluidX3D");
                    }

                    current = current.Parent;
                }
            }
        }
    }

    [Trait("Category", "Integration")]
    [Trait("Category", "FluidX3D")]
    [Trait("Category", "Slow")]
    public class FluidX3DSimulationIntegrationTests
    {
        private static readonly Regex DimensionsRegex = new Regex(
            @"^DIMENSIONS\s+(?<nx>\d+)\s+(?<ny>\d+)\s+(?<nz>\d+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Fact]
        public void FluidX3DSimulation_EndToEnd_RunsSolverAndProducesRealVtkOutputs()
        {
            Assert.True(
                FluidX3DTestEnvironment.TryResolveSourceDirectory(out string sourceRoot, out string reason),
                reason);

            string tempRoot = TestFixtures.CreateTestDirectory("fluidx3d-sim-gpu");
            try
            {
                string workingRoot = Path.Combine(tempRoot, "working");

                FluidX3DAblSettings settings = new FluidX3DAblSettings
                {
                    MemoryMb = 256,
                    Uref = 5.0,
                    Zref = 10.0,
                    Z0 = 0.1,
                    SimSeconds = 180.0,
                    ExportIntervalSeconds = 10.0,
                    DomainLx = 120.0,
                    DomainLy = 120.0,
                    DomainLz = 50.0
                };

                FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(sourceRoot, workingRoot, settings);

                Assert.True(Directory.Exists(result.CaseRoot));
                Assert.Equal(Path.Combine(result.CaseRoot, "bin", "export"), result.ExportDirectory);

                string runLog = RunFluidX3DCase(result.CaseRoot);
                Assert.False(string.IsNullOrWhiteSpace(runLog), "Expected non-empty FluidX3D run log.");

                string[] uFiles = Directory.GetFiles(result.ExportDirectory, "u-*.vtk", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                string[] rhoFiles = Directory.GetFiles(result.ExportDirectory, "rho-*.vtk", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                string[] flagsFiles = Directory.GetFiles(result.ExportDirectory, "flags-*.vtk", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Assert.True(uFiles.Length >= 2, "Expected at least 2 velocity VTK files from simulation.");
                Assert.True(rhoFiles.Length >= 1, "Expected at least 1 density VTK file from simulation.");
                Assert.True(flagsFiles.Length >= 1, "Expected at least 1 flags VTK file from simulation.");

                VtkInfo uInfo = AssertValidLegacyBinaryVtk(uFiles[uFiles.Length - 1], expectedComponents: 3);
                VtkInfo rhoInfo = AssertValidLegacyBinaryVtk(rhoFiles[rhoFiles.Length - 1], expectedComponents: 1);
                VtkInfo flagsInfo = AssertValidLegacyBinaryVtk(flagsFiles[flagsFiles.Length - 1], expectedComponents: 1);

                // All fields should have the same grid layout in a single run.
                Assert.Equal(uInfo.Nx, rhoInfo.Nx);
                Assert.Equal(uInfo.Ny, rhoInfo.Ny);
                Assert.Equal(uInfo.Nz, rhoInfo.Nz);
                Assert.Equal(uInfo.Nx, flagsInfo.Nx);
                Assert.Equal(uInfo.Ny, flagsInfo.Ny);
                Assert.Equal(uInfo.Nz, flagsInfo.Nz);

                // Real simulation outputs should be non-trivial in size.
                Assert.True(uInfo.PayloadBytes > 100_000L, "Velocity payload unexpectedly small.");
                Assert.True(rhoInfo.PayloadBytes > 30_000L, "Density payload unexpectedly small.");
                Assert.True(flagsInfo.PayloadBytes > 30_000L, "Flags payload unexpectedly small.");

                // Vector field payload should be exactly 3x scalar payload for same grid.
                Assert.Equal(rhoInfo.PayloadBytes * 3L, uInfo.PayloadBytes);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(tempRoot);
            }
        }

        private static string RunFluidX3DCase(string caseRoot)
        {
            if (string.IsNullOrWhiteSpace(caseRoot))
            {
                throw new ArgumentException("Case root is required.", nameof(caseRoot));
            }

            if (!Directory.Exists(caseRoot))
            {
                throw new DirectoryNotFoundException("Case root not found: " + caseRoot);
            }

            if (OperatingSystem.IsWindows())
            {
                return RunFluidX3DCaseWindows(caseRoot);
            }

            return RunFluidX3DCaseUnix(caseRoot);
        }

        private static string RunFluidX3DCaseUnix(string caseRoot)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine(RunProcess("/bin/bash", "make.sh", caseRoot, timeoutMs: 900000));
            return log.ToString();
        }

        private static string RunFluidX3DCaseWindows(string caseRoot)
        {
            string slnPath = Path.Combine(caseRoot, "FluidX3D.sln");
            if (!File.Exists(slnPath))
            {
                throw new FileNotFoundException("FluidX3D solution file not found.", slnPath);
            }

            string msbuildPath = ResolveMsBuildPath(caseRoot);
            StringBuilder log = new StringBuilder();
            log.AppendLine(RunProcess(
                msbuildPath,
                "\"" + slnPath + "\" /m /p:Configuration=Release /p:Platform=x64",
                caseRoot,
                timeoutMs: 900000));

            string[] exeCandidates = new[]
            {
                Path.Combine(caseRoot, "bin", "FluidX3D.exe"),
                Path.Combine(caseRoot, "bin", "Release", "FluidX3D.exe"),
                Path.Combine(caseRoot, "bin", "x64", "Release", "FluidX3D.exe")
            };

            string exePath = exeCandidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(exePath))
            {
                throw new FileNotFoundException(
                    "FluidX3D.exe not found after build. Checked: " + string.Join(", ", exeCandidates));
            }

            log.AppendLine(RunProcess(exePath, string.Empty, caseRoot, timeoutMs: 900000));
            return log.ToString();
        }

        private static string ResolveMsBuildPath(string workingDirectory)
        {
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string vswhere = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (File.Exists(vswhere))
            {
                string output = RunProcess(
                    vswhere,
                    "-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe",
                    workingDirectory,
                    timeoutMs: 60000,
                    throwOnNonZero: false);

                string firstLine = output
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(firstLine) && File.Exists(firstLine.Trim()))
                {
                    return firstLine.Trim();
                }
            }

            return "MSBuild.exe";
        }

        private static string RunProcess(
            string fileName,
            string arguments,
            string workingDirectory,
            int timeoutMs,
            bool throwOnNonZero = true)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            StringBuilder log = new StringBuilder();
            using (System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = psi, EnableRaisingEvents = true })
            {
                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        log.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        log.AppendLine("[stderr] " + e.Data);
                    }
                };

                if (!process.Start())
                {
                    throw new InvalidOperationException("Failed to start process: " + fileName);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(timeoutMs))
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    throw new TimeoutException(
                        "Process timed out after " + timeoutMs.ToString(CultureInfo.InvariantCulture)
                        + " ms: " + fileName + " " + arguments + Environment.NewLine + log.ToString());
                }

                process.WaitForExit();

                if (throwOnNonZero && process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "Process failed with exit code " + process.ExitCode.ToString(CultureInfo.InvariantCulture)
                        + ": " + fileName + " " + arguments + Environment.NewLine + log.ToString());
                }
            }

            return log.ToString();
        }

        private static VtkInfo AssertValidLegacyBinaryVtk(string path, int expectedComponents)
        {
            Assert.True(File.Exists(path), "Expected VTK file missing: " + path);

            using (FileStream fs = File.OpenRead(path))
            {
                bool isBinary = false;
                bool isStructuredPoints = false;
                int nx = -1;
                int ny = -1;
                int nz = -1;
                int components = 1;
                string dataType = "float";
                long dataOffset = -1L;

                while (true)
                {
                    string line = ReadAsciiLine(fs);
                    Assert.False(line == null, "Unexpected EOF while reading VTK header: " + path);

                    string trimmed = line.Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    if (trimmed.Equals("BINARY", StringComparison.OrdinalIgnoreCase))
                    {
                        isBinary = true;
                    }
                    else if (trimmed.StartsWith("DATASET", StringComparison.OrdinalIgnoreCase))
                    {
                        isStructuredPoints = trimmed.IndexOf("STRUCTURED_POINTS", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    else
                    {
                        Match dimMatch = DimensionsRegex.Match(trimmed);
                        if (dimMatch.Success)
                        {
                            nx = int.Parse(dimMatch.Groups["nx"].Value, CultureInfo.InvariantCulture);
                            ny = int.Parse(dimMatch.Groups["ny"].Value, CultureInfo.InvariantCulture);
                            nz = int.Parse(dimMatch.Groups["nz"].Value, CultureInfo.InvariantCulture);
                            continue;
                        }

                        if (trimmed.StartsWith("SCALARS", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                            dataType = parts.Length >= 3 ? parts[2] : "float";
                            components = parts.Length >= 4 ? int.Parse(parts[3], CultureInfo.InvariantCulture) : 1;
                            continue;
                        }

                        if (trimmed.StartsWith("VECTORS", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                            dataType = parts.Length >= 3 ? parts[2] : "float";
                            components = 3;
                            dataOffset = fs.Position;
                            break;
                        }

                        if (trimmed.StartsWith("LOOKUP_TABLE", StringComparison.OrdinalIgnoreCase))
                        {
                            dataOffset = fs.Position;
                            break;
                        }
                    }
                }

                Assert.True(isBinary, "VTK is not binary: " + path);
                Assert.True(isStructuredPoints, "VTK is not STRUCTURED_POINTS: " + path);
                Assert.True(nx > 0 && ny > 0 && nz > 0, "Invalid VTK dimensions in: " + path);
                Assert.Equal(expectedComponents, components);

                int bytesPerComponent = GetVtkTypeSizeBytes(dataType);
                long expectedPayloadBytes = (long)nx * ny * nz * components * bytesPerComponent;
                long actualPayloadBytes = fs.Length - dataOffset;
                Assert.Equal(expectedPayloadBytes, actualPayloadBytes);

                return new VtkInfo
                {
                    Nx = nx,
                    Ny = ny,
                    Nz = nz,
                    Components = components,
                    DataType = dataType,
                    BytesPerComponent = bytesPerComponent,
                    PayloadBytes = actualPayloadBytes
                };
            }
        }

        private static int GetVtkTypeSizeBytes(string vtkType)
        {
            string key = (vtkType ?? string.Empty).Trim().ToLowerInvariant();
            switch (key)
            {
                case "char":
                case "unsigned_char":
                case "signed_char":
                    return 1;
                case "short":
                case "unsigned_short":
                    return 2;
                case "int":
                case "unsigned_int":
                case "long":
                case "unsigned_long":
                case "float":
                    return 4;
                case "double":
                    return 8;
                default:
                    throw new InvalidDataException("Unsupported VTK scalar type: " + vtkType);
            }
        }

        private static string ReadAsciiLine(FileStream fs)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int raw = fs.ReadByte();
                if (raw < 0)
                {
                    return sb.Length == 0 ? null : sb.ToString();
                }

                char ch = (char)raw;
                if (ch == '\n')
                {
                    return sb.ToString();
                }

                if (ch != '\r')
                {
                    sb.Append(ch);
                }
            }
        }

        private sealed class VtkInfo
        {
            public int Nx;
            public int Ny;
            public int Nz;
            public int Components;
            public string DataType;
            public int BytesPerComponent;
            public long PayloadBytes;
        }
    }
}
