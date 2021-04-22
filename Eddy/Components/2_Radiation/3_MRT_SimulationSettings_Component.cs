using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Eddy.Components._2_Radiation
{
    public class SimulationSettings_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary | GH_Exposure.obscure; }
        }
        /// <summary>
        /// Initializes a new instance of the _2_SurfaceMaterialSettings_Component class.
        /// </summary>
        public SimulationSettings_Component()
          : base("Settings", "Set", "Simulation settings, A higher VFC will slow down the simulation, approximately 30-50% for every 0.1 increase" + EddyVersion.toString(), EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("ComputeReflectionsAndDiffuseRadiation", "Refl", "ComputeReflectionsAndDiffuseRadiation", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("CummulativeViewFactorCutoff", "VFC", "CummulativeViewFactorCutoff", GH_ParamAccess.item, 0.2);
            pManager.AddBooleanParameter("ComputeSurfaceTemperatureEnergyPlus", "Ep", "ComputeSurfaceTemperatureEnergyPlus", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("ComputeLongWaveExchangeEnergyPlus", "LWR", "ComputeLongWaveExchangeEnergyPlus", GH_ParamAccess.item, false);
         }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("Settings", "Set", "Simulation settings ");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool ComputeReflectionsAndDiffuseRadiation = true;
            double CummulativeViewFactorCutoff = 0.2;
            bool ComputeSurfaceTemperatureEnergyPlus = true;
            bool ComputeLongWaveExchangeEnergyPlus = false;
           

            if (!DA.GetData(0, ref ComputeReflectionsAndDiffuseRadiation)) return;
            if (!DA.GetData(1, ref CummulativeViewFactorCutoff)) return;
            if (!DA.GetData(2, ref ComputeSurfaceTemperatureEnergyPlus)) return;
            if (!DA.GetData(3, ref ComputeLongWaveExchangeEnergyPlus)) return;
 

            MRT_Simulation_Settings settings = new MRT_Simulation_Settings();
            settings.ComputeReflectionsAndDiffuseRadiation = ComputeReflectionsAndDiffuseRadiation;
            settings.CummulativeViewFactorCutoff = CummulativeViewFactorCutoff;
            settings.ComputeSurfaceTemperatureEnergyPlus = ComputeSurfaceTemperatureEnergyPlus;
            settings.ComputeLongWaveExchangeEnergyPlus = ComputeLongWaveExchangeEnergyPlus;

            DA.SetData(0, settings);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.Eddy_MRT_Sim_Settings;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{92C04889-BB25-4BBA-80D0-06ED65C18399}"); }
        }
    }
}
