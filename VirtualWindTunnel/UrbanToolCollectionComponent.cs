using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class STLExporter : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public STLExporter()
          : base("STLExporter", "STLExporter",
              "STLExporter",
              "Export", "VirtualWindTunnel")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("b", "b", "", GH_ParamAccess.item);
            pManager.AddTextParameter("File", "File", "", GH_ParamAccess.item);

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
            string filepath = "";
            Brep geo = null;

            DA.GetData(0, ref geo);
            DA.GetData(1, ref filepath);



            MeshingParameters mp = new MeshingParameters();

            MeshingParameters mps = MeshingParameters.Smooth;


            Mesh[] meshArray = Mesh.CreateFromBrep(geo, mp) ;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("solid xxxxx");

            foreach (Mesh m in meshArray) {

                m.FaceNormals.ComputeFaceNormals();

                for (int i = 0; i < m.Faces.Count; i++) {


                    if (m.Faces[i].IsQuad)
                    {
                        var pt1 = m.Vertices[m.Faces[i].A];
                        var pt2 = m.Vertices[m.Faces[i].B];
                        var pt3 = m.Vertices[m.Faces[i].C];
                        var pt4 = m.Vertices[m.Faces[i].D];

                        sb.AppendLine("facet normal " + m.FaceNormals[i].X + " " + m.FaceNormals[i].Y + " " + m.FaceNormals[i].Z);
                        sb.AppendLine("outer loop");
                        sb.AppendLine("vertex " + pt1.X + " " + pt1.Y + " " + pt1.Z);
                        sb.AppendLine("vertex " + pt2.X + " " + pt2.Y + " " + pt2.Z);
                        sb.AppendLine("vertex " + pt3.X + " " + pt3.Y + " " + pt3.Z);
                        sb.AppendLine("vertex " + pt4.X + " " + pt4.Y + " " + pt4.Z);
                        sb.AppendLine("endloop");
                        sb.AppendLine("endfacet");

                    }

                    else {
                        var pt1 = m.Vertices[m.Faces[i].A];
                        var pt2 = m.Vertices[m.Faces[i].B];
                        var pt3 = m.Vertices[m.Faces[i].C];
                 

                        sb.AppendLine("facet normal " + m.FaceNormals[i].X + " " + m.FaceNormals[i].Y + " " + m.FaceNormals[i].Z);
                        sb.AppendLine("outer loop");
                        sb.AppendLine("vertex " + pt1.X + " " + pt1.Y + " " + pt1.Z);
                        sb.AppendLine("vertex " + pt2.X + " " + pt2.Y + " " + pt2.Z);
                        sb.AppendLine("vertex " + pt3.X + " " + pt3.Y + " " + pt3.Z);
               
                        sb.AppendLine("endloop");
                        sb.AppendLine("endfacet");



                    }

                }



            }



            System.IO.File.WriteAllText(filepath, sb.ToString()); 



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
            get { return new Guid("{0AD4BDF7-33AC-492D-ABF0-622A5488C8E2}"); }
        }
    }
}
