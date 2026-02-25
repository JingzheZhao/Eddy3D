using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EddyLib.FluidX3D
{
    public sealed class FluidX3DCaseRunResult
    {
        public string SourceDirectory { get; set; }
        public string WorkingDirectory { get; set; }
        public string CaseRoot { get; set; }
        public string ExportDirectory { get; set; }
        public string LaunchScriptPath { get; set; }
        public bool Prepared { get; set; }
        public bool Launched { get; set; }
        public string Status { get; set; }
    }

    public static class FluidX3DCaseRunner
    {
        public static FluidX3DCaseRunResult Execute(
            OFBaseDomain domain,
            string workingDirectory,
            FluidX3DRunSettings runSettings,
            bool prepareCase,
            bool launchSolver)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            FluidX3DRunSettings settings = runSettings ?? new FluidX3DRunSettings();
            ValidateRunSettings(settings);

            string resolvedWorkingDirectory = Path.GetFullPath(
                DefaultDirectoriesAndPaths.ResolveWorkingDirectory(workingDirectory));
            Directory.CreateDirectory(resolvedWorkingDirectory);

            string sourceDirectory = ResolveSourceDirectory(settings.SourceDirectory);

            string caseRoot = Path.Combine(resolvedWorkingDirectory, "FluidX3D");
            string caseExportDirectory = Path.Combine(caseRoot, "VTK");
            string scriptsDirectory = Path.Combine(resolvedWorkingDirectory, "Scripts");
            string launchScriptPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(scriptsDirectory, "run_fluidx3d.bat")
                : Path.Combine(scriptsDirectory, "run_fluidx3d.command");

            StringBuilder status = new StringBuilder();
            status.AppendLine("Source: " + sourceDirectory);
            status.AppendLine("Working: " + resolvedWorkingDirectory);
            status.AppendLine("Case: " + caseRoot);

            FluidX3DCaseRunResult result = new FluidX3DCaseRunResult
            {
                SourceDirectory = sourceDirectory,
                WorkingDirectory = resolvedWorkingDirectory,
                CaseRoot = caseRoot,
                ExportDirectory = caseExportDirectory,
                LaunchScriptPath = launchScriptPath,
                Prepared = false,
                Launched = false,
                Status = string.Empty
            };

            if (prepareCase)
            {
                if (!FluidX3DAblWorkflow.IsValidSourceDirectory(sourceDirectory))
                {
                    throw new InvalidOperationException(
                        "FluidX3D source not found. Install it via 'Install Engines', or set EDDY_FLUIDX3D_SOURCE.");
                }

                Directory.CreateDirectory(caseRoot);
                ResetDirectoryContents(caseExportDirectory);

                ABL abl = ExtractAblBoundaryCondition(domain);
                Mesh buildingMesh = ExtractBuildingMesh(domain);
                BoundingBox bbox = buildingMesh.GetBoundingBox(true);
                if (!bbox.IsValid)
                {
                    throw new InvalidOperationException("Unable to compute valid building bounding box.");
                }

                DeriveDomain(
                    bbox,
                    settings.GroundZ,
                    out double groundZUsed,
                    out double lx,
                    out double ly,
                    out double lz,
                    out double xMin,
                    out double yMin);

                Vector3d flowDirection = ResolveAblFlowDirection(abl);

                FluidX3DAblSettings ablSettings = new FluidX3DAblSettings
                {
                    MemoryMb = settings.MemoryMb,
                    Uref = abl.URef,
                    Zref = abl.zref,
                    Z0 = abl.z0,
                    FlowDirectionX = flowDirection.X,
                    FlowDirectionY = flowDirection.Y,
                    SimSeconds = settings.SimSeconds,
                    ExportIntervalSeconds = settings.ExportIntervalSeconds,
                    DomainLx = lx,
                    DomainLy = ly,
                    DomainLz = lz
                };
                ablSettings.BuildingStlFiles.Add("building_000.stl");

                FluidX3DAblPrepareResult prepareResult =
                    FluidX3DAblWorkflow.PrepareCase(sourceDirectory, resolvedWorkingDirectory, ablSettings);

                ExportBuildingStl(
                    buildingMesh,
                    prepareResult.CaseRoot,
                    xMin,
                    yMin,
                    groundZUsed,
                    ablSettings.BuildingStlFiles[0]);

                WriteProbeTransformMetadata(
                    caseExportDirectory,
                    xMin,
                    yMin,
                    groundZUsed,
                    ablSettings.ExportIntervalSeconds);

                // Keep metadata in source export as well for direct/manual probing.
                WriteProbeTransformMetadata(
                    prepareResult.ExportDirectory,
                    xMin,
                    yMin,
                    groundZUsed,
                    ablSettings.ExportIntervalSeconds);

                result.CaseRoot = caseRoot;
                result.ExportDirectory = caseExportDirectory;
                result.LaunchScriptPath = prepareResult.LaunchScriptPath;
                result.Prepared = true;

                status.AppendLine("Prepared FluidX3D case.");
                status.AppendLine("Engine source case: " + prepareResult.CaseRoot);
                status.AppendLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Domain (Lx/Ly/Lz) = {0:0.###}/{1:0.###}/{2:0.###} m",
                        lx, ly, lz));
                status.AppendLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Flow dir (x,y) = ({0:0.###}, {1:0.###})",
                        flowDirection.X,
                        flowDirection.Y));
                status.AppendLine("Launch script: " + prepareResult.LaunchScriptPath);
                status.AppendLine("VTK dir: " + caseExportDirectory);
                status.AppendLine("Engine export dir: " + prepareResult.ExportDirectory);

                List<int> windDirections = domain.BCond?.WindDirections;
                if (windDirections != null && windDirections.Count > 1)
                {
                    status.AppendLine(
                        "Note: FluidX3D currently runs a single case per execution. "
                        + "Using the first ABL boundary condition.");
                }
            }
            else
            {
                status.AppendLine("Prepare skipped.");
            }

            if (launchSolver)
            {
                if (TryLaunchScript(result.LaunchScriptPath, out string launchMessage))
                {
                    result.Launched = true;
                    status.AppendLine(launchMessage);
                }
                else
                {
                    throw new InvalidOperationException(launchMessage);
                }
            }
            else
            {
                status.AppendLine("Run skipped.");
            }

            result.Status = status.ToString().Trim();
            return result;
        }

        private static void ResetDirectoryContents(string directory)
        {
            Directory.CreateDirectory(directory);

            foreach (string file in Directory.GetFiles(directory))
            {
                File.Delete(file);
            }

            foreach (string subDir in Directory.GetDirectories(directory))
            {
                Directory.Delete(subDir, true);
            }
        }

        private static string ResolveSourceDirectory(string sourceOverride)
        {
            List<string> candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(sourceOverride))
            {
                candidates.Add(Path.GetFullPath(sourceOverride.Trim()));
            }

            string fromEnvironment = Environment.GetEnvironmentVariable("EDDY_FLUIDX3D_SOURCE");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                candidates.Add(Path.GetFullPath(fromEnvironment.Trim()));
            }

            candidates.Add(Path.GetFullPath(FluidX3DAblWorkflow.GetDefaultSourceDirectory()));

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < candidates.Count; i++)
            {
                string candidate = candidates[i];
                if (string.IsNullOrWhiteSpace(candidate) || !seen.Add(candidate))
                {
                    continue;
                }

                if (Directory.Exists(candidate) && FluidX3DAblWorkflow.IsValidSourceDirectory(candidate))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException(
                "FluidX3D source not found. Set EDDY_FLUIDX3D_SOURCE to a valid FluidX3D clone, "
                + "or install under " + FluidX3DAblWorkflow.GetDefaultSourceDirectory() + ".");
        }

        private static void ValidateRunSettings(FluidX3DRunSettings settings)
        {
            if (settings.MemoryMb < 256)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.MemoryMb), "MemoryMb must be >= 256.");
            }

            if (settings.SimSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.SimSeconds), "SimSeconds must be > 0.");
            }

            if (settings.ExportIntervalSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settings.ExportIntervalSeconds),
                    "ExportIntervalSeconds must be > 0.");
            }
        }

        private static ABL ExtractAblBoundaryCondition(OFBaseDomain domain)
        {
            if (domain?.BCond?.BCs == null || domain.BCond.BCs.Count == 0)
            {
                throw new InvalidOperationException("Domain does not contain boundary conditions.");
            }

            ABL abl = domain.BCond.BCs.OfType<ABL>().FirstOrDefault();
            if (abl == null)
            {
                throw new InvalidOperationException(
                    "FluidX3D requires an ABL boundary condition. Connect the 'ABL Flow' output to your domain.");
            }

            if (abl.URef <= 0.0 || abl.zref <= 0.0 || abl.z0 <= 0.0)
            {
                throw new InvalidOperationException("Invalid ABL parameters (Uref, zref, z0).");
            }

            return abl;
        }

        private static Vector3d ResolveAblFlowDirection(ABL abl)
        {
            Vector3d dir = abl?.flowDir ?? Vector3d.Zero;
            if (dir.IsTiny())
            {
                dir = Utilities.Dir2Vec(abl?.windDir ?? 0);
            }

            dir.Z = 0.0;
            if (!dir.Unitize() || dir.IsTiny())
            {
                dir = new Vector3d(0.0, -1.0, 0.0);
            }

            return dir;
        }

        private static Mesh ExtractBuildingMesh(OFBaseDomain domain)
        {
            Mesh source = domain?.BuildingGeometry;
            if (source == null || source.Faces.Count == 0 || source.Vertices.Count == 0)
            {
                throw new InvalidOperationException("Domain does not contain valid building geometry.");
            }

            Mesh mesh = source.DuplicateMesh();
            mesh.Faces.ConvertQuadsToTriangles();
            mesh.UnifyNormals();
            mesh.Normals.ComputeNormals();
            mesh.FaceNormals.ComputeFaceNormals();
            mesh.Compact();
            return mesh;
        }

        private static void DeriveDomain(
            BoundingBox bbox,
            double groundZInput,
            out double domainGroundZ,
            out double lx,
            out double ly,
            out double lz,
            out double xMin,
            out double yMin)
        {
            domainGroundZ = Math.Min(groundZInput, bbox.Min.Z);
            double height = Math.Max(1.0, bbox.Max.Z - domainGroundZ);

            double side = Math.Max(30.0, 2.0 * height);
            double upwind = Math.Max(30.0, 2.0 * height);
            double downwind = Math.Max(60.0, 4.0 * height);
            double top = Math.Max(40.0, 3.0 * height);

            xMin = bbox.Min.X - side;
            double xMax = bbox.Max.X + side;
            yMin = bbox.Min.Y - upwind;
            double yMax = bbox.Max.Y + downwind;
            double zMax = bbox.Max.Z + top;

            lx = Math.Max(40.0, xMax - xMin);
            ly = Math.Max(60.0, yMax - yMin);
            lz = Math.Max(40.0, zMax - domainGroundZ);
        }

        private static void ExportBuildingStl(
            Mesh sourceMesh,
            string caseRoot,
            double xMin,
            double yMin,
            double groundZ,
            string stlName)
        {
            string stlDir = Path.Combine(caseRoot, "stl");
            Directory.CreateDirectory(stlDir);

            Mesh mesh = sourceMesh.DuplicateMesh();
            Transform toLocal = Transform.Translation(-xMin, -yMin, -groundZ);
            mesh.Transform(toLocal);
            mesh.Compact();

            STLExport.ExportBinary(Path.Combine(stlDir, stlName), mesh);
        }

        private static void WriteProbeTransformMetadata(
            string exportDir,
            double xMin,
            double yMin,
            double groundZ,
            double exportIntervalSeconds)
        {
            Directory.CreateDirectory(exportDir);
            string transformPath = Path.Combine(exportDir, "eddy_probe_transform.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Eddy3D FluidX3D probe transform metadata");
            sb.AppendLine("x_min=" + xMin.ToString("0.###########", CultureInfo.InvariantCulture));
            sb.AppendLine("y_min=" + yMin.ToString("0.###########", CultureInfo.InvariantCulture));
            sb.AppendLine("ground_z=" + groundZ.ToString("0.###########", CultureInfo.InvariantCulture));
            sb.AppendLine(
                "export_interval_seconds="
                + exportIntervalSeconds.ToString("0.###########", CultureInfo.InvariantCulture));
            File.WriteAllText(transformPath, sb.ToString());
        }

        private static bool TryLaunchScript(string scriptPath, out string message)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                message = "Launch script path is empty.";
                return false;
            }

            string fullScriptPath = Path.GetFullPath(scriptPath);
            if (!File.Exists(fullScriptPath))
            {
                message = "Launch script not found: " + fullScriptPath;
                return false;
            }

            try
            {
                string workingDirectory = Path.GetDirectoryName(fullScriptPath) ?? string.Empty;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/k \"" + fullScriptPath + "\"",
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    EnsureExecutableUnix(fullScriptPath);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "/usr/bin/open",
                        Arguments = "-a Terminal \"" + fullScriptPath + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = "\"" + fullScriptPath + "\"",
                        UseShellExecute = true
                    });
                }

                message = "Launched: " + fullScriptPath;
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to launch script: " + ex.Message;
                return false;
            }
        }

        private static void EnsureExecutableUnix(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = "+x \"" + filePath + "\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            }))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start chmod for " + filePath);
                }

                string stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("chmod failed for " + filePath + ": " + stdErr.Trim());
                }
            }
        }
    }
}
