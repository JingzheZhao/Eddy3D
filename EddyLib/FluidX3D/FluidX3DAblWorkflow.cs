using System;
using System.Collections.Generic;
using System.Diagnostics;
using EddyLib.Helpers;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace EddyLib.FluidX3D
{
    public sealed class FluidX3DAblSettings
    {
        public int MemoryMb { get; set; } = 1000;
        public double Uref { get; set; } = 5.0;
        public double Zref { get; set; } = 10.0;
        public double Z0 { get; set; } = 0.1;
        public double FlowDirectionX { get; set; } = 0.0;
        public double FlowDirectionY { get; set; } = 1.0;
        public double SimSeconds { get; set; } = 360.0;
        public double ExportIntervalSeconds { get; set; } = 30.0;
        public double DomainLx { get; set; } = 300.0;
        public double DomainLy { get; set; } = 200.0;
        public double DomainLz { get; set; } = 100.0;
        public List<string> BuildingStlFiles { get; } = new List<string>();
    }

    public sealed class FluidX3DAblPrepareResult
    {
        public string CaseRoot { get; set; }
        public string SetupPath { get; set; }
        public string DefinesPath { get; set; }
        public string ExportDirectory { get; set; }
        public string ScriptsDirectory { get; set; }
        public string CommandScriptPath { get; set; }
        public string BatchScriptPath { get; set; }
        public string LaunchScriptPath { get; set; }
        public string ReadmePath { get; set; }
    }

    public static class FluidX3DAblWorkflow
    {
        public const string RepositoryUrl = "https://github.com/ProjectPhysX/FluidX3D.git";
        public const string CommitEnvironmentVariable = "EDDY_FLUIDX3D_COMMIT";
        public const string DefaultPinnedCommit = "62a1756b7b257918226ab2102efd3a47fd99ff2c";
        public const string WindowsBuildToolsDownloadUrl = "https://aka.ms/vs/17/release/vs_BuildTools.exe";
        public const string WindowsBuildToolsBootstrapperFileName = "Eddy3D-vs_BuildTools.exe";
        public const string WindowsCppWorkloadId = "Microsoft.VisualStudio.Workload.VCTools";
        public const string WindowsV142ToolsetComponentId = "Microsoft.VisualStudio.ComponentGroup.VC.Tools.142.x86.x64";

        public static FluidX3DAblPrepareResult PrepareCase(
            string fluidX3DSourceRoot,
            string workingDirectory,
            FluidX3DAblSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            ValidateSettings(settings);

            if (string.IsNullOrWhiteSpace(fluidX3DSourceRoot))
            {
                throw new ArgumentException("FluidX3D source directory is required.", nameof(fluidX3DSourceRoot));
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentException("Working directory is required.", nameof(workingDirectory));
            }

            string sourceRoot = Path.GetFullPath(fluidX3DSourceRoot.Trim());
            string workingRoot = Path.GetFullPath(workingDirectory.Trim());

            if (!Directory.Exists(sourceRoot))
            {
                throw new DirectoryNotFoundException("FluidX3D source directory does not exist: " + sourceRoot);
            }

            if (IsSubPath(sourceRoot, workingRoot))
            {
                throw new InvalidOperationException(
                    "Working directory cannot be inside the FluidX3D source directory. Choose a different working directory.");
            }

            Directory.CreateDirectory(workingRoot);

            // Keep FluidX3D engine files in the case folder so each case is isolated
            // from shared engine-state side effects.
            string caseDirectoryRoot = Path.Combine(workingRoot, "FluidX3D");
            string caseRoot = EnsureCaseEngineSourceDirectory(sourceRoot, caseDirectoryRoot);
            NormalizeBuildScripts(caseRoot);

            string setupPath = Path.Combine(caseRoot, "src", "setup.cpp");
            string definesPath = Path.Combine(caseRoot, "src", "defines.hpp");

            if (!File.Exists(setupPath))
            {
                throw new FileNotFoundException("FluidX3D setup.cpp not found at expected path.", setupPath);
            }

            if (!File.Exists(definesPath))
            {
                throw new FileNotFoundException("FluidX3D defines.hpp not found at expected path.", definesPath);
            }

            BackupOriginalIfMissing(setupPath);
            BackupOriginalIfMissing(definesPath);

            File.WriteAllText(setupPath, BuildSetupFile(settings));

            string definesText = File.ReadAllText(definesPath);
            definesText = SetDefine(definesText, "FP16S", true);
            definesText = SetDefine(definesText, "BENCHMARK", false);
            definesText = SetDefine(definesText, "FORCE_FIELD", true);
            definesText = SetDefine(definesText, "EQUILIBRIUM_BOUNDARIES", true);
            definesText = SetDefine(definesText, "SUBGRID", true);
            File.WriteAllText(definesPath, definesText);

            string exportDirectory = Path.Combine(caseRoot, "bin", "export");
            ResetDirectoryContents(exportDirectory);

            string scriptsDirectory = Path.Combine(workingRoot, "Scripts");
            Directory.CreateDirectory(scriptsDirectory);

            string caseExportDirectory = Path.Combine(caseDirectoryRoot, "VTK");
            string commandScriptPath = Path.Combine(scriptsDirectory, "run_fluidx3d.command");
            string batchScriptPath = Path.Combine(scriptsDirectory, "run_fluidx3d.bat");
            string windowsPlatformToolset = ResolveWindowsPlatformToolsetOverride(caseRoot);
            File.WriteAllText(commandScriptPath, BuildMacLaunchScript(caseRoot, caseExportDirectory));
            File.WriteAllText(batchScriptPath, BuildWindowsLaunchScript(caseRoot, caseExportDirectory, windowsPlatformToolset));
            MakeExecutable(commandScriptPath);

            string readmePath = Path.Combine(workingRoot, "FluidX3D_Eddy_Readme.txt");
            File.WriteAllText(readmePath, BuildReadme(settings));

            string launchScriptPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? batchScriptPath
                : commandScriptPath;

            return new FluidX3DAblPrepareResult
            {
                CaseRoot = caseRoot,
                SetupPath = setupPath,
                DefinesPath = definesPath,
                ExportDirectory = exportDirectory,
                ScriptsDirectory = scriptsDirectory,
                CommandScriptPath = commandScriptPath,
                BatchScriptPath = batchScriptPath,
                LaunchScriptPath = launchScriptPath,
                ReadmePath = readmePath
            };
        }

        public static string GetDefaultSourceDirectory()
        {
            return Path.Combine(DefaultDirectoriesAndPaths.Eddy3DInstallDir, "Engines", "FluidX3D");
        }

        public static string ResolvePinnedCommit()
        {
            string fromEnvironment = NormalizeCommitPrefixOrNull(
                Environment.GetEnvironmentVariable(CommitEnvironmentVariable));

            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            return NormalizeCommitPrefixOrNull(DefaultPinnedCommit);
        }

        public static IReadOnlyList<string> GetInstalledWindowsPlatformToolsets()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Array.Empty<string>();
            }

            try
            {
                return DiscoverInstalledWindowsPlatformToolsets()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(ParsePlatformToolsetVersion)
                    .ThenByDescending(t => t, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static string GetWindowsBuildToolsInstallerArguments()
        {
            return "--wait --passive --norestart --add " + WindowsCppWorkloadId
                + " --add " + WindowsV142ToolsetComponentId;
        }

        public static void EnsureSourceRepository(
            string sourceRoot,
            bool updateIfExists,
            out string statusMessage,
            string pinnedCommit = null)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                throw new ArgumentException("Source directory is required.", nameof(sourceRoot));
            }

            string fullSourceRoot = Path.GetFullPath(sourceRoot.Trim());
            string normalizedPinnedCommit = NormalizeCommitPrefixOrNull(pinnedCommit);
            string parent = Path.GetDirectoryName(fullSourceRoot);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            StringBuilder status = new StringBuilder();

            if (!Directory.Exists(fullSourceRoot) || DirectoryHelpers.IsEmpty(fullSourceRoot))
            {
                Directory.CreateDirectory(fullSourceRoot);
                if (!DirectoryHelpers.IsEmpty(fullSourceRoot))
                {
                    throw new InvalidOperationException("Source directory exists but is not empty: " + fullSourceRoot);
                }

                string cloneArgs = string.IsNullOrWhiteSpace(normalizedPinnedCommit)
                    ? "clone --depth 1 \"" + RepositoryUrl + "\" \"" + fullSourceRoot + "\""
                    : "clone \"" + RepositoryUrl + "\" \"" + fullSourceRoot + "\"";

                string cloneOutput = RunProcess("git", cloneArgs, null);
                status.AppendLine("Cloned FluidX3D repository.");
                if (!string.IsNullOrWhiteSpace(cloneOutput))
                {
                    status.AppendLine(cloneOutput.Trim());
                }
            }
            else if (!IsValidSourceDirectory(fullSourceRoot))
            {
                throw new InvalidOperationException(
                    "Existing source directory does not appear to be a valid FluidX3D source: " + fullSourceRoot);
            }
            else
            {
                status.AppendLine("Using existing FluidX3D source directory.");

                string gitDir = Path.Combine(fullSourceRoot, ".git");
                bool canPullLatest = updateIfExists
                    && Directory.Exists(gitDir)
                    && string.IsNullOrWhiteSpace(normalizedPinnedCommit);

                if (canPullLatest)
                {
                    string pullOutput = RunProcess("git", "-C \"" + fullSourceRoot + "\" pull --ff-only", null);
                    status.AppendLine("Updated existing FluidX3D source.");
                    if (!string.IsNullOrWhiteSpace(pullOutput))
                    {
                        status.AppendLine(pullOutput.Trim());
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedPinnedCommit))
            {
                status.AppendLine(CheckoutPinnedCommit(fullSourceRoot, normalizedPinnedCommit));
            }

            statusMessage = status.ToString().Trim();
        }

        private static string CheckoutPinnedCommit(string repositoryRoot, string commitPrefix)
        {
            string gitDir = Path.Combine(repositoryRoot, ".git");
            if (!Directory.Exists(gitDir) && !File.Exists(gitDir))
            {
                throw new InvalidOperationException(
                    "Pinned commit requested but repository metadata (.git) is missing: " + repositoryRoot);
            }

            string normalized = NormalizeCommitPrefixOrNull(commitPrefix);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "No pinned commit configured.";
            }

            if (TryReadGitHeadCommit(repositoryRoot, out string currentHead)
                && currentHead.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return "Pinned commit already checked out: " + currentHead + ".";
            }

            RunProcess("git", "-C \"" + repositoryRoot + "\" fetch --all --tags --prune", null);
            string checkoutOutput = RunProcess("git", "-C \"" + repositoryRoot + "\" checkout " + normalized, null);

            if (!TryReadGitHeadCommit(repositoryRoot, out string newHead)
                || !newHead.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Failed to checkout pinned commit " + normalized + " in " + repositoryRoot + ".");
            }

            if (string.IsNullOrWhiteSpace(checkoutOutput))
            {
                return "Checked out pinned commit: " + newHead + ".";
            }

            return "Checked out pinned commit: " + newHead + "." + Environment.NewLine + checkoutOutput.Trim();
        }

        private static bool TryReadGitHeadCommit(string repositoryRoot, out string commit)
        {
            commit = null;

            try
            {
                string output = RunProcess("git", "-C \"" + repositoryRoot + "\" rev-parse HEAD", null);
                string value = output
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, "^[0-9a-fA-F]{40}$"))
                {
                    return false;
                }

                commit = value.ToLowerInvariant();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeCommitPrefixOrNull(string commitRaw)
        {
            if (string.IsNullOrWhiteSpace(commitRaw))
            {
                return null;
            }

            string commit = commitRaw.Trim();
            if (!Regex.IsMatch(commit, "^[0-9a-fA-F]{7,40}$"))
            {
                throw new InvalidOperationException(
                    CommitEnvironmentVariable + " must be a 7-40 character hexadecimal SHA. Value: '" + commitRaw + "'.");
            }

            return commit.ToLowerInvariant();
        }

        private static void ValidateSettings(FluidX3DAblSettings settings)
        {
            if (settings.MemoryMb < 256)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.MemoryMb), "VRAM budget must be >= 256 MB.");
            }

            if (settings.Uref <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.Uref), "Reference velocity must be > 0 m/s.");
            }

            if (settings.Zref <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.Zref), "Reference height must be > 0 m.");
            }

            if (settings.Z0 <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.Z0), "Roughness length must be > 0 m.");
            }

            if (settings.Z0 >= settings.Zref)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.Z0), "Roughness length must be smaller than reference height.");
            }

            if (settings.SimSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.SimSeconds), "Simulation time must be > 0 s.");
            }

            if (settings.ExportIntervalSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.ExportIntervalSeconds), "Export interval must be > 0 s.");
            }

            if (settings.DomainLx <= 0.0 || settings.DomainLy <= 0.0 || settings.DomainLz <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.DomainLx), "Domain lengths must be > 0.");
            }
        }

        private static void BackupOriginalIfMissing(string path)
        {
            string backupPath = path + ".eddy.bak";
            if (!File.Exists(backupPath))
            {
                File.Copy(path, backupPath);
            }
        }

        private static void ResetDirectoryContents(string directory)
        {
            EnsureDirectoryPathExists(directory);

            foreach (string file in Directory.GetFiles(directory))
            {
                File.Delete(file);
            }

            foreach (string subDir in Directory.GetDirectories(directory))
            {
                Directory.Delete(subDir, true);
            }
        }

        private static void EnsureDirectoryPathExists(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory path is required.", nameof(directory));
            }

            string full = Path.GetFullPath(directory);
            if (File.Exists(full) && !Directory.Exists(full))
            {
                File.Delete(full);
            }
            else if (Directory.Exists(full))
            {
                FileAttributes attrs = File.GetAttributes(full);
                if ((attrs & FileAttributes.ReparsePoint) != 0)
                {
                    Directory.Delete(full);
                }
            }

            Directory.CreateDirectory(full);
        }

        private static string EnsureCaseEngineSourceDirectory(string sourceRoot, string caseDirectoryRoot)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                throw new ArgumentException("Source directory is required.", nameof(sourceRoot));
            }

            if (string.IsNullOrWhiteSpace(caseDirectoryRoot))
            {
                throw new ArgumentException("Case directory is required.", nameof(caseDirectoryRoot));
            }

            string sourceFull = Path.GetFullPath(sourceRoot);
            string caseRoot = Path.Combine(Path.GetFullPath(caseDirectoryRoot), "Engine");

            if (IsValidSourceDirectory(caseRoot))
            {
                return caseRoot;
            }

            if (File.Exists(caseRoot) && !Directory.Exists(caseRoot))
            {
                File.Delete(caseRoot);
            }
            else if (Directory.Exists(caseRoot))
            {
                Directory.Delete(caseRoot, true);
            }

            CopyDirectoryRecursive(sourceFull, caseRoot);
            return caseRoot;
        }

        private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destination = Path.Combine(targetDir, fileName);
                File.Copy(file, destination, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string name = Path.GetFileName(subDir);
                if (ShouldSkipCaseSourceDirectory(name))
                {
                    continue;
                }

                string destination = Path.Combine(targetDir, name);
                CopyDirectoryRecursive(subDir, destination);
            }
        }

        private static void NormalizeBuildScripts(string caseRoot)
        {
            if (string.IsNullOrWhiteSpace(caseRoot) || !Directory.Exists(caseRoot))
            {
                return;
            }

            NormalizeMakeScript(Path.Combine(caseRoot, "make.sh"));
            NormalizeMakefile(Path.Combine(caseRoot, "Makefile"));
            NormalizeWindowsProject(Path.Combine(caseRoot, "FluidX3D.vcxproj"));
        }

        private static void NormalizeMakeScript(string makeScriptPath)
        {
            if (string.IsNullOrWhiteSpace(makeScriptPath) || !File.Exists(makeScriptPath))
            {
                return;
            }

            string original = File.ReadAllText(makeScriptPath);
            string updated = original;

            const string echoAndExecuteLine = "echo_and_execute() { echo \"$@\"; \"$@\"; }";
            const string cpuDetectionBlock =
@"echo_and_execute() { echo ""$@""; ""$@""; }
detect_cpu_cores() {
	if command -v nproc >/dev/null 2>&1; then
		nproc
	elif command -v sysctl >/dev/null 2>&1; then
		sysctl -n hw.logicalcpu 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 1
	else
		echo 1
	fi
}
CPU_CORES=""$(detect_cpu_cores)""";

            if (updated.Contains(echoAndExecuteLine) && !updated.Contains("detect_cpu_cores()", StringComparison.Ordinal))
            {
                updated = updated.Replace(echoAndExecuteLine, cpuDetectionBlock);
            }

            if (updated.Contains("CPU_CORES=\"$(detect_cpu_cores)\"", StringComparison.Ordinal))
            {
                updated = updated.Replace("$(nproc)", "${CPU_CORES}");
            }

            if (!updated.Contains("-Wno-deprecated-declarations", StringComparison.Ordinal))
            {
                updated = updated.Replace(
                    "macOS    ) echo_and_execute g++ src/*.cpp -o bin/FluidX3D -std=c++17 -pthread -O -Wno-comment -I./src/OpenCL/include -framework OpenCL",
                    "macOS    ) echo_and_execute g++ src/*.cpp -o bin/FluidX3D -std=c++17 -pthread -O -Wno-comment -Wno-deprecated-declarations -I./src/OpenCL/include -framework OpenCL");
            }

            if (!string.Equals(updated, original, StringComparison.Ordinal))
            {
                File.WriteAllText(makeScriptPath, updated);
            }
        }

        private static void NormalizeMakefile(string makefilePath)
        {
            if (string.IsNullOrWhiteSpace(makefilePath) || !File.Exists(makefilePath))
            {
                return;
            }

            string original = File.ReadAllText(makefilePath);
            string updated = original;

            if (updated.Contains("MAKEFLAGS = -j$(nproc)", StringComparison.Ordinal))
            {
                updated = updated.Replace(
                    "MAKEFLAGS = -j$(nproc)",
                    "MAKEFLAGS = -j$(shell sh -c 'command -v nproc >/dev/null 2>&1 && nproc || sysctl -n hw.logicalcpu 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 1')");
            }

            if (!updated.Contains("macOS: CFLAGS += -Wno-deprecated-declarations", StringComparison.Ordinal))
            {
                updated = updated.Replace(
                    "CFLAGS = -std=c++17 -pthread -O -Wno-comment",
                    "CFLAGS = -std=c++17 -pthread -O -Wno-comment\nmacOS: CFLAGS += -Wno-deprecated-declarations");
            }

            if (!string.Equals(updated, original, StringComparison.Ordinal))
            {
                File.WriteAllText(makefilePath, updated);
            }
        }

        private static void NormalizeWindowsProject(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                return;
            }

            string original = File.ReadAllText(projectPath);
            string updated = original;

            updated = Regex.Replace(
                updated,
                "<DisableSpecificWarnings>(?<value>[^<]*)</DisableSpecificWarnings>",
                match =>
                {
                    string value = match.Groups["value"].Value;
                    if (Regex.IsMatch(value, @"(^|;)4996($|;)", RegexOptions.CultureInvariant))
                    {
                        return match.Value;
                    }

                    string normalized = string.IsNullOrWhiteSpace(value)
                        ? "4996"
                        : value.Trim();

                    if (normalized.StartsWith("%(", StringComparison.Ordinal))
                    {
                        normalized = "4996;" + normalized;
                    }
                    else
                    {
                        normalized = normalized.TrimEnd(';');
                        normalized = normalized + ";4996";
                    }

                    return "<DisableSpecificWarnings>" + normalized + "</DisableSpecificWarnings>";
                },
                RegexOptions.CultureInvariant);

            if (!string.Equals(updated, original, StringComparison.Ordinal))
            {
                File.WriteAllText(projectPath, updated);
            }
        }

        private static bool ShouldSkipCaseSourceDirectory(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                return false;
            }

            return directoryName.Equals(".git", StringComparison.OrdinalIgnoreCase)
                || directoryName.Equals(".vs", StringComparison.OrdinalIgnoreCase)
                || directoryName.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || directoryName.Equals("temp", StringComparison.OrdinalIgnoreCase);
        }

        private static void NormalizeHorizontalFlowDirection(
            double x,
            double y,
            out double normalizedX,
            out double normalizedY)
        {
            double length = Math.Sqrt((x * x) + (y * y));
            if (length <= 1.0e-12)
            {
                normalizedX = 0.0;
                normalizedY = 1.0;
                return;
            }

            normalizedX = x / length;
            normalizedY = y / length;
        }

        public static bool IsValidSourceDirectory(string sourceRoot)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                return false;
            }

            string full = Path.GetFullPath(sourceRoot.Trim());
            return File.Exists(Path.Combine(full, "src", "setup.cpp"))
                && File.Exists(Path.Combine(full, "src", "defines.hpp"))
                && (File.Exists(Path.Combine(full, "make.sh")) || File.Exists(Path.Combine(full, "FluidX3D.sln")));
        }

        private static bool IsSubPath(string parentPath, string candidatePath)
        {
            string parent = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return candidate.StartsWith(parent, comparison);
        }

        private static string SetDefine(string text, string defineName, bool enabled)
        {
            bool replaced = false;
            string pattern = @"^(?<indent>\s*)(?<comment>//\s*)?#define\s+" + Regex.Escape(defineName) + @"(?<rest>\b.*)$";
            string replacement = enabled
                ? "${indent}#define " + defineName + "${rest}"
                : "${indent}//#define " + defineName + "${rest}";

            string output = Regex.Replace(
                text,
                pattern,
                match =>
                {
                    replaced = true;
                    return match.Result(replacement);
                },
                RegexOptions.Multiline);

            if (!replaced)
            {
                string line = enabled
                    ? "#define " + defineName + " // added by Eddy3D"
                    : "//#define " + defineName + " // added by Eddy3D";

                if (!output.EndsWith("\n", StringComparison.Ordinal) && !output.EndsWith("\r", StringComparison.Ordinal))
                {
                    output += Environment.NewLine;
                }

                output += line + Environment.NewLine;
            }

            return output;
        }

        private static string BuildSetupFile(FluidX3DAblSettings settings)
        {
            NormalizeHorizontalFlowDirection(
                settings.FlowDirectionX,
                settings.FlowDirectionY,
                out double flowDirX,
                out double flowDirY);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#include \"setup.hpp\"");
            sb.AppendLine();
            sb.AppendLine("#ifndef BENCHMARK");
            sb.AppendLine();
            sb.AppendLine("void main_setup() { // FluidX3D ABL setup generated by Eddy3D");
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\t// Physical parameters (SI)");
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\tconst float si_u_ref   = " + ToCppFloatLiteral(settings.Uref) + ";     // reference wind speed [m/s] at z_ref");
            sb.AppendLine("\tconst float si_z_ref   = " + ToCppFloatLiteral(settings.Zref) + ";    // reference height [m]");
            sb.AppendLine("\tconst float si_z0      = " + ToCppFloatLiteral(settings.Z0) + ";     // aerodynamic roughness length [m]");
            sb.AppendLine("\tconst float si_rho     = 1.225f;   // air density [kg/m^3]");
            sb.AppendLine("\tconst float si_nu      = 1.48e-5f; // kinematic viscosity [m^2/s]");
            sb.AppendLine();
            sb.AppendLine("\t// Domain size in SI (x=crosswind, y=streamwise, z=vertical)");
            sb.AppendLine("\tconst float si_Lx = " + ToCppFloatLiteral(settings.DomainLx) + ";");
            sb.AppendLine("\tconst float si_Ly = " + ToCppFloatLiteral(settings.DomainLy) + ";");
            sb.AppendLine("\tconst float si_Lz = " + ToCppFloatLiteral(settings.DomainLz) + ";");
            sb.AppendLine();
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\t// LBM resolution and unit conversion");
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\tconst uint memory = " + settings.MemoryMb.ToString(CultureInfo.InvariantCulture) + "u; // VRAM budget [MB]");
            sb.AppendLine("\tconst float lbm_u = 0.06f; // keep < 0.15 for stability");
            sb.AppendLine();
            sb.AppendLine("\tconst uint3 lbm_N = resolution(float3(si_Lx, si_Ly, si_Lz), memory);");
            sb.AppendLine("\tconst float lbm_length = (float)lbm_N.y; // map si_Ly to Ny lattice cells");
            sb.AppendLine("\tunits.set_m_kg_s(lbm_length, lbm_u, 1.0f, si_Ly, si_u_ref, si_rho);");
            sb.AppendLine();
            sb.AppendLine("\tconst float lbm_nu = units.nu(si_nu);");
            sb.AppendLine("\tconst float Re = si_u_ref * si_Ly / si_nu;");
            sb.AppendLine("\tprint_info(\"Domain: \" + to_string(lbm_N.x) + \" x \" + to_string(lbm_N.y) + \" x \" + to_string(lbm_N.z));");
            sb.AppendLine("\tprint_info(\"Re = \" + to_string(to_uint(Re)));");
            sb.AppendLine("\tprint_info(\"nu_lbm = \" + to_string(lbm_nu, 6u));");
            sb.AppendLine("\tprint_info(\"Building STL count = " + settings.BuildingStlFiles.Count.ToString(CultureInfo.InvariantCulture) + "\");");
            sb.AppendLine();
            sb.AppendLine("\tLBM lbm(lbm_N, lbm_nu);");
            sb.AppendLine();
            sb.AppendLine("\tconst float s = lbm_length / si_Ly; // SI-to-LBM scale factor");
            sb.AppendLine();
            sb.AppendLine("\t// ABL profile parameters in LBM units");
            sb.AppendLine("\tconst float lbm_z0    = si_z0 * s;");
            sb.AppendLine("\tconst float kappa      = 0.41f; // von Karman constant");
            sb.AppendLine("\tconst float u_star     = si_u_ref * kappa / log(si_z_ref / si_z0); // friction velocity");
            sb.AppendLine("\tconst float lbm_u_star = u_star * (lbm_u / si_u_ref); // scale to LBM");
            sb.AppendLine("\tconst float flow_dir_x = " + ToCppFloatLiteral(flowDirX) + "; // horizontal ABL flow direction x");
            sb.AppendLine("\tconst float flow_dir_y = " + ToCppFloatLiteral(flowDirY) + "; // horizontal ABL flow direction y");
            sb.AppendLine();
            sb.AppendLine("\tconst uint Nx = lbm.get_Nx(), Ny = lbm.get_Ny(), Nz = lbm.get_Nz();");
            sb.AppendLine();

            if (settings.BuildingStlFiles.Count > 0)
            {
                sb.AppendLine("\t// ============================================================");
                sb.AppendLine("\t// Voxelize building meshes from stl/*.stl");
                sb.AppendLine("\t// ============================================================");
                for (int i = 0; i < settings.BuildingStlFiles.Count; i++)
                {
                    string fileName = settings.BuildingStlFiles[i].Replace("\\", "/");
                    sb.AppendLine("\t{");
                    sb.AppendLine("\t\tMesh* building = read_stl(get_exe_path()+\"../stl/" + EscapeForCpp(fileName) + "\", s, float3x3(1.0f), float3(0.0f));");
                    sb.AppendLine("\t\tlbm.voxelize_mesh_on_device(building, TYPE_S | TYPE_X);");
                    sb.AppendLine("\t\tdelete building;");
                    sb.AppendLine("\t}");
                }
            }
            else
            {
                sb.AppendLine("\t// ============================================================");
                sb.AppendLine("\t// Fallback demo buildings when no STL files are provided");
                sb.AppendLine("\t// ============================================================");
                sb.AppendLine("\tconst float3 bA = float3(0.43f*(float)Nx, 0.40f*(float)Ny, 0.20f*(float)Nz);");
                sb.AppendLine("\tconst float3 sA = float3(0.10f*(float)Nx, 0.10f*(float)Ny, 0.40f*(float)Nz);");
                sb.AppendLine("\tconst float3 bB = float3(0.57f*(float)Nx, 0.60f*(float)Ny, 0.125f*(float)Nz);");
                sb.AppendLine("\tconst float3 sB = float3(0.07f*(float)Nx, 0.10f*(float)Ny, 0.25f*(float)Nz);");
                sb.AppendLine("\tparallel_for(lbm.get_N(), [&](ulong n) {");
                sb.AppendLine("\t\tuint x = 0u, y = 0u, z = 0u;");
                sb.AppendLine("\t\tlbm.coordinates(n, x, y, z);");
                sb.AppendLine("\t\tif(cuboid(x, y, z, bA, sA) || cuboid(x, y, z, bB, sB)) lbm.flags[n] = TYPE_S | TYPE_X;");
                sb.AppendLine("\t});");
            }
            sb.AppendLine();
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\t// Initialize density/velocity field and boundary conditions");
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\tparallel_for(lbm.get_N(), [&](ulong n) {");
            sb.AppendLine("\t\tuint x = 0u, y = 0u, z = 0u;");
            sb.AppendLine("\t\tlbm.coordinates(n, x, y, z);");
            sb.AppendLine();
            sb.AppendLine("\t\t// Log-law ABL velocity profile: u(z) = (u*/kappa) * ln(z/z0)");
            sb.AppendLine("\t\tconst float z_phys = ((float)z + 0.5f); // cell-centered height in LBM units");
            sb.AppendLine("\t\tfloat u_abl = 0.0f;");
            sb.AppendLine("\t\tif(z_phys > lbm_z0) {");
            sb.AppendLine("\t\t\tu_abl = (lbm_u_star / kappa) * log(z_phys / lbm_z0);");
            sb.AppendLine("\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\tconst bool isSolid = (lbm.flags[n] & TYPE_S) != 0u;");
            sb.AppendLine("\t\tlbm.rho[n] = 1.0f;");
            sb.AppendLine("\t\tlbm.u.x[n] = 0.0f;");
            sb.AppendLine("\t\tlbm.u.y[n] = 0.0f;");
            sb.AppendLine("\t\tlbm.u.z[n] = 0.0f;");
            sb.AppendLine();
            sb.AppendLine("\t\tif(z == 0u) {");
            sb.AppendLine("\t\t\t// Ground plane (no-slip)");
            sb.AppendLine("\t\t\tlbm.flags[n] = isSolid ? lbm.flags[n] : TYPE_S;");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\telse if(isSolid) {");
            sb.AppendLine("\t\t\t// Preserve voxelized solid cells (TYPE_S | TYPE_X).");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\telse {");
            sb.AppendLine("\t\t\tif(x == 0u || x == Nx-1u || y == 0u || y == Ny-1u || z == Nz-1u) {");
            sb.AppendLine("\t\t\t\t// Equilibrium boundaries on domain faces (inlet, outlet, sides, top)");
            sb.AppendLine("\t\t\t\tlbm.flags[n] = TYPE_E;");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\telse {");
            sb.AppendLine("\t\t\t\tlbm.flags[n] = 0u; // interior fluid");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t\t// Initialize velocity along Eddy ABL flow direction.");
            sb.AppendLine("\t\t\tlbm.u.x[n] = flow_dir_x * u_abl;");
            sb.AppendLine("\t\t\tlbm.u.y[n] = flow_dir_y * u_abl;");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t});");
            sb.AppendLine();
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\t// Simulation parameters");
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\tconst float si_T   = " + ToCppFloatLiteral(settings.SimSeconds) + ";");
            sb.AppendLine("\tconst ulong lbm_T  = units.t(si_T);");
            sb.AppendLine("\tconst ulong export_interval = units.t(" + ToCppFloatLiteral(settings.ExportIntervalSeconds) + ");");
            sb.AppendLine();
            sb.AppendLine("\tprint_info(\"Total timesteps: \" + to_string(lbm_T));");
            sb.AppendLine("\tprint_info(\"Export interval: \" + to_string(export_interval) + \" steps\");");
            sb.AppendLine();
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\t// Run simulation with periodic VTK export");
            sb.AppendLine("\t// ============================================================");
            sb.AppendLine("\tlbm.run(0u, lbm_T);");
            sb.AppendLine("\tlbm.flags.write_device_to_vtk();");
            sb.AppendLine();
            sb.AppendLine("\twhile(lbm.get_t() <= lbm_T) {");
            sb.AppendLine("\t\tlbm.u.write_device_to_vtk();");
            sb.AppendLine();
            sb.AppendLine("\t\tconst float3 F_bldg = lbm.object_force(TYPE_S | TYPE_X);");
            sb.AppendLine("\t\tprint_info(\"t=\" + to_string(units.si_t(lbm.get_t()), 1u) + \"s\"");
            sb.AppendLine("\t\t\t+ \"  F_buildings=(\" + to_string(units.si_F(F_bldg.x), 2u) + \",\" + to_string(units.si_F(F_bldg.y), 2u) + \",\" + to_string(units.si_F(F_bldg.z), 2u) + \") N\"");
            sb.AppendLine("\t\t);");
            sb.AppendLine();
            sb.AppendLine("\t\tlbm.run(export_interval, lbm_T);");
            sb.AppendLine("\t}");
            sb.AppendLine();
            sb.AppendLine("\tlbm.u.write_device_to_vtk();");
            sb.AppendLine("\tlbm.rho.write_device_to_vtk();");
            sb.AppendLine("\tprint_info(\"Simulation complete. VTK files in bin/export/. Eddy3D launch scripts redirect this to the case VTK folder.\");");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("#endif // BENCHMARK");
            return sb.ToString();
        }

        private static string BuildMacLaunchScript(string sourceRoot, string caseExportDirectory)
        {
            string sourceRootEscaped = EscapeForBashDoubleQuotedString(sourceRoot);
            string caseExportEscaped = EscapeForBashDoubleQuotedString(caseExportDirectory);
            return
@"#!/bin/bash
SOURCE_DIR=""__SOURCE_DIR__""
SOURCE_EXPORT_DIR=""$SOURCE_DIR/bin/export""
CASE_EXPORT_DIR=""__CASE_EXPORT_DIR__""
cd ""$SOURCE_DIR"" || exit 1
mkdir -p ""$CASE_EXPORT_DIR""

redirect_exports() {
  local export_parent
  local existing_target
  export_parent=""$(dirname ""$SOURCE_EXPORT_DIR"")""
  mkdir -p ""$export_parent""

  if [ -L ""$SOURCE_EXPORT_DIR"" ]; then
    existing_target=""$(readlink ""$SOURCE_EXPORT_DIR"" 2>/dev/null || true)""
    if [ ""$existing_target"" = ""$CASE_EXPORT_DIR"" ]; then
      return 0
    fi
  fi

  if [ -e ""$SOURCE_EXPORT_DIR"" ] || [ -L ""$SOURCE_EXPORT_DIR"" ]; then
    rm -rf ""$SOURCE_EXPORT_DIR"" >/dev/null 2>&1 || {
      if [ -L ""$SOURCE_EXPORT_DIR"" ]; then
        existing_target=""$(readlink ""$SOURCE_EXPORT_DIR"" 2>/dev/null || true)""
        if [ ""$existing_target"" = ""$CASE_EXPORT_DIR"" ]; then
          return 0
        fi
      fi
      return 1
    }
  fi

  ln -s ""$CASE_EXPORT_DIR"" ""$SOURCE_EXPORT_DIR"" >/dev/null 2>&1 || {
    if [ -L ""$SOURCE_EXPORT_DIR"" ]; then
      existing_target=""$(readlink ""$SOURCE_EXPORT_DIR"" 2>/dev/null || true)""
      [ ""$existing_target"" = ""$CASE_EXPORT_DIR"" ] && return 0
    fi
    return 1
  }

  return 0
}

if redirect_exports; then
  echo ""VTK outputs redirected to $CASE_EXPORT_DIR.""
else
  echo ""Warning: failed to redirect $SOURCE_EXPORT_DIR to case folder.""
  mkdir -p ""$SOURCE_EXPORT_DIR""
fi

chmod +x make.sh
./make.sh
STATUS=$?

if [ -d ""$SOURCE_EXPORT_DIR"" ] && [ ! -L ""$SOURCE_EXPORT_DIR"" ]; then
  cp -f ""$SOURCE_EXPORT_DIR""/*.vtk ""$CASE_EXPORT_DIR""/ 2>/dev/null || true
  cp -f ""$SOURCE_EXPORT_DIR""/eddy_probe_transform.txt ""$CASE_EXPORT_DIR""/ 2>/dev/null || true
  echo ""VTK outputs mirrored to $CASE_EXPORT_DIR.""
else
  echo ""VTK outputs available in $CASE_EXPORT_DIR.""
fi

echo
if [ $STATUS -eq 0 ]; then
  echo ""FluidX3D completed successfully.""
else
  echo ""FluidX3D failed with exit code $STATUS.""
fi
read -r -p ""Press Enter to close...""
exit $STATUS
"
            .Replace("__SOURCE_DIR__", sourceRootEscaped)
            .Replace("__CASE_EXPORT_DIR__", caseExportEscaped);
        }

        private static string BuildWindowsLaunchScript(string sourceRoot, string caseExportDirectory, string platformToolsetOverride)
        {
            string sourceRootEscaped = EscapeForBatchQuotedValue(sourceRoot);
            string caseExportEscaped = EscapeForBatchQuotedValue(caseExportDirectory);
            string safeToolset = SanitizePlatformToolsetForBatch(platformToolsetOverride);
            string buildToolsUrlEscaped = EscapeForBatchQuotedValue(WindowsBuildToolsDownloadUrl);
            string buildToolsBootstrapperEscaped = EscapeForBatchQuotedValue(WindowsBuildToolsBootstrapperFileName);
            string buildToolsArgsEscaped = EscapeForBatchQuotedValue(GetWindowsBuildToolsInstallerArguments());
            string cppWorkloadIdEscaped = EscapeForBatchQuotedValue(WindowsCppWorkloadId);
            string v142ComponentIdEscaped = EscapeForBatchQuotedValue(WindowsV142ToolsetComponentId);
            return
@"@echo off
setlocal
set ""SOURCE_DIR=__SOURCE_DIR__""
set ""SOURCE_EXPORT_DIR=%SOURCE_DIR%\bin\export""
set ""SOURCE_BIN_DIR=%SOURCE_DIR%\bin""
set ""CASE_EXPORT_DIR=__CASE_EXPORT_DIR__""
cd /d ""%SOURCE_DIR%"" || exit /b 1
if not exist ""%CASE_EXPORT_DIR%"" mkdir ""%CASE_EXPORT_DIR%"" >nul 2>nul
if not exist ""%SOURCE_BIN_DIR%"" mkdir ""%SOURCE_BIN_DIR%"" >nul 2>nul

set ""EXPORT_REDIRECTED=0""
if exist ""%SOURCE_EXPORT_DIR%"" (
  rmdir /s /q ""%SOURCE_EXPORT_DIR%"" >nul 2>nul
)
if exist ""%SOURCE_EXPORT_DIR%"" (
  del /f /q ""%SOURCE_EXPORT_DIR%"" >nul 2>nul
)
mklink /J ""%SOURCE_EXPORT_DIR%"" ""%CASE_EXPORT_DIR%"" >nul 2>nul
if errorlevel 1 (
  echo Warning: failed to redirect %SOURCE_EXPORT_DIR% to case folder.
  if not exist ""%SOURCE_EXPORT_DIR%"" mkdir ""%SOURCE_EXPORT_DIR%"" >nul 2>nul
) else (
  set ""EXPORT_REDIRECTED=1""
  echo VTK outputs redirected to %CASE_EXPORT_DIR%.
)

set ""VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe""
set ""MSBUILD=""
set ""PLATFORM_TOOLSET=__PLATFORM_TOOLSET__""
set ""VS_BUILD_TOOLS_URL=__VS_BUILD_TOOLS_URL__""
set ""VS_BUILD_TOOLS_BOOTSTRAPPER=%TEMP%\__VS_BUILD_TOOLS_BOOTSTRAPPER__""
set ""VS_BUILD_TOOLS_ARGS=__VS_BUILD_TOOLS_ARGS__""
set ""VS_CPP_WORKLOAD=__VS_CPP_WORKLOAD__""
set ""VS_V142_COMPONENT=__VS_V142_COMPONENT__""
if exist ""%VSWHERE%"" (
  for /f ""usebackq tokens=*"" %%i in (`""%VSWHERE%"" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    set ""MSBUILD=%%i""
  )
)

if not defined MSBUILD (
  echo Could not find MSBuild automatically.
  echo Downloading Visual Studio Build Tools bootstrapper...
  powershell -NoProfile -ExecutionPolicy Bypass -Command ""[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%VS_BUILD_TOOLS_URL%' -OutFile '%VS_BUILD_TOOLS_BOOTSTRAPPER%'""
  if errorlevel 1 (
    echo Failed to download Visual Studio Build Tools bootstrapper.
    echo Manual download: %VS_BUILD_TOOLS_URL%
    pause
    exit /b 1
  )
  echo Launching Visual Studio Build Tools installer...
  echo   %VS_BUILD_TOOLS_BOOTSTRAPPER% %VS_BUILD_TOOLS_ARGS%
  ""%VS_BUILD_TOOLS_BOOTSTRAPPER%"" %VS_BUILD_TOOLS_ARGS%
  set ""VS_INSTALL_EXIT=%ERRORLEVEL%""
  if not ""%VS_INSTALL_EXIT%""==""0"" if not ""%VS_INSTALL_EXIT%""==""3010"" (
    echo Visual Studio Build Tools installer failed with exit code %VS_INSTALL_EXIT%.
    pause
    exit /b %VS_INSTALL_EXIT%
  )
  if exist ""%VSWHERE%"" (
    for /f ""usebackq tokens=*"" %%i in (`""%VSWHERE%"" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
      set ""MSBUILD=%%i""
    )
  )
)

if not defined MSBUILD (
  echo Could not find MSBuild after Visual Studio Build Tools installation.
  echo Opening FluidX3D.sln. Build Release^|x64 and run Local Windows Debugger.
  start """" ""FluidX3D.sln""
  exit /b 0
)

