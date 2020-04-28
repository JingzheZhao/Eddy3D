using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Linq;

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
          : base("Annual Pedestrian Comfort", "Pedestrian Comfort", @"Pedestrian Comfort

Based on the weather data input, this component calculates wind velocities, wind factors, and pedestrian comfort idices for each probing point [8760 hourly branches x number of probing points].
The wind factors are calculated based on the wind velocity and direction for each hour which is scaled up/down accordingly given probing height from ground.
For this, we support either a look-up for the closest simulated wind direction or an interpolation between the closest two wind directions.
" + EddyVersion.toString(),
              EddyVersion.Name, "6 | Outdoor Comfort")
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
            pManager.AddPointParameter("Probing points", "Points", "List of probing points (caution: might have been culled)", GH_ParamAccess.list);
            pManager.AddVectorParameter("Wind Velocity", "U", @"Wind Velocity [DataTree] where the [branches] are the wind directions and the [items] are the values for each probing point.", GH_ParamAccess.tree);

            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Comfort Index", "CmftIdx", "Select a Pedestrian Wind Comfort Index with a right click.", GH_ParamAccess.item, 0);

            //Using an enum to generate the dropdown items
            var types = Enum.GetNames(typeof(EddyLib.PedestrianComfort.PedestrianComfortIdx));
            Param_Integer param = pManager[3] as Param_Integer;

            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }

            pManager.AddBooleanParameter("Run", "Run", "Run the calculation", GH_ParamAccess.item);

            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("Wind Velocities", "WF", @"Wind Velocities

Wind velocity of each probing point from the nearest simulated wind direction multiplied with the corresponding velocity from the weather data for every hour of the year.
This yields a datatree of the size [8760 h x number of sensor points].", GH_ParamAccess.item);

            pManager.AddGenericParameter("Wind Factors", "WF", @"Wind Factors

Dimensionless wind velocity of each probing point from the nearest simulated wind direction for every hour of the year.
This yields a datatree of the size [8760 h x number of sensor points].", GH_ParamAccess.item);

            pManager.AddNumberParameter("Pedestrian Wind Comfort", "Cmft", @"Pedestrian Wind Comfort

General Lawson

1 - A > 1.8 m/s < 2 % Sitting Long
2 - B > 3.6 m/s < 2 % Sitting Short
3 - C > 5.3 m/s < 2 % Walking Leisurely
4 - D > 7.6 m/s > 5 % Walking Fast
5 - E > 7.6 m/s >= 2 % Uncomfortable

Lawson LDDC

1 - A > 2.5 m/s < 5 % Frequent sitting
2 - B > 4 m/s < 5 % Occasional sitting
3 - C > 6 m/s < 5 % Standing
4 - D > 8 m/s < 5 % Walking
5 - E > 8 m/s > 5 % Uncomfortable
6 - S > 15 m/s > 0.022 % Unsafe

Lawson 2001

1 - A   > 4 m/s < 5 % Sitting
2 - B   > 6 m/s < 5 % Standing
3 - C   > 8 m/s < 5 % Strolling
4 - D   > 10 m/s < 5 % Business Walking
5 - E   > 10 m/s > 5 % Uncomfortable
6 - S15 > 15 m/s > 0.023 % Unsafe frail
7 - S20 > 20 m/s > 0.023 % Unsafe all

Davenport

1 - A > 3.6 m/s < 1.5 % Sitting Long
2 - B > 5.3 m/s < 1.5 % Sitting Short
3 - C > 7.6 m/s < 1.5 % Walking Leisurely
4 - D > 9.8 m/s < 1.5 % Walking Fast
5 - E > 9.8 m/s >= 1.5 % Uncomfortable
6 - S > 15.1 m/s >= 0.01 % Dangerous

NEN8100

1 - A > 5 m/s < 2.5 % Sitting Long
2 - B > 5 m/s < 5 % Sitting Short
3 - C > 5 m/s < 10 % Walking Leisurely
4 - D > 5 m/s < 20 % Walking Fast
5 - E > 5 m/s > 20 % Uncomfortable
6 - S > 15 m/s > 0.05 % Dangerous", GH_ParamAccess.list);

            pManager.AddGenericParameter("OffSet", "OffS", "OffSet between simulated wind directions and directions in the weather file.", GH_ParamAccess.list);
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

            int cmftidx = 0;
            DA.GetData("Comfort Index", ref cmftidx);
            PedestrianComfort.PedestrianComfortIdx cmftcmftindex = (PedestrianComfort.PedestrianComfortIdx)cmftidx;

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
            DA.GetDataTree("Wind Velocity", out GH_Structure<GH_Vector> U);

            //DA.GetDataTree("U", out DataTree<Vector> U);

            #region Error checks

            if (probes.Any(val => val.Z < RES.Domain.DomainMesh.GetBoundingBox(false).Min.Z || probes.Any(val2 => val2.Z > RES.Domain.DomainMesh.GetBoundingBox(false).Max.Z)))
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

            PedestrianComfort av = new PedestrianComfort(RES.WorkingDirectory, RES.Domain.BCond.windDirs.ToArray(), ArrayHelper.To2DArrayVec3d(U), true, run);

            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }

            if (av.wrongNumberOfProbes)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated AnnualVelocity array has the wrong number of probing points. Please recalculate.");
                return;
            }

            if (av.Values is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Either precalculated results could not be loaded or the AnnualVelocity array has not been calculated yet.");
                return;
            }

            if (av.resultPrecalculated)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated AnnualVelocity results have been loaded.");
            }
            if (av.infValues)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some probed velocities with very large values (likely because they weren't inside the simulation domain) have been replaced with 0s.");
            }

            #endregion Annual Velocities

            #region Wind Factors

            var wf = new WindFactors(RES.WorkingDirectory, RES.Domain.BCond, weather, av, probes, interpolate, run, cmftcmftindex);

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

            if (wf.offSetAverage >= 13)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The average offset between simulated wind directions and directions in the weather file is " + Math.Round(wf.offSetAverage, 2) + "°. You might want to consider changing the input wind directions to better fit the weather file.");
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The average offset between simulated wind directions and wind directions in the weather file is " + Math.Round(wf.offSetAverage, 2) + "°.");
            }

            DA.SetData(0, wf);
            DA.SetDataList(1, wf.ValuesPedestrianWindComfort);
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