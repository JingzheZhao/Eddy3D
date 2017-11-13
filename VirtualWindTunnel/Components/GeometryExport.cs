using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class GeometryExportComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public GeometryExportComponent()
          : base("STLExporter", "STLExporter",
              "STLExporter",
              "CFDTool", "Meshing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Breps", "B", "Add Breps", GH_ParamAccess.list);
            pManager.AddTextParameter("File", "F", "Provide a file path", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mode", "Mode", "Output mode: Binary = 0, ASCI = 1", GH_ParamAccess.item, 0);


            Param_Integer param = pManager[2] as Param_Integer;

            param.AddNamedValue("Binary", 0);
            param.AddNamedValue("ASCI", 1);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
         //   pManager.AddGenericParameter("DateTime", "Dt", "System Date Time Object", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filePath = "";
            int MODE = 0;
            List<Brep> geo = new List<Brep>();

            DA.GetDataList(0,  geo);
            DA.GetData(1, ref filePath);
            DA.GetData(2, ref MODE);

            
            

            MeshingParameters mp = new MeshingParameters();
            MeshingParameters mps = MeshingParameters.Smooth;

            List<Mesh> meshObjects = new List<Mesh>();
            
            foreach (Brep b in geo)
            {
                meshObjects.AddRange(Mesh.CreateFromBrep( b , mp) );

            }


            if (MODE == 0) {
                STLExport.ExportBinary(filePath, meshObjects);
            }
            else {
                STLExport.ExportASCI(filePath, meshObjects);
            }




        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{D63D8BF0-E745-4452-A250-23F02666EA70}"); }
        }
    }
}
