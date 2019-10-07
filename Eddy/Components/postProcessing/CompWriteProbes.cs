using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class WriteProbes : GH_Component
    {
        private DataTree<double> cpTree = new DataTree<double>();
        private DataTree<Vector3d> uTree = new DataTree<Vector3d>();

        // exposure
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public WriteProbes()
          : base("WriteProbes", "WriteProbes", "WriteProbes", "Eddy", "5 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation result", "Res", "Eddy simulation result", GH_ParamAccess.item);
            pManager.AddPointParameter("Porbing points", "Points", "List of probing points", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "Name", "Name", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Field", "Field", "Field", GH_ParamAccess.item, 1);
            Param_Integer param = pManager[3] as Param_Integer;
            param.AddNamedValue("U", 0);
            param.AddNamedValue("total(p)_coeff", 1);
            param.AddNamedValue("p", 2);
            param.AddNamedValue("epsilon", 3);
            param.AddNamedValue("omega", 4);
            param.AddNamedValue("k", 5);
            param.AddNamedValue("nut", 6);
            param.AddNamedValue("phi", 7);

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
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OFResult RES = null;
            DA.GetData("Result", ref RES);

            int OFFieldInt = 0;
            List<Point3d> listOfPoints = new List<Point3d>();
            string probeName = "";

            DA.GetDataList("List of Points", listOfPoints);
            DA.GetData("Name", ref probeName);
            DA.GetData("Field", ref OFFieldInt);
            //DA.GetData(3, ref run);

            listOfPoints = Utilities.DiscardPoints(listOfPoints, RES.Domain);

            int numberOfProbes = listOfPoints.Count();

            // Error handling

            if (numberOfProbes < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of point to the component.");
            }

            // Export probes file
            File.WriteAllText(Path.Combine(RES.WorkingDirectory + "\\" + "run_probes.bat"), EddyLib.StrTemp.BatFiles.Run_Probes(RES.Domain, RES.MeshSettings));

            // export pts file for Daysim
            if (!Directory.Exists(RES.WorkingDirectory + @"Rad\"))
            {
                Directory.CreateDirectory(RES.WorkingDirectory + @"Rad\");
            }
            RadianceFiles.writePTS(RES.WorkingDirectory + @"\Rad\sensors.pts", listOfPoints);

            OFField currField = new OFField(OFField.ReformatOFFields(OFFieldInt), probeName);

            if (numberOfProbes > 0)
            {
                cpTree = new DataTree<double>();
                uTree = new DataTree<Vector3d>();

                if (OFFieldInt == 0) // cp
                {
                    string pointName = "cp_Probes";

                    for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                    {
                        File.WriteAllText(RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + "controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(RES.RunSettings, RES.Domain, null, i));
                        File.WriteAllText(RES.WorkingDirectory + RES.Domain.BCond.windDirs[i] + @"\system\" + pointName, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, currField));
                    }
                }

                if (OFFieldInt == 1) // U
                {
                    string pointName = "U_Probes";

                    foreach (int v in RES.Domain.BCond.windDirs)
                    {
                        // Write the dicts

                        File.WriteAllText(RES.WorkingDirectory + v + @"\system\" + pointName, EddyLib.StrTemp.OFExecDicts.SampleProbes(listOfPoints, currField));
                    }
                }
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_writeProbs;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{B9E3FDF3-5B76-456F-BE10-3A6EAFAB641A}");
    }
}