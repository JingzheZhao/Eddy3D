using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using System.Linq;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;
using System.Text.RegularExpressions;
using Grasshopper;
using EddyLib;
using Eddy.Properties;
using System.Threading;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class PostProcessingProbes : GH_Component
    {


        DataTree<double> cpTree = new DataTree<double>();
        DataTree<Vector3d> uTree = new DataTree<Vector3d>();




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public PostProcessingProbes()
          : base("Probes", "Probes", "PostProcessing", "Eddy", "PostProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddPointParameter("points", "points", "points", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 0);
            Param_Integer param = pManager[2] as Param_Integer;
            param.AddNamedValue("cp_Probes", 0);
            param.AddNamedValue("U_Probes", 1);

            pManager.AddBooleanParameter("Run", "Run", "Clean the directory", GH_ParamAccess.item, false);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Out", "Result", GH_ParamAccess.tree);
            //pManager.AddGenericParameter("Result", "Out", "Result", GH_ParamAccess.tree);
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

            // Error handling

            var numberOfProbes = listOfPoints.Count();

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of point to the component.");

            }



            // export pts file for Daysim
            if (!Directory.Exists(DOM.baseWorkingDirectory + @"Rad\"))
            {
                Directory.CreateDirectory(DOM.baseWorkingDirectory + @"Rad\");
            }
            RadianceFiles.writePTS(DOM.baseWorkingDirectory + @"\Rad\sensors.pts", listOfPoints);




            if (Utilities.IsDirectoryEmpty(DOM.meshPolyMeshDirectory) == true)
            {
                throw new System.ArgumentException("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
            }



           

            if (run == true && numberOfProbes > 0)
            {

                cpTree = new DataTree<double>();
                uTree = new DataTree<Vector3d>();




                //Weld Mesh to prevent wrong inclusion test
                DOM.combinedMeshes.Weld(Math.PI);


                // Filter the list
                int kept = 0;
                for (int i = 0; i < listOfPoints.Count; i++)
                {
                    // Test whether this is an element that we want to keep.
                    if (DOM.combinedMeshes.IsPointInside(listOfPoints[i], 0.001, true) == false)
                    {
                        // Add it to the list of kept elements.
                        listOfPoints[kept] = listOfPoints[i];
                        kept++;
                    }
                }
                // Unfortunately IList has no Resize method. So instead we
                // remove the last element of the list until: elements.Count == kept.
                while (kept < listOfPoints.Count) listOfPoints.RemoveAt(listOfPoints.Count - 1);



                if (mode == 0) // cp
                {

                    StringBuilder command = new StringBuilder();

                    string pointName = "cp_Probes";
                    string OFfield = "total(p)_coeff";

                    for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                    {

                        File.WriteAllText(DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + "controlDict", StringTemplates.controlDict(DOM, null, i));
                        File.WriteAllText(DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, mode));

                        command.Append(@"postProcess -case " + DOM.BCInflow.windDir[i] + " -func " + pointName + @" -latestTime;");


                    }

                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + DOM.baseWorkingDirectory);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();

                    Thread.Sleep(2 * numberOfProbes);

                    for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                    {
                        ParsingValues cp = new ParsingValues(listOfPoints, pointName, DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i], OFfield);
                        cpTree.AddRange(cp.cpValues, new Grasshopper.Kernel.Data.GH_Path(i));
                    }


                }

                if (mode == 1) // U
                {


                    StringBuilder command = new StringBuilder();

                    string pointName = "U_Probes";
                    string OFfield = "U";

                    for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                    {


                        // Write the dicts

                        File.WriteAllText(DOM.baseWorkingDirectory + DOM.BCInflow.windDir[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, mode));

                        command.Append(@"postProcess -case " + DOM.BCInflow.windDir[i] + " -func " + pointName + @" -latestTime;");


                    }

                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + DOM.baseWorkingDirectory);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();

                    Thread.Sleep(2 * numberOfProbes);

                    for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                    {

                        // Parse values

                        var U = new ParsingValues(listOfPoints, pointName, DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i], OFfield);


                        // Create datatree

                        uTree.AddRange(U.uValues, new Grasshopper.Kernel.Data.GH_Path(i));

                    }



                }
            }

            if (mode == 0)
            {
                DA.SetDataTree(0, cpTree);
            }
            else if (mode == 1)
            {
                DA.SetDataTree(0, uTree);
            }
           

        }


        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.Eddy_probes;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{D39A60E1-7086-4C6F-BFF1-492D84910227}"); }
        }
    }
}



