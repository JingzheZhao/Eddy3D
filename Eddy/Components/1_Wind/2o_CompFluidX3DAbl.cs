using Eddy.Properties;
using EddyLib;
using EddyLib.FluidX3D;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Eddy
{
    public class FluidX3DAbl_Component : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

        public FluidX3DAbl_Component()
          : base(
              GH_Strings.FluidX3D.Name,
              GH_Strings.FluidX3D.Nick,
              GH_Strings.FluidX3D.Desc + EddyVersion.toString(),
              EddyVersion.Name,
              "1 | Wind")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter(
                GH_Strings.FluidX3D.Buildings,
                GH_Strings.FluidX3D.BuildingsNick,
                GH_Strings.FluidX3D.BuildingsDesc,
                GH_ParamAccess.list);
            pManager[0].Optional = true;

            pManager.AddTextParameter(
                GH_Strings.FluidX3D.SourceDir,
                GH_Strings.FluidX3D.SourceDirNick,
                GH_Strings.FluidX3D.SourceDirDesc,
                GH_ParamAccess.item,
                FluidX3DAblWorkflow.GetDefaultSourceDirectory());
            pManager[1].Optional = true;

            pManager.AddTextParameter(
                GH_Strings.FluidX3D.WorkingDir,
                GH_Strings.FluidX3D.WorkingDirNick,
                GH_Strings.FluidX3D.WorkingDirDesc,
                GH_ParamAccess.item,
                DefaultDirectoriesAndPaths.CasesDir);
            pManager[2].Optional = true;

            pManager.AddBooleanParameter(
                GH_Strings.FluidX3D.CloneUpdate,
                GH_Strings.FluidX3D.CloneUpdateNick,
                GH_Strings.FluidX3D.CloneUpdateDesc,
                GH_ParamAccess.item,
                true);

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3D.MemoryMb,
                GH_Strings.FluidX3D.MemoryMbNick,
                GH_Strings.FluidX3D.MemoryMbDesc,
                GH_ParamAccess.item,
                1000);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3D.Uref,
                GH_Strings.FluidX3D.UrefNick,
                GH_Strings.FluidX3D.UrefDesc,
                GH_ParamAccess.item,
                5.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3D.Zref,
                GH_Strings.FluidX3D.ZrefNick,
                GH_Strings.FluidX3D.ZrefDesc,
                GH_ParamAccess.item,
                10.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3D.Z0,
                GH_Strings.FluidX3D.Z0Nick,
                GH_Strings.FluidX3D.Z0Desc,
                GH_ParamAccess.item,
                0.1);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3D.SimTime,
                GH_Strings.FluidX3D.SimTimeNick,
                GH_Strings.FluidX3D.SimTimeDesc,
                GH_ParamAccess.item,
                30.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3D.ExportEvery,
                GH_Strings.FluidX3D.ExportEveryNick,
                GH_Strings.FluidX3D.ExportEveryDesc,
                GH_ParamAccess.item,
                10.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3D.GroundZ,
                GH_Strings.FluidX3D.GroundZNick,
                GH_Strings.FluidX3D.GroundZDesc,
                GH_ParamAccess.item,
                0.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3D.MeshEdge,
                GH_Strings.FluidX3D.MeshEdgeNick,
                GH_Strings.FluidX3D.MeshEdgeDesc,
                GH_ParamAccess.item,
                0.0);
            pManager[11].Optional = true;

            pManager.AddBooleanParameter(
                GH_Strings.FluidX3D.Prepare,
                GH_Strings.FluidX3D.PrepareNick,
                GH_Strings.FluidX3D.PrepareDesc,
                GH_ParamAccess.item,
                false);

            pManager.AddBooleanParameter(
                GH_Strings.FluidX3D.Run,
                GH_Strings.FluidX3D.RunNick,
                GH_Strings.FluidX3D.RunDesc,
                GH_ParamAccess.item,
                false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(
                GH_Strings.FluidX3D.CaseDir,
                GH_Strings.FluidX3D.CaseDirNick,
                GH_Strings.FluidX3D.CaseDirDesc,
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                GH_Strings.FluidX3D.LaunchScript,
                GH_Strings.FluidX3D.LaunchScriptNick,
                GH_Strings.FluidX3D.LaunchScriptDesc,
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                GH_Strings.FluidX3D.ExportDir,
                GH_Strings.FluidX3D.ExportDirNick,
                GH_Strings.FluidX3D.ExportDirDesc,
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                GH_Strings.FluidX3D.Status,
                GH_Strings.FluidX3D.StatusNick,
                GH_Strings.FluidX3D.StatusDesc,
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Message = "Experimental";

            List<GeometryBase> inputGeometry = new List<GeometryBase>();
            DA.GetDataList(0, inputGeometry);

            string sourceDirInput = FluidX3DAblWorkflow.GetDefaultSourceDirectory();
            DA.GetData(1, ref sourceDirInput);

            string workingDirInput = string.Empty;
            DA.GetData(2, ref workingDirInput);
            string workingDir = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(workingDirInput);
            Directory.CreateDirectory(workingDir);

            bool cloneUpdate = true;
            int memoryMb = 1000;
            double uref = 5.0;
            double zref = 10.0;
            double z0 = 0.1;
            double simTime = 30.0;
            double exportEvery = 10.0;
            double groundZInput = 0.0;
            double meshEdge = 0.0;
            bool prepare = false;
            bool run = false;

            DA.GetData(3, ref cloneUpdate);
            DA.GetData(4, ref memoryMb);
            DA.GetData(5, ref uref);
            DA.GetData(6, ref zref);
            DA.GetData(7, ref z0);
            DA.GetData(8, ref simTime);
            DA.GetData(9, ref exportEvery);
            DA.GetData(10, ref groundZInput);
            DA.GetData(11, ref meshEdge);
            DA.GetData(12, ref prepare);
            DA.GetData(13, ref run);

            string sourceDir = string.IsNullOrWhiteSpace(sourceDirInput)
                ? FluidX3DAblWorkflow.GetDefaultSourceDirectory()
                : sourceDirInput.Trim();
            sourceDir = Path.GetFullPath(sourceDir);

            string caseDir = Path.Combine(workingDir, "FluidX3D");
            string scriptsDir = Path.Combine(workingDir, "Scripts");
            string scriptPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(scriptsDir, "run_fluidx3d.bat")
                : Path.Combine(scriptsDir, "run_fluidx3d.command");
            string exportDir = Path.Combine(caseDir, "bin", "export");

            StringBuilder status = new StringBuilder();
            status.AppendLine("Source: " + sourceDir);
            status.AppendLine("Working: " + workingDir);
            status.AppendLine("FluidX3D case: " + caseDir);
            status.AppendLine("Launch script: " + scriptPath);
            status.AppendLine("Export dir: " + exportDir);

            bool needsSourceSetup = prepare || run;
            if (needsSourceSetup)
            {
                if (cloneUpdate)
                {
                    try
                    {
                        FluidX3DAblWorkflow.EnsureSourceRepository(sourceDir, true, out string cloneStatus);
                        status.AppendLine(cloneStatus);
                    }
                    catch (Exception ex)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                        status.AppendLine("Source setup failed: " + ex.Message);
                        WriteOutputs(DA, caseDir, scriptPath, exportDir, status.ToString());
                        return;
                    }
                }
                else if (!FluidX3DAblWorkflow.IsValidSourceDirectory(sourceDir))
                {
                    string message = "Invalid FluidX3D source directory: " + sourceDir
                        + ". Enable Clone/Update or provide a valid FluidX3D clone.";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
                    status.AppendLine(message);
                    WriteOutputs(DA, caseDir, scriptPath, exportDir, status.ToString());
                    return;
                }
            }
            else
            {
                status.AppendLine("Set Prepare=true or Run=true to trigger source clone/update checks.");
            }

            bool hasBuildingGeometry = TryBuildMeshes(inputGeometry, meshEdge, out List<Mesh> buildingMeshes, out int skippedGeometry);
            if (skippedGeometry > 0)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    skippedGeometry.ToString(CultureInfo.InvariantCulture)
                    + " unsupported/empty geometry item(s) were skipped.");
            }

            if (prepare)
            {
                try
                {
                    FluidX3DAblSettings settings = new FluidX3DAblSettings
                    {
                        MemoryMb = memoryMb,
                        Uref = uref,
                        Zref = zref,
                        Z0 = z0,
                        SimSeconds = simTime,
                        ExportIntervalSeconds = exportEvery
                    };

                    double xMin = 0.0;
                    double yMin = 0.0;
                    double groundZUsed = Math.Min(groundZInput, 0.0);

                    if (hasBuildingGeometry)
                    {
                        BoundingBox bbox = GetCombinedBoundingBox(buildingMeshes);
                        if (!bbox.IsValid)
                        {
                            throw new InvalidOperationException("Failed to compute a valid bounding box from input geometry.");
                        }

                        DeriveDomain(
                            bbox,
                            groundZInput,
                            out groundZUsed,
                            out double lx,
                            out double ly,
                            out double lz,
                            out xMin,
                            out yMin);

                        settings.DomainLx = lx;
                        settings.DomainLy = ly;
                        settings.DomainLz = lz;

                        status.AppendLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "Derived domain (m): Lx={0:0.###}, Ly={1:0.###}, Lz={2:0.###}",
                            lx, ly, lz));
                        status.AppendLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "Ground Z used: {0:0.###} m",
                            groundZUsed));
                    }
                    else
                    {
                        status.AppendLine("No building geometry connected. Using fallback demo boxes in generated setup.");
                    }

                    if (hasBuildingGeometry)
                    {
                        settings.BuildingStlFiles.Clear();
                        for (int i = 0; i < buildingMeshes.Count; i++)
                        {
                            settings.BuildingStlFiles.Add("building_" + i.ToString("D3", CultureInfo.InvariantCulture) + ".stl");
                        }
                    }

                    FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(sourceDir, workingDir, settings);
                    caseDir = result.CaseRoot;
                    scriptPath = result.LaunchScriptPath;
                    exportDir = result.ExportDirectory;

                    if (hasBuildingGeometry)
                    {
                        ExportBuildingStls(buildingMeshes, result.CaseRoot, xMin, yMin, groundZUsed, settings.BuildingStlFiles);
                        status.AppendLine("Exported building STL files: " + settings.BuildingStlFiles.Count.ToString(CultureInfo.InvariantCulture));
                    }

                    status.AppendLine("Prepare completed.");
                    status.AppendLine("Generated setup: " + result.SetupPath);
                    status.AppendLine("Generated defines: " + result.DefinesPath);
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                    status.AppendLine("Prepare failed: " + ex.Message);
                }
            }
            else
            {
                status.AppendLine("Set Prepare=true once to generate setup, scripts, and STL files.");
            }

            if (run)
            {
                if (!File.Exists(scriptPath))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Launch script not found. Run once with Prepare=true first.");
                    status.AppendLine("Run skipped: launch script missing.");
                }
                else if (TryLaunchScript(scriptPath, out string launchMessage))
                {
                    status.AppendLine(launchMessage);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, launchMessage);
                    status.AppendLine("Run failed: " + launchMessage);
                }
            }

            WriteOutputs(DA, caseDir, scriptPath, exportDir, status.ToString());
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

        private static void ExportBuildingStls(
            IList<Mesh> sourceMeshes,
            string caseRoot,
            double xMin,
            double yMin,
            double groundZ,
            IList<string> stlFileNames)
        {
            string stlDir = Path.Combine(caseRoot, "stl");
            Directory.CreateDirectory(stlDir);

            Transform toLocal = Transform.Translation(-xMin, -yMin, -groundZ);

            for (int i = 0; i < sourceMeshes.Count; i++)
            {
                Mesh mesh = sourceMeshes[i].DuplicateMesh();
                mesh.Transform(toLocal);
                mesh.Compact();

                string name = stlFileNames[i];
                string path = Path.Combine(stlDir, name);
                STLExport.ExportBinary(path, mesh);
            }
        }

        private static bool TryBuildMeshes(
            IList<GeometryBase> geometry,
            double edgeLength,
            out List<Mesh> meshes,
            out int skipped)
        {
            meshes = new List<Mesh>();
            skipped = 0;

            if (geometry == null || geometry.Count == 0)
            {
                return false;
            }

            MeshingParameters mp = new MeshingParameters();
            if (edgeLength > 0.0)
            {
                mp.MaximumEdgeLength = edgeLength;
                mp.MinimumEdgeLength = edgeLength;
            }

            foreach (GeometryBase geo in geometry)
            {
                if (geo == null)
                {
                    skipped++;
                    continue;
                }

                if (geo is Mesh mesh)
                {
                    AddMesh(mesh.DuplicateMesh(), meshes);
                    continue;
                }

                Brep brep = null;
                if (geo is Brep directBrep)
                {
                    brep = directBrep;
                }
                else if (geo is Extrusion extrusion)
                {
                    brep = extrusion.ToBrep();
                }
                else if (geo is Surface surface)
                {
                    brep = surface.ToBrep();
                }

                if (brep != null)
                {
                    Mesh[] brepMeshes = Mesh.CreateFromBrep(brep, mp);
                    if (brepMeshes == null || brepMeshes.Length == 0)
                    {
                        skipped++;
                        continue;
                    }

                    Mesh joined = new Mesh();
                    for (int i = 0; i < brepMeshes.Length; i++)
                    {
                        if (brepMeshes[i] != null)
                        {
                            joined.Append(brepMeshes[i]);
                        }
                    }

                    AddMesh(joined, meshes);
                    continue;
                }

                skipped++;
            }

            return meshes.Count > 0;
        }

        private static void AddMesh(Mesh mesh, IList<Mesh> output)
        {
            if (mesh == null || mesh.Faces.Count == 0 || mesh.Vertices.Count == 0)
            {
                return;
            }

            mesh.Faces.ConvertQuadsToTriangles();
            mesh.UnifyNormals();
            mesh.Normals.ComputeNormals();
            mesh.FaceNormals.ComputeFaceNormals();
            mesh.Compact();

            output.Add(mesh);
        }

        private static BoundingBox GetCombinedBoundingBox(IList<Mesh> meshes)
        {
            BoundingBox bbox = BoundingBox.Empty;
            for (int i = 0; i < meshes.Count; i++)
            {
                BoundingBox current = meshes[i].GetBoundingBox(true);
                if (!current.IsValid)
                {
                    continue;
                }

                if (!bbox.IsValid)
                {
                    bbox = current;
                }
                else
                {
                    bbox.Union(current);
                }
            }

            return bbox;
        }

        private static void WriteOutputs(IGH_DataAccess DA, string caseDir, string scriptPath, string exportDir, string status)
        {
            DA.SetData(0, caseDir);
            DA.SetData(1, scriptPath);
            DA.SetData(2, exportDir);
            DA.SetData(3, status.Trim());
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

                    string makeShPath = Path.GetFullPath(Path.Combine(workingDirectory, "..", "FluidX3D", "make.sh"));
                    if (File.Exists(makeShPath))
                    {
                        EnsureExecutableUnix(makeShPath);
                    }

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

        protected override System.Drawing.Bitmap Icon => Resources.Eddy_abl;

        public override Guid ComponentGuid => new Guid("{6CF70F06-1A42-4968-B8B1-5375264FE291}");
    }
}
