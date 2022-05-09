using Eddy.Properties;
using EddyLib;
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
using System.Text;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompVisProbesCustom : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.senary | GH_Exposure.obscure; }
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
        public CompVisProbesCustom()
          : base("Probing", "Probing", "Probe the simulation." + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Eddy Result", GH_ParamAccess.item);
            pManager.AddPointParameter("Probing points", "Points", "List of probing points", GH_ParamAccess.list);
            pManager.AddTextParameter("Name of instance", "Name", "Name of instance to be probed", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Interpolation Scheme", "IS", "Interpolation Scheme", GH_ParamAccess.item, 3);
            Param_Integer interpolationScheme = pManager[3] as Param_Integer;
            interpolationScheme.AddNamedValue("cell", 0);
            interpolationScheme.AddNamedValue("cellPoint", 1);
            interpolationScheme.AddNamedValue("cellPointFace", 2);
            interpolationScheme.AddNamedValue("pointMVC", 3);
            interpolationScheme.AddNamedValue("cellPatchConstrained", 4);

            pManager.AddIntegerParameter("Name of field", "Field", "Name of field to be probed", GH_ParamAccess.item, 0);
            Param_Integer param = pManager[4] as Param_Integer;
            param.AddNamedValue("Velocity (U) [m/s]", 0);
            param.AddNamedValue("Pressure coefficient (total(p)_coeff) [-]", 1);
            param.AddNamedValue("Pressure (p) [m^2/s^2]", 2);
            param.AddNamedValue("Turbulent dissipation rate (epsilon) [m^2/s^3]", 3);
            param.AddNamedValue("Scale of turbulence (omega) [1/s] ", 4);
            param.AddNamedValue("Turbulent kinetic energy (k) [m^2/s^2]", 5);
            param.AddNamedValue("Turbulent viscosity (nut) [m^2/s]", 6);
            param.AddNamedValue("Mass flow (phi) [m^3/s]", 7);
            param.AddNamedValue("Age of air (aoa) [s]", 8);
            param.AddNamedValue("Particle Concentration (covid19) []", 9);
            // Todo Zoe

            //pManager.AddIntegerParameter("FieldType", "FieldType", "FieldType", GH_ParamAccess.item, 1);
            //Param_Integer param2 = pManager[4] as Param_Integer;
            //param2.AddNamedValue("Scalar", 0);
            //param2.AddNamedValue("Vector", 1);

            pManager.AddBooleanParameter("Run", "Run", "Run the component.", GH_ParamAccess.item, false);

            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Probing points", "Probes", "List of probing points (caution: maybe culled)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Probing result", "Res", "Probed results [DataTree] where the [branches] are the wind directions and the [items] are the values for each probing point.", GH_ParamAccess.tree);
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

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region Load Inputs

            // mode to select simulation environment
            //if (Culling) { Message = "Cull Points"; }
            //else { Message = "No Culling"; }

            OFResult RES = null;
            DA.GetData(0, ref RES);

            List<Point3d> listOfPoints = new List<Point3d>();

            bool run = false;
            int OFFieldInt = 0;
            string probeNameByUser = "";
            int InterpolationScheme = 0;

            DA.GetDataList(1, listOfPoints);

            DA.GetData(2, ref probeNameByUser);

            DA.GetData(3, ref InterpolationScheme);
            DA.GetData(4, ref OFFieldInt);

            //DA.GetData(4, ref fieldType);
            DA.GetData(5, ref run);

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

            //Discard points outside
            //if (Culling)
            //{
            //    listOfPoints = Utilities.DiscardPoints(listOfPoints, RES.Domain.BuildingGeometry);
            //}

            int numberOfProbes = listOfPoints.Count();

            #endregion Load Inputs

            GH_Structure<GH_Number> treeDouble = new GH_Structure<GH_Number>();
            GH_Structure<GH_Vector> treeVector = new GH_Structure<GH_Vector>();

            string OFField = EddyLib.OFField.ReformatOFFields(OFFieldInt);
            OFField currField = new OFField(OFField, probeNameByUser, InterpolationScheme);

            #region Error handling

            bool meshExists = false;

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of points to the component.");
                return;
            }

            // Check if U file is in last iteration
            for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
            {
                string path = RES.WorkingDirectory + @"\" + RES.Domain.BCond.windDirs[i];
                string iter = Utilities.GetLastIterationFromDirectory(path).ToString();
                string fp = RES.WorkingDirectory + @"\" + RES.Domain.BCond.windDirs[i] + @"\" + iter + @"\U";

                if (!File.Exists(fp))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The last iteration """ + iter + @""" of the wind direction """ + RES.Domain.BCond.windDirs[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                }
            }

            // Export probes file
            //File.WriteAllText(Path.Combine(RES.WorkingDirectory + "\\" + "run_probes.bat"), EddyLib.Strings.BatFiles.Run_Probes(RES.Domain, RES.MeshSettings));

            // export pts file for Daysim
            if (!Directory.Exists(RES.WorkingDirectory + @"Rad\"))
            {
                Directory.CreateDirectory(RES.WorkingDirectory + @"Rad\");
            }

            RadianceFiles.writePTS(RES.WorkingDirectory + @"\Rad\sensors.pts", listOfPoints);

            string meshDir = "";

            if (RES.Domain is OFCylDomain)
            {
                meshDir = RES.MeshSettings.meshPolyMeshDir;
            }
            else if (RES.Domain is OFBoxDomain)
            {
                meshDir = RES.WorkingDirectory + RES.Domain.BCond.windDirs[0] + @"\constant\polyMesh";
            }

            if (Directory.Exists(meshDir) == false)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The mesh folder " + meshDir + " does not exist. Please create a mesh first.");

                //throw new System.ArgumentException("The mesh folder is does not exist. Please create a mesh first.");
                return;
            }
            else
            {
                if (Utilities.Directories.IsDirectoryEmpty(meshDir) == true)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The mesh folder " + meshDir + @" is empty. Can't retrieve probes from a mesh that does not exist.");

                    // throw new System.ArgumentException("The mesh folder is empty. Can't retrieve probes from a mesh that does not exist.");
                    return;
                }
                else
                {
                    meshExists = true;
                }
            }

            int threshold = 100000;
            if (listOfPoints.Count > threshold)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Probing more than " + threshold + " points may slow Grasshopper down considerably.");
            }

            if (RES.RunSettings.writeInterval > 1 && currField.FieldName == "total(p)_coeff")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.ProbingFuncObjects(RES, currField));
            }

            #endregion Error handling

            if (numberOfProbes > 0 && meshExists)
            {
                try
                {
                    StringBuilder command = new StringBuilder();

                    for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                    {
                        // Check if mesh exists

                        string pathToPointFile = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\constant\polyMesh\points";
                        string currCase = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i];

                        if (!File.Exists(pathToPointFile))
                        {
                            base.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.MeshDoesntExist(pathToPointFile));
                            return;
                        }

                        // If yes, write the dicts for both Docker and BlueCFD
                        string path = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + probeNameByUser;
                        File.WriteAllText(path, EddyLib.Strings.OFExecDicts.SampleProbes(listOfPoints, currField));

                        if (RES.RunSettings.simEngine == SimEngine.Docker)
                        {
                            command.Append(@"postProcess -func " + currField.ProbeName + @" -time " + Probing.GetLatestTime(currCase, RES, currField) + @"| tee  " + RES.Domain.BCond.windDirs[i] + @"/log_probes;");
                        }
                        else
                        {// piping interfers with the windows executables which rely on linux syntax. Need to find a way to load environment variables of entire linux env
                            // Todo: check here if we need a semicolon to sepaate the command
                            // from the suffix
                            command.AppendLine(@"postProcess -case " + RES.Domain.BCond.windDirs[i] + " -func " + probeNameByUser + @" -time " + Probing.GetLatestTime(currCase, RES, currField));
                        }
                    }

                    if (run == true && canRun == true)
                    {
                        if (RES.RunSettings.simEngine == SimEngine.Docker)
                        {
                            var arg = BatFiles.DockerPrefixPath(RES.Domain, RES.MeshSettings, RES.RunSettings, OFExecutionMode.Simulation) + command;
                            Utilities.StartProcess.StartProcessCMDNT(arg, false, true, false, true, probingComplete);
                        }
                        else
                        {
                            var cmdArg = BatFiles.TempBlueCFD(new List<string> { command.ToString() }, RES.WorkingDirectory, RunMode.Canvas);
                            Utilities.StartProcess.StartProcessCMDNT(cmdArg, false, true, true, true, probingComplete);
                        }
                    }

                    for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                    {
                        string currentCaseDir = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i];

                        // We must check if this exists before we construct the Probing object
                        string pathToProbeFile = Probing.GetPathToProbedResults(currentCaseDir, currField, RES);
                        if (File.Exists(pathToProbeFile))
                        {
                            if (currField.FieldType == fieldType.vector)
                            {
                                Probing Vectors = new Probing(listOfPoints, currentCaseDir, RES.WorkingDirectory, currField, RES.Domain.BCond.windDirs[i], RES, run);

                                // Create datatree
                                treeVector.AppendRange(Vectors.ResultVec, new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                            else
                            {
                                Probing Scalars = new Probing(listOfPoints, currentCaseDir, RES.WorkingDirectory, currField, RES.Domain.BCond.windDirs[i], RES, run);

                                // Create datatree
                                treeDouble.AppendRange(Scalars.ResultScalar, new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                        }
                        else
                        {
                            base.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.FieldDoesntExist(currentCaseDir, currField));
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.ToString());

                    //throw new System.ArgumentException("Parsing of the probes failed. This data does not exist yet. Please run the probing component.");
                }
            }

            // Todo: Move this into class object once its properly architected

            if (currField.FieldType == fieldType.vector)
            {
                try
                {
                    if (!treeVector.IsEmpty)
                    {
                        var list = treeVector.get_Branch(new GH_Path(0));
                        var listVecs = new List<GH_Vector>();

                        foreach (object item in list)
                        {
                            listVecs.Add((GH_Vector)item);
                        }

                        int[] IndecesOfExtremeProbes = Probing.ReturnProbeIndicesOutsideDomain(listVecs);
                        if (IndecesOfExtremeProbes.Length > 0)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.PointsOutsideDomain(IndecesOfExtremeProbes));
                        }
                    }
                }
                catch (Exception)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.PleaseRunProbingComponent());
                }
            }

            if (currField.FieldType == fieldType.scalar)
            {
                DA.SetDataTree(1, treeDouble);
                DA.SetDataList(0, listOfPoints);
            }
            else if (currField.FieldType == fieldType.vector)
            {
                DA.SetDataTree(1, treeVector);
                DA.SetDataList(0, listOfPoints);
            }

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
        public override Guid ComponentGuid => new Guid("{79224E0A-21EF-41C5-88B1-E44B860F5A4E}");
    }
}