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
          : base("Flow Rates", "Flow Rates", "Compute flow rates across a mesh while treating its vertices as velocity probes." + EddyVersion.toString(),
              EddyVersion.Name, "5 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddVectorParameter("Velocity vectors", "U", "List of velocity vectors that correspond the the vertices of a mesh.", GH_ParamAccess.list);

            //pManager.AddGenericParameter("Area", "Area", "Area to be evaluated.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh with vertices are to be evaluated.", GH_ParamAccess.item);

            //pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddNumberParameter("FlowRates", "FlowRates", "Volumetric flow rates of mesh faces in m^3/s.", GH_ParamAccess.list);
            pManager.AddPointParameter("Centers", "Centers", "Centers of the mesh faces", GH_ParamAccess.list);
            pManager.AddVectorParameter("FlowDirs", "FlowDirs", "FlowDirs ", GH_ParamAccess.list);
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
            var m = new Mesh();

            DA.GetData(1, ref m);

            List<Vector3d> U = new List<Vector3d>();

            try
            {
                DA.GetDataList(0, U);

                var faces = m.Faces;
                var faceNormals = m.FaceNormals;

                var flowRates = new double[m.Faces.Count];
                var florDirs = new Vector3d[m.Faces.Count];
                var meshFaceCenters = new Point3d[m.Faces.Count];

                m.FaceNormals.ComputeFaceNormals();

                for (int mf_id = 0; mf_id < m.Faces.Count; mf_id++)
                {
                    //get points into a nice, concise format

                    Point3d[] pts = new Point3d[4];
                    pts[0] = m.Vertices[m.Faces[mf_id].A];
                    pts[1] = m.Vertices[m.Faces[mf_id].B];
                    pts[2] = m.Vertices[m.Faces[mf_id].C];
                    if (m.Faces[mf_id].IsQuad)
                    {
                        pts[3] = m.Vertices[m.Faces[mf_id].D];
                    }

                    var avgVec = new Vector3d();
                    var center = new Point3d();

                    if (m.Faces[mf_id].IsQuad)
                    {
                        avgVec = Utilities.AverageVectors(new List<Vector3d>() { U[m.Faces[mf_id].A], U[m.Faces[mf_id].B], U[m.Faces[mf_id].C], U[m.Faces[mf_id].D] });
                        center = Utilities.AveragePoints(new List<Point3d>() { pts[0], pts[1], pts[2], pts[3] });
                    }
                    else
                    {
                        avgVec = Utilities.AverageVectors(new List<Vector3d>() { U[m.Faces[mf_id].A], U[m.Faces[mf_id].B], U[m.Faces[mf_id].C] });
                        center = Utilities.AveragePoints(new List<Point3d>() { pts[0], pts[1], pts[2] });
                    }
                    string error = "";

                    var angleBetween = Vector3d.VectorAngle(avgVec, m.FaceNormals[mf_id]);

                    meshFaceCenters[mf_id] = center;

                    flowRates[mf_id] = Utilities.MeshFaceArea(mf_id, m) * Math.Cos(angleBetween) * avgVec.Length;
                    florDirs[mf_id] = avgVec;
                }

                DA.SetDataList(0, flowRates);
                DA.SetDataList(1, meshFaceCenters);
                DA.SetDataList(2, florDirs);
            }
            catch
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong.");
            }
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