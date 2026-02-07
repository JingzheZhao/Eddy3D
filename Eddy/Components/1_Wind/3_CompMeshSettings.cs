using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class MeshSettings_Component : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the MeshSettings_Component class.
        /// </summary>
        public MeshSettings_Component()
          : base(
              GH_Strings.MeshSettings.Name, 
              GH_Strings.MeshSettings.Nick, 
              GH_Strings.MeshSettings.Desc + EddyVersion.toString(),
              EddyVersion.Name, 
              "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.BldMin, GH_Strings.MeshSettings.BldMinNick, 
                GH_Strings.MeshSettings.BldMinDesc, 
                GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.BldMax, GH_Strings.MeshSettings.BldMaxNick, 
                GH_Strings.MeshSettings.BldMaxDesc, 
                GH_ParamAccess.item, 4);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Feature, GH_Strings.MeshSettings.FeatureNick, 
                GH_Strings.MeshSettings.FeatureDesc, 
                GH_ParamAccess.item, 4);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.BBox, GH_Strings.MeshSettings.BBoxNick, 
                GH_Strings.MeshSettings.BBoxDesc, 
                GH_ParamAccess.item, 0);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Ground, GH_Strings.MeshSettings.GroundNick, 
                GH_Strings.MeshSettings.GroundDesc, 
                GH_ParamAccess.item, 3);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Misc, GH_Strings.MeshSettings.MiscNick, 
                GH_Strings.MeshSettings.MiscDesc, 
                GH_ParamAccess.item, 1);
            if (pManager[5] is Param_Integer param0)
            {
                param0.AddNamedValue("Default", 0);
                param0.AddNamedValue("Optimized", 1);
            }

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Layers, GH_Strings.MeshSettings.LayersNick, 
                GH_Strings.MeshSettings.LayersDesc, 
                GH_ParamAccess.item, 4);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Cells, GH_Strings.MeshSettings.CellsNick, 
                GH_Strings.MeshSettings.CellsDesc, 
                GH_ParamAccess.item, 4);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Mode, GH_Strings.MeshSettings.ModeNick, 
                GH_Strings.MeshSettings.ModeDesc, 
                GH_ParamAccess.item, 1);
            if (pManager[8] is Param_Integer param1)
            {
                param1.AddNamedValue("No snapping, no layers", 0);
                param1.AddNamedValue("With Snapping, no layers", 1);
                param1.AddNamedValue("With Snapping, with layers (not always robust, >> RAM)", 2);
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(GH_Strings.Common.MeshSettings, GH_Strings.Common.MeshSettingsNick, GH_Strings.Common.MeshSettingsDesc, GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int bldMin = 2;
            int bldMax = 4;
            int feat = 4;
            int bbox = 0;
            int ground = 3;
            int misc = 1;
            int layers = 4;
            int cells = 4;
            int mode = 1;

            DA.GetData(GH_Strings.MeshSettings.BldMin, ref bldMin);
            DA.GetData(GH_Strings.MeshSettings.BldMax, ref bldMax);
            DA.GetData(GH_Strings.MeshSettings.Feature, ref feat);
            DA.GetData(GH_Strings.MeshSettings.BBox, ref bbox);
            DA.GetData(GH_Strings.MeshSettings.Ground, ref ground);
            DA.GetData(GH_Strings.MeshSettings.Misc, ref misc);
            DA.GetData(GH_Strings.MeshSettings.Layers, ref layers);
            DA.GetData(GH_Strings.MeshSettings.Cells, ref cells);
            DA.GetData(GH_Strings.MeshSettings.Mode, ref mode);

            if (bldMax >= 5 || bldMin >= 5 || feat >= 5 || bbox >= 5 || ground >= 5 || layers >= 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A high number of refinement levels might significantly slow down mesh creation. Try to create a reasonably fine mesh with the Domain component and/or make sure to use more than one CPU.");
            }

            DA.SetData(GH_Strings.Common.MeshSettings, new OFMeshSettings()
            {
                accBuildings = bldMin,
                accBuildingsMax = bldMax,
                accFeatures = feat,
                accBoxRefinement = bbox,
                accGround = ground,
                miscSettings = (SnappyMiscSettings)misc,
                nLayers = layers,
                nCellsBetweenLevels = cells,
                snappySetting = (SnappySnapSettings)mode
            });
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_mesh_settings;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{012E1F38-3EEB-4B0E-BA77-3994AF3429F9}"); }
        }
    }
}
