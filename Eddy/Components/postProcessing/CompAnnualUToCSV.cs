using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using EddyLib;
using Eddy.Properties;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ParseHourlyU : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ParseHourlyU()
          : base("AnnualUToCSV", "AnnualUToCSV", "AnnualUToCSV", "Eddy", "5 | PostProcessing")
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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
           // pManager.AddGenericParameter("out", "out", "out", GH_ParamAccess.list);
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


            List<Point3d> points = new List<Point3d>();
            //DA.GetDataList(1, points);


            DataTree<Vector3d> UTree = new DataTree<Vector3d>();

            List<string> fullProbeFilePath = new List<String>();


            //Build paths as list

            for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
            {
                fullProbeFilePath.Add(RES.WorkingDirectory + "\\" + RES.Domain.BCond.windDirs[i] + @"\postProcessing\U_Probes.csv");
            }

            var numberOfWindDirs = RES.Domain.BCond.windDirs.Count();
            var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count();
            //string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);


            // Array for output data

            var listOfAnnualData = new Vector3d[numberOfWindDirs][];

            for (int r = 0; r < numberOfWindDirs; r++)
            {
                listOfAnnualData[r] = new Vector3d[numberOfProbes];
                //int counter = 1;
                for (int c = 0; c < numberOfProbes; c++)
                {
                    listOfAnnualData[r][c] = new Vector3d(double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0])/RES.Domain.BCond.UPedestrianHeight, double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]) / RES.Domain.BCond.UPedestrianHeight, double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[2]) / RES.Domain.BCond.UPedestrianHeight);
                    //counter += 3;
                }
            }



            


            // Write Array to dataTree



            //for (int c = 0; c < numberOfWindDirs; c++)
            //{
            //    for (int r = 0; r < numberOfProbes; r++)
            //    {
            //        UTree.Add(listOfAnnualData[c][r], new Grasshopper.Kernel.Data.GH_Path(c));
            //    }

            //}


            //DA.SetDataTree(0, UTree);


            //Write U Array to file
            System.Text.StringBuilder UFile = new System.Text.StringBuilder();

            for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
            {
                UFile.AppendLine(RES.Domain.BCond.windDirs[i] + ", , ,");
                UFile.AppendLine("x, y, z,");
            }

            UFile.AppendLine("");

            for (int r = 0; r < numberOfProbes; r++)
            {
                for (int c = 0; c < numberOfWindDirs; c++)
                {

                    UFile.AppendLine(listOfAnnualData[c][r] + ",");

                }
                UFile.AppendLine("");
            }
            File.WriteAllText(RES.WorkingDirectory + @"\hourlyU.csv", UFile.ToString());

            //Write Reduction Array to file

            System.Text.StringBuilder ReductionFile = new System.Text.StringBuilder();

            for (int i = 0; i < RES.Domain.BCond.windDirs.Count; i++)
            {
                ReductionFile.Append(RES.Domain.BCond.windDirs[i] + ",");

            }

            ReductionFile.AppendLine("");
            for (int r = 0; r < numberOfProbes; r++)
            {

                for (int c = 0; c < numberOfWindDirs; c++)
                {

                    //ReductionFile.Append(Math.Sqrt(Math.Pow(listOfAnnualData[c][r].X,2)* Math.Pow(listOfAnnualData[c][r].Y,2)* Math.Pow(listOfAnnualData[c][r].Z,2) )+ ",");
                    ReductionFile.Append(Math.Round(listOfAnnualData[c][r].Length,3) + ",");
                }
                ReductionFile.AppendLine("");
            }
            File.WriteAllText(RES.WorkingDirectory + @"\WindReductionData.csv", ReductionFile.ToString());



        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {

                return Resources.Eddy_annual;

            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{1A676633-817E-4868-A3BD-B737724A8D8E}"); }
        }
    }
}
