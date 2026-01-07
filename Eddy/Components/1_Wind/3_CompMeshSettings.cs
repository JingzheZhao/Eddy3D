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
              "Mesh Settings", 
              "MSet", 
              @"Configure snappyHexMesh refinement levels for CFD simulation.

Higher refinement levels = finer mesh = more accurate but slower.
Levels 2-3 are typical for buildings. Level 4+ requires significant RAM.

" + EddyVersion.toString(),
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
                "Building Min Level", "BldMin", 
                "Minimum refinement level for building surfaces. Higher = finer. Typical: 2-3. Default: 2", 
                GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter(
                "Building Max Level", "BldMax", 
                "Maximum refinement level for building surfaces. Must be >= min. Default: 2", 
                GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter(
                "Feature Level", "Feat", 
                "Refinement level for building corners and features. Default: 2", 
                GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter(
                "Bounding Box Level", "BBox", 
                "Refinement level for region around buildings. 0 = no extra refinement. Default: 0", 
                GH_ParamAccess.item, 0);

            pManager.AddIntegerParameter(
                "Ground Level", "Gnd", 
                "Refinement level for ground surface. Default: 2", 
                GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter(
                "Misc Settings", "Misc", 
                "0: Default, 1: Optimized quality settings", 
                GH_ParamAccess.item, 1);
            if (pManager[5] is Param_Integer param0)
            {
                param0.AddNamedValue("Default", 0);
                param0.AddNamedValue("Optimized", 1);
            }

            pManager.AddIntegerParameter(
                "Boundary Layers", "nLay", 
                "Number of mesh layers near walls. More = better boundary layer resolution. Default: 4", 
                GH_ParamAccess.item, 4);

            pManager.AddIntegerParameter(
                "Cells Between Levels", "nCells", 
                "Number of cells between refinement levels. More = smoother transition. Default: 4", 
                GH_ParamAccess.item, 4);

            pManager.AddIntegerParameter(
                "Mesh Mode", "Mode", 
                "0: No snapping (fast debug), 1: With snapping (production), 2: With layers (accurate but slow)", 
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
            pManager.AddGenericParameter("Mesh Settings", "MSet", "Mesh settings object to connect to Simulation component", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int bldMin = 2;
            int bldMax = 2;
            int feat = 2;
            int bbox = 0;
            int ground = 2;
            int misc = 1;
            int layers = 4;
            int cells = 4;
            int mode = 1;

            DA.GetData("Building Min Level", ref bldMin);
            DA.GetData("Building Max Level", ref bldMax);
            DA.GetData("Feature Level", ref feat);
            DA.GetData("Bounding Box Level", ref bbox);
            DA.GetData("Ground Level", ref ground);
            DA.GetData("Misc Settings", ref misc);
            DA.GetData("Boundary Layers", ref layers);
            DA.GetData("Cells Between Levels", ref cells);
            DA.GetData("Mesh Mode", ref mode);

            if (bldMax >= 5 || bldMin >= 5 || feat >= 5 || bbox >= 5 || ground >= 5 || layers >= 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A high number of refinement levels might significantly slow down mesh creation. Try to create a reasonably fine mesh with the Domain component and/or make sure to use more than one CPU.");
            }

            DA.SetData("Mesh Settings", new OFMeshSettings()
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