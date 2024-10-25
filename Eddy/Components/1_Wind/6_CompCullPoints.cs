using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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
              EddyVersion.Name, "5 | Post-Processing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Building Mesh", "BM", "Joined Building Mesh.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Ground Mesh", "GM", "Ground Mesh.", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Target Count", "TC", "TC.", GH_ParamAccess.item, 50000);
            pManager.AddBooleanParameter("Convert Quads to Triangles", "QT", "Convert quads to triangles in the resulting mesh.", GH_ParamAccess.item, true);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
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
            List<Mesh> BuildingMesh = new List<Mesh>();
            List<Mesh> GroundMesh = new List<Mesh>();

            bool QT = true;
            int TargetCount = 50000;

            DA.GetDataList(0, BuildingMesh);
            DA.GetDataList(1, GroundMesh);

            DA.GetData(2, ref TargetCount);
            DA.GetData(3, ref QT);

            Mesh JoinedGroundMesh = JoinMeshes(GroundMesh);
            Mesh JoinedBuildingMesh = JoinMeshes(BuildingMesh);

            var outsidePoints = Utilities.GetOutsidePoints(JoinedGroundMesh, JoinedBuildingMesh, TargetCount);
            JoinedGroundMesh.Vertices.Remove(outsidePoints, QT);

            DA.SetData(0, JoinedGroundMesh);
        }

        private Mesh JoinMeshes(List<Mesh> list)
        {
            // Remove all null meshes from the list
            list.RemoveAll(mesh => mesh == null);

            // Create a new mesh to store the combined result
            Mesh resultMesh = new Mesh();

            // If there are any remaining meshes in the list, append them to the resultMesh
            if (list.Count > 0)
            {
                resultMesh.Append((IEnumerable<Mesh>)list);
            }

            // If the resultMesh has vertices, return it; otherwise, return null (or handle as needed)
            if (resultMesh.Vertices.Count > 0)
            {
                return resultMesh;
            }

            return null; // Handle no valid meshes case
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