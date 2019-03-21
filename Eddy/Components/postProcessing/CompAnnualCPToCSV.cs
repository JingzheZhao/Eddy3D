using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ParseAnnualCP : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ParseAnnualCP()
          : base("AnnualCPToCSV", "AnnualCPToCSV", "AnnualCPToCSV", "Eddy", "5 | PostProcessing")
        {
        }

        // exposure
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            //pManager.AddPointParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("out", "out", "out", GH_ParamAccess.list);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            OFResult RES = null;
            DA.GetData(0, ref RES);

            bool run = false;
            DA.GetData(1, ref run);

            List<Point3d> points = new List<Point3d>();
            //DA.GetDataList(1, points);


            DataTree<double> cpTree = new DataTree<double>();

            List<string> fullProbeFilePath = new List<String>();


            //Build paths as list


            if (run == true)
            {



                for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                {
                    fullProbeFilePath.Add(RES.WorkingDirectory + "\\" + RES.Domain.BCond.windDirs[i] + @"\postProcessing\cp_Probes.csv");
                }

                var numberOfWindDirs = RES.Domain.BCond.windDirs.Count();
                var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count();



                // Array for output data

                var listOfAnnualData = new double[numberOfWindDirs][];

                for (int r = 0; r < numberOfWindDirs; r++)
                {
                    listOfAnnualData[r] = new double[numberOfProbes];
                    for (int c = 0; c < numberOfProbes; c++)
                    {
                        listOfAnnualData[r][c] = double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c]);
                    }
                }



                // Write Array to dataTree





                //for (int c = 0; c < numberOfWindDirs; c++)
                //{
                //    for (int r = 0; r < numberOfProbes; r++)
                //    {
                //        cpTree.Add(listOfAnnualData[c][r], new Grasshopper.Kernel.Data.GH_Path(c));
                //    }

                //}


                //DA.SetDataTree(0, cpTree);


                //Write Array to file
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
                {
                    sb.Append(RES.Domain.BCond.windDirs[i] + ",");

                }
                sb.AppendLine("");
                for (int r = 0; r < numberOfProbes; r++)
                {
                    for (int c = 0; c < numberOfWindDirs; c++)
                    {
                        //sb.Append(points[r].X + ","+ points[r].Y + ","+points[r].Z + ",");
                        sb.Append(listOfAnnualData[c][r] + ",");

                    }
                    sb.AppendLine("");
                }
                File.WriteAllText(RES.WorkingDirectory + @"\annualCPData.csv", sb.ToString());


            }



        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_annual;//return null;

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{ED246ABD-F3E7-4AEC-A24C-CD6ADA25A389}");
    }
}
