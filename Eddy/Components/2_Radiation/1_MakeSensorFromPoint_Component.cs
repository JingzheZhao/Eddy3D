using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Radiation
{
    public class MakeRadiationSensorFromPoint_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MakeRadiationSensor_Component class.
        /// </summary>
        public MakeRadiationSensorFromPoint_Component()
          : base("Sensor", "Sen", 
@"Single Point Sensor

Creates a single sensor point for detailed MRT analysis at a specific location, such as a specific seat or standing spot.

" + EddyVersion.toString(), EddyVersion.Name, "2 | Radiation")
        {
            Analytics.Analytics.TrackComponentView("MRTPointSensor");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "Pt", "Sensor node", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddVectorParameter("Normal", "V", "Sensor normal", GH_ParamAccess.item, Vector3d.ZAxis);
            pManager.AddMeshParameter("Mesh", "M", "Mesh geometry for previews", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Sensor", "Sen", "Radiation Simulation Sensor", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d pt = Point3d.Origin;
            Vector3d vec = Vector3d.ZAxis;
            Mesh m = null;

            DA.GetData(0, ref pt);
            DA.GetData(1, ref vec);
            DA.GetData(2, ref m);

            EddyProbe rp = new EddyProbe(pt, vec);

            if (m != null) rp = new EddyProbe(pt, vec, m);

            DA.SetData(0, rp);
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
                return Resources.Eddy_Sensor;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0d5c243d-68d8-4df7-b458-daf9b5b98cfe"); }
        }
    }
}
