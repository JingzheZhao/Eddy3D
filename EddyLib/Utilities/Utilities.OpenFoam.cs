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
            var sb = new StringBuilder();
            string escapedPath = Directories.InsertDoubleBackslashes(baseWorkingDir);

            sb.AppendLine("from paraview.simple import *");
            sb.AppendLine();

            // Load building and ground geometries
            sb.AppendLine($@"building = OpenDataFile(""{escapedPath}mesh\\constant\\triSurface\\building.stl"")");
            sb.AppendLine($@"ground = OpenDataFile(""{escapedPath}mesh\\constant\\triSurface\\ground.stl"")");

            // Load each wind direction case
            foreach (int dir in windDirections)
            {
                sb.AppendLine($@"case_{dir} = OpenDataFile(""{escapedPath}{dir}\\{dir}.foam"")");
            }

            sb.AppendLine();
            sb.AppendLine("Show(building)");
            sb.AppendLine("Show(ground)");

            foreach (int dir in windDirections)
            {
                sb.AppendLine($"Show(case_{dir})");
            }

            // Append Paraview setup script
            sb.AppendLine(GetParaviewSetupScript());

            return sb.ToString();
        }

        private static string GetParaviewSetupScript()
        {
            return @"
#### disable automatic camera reset on 'Show'
paraview.simple._DisableFirstRenderCameraReset()

# find sources
sTLReader1 = FindSource('STLReader1')
sTLReader2 = FindSource('STLReader2')
openFOAMReader1 = GetActiveSource()

# Properties modified on openFOAMReader1
openFOAMReader1.CellArrays = ['U']

# get active view
renderView1 = GetActiveViewOrCreate('RenderView')

# get display properties
openFOAMReader1Display = GetDisplayProperties(openFOAMReader1, view=renderView1)
openFOAMReader1Display.SelectScaleArray = 'None'

# get color transfer functions
pLUT = GetColorTransferFunction('p')
pPWF = GetOpacityTransferFunction('p')

# update display
openFOAMReader1Display.GlyphTableIndexArray = 'None'
openFOAMReader1Display.SetScaleArray = ['POINTS', 'U']
openFOAMReader1Display.OpacityArray = ['POINTS', 'U']
openFOAMReader1Display.OSPRayScaleArray = 'U'

# update animation
animationScene1 = GetAnimationScene()
animationScene1.UpdateAnimationUsingDataTimeSteps()
renderView1.Update()

# add dimensional units
openFOAMReader1.Adddimensionalunitstoarraynames = 1
renderView1.Update()

# update display properties
openFOAMReader1Display.SelectOrientationVectors = 'None'
openFOAMReader1Display.SetScaleArray = ['POINTS', 'U [m/s]']
openFOAMReader1Display.OpacityArray = ['POINTS', 'U [m/s]']
openFOAMReader1Display.OSPRayScaleArray = 'U [m/s]'

# set scalar coloring
ColorBy(openFOAMReader1Display, ('POINTS', 'U [m/s]', 'Magnitude'))
HideScalarBarIfNotNeeded(pLUT, renderView1)
openFOAMReader1Display.RescaleTransferFunctionToDataRange(True, False)
openFOAMReader1Display.SetScalarBarVisibility(renderView1, True)

# color map setup
umsLUT = GetColorTransferFunction('Ums')
umsPWF = GetOpacityTransferFunction('Ums')

# reset view
renderView1.ResetCamera()
renderView1.Background = [1.0, 1.0, 1.0]

# apply viridis colormap
umsLUT.ApplyPreset('Viridis (matplotlib)', True)

# configure color bar
umsLUTColorBar = GetScalarBar(umsLUT, renderView1)
umsLUTColorBar.TitleColor = [0.0, 0.0, 0.0]
umsLUTColorBar.TitleBold = 1
umsLUTColorBar.LabelColor = [0.0, 0.0, 0.0]
umsLUTColorBar.LabelBold = 1
umsLUTColorBar.AutomaticLabelFormat = 0
umsLUTColorBar.LabelFormat = '%-#6.1f'
umsLUTColorBar.RangeLabelFormat = '%-#6.1f'
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
