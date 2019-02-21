using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{





    public class CompVisProbesCustom : GH_Component
    {






        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompVisProbesCustom()
          : base("VisProbes", "VisProbes", "PostProcessing", "Eddy", "PostProcessing")
        {
        }





        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "Name", "Name", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Field", "Field", "Field", GH_ParamAccess.item, 0);
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


            pManager.AddBooleanParameter("Run", "Run", "Run the probing component.", GH_ParamAccess.item, false);



        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddGenericParameter("Result", "Result", "Result", GH_ParamAccess.tree);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            OFBaseDomain DOM = null;



            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }





            List<Point3d> listOfPoints = new List<Point3d>();

            bool run = false;
            int OFFieldInt = 0;
            string enumeratedProbeName = "";

            DA.GetDataList(1, listOfPoints);
            DA.GetData(2, ref enumeratedProbeName);
            DA.GetData(3, ref OFFieldInt);
            //DA.GetData(4, ref fieldType);
            DA.GetData(4, ref run);


            Probes.ReformatOFFields(OFFieldInt, out string OFField, out int fieldType);

            //Discard points outside
            listOfPoints = Utilities.DiscardPointsOutsideDomain(listOfPoints, DOM);
            var numberOfProbes = listOfPoints.Count();


            // Error handling

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of point to the component.");
            }


            // Check if U file is in last iteration
            for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
            {
                string path = DOM.baseWorkingDir + @"\" + DOM.BCInflow.windDirs[i];
                string iter = Utilities.GetLastIterationFromDirectory(path).ToString();
                string fp = DOM.baseWorkingDir + @"\" + DOM.BCInflow.windDirs[i] + @"\" + iter + @"\U";


                if (!File.Exists(fp))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The last iteration """ + iter + @""" of the wind direction """ + DOM.BCInflow.windDirs[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                }
            }

            // Export probes file
            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_probes.bat"), EddyLib.StrTemp.BatFiles.Run_Probes(DOM));


            // export pts file for Daysim
            if (!Directory.Exists(DOM.baseWorkingDir + @"Rad\"))
            {
                Directory.CreateDirectory(DOM.baseWorkingDir + @"Rad\");
            }

            RadianceFiles.writePTS(DOM.baseWorkingDir + @"\Rad\sensors.pts", listOfPoints);

            if (Utilities.IsDirectoryEmpty(DOM.meshPolyMeshDir) == true)
            {
                throw new System.ArgumentException("The mesh folder is empty. Can't retrieve probes from a mesh that does not exist.");
            }


            var treeDouble = new DataTree<double>();
            var treeVector = new DataTree<Vector3d>();


            if (numberOfProbes > 0)
            {


                try
                {


                    if (fieldType == 0) // double
                    {

                        StringBuilder command = new StringBuilder();


                        for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                        {
                            var path = DOM.baseWorkingDir + DOM.BCInflow.windDirs[i] + @"\system\" + enumeratedProbeName;

                            // cp parsing
                            File.WriteAllText(DOM.baseWorkingDir + DOM.BCInflow.windDirs[i] + @"\system\" + "controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(DOM, null, i));
                            File.WriteAllText(path, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, enumeratedProbeName, OFField));

                            // Write the dicts
                            if (DOM.simEngine == 0)
                            {
                                command.Append(@"postProcess -case " + DOM.BCInflow.windDirs[i] + " -func " + enumeratedProbeName + @" -latestTime | tee  " + DOM.BCInflow.windDirs[i] + @"/log_probes;");
                            }
                            else
                            {// piping interfers with the windows executables which rely on linux syntax. Need to find a way to load environment variables of entire linux env
                                command.AppendLine(@"postProcess -case " + DOM.BCInflow.windDirs[i] + " -func " + enumeratedProbeName + @" -latestTime");
                            }
                        }

                        if (run == true)
                        {

                            if (DOM.simEngine == 0)
                            {
                                Utilities.StartProcessCMD(@" -e """ + command + @""" -f " + "\"" + DOM.OFbaseWorkingDir, false, true, false, Utilities.AssemblyDirectory + @"\CallOF.exe");
                            }
                            else
                            {
                                //Utilities.StartProcessCMD(EddyLib.StrTemp.BatFiles.TempBlueCFD(new List<string> { command.ToString(), "type log" }, DOM.baseWorkingDir), false, true, true);
                                Utilities.StartProcessCMD(EddyLib.StrTemp.BatFiles.TempBlueCFD(new List<string> { command.ToString() }, DOM.baseWorkingDir), false, true, true);
                            }

                        }

                        //Thread.Sleep(2 * numberOfProbes);

                        for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                        {

                            var caseDir = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i];
                            string pathToProbeFile = Probes.GetFullPathToProbeFile(enumeratedProbeName, caseDir, OFField);
                            if (File.Exists(pathToProbeFile))
                            {
                                Probes Numbers = new Probes(listOfPoints, enumeratedProbeName, caseDir, OFField, fieldType);

                                // Create datatree

                                treeDouble.AddRange(Utilities.FilterExtremeCPs(Numbers.numberValues), new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                            else
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"The file  """ + pathToProbeFile + @""" does not exist. Please run the probing component.");

                            }
                        }

                    }

                    if (fieldType == 1) //vector
                    {

                        StringBuilder command = new StringBuilder();

                        //string pointName = "U_Probes";
                        //string cleanedOFField = Regex.Replace(fieldName, @"[^a-zA-Z]", "");

                        for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                        {

                            // Write the dicts
                            if (DOM.simEngine == 0)
                            {
                                var path = DOM.baseWorkingDir + DOM.BCInflow.windDirs[i] + @"\system\" + enumeratedProbeName;
                                File.WriteAllText(path, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, enumeratedProbeName, OFField));
                                command.Append(@"postProcess -case " + DOM.BCInflow.windDirs[i] + " -func " + enumeratedProbeName + @" -latestTime | tee  " + DOM.BCInflow.windDirs[i] + @"/log_probes;");
                            }
                            else
                            {// piping interfers with the windows executables which rely on linux syntax. Need to find a way to load environment variables of entire linux env
                                var path = DOM.baseWorkingDir + DOM.BCInflow.windDirs[i] + @"\system\" + enumeratedProbeName;
                                File.WriteAllText(path, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, enumeratedProbeName, OFField));
                                command.AppendLine(@"postProcess -case " + DOM.BCInflow.windDirs[i] + " -func " + enumeratedProbeName + @" -latestTime");
                            }
                        }

                        if (run == true)
                        {
                            if (DOM.simEngine == 0)
                            {
                                Utilities.StartProcessCMD(@" -e """ + command + @""" -f " + "\"" + DOM.OFbaseWorkingDir, false, true, false, Utilities.AssemblyDirectory + @"\CallOF.exe");
                            }
                            else
                            {
                                // Utilities.StartProcessCMD(EddyLib.StrTemp.BatFiles.TempBlueCFD(new List<string> { command.ToString(), "type log" }, DOM.baseWorkingDir), false, true, true);
                                Utilities.StartProcessCMD(EddyLib.StrTemp.BatFiles.TempBlueCFD(new List<string> { command.ToString() }, DOM.baseWorkingDir), false, true, true);
                            }
                        }
                        //Thread.Sleep(2 * numberOfProbes);

                        for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                        {
                           
                            var caseDir = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i];
                            string pathToProbeFile = Probes.GetFullPathToProbeFile(enumeratedProbeName, caseDir, OFField);
                            if (File.Exists(pathToProbeFile))
                            {

                                var Vectors = new Probes(listOfPoints, enumeratedProbeName, caseDir, OFField, fieldType);

                                // Create datatree

                                treeVector.AddRange(Vectors.vectorValues, new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                            else
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The file  """ + pathToProbeFile + @""" does not exist. Please run the probing component.");

                            }

                        }
                    }

                }
                catch (Exception)
                {

                    throw;
                }
            }

            if (fieldType == 0)
            {
                DA.SetDataTree(1, treeDouble);
                DA.SetDataList(0, listOfPoints);
            }
            else if (fieldType == 1)
            {
                DA.SetDataTree(1, treeVector);
                DA.SetDataList(0, listOfPoints);
            }


        }


        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_probes;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{79224E0A-21EF-41C5-88B1-E44B860F5A4E}");
    }
}



