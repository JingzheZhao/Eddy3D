using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib.Strings
{
    // Adds a wind direction prefix for the individual simulation folders
    public enum OFExecutionMode
    {
        Simulation,

        Meshing
    }

    // This is necessary to make sure that a cmd window started from GH will be executed in the case working folder
    public enum RunMode
    {
        Canvas,

        Batchfile
    }

    public class BatFiles
    {
        //Run commands as list

        private static List<string> TopoSet = new List<string>
        {
            "topoSet",
            "setsToZones -noFlipMap" };

        public static string Run_Make_Trees(OFRunSettings RunSettings, OFMeshSettings MeshSettings, OFBaseDomain DOM, OFExecutionMode mode)
        {
            StringBuilder sb = new StringBuilder();

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                {
                    foreach (string str in TopoSet)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str);
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(TopoSet, MeshSettings.meshWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            return sb.ToString();
        }

        private static readonly List<string> RCCheckMeshSingleCPU = new List<string> {
       // "checkMesh -allGeometry -allTopology -writeAllFields -writeSets vtk",  // Not supported in OpenFOAM 5 yet
        "checkMesh -allGeometry -allTopology -writeSets vtk",
        "foamToVTK -faceSet highAspectRatioCells -ascii",
        "foamToVTK -faceSet nonOrthoFaces -ascii",
        "foamToVTK -faceSet skewFaces -ascii",
        "foamToVTK -faceSet wrongOrientedFaces -ascii",
        "foamToVTK -faceSet zeroVolumeCells -ascii",
        "foamToVTK -faceSet edgeFaces -ascii",
        "foamToVTK -cellSet oneInternalFaceCells -ascii",
        "foamToVTK -cellSet concaveCells -ascii"};

        private static readonly List<string> RCBlockMeshSingleCPU = new List<string> {
        "blockMesh"};

        private static List<string> RCSimMultiCPU(OFRunSettings RunSettings)
        {
            List<string> lst = new List<string>();
            if (RunSettings.potentialFoamInit)
            {
                lst = new List<string>{
     "decomposePar -force",

     "mpiexec -np " + RunSettings.CPUs + @" potentialFoam -parallel",
     "mpiexec -np " + RunSettings.CPUs + @" simpleFoam -parallel",
     "reconstructPar -latestTime"};
            }
            else
            {
                lst = new List<string>{
                "decomposePar -force",
                "mpiexec -np " + RunSettings.CPUs + @" simpleFoam -parallel",
                "reconstructPar -latestTime"            };
            }
            return lst;
        }

        private static List<string> RCSimSingleCPU(OFRunSettings RunSettings)
        {
            var lst = new List<string>();

            if (RunSettings.potentialFoamInit)
            {
                lst = new List<string> {
        "potentialFoam",
        "simpleFoam"
                };
            }
            else
            {
                lst = new List<string> {
        "simpleFoam"
                };
            }
            return lst;
        }

        private static readonly List<string> RCSimContinueSingleCPU = new List<string> {
        "simpleFoam"};

        private static List<string> reconstructMesh()
        {
            List<string> lst = new List<string>
            {
                "reconstructParMesh -constant"
            };
            return lst;
        }

        private static List<string> RCSimContinueMultiCPU(OFRunSettings RunSettings)
        {
            List<string> lst = new List<string>
            {
                "mpiexec -np " + RunSettings.CPUs + @" simpleFoam -parallel",
                "reconstructPar -latestTime"
            };
            return lst;
        }

        private static List<string> RCMeshMultiCPU(OFRunSettings RunSettings, OFMeshSettings MeshSettings)
        {
            List<string> lst = new List<string>();

            if (MeshSettings.snappySetting == SnappySnapSettings.BlocksSnapping || MeshSettings.snappySetting == SnappySnapSettings.BlocksSnappingLayers)
            {
                lst.AddRange(new List<string>{
                "blockMesh",
                "surfaceFeatures",
                "decomposePar -force",
                "mpiexec -np " + RunSettings.CPUs + @" snappyHexMesh -overwrite -parallel",
                "reconstructParMesh -constant",
                "renumberMesh -overwrite"
            });
            }
            else
            {
                lst.AddRange(new List<string>{
                "blockMesh",
                "decomposePar -force",
                "mpiexec -np " + RunSettings.CPUs + @" snappyHexMesh -overwrite -parallel",
                "reconstructParMesh -constant",
                "renumberMesh -overwrite"
            });
            }

            return lst;
        }

        private static readonly List<string> RCMeshSingleCPU = new List<string> {
        "blockMesh",
        "surfaceFeatures",
        "snappyHexMesh -overwrite",
        "renumberMesh -overwrite"};

        private static readonly List<string> divU = new List<string> { "postProcess -func ttt -latestTime" };

        public static string DockerPrefixPath(OFBaseDomain DOM, OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFExecutionMode mode)
        {
            StringBuilder sb = new StringBuilder();
            if (mode == OFExecutionMode.Simulation && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.DockerbaseWorkingDir + DOM.BCond.WindDirections[0] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == OFExecutionMode.Meshing && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.DockermeshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == OFExecutionMode.Simulation)
            {
                sb.Append(@"docker run -v """ + MeshSettings.baseWorkingDir + DOM.BCond.WindDirections[0] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == OFExecutionMode.Meshing)
            {
                sb.Append(@"docker run -v """ + MeshSettings.meshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }

            return sb.ToString();
        }

        public static string DockerPrefixPath(OFBaseDomain DOM, OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFExecutionMode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            if (mode == OFExecutionMode.Simulation && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.DockerbaseWorkingDir + +DOM.BCond.WindDirections[d] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == OFExecutionMode.Meshing && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.DockermeshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == OFExecutionMode.Simulation)
            {
                sb.Append(@"docker run -v """ + MeshSettings.baseWorkingDir + +DOM.BCond.WindDirections[d] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == OFExecutionMode.Meshing)
            {
                sb.Append(@"docker run -v """ + MeshSettings.meshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }

            return sb.ToString();
        }

        public static string Run_Mesh(OFRunSettings RunSettings, OFMeshSettings MeshSettings, OFBaseDomain DOM, OFExecutionMode mode)
        {
            StringBuilder sb = new StringBuilder();

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in RCMeshMultiCPU(RunSettings, MeshSettings))
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str);
                    }
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCMeshSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str);
                    }
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(RCMeshMultiCPU(RunSettings, MeshSettings), MeshSettings.meshWorkingDir));
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(RCMeshSingleCPU, MeshSettings.meshWorkingDir));
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
            }
            return sb.ToString();
        }

        public static string Run_sim(OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFBaseDomain DOM, OFExecutionMode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = MeshSettings.baseWorkingDir + "\\" + DOM.BCond.WindDirections[d];

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in RCSimMultiCPU(RunSettings))
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str);
                    }
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCSimSingleCPU(RunSettings))
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str);
                    }
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(RCSimMultiCPU(RunSettings), caseWorkingDir));
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(RCSimSingleCPU(RunSettings), caseWorkingDir));
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
            }

            // Add gnuplot command to plot residuals at the end
            sb.AppendLine();
            sb.AppendLine("REM Generate residuals plot");
            sb.AppendLine($"if exist \"{caseWorkingDir}\\postProcessing\\residuals\\0\\residuals.dat\" (");
            sb.AppendLine($"    echo Generating residuals plot...");
            sb.AppendLine($"    gnuplot \"{caseWorkingDir}\\plot_residuals.plt\"");
            sb.AppendLine($"    if exist \"{caseWorkingDir}\\residuals.png\" (");
            sb.AppendLine($"        echo Residuals plot saved to: {caseWorkingDir}\\residuals.png");
            sb.AppendLine($"    ) else (");
            sb.AppendLine($"        echo Warning: gnuplot failed or not installed");
            sb.AppendLine($"    )");
            sb.AppendLine($") else (");
            sb.AppendLine($"    echo Warning: residuals.dat not found");
            sb.AppendLine($")");

            return sb.ToString();
        }

        public static string Run_sim_continue(OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFBaseDomain DOM, OFExecutionMode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = MeshSettings.baseWorkingDir + "\\" + DOM.BCond.WindDirections[d];

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in RCSimContinueMultiCPU(RunSettings))
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str);
                    }
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCSimContinueSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str);
                    }
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(RCSimContinueMultiCPU(RunSettings), caseWorkingDir));
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(RCSimContinueSingleCPU, caseWorkingDir));
#if DEBUG
                    //sb.AppendLine("PAUSE");
