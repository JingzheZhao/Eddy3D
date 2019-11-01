using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompVisProbesCustom : GH_Component
    {
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "No Culling of Probing Points", Menu_DoClick, true, !Culling);
        }

        private void Menu_DoClick(object sender, EventArgs e)
        {
            Culling = !Culling;
            ExpireSolution(true);
        }

        public bool Culling = true;

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetBoolean("Culling", Culling);
            // Then call the base class implementation.
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            Culling = reader.GetBoolean("Culling");
            // Then call the base class implementation.
            return base.Read(reader);
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompVisProbesCustom()
          : base("Visualize Probes", "VisProbes", "PostProcessing", "Eddy", "5 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Eddy Result", GH_ParamAccess.item);
            pManager.AddPointParameter("Porbing points", "Points", "List of probing points", GH_ParamAccess.list);
            pManager.AddTextParameter("Name of instance", "Name", "Name of instance to be probed", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Name of field", "Field", "Name of field to be probed", GH_ParamAccess.item, 0);
            Param_Integer param = pManager[3] as Param_Integer;
            param.AddNamedValue("U", 0);
            param.AddNamedValue("total(p)_coeff", 1);
            param.AddNamedValue("p", 2);
            param.AddNamedValue("epsilon", 3);
            param.AddNamedValue("omega", 4);
            param.AddNamedValue("k", 5);
            param.AddNamedValue("nut", 6);
            param.AddNamedValue("phi", 7);
            //pManager.AddIntegerParameter("FieldType", "FieldType", "FieldType", GH_ParamAccess.item, 1);
            //Param_Integer param2 = pManager[4] as Param_Integer;
            //param2.AddNamedValue("Scalar", 0);
            //param2.AddNamedValue("Vector", 1);

            pManager.AddBooleanParameter("Run", "Run", "Run the component.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Porbing points", "Probes", "List of probing points (those maybe culled if culling was on)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Probing result", "Res", "Probing result", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region Load Inputs

            // mode to select simulation environment
            if (Culling) { Message = "Cull Points"; }
            else { Message = "No Culling"; }

            OFResult RES = null;
            DA.GetData(0, ref RES);

            List<Point3d> listOfPoints = new List<Point3d>();

            bool run = false;
            int OFFieldInt = 0;
            string probeNameByUser = "";

            DA.GetDataList(1, listOfPoints);
            DA.GetData(2, ref probeNameByUser);
            DA.GetData(3, ref OFFieldInt);
            //DA.GetData(4, ref fieldType);
            DA.GetData(4, ref run);

            //Discard points outside
            if (Culling)
            {
                listOfPoints = Utilities.DiscardPoints(listOfPoints, RES.Domain);
            }

            int numberOfProbes = listOfPoints.Count();

            #endregion Load Inputs

            #region Error handling

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of point to the component.");
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

            #endregion Error handling

            // Export probes file
            File.WriteAllText(Path.Combine(RES.WorkingDirectory + "\\" + "run_probes.bat"), EddyLib.StrTemp.BatFiles.Run_Probes(RES.Domain, RES.MeshSettings));

            // export pts file for Daysim
            if (!Directory.Exists(RES.WorkingDirectory + @"Rad\"))
            {
                Directory.CreateDirectory(RES.WorkingDirectory + @"Rad\");
            }

            RadianceFiles.writePTS(RES.WorkingDirectory + @"\Rad\sensors.pts", listOfPoints);

            if (Utilities.Directories.IsDirectoryEmpty(RES.MeshSettings.meshPolyMeshDir) == true)
            {
                throw new System.ArgumentException("The mesh folder is empty. Can't retrieve probes from a mesh that does not exist.");
            }

            GH_Structure<GH_Number> treeDouble = new GH_Structure<GH_Number>();
            GH_Structure<GH_Vector> treeVector = new GH_Structure<GH_Vector>();

            string OFField = EddyLib.OFField.ReformatOFFields(OFFieldInt);
            OFField currField = new OFField(OFField, probeNameByUser);

            if (numberOfProbes > 0)
            {
                try
                {
                    #region NUMBERS

                    if (currField.FieldType == EddyLib.OFField.fieldType.number)
                    {
                        StringBuilder command = new StringBuilder();

                        for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                        {
                            string pathToPointFile = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\constant\polyMesh\points";
                            if (!File.Exists(pathToPointFile))
                            {
                                base.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"The file  """ + pathToPointFile + @""" does not exist. Please make sure that a mesh with point exists.");
                                return;
                            }

                            string path = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + currField.ProbeName;

                            File.WriteAllText(RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + "controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(RES.RunSettings, RES.Domain, null, i));
                            File.WriteAllText(path, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, currField));

                            // Write the dicts
                            if (RES.RunSettings.simEngine == SimEngine.Docker)
                            {
                                command.Append(@"postProcess -func " + currField.ProbeName + @" -latestTime | tee " + RES.Domain.BCond.windDirs[i] + @"/log_probes;");
                            }
                            else
                            {// piping interfers with the windows executables which rely on linux syntax. Need to find a way to load environment variables of entire linux env
                                command.AppendLine(@"postProcess -case " + RES.Domain.BCond.windDirs[i] + " -func " + currField.ProbeName + @" -latestTime");
                            }
                        }

                        if (run == true)
                        {
                            if (RES.RunSettings.simEngine == SimEngine.Docker)
                            {
                                var arg = EddyLib.StrTemp.BatFiles.DockerPrefixPath(RES.Domain, RES.MeshSettings, RES.RunSettings, EddyLib.StrTemp.Mode.Simulation) + command;
                                Utilities.StartProcess.StartProcessCMDNT(arg, false, true, false, true);
                            }
                            else
                            {
                                //Utilities.StartProcessCMD(EddyLib.StrTemp.BatFiles.TempBlueCFD(new List<string> { command.ToString(), "type log" }, RES.WorkingDirectory), false, true, true);
                                Utilities.StartProcess.StartProcessCMDNT(EddyLib.StrTemp.BatFiles.TempBlueCFD(new List<string> { command.ToString() }, RES.WorkingDirectory), false, true, false, true);
                            }
                        }

                        for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                        {
                            string currentCaseDir = RES.WorkingDirectory + "\\" + RES.Domain.BCond.windDirs[i];
                            // Todo: This throws exception if the folder doesn't exit, meaning if it
                            // wasn't run yet. Second, it throws an exception if the folder exists
                            // but is empty. here, it also won't find the iteration path.
                            string pathToProbeFile = Probing.GetIterationPathToProbedResults(currentCaseDir, currField);
                            if (File.Exists(pathToProbeFile))
                            {
                                Probing Numbers = new Probing(listOfPoints, currentCaseDir, RES.WorkingDirectory, currField, RES.Domain.BCond.windDirs[i]);
                                // Create datatree
                                treeDouble.AppendRange(Numbers.ResultNum, new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                            else
                            {
                                base.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"The file """ + pathToProbeFile + @""" does not exist. Please run the probing component.");
                                return;
                            }
                        }
                    }

                    #endregion NUMBERS

                    #region VECTORS

                    if (currField.FieldType == EddyLib.OFField.fieldType.vector)
                    {
                        StringBuilder command = new StringBuilder();

                        //string pointName = "U_Probes";
                        //string cleanedOFField = Regex.Replace(fieldName, @"[^a-zA-Z]", "");

                        for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                        {
                            string pathToPointFile = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\constant\polyMesh\points";
                            if (!File.Exists(pathToPointFile))
                            {
                                base.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"The file  """ + pathToPointFile + @""" does not exist. Please make sure that a mesh with point exists.");
                                return;
                            }

                            // Write the dicts
                            if (RES.RunSettings.simEngine == SimEngine.Docker)
                            {
                                string path = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + probeNameByUser;
                                File.WriteAllText(path, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, currField));
                                command.Append(@"postProcess -func " + currField.ProbeName + @" -latestTime | tee  " + RES.Domain.BCond.windDirs[i] + @"/log_probes;");
                            }
                            else
                            {// piping interfers with the windows executables which rely on linux syntax. Need to find a way to load environment variables of entire linux env
                                string path = RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + probeNameByUser;
                                File.WriteAllText(path, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, currField));
                                // Todo: check here if we need a semicolon to sepaate the command
                                // from the suffix
                                command.AppendLine(@"postProcess -case " + RES.Domain.BCond.windDirs[i] + " -func " + probeNameByUser + @" -latestTime");
                            }
                        }

                        if (run == true)
                        {
                            if (RES.RunSettings.simEngine == SimEngine.Docker)
                            {
                                var arg = EddyLib.StrTemp.BatFiles.DockerPrefixPath(RES.Domain, RES.MeshSettings, RES.RunSettings, EddyLib.StrTemp.Mode.Simulation) + command;
                                Utilities.StartProcess.StartProcessCMDNT(arg, false, true, false, true);
                            }
                            else
                            {
                                // Utilities.StartProcessCMD(EddyLib.StrTemp.BatFiles.TempBlueCFD(new
                                // List<string> { command.ToString(), "type log" },
                                // RES.WorkingDirectory), false, true, true);
                                Utilities.StartProcess.StartProcessCMDNT(EddyLib.StrTemp.BatFiles.TempBlueCFD(new List<string> { command.ToString() }, RES.WorkingDirectory), false, true, false, true);
                            }
                        }

                        for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                        {
                            string currentCaseDir = RES.WorkingDirectory + "\\" + RES.Domain.BCond.windDirs[i];
                            string pathToProbeFile = Probing.GetIterationPathToProbedResults(currentCaseDir, currField);
                            if (File.Exists(pathToProbeFile))
                            {
                                Probing Vectors = new Probing(listOfPoints, currentCaseDir, RES.WorkingDirectory, currField, RES.Domain.BCond.windDirs[i]);
                                // Create datatree
                                treeVector.AppendRange(Vectors.ResultVec, new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                            else
                            {
                                base.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The file  """ + pathToProbeFile + @""" does not exist. Please run the probing component.");
                            }
                        }
                    }

                    #endregion VECTORS
                }
                catch (Exception)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Parsing of the probes failed. This data does not exist yet. Please run the probing component.");
                    //throw new System.ArgumentException("This data does not exist yet. Please run the probing component.");
                }
            }

            // Todo: Move this into class object once its properly architected

            if (currField.FieldType == EddyLib.OFField.fieldType.vector)
            {
                var list = treeVector.get_Branch(new GH_Path(0));
                var listVecs = new List<GH_Vector>();

                foreach (object item in list)
                {
                    listVecs.Add((GH_Vector)item);
                }

                int[] IndecesOfExtremeProbes = Probing.ReturnIndexOfExtremeProbes(listVecs);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, @"The probes with the indices: " + string.Join(",", IndecesOfExtremeProbes) + " can't be probed within the simulation domain.");
            }

            if (currField.FieldType == EddyLib.OFField.fieldType.number)
            {
                DA.SetDataTree(1, treeDouble);
                DA.SetDataList(0, listOfPoints);
            }
            else if (currField.FieldType == EddyLib.OFField.fieldType.vector)
            {
                DA.SetDataTree(1, treeVector);
                DA.SetDataList(0, listOfPoints);
            }
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