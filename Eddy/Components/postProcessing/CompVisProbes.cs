using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompVisProbes : GH_Component
    {
        private DataTree<double> cpTree = new DataTree<double>();
        private DataTree<Vector3d> uTree = new DataTree<Vector3d>();




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompVisProbes()
          : base("VisProbes", "VisProbes", "PostProcessing", "Eddy", "PostProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddPointParameter("points", "points", "points", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 1);
            Param_Integer param = pManager[2] as Param_Integer;
            param.AddNamedValue("cp", 0);
            param.AddNamedValue("U", 1);


            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);


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




            int mode = 0;
            List<Point3d> listOfPoints = new List<Point3d>();

            bool run = false;

            DA.GetDataList(1, listOfPoints);
            //DA.GetData(2, ref pointName);
            DA.GetData(2, ref mode);
            DA.GetData(3, ref run);




            // Inclusion check for probes

            // Filter the list
            int kept = 0;
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                // Test whether this is an element that we want to keep.
                if (DOM.inputBreps.IsPointInside(listOfPoints[i], 0.01, true) == false)
                {
                    // Add it to the list of kept elements.
                    listOfPoints[kept] = listOfPoints[i];
                    kept++;
                }
            }
            // Unfortunately IList has no Resize method. So instead we
            // remove the last element of the list until: elements.Count == kept.
            while (kept < listOfPoints.Count)
            {
                listOfPoints.RemoveAt(listOfPoints.Count - 1);
            }

            var numberOfProbes = listOfPoints.Count();


            // Error handling

            StringBuilder errorLog = new StringBuilder();

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of point to the component.");
            }


            var numberOfWindDirs = DOM.BCInflow.windDir.Count;





            for (int i = 0; i < numberOfWindDirs; i++)
            {
                var fp = DOM.baseWorkingDirectory + @"\mesh\constant\polyMesh";
                if (!Directory.Exists(fp))
                {
                    errorLog.AppendLine(@"The wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The wind direction " + DOM.BCInflow.windDir[i] + @" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");

                    throw new System.ArgumentException("The wind direction " + DOM.BCInflow.windDir[i] + @" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");


                }
            }

            for (int i = 0; i < numberOfWindDirs; i++)
            {
                var ABLfilePath = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\0.org\ABLConditions";
                if (!File.Exists(ABLfilePath)) { Console.WriteLine(ABLfilePath + " not found. Exiting"); errorLog.AppendLine(ABLfilePath + " not found. Exiting"); }
            }


            // Check if U file is in last iteration
            for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
            {
                string path = DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i];
                string iter = Utilities.GetLastIterationInSimfolder(path).ToString();
                string fp = DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i] + @"\" + iter + @"\U";


                if (!File.Exists(fp))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The simulation folder of the wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                }
            }



            // export pts file for Daysim
            if (!Directory.Exists(DOM.baseWorkingDirectory + @"Rad\"))
            {
                Directory.CreateDirectory(DOM.baseWorkingDirectory + @"Rad\");
            }
            RadianceFiles.writePTS(DOM.baseWorkingDirectory + @"\Rad\sensors.pts", listOfPoints);

            if (Utilities.IsDirectoryEmpty(DOM.meshPolyMeshDirectory) == true)
            {
                throw new System.ArgumentException("The mesh folder is empty. Can't retrieve probes from a mesh that does not exist.");
            }


            if (numberOfProbes > 0)
            {


                try
                {
                    cpTree = new DataTree<double>();
                    uTree = new DataTree<Vector3d>();


                    if (mode == 0) // cp
                    {




                        StringBuilder command = new StringBuilder();

                        string pointName = "cp_Probes";
                        string OFfield = "total(p)_coeff";
                        int fieldtype = 0; // double

                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {

                            File.WriteAllText(DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + "controlDict", StringTemplates.ControlDict(DOM, null, i));
                            File.WriteAllText(DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + pointName, StringTemplates.SampleProbes(listOfPoints, pointName, OFfield));

                            command.Append(@"postProcess -case " + DOM.BCInflow.windDir[i] + " -func " + pointName + @" -latestTime | tee  " + DOM.BCInflow.windDir[i] + @"/log_probes;");
                        }

                        for (int i = 0; i < numberOfWindDirs; i++)

                        {
                            var fp = DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i] + @"\system\cp_Probes";
                            if (!File.Exists(fp))
                            {

                                errorLog.AppendLine(@"The wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the probing dictionary for cp values. Possible solution: Please connect the ""writeProbes"" component and recompute the solution.");
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The wind direction " + DOM.BCInflow.windDir[i] + @" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");

                                throw new System.ArgumentException("The wind direction " + DOM.BCInflow.windDir[i] + @" misses the probing dictionary  for cp values. Possible solution: Please connect the component ""writeProbes"" and recompute the solution.");
                            }
                        }


                        if (run == true)
                        {
                            ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + DOM.OFbaseWorkingDirectory);
                            Process p = new Process
                            {
                                StartInfo = psi
                            };
                            p.Start();
                            p.WaitForExit();
                        }


                        //Thread.Sleep(2 * numberOfProbes);

                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {




                            var caseDir = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i];
                            string pathToProbeFile = ParsingProbes.GetLastProcProssDir(pointName, caseDir, OFfield);
                            if (File.Exists(pathToProbeFile))
                            {

                                //string pointName = "cp_Probes";
                                //string OFfield = "total(p)_coeff";

                                ParsingProbes cp = new ParsingProbes(listOfPoints, pointName, caseDir, OFfield, fieldtype);
                                cpTree.AddRange(Utilities.FilterExtremeCPs(cp.numberValues), new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                            else
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"The file  """ + pathToProbeFile + @""" does not exist. Please run the probing component.");

                            }
                        }


                    }

                    if (mode == 1) // U
                    {



                        StringBuilder command = new StringBuilder();

                        string pointName = "U_Probes";
                        string OFfield = "U";
                        int fieldtype = 1; // vectors

                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {

                            // Write the dicts

                            File.WriteAllText(DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + pointName, StringTemplates.SampleProbes(listOfPoints, pointName, OFfield));

                            command.Append(@"postProcess -case " + DOM.BCInflow.windDir[i] + " -func " + pointName + @" -latestTime | tee  " + DOM.BCInflow.windDir[i] + @"/log_probes;");


                        }

                        for (int i = 0; i < numberOfWindDirs; i++)

                        {
                            var fp = DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i] + @"\system\U_Probes";
                            if (!File.Exists(fp))
                            {
                                errorLog.AppendLine(@"The wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the probing dictionary for U values. Possible solution: Please connect the ""writeProbes"" component and recompute the solution.");
                                throw new System.ArgumentException("The wind direction " + DOM.BCInflow.windDir[i] + @" misses the probing dictionary for U values. Possible solution: Please connect the component ""writeProbes"" and recompute the solution.");
                            }
                        }

                        if (run == true)
                        {

                            ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + DOM.OFbaseWorkingDirectory);
                            Process p = new Process
                            {
                                StartInfo = psi
                            };
                            p.Start();
                            p.WaitForExit();
                        }

                        //Thread.Sleep(2 * numberOfProbes);

                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {
                            // Parse values

                            var caseDir = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i];
                            string pathToProbeFile = ParsingProbes.GetLastProcProssDir(pointName, caseDir, OFfield);
                            if (File.Exists(pathToProbeFile))
                            {

                                var U = new ParsingProbes(listOfPoints, pointName, caseDir, OFfield, fieldtype);

                                // Create datatree

                                uTree.AddRange(U.vectorValues, new Grasshopper.Kernel.Data.GH_Path(i));
                            }
                            else
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"The file  """ + pathToProbeFile + @""" does not exist. Please run the probing component.");

                            }
                        }
                    }
                }


                catch (Exception e) { Console.WriteLine(e.Message); File.WriteAllText(DOM.baseWorkingDirectory + @"\Probes.err", errorLog.ToString()); return; }




            }

            if (mode == 0)
            {
                DA.SetDataTree(1, cpTree);
                DA.SetDataList(0, listOfPoints);
            }
            else if (mode == 1)
            {
                DA.SetDataTree(1, uTree);
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
        public override Guid ComponentGuid => new Guid("{D39A60E1-7086-4C6F-BFF1-492D84910227}");
    }
}



