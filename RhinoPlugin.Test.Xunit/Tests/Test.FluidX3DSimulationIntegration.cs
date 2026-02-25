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
        public const string SourceEnvironmentVariable = "EDDY_FLUIDX3D_SOURCE";
        public const string CommitEnvironmentVariable = "EDDY_FLUIDX3D_COMMIT";

        public static bool TryResolveSourceDirectory(out string sourceRoot, out string reason)
        {
            List<string> candidates = new List<string>();
            string env = Environment.GetEnvironmentVariable(SourceEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(env))
            {
                candidates.Add(Path.GetFullPath(env.Trim()));
            }

            // Prefer installed engine source before local repository copies.
            candidates.Add(Path.GetFullPath(FluidX3DAblWorkflow.GetDefaultSourceDirectory()));

            foreach (string localCandidate in EnumerateLocalRepositoryCandidates())
            {
                candidates.Add(localCandidate);
            }

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
            reason = "FluidX3D source not found. Set " + SourceEnvironmentVariable + " to a valid FluidX3D clone, "
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
        public void FluidX3DInstallation_SourceIsValid_AndMatchesPinnedCommitWhenConfigured()
        {
            Assert.True(
                FluidX3DTestEnvironment.TryResolveSourceDirectory(out string sourceRoot, out string reason),
                reason);

            Assert.True(
                FluidX3DAblWorkflow.IsValidSourceDirectory(sourceRoot),
                "Resolved FluidX3D source directory is invalid: " + sourceRoot);

            string gitMetadataPath = Path.Combine(sourceRoot, ".git");
            Assert.True(
                Directory.Exists(gitMetadataPath) || File.Exists(gitMetadataPath),
                "FluidX3D source should be a git clone (.git metadata missing): " + sourceRoot);

            if (OperatingSystem.IsWindows())
            {
                Assert.True(
                    File.Exists(Path.Combine(sourceRoot, "FluidX3D.sln")),
                    "Windows installation expects FluidX3D.sln in source root.");

                string msbuildPath = ResolveMsBuildPath(sourceRoot);
                EnsureWindowsCppBuildToolchainAvailable(msbuildPath, sourceRoot);
            }
            else
            {
                Assert.True(
                    File.Exists(Path.Combine(sourceRoot, "make.sh")),
                    "macOS/Linux installation expects make.sh in source root.");
            }

            string actualCommit = ResolveGitHeadCommit(sourceRoot);
            Assert.Matches("^[0-9a-f]{40}$", actualCommit);

            string expectedCommitRaw = Environment.GetEnvironmentVariable(FluidX3DTestEnvironment.CommitEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(expectedCommitRaw))
            {
                expectedCommitRaw = FluidX3DAblWorkflow.ResolvePinnedCommit();
            }

            string expectedCommit = NormalizePinnedCommit(expectedCommitRaw);
            Assert.True(
                actualCommit.StartsWith(expectedCommit, StringComparison.OrdinalIgnoreCase),
                "FluidX3D commit mismatch. Expected prefix " + expectedCommit
                + " from " + FluidX3DTestEnvironment.CommitEnvironmentVariable
                + " but found " + actualCommit + " at " + sourceRoot + ".");
        }

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

                // Regression guard: payload can be structurally valid yet contain only zeros.
                AssertVelocityFieldHasNonZeroBoundarySample(uFiles[uFiles.Length - 1], uInfo);
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
            EnsureWindowsCppBuildToolchainAvailable(msbuildPath, caseRoot);
            string vcxprojPath = Path.Combine(caseRoot, "FluidX3D.vcxproj");
            string platformToolset = ResolveWindowsPlatformToolset(vcxprojPath, msbuildPath, caseRoot);

            string buildArguments = "\"" + slnPath + "\" /m /p:Configuration=Release /p:Platform=x64";
            if (!string.IsNullOrWhiteSpace(platformToolset))
            {
                buildArguments += " /p:PlatformToolset=" + platformToolset;
            }

            StringBuilder log = new StringBuilder();
            log.AppendLine(RunProcess(
                msbuildPath,
                buildArguments,
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
                // Prefer Visual Studio instances that include both MSBuild and VC++ tools.
                string vcReadyOutput = RunProcess(
                    vswhere,
                    "-latest -products * -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -find MSBuild\\**\\Bin\\MSBuild.exe",
                    workingDirectory,
                    timeoutMs: 60000,
                    throwOnNonZero: false);

                string vcReadyFirstLine = vcReadyOutput
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(vcReadyFirstLine) && File.Exists(vcReadyFirstLine.Trim()))
                {
                    return vcReadyFirstLine.Trim();
                }

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

        private static void EnsureWindowsCppBuildToolchainAvailable(string msbuildPath, string workingDirectory)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            string vcTargetsPath = Environment.GetEnvironmentVariable("VCTargetsPath");
            if (!string.IsNullOrWhiteSpace(vcTargetsPath))
            {
                string envProps = Path.Combine(vcTargetsPath, "Microsoft.Cpp.Default.props");
                if (File.Exists(envProps))
                {
                    return;
                }
            }

            string msbuildExecutable = ResolveAbsoluteMsBuildPath(msbuildPath, workingDirectory);
            string propsPath = FindCppDefaultPropsNearMsBuild(msbuildExecutable);
            if (!string.IsNullOrWhiteSpace(propsPath))
            {
                return;
            }

            throw new InvalidOperationException(
                "Windows C++ build targets are missing. FluidX3D requires Visual Studio C++ build tools "
                + "(Desktop development with C++ / component Microsoft.VisualStudio.Component.VC.Tools.x86.x64)."
                + Environment.NewLine
                + "Resolved MSBuild: " + (string.IsNullOrWhiteSpace(msbuildExecutable) ? msbuildPath : msbuildExecutable));
        }

        private static string ResolveAbsoluteMsBuildPath(string msbuildPath, string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(msbuildPath))
            {
                return null;
            }

            if (Path.IsPathRooted(msbuildPath) && File.Exists(msbuildPath))
            {
                return msbuildPath;
            }

            string whereOutput = RunProcess(
                "where",
                "MSBuild.exe",
                workingDirectory,
                timeoutMs: 60000,
                throwOnNonZero: false);

            return whereOutput
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(File.Exists);
        }

        private static string FindCppDefaultPropsNearMsBuild(string msbuildPath)
        {
            if (string.IsNullOrWhiteSpace(msbuildPath) || !File.Exists(msbuildPath))
            {
                return null;
            }

            try
            {
                DirectoryInfo binDir = new FileInfo(msbuildPath).Directory;
                DirectoryInfo currentDir = binDir?.Parent;
                DirectoryInfo msbuildRoot = currentDir?.Parent;
                if (msbuildRoot == null)
                {
                    return null;
                }

                string vcRoot = Path.Combine(msbuildRoot.FullName, "Microsoft", "VC");
                if (!Directory.Exists(vcRoot))
                {
                    return null;
                }

                foreach (string versionDir in Directory.GetDirectories(vcRoot, "v*")
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    string propsPath = Path.Combine(versionDir, "Microsoft.Cpp.Default.props");
                    if (File.Exists(propsPath))
                    {
                        return propsPath;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveWindowsPlatformToolset(string vcxprojPath, string msbuildPath, string workingDirectory)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            string requestedToolset = ReadRequestedPlatformToolset(vcxprojPath);
            List<string> availableToolsets = DiscoverInstalledWindowsPlatformToolsets(msbuildPath, workingDirectory);
            if (availableToolsets.Count == 0)
            {
                throw new InvalidOperationException(
                    "No Visual C++ platform toolsets found. Ensure Desktop development with C++ is installed.");
            }

            if (!string.IsNullOrWhiteSpace(requestedToolset)
                && availableToolsets.Any(t => string.Equals(t, requestedToolset, StringComparison.OrdinalIgnoreCase)))
            {
                return requestedToolset;
            }

            string fallback = availableToolsets
                .OrderByDescending(ParseToolsetVersion)
                .ThenByDescending(t => t, StringComparer.OrdinalIgnoreCase)
                .First();

            return fallback;
        }

        private static string ReadRequestedPlatformToolset(string vcxprojPath)
        {
            if (string.IsNullOrWhiteSpace(vcxprojPath) || !File.Exists(vcxprojPath))
            {
                return null;
            }

            try
            {
                string content = File.ReadAllText(vcxprojPath);
                Match match = Regex.Match(content, "<PlatformToolset>(?<value>[^<]+)</PlatformToolset>", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    return null;
                }

                string value = match.Groups["value"].Value?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            catch
            {
                return null;
            }
        }

        private static List<string> DiscoverInstalledWindowsPlatformToolsets(string msbuildPath, string workingDirectory)
        {
            List<string> toolsets = new List<string>();
            string msbuildExecutable = ResolveAbsoluteMsBuildPath(msbuildPath, workingDirectory);
            if (string.IsNullOrWhiteSpace(msbuildExecutable) || !File.Exists(msbuildExecutable))
            {
                return toolsets;
            }

            try
            {
                DirectoryInfo binDir = new FileInfo(msbuildExecutable).Directory;
                DirectoryInfo currentDir = binDir?.Parent;
                DirectoryInfo msbuildRoot = currentDir?.Parent;
                if (msbuildRoot == null)
                {
                    return toolsets;
                }

                string[] platformRoots = new[]
                {
                    Path.Combine(msbuildRoot.FullName, "Microsoft", "VC", "v180", "Platforms", "x64", "PlatformToolsets"),
                    Path.Combine(msbuildRoot.FullName, "Microsoft", "VC", "v170", "Platforms", "x64", "PlatformToolsets"),
                    Path.Combine(msbuildRoot.FullName, "Microsoft", "VC", "v160", "Platforms", "x64", "PlatformToolsets"),
                    Path.Combine(msbuildRoot.FullName, "Microsoft", "VC", "v150", "Platforms", "x64", "PlatformToolsets")
                };

                foreach (string platformRoot in platformRoots)
                {
                    if (!Directory.Exists(platformRoot))
                    {
                        continue;
                    }

                    foreach (string dir in Directory.GetDirectories(platformRoot))
                    {
                        string name = Path.GetFileName(dir);
                        if (!string.IsNullOrWhiteSpace(name) && name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                        {
                            toolsets.Add(name);
                        }
                    }
                }
            }
            catch
            {
                return toolsets;
            }

            return toolsets
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int ParseToolsetVersion(string toolset)
        {
            if (string.IsNullOrWhiteSpace(toolset))
            {
                return -1;
            }

            string digits = new string(toolset.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int version))
            {
                return version;
            }

            return -1;
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

        private static string ResolveGitHeadCommit(string repositoryRoot)
        {
            string output = RunProcess(
                "git",
                "rev-parse HEAD",
                repositoryRoot,
                timeoutMs: 60000);

            string commit = output
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Length > 0);

            if (string.IsNullOrWhiteSpace(commit) || !Regex.IsMatch(commit, "^[0-9a-fA-F]{40}$"))
            {
                throw new InvalidDataException(
                    "Unable to resolve FluidX3D git HEAD commit from: " + repositoryRoot
                    + Environment.NewLine + output);
            }

            return commit.ToLowerInvariant();
        }

        private static string NormalizePinnedCommit(string commitRaw)
        {
            string commit = (commitRaw ?? string.Empty).Trim();
            if (!Regex.IsMatch(commit, "^[0-9a-fA-F]{7,40}$"))
            {
                throw new InvalidDataException(
                    FluidX3DTestEnvironment.CommitEnvironmentVariable
                    + " must be a 7-40 character hexadecimal git SHA. Value: '" + commitRaw + "'.");
            }

            return commit.ToLowerInvariant();
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
                    PayloadBytes = actualPayloadBytes,
                    DataOffset = dataOffset
                };
            }
        }

        private static void AssertVelocityFieldHasNonZeroBoundarySample(string vtkPath, VtkInfo info)
        {
            Assert.Equal(3, info.Components);
            Assert.Equal(4, info.BytesPerComponent);
            Assert.True(info.DataOffset >= 0L, "Invalid VTK payload offset: " + vtkPath);

            int ix = Math.Min(Math.Max(info.Nx / 2, 0), info.Nx - 1);
            int iy = Math.Min(Math.Max(info.Ny / 2, 0), info.Ny - 1);
            int iz = info.Nz - 1; // top boundary is set to TYPE_E with non-zero ABL velocity

            long pointIndex = ix + (long)info.Nx * (iy + (long)info.Ny * iz);
            long baseOffset = info.DataOffset + pointIndex * info.Components * info.BytesPerComponent;

            using (FileStream fs = File.OpenRead(vtkPath))
            {
                float ux = ReadSingleBigEndian(fs, baseOffset + 0L);
                float uy = ReadSingleBigEndian(fs, baseOffset + 4L);
                float uz = ReadSingleBigEndian(fs, baseOffset + 8L);
                double magnitude = Math.Sqrt((double)ux * ux + (double)uy * uy + (double)uz * uz);

                Assert.True(
                    magnitude > 1e-6,
                    "Velocity sample at top boundary is zero. "
                    + "Expected non-zero inflow/outflow boundary velocity. "
                    + "Sample=("
                    + ux.ToString("G9", CultureInfo.InvariantCulture) + ", "
                    + uy.ToString("G9", CultureInfo.InvariantCulture) + ", "
                    + uz.ToString("G9", CultureInfo.InvariantCulture) + ")");
            }
        }

        private static float ReadSingleBigEndian(FileStream fs, long offset)
        {
            byte[] bytes = new byte[4];
            fs.Position = offset;
            fs.ReadExactly(bytes, 0, bytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToSingle(bytes, 0);
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
            public long DataOffset;
        }
    }
}
