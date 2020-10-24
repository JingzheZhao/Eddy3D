using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

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
              @"Create a tree in the domain that acts as a momentum sink. Its recommended to pass the tree geometries as single geometries as Eddy needs to compute the dimension of every tree for a proper setup.

Pass either a type (coarse, medium, dense) or a LAI to set up a tree.

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
            pManager.AddGeometryParameter("Geo", "Geo", "Tree geometry", GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "Type", @"Tree type.

Either pass tree type as ""coarse"", ""medium"", or ""dense"",  or pass a multiline string that references the ""A"" and ""B"" coefficients from a pressure drop polynomial fit for dp = A*u + B*u^2 for the Darcy-Forchheimer Model. E.g.

such as:

1.7, 1.7, 1.7
4.5, 4.5, 4.5

meaning:

A = (1.7, 1.7, 1.7)
B = (4.5, 4.5, 4.5)

Eddy will transform those into the appropriate Darcy and Forchheimer coefficients along with the dimensions of the tree, e.g:

D = (93922, 93922, 93922)
F = (7.5, 7.5, 7.5)

for a dense tree with a 1 m diameter.

", GH_ParamAccess.list);

            pManager.AddNumberParameter("LAI", "LAI", "Leaf area index", GH_ParamAccess.item);

            //Param_Integer param = pManager[1] as Param_Integer;

            ////Using an enum to generate the dropdown items
            //var types = Enum.GetNames(typeof(EddyLib.TreeType));
            //for (int i = 0; i < types.Length; i++)
            //{
            //    param.AddNamedValue(types[i], i);
            //}

            pManager[1].Optional = true;
            pManager[2].Optional = true;
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

            GeometryBase geo = null;

            double LAI = 0;

            if (!DA.GetData("Geo", ref geo)) { };
            DA.GetDataList("Type", type);
            DA.GetData("LAI", ref LAI);

            if (LAI != 0 && type.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Please pass either type or LAI, not both.");
                return;
            }

            if (type.Count > 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Please pass a multiline string in the correct format.");
                return;
            }

            if (type.Count != 0 && LAI == 0)
            {
                var PorosityCoeffs_A = new double[3];
                var PorosityCoeffs_B = new double[3];

                // Part 1: try to convert the string to an enum.
                //TreeType treetype = (TreeType)Enum.Parse(typeof(TreeType), type[0]);

                if (type[0] == "" && type[1] == "")
                {
                    PorosityCoeffs_A = new double[] { 1.7, 1.7, 1.7 };
                    PorosityCoeffs_B = new double[] { 4.5, 4.5, 4.5 };
                }

                // Part 2: see if the conversion succeeded.
                else if (type[0] == "coarse")
                {
                    PorosityCoeffs_A = new double[] { 0.09, 0.09, 0.09 };
                    PorosityCoeffs_B = new double[] { 0.3, 0.3, 0.3 };
                }
                else if (type[0] == "medium")
                {
                    PorosityCoeffs_A = new double[] { 0.2, 0.2, 0.2 };
                    PorosityCoeffs_B = new double[] { 1.7, 1.7, 1.7 };
                }
                else if (type[0] == "dense")
                {
                    PorosityCoeffs_A = new double[] { 1.7, 1.7, 1.7 };
                    PorosityCoeffs_B = new double[] { 4.5, 4.5, 4.5 };
                }

                //      if (type[0] == "" && type[1] == "")
                //{
                //    PorosityCoeffs_D = new double[] { 93922, 93922, 93922 };
                //    PorosityCoeffs_F = new double[] { 7.5, 7.5, 7.5 };
                //}

                //// Part 2: see if the conversion succeeded.
                //else if (type[0] == "coarse")
                //{
                //    PorosityCoeffs_D = new double[] { 4972, 4972, 4972 };
                //    PorosityCoeffs_F = new double[] { 0.5, 0.5, 0.5 };
                //}
                //else if (type[0] == "medium")
                //{
                //    PorosityCoeffs_D = new double[] { 11049, 11049, 11049 };
                //    PorosityCoeffs_F = new double[] { 2.8, 2.8, 2.8 };
                //}
                //else if (type[0] == "dense")
                //{
                //    PorosityCoeffs_D = new double[] { 93922, 93922, 93922 };
                //    PorosityCoeffs_F = new double[] { 7.5, 7.5, 7.5 };
                //}
                else if (type[0] != "" && type[1] != "")
                {
                    string[] f_String = type[0].Split(',').ToArray();
                    PorosityCoeffs_B = Array.ConvertAll<string, double>(f_String, Double.Parse);

                    string[] d_String = type[1].Split(',').ToArray();
                    PorosityCoeffs_A = Array.ConvertAll<string, double>(d_String, Double.Parse);
                }
                else
                {
                    return;
                }
                var tree = new EddyLib.Tree(geo, PorosityCoeffs_B, PorosityCoeffs_A);

                DA.SetData(0, tree);
            }
            else if (LAI != 0 && type.Count == 0)
            {
                var tree = new EddyLib.Tree(geo, LAI);

                DA.SetData(0, tree);
            }
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