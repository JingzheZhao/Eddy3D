using Eddy.Properties;
using EddyLib;
using EddyLib.BCs;
using EddyLib.Docker;
using EddyLib.Radiation;
using EddyLib.Strings;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompVisProbesCustomWProbes : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.senary; }
        }

        //protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        //{
        //    base.AppendAdditionalComponentMenuItems(menu);
        //    Menu_AppendItem(menu, "No Culling of Probing Points", Menu_DoClick, true, !Culling);
        //}

        //private void Menu_DoClick(object sender, EventArgs e)
        //{
        //    Culling = !Culling;
        //    ExpireSolution(true);
        //}

        //public bool Culling = false;

        //public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        //{
        //    // First add our own field.
        //    writer.SetBoolean("Culling", Culling);

        //    // Then call the base class implementation.
        //    return base.Write(writer);
        //}

        //public override bool Read(GH_IO.Serialization.GH_IReader reader)
        //{
        //    // First read our own field.
        //    Culling = reader.GetBoolean("Culling");

        //    // Then call the base class implementation.
        //    return base.Read(reader);
        //}

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompVisProbesCustomWProbes()
          : base("WProbes", "WProbes", 
@"Wind Field Visualizer

Generates visualizations of the wind field, including vector arrows and streamlines, to understand flow patterns and identify problematic high-wind or stagnant zones.

" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Eddy Result", GH_ParamAccess.item);
            pManager.AddGenericParameter("Sensors", "Sen", "Wind sensors. Provide as [Mesh] or [RProbe]", GH_ParamAccess.tree);
            pManager.AddTextParameter("Name of instance", "Name", "Name of instance to be probed", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Interpolation Scheme", "IS", "Interpolation Scheme", GH_ParamAccess.item, 0);
            Param_Integer interpolationScheme = pManager[3] as Param_Integer;
            interpolationScheme.AddNamedValue("cell", 0);
            interpolationScheme.AddNamedValue("cellPoint", 1);
            interpolationScheme.AddNamedValue("cellPointFace", 2);
            interpolationScheme.AddNamedValue("pointMVC", 3);
            interpolationScheme.AddNamedValue("cellPatchConstrained", 4);

            pManager.AddBooleanParameter("Run", "Run", "Run the component.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Result file path", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        ///

        private bool canRun = true;

        public void probingComplete(object sender, System.EventArgs e)
        {
            //RhinoApp.WriteLine("Proping complete");
            canRun = false;
            this.ExpireSolution(true);
        }

        private string _dockerProbingError;

        private void RunDockerProbing(List<string> commands, string workDir)
        {
            _dockerProbingError = null;
            try
            {
                var runner = new DockerRunner();
                var bashCmd = DockerRunner.BuildCommandChain(commands);
                var launchLog = runner.RunInteractive(bashCmd, workDir);
                if (!string.IsNullOrWhiteSpace(launchLog) && launchLog.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _dockerProbingError = "Docker probing launch issue: " + launchLog;
                }
            }
            catch (System.Exception ex)
            {
                _dockerProbingError = "Docker probing launch exception: " + ex.Message;
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region Load Inputs

            OFResult RES = null;
            DA.GetData("Result", ref RES);

            // Show engine and report any Docker probing errors from previous run
            if (RES != null)
            {
                Message = RES.RunSettings.simEngine.ToString();
            }
            if (_dockerProbingError != null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _dockerProbingError);
                _dockerProbingError = null;
            }

            bool run = false;

            string probeNameByUser = "";
            int InterpolationScheme = 0;

            // ----------------------
            // Get the probing points
            // ----------------------
            List<EddyProbe> probes = new List<EddyProbe>();
            List<Mesh> probeMeshes = new List<Mesh>();
            //DA.GetDataList(4, probeMeshes);

            GH_Structure<IGH_Goo> GH_RProbeTree;
            if (!DA.GetDataTree("Sensors", out GH_RProbeTree)) { }
            foreach (GH_Path p in GH_RProbeTree.Paths)
            {
                foreach (IGH_Goo o in GH_RProbeTree.get_Branch(p))
                {
                    if (o != null)
                    {
                        Mesh m;
                        EddyProbe pr;
                        if (o.CastTo(out m))
                        { probeMeshes.Add(m); }
                        else if (o.CastTo(out pr))
                        { probes.Add(pr); }
                    }
                }
            }

            //

            DA.GetData("Name of instance", ref probeNameByUser);
            DA.GetData("Interpolation Scheme", ref InterpolationScheme);

            DA.GetData("Run", ref run);

            if (run && RES != null
                && RES.RunSettings.simEngine == SimEngine.Docker
                && (RES.Domain is OFCylDomain || RES.Domain is OFBoxDomain))
            {
                var scriptExt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".bat" : ".command";
                string scriptPath = Path.Combine(RES.WorkingDirectory, "Scripts", "copy_mesh_to_wind_dirs" + scriptExt);
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Docker mode is active. If you switched from BlueCFD to Docker after meshing, run \"" + scriptPath + "\" once to copy meshes into all integer wind-direction folders before probing.");
            }

            #endregion Load Inputs

            #region Instantiate types

            // ----------------------
            // New approach
            // ----------------------
            List<WProbe> WProbes = new List<WProbe>();

            if (RES.Domain.BCond.BCs.All(item => item is ABL))
            {
                foreach (var p in probes)
                {
                    WProbes.Add(new WProbe(p.Point, RES.Domain.BCond.WindDirections.Count)
                    {
                        WindDirections = RES.Domain.BCond.WindDirections.ToArray(),
                        Uref = RES.Domain.BCond.BCs.Select(val => (float)val.URef).ToArray(),
                        Z0 = RES.Domain.BCond.BCs.Select(val => (float)val.z0).ToArray(),
                        Zref = RES.Domain.BCond.BCs.Where(val => val is ABL).Select(val => (float)(val as ABL).zref).ToArray()
                    });
                }
            }
            else
            {
                foreach (var p in probes)
                {
                    WProbes.Add(new WProbe(p.Point, RES.Domain.BCond.WindDirections.Count)
                    {
                        WindDirections = RES.Domain.BCond.WindDirections.ToArray(),
                        Uref = RES.Domain.BCond.BCs.Select(val => (float)val.URef).ToArray(),
                        Z0 = RES.Domain.BCond.BCs.Select(val => (float)val.z0).ToArray(),
                    });
                }
            }

            // ----------------------
            // New approach
            // ----------------------

            List<Point3d> Probes = new List<Point3d>();
            foreach (var p in probes)
            {
                Probes.Add(p.Point);
            }
            int numberOfProbes = Probes.Count();

            GH_Structure<GH_Number> treeDouble = new GH_Structure<GH_Number>();
            GH_Structure<GH_Vector> treeVector = new GH_Structure<GH_Vector>();

            // OFFieldNew currField = null;

            #endregion Instantiate types

            #region Error handling

            bool meshExists = false;

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of points to the component.");
                return;
            }

            if (probeNameByUser == "")
            {
                probeNameByUser = "test";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, @"Please provide a unique name for this probing instance, otherwise a new instance will overwrite the results.");
            }
            if (Char.IsDigit((probeNameByUser).First()))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"Please make sure name doesn't start with digit.");
                return;
            }

            // Check if U file is in last iteration
            for (int i = 0; i < RES.Domain.BCond.WindDirections.Count; i++)
            {
                string path = Path.Combine(RES.WorkingDirectory, RES.Domain.BCond.WindDirections[i].ToString());
                string iter = Utilities.GetLastIterationFromDirectory(path).ToString();
                string fp = Path.Combine(RES.WorkingDirectory, RES.Domain.BCond.WindDirections[i].ToString(), iter, "U");

                if (!File.Exists(fp))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The last iteration """ + iter + @""" of the wind direction """ + RES.Domain.BCond.WindDirections[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                }
            }

            if (Directory.Exists(RES.MeshSettings.meshPolyMeshDir) == false)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The mesh folder does not exist. Please create a mesh first.");

                return;
            }
            else
            {
                if (Utilities.Directories.IsDirectoryEmpty(RES.MeshSettings.meshPolyMeshDir) == true)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The mesh folder is still empty. Can't retrieve probes from a mesh that does not exist.");

                    return;
                }
                else
                {
                    meshExists = true;
                }
            }

            int threshold = 10000;
            if (numberOfProbes > threshold)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Probing more than " + threshold + " points may slow down Grasshopper considerably.");
            }

            //if (RES.RunSettings.writeInterval > 1 && currField.FieldName == "total(p)_coeff")
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.ProbingFuncObjects(RES, currField));
            //}

            if (!(numberOfProbes > 0) || !meshExists)
            {
                if (!(numberOfProbes > 0))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The number of probes must be greater than 0.");
                }
                return;
            }

            #endregion Error handling

            try
            {
                StringBuilder command = new StringBuilder();
                var dockerProbeCmds = new List<string>();

                for (int i = 0; i < RES.Domain.BCond.WindDirections.Count; i++)
                {
                    // Check if mesh exists

                    string pathToPointFile = Path.Combine(RES.WorkingDirectory, RES.Domain.BCond.WindDirections[i].ToString(), "constant", "polyMesh", "points");
                    string currCase = Path.Combine(RES.WorkingDirectory, RES.Domain.BCond.WindDirections[i].ToString());

                    if (!File.Exists(pathToPointFile))
                    {
                        base.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.MeshDoesntExist(pathToPointFile));
                        return;
                    }

                    // If yes, write the dicts for both Docker and BlueCFD
                    string path = Path.Combine(RES.WorkingDirectory, RES.Domain.BCond.WindDirections[i].ToString(), "system", probeNameByUser);
                    File.WriteAllText(path, EddyLib.Strings.OFExecDicts.SampleProbesAllFields(Probes, probeNameByUser, Probing.ReformatIS(InterpolationScheme)));

                    if (RES.RunSettings.simEngine == SimEngine.Docker)
                    {
                        if (i > 0) dockerProbeCmds.Add("cd " + DockerConfig.CaseMountPoint);
                        string source = string.Format("{0}/mesh/constant/polyMesh", DockerConfig.CaseMountPoint);
                        string targetConstant = string.Format("{0}/{1}/constant", DockerConfig.CaseMountPoint, RES.Domain.BCond.WindDirections[i]);
                        string target = string.Format("{0}/polyMesh", targetConstant);
                        dockerProbeCmds.Add(string.Format("if [ ! -d \"{0}\" ]; then echo \"ERROR: Mesh source not found at {0}\"; exit 1; fi", source));
                        dockerProbeCmds.Add(string.Format("mkdir -p \"{0}\"", targetConstant));
                        dockerProbeCmds.Add(string.Format("rm -rf \"{0}\"", target));
                        dockerProbeCmds.Add(string.Format("cp -r \"{0}\" \"{1}\"", source, target));
                        dockerProbeCmds.Add(string.Format("cd {0}", RES.Domain.BCond.WindDirections[i]));
                        dockerProbeCmds.Add(string.Format("postProcess -func {0} -time {1}",
                            probeNameByUser, ProbingNew.GetLatestTime(currCase, RES)));
                    }
                    else
                    {
                        command.AppendLine(@"postProcess -case " + RES.Domain.BCond.WindDirections[i] + " -func " + probeNameByUser + @" -time " + ProbingNew.GetLatestTime(currCase, RES));
                    }
                }

                if (run == true && canRun == true)
                {
                    if (RES.RunSettings.simEngine == SimEngine.Docker)
                    {
                        RunDockerProbing(dockerProbeCmds, RES.WorkingDirectory);
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                            "Docker probing launched in an interactive terminal. Wait for it to finish, then set Run=false and recompute to load results.");
                        return;
                    }
                    else
                    {
                        var cmdArg = BatFiles.BlueCfdScriptBuilder.BuildBlueCfdBatch(new List<string> { command.ToString() }, RES.WorkingDirectory, RunMode.Canvas);
                        Utilities.StartProcess.StartProcessCMDNT(cmdArg, false, true, true, true, probingComplete);
                        return;
                    }
                }

                for (int i = 0; i < RES.Domain.BCond.WindDirections.Count; i++)
                {
                    string currentCaseDir = Path.Combine(RES.WorkingDirectory, RES.Domain.BCond.WindDirections[i].ToString());

                    foreach (field f in Enum.GetValues(typeof(field)))
                    {
                        var currField = new OFFieldNew(probeNameByUser, f, InterpolationScheme);
                        //  var currField = new OFFieldNew(probeNameByUser, field.U);

                        try
                        {
                            string pathToProbeFile = ProbingNew.GetPathToProbedResults(currentCaseDir, currField, RES);
                            if (File.Exists(pathToProbeFile))
                            {
                                if (currField.FieldType == fieldType.vector)
                                {
                                    ProbingNew Vectors = new ProbingNew(Probes, currentCaseDir, RES.WorkingDirectory, currField, RES.Domain.BCond.WindDirections[i], RES);

                                    for (int p = 0; p < numberOfProbes; p++)
                                    {
                                        //  WProbes[p].U[i] = new EddyVector(Vectors.ResultVec[p].Value);
                                        WProbes[p].SetOFFields(f, Vectors.ResultVec[p].Value, i);
                                    }
                                }
                                else
                                {
                                    ProbingNew Scalars = new ProbingNew(Probes, currentCaseDir, RES.WorkingDirectory, currField, RES.Domain.BCond.WindDirections[i], RES);

                                    for (int p = 0; p < numberOfProbes; p++)
                                    {
                                        WProbes[p].SetOFFields(f, (float)Scalars.ResultScalar[p].Value, i);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            base.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.FieldDoesntExist(currentCaseDir, currField));
                        }

                        // We must check if this exists before we construct the Probing object
                    }
                }
            }
            catch (Exception)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, EddyLib.Strings.ReturnMsg.ParsingFailed());

                //throw new System.ArgumentException("Parsing of the probes failed. This data does not exist yet. Please run the probing component.");
            }

            var respath = Path.GetFullPath(RES.WorkingDirectory);

            //DirectoryInfo parentDir = Directory.GetParent(respath.EndsWith("\\") ? respath : string.Concat(respath, "\\"));
            DirectoryInfo parentDir = Directory.GetParent(respath);

            string resultFilePath = Path.Combine(parentDir.Parent.FullName, "Wind.eddy." + probeNameByUser);

            WProbeResultProto res = new WProbeResultProto("probes", RES.WorkingDirectory, WProbes);
            res.WriteToFile(resultFilePath);

            DA.SetData(0, resultFilePath);

            canRun = true;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_visualProbs;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{8469A83F-B88C-431B-A2EC-EEF101AC69ED}");
    }
}
