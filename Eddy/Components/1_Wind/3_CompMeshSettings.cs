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
        private static readonly string[] ModeNames =
        {
            "No snapping, no layers",
            "With Snapping, no layers",
            "With Snapping, with layers (not always robust, >> RAM)"
        };

        private static readonly string[] PresetNames = { "Default", "GPT-53 Codex" };

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

        public override void CreateAttributes()
        {
            m_attributes = new DropdownComponentAttributes(this, new DropdownComponentAttributes.DropdownDef[]
            {
                new DropdownComponentAttributes.DropdownDef(7, ModeNames, 1),
                new DropdownComponentAttributes.DropdownDef(8, PresetNames, 0)
            });
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
                GH_ParamAccess.item, 3);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Feature, GH_Strings.MeshSettings.FeatureNick,
                GH_Strings.MeshSettings.FeatureDesc,
                GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.BBox, GH_Strings.MeshSettings.BBoxNick,
                GH_Strings.MeshSettings.BBoxDesc,
                GH_ParamAccess.item, 0);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Ground, GH_Strings.MeshSettings.GroundNick,
                GH_Strings.MeshSettings.GroundDesc,
                GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Layers, GH_Strings.MeshSettings.LayersNick,
                GH_Strings.MeshSettings.LayersDesc,
                GH_ParamAccess.item, 4);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Cells, GH_Strings.MeshSettings.CellsNick,
                GH_Strings.MeshSettings.CellsDesc,
                GH_ParamAccess.item, 5);

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Mode, GH_Strings.MeshSettings.ModeNick,
                GH_Strings.MeshSettings.ModeDesc,
                GH_ParamAccess.item, 1);
            if (pManager[7] is Param_Integer param1)
            {
                foreach (string name in ModeNames)
                {
                    param1.AddNamedValue(name, Array.IndexOf(ModeNames, name));
                }
            }

            pManager.AddIntegerParameter(
                GH_Strings.MeshSettings.Preset, GH_Strings.MeshSettings.PresetNick,
                GH_Strings.MeshSettings.PresetDesc,
                GH_ParamAccess.item, 0);
            if (pManager[8] is Param_Integer presetParam)
            {
                foreach (string name in PresetNames)
                {
                    presetParam.AddNamedValue(name, Array.IndexOf(PresetNames, name));
                }
            }
            pManager[8].Optional = true;
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
            int feat = 2;
            int bbox = 2;
            int ground = 2;
            int layers = 4;
            int cells = 5;
            int mode = 1;
            int preset = 0;

            DA.GetData(GH_Strings.MeshSettings.BldMin, ref bldMin);
            DA.GetData(GH_Strings.MeshSettings.BldMax, ref bldMax);
            DA.GetData(GH_Strings.MeshSettings.Feature, ref feat);
            DA.GetData(GH_Strings.MeshSettings.BBox, ref bbox);
            DA.GetData(GH_Strings.MeshSettings.Ground, ref ground);
            DA.GetData(GH_Strings.MeshSettings.Layers, ref layers);
            DA.GetData(GH_Strings.MeshSettings.Cells, ref cells);
            DA.GetData(GH_Strings.MeshSettings.Mode, ref mode);
            DA.GetData(GH_Strings.MeshSettings.Preset, ref preset);

            if (bldMax >= 5 || bldMin >= 5 || feat >= 5 || bbox >= 5 || ground >= 5 || layers >= 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A high number of refinement levels might significantly slow down mesh creation. Try to create a reasonably fine mesh with the Domain component and/or make sure to use more than one CPU.");
            }

            var meshSettings = new OFMeshSettings()
            {
                accBuildings = bldMin,
                accBuildingsMax = bldMax,
                accFeatures = feat,
                accBoxRefinement = bbox,
                accGround = ground,
                nLayers = layers,
                nCellsBetweenLevels = cells,
                snappySetting = (SnappySnapSettings)mode,
                preset = preset == (int)MeshPreset.GPT53Codex ? MeshPreset.GPT53Codex : MeshPreset.Default
            };

            if (meshSettings.preset == MeshPreset.GPT53Codex)
            {
                meshSettings.snappySetting = SnappySnapSettings.BlocksSnapping;
                meshSettings.nCellsBetweenLevels = Math.Max(5, meshSettings.nCellsBetweenLevels);
            }

            DA.SetData(GH_Strings.Common.MeshSettings, meshSettings);
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
