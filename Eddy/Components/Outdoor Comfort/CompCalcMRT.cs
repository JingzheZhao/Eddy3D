using System;
using System.Collections.Generic;
using System.IO;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCalcMRT : GH_Component
    {
        // exposure
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompCalcMRT()
          : base("MRT", "MRT", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation Result", "Res", "Simulation Result", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            pManager.AddPointParameter("Porbing points", "Points", "List of probing points", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run the calculation", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("Mean Radiant Temperature [°C]", "MRT", "Mean Radiant Temperature [°C] Object", GH_ParamAccess.item);
            // pManager.AddGenericParameter("MRT_T", "MRT_T", "MRT_T", GH_ParamAccess.tree);
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
            DA.GetData(0, ref RES);

            //// Hour of the year
            //List<int> hours = new List<int>() { 0 };
            //DA.GetDataList(1, hours);

            List<Point3d> probes = new List<Point3d>();
            DA.GetDataList("Points", probes);
            var numberOfProbes = probes.Count;
            var probesArr = probes.ToArray();

            bool run = false;
            DA.GetData("Run", ref run);

            #region Load prerequisites

            Console.WriteLine("Load weather data...");

            //Weather data...

            if (RES.Domain.BCond.epwFilePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Without a weather file (.epw) connected you will not be able to perform outdoor comfort calculations.");
                return;
            }

            if (Utilities.HasWhiteSpace(RES.WorkingDirectory))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "FilePath cannot contain whitespaces to perform any outdoor comfort calculations at this time.");
                return;
            }

            Weather weather = new Weather(RES.Domain.BCond.epwFilePath);

            #endregion Load prerequisites

            #region Daysim

            double[][] DiffRad = null;
            double[][] DirRad = null;

            var difillFile = RES.WorkingDirectory + @"\Rad\CallRay.dif.ill";
            var dirillFile = RES.WorkingDirectory + @"\Rad\CallRay.dir.ill";

            if (File.Exists(difillFile) && File.Exists(dirillFile) && new FileInfo(difillFile).Length != 0 && new FileInfo(dirillFile).Length != 0)
            {
                // Load radiation datasets [x][] time [][x] points

                DiffRad = RadianceFiles.loadILL(difillFile);
                DirRad = RadianceFiles.loadILL(dirillFile);

                int sensorPointCountExisting = DiffRad[0].GetLength(0);

                if (sensorPointCountExisting != numberOfProbes && !run)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated Daysim results do not have the correct number of probing points. The results need to be recalculated.");
                    return;
                }

                if (sensorPointCountExisting != numberOfProbes && run)
                {
                    Utilities.CleanDirectory(RES.MeshSettings.baseWorkingDir + @"Rad\");

                    Daysim ds = new Daysim(RES.WorkingDirectory, RES.Domain.BuildingGeometry, probes, weather);

                    DiffRad = ds.difIll;
                    DirRad = ds.dirIll;

                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated Daysim results did not have the correct number of probing points. Results have been recalculated.");
                }
                else if (sensorPointCountExisting == numberOfProbes && !run)
                {
                    DiffRad = RadianceFiles.loadILL(difillFile);
                    DirRad = RadianceFiles.loadILL(dirillFile);

                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated Daysim results have been loaded.");
                }
            }
            else if (run)
            {
                Utilities.CleanDirectory(RES.MeshSettings.baseWorkingDir + @"Rad\");

                Daysim ds = new Daysim(RES.WorkingDirectory, RES.Domain.BuildingGeometry, probes, weather);

                DiffRad = ds.difIll;
                DirRad = ds.dirIll;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No precalculated Daysim results found. Please calculate.");
            }

            #endregion Daysim

            #region MRT

            var mrt = new MRT(RES.WorkingDirectory, weather, MRT.MRTType.kessling, DiffRad, DirRad, probesArr, run);

            if (mrt.Values is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Either precalculated results could not be loaded or the MRT array has not been calculated yet.");
                return;
            }

            if (mrt.resultPrecalculated)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated MRT results have been loaded.");
            }
            if (mrt.wrongNumberOfProbes)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated MRT array has the wrong number of probing points. Please recalculate.");
                return;
            }
            DA.SetData(0, mrt);

            #endregion MRT
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_calMRT;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{FE115CFE-B2B9-4DC6-8DBE-DDB43710090C}");
    }
}