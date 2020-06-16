using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using static EddyLib.Tree;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class TreeComp : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public TreeComp()
          : base("Tree", "Tree",
              @"Create a tree in the domain that acts as a momentum sink.

" + EddyVersion.toString(),
              EddyVersion.Name, "8 | Function Objects")
        {
        }

        private IGH_DocumentObject[] AllCanvasObjects()
        {
            var doc = OnPingDocument();
            if (doc == null)
                return new IGH_DocumentObject[0];
            return doc.Objects.ToArray();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geo", "Geo", "Tree geometry", GH_ParamAccess.list);
            pManager.AddTextParameter("Type", "Type", @"Tree type.

Either pass tree type as ""coarse"", ""medium"", or ""dense"",  or pass a multiline string that references the ""d"" and ""f"" coefficients for the Darcy-Forchheimer Model.
", GH_ParamAccess.list);

            //Param_Integer param = pManager[1] as Param_Integer;

            ////Using an enum to generate the dropdown items
            //var types = Enum.GetNames(typeof(EddyLib.TreeType));
            //for (int i = 0; i < types.Length; i++)
            //{
            //    param.AddNamedValue(types[i], i);
            //}

            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tree", "Tree", "Tree Object", GH_ParamAccess.item);
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
            List<string> type = new List<string>();

            List<GeometryBase> geo = new List<GeometryBase>();
            DA.GetDataList("Geo", geo);
            DA.GetDataList("Type", type);

            var PorosityCoeffs_D = new double[3];
            var PorosityCoeffs_F = new double[3];

            // Part 1: try to convert the string to an enum.
            //TreeType treetype = (TreeType)Enum.Parse(typeof(TreeType), type[0]);

            if (type[0] == "" && type[1] == "")
            {
                PorosityCoeffs_D = new double[] { 40, 40, 40 };
                PorosityCoeffs_F = new double[] { 40, 40, 40 };
            }

            // Part 2: see if the conversion succeeded.
            else if (type[0] == "coarse")
            {
                PorosityCoeffs_D = new double[] { 20, 20, 20 };
                PorosityCoeffs_F = new double[] { 20, 20, 20 };
            }
            else if (type[0] == "medium")
            {
                PorosityCoeffs_D = new double[] { 40, 40, 40 };
                PorosityCoeffs_F = new double[] { 40, 40, 40 };
            }
            else if (type[0] == "dense")
            {
                PorosityCoeffs_D = new double[] { 80, 80, 80 };
                PorosityCoeffs_F = new double[] { 80, 80, 80 };
            }
            else if (type[0] != "" && type[1] != "")
            {
                string[] f_String = type[0].Split(',').ToArray();
                PorosityCoeffs_F = Array.ConvertAll<string, double>(f_String, Double.Parse);

                string[] d_String = type[1].Split(',').ToArray();
                PorosityCoeffs_D = Array.ConvertAll<string, double>(d_String, Double.Parse);
            }
            else
            {
                return;
            }

            var tree = new EddyLib.Tree(geo, PorosityCoeffs_F, PorosityCoeffs_D);

            DA.SetData(0, tree);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                    // You can add image files to your project resources and access them like this:
                    Resources.Eddy_trees;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{AED1E85B-30B7-4255-B767-EF615D66DCA1}");
    }
}