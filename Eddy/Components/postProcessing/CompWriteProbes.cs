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

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class WriteProbes : GH_Component
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
        public WriteProbes()
          : base("WriteProbes", "WriteProbes", "WriteProbes", "Eddy", "postProcessing")
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
            param.AddNamedValue("cp_Probes", 0);
            param.AddNamedValue("U_Probes", 1);

            //pManager.AddBooleanParameter("Run", "Run", "Clean the directory", GH_ParamAccess.item, false);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Result", "Out", "Result", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Result", "Out", "Result", GH_ParamAccess.tree);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            OFResult RES = null;
            DA.GetData(0, ref RES);


            int mode = 0;
            List<Point3d> listOfPoints = new List<Point3d>();

            DA.GetDataList(1, listOfPoints);
            //DA.GetData(2, ref pointName);
            DA.GetData(2, ref mode);
            //DA.GetData(3, ref run);



            // Inclusion check for probes

            // Filter the list
            int kept = 0;
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                // Test whether this is an element that we want to keep.
                if (RES.Domain.inputBreps.IsPointInside(listOfPoints[i], 0.01, true) == false)
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

            // Export probes file
            File.WriteAllText(Path.Combine(RES.WorkingDirectory + "\\" + "run_probes.bat"), EddyLib.StrTemp.BatFiles.Run_Probes(RES.Domain));


            // export pts file for Daysim
            if (!Directory.Exists(RES.WorkingDirectory + @"Rad\"))
            {
                Directory.CreateDirectory(RES.WorkingDirectory + @"Rad\");
            }
            RadianceFiles.writePTS(RES.WorkingDirectory + @"\Rad\sensors.pts", listOfPoints);






            if (numberOfProbes > 0)
            {

                cpTree = new DataTree<double>();
                uTree = new DataTree<Vector3d>();




                if (mode == 0) // cp
                {



                    string pointName = "cp_Probes";
                    string OFfield = "total(p)_coeff";

                    for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                    {

                        File.WriteAllText(RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + "controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(RES.RunSettings,RES.Domain, null, i));
                        File.WriteAllText(RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + pointName, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, pointName, OFfield));



                    }




                }

                if (mode == 1) // U
                {


                    string pointName = "U_Probes";
                    string OFfield = "U";

                    for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                    {


                        // Write the dicts

                        File.WriteAllText(RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + pointName, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, pointName, OFfield));



                    }


                }
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
        public override Guid ComponentGuid => new Guid("{B9E3FDF3-5B76-456F-BE10-3A6EAFAB641A}");
    }
}



