using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public double SimSeconds { get; set; } = 30.0;
        public double ExportIntervalSeconds { get; set; } = 10.0;
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

        private static readonly HashSet<string> ExcludedDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            "bin",
            "temp",
            "obj",
            ".vs"
        };

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

            Directory.CreateDirectory(workingRoot);

            string caseRoot = Path.Combine(workingRoot, "FluidX3D");
            if (!PathsEqual(sourceRoot, caseRoot))
            {
                if (IsSubPath(sourceRoot, caseRoot))
                {
                    throw new InvalidOperationException(
                        "Working directory cannot be inside the FluidX3D source directory. Choose a different working directory.");
                }

                MirrorDirectory(sourceRoot, caseRoot);
            }

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

            string scriptsDirectory = Path.Combine(workingRoot, "Scripts");
            Directory.CreateDirectory(scriptsDirectory);

            string commandScriptPath = Path.Combine(scriptsDirectory, "run_fluidx3d.command");
            string batchScriptPath = Path.Combine(scriptsDirectory, "run_fluidx3d.bat");
            string windowsPlatformToolset = ResolveWindowsPlatformToolsetOverride(caseRoot);
            File.WriteAllText(commandScriptPath, BuildMacLaunchScript());
            File.WriteAllText(batchScriptPath, BuildWindowsLaunchScript(windowsPlatformToolset));
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
                ExportDirectory = Path.Combine(caseRoot, "bin", "export"),
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

        public static void EnsureSourceRepository(
            string sourceRoot,
            bool updateIfExists,
            out string statusMessage)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                throw new ArgumentException("Source directory is required.", nameof(sourceRoot));
            }

            string fullSourceRoot = Path.GetFullPath(sourceRoot.Trim());
            string parent = Path.GetDirectoryName(fullSourceRoot);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            if (!Directory.Exists(fullSourceRoot) || Utilities.Directories.IsDirectoryEmpty(fullSourceRoot))
            {
                Directory.CreateDirectory(fullSourceRoot);
                if (!Utilities.Directories.IsDirectoryEmpty(fullSourceRoot))
                {
                    throw new InvalidOperationException("Source directory exists but is not empty: " + fullSourceRoot);
                }

                string cloneOutput = RunProcess("git", "clone --depth 1 \"" + RepositoryUrl + "\" \"" + fullSourceRoot + "\"", null);
                statusMessage = "Cloned FluidX3D repository." + Environment.NewLine + cloneOutput.Trim();
                return;
            }

            if (!IsValidSourceDirectory(fullSourceRoot))
            {
                throw new InvalidOperationException(
                    "Existing source directory does not appear to be a valid FluidX3D source: " + fullSourceRoot);
            }

            string gitDir = Path.Combine(fullSourceRoot, ".git");
            if (updateIfExists && Directory.Exists(gitDir))
            {
                string pullOutput = RunProcess("git", "-C \"" + fullSourceRoot + "\" pull --ff-only", null);
                statusMessage = "Updated existing FluidX3D source." + Environment.NewLine + pullOutput.Trim();
                return;
            }

            statusMessage = "Using existing FluidX3D source directory.";
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

        private static bool PathsEqual(string a, string b)
        {
            string normalizedA = Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedB = Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(normalizedA, normalizedB, comparison);
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

        private static void MirrorDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string targetPath = Path.Combine(targetDir, fileName);
                File.Copy(filePath, targetPath, true);
            }

            foreach (string directoryPath in Directory.GetDirectories(sourceDir))
            {
                string directoryName = Path.GetFileName(directoryPath);
                if (ExcludedDirectoryNames.Contains(directoryName))
                {
                    continue;
                }

                MirrorDirectory(directoryPath, Path.Combine(targetDir, directoryName));
            }
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
            sb.AppendLine("\t\tlbm.u.z[n] = 0.0f;");
            sb.AppendLine();
            sb.AppendLine("\t\tif(z == 0u) {");
            sb.AppendLine("\t\t\t// Ground plane (no-slip)");
            sb.AppendLine("\t\t\tlbm.flags[n] = isSolid ? lbm.flags[n] : TYPE_S;");
            sb.AppendLine("\t\t\tlbm.u.y[n] = 0.0f;");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\telse if(isSolid) {");
            sb.AppendLine("\t\t\t// Preserve voxelized solid cells (TYPE_S | TYPE_X).");
            sb.AppendLine("\t\t\tlbm.u.y[n] = 0.0f;");
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
            sb.AppendLine("\t\t\t// Initialize velocity (wind blows in +y direction)");
            sb.AppendLine("\t\t\tlbm.u.y[n] = u_abl;");
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
            sb.AppendLine("\tprint_info(\"Simulation complete. VTK files in bin/export/\");");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("#endif // BENCHMARK");
            return sb.ToString();
        }

        private static string BuildMacLaunchScript()
        {
            return
@"#!/bin/bash
SCRIPT_DIR=""$(cd ""$(dirname ""$0"")"" && pwd)""
cd ""$SCRIPT_DIR/../FluidX3D"" || exit 1
chmod +x make.sh
./make.sh
STATUS=$?
echo
if [ $STATUS -eq 0 ]; then
  echo ""FluidX3D completed successfully.""
else
  echo ""FluidX3D failed with exit code $STATUS.""
fi
read -r -p ""Press Enter to close...""
exit $STATUS
";
        }

        private static string BuildWindowsLaunchScript(string platformToolsetOverride)
        {
            string safeToolset = SanitizePlatformToolsetForBatch(platformToolsetOverride);
            return
@"@echo off
setlocal
cd /d ""%~dp0..\FluidX3D"" || exit /b 1

set ""VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe""
set ""MSBUILD=""
set ""PLATFORM_TOOLSET=__PLATFORM_TOOLSET__""
if exist ""%VSWHERE%"" (
  for /f ""usebackq tokens=*"" %%i in (`""%VSWHERE%"" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    set ""MSBUILD=%%i""
  )
)

if not defined MSBUILD (
  echo Could not find MSBuild automatically.
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
if not ""%FLUIDX3D_EXIT%""==""0"" (
  echo FluidX3D failed with exit code %FLUIDX3D_EXIT%.
  pause
  exit /b %FLUIDX3D_EXIT%
)

echo FluidX3D completed successfully.
pause
exit /b 0
"
            .Replace("__PLATFORM_TOOLSET__", safeToolset);
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
            sb.AppendLine("Open files in FluidX3D/bin/export (for example the latest u-*.vtk).");
            sb.AppendLine("Use a Calculator filter with expression mag(data) to visualize velocity magnitude.");
            return sb.ToString();
        }

        private static string EscapeForCpp(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
