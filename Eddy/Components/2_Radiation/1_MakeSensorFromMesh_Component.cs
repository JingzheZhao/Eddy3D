using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Eddy.Components.Radiation
{
    public class MakeRadiationSensorFromMesh_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MakeRadiationSensor_Component class.
        /// </summary>
        public MakeRadiationSensorFromMesh_Component()
          : base("Mesh Sensor", "MeshSen",
@"Mesh Sensor Grid

Converts a mesh surface into a grid of sensors for MRT analysis. Each mesh face serves as a measurement point for radiation and thermal comfort.

" + EddyVersion.toString(),
              EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Grid Mesh", "Mesh", "Analysis grid mesh. Each face center becomes a sensor point.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Sensors", "Sen", "Sensor points for MRT Simulation component", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh m = null;

            DA.GetData(0, ref m);

            List<EddyProbe> probes = new List<EddyProbe>();

            if (m != null)
            {
                probes.AddRange(EddyProbe.Mesh2Probes(m));
            }

            DA.SetDataList(0, probes);
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
                return Resources.Eddy_SensorFromMesh;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{998A5593-8CD0-4E97-A466-EC71AE6C4FAE}"); }
        }
    }
}