set ""MSBUILD_ARGS=/m /p:Configuration=Release /p:Platform=x64""
if defined PLATFORM_TOOLSET (
  echo Using PlatformToolset=%PLATFORM_TOOLSET%
  set ""MSBUILD_ARGS=%MSBUILD_ARGS% /p:PlatformToolset=%PLATFORM_TOOLSET%""
)

echo Building FluidX3D (Release x64)...
""%MSBUILD%"" ""FluidX3D.sln"" %MSBUILD_ARGS%
if errorlevel 1 (
  echo Build failed.
  echo.
  echo If you see MSB8020 or missing C++ targets, install Visual Studio Build Tools:
  echo   %VS_BUILD_TOOLS_URL%
  echo Required installer selections:
  echo   Workload: %VS_CPP_WORKLOAD%
  echo   Component: %VS_V142_COMPONENT%
  pause
  exit /b 1
)

set ""FLUIDX3D_EXE=""
if exist ""bin\FluidX3D.exe"" set ""FLUIDX3D_EXE=bin\FluidX3D.exe""
if not defined FLUIDX3D_EXE if exist ""bin\Release\FluidX3D.exe"" set ""FLUIDX3D_EXE=bin\Release\FluidX3D.exe""
if not defined FLUIDX3D_EXE if exist ""bin\x64\Release\FluidX3D.exe"" set ""FLUIDX3D_EXE=bin\x64\Release\FluidX3D.exe""

