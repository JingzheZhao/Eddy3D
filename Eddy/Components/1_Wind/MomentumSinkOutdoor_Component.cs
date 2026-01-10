using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eddy.Components.Indoor
{

    public class MomentumSinkOutdoor_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public MomentumSinkOutdoor_Component()
          : base("Momentum Sink Outdoor", "MSink", "Momentum Sink Outdoor" + EddyVersion.toString(), EddyVersion.Name, "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            pManager.AddTextParameter("Darcy-Forchheimer Coefficients", "Type", @"Darcy-Forchheimer Coefficients.

Pass a multiline string that references the ""A"" and ""B"" coefficients from a pressure drop polynomial fit for dp = A*u + B*u^2 for the Darcy-Forchheimer Model.", GH_ParamAccess.list);

            pManager.AddTextParameter("Name", "N", "Name", GH_ParamAccess.item, "");

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Function Object", "FO", "Momentum Sink Function Object", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> type = new List<string>();

            GeometryBase geo = null;
            if (!DA.GetData("Geo", ref geo)) { };

            string Name = "";
            DA.GetData(2, ref Name);

            DA.GetDataList("Darcy-Forchheimer Coefficients", type);

            if (type.Count != 0)
            {
                var PorosityCoeffs_A = new double[3];
                var PorosityCoeffs_B = new double[3];

                // Part 1: try to convert the string to an enum.
                //TreeType treetype = (TreeType)Enum.Parse(typeof(TreeType), type[0]);

                if (type[0] == "" && type[1] == "")
                {
                    PorosityCoeffs_A = new double[] { 1e9, 1e9, 1e9 };
                    PorosityCoeffs_B = new double[] { 1e9, 1e9, 1e9 };
                }
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
                var furniture = new EddyLib.Indoor.MomentumSink.Tree(geo, PorosityCoeffs_B, PorosityCoeffs_A, Name);

                DA.SetData(0, furniture);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_Indoor_Emitter;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0163E2D4-01F4-4535-8255-461483A28ACE"); }
        }
    }
}