#endif
                }
            }

            return sb.ToString();
        }

        public static string Run_divU(OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFBaseDomain DOM, OFExecutionMode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = MeshSettings.baseWorkingDir + "\\" + DOM.BCond.WindDirections[d];

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in divU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str);
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in divU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str);
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(divU, caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(divU, caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }

            return sb.ToString();
        }

        public static string Run_checkMesh(OFRunSettings RunSettings, OFMeshSettings MeshSettings, OFBaseDomain DOM, OFExecutionMode mode)
        {
            StringBuilder sb = new StringBuilder();
            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                foreach (string str in RCCheckMeshSingleCPU)
                {
                    sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str);
                }
#if DEBUG
                //sb.AppendLine("PAUSE");
#endif
            }
            else
            {
                sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(RCCheckMeshSingleCPU, MeshSettings.meshWorkingDir));

#if DEBUG
                //sb.AppendLine("PAUSE");
#endif
            }
            return sb.ToString();
        }

        public static string Run_reconstructMesh(OFRunSettings RunSettings, OFMeshSettings MeshSettings, OFBaseDomain DOM, OFExecutionMode mode)
        {
            StringBuilder sb = new StringBuilder();
            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                foreach (string str in reconstructMesh())
                {
                    sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str);
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            else
            {
                sb.Append(BlueCfdScriptBuilder.BuildBlueCfdBatch(reconstructMesh(), MeshSettings.meshWorkingDir));

#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            return sb.ToString();
        }

        public static string Run(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + @"run_mesh.bat""");
            sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + @"run_checkMesh.bat""");
            foreach (int i in DOM.BCond.WindDirections)
            {
                sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + i + @"_run_sim.bat""");
            }