if not defined FLUIDX3D_EXE (
  echo Build succeeded but FluidX3D.exe was not found in expected locations.
  echo Checked: bin\FluidX3D.exe, bin\Release\FluidX3D.exe, bin\x64\Release\FluidX3D.exe
  pause
  exit /b 1
)

echo Running FluidX3D from %FLUIDX3D_EXE%...
""%FLUIDX3D_EXE%""
set ""FLUIDX3D_EXIT=%ERRORLEVEL%""

if not ""%EXPORT_REDIRECTED%""==""1"" (
  if exist ""%SOURCE_EXPORT_DIR%"" (
    robocopy ""%SOURCE_EXPORT_DIR%"" ""%CASE_EXPORT_DIR%"" *.vtk eddy_probe_transform.txt /R:1 /W:1 /NFL /NDL /NJH /NJS /NP >nul
    set ""ROBO_EXIT=%ERRORLEVEL%""
    if %ROBO_EXIT% GEQ 8 (
      echo Warning: failed to mirror VTK outputs to %CASE_EXPORT_DIR%.
    ) else (
      echo VTK outputs mirrored to %CASE_EXPORT_DIR%.
    )
  )
)

if not ""%FLUIDX3D_EXIT%""==""0"" (
  echo FluidX3D failed with exit code %FLUIDX3D_EXIT%.
  pause
  exit /b %FLUIDX3D_EXIT%
)

