using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib
{
    public static partial class Utilities
    {
        public static void DeletePhi(OFMeshSettings MeshSettings, OFBaseDomain DOM)
        {
            foreach (int dir in DOM.BCond.WindDirections)
            {
                string phiPath = MeshSettings.baseWorkingDir + dir + @"\0\phi";
                if (File.Exists(phiPath)) { File.Delete(phiPath); }
            }
        }

        public static class FoamCleaner
        {
            private static readonly Regex TimeDirRegex =
                new Regex(@"^\d+(\.\d+)?$", RegexOptions.Compiled);

            private static readonly Regex ProcessorDirRegex =
                new Regex(@"^processor\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private static readonly HashSet<string> PreserveTopLevel =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
            "system", "constant", "0", "0.org", "0.orig"
                };

            private static readonly string[] TransientRootFileGlobs = new string[]
            {
        "*.log", "log.*", "*.OpenFOAM", "*.foam", "core", "backtrace.*"
            };

            /// <summary>
            /// Cleans an OpenFOAM case directory in-place, preserving setup.
            /// Returns true if everything scheduled for deletion was removed.
            /// </summary>
            public static bool CleanCase(string caseRoot)
            {
                bool ok = true;

                // 1) Remove transient root files
                foreach (var pattern in TransientRootFileGlobs)
                {
                    foreach (var file in Directory.GetFiles(caseRoot, pattern, SearchOption.TopDirectoryOnly))
                    {
                        try { File.Delete(file); }
                        catch (Exception) { ok = false; }
                    }
                }

                // 2) Remove transient directories
                foreach (var dir in Directory.GetDirectories(caseRoot, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(dir);

                    // Always preserve these
                    if (PreserveTopLevel.Contains(name))
                        continue;

                    // Delete numeric time dirs, processor dirs, and known transient dirs
                    if (TimeDirRegex.IsMatch(name)
                        || ProcessorDirRegex.IsMatch(name)
                        || name.Equals("postProcessing", StringComparison.OrdinalIgnoreCase)
                        || name.Equals("dynamicCode", StringComparison.OrdinalIgnoreCase)
                        || name.Equals("logs", StringComparison.OrdinalIgnoreCase))
                    {
                        try { Directory.Delete(dir, true); }
                        catch (Exception) { ok = false; }
                        continue;
                    }

                    // Anything else at the top level - leave it alone by default.
                }

                // 3) (Optional) Prune inside constant/ but keep polyMesh/ - safest is to leave constant/ intact.
                // Uncomment only if you really want to prune constant/.
                /*
                var constantDir = Path.Combine(caseRoot, "constant");
                if (Directory.Exists(constantDir))
                {
                    foreach (var sub in Directory.GetDirectories(constantDir))
                    {
                        if (string.Equals(Path.GetFileName(sub), "polyMesh", StringComparison.OrdinalIgnoreCase))
                            continue;
                        try { Directory.Delete(sub, true); } catch (Exception) { ok = false; }
                    }
                }
                */

                return ok;
            }
        }

        public static int GetLastIterationFromDirectory(string simWorkingDirectory)
        {
            simWorkingDirectory = Directories.ReplaceDoubleBackslashes(simWorkingDirectory);

            // Full path
            List<string> directoriesInDir = Directories.GetDirectories(simWorkingDirectory);

            // Without trailing path
            List<string> listOfDirs = new List<string>();
            foreach (string str in directoriesInDir)
            {
                listOfDirs.Add(new DirectoryInfo(str).Name);
            }

            IEnumerable<string> filteredNumbers = listOfDirs.Where(s => s.All(char.IsDigit));

            string lastIteration = filteredNumbers.Max();
            int lastIterationInt = int.Parse(lastIteration);

            return lastIterationInt;
        }

        public static int CalcOptimCPU(string meshWorkingDirectory, int CPUSetByUser)
        {
            int CPU = CPUSetByUser;
            int numberOfCellsInMesh = 0;
            int numberOfCPUsOnMachine = System.Environment.ProcessorCount;

            if (File.Exists(meshWorkingDirectory + @"\log"))
            {
                string[] logFile = File.ReadAllLines(meshWorkingDirectory + @"\log");
                foreach (string line in logFile)
                {
                    if (line.StartsWith("    cells:"))
                    {
                        numberOfCellsInMesh = int.Parse(line.Split(':')[1]);

                        if (numberOfCellsInMesh > 50000)
                        {
                            CPU = numberOfCellsInMesh / 50000;
                            if (CPU > numberOfCPUsOnMachine / 2)
                            {
                                CPU = numberOfCPUsOnMachine / 2;
                            }
                        }

                        if (CPU < 1)
                        {
                            CPU = 1;
                        }
                    }
                    else
                    {
                        CPU = numberOfCPUsOnMachine / 2;
                        if (CPU < 1)
                        {
                            CPU = 1;
                        }
                    }
                }
            }
            else
            {
                CPU = numberOfCPUsOnMachine / 2;
                if (CPU < 1)
                {
                    CPU = 1;
                }
            }

            return CPU;
        }

        public static void ParseABLConditionsFromCaseFolder(string ABLConditionsFilePath, out double URef, out double z0, out double zref)
        {
            URef = 0.0;
            zref = 0.0;
            z0 = 0.0;

            string[] lines = File.ReadAllLines(ABLConditionsFilePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];

                if (l.Contains("Uref"))
                {
                    URef = double.Parse(l.Replace("Uref", "").Replace(";", "").Trim());
                }

                if (l.Contains("z0"))
                {
                    z0 = double.Parse(l.Replace("z0 uniform", "").Replace(";", "").Trim());
                }

                if (l.Contains("Zref"))
                {
                    zref = double.Parse(l.Replace("Zref", "").Replace(";", "").Trim());
                }
            }
        }

        public static string PrepareParaviewLoadScript(String baseWorkingDir, List<int> dirs)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("from paraview.simple import *");

            // build strings

            // building and ground

            sb.AppendLine(@"building = OpenDataFile(""" + Utilities.Directories.InsertDoubleBackslashes(baseWorkingDir) + @"mesh\\constant\\triSurface\\building.stl"")");
            sb.AppendLine(@"ground = OpenDataFile(""" + Utilities.Directories.InsertDoubleBackslashes(baseWorkingDir) + @"mesh\\constant\\triSurface\\ground.stl"")");

            foreach (int dir in dirs)
            {
                sb.AppendLine("case_" + dir + @" = OpenDataFile(""" + Utilities.Directories.InsertDoubleBackslashes(baseWorkingDir) + dir + @"\\" + dir + @".foam"")");
            }

            sb.AppendLine("Show(building)");
            sb.AppendLine("Show(ground)");

            foreach (int dir in dirs)
            {
                sb.AppendLine("Show(case_" + dir + @")");
            }

            sb.AppendLine(@"from paraview.simple import *
#### disable automatic camera reset on 'Show'
paraview.simple._DisableFirstRenderCameraReset()

# find source
sTLReader1 = FindSource('STLReader1')

# find source
sTLReader2 = FindSource('STLReader2')

# get active source.
openFOAMReader1 = GetActiveSource()

# Properties modified on openFOAMReader1
openFOAMReader1.CellArrays = ['U']

# get active view
renderView1 = GetActiveViewOrCreate('RenderView')
# uncomment following to set a specific view size
# renderView1.ViewSize = [2135, 550]

# get display properties
openFOAMReader1Display = GetDisplayProperties(openFOAMReader1, view = renderView1)

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SelectScaleArray = 'None'

# get color transfer function/color map for 'p'
pLUT = GetColorTransferFunction('p')

# get opacity transfer function/opacity map for 'p'
pPWF = GetOpacityTransferFunction('p')

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.GlyphTableIndexArray = 'None'

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SetScaleArray = ['POINTS', 'U']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OpacityArray = ['POINTS', 'U']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OSPRayScaleArray = 'U'

# get animation scene
animationScene1 = GetAnimationScene()

# update animation scene based on data timesteps
animationScene1.UpdateAnimationUsingDataTimeSteps()

# update the view to ensure updated data information
renderView1.Update()

# Properties modified on openFOAMReader1
openFOAMReader1.Adddimensionalunitstoarraynames = 1

# update the view to ensure updated data information
renderView1.Update()

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SelectOrientationVectors = 'None'

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SetScaleArray = ['POINTS', 'U [m/s]']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OpacityArray = ['POINTS', 'U [m/s]']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OSPRayScaleArray = 'U [m/s]'

# set scalar coloring
ColorBy(openFOAMReader1Display, ('POINTS', 'U [m/s]', 'Magnitude'))

# Hide the scalar bar for this color map if no visible data is colored by it.
HideScalarBarIfNotNeeded(pLUT, renderView1)

# rescale color and/or opacity maps used to include current data range
openFOAMReader1Display.RescaleTransferFunctionToDataRange(True, False)

# show color bar/color legend
openFOAMReader1Display.SetScalarBarVisibility(renderView1, True)

# get color transfer function/color map for 'Ums'
umsLUT = GetColorTransferFunction('Ums')

# get opacity transfer function/opacity map for 'Ums'
umsPWF = GetOpacityTransferFunction('Ums')

# reset view to fit data
renderView1.ResetCamera()

# Properties modified on renderView1
renderView1.Background = [1.0, 1.0, 1.0]

# get the material library
materialLibrary1 = GetMaterialLibrary()

# Apply a preset using its name. Note this may not work as expected when presets have duplicate names.
umsLUT.ApplyPreset('Viridis (matplotlib)', True)

# get color legend/bar for umsLUT in view renderView1
umsLUTColorBar = GetScalarBar(umsLUT, renderView1)

# Properties modified on umsLUTColorBar
umsLUTColorBar.TitleColor = [0.0, 0.0, 0.0]
umsLUTColorBar.TitleBold = 1
umsLUTColorBar.LabelColor = [0.0, 0.0, 0.0]
umsLUTColorBar.LabelBold = 1
umsLUTColorBar.AutomaticLabelFormat = 0
umsLUTColorBar.LabelFormat = '%-#6.1f'
umsLUTColorBar.RangeLabelFormat = '%-#6.1f'

#### saving camera placements for all active views

# current camera placement for renderView1
renderView1.CameraPosition = [-109.98370361328125, 1374.7207336425781, 8420.956940089278]
renderView1.CameraFocalPoint = [-109.98370361328125, 1374.7207336425781, 594.7585678100586]
renderView1.CameraParallelScale = 2025.5691894962097
renderView1.CameraParallelProjection = 1

#### uncomment the following to render all views
# RenderAllViews()
# alternatively, if you want to write images, you can use SaveScreenshot(...).
");

            return sb.ToString();
        }

        public static string GetGnuplotPath(OFRunSettings RS, int version)
        {
            string gnuplotpath = "";

            if (version == 0 && RS.WindowsGnuplotInstalled)
            {
                gnuplotpath = @"C:\Program Files\gnuplot\bin\gnuplot.exe";
            }
            else if (version == 1 && RS.BlueCFDIsInstalled)
            {
                gnuplotpath = @"C:\Program Files\blueCFD-Core-2020\msys64\mingw64\bin\gnuplot.exe";
            }
            else if (version == 0 && !RS.WindowsGnuplotInstalled && RS.BlueCFDIsInstalled)
            {
                gnuplotpath = @"C:\Program Files\blueCFD-Core-2020\msys64\mingw64\bin\gnuplot.exe";
            }
            else if (version == 1 && !RS.BlueCFDIsInstalled && RS.WindowsGnuplotInstalled)
            {
                gnuplotpath = @"C:\Program Files\gnuplot\bin\gnuplot.exe";
            }

            return gnuplotpath;
        }

        public static string GetParaviewPath(int version)
        {
            //param.AddNamedValue("Windows V4", 0);
            //param.AddNamedValue("Windows V5", 1);
            //param.AddNamedValue("BlueCFD", 2);

            string matchingvalues = "";

            string str4 = @"C:\Program Files (x86)\";
            string str5 = @"C:\Program Files\";
            string para = "ParaView";

            string paraviewPath = "";

            if (version == 0)
            {
                DirectoryInfo[] di = new DirectoryInfo(str4).GetDirectories();
                List<string> list = new List<string>();

                foreach (DirectoryInfo d in di)
                {
                    list.Add(d.ToString());
                }
                matchingvalues = list.LastOrDefault(stringToCheck => stringToCheck.StartsWith(para));
                paraviewPath = str4 + matchingvalues + @"\bin\paraview.exe";
            }
            else if (version == 1)
            {
                DirectoryInfo[] di = new DirectoryInfo(str5).GetDirectories();
                List<string> list = new List<string>();

                foreach (DirectoryInfo d in di)
                {
                    list.Add(d.ToString());
                }
                matchingvalues = list.LastOrDefault(stringToCheck => stringToCheck.StartsWith(para));
                paraviewPath = str5 + matchingvalues + @"\bin\paraview.exe";
            }
            else
            {
                paraviewPath = @"C:\Program Files\blueCFD-Core-2020\AddOns\ParaView\bin\paraview.exe";
            }

            return paraviewPath;
        }
    }
}
