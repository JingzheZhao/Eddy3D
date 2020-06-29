using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompComputeFlowRateFromU : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompComputeFlowRateFromU()
          : base("Flow Rates", "Flow Rates", "Compute flow rates from velocity probes" + EddyVersion.toString(),
              EddyVersion.Name, "5 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddVectorParameter("Velocity vectors", "U", "List of velocity vectors.", GH_ParamAccess.list);

            //pManager.AddGenericParameter("Area", "Area", "Area to be evaluated.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh surface to be evaluated.", GH_ParamAccess.item);

            //pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddNumberParameter("Result", "Res", "Result: Volumetric flow rate in m^3/s.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min", "Min", "Minimum value in m/s", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max", "Max", "Maximum value in m/s", GH_ParamAccess.item);
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
            //OFBaseDomain DOM = null;

            //GH_ObjectWrapper gobj = null;
            //if (!DA.GetData(0, ref gobj)) { }

            //if ((gobj.Value is OFBaseDomain))
            //{
            //    DOM = (OFBaseDomain)gobj.Value;
            //}
            //if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }

            var mesh = new Mesh();

            DA.GetData(1, ref mesh);

            StringBuilder errorLog = new StringBuilder();

            var listInputVelocities = new List<Vector3d>();
            var ArrayInputVelocities = new Vector3d[listInputVelocities.Count];
            DA.GetDataList(0, listInputVelocities);

            double AverageFlowRate = 0;
            double Min = 0;
            double Max = 0;
            var Magnitudes = new List<double>();
            double VolumetricFlowRate = 0.0;

            try
            {
                double Area = 0;

                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    Area += (Utilities.MeshFaceArea(i, mesh));
                }

                DA.GetDataList(0, listInputVelocities);

                int cnt = 0;

                // Clean input

                //Build array

                //for (int i= 0; i< listInputVelocities.Count; i++)
                //{
                //    ArrayInputVelocities[i] = listInputVelocities[i];
                //}

                //var cleanedVelocities = Utilities.FilterExtremeVectorLengths(ArrayInputVelocities);
                //var cleanedVelocities = ArrayInputVelocities;

                // Compute average flow rate for all probes

                foreach (Vector3d U in listInputVelocities)
                {
                    AverageFlowRate += U.Length;
                    Magnitudes.Add(U.Length);
                    cnt++;
                }

                AverageFlowRate = AverageFlowRate / cnt; // m/s

                VolumetricFlowRate = AverageFlowRate * Area;

                Min = Magnitudes.Any() ? Magnitudes.Min(x => x) : 0;
                Max = Magnitudes.Any() ? Magnitudes.Max(x => x) : 0;
            }
            catch (Exception e) { Console.WriteLine(e.Message); };// File.WriteAllText(RES.WorkingDirectoryectory + @"\FlowRate.err", errorLog.ToString()); return; }

            DA.SetData(0, VolumetricFlowRate);
            DA.SetData(1, Min);
            DA.SetData(2, Max);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_flow;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{4ABC334E-1FEC-41B2-9852-D005151DD79B}");
    }
}