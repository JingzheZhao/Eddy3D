using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCalcWindFactors : GH_Component
    {// exposure
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary | GH_Exposure.obscure; }
        }

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
          : base("Wind Factors", "Wind Factors", @"Wind Factors

Based on the probed simulation and the weather data, this component calculates wind velocities, wind factors for each probing point [8760 hourly branches x number of probing points].
The wind factors are calculated based on the wind velocity and direction for each hour which is scaled up/down accordingly given probing height from ground.
For this, we support either a look-up for the closest simulated wind direction or an interpolation between the closest two wind directions.
" + EddyVersion.toString(),
              EddyVersion.Name, "3 | PostProcessing")
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

        public bool interpolate = false;

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
            pManager.AddPointParameter("Probing points", "Points", "List of probing points (caution: might have been culled). It is assumed that your probes are probed at z= 2, regardless of the z-values of the probing points.", GH_ParamAccess.list);
            pManager.AddVectorParameter("Wind Velocity", "U", @"Wind Velocity [DataTree] where the [branches] are the wind directions and the [items] are the values for each probing point.", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Run", "Run", "Run the calculation", GH_ParamAccess.item);

            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Wind Factors Spatial", "WFS", @"Wind Amplification Factors Spatial

Wind Amplification Factors (dimensionless wind velocity) for each simulated wind direction.
This yields a datatree of the size [Number of simulated wind directions x number of sensor points].", GH_ParamAccess.item);

            pManager.AddGenericParameter("Wind Factors Annual", "WFA", @"Wind Factors Annual

Wind Factors multiplied with the corresponding EPW wind velocity from the nearest simulated wind direction for every hour of the year.
This yields a datatree of the size [8760 h x number of sensor points].", GH_ParamAccess.item);

            //  pManager.AddGenericParameter("OffSet", "OffS", "OffSet between simulated wind directions and directions in the weather file.", GH_ParamAccess.list);
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

            List<Point3d> probes = new List<Point3d>();
            DA.GetDataList("Probing points", probes);

            bool run = false;
            DA.GetData("Run", ref run);

            DA.GetDataTree("Wind Velocity", out GH_Structure<GH_Vector> U);

            #region Error checks

            if (probes.Any(val => val.Z < RES.Domain.DomainMesh.GetBoundingBox(false).Min.Z || probes.Any(val2 => val2.Z > RES.Domain.DomainMesh.GetBoundingBox(false).Max.Z)))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You cannot probe that set of probes outside of the simulation domain.");
                return;
            }

            #endregion Error checks

            #region Annual Velocities

            MultiDirectionalVelocities mdv = new MultiDirectionalVelocities(RES.WorkingDirectory, RES.Domain.BCond.WindDirections.ToArray(), GrasshopperConversions.To2DArrayVec3d(U), true, run);

            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }

            if (mdv.wrongNumberOfProbes)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated spatial wind factor array has the wrong number of probing points. Please recalculate.");
                return;
            }

            if (mdv.Values is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Either precalculated results could not be loaded or the AnnualVelocity array has not been calculated yet.");
                return;
            }

            if (mdv.resultPrecalculated)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated spatial wind factor results have been loaded.");
            }
            if (mdv.infValues)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some probed velocities with very large values (likely because they weren't inside the simulation domain) have been replaced with 0s.");
            }

            #endregion Annual Velocities

            #region Wind Factors

            var wfspatial = new WindFactorsSpatial(RES.WorkingDirectory, RES.Domain.BCond, mdv, probes, interpolate, run);

            DA.SetData(0, wfspatial);

            if (RES.Domain.BCond.epwFilePath == "" || RES.Domain.BCond.epwFilePath == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Without a weather file (.epw) connected you will not be able to perform the annual wind comfort calculations.");
                return;
            }

            if ((RES.Domain.BCond.epwFilePath.EndsWith(".epw")))
            {
                #region Load weather

                Console.WriteLine("Load weather data...");

                Weather weather = new Weather(RES.Domain.BCond.epwFilePath);

                #endregion Load weather

                var wftemporal = new WindFactorsTemporal(RES.WorkingDirectory, RES.Domain.BCond, weather, wfspatial, probes, interpolate, run);

                if (GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }

                if (wftemporal.ValuesTemporalAtProbingHeight is null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Either precalculated results could not be loaded or the WindFactors array has not been calculated yet.");
                    return;
                }

                if (wftemporal.wrongNumberOfProbes)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated results the wrong number of probing points. Please recalculate.");
                    return;
                }
                if (wftemporal.resultPrecalculated)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated Wind Factor results have been loaded.");
                }

                DA.SetData(1, wftemporal);
            }

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