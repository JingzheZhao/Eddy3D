using EddyLib.OpenFOAM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib
{
    /// <summary>
    /// OpenFOAM-related utility methods.
    /// </summary>
    public static partial class Utilities
    {
        #region Delegate Methods (using OpenFOAMHelpers)

        /// <summary>
        /// Deletes phi files for all wind directions.
        /// </summary>
        public static void DeletePhi(OFMeshSettings meshSettings, OFBaseDomain domain)
        {
            OpenFOAMHelpers.DeletePhi(meshSettings, domain);
        }

        /// <summary>
        /// Gets the last iteration number from a simulation directory.
        /// </summary>
        public static int GetLastIterationFromDirectory(string simWorkingDirectory)
        {
            return OpenFOAMHelpers.GetLastIterationFromDirectory(simWorkingDirectory);
        }

        /// <summary>
        /// Calculates optimal CPU count based on mesh size.
        /// </summary>
        public static int CalcOptimCPU(string meshWorkingDirectory, int cpuSetByUser)
        {
            return OpenFOAMHelpers.CalculateOptimalCPUs(meshWorkingDirectory, cpuSetByUser);
        }

        #endregion

        #region FoamCleaner (delegates to OpenFOAMHelpers)

        /// <summary>
        /// Cleans OpenFOAM case directories - delegates to OpenFOAM.FoamCleaner.
        /// </summary>
        public static class FoamCleaner
        {
            /// <summary>
            /// Cleans an OpenFOAM case directory, preserving setup files.
            /// </summary>
            public static bool CleanCase(string caseRoot)
            {
                return OpenFOAM.FoamCleaner.CleanCase(caseRoot);
            }

            /// <summary>
            /// Removes Eddy probe cache binaries from the root postProcessing folder.
            /// </summary>
            public static bool CleanProbeCacheFiles(string workingDirectory)
            {
                return OpenFOAM.FoamCleaner.CleanProbeCacheFiles(workingDirectory);
            }
        }

        #endregion

        #region ABL Parsing

        /// <summary>
        /// Parses ABL conditions from a case folder.
        /// </summary>
        public static void ParseABLConditionsFromCaseFolder(string ablConditionsFilePath,
            out double URef, out double z0, out double zref)
        {
            URef = 0.0;
            zref = 0.0;
            z0 = 0.0;

            if (!File.Exists(ablConditionsFilePath)) return;

            foreach (string line in File.ReadAllLines(ablConditionsFilePath))
            {
                if (line.Contains("Uref"))
                {
                    if (double.TryParse(line.Replace("Uref", "").Replace(";", "").Trim(), out double val))
                        URef = val;
                }
                else if (line.Contains("z0") && line.Contains("uniform"))
                {
                    if (double.TryParse(line.Replace("z0 uniform", "").Replace(";", "").Trim(), out double val))
                        z0 = val;
                }
                else if (line.Contains("Zref"))
                {
                    if (double.TryParse(line.Replace("Zref", "").Replace(";", "").Trim(), out double val))
                        zref = val;
                }
            }
        }

        #endregion

        #region Paraview Script Generation

        /// <summary>
        /// Generates a Paraview Python script to load simulation results.
        /// </summary>
        public static string PrepareParaviewLoadScript(string baseWorkingDir, List<int> windDirections)
        {
            if (windDirections == null || windDirections.Count == 0)
            {
                windDirections = new List<int> { 0 };
            }

            var sb = new StringBuilder();
            string normalizedBaseDir = EscapePathForPython(Path.GetFullPath(baseWorkingDir));
            int primaryDirection = (windDirections != null && windDirections.Count > 0) ? windDirections[0] : 0;

            sb.AppendLine("from paraview.simple import *");
            sb.AppendLine("import os");
            sb.AppendLine();
            sb.AppendLine($@"base_dir = ""{normalizedBaseDir}""");
            sb.AppendLine(@"tri_surface_dir = os.path.join(base_dir, ""mesh"", ""constant"", ""triSurface"")");
            sb.AppendLine();

            // Load building and ground geometries
            sb.AppendLine("building = None");
            sb.AppendLine("ground = None");
            sb.AppendLine("try:");
            sb.AppendLine(@"    building = OpenDataFile(os.path.join(tri_surface_dir, ""building.stl""))");
            sb.AppendLine("except Exception:");
            sb.AppendLine("    building = None");
            sb.AppendLine("try:");
            sb.AppendLine(@"    ground = OpenDataFile(os.path.join(tri_surface_dir, ""ground.stl""))");
            sb.AppendLine("except Exception:");
            sb.AppendLine("    ground = None");

            // Load each wind direction case
            foreach (int dir in windDirections)
            {
                sb.AppendLine($"case_{dir} = None");
                sb.AppendLine("try:");
                sb.AppendLine($@"    case_{dir} = OpenDataFile(os.path.join(base_dir, ""{dir}"", ""{dir}.foam""))");
                sb.AppendLine($"    RenameSource('Case_{dir}', case_{dir})");
                sb.AppendLine("except Exception:");
                sb.AppendLine($"    case_{dir} = None");
            }

            sb.AppendLine();
            sb.AppendLine("all_cases = []");
            foreach (int dir in windDirections)
            {
                sb.AppendLine($"if case_{dir} is not None:");
                sb.AppendLine($"    all_cases.append(case_{dir})");
            }
            sb.AppendLine($"primary_case = case_{primaryDirection}");
            sb.AppendLine("if primary_case is None and len(all_cases) > 0:");
            sb.AppendLine("    primary_case = all_cases[0]");

            // Append ParaView setup script
            sb.AppendLine(GetParaviewSetupScript());

            return sb.ToString();
        }

        private static string EscapePathForPython(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            // Forward slashes work on Windows/macOS/Linux and avoid escape issues in Python strings.
            return path.Replace("\\", "/").Replace("\"", "\\\"");
        }

        private static string GetParaviewSetupScript()
        {
            return $@"
#### disable automatic camera reset on 'Show'
paraview.simple._DisableFirstRenderCameraReset()

def _safe_set(obj, prop, value):
    try:
        setattr(obj, prop, value)
        return True
    except Exception:
        return False

def _has_array(data_info, array_name):
    try:
        return data_info.GetArrayInformation(array_name) is not None
    except Exception:
        return False

def _try_apply_preset(lut, presets):
    if lut is None:
        return
    for p in presets:
        try:
            lut.ApplyPreset(p, True)
            return
        except Exception:
            pass

# get active view
renderView1 = GetActiveViewOrCreate('RenderView')

# styling for static geometry
if building is not None:
    bDisp = Show(building, renderView1)
    _safe_set(bDisp, 'Representation', 'Surface')
    _safe_set(bDisp, 'DiffuseColor', [0.24, 0.24, 0.24])
    _safe_set(bDisp, 'AmbientColor', [0.24, 0.24, 0.24])
    _safe_set(bDisp, 'Opacity', 1.0)
    try:
        Hide(building, renderView1)
    except Exception:
        pass

if ground is not None:
    gDisp = Show(ground, renderView1)
    _safe_set(gDisp, 'Representation', 'Surface')
    _safe_set(gDisp, 'DiffuseColor', [0.72, 0.72, 0.72])
    _safe_set(gDisp, 'AmbientColor', [0.72, 0.72, 0.72])
    _safe_set(gDisp, 'Opacity', 1.0)
    try:
        Hide(ground, renderView1)
    except Exception:
        pass

# show all cases but keep only primary visible for cleaner default render
for i, src in enumerate(all_cases):
    d = Show(src, renderView1)
    _safe_set(d, 'Representation', 'Surface')
    _safe_set(d, 'Opacity', 0.98)
    _safe_set(d, 'Specular', 0.05)
    if i > 0 and src is not primary_case:
        try:
            Hide(src, renderView1)
        except Exception:
            pass

if primary_case is not None:
    SetActiveSource(primary_case)
    openFOAMReader1 = primary_case

    # load common fields when available
    for propName in ['CellArrays', 'PointArrays']:
        try:
            setattr(openFOAMReader1, propName, ['U', 'p'])
        except Exception:
            pass

    try:
        openFOAMReader1.Adddimensionalunitstoarraynames = 1
    except Exception:
        pass

    # update animation from result timesteps
    try:
        animationScene1 = GetAnimationScene()
        animationScene1.UpdateAnimationUsingDataTimeSteps()
    except Exception:
        pass
    renderView1.Update()

    # display props
    openFOAMReader1Display = GetDisplayProperties(openFOAMReader1, view=renderView1)
    _safe_set(openFOAMReader1Display, 'SelectScaleArray', 'None')
    _safe_set(openFOAMReader1Display, 'GlyphTableIndexArray', 'None')
    _safe_set(openFOAMReader1Display, 'SelectOrientationVectors', 'None')

    # choose best available velocity-like array
    point_data = openFOAMReader1.GetDataInformation().GetPointDataInformation()
    cell_data = openFOAMReader1.GetDataInformation().GetCellDataInformation()

    assoc = None
    arr = None
    arr_info = None
    for candidate in ['U [m/s]', 'U']:
        if _has_array(point_data, candidate):
            assoc = 'POINTS'
            arr = candidate
            arr_info = point_data.GetArrayInformation(candidate)
            break

    if arr is None:
        for candidate in ['U [m/s]', 'U']:
            if _has_array(cell_data, candidate):
                assoc = 'CELLS'
                arr = candidate
                arr_info = cell_data.GetArrayInformation(candidate)
                break

    if arr is not None:
        try:
            openFOAMReader1Display.SetScaleArray = [assoc, arr]
            openFOAMReader1Display.OpacityArray = [assoc, arr]
            openFOAMReader1Display.OSPRayScaleArray = arr
        except Exception:
            pass

        nComp = 1
        try:
            nComp = arr_info.GetNumberOfComponents()
        except Exception:
            pass

        colored = False
        if nComp >= 3:
            try:
                ColorBy(openFOAMReader1Display, (assoc, arr, 'Magnitude'))
                colored = True
            except Exception:
                pass
        if not colored:
            try:
                ColorBy(openFOAMReader1Display, (assoc, arr))
                colored = True
            except Exception:
                pass

        if colored:
            try:
                openFOAMReader1Display.RescaleTransferFunctionToDataRange(True, False)
            except Exception:
                pass
            try:
                openFOAMReader1Display.SetScalarBarVisibility(renderView1, True)
            except Exception:
                pass
            try:
                velLUT = GetColorTransferFunction(arr)
                _try_apply_preset(velLUT, ['Turbo', 'Viridis (matplotlib)', 'Cool to Warm'])
                velBar = GetScalarBar(velLUT, renderView1)
                _safe_set(velBar, 'TitleColor', [0.0, 0.0, 0.0])
                _safe_set(velBar, 'LabelColor', [0.0, 0.0, 0.0])
                _safe_set(velBar, 'TitleBold', 1)
                _safe_set(velBar, 'LabelBold', 1)
                _safe_set(velBar, 'AutomaticLabelFormat', 0)
                _safe_set(velBar, 'LabelFormat', '%-#6.2f')
                _safe_set(velBar, 'RangeLabelFormat', '%-#6.2f')
            except Exception:
                pass
    else:
        # fallback to solid color if no velocity array exists
        try:
            ColorBy(openFOAMReader1Display, None)
        except Exception:
            pass

# scene quality defaults (with safe fallbacks)
_safe_set(renderView1, 'Background', [0.972549, 0.972549, 0.972549])
_safe_set(renderView1, 'Background2', [0.972549, 0.972549, 0.972549])
_safe_set(renderView1, 'UseGradientBackground', 0)
_safe_set(renderView1, 'OrientationAxesVisibility', 1)
_safe_set(renderView1, 'UseFXAA', 1)
_safe_set(renderView1, 'CameraParallelProjection', 0)

# reset and apply an isometric-like camera
try:
    renderView1.ResetCamera()
    camera = GetActiveCamera()
    camera.SetParallelProjection(0)
    camera.Azimuth(35)
    camera.Elevation(22)
    renderView1.ResetCameraClippingRange()
except Exception:
    pass

Render()
";
        }

        #endregion

        #region External Tool Paths

        /// <summary>
        /// Gets the Gnuplot executable path.
        /// </summary>
        public static string GetGnuplotPath(OFRunSettings runSettings, int version)
        {
            const string windowsPath = @"C:\Program Files\gnuplot\bin\gnuplot.exe";
            string blueCfdPath = Path.Combine(DefaultDirectoriesAndPaths.BlueCfdDir, @"msys64\mingw64\bin\gnuplot.exe");

            if (version == 0 && runSettings.WindowsGnuplotInstalled)
                return windowsPath;
            if (version == 1 && runSettings.BlueCFDIsInstalled)
                return blueCfdPath;
            if (version == 0 && runSettings.BlueCFDIsInstalled)
                return blueCfdPath;
            if (version == 1 && runSettings.WindowsGnuplotInstalled)
                return windowsPath;

            return string.Empty;
        }

        /// <summary>
        /// Gets the ParaView executable path.
        /// Prefers the newest standalone ParaView install and falls back to blueCFD on Windows.
        /// </summary>
        public static string GetParaviewPath()
        {
            // 1) Prefer standalone ParaView (newest version found)
            string standalone = GetStandaloneParaviewPath();
            if (!string.IsNullOrWhiteSpace(standalone))
            {
                return standalone;
            }

            // 2) Windows fallback: blueCFD bundled ParaView
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string blueCfdPath = Path.Combine(DefaultDirectoriesAndPaths.BlueCfdDir, @"AddOns\ParaView\bin\paraview.exe");
                if (File.Exists(blueCfdPath))
                {
                    return blueCfdPath;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Legacy overload kept for compatibility; version selection is now automatic.
        /// </summary>
        public static string GetParaviewPath(int version)
        {
            return GetParaviewPath();
        }

        private static string GetStandaloneParaviewPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetNewestWindowsParaviewPath();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetNewestMacParaviewPath();
            }

            return GetLinuxParaviewPath();
        }

        private static string GetNewestWindowsParaviewPath()
        {
            var candidates = new List<(string Exe, Version Ver, bool IsX86, string FolderName)>();

            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            }
            .Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
            .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var root in roots)
            {
                bool isX86Root = root.IndexOf("x86", StringComparison.OrdinalIgnoreCase) >= 0;
                IEnumerable<string> dirs;
                try
                {
                    dirs = Directory.GetDirectories(root, "ParaView*", SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (var dir in dirs)
                {
                    string exe = Path.Combine(dir, "bin", "paraview.exe");
                    if (!File.Exists(exe))
                    {
                        continue;
                    }

                    string folderName = Path.GetFileName(dir);
                    candidates.Add((exe, ParseParaviewVersion(folderName), isX86Root, folderName));
                }
            }

            var best = candidates
                .OrderByDescending(c => c.Ver)
                .ThenBy(c => c.IsX86)
                .ThenByDescending(c => c.FolderName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(best.Exe) ? string.Empty : best.Exe;
        }

        private static string GetNewestMacParaviewPath()
        {
            const string applicationsDir = "/Applications";
            if (!Directory.Exists(applicationsDir))
            {
                return string.Empty;
            }

            var candidates = new List<(string Exe, Version Ver, string FolderName)>();
            IEnumerable<string> apps;
            try
            {
                apps = Directory.GetDirectories(applicationsDir, "ParaView*.app", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return string.Empty;
            }

            foreach (var app in apps)
            {
                string folderName = Path.GetFileName(app);
                string exe1 = Path.Combine(app, "Contents", "bin", "paraview");
                string exe2 = Path.Combine(app, "Contents", "MacOS", "paraview");

                if (File.Exists(exe1))
                {
                    candidates.Add((exe1, ParseParaviewVersion(folderName), folderName));
                }
                else if (File.Exists(exe2))
                {
                    candidates.Add((exe2, ParseParaviewVersion(folderName), folderName));
                }
            }

            var best = candidates
                .OrderByDescending(c => c.Ver)
                .ThenByDescending(c => c.FolderName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(best.Exe) ? string.Empty : best.Exe;
        }

        private static string GetLinuxParaviewPath()
        {
            var candidates = new[]
            {
                "/usr/bin/paraview",
                "/usr/local/bin/paraview",
                "/snap/bin/paraview"
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static Version ParseParaviewVersion(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return new Version(0, 0);
            }

            // Handles names such as:
            // "ParaView 6.0.1"
            // "ParaView 5.12.0-Windows-Python3.10-msvc2017-AMD64"
            var match = Regex.Match(folderName, @"\d+(\.\d+){1,3}");
            if (!match.Success)
            {
                return new Version(0, 0);
            }

            if (Version.TryParse(match.Value, out var ver))
            {
                return ver;
            }

            return new Version(0, 0);
        }

        #endregion
    }
}
