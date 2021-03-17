using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCullPoints : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.senary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompCullPoints()
          : base("Probing", "Probing", @"Solve Building/Ground Mesh intersection (can be slow for a large number of points and/or a large building mesh).
" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Building Mesh", "BM", "Joined Building Mesh.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Ground Mesh", "GM", "Ground Mesh.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Convert Quads to Triangles", "QT", "Convert quads to triangles in the resulting mesh.", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Culled Ground Mesh", "CGM", "Culled ground mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        ///

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh BuildingMesh = null;
            Mesh GroundMesh = null;
            bool QT = false;
            DA.GetData(0, ref BuildingMesh);
            DA.GetData(1, ref GroundMesh);
            DA.GetData(2, ref QT);

            var outsidePoints = Utilities.GetOutsidePoints(GroundMesh, BuildingMesh, 0.5);
            GroundMesh.Vertices.Remove(outsidePoints, QT);

            DA.SetData(0, GroundMesh);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_cullPoints;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{3CEADF77-9A6A-4CB1-B074-D7A036DE6F96}");
    }
}