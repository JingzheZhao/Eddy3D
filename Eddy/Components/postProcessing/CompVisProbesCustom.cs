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
          : base("VisProbesCustom", "VisProbesCustom", "PostProcessing", "Eddy", "PostProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddPointParameter("points", "points", "points", GH_ParamAccess.list);
            pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);

            pManager.AddTextParameter("Field", "Field", "Field", GH_ParamAccess.item);
            pManager.AddIntegerParameter("FieldType", "FieldType", "FieldType", GH_ParamAccess.item, 1);
            Param_Integer param = pManager[4] as Param_Integer;
            param.AddNamedValue("double", 0);
            param.AddNamedValue("vector", 1);


            pManager.AddBooleanParameter("Run", "Run", "Clean the directory", GH_ParamAccess.item, false);


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




            int fieldType = 0;
            List<Point3d> listOfPoints = new List<Point3d>();

            bool run = false;
            String OFField = "";
            String enumeratedProbeName = "";

            DA.GetDataList(1, listOfPoints);
            DA.GetData(2, ref enumeratedProbeName);
            DA.GetData(3, ref OFField);
            DA.GetData(4, ref fieldType);
            DA.GetData(5, ref run);



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

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of point to the component.");
            }


            // Check if U file is in last iteration
            for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
            {
                string iter = Utilities.GetLastIterationInSimfolder(DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i]).ToString();
                string fp = DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i] + @"\" + iter + @"\U";


                if (!File.Exists(fp))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"The simulation folder of the wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
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


            var treeDouble = new DataTree<double>();
            var treeVector = new DataTree<Vector3d>();


            if (numberOfProbes > 0)
            {


                try
                {


                    if (fieldType == 0) // double
                    {

                        StringBuilder command = new StringBuilder();


                        //string pointName = "cp_Probes";                        
                        //string cleanedOFField = Regex.Replace(OFField, @"[^a-zA-Z]", "");



                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {
                            var path = DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + enumeratedProbeName;

                            File.WriteAllText(DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + "controlDict", StringTemplates.ControlDict(DOM, null, i));
                            File.WriteAllText(path, StringTemplates.SampleProbes(listOfPoints, enumeratedProbeName, OFField));

                            command.Append(@"postProcess -case " + DOM.BCInflow.windDir[i] + " -func " + enumeratedProbeName + @" -latestTime | tee  " + DOM.BCInflow.windDir[i] + @"/log_probes;");


                        }

                        if (run == true)
                        {

                            ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + DOM.baseWorkingDirectory);
                            Process p = new Process();
                            p.StartInfo = psi;
                            p.Start();
                            p.WaitForExit();

                        }

                        //Thread.Sleep(2 * numberOfProbes);

                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {



                            var caseDir = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i];
                            string pathToProbeFile = ParsingProbes.GetLastProcProssDir(enumeratedProbeName, caseDir, OFField);
                            if (File.Exists(pathToProbeFile))
                            {
                                ParsingProbes Numbers = new ParsingProbes(listOfPoints, enumeratedProbeName, caseDir, OFField, fieldType);

                                // Create datatree

                                treeDouble.AddRange(Utilities.filterExtremeCPs(Numbers.numberValues), new Grasshopper.Kernel.Data.GH_Path(i));
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

                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {

                            // Write the dicts

                            var path = DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + enumeratedProbeName;

                            File.WriteAllText(path, StringTemplates.SampleProbes(listOfPoints, enumeratedProbeName, OFField));

                            command.Append(@"postProcess -case " + DOM.BCInflow.windDir[i] + " -func " + enumeratedProbeName + @" -latestTime | tee  " + DOM.BCInflow.windDir[i] + @"/log_probes;");


                        }

                        if (run == true)
                        {
                            ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + DOM.baseWorkingDirectory);
                            Process p = new Process();
                            p.StartInfo = psi;
                            p.Start();
                            p.WaitForExit();
                        }
                        //Thread.Sleep(2 * numberOfProbes);





                        for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                        {
                            // Parse values


                            var caseDir = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i];
                            string pathToProbeFile = ParsingProbes.GetLastProcProssDir(enumeratedProbeName, caseDir, OFField);
                            if (File.Exists(pathToProbeFile))
                            {
                                
                                var Vectors = new ParsingProbes(listOfPoints, enumeratedProbeName, caseDir, OFField, fieldType);

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



