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
            pManager.AddGeometryParameter("Geometry", "G", "Breps and Meshes supported", GH_ParamAccess.list);
            pManager.AddTextParameter("File", "F", "Provide a file path", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mode", "Mode", "Output mode: Binary = 0, ASCI = 1", GH_ParamAccess.item, 0);


            Param_Integer param = pManager[2] as Param_Integer;

            param.AddNamedValue("Binary", 0);
            param.AddNamedValue("ASCI", 1);
            param.AddNamedValue("BinaryList", 2);
            param.AddNamedValue("ASCIList", 3);

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

            List<GeometryBase> geo = new List<GeometryBase>();



            DA.GetDataList(0, geo);
            DA.GetData(1, ref filePath);
            DA.GetData(2, ref MODE);

            var Dir = Path.GetDirectoryName(filePath);
            var FileName = Path.GetFileNameWithoutExtension(filePath);
            if (!Directory.Exists(Dir)) { Directory.CreateDirectory(Dir); }




            MeshingParameters mp = new MeshingParameters();

            Mesh allTogether = new Mesh();
            List<Mesh> allSeparate = new List<Mesh>();


            foreach (GeometryBase b in geo)
            {

                if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                {
                    Mesh obj = (Mesh)b;
                    allTogether.Append(obj);
                    allSeparate.Add(obj);
                }
                else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                {
                    Brep obj = (Brep)b;
                    var m = Mesh.CreateFromBrep(obj, mp);
                    foreach (Mesh mm in m) allTogether.Append(mm);

                    Mesh meshForMeshList = new Mesh();
                    foreach (Mesh mm in m) meshForMeshList.Append(mm);
                    allSeparate.Add(meshForMeshList);
                }


            }


            if (MODE == 0)
            {
                STLExport.ExportBinary(filePath, allTogether);
            }
            else if (MODE == 1)
            {
                STLExport.ExportASCI(filePath, allTogether);
            }
            else if (MODE == 2)
            {

                for (int i = 0; i < allSeparate.Count; i++)
                {
                    string filePath2 = Dir + @"\" + FileName + i + ".stl";
                    STLExport.ExportBinary(filePath2, allSeparate[i]);
                }

            }
            else if (MODE == 3)
            {
                for (int i = 0; i < allSeparate.Count; i++)
                {
                    string filePath2 = Dir + @"\" + FileName + i + ".stl";
                    STLExport.ExportASCI(filePath2, allSeparate[i]);
                }
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
