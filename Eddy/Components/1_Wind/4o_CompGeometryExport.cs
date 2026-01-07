using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class STLExporter_Component : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

        /// <summary>
        /// Initializes a new instance of the STLExporter_Component class.
        /// </summary>
        public STLExporter_Component()
          : base(
              "STL Exporter", 
              "STLExport",
              @"Export geometry to STL format for OpenFOAM or other CFD tools.

Supports meshes and Breps (auto-meshed). Select between binary or ASCII 
output and single or multiple file export.

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
            pManager.AddGeometryParameter(
                "Geometry", "Geo", 
                "Meshes or Breps to export.", 
                GH_ParamAccess.list);

            pManager.AddTextParameter(
                "File Path", "File", 
                "Destination file path (.stl).", 
                GH_ParamAccess.item);

            pManager.AddIntegerParameter(
                "Mode", "Mode", 
                "Export mode: 0=Binary, 1=ASCII, 2=Binary (Multi-file), 3=ASCII (Multi-file)", 
                GH_ParamAccess.item, 0);

            if (pManager[2] is Param_Integer param)
            {
                param.AddNamedValue("Binary", 0);
                param.AddNamedValue("ASCII", 1);
                param.AddNamedValue("Binary List", 2);
                param.AddNamedValue("ASCII List", 3);
            }

            pManager.AddNumberParameter(
                "Edge Length", "Edge", 
                "Optional: Maximum edge length for auto-meshing Breps. Units: meters.", 
                GH_ParamAccess.item, 0);
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
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
            var geo = new List<GeometryBase>();
            if (!DA.GetDataList("Geometry", geo)) return;

            string filePath = "";
            if (!DA.GetData("File Path", ref filePath) || string.IsNullOrEmpty(filePath)) return;

            int mode = 0;
            DA.GetData("Mode", ref mode);

            double edgeLength = 0;
            DA.GetData("Edge Length", ref edgeLength);

            var dir = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var mp = new MeshingParameters();
            if (edgeLength != 0)
            {
                mp.MaximumEdgeLength = edgeLength;
                mp.MinimumEdgeLength = edgeLength;
            }

            var allTogether = new Mesh();
            var allSeparate = new List<Mesh>();

            foreach (GeometryBase b in geo)
            {
                if (b == null) continue;

                if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                {
                    Mesh obj = (Mesh)b;
                    allTogether.Append(obj);
                    allSeparate.Add(obj);
                }
                else if (b is Brep brep)
                {
                    var meshes = Mesh.CreateFromBrep(brep, mp);
                    if (meshes != null)
                    {
                        var meshForList = new Mesh();
                        foreach (Mesh m in meshes)
                        {
                            allTogether.Append(m);
                            meshForList.Append(m);
                        }
                        allSeparate.Add(meshForList);
                    }
                }
            }

            if (mode == 0) STLExport.ExportBinary(filePath, allTogether);
            else if (mode == 1) STLExport.ExportASCI(filePath, allTogether);
            else if (mode == 2 || mode == 3)
            {
                for (int i = 0; i < allSeparate.Count; i++)
                {
                    string path = Path.Combine(dir, $"{fileName}_{i}.stl");
                    if (mode == 2) STLExport.ExportBinary(path, allSeparate[i]);
                    else STLExport.ExportASCI(path, allSeparate[i]);
                }
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.Eddy_modelSTLexport;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{D63D8BF0-E745-4452-A250-23F02666EA70}"); }
        }
    }
}