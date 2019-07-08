using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCalcWindFactors : GH_Component
    {
        // exposure
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompCalcWindFactors()
          : base("Calculate WindFactors", "CalcWindFactors", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
        {
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "No interpolation", Menu_DoClick, true, !interpolate);
        }

        private void Menu_DoClick(object sender, EventArgs e)
        {
            interpolate = !interpolate;
            ExpireSolution(true);
        }

        public bool interpolate = true;

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetBoolean("Interpolation", interpolate);
            // Then call the base class implementation.
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            interpolate = reader.GetBoolean("Interpolation");
            // Then call the base class implementation.
            return base.Read(reader);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Res", "Res", "Res", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            pManager.AddVectorParameter("U", "U", "U", GH_ParamAccess.tree);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            pManager.AddPointParameter("Probes", "Probes", "Probes", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("WF", "WF", "WF", GH_ParamAccess.item);
            // pManager.AddGenericParameter("MRT_T", "MRT_T", "MRT_T", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // mode to select environment
            if (!interpolate) { Message = "No interpolation"; }
            else { Message = "Interpolation"; }

            OFResult RES = null;
            DA.GetData(0, ref RES);

            //// Hour of the year
            //List<int> hours = new List<int>() { 0 };
            //DA.GetDataList(1, hours);

            List<Point3d> probes = new List<Point3d>();
            DA.GetDataList("Probes", probes);
            var numberOfProbes = probes.Count;

            bool run = false;
            DA.GetData("Run", ref run);

            // Do not use DataTree inside actual components, it is only meant to be used inside script components. Use GH_Structure instead. https://www.grasshopper3d.com/forum/topics/getdatatree-fro-a-datatree-point3d
            // DataTree and GH_Structure are annoyingly similar yet non-overlapping classes.GH_Structure is used by Grasshopper itself to store data, DataTree is a version that was made specifically for the use inside script components.This part of the SDK is a mess but there's nothing we can do about it at this point.

            //Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> U = null;
            //DA.GetDataTree("U", out U);//
            DA.GetDataTree("U", out GH_Structure<GH_Vector> U);
            //DA.GetDataTree("U", out DataTree<Vector> U);

            #region Load weather

            Console.WriteLine("Load weather data...");

            //Weather data...

            if (RES.Domain.BCond.epwFilePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Without a weather file (.epw) connected you will not be able to perform outdoor comfort calculations.");
                return;
            }

            Weather weather = new Weather(RES.Domain.BCond.epwFilePath);

            #endregion Load weather

            var csvAnnualVelProbes = RES.WorkingDirectory + "AnnualVelocityProbes.csv";

            #region Annual Velocities

            AnnualVelocities av = new AnnualVelocities(RES.Domain.BCond.windDirs.ToArray(), ArrayHelper.To2DArrayVec3d(U), csvAnnualVelProbes, true);

            #endregion Annual Velocities

            var csvWindFactors = RES.WorkingDirectory + @"WindFactors.csv";

            var sum = 0.0;
            foreach (Point3d pp in probes) { sum += pp.Z; }
            var probingHeight = sum / probes.Count;

            if (probingHeight < RES.Domain.DomainMesh.GetBoundingBox(false).Min.Z || probingHeight > RES.Domain.DomainMesh.GetBoundingBox(false).Max.Z)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You cannot probe that set of probes outside of the simulation domain.");
                return;
            }

            WindFactors wf = new WindFactors(RES.WorkingDirectory, RES.Domain.BCond, weather, av, probingHeight, interpolate);
            var numberOfProbesCSV = RadianceFiles.readCSVFile(csvAnnualVelProbes).GetLength(1);

            if (File.Exists(csvWindFactors) && !run)
            {
                if (numberOfProbesCSV != numberOfProbes)
                {
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated WindFactors array has the wrong number of probing points. Please recalculate.");
                        return;
                    }
                }
            }

            if (run)
            {
                if (numberOfProbesCSV == numberOfProbes)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated WindFactors results have been loaded.");
                }

                ArrayHelper._2DArray2CSV(wf.Values, csvWindFactors, true, 1);
            }

            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }

            DA.SetData(0, wf);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        //protected override System.Drawing.Bitmap Icon =>
        // You can add image files to your project resources and access them like this:
        //Resources.Eddy_calMRT;

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{F165ADD0-A047-409E-A008-DD9CF82605F3}");
    }
}