#if DEBUG
            //sb.AppendLine("PAUSE");
#endif
            return sb.ToString();
        }

        public static string RunSimOnly(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int i in DOM.BCond.WindDirections)
            {
                sb.AppendLine("call \"" + MeshSettings.baseWorkingDir + i + "_run_sim.bat\"");
            }
#if DEBUG
            //sb.AppendLine("PAUSE");
#endif
            return sb.ToString();
        }

        public static string RunDivU_Only(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int i in DOM.BCond.WindDirections)
            {
                sb.AppendLine("call \"" + MeshSettings.baseWorkingDir + i + "_run_divU.bat\"" + " <nul");
            }
#if DEBUG
            //sb.AppendLine("PAUSE");
#endif
            return sb.ToString();
        }

        public static class BlueCfdScriptBuilder
        {
            /// <summary>
            /// Builds a Windows batch script for running blueCFD/OpenFOAM commands.
            /// </summary>
            /// <param name="commands">Commands to run (one per line, e.g., "blockMesh").</param>
            /// <param name="caseDir">Absolute path to the case directory.</param>
            /// <param name="runMode">
            /// Batchfile: assume the .bat file will sit next to the case folder; cd using %~dp0.
            /// Direct: cd directly into caseDir.
            /// </param>
            /// <param name="installationPath">Path to blueCFD installation (trailing backslash optional).</param>
            /// <param name="logFile">Name of the log file to tee into.</param>
            public static string BuildBlueCfdBatch(
                IEnumerable<string> commands,
                string caseDir,
                RunMode runMode = RunMode.Batchfile,
                string installationPath = null,
                string logFile = "log.txt")
            {
                if (commands is null) throw new ArgumentNullException(nameof(commands));
                if (string.IsNullOrWhiteSpace(caseDir)) throw new ArgumentException("caseDir is required.", nameof(caseDir));

                // Use default if null
                installationPath = installationPath ?? DefaultDirectoriesAndPaths.BlueCfdDir;

                // Normalize paths
                installationPath = EnsureTrailingBackslash(installationPath);
                var caseDirTrimmed = caseDir.Trim().Trim('"');
                var caseDirQuoted = $"\"{caseDirTrimmed}\"";

                // Determine drive switch (avoid assuming just "C")
                var root = Path.GetPathRoot(caseDirTrimmed);
                var driveLetter = string.IsNullOrEmpty(root) ? 'C' : char.ToUpperInvariant(root[0]);
                var needsDriveSwitch = driveLetter != 'C';

                // Folder name for Batchfile mode (cd from %~dp0)

                var caseLeaf = new DirectoryInfo(caseDirTrimmed).Name;

                var sb = new StringBuilder();

                sb.AppendLine("@echo off");
                sb.AppendLine("setlocal enableextensions");
                sb.AppendLine($@"call ""{installationPath}setvars_OF8.bat""");
                sb.AppendLine(@"set PATH=%HOME%\msys64\usr\bin;%PATH%");

                if (needsDriveSwitch)
                    sb.AppendLine($"{driveLetter}:");


                if (runMode == RunMode.Batchfile)
                {
                    //sb.AppendLine($@"REM cd {caseDirQuoted}");
                    sb.AppendLine($@"cd /d ""%~dp0{caseLeaf}""");
                }
                else
                {
                    sb.AppendLine($@"cd /d {caseDirQuoted}");
                }

                foreach (var line in commands.Select(c => (c ?? string.Empty).Trim())
                             .Where(c => !string.IsNullOrEmpty(c)))
                {
                    var logForThisCommand = InferLogFileName(line, "log.txt");
                    if (line.StartsWith("reconstructParMesh", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"{line} >> \"reconstructParMesh.log\" 2>&1");
                    }
                    else
                    {
                        sb.AppendLine($"{line}{AppendToLog(logForThisCommand)}");
                    }
                }

                return sb.ToString();
            }

            private static string EnsureTrailingBackslash(string path) =>
                string.IsNullOrEmpty(path) ? path : (path.EndsWith("\\") ? path : path + "\\");

            private static string InferLogFileName(string command, string fallback = "log.txt")
            {
                if (string.IsNullOrWhiteSpace(command)) return fallback;

                var tokens = Tokenize(command);
                if (tokens.Count == 0) return fallback;

                var wrappers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "mpirun","mpiexec","srun","wsl","bash","sh","cmd","cmd.exe",
        "powershell","powershell.exe","python","python.exe","py","py.exe",
    };

                // Options (for wrappers) that take a value in the *next* token
                var wrapperOptsTakeValue = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "-np","-n","--np",
        "--map-by","--bind-to","--rank-by",
        "--host","-H","--hostfile","-host","-machinefile",
        "--mca","-mca",
        "--wdir","-wdir","--wd","-wd",
        "--app", "--path"
        // (expand as needed)
    };

                // Walk tokens with index awareness so we can skip option values
                for (int i = 0; i < tokens.Count; i++)
                {
                    var t = tokens[i].Trim();
                    if (t.Length == 0) continue;

                    // Skip global wrappers themselves
                    if (wrappers.Contains(t)) continue;

                    // Skip options (e.g., -np, --map-by)
                    if (t.StartsWith("-"))
                    {
                        // Also skip the immediate value token if this option expects one
                        if (wrapperOptsTakeValue.Contains(t) && i + 1 < tokens.Count)
                            i++; // consume the value (like the "8" after -np)
                        continue;
                    }

                    // If this token is the value of the *preceding* option, skip it
                    if (i > 0 && wrapperOptsTakeValue.Contains(tokens[i - 1]))
                        continue;

                    // We have a candidate executable/script now.
                    var baseName = Path.GetFileName(t);
                    if (string.IsNullOrEmpty(baseName)) continue;

                    var noExt = StripKnownExt(baseName);
                    if (string.IsNullOrEmpty(noExt)) continue;

                    return $"{noExt}.log";
                }

                return fallback;
            }

            private static string StripKnownExt(string file)
            {
                // Remove one of these extensions if present
                var exts = new[] { ".exe", ".bat", ".cmd", ".sh", ".py" };
                foreach (var ext in exts)
                {
                    if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                        return Path.GetFileNameWithoutExtension(file);
                }
                // Also strip trailing path separators accidentally captured
                var name = file.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.GetFileNameWithoutExtension(name);
            }

            // Minimal tokenizer that respects single/double quotes
            private static List<string> Tokenize(string command)
            {
                var list = new List<string>();
                // matches either "double quoted", 'single quoted', or bare non-space
                var rx = new Regex("\"([^\"]*)\"|'([^']*)'|([^\\s]+)", RegexOptions.Compiled);
                foreach (Match m in rx.Matches(command))
                {
                    if (m.Groups[1].Success) list.Add(m.Groups[1].Value);
                    else if (m.Groups[2].Success) list.Add(m.Groups[2].Value);
                    else list.Add(m.Groups[3].Value);
                }
                return list;
            }
        }

        public static string AppendToLog(string logFile) =>
    $" 2>&1 | tee -a \"{logFile}\"";

        /// <summary>
        public static string SymbolicLinkCreatorBatch()
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal EnableExtensions EnableDelayedExpansion");
            sb.AppendLine("set \"SOURCE=%~dp0mesh\\constant\\polyMesh\"");
            sb.AppendLine("set \"CREATED=0\"");
            sb.AppendLine("set \"SKIPPED=0\"");
            sb.AppendLine("if not exist \"%SOURCE%\" (");
            sb.AppendLine("    echo ERROR: Source not found: %SOURCE%");
            sb.AppendLine("    pause");
            sb.AppendLine("    exit /b 1");
            sb.AppendLine(")");
            sb.AppendLine("for /D %%F in (*) do (");
            sb.AppendLine("    set \"N=%%~nxF\"");
            sb.AppendLine("    echo.!N!| findstr /R /C:\"^[0-9][0-9]*$\" >nul && (");
            sb.AppendLine("        if not exist \"%%~fF\\constant\" mkdir \"%%~fF\\constant\"");
            sb.AppendLine("        if exist \"%%~fF\\constant\\polyMesh\" rmdir /S /Q \"%%~fF\\constant\\polyMesh\"");
            sb.AppendLine("        mklink /J \"%%~fF\\constant\\polyMesh\" \"%SOURCE%\" >nul && (set /a CREATED+=1) || (set /a SKIPPED+=1)");
            sb.AppendLine("    )");
            sb.AppendLine(")");
            sb.AppendLine("echo Created: %CREATED%   Skipped/Failed: %SKIPPED%");
            sb.AppendLine("timeout /t 5 /nobreak >nul");
            return sb.ToString();
        }
    }
}