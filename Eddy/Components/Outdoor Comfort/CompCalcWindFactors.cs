using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

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
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompCalcWindFactors()
          : base("WindFactors", "WindFactors", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
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
            pManager.AddGenericParameter("Result", "Res", "Eddy Result", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            pManager.AddPointParameter("Probing points", "Points", "List of probing points", GH_ParamAccess.list);
            pManager.AddVectorParameter("U", "U", "U", GH_ParamAccess.tree);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run the calculation", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("Wind Factors", "WF", "WF", GH_ParamAccess.item);
            pManager.AddGenericParameter("Pedestrian Comfort", "PD", "PD", GH_ParamAccess.list);
            pManager.AddGenericParameter("OffSet", "OF", "OffSet between simulated wind directions and directions in the weather file.", GH_ParamAccess.list);
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
            // mode to select environment
            if (!interpolate) { Message = "No interpolation"; }
            else { Message = "Interpolation"; }

            OFResult RES = null;
            DA.GetData(0, ref RES);

            //// Hour of the year
            //List<int> hours = new List<int>() { 0 };
            //DA.GetDataList(1, hours);

            List<Point3d> probes = new List<Point3d>();
            DA.GetDataList("Probing points", probes);

            bool run = false;
            DA.GetData("Run", ref run);

            // Do not use DataTree inside actual components, it is only meant to be used inside
            // script components. Use GH_Structure instead.
            // https://www.grasshopper3d.com/forum/topics/getdatatree-fro-a-datatree-point3d DataTree
            // and GH_Structure are annoyingly similar yet non-overlapping classes.GH_Structure is
            // used by Grasshopper itself to store data, DataTree is a version that was made
            // specifically for the use inside script components.This part of the SDK is a mess but
            // there's nothing we can do about it at this point.

            //Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> U = null;
            //DA.GetDataTree("U", out U);//
            DA.GetDataTree("U", out GH_Structure<GH_Vector> U);
            //DA.GetDataTree("U", out DataTree<Vector> U);

            #region Error checks

            var sum = 0.0;
            foreach (Point3d pp in probes) { sum += pp.Z; }
            var probingHeight = sum / probes.Count;

            if (probingHeight < RES.Domain.DomainMesh.GetBoundingBox(false).Min.Z || probingHeight > RES.Domain.DomainMesh.GetBoundingBox(false).Max.Z)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You cannot probe that set of probes outside of the simulation domain.");
                return;
            }

            #endregion Error checks

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

            #region Annual Velocities

            var csvAnnualVelProbes = RES.WorkingDirectory + "AnnualVelocityProbes.csv";
            AnnualVelocities av = new AnnualVelocities(RES.Domain.BCond.windDirs.ToArray(), ArrayHelper.To2DArrayVec3d(U), csvAnnualVelProbes, true, run);

            if (av.Values is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Either precalculated results could not be loaded or the AnnualVelocity array has not been calculated yet.");
                return;
            }

            if (av.wrongNumberOfProbes)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated AnnualVelocity array has the wrong number of probing points. Please recalculate.");
                return;
            }
            if (av.resultPrecalculated)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated AnnualVelocity results have been loaded.");
            }
            if (av.infValues)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some values probed velocities showed very large values which have been replaced with 0s.");
            }

            #endregion Annual Velocities

            #region Wind Factors

            var wf = new WindFactors(RES.WorkingDirectory, RES.Domain.BCond, weather, av, probingHeight, interpolate, run);

            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }

            if (wf.ValuesWindFactors is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Either precalculated results could not be loaded or the WindFactors array has not been calculated yet.");
                return;
            }

            if (wf.wrongNumberOfProbes)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated WindFactors array has the wrong number of probing points. Please recalculate.");
                return;
            }
            if (wf.resultPrecalculated)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated WindFactors results have been loaded.");
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The average offset between simulated wind directions and directions in the weather file is " + Math.Round(wf.offSetAverage, 1) + "°.");

            DA.SetData(0, wf);
            DA.SetDataList(1, wf.ValuesPedestrianComfort);
            DA.SetDataList(2, wf.offSet);

            #endregion Wind Factors
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_windFactors;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{F165ADD0-A047-409E-A008-DD9CF82605F3}");
    }
}