echo FluidX3D completed successfully.
echo VTK outputs available in %CASE_EXPORT_DIR%.
pause
exit /b 0
"
            .Replace("__SOURCE_DIR__", sourceRootEscaped)
            .Replace("__CASE_EXPORT_DIR__", caseExportEscaped)
            .Replace("__PLATFORM_TOOLSET__", safeToolset)
            .Replace("__VS_BUILD_TOOLS_URL__", buildToolsUrlEscaped)
            .Replace("__VS_BUILD_TOOLS_BOOTSTRAPPER__", buildToolsBootstrapperEscaped)
            .Replace("__VS_BUILD_TOOLS_ARGS__", buildToolsArgsEscaped)
            .Replace("__VS_CPP_WORKLOAD__", cppWorkloadIdEscaped)
            .Replace("__VS_V142_COMPONENT__", v142ComponentIdEscaped);
        }

        private static string ResolveWindowsPlatformToolsetOverride(string caseRoot)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            try
            {
                string vcxprojPath = Path.Combine(caseRoot, "FluidX3D.vcxproj");
                string requested = ReadRequestedPlatformToolset(vcxprojPath);
                List<string> available = DiscoverInstalledWindowsPlatformToolsets();
                if (available.Count == 0)
                {
                    return requested;
                }

                if (!string.IsNullOrWhiteSpace(requested)
                    && available.Any(t => string.Equals(t, requested, StringComparison.OrdinalIgnoreCase)))
                {
                    return requested;
                }

                return available
                    .OrderByDescending(ParsePlatformToolsetVersion)
                    .ThenByDescending(t => t, StringComparer.OrdinalIgnoreCase)
                    .First();
            }
            catch
            {
                return null;
            }
        }

        private static string ReadRequestedPlatformToolset(string vcxprojPath)
        {
            if (string.IsNullOrWhiteSpace(vcxprojPath) || !File.Exists(vcxprojPath))
            {
                return null;
            }

            string xml = File.ReadAllText(vcxprojPath);
            Match match = Regex.Match(
                xml,
                "<PlatformToolset>(?<value>[^<]+)</PlatformToolset>",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            string value = match.Groups["value"].Value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static List<string> DiscoverInstalledWindowsPlatformToolsets()
        {
            HashSet<string> toolsets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string installPath = TryResolveLatestVisualStudioInstallPath();
            if (!string.IsNullOrWhiteSpace(installPath))
            {
                AddToolsetsFromVisualStudioInstall(installPath, toolsets);
            }

            if (toolsets.Count == 0)
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string root = Path.Combine(programFiles, "Microsoft Visual Studio");
                if (Directory.Exists(root))
                {
                    foreach (string yearDir in Directory.GetDirectories(root))
                    {
                        foreach (string editionDir in Directory.GetDirectories(yearDir))
                        {
                            AddToolsetsFromVisualStudioInstall(editionDir, toolsets);
                        }
                    }
                }
            }

            return toolsets.ToList();
        }

        private static string TryResolveLatestVisualStudioInstallPath()
        {
            try
            {
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string vswhere = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
                if (!File.Exists(vswhere))
                {
                    return null;
                }

                string output = RunProcess(
                    vswhere,
                    "-latest -products * -property installationPath",
                    null);

                string path = output
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .FirstOrDefault();

                return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        private static void AddToolsetsFromVisualStudioInstall(string installPath, ISet<string> destination)
        {
            if (destination == null || string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath))
            {
                return;
            }

            string vcRoot = Path.Combine(installPath, "MSBuild", "Microsoft", "VC");
            if (!Directory.Exists(vcRoot))
            {
                return;
            }

            foreach (string vcVersionDir in Directory.GetDirectories(vcRoot, "v*"))
            {
                string[] platformRoots = new[]
                {
                    Path.Combine(vcVersionDir, "Platforms", "x64", "PlatformToolsets"),
                    Path.Combine(vcVersionDir, "Platforms", "Win32", "PlatformToolsets")
                };

                foreach (string platformRoot in platformRoots)
                {
                    if (!Directory.Exists(platformRoot))
                    {
                        continue;
                    }

                    foreach (string toolsetDir in Directory.GetDirectories(platformRoot))
                    {
                        string name = Path.GetFileName(toolsetDir);
                        if (!string.IsNullOrWhiteSpace(name) && name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                        {
                            destination.Add(name);
                        }
                    }
                }
            }
        }

        private static int ParsePlatformToolsetVersion(string toolset)
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

        private static string SanitizePlatformToolsetForBatch(string toolset)
        {
            if (string.IsNullOrWhiteSpace(toolset))
            {
                return string.Empty;
            }

            string trimmed = toolset.Trim();
            return Regex.IsMatch(trimmed, "^[A-Za-z0-9_.-]+$")
                ? trimmed
                : string.Empty;
        }

        private static string BuildReadme(FluidX3DAblSettings settings)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Eddy3D FluidX3D ABL Example");
            sb.AppendLine("===========================");
            sb.AppendLine();
            sb.AppendLine("This folder was prepared by Eddy3D for a minimal FluidX3D case:");
            sb.AppendLine("- Two offset box buildings");
            sb.AppendLine("- Log-law atmospheric boundary layer");
            sb.AppendLine("- Velocity exports for ParaView");
            sb.AppendLine();
            sb.AppendLine("Parameters");
            sb.AppendLine("----------");
            sb.AppendLine("- Uref: " + settings.Uref.ToString(CultureInfo.InvariantCulture) + " m/s");
            sb.AppendLine("- zRef: " + settings.Zref.ToString(CultureInfo.InvariantCulture) + " m");
            sb.AppendLine("- z0: " + settings.Z0.ToString(CultureInfo.InvariantCulture) + " m");
            sb.AppendLine("- Flow dir (x,y): (" + settings.FlowDirectionX.ToString(CultureInfo.InvariantCulture) + ", "
                + settings.FlowDirectionY.ToString(CultureInfo.InvariantCulture) + ")");
            sb.AppendLine("- VRAM budget: " + settings.MemoryMb.ToString(CultureInfo.InvariantCulture) + " MB");
            sb.AppendLine("- Sim time: " + settings.SimSeconds.ToString(CultureInfo.InvariantCulture) + " s");
            sb.AppendLine("- Export interval: " + settings.ExportIntervalSeconds.ToString(CultureInfo.InvariantCulture) + " s");
            sb.AppendLine("- Domain (Lx, Ly, Lz): (" + settings.DomainLx.ToString(CultureInfo.InvariantCulture) + ", "
                + settings.DomainLy.ToString(CultureInfo.InvariantCulture) + ", "
                + settings.DomainLz.ToString(CultureInfo.InvariantCulture) + ") m");
            sb.AppendLine("- STL building count: " + settings.BuildingStlFiles.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine();
            sb.AppendLine("Run");
            sb.AppendLine("---");
            sb.AppendLine("- macOS/Linux: Scripts/run_fluidx3d.command");
            sb.AppendLine("- Windows: Scripts/run_fluidx3d.bat");
            sb.AppendLine();
            sb.AppendLine("ParaView");
            sb.AppendLine("--------");
            sb.AppendLine("Open files in the case folder VTK directory (working-dir/FluidX3D/VTK).");
            sb.AppendLine("Eddy3D launch scripts redirect FluidX3D/bin/export to that case VTK directory.");
            sb.AppendLine("Use a Calculator filter with expression mag(data) to visualize velocity magnitude.");
            return sb.ToString();
        }

        private static string EscapeForCpp(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string EscapeForBashDoubleQuotedString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("$", "\\$")
                .Replace("`", "\\`");
        }

        private static string EscapeForBatchQuotedValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\"", "\"\"");
        }

        private static string ToCppFloatLiteral(double value)
        {
            string text = value.ToString("0.###########", CultureInfo.InvariantCulture);
            if (!text.Contains(".") && !text.Contains("e") && !text.Contains("E"))
            {
                text += ".0";
            }

            return text + "f";
        }

        private static void MakeExecutable(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                using (Process process = Process.Start(new ProcessStartInfo("/bin/chmod", "+x \"" + path + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                }))
                {
                    process?.WaitForExit();
                }
            }
            catch
            {
                // If chmod fails, users can still run the script manually.
            }
        }

        private static string RunProcess(string fileName, string arguments, string workingDirectory)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            StringBuilder output = new StringBuilder();
            using (Process process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start process: " + fileName);
                }

                string stdOut = process.StandardOutput.ReadToEnd();
                string stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(stdOut))
                {
                    output.AppendLine(stdOut.Trim());
                }

                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    output.AppendLine(stdErr.Trim());
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        fileName + " failed with exit code " + process.ExitCode.ToString(CultureInfo.InvariantCulture)
                        + Environment.NewLine + output.ToString().Trim());
                }
            }

            return output.ToString();
        }
    }
}
