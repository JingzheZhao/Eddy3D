using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
  
    public class CompCalcWindFactors : GH_Component
    {


        // exposure
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}


        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompCalcWindFactors()
          : base("Calculate WindFactors", "CalcWindFactors", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Res", "Res", "Res", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            pManager.AddPointParameter("Probes", "Probes", "Probes", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("WF", "WF", "WF", GH_ParamAccess.item);
            // pManager.AddGenericParameter("MRT_T", "MRT_T", "MRT_T", GH_ParamAccess.tree);
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

            //// Hour of the year
            //List<int> hours = new List<int>() { 0 };
            //DA.GetDataList(1, hours);

            List<Point3d> probes = new List<Point3d>();
            DA.GetDataList("Probes", probes);
            var numberOfProbes = probes.Count;
            var probesArr = probes.ToArray();


            bool run = false;
            DA.GetData("Run", ref run);



            #region Load prerequisites


            Console.WriteLine("Load weather data...");

            //Weather data...

            if (RES.Domain.BCond.epwFilePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Without a weather file (.epw) connected you will not be able to perform outdoor comfort calculations.");
                return;
            }

            Weather weather = new Weather();
            weather.LoadWeatherData(RES.Domain.BCond.epwFilePath);


            double[][] DiffRad = null;
            double[][] DirRad = null;

            var difillFile = RES.WorkingDirectory + @"\Rad\CallRay.dif.ill";
            var dirillFile = RES.WorkingDirectory + @"\Rad\CallRay.dir.ill";

            if (File.Exists(difillFile) && File.Exists(dirillFile))
            {
                //  Load radiation datasets
                //  [x][]  time
                //  [][x]  points

                DiffRad = RadianceFiles.loadILL(difillFile);
                DirRad = RadianceFiles.loadILL(dirillFile);
                int sensorPointCountExisting = DiffRad[0].Length;

                if (sensorPointCountExisting != numberOfProbes && run)
                {

                    Daysim.Epw2Wea(weather.epwFilePath, RES.WorkingDirectory + @"\Rad");

                    DaysimSettings set = new DaysimSettings
                    {
                        AB = 1,
                        WorkDir = RES.WorkingDirectory + @"\Rad"
                    };
                    Daysim.RunDaysim(set);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated Daysim results did not have the correct number of probing points. Results have been recalculated.");

                }
                else if(sensorPointCountExisting == numberOfProbes && !run)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated Daysim results have been loaded.");

                }

            }
            else if (run)
            {
                               
                    Daysim.Epw2Wea(weather.epwFilePath, RES.WorkingDirectory + @"\Rad");

                    DaysimSettings set = new DaysimSettings
                    {
                        AB = 1,
                        WorkDir = RES.WorkingDirectory + @"\Rad"
                    };
                    Daysim.RunDaysim(set);

                    DiffRad = RadianceFiles.loadILL(difillFile);
                    DirRad = RadianceFiles.loadILL(dirillFile);

                
            }
            else {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No precalculated Daysim results found. Please calculate.");
            }



            #endregion



            var Matrix = new double[8760, numberOfProbes];

            var csvMRT = RES.WorkingDirectory + @"WindFactors.csv";

            MRT mrt = null;


            if (File.Exists(csvMRT) && !run)
            {
                Matrix = RadianceFiles.readCSVFile(csvMRT);

                var numberOfProbesCSV = Matrix.GetUpperBound(1) + 1;
                if (numberOfProbesCSV == numberOfProbes)
                {


                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated MRT results have been loaded.");

                    mrt = new MRT(weather, MRT.MRTType.kessling, DiffRad, DirRad, probesArr, false);
                    mrt.Values = Matrix;

                    DA.SetData(0, mrt);

                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated MRT array has the wrong number of probing points. Please recalculate.");

                }

            }


            // Array casting
            //var MRT = Matrix.Cast<double[]>().ToArray();
            //double[][] MRT2 = ((object[][])Matrix).Select(x => x.Select(y => Convert.ToDouble(y)).ToArray()).ToArray();





            if (run)
            {


                //  [x][]  time
                //  [][x]  points


                mrt = new MRT(weather, MRT.MRTType.kessling, DiffRad, DirRad, probesArr, true);


                if (GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }




                Utilities._2DArray2CSV(mrt.Values, csvMRT, true, 1);



                DA.SetData(0, mrt);
            }




        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_calMRT;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{F165ADD0-A047-409E-A008-DD9CF82605F3}");
    }
}



