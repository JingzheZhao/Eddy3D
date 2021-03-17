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
    public class MeshSettings : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public MeshSettings()
          : base("Mesh Settings", "MSet",
              "Mesh Settings" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("AccBuilding", "AccBuilding", "Level accuracy of building mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("AccFeatures", "AccFeatures", "Level accuracy accuracy of building features (corners) mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("AccBBox", "AccBBox", "Level accuracy of building bounding box.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("AccGround", "AccGround", "Level accuracy of ground mesh.", GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter("MiscSettings.", "MiscS", "MiscSettings.", GH_ParamAccess.item, 1);
            Param_Integer param0 = pManager[4] as Param_Integer;
            param0.AddNamedValue("Default", 0);
            param0.AddNamedValue("Optimized", 1);

            pManager.AddIntegerParameter("Number of layers", "nLay", "Number of mesh layers.", GH_ParamAccess.item, 4);
            pManager.AddIntegerParameter("Mode", "Mode", @"Mode:
0: No snapping, no layers
1: With Snapping, no layers
2: With Snapping, with layers", GH_ParamAccess.item, 1);
            Param_Integer param1 = pManager[6] as Param_Integer;
            param1.AddNamedValue("No snapping, no layers", 0);
            param1.AddNamedValue("With Snapping, no layers", 1);
            param1.AddNamedValue("With Snapping, with layers (not always robust, >> RAM)", 2);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Mesh Settings", "MSet", "Mesh Settings", GH_ParamAccess.item);
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
            int _accBuilding = 3;
            int _accFeatures = 3;
            int _accRefinement = 3;
            int _accGround = 3;

            int _miscSettings = 1;

            int _nLayers = 3;
            int _mode = 1;

            DA.GetData(0, ref _accBuilding);
            DA.GetData(1, ref _accFeatures);
            DA.GetData(2, ref _accRefinement);
            DA.GetData(3, ref _accGround);

            DA.GetData(4, ref _miscSettings);

            DA.GetData(5, ref _nLayers);
            DA.GetData(6, ref _mode);

            if (_accBuilding >= 5 || _accFeatures >= 5 || _accRefinement >= 5 || _accGround >= 5 || _nLayers >= 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A high number of refinment levels might significantly slow down mesh creation. Try to create a reasonable fine mesh with the Domain component and/or make sure to use more than one CPU.");
            }

            DA.SetData(0, new OFMeshSettings()
            {
                accBuildings = _accBuilding,
                accFeatures = _accFeatures,
                accRefinement = _accRefinement,
                accGround = _accGround,
                miscSettings = (SnappyMiscSettings)_miscSettings,
                nLayers = _nLayers,
                snappySetting = (SnappySnapSettings)_mode
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