using System;
using System.Collections.Generic;
using System.Drawing;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Eddy.Components.Indoor
{
    public class Inlet : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Inlet class.
        /// </summary>
        public Inlet()
          : base("Inlet", "Il",
              "Inlet" + EddyVersion.toString(),
              EddyVersion.Name, "7 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vel", "V", "Velocity", GH_ParamAccess.item);
            pManager.AddNumberParameter("Temp", "T", "Temperature [C]", GH_ParamAccess.item);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Inlet", "In", "Inlet", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh m = null;
            DA.GetData(0, ref m);
            Vector3d vec = Vector3d.ZAxis;
            DA.GetData(1, ref vec);
            double T = 1;
            DA.GetData(2, ref T);


            var inlet = new IndoorBCs.Inlet(m, T + 273.15, vec);


            dir.Clear();
            dir.Add(vec* 10);
            face.Clear();
            face.Add(m);
            edges.Clear();
            edges.AddRange(m.GetNakedEdges());


            DA.SetData(0 ,  inlet);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_Indoor_Inlet;

            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("cb1b044e-b0bf-4fb3-b844-9f9469931244"); }
        }



        #region Preview Override

        List<Vector3d> dir = new List<Vector3d>();
        List<Mesh> face = new List<Mesh>();
        List<Polyline> edges = new List<Polyline>();

        // draw all meshes in this method
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (Hidden || Locked) return;

            //if (true) DrawArrow(args, dir, face, Color.Green);
            //if (true) DrawWireThick(args, edges, Color.Green);
            //if (true) DrawMesh(args, face, Color.LightBlue);
           

        }

        private void DrawArrow(IGH_PreviewArgs args, List<Vector3d> vecs, List<Mesh> prevMesh, Color col)
        {

            for (int i = 0; i < prevMesh.Count; i++)
            {
               var pt = AreaMassProperties.Compute(prevMesh).Centroid;
                args.Display.DrawArrow(new Line(pt, vecs[i]), col);
            }
        }

            private void DrawMesh(IGH_PreviewArgs args, List<Mesh> prevMesh, Color front, double trans = 0.1)
        {

            var frontcolor_selected = Color.FromArgb(25, 225, 25);
            var backcolor_selected = Color.FromArgb(6, 56, 6);
            if (prevMesh != null)
            {
                for (int i = 0; i < prevMesh.Count; i++)
                {
                    // create two sided material
                    var material = new Rhino.Display.DisplayMaterial();
                    material.IsTwoSided = true;

                    // set color depending on selection state
                    if (Attributes.Selected)
                    {
                        material.Diffuse = frontcolor_selected;
                        material.BackDiffuse = Color.Black;
                        material.Transparency = 0.5;
                        material.Shine = 0.25;
                    }
                    else
                    {
                        material.IsTwoSided = true;
                        material.Emission = front;
                        material.BackEmission =   Color.Black;

                        // set diffuse channel to black to avoid shading
                        material.Diffuse = Color.Black;
                        material.BackDiffuse = Color.Black;

                        material.Transparency = trans;
                        material.Shine = 0.0;
                    }

                    // draw preview
                    args.Display.DrawMeshShaded(prevMesh[i], material);
                }
            }

        }

        // draw all wires and points in this method
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || Locked) return;

            // set colors depending on selection state
            var wirecolor = (Attributes.Selected) ? Color.FromArgb(12, 112, 12) : Color.FromArgb(200, 200, 200);


            if (true) DrawArrow(args, dir, face, wirecolor);
            if (true) DrawWireThick(args, edges, wirecolor);
           // if (true) DrawMesh(args, face, Color.LightBlue);

        }




        private void DrawWire(IGH_PreviewArgs args, List<Polyline> polys, Color wirecolor)
        {
            if (polys != null)
            {
                for (int i = 0; i < polys.Count; i++)
                {
                    args.Display.DrawPolyline(polys[i], wirecolor);
                }
            }
        }
        private void DrawWireThick(IGH_PreviewArgs args, List<Polyline> polys, Color wirecolor)
        {
            if (polys != null)
            {
                for (int i = 0; i < polys.Count; i++)
                {
                    args.Display.DrawPolyline(polys[i], wirecolor, 2);
                }
            }
        }

        


        #endregion


    }
}