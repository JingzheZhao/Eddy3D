using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Eddy.Properties;
using EddyLib;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class MeshSettings : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public MeshSettings()
          : base("Mesh Settings", "MSet",
              "Mesh Settings",
              "Eddy", "2 | Settings")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("AccBuilding", "AccBuilding", "Specify accuracy of building mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("AccFeatures", "AccFeatures", "Specify accuracy of building features (corners) mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("AccRefinement", "AccRefinement", "Specify accuracy of bounding box mesh.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("AccGround", "AccGround", "Specify accuracy of ground mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("nLayer", "nLay", "Number of mesh layers.", GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("Mode", "Mode", @"Mode: 
0: No snapping, no layers
1: With Snapping, no layers
2: With Snapping, with layers", GH_ParamAccess.item, 2);
            Param_Integer param = pManager[5] as Param_Integer;
            param.AddNamedValue("No snapping, no layers", 0);
            param.AddNamedValue("With Snapping, no layers", 1);
            param.AddNamedValue("With Snapping, with layers", 2);
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
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            int _accBuilding = 3;
            int _accFeatures = 3;
            int _accRefinement = 3;
            int _accGround = 3;
            int _nLayers = 3;
            int _mode = 2;



            DA.GetData(0, ref _accBuilding);
            DA.GetData(1, ref _accFeatures);
            DA.GetData(2, ref _accRefinement);
            DA.GetData(3, ref _accGround);
            DA.GetData(4, ref _nLayers);
            DA.GetData(5, ref _mode);


            DA.SetData(0, new OFMeshSettings() {

            accBuildings = _accBuilding,
            accFeatures = _accFeatures,
            accRefinement = _accRefinement,
            accGround = _accGround,
            nLayers = _nLayers,
            snappySetting = (SnappySetting) _mode

            });

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_mesh_settings;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{012E1F38-3EEB-4B0E-BA77-3994AF3429F9}"); }
        }
    }
}
