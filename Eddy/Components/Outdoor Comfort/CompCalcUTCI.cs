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
    public class CompCalcUTCI : GH_Component
    {




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompCalcUTCI()
          : base("CalcUTCI", "CalcUTCI", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Hour", "Hour", "Hour", GH_ParamAccess.item);
            pManager.AddVectorParameter("U", "U", "U", GH_ParamAccess.list);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("MRT", "MRT", "MRT", GH_ParamAccess.tree);
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



            // Hour of the year
            int hour = 0;
            DA.GetData(1, ref hour);


            List<Vector3d> velocityProbes = new List<Vector3d>();
            DA.GetDataList(2, velocityProbes);




            Console.WriteLine("Load weather data...");

            //Weather data...

            Weather weather = new Weather();
            weather.LoadWeatherData(RES.Domain.BCond.weather);


            //  Load radiation datasets
            //  [x][]  time
            //  [][x]  points

            Console.WriteLine("Loading: Radiation data...");

            var DiffRad = RadianceFiles.loadILL(RES.WorkingDirectory+ @"\Rad\CallRay.dif.ill");
            var DirRad = RadianceFiles.loadILL(RES.WorkingDirectory + @"\Rad\CallRay.dir.ill");
                        

            int sensorPointCount = DiffRad[0].Length;
            double[,] Utci = new double[1, sensorPointCount];
            //double[,] conditionOfPerson = new double[8760, sensorPointCount];
            

            Console.WriteLine("Loading: Wind data");
            
            //var windDirList = new List<double> { 0, 45, 90, 135, 180, 225, 270, 315 };
            //var windDirList = new List<double>();// { 0, 45, 90, 135, 180, 225, 270, 315 };
            //List<int> windDirList = options.windDirs;


            var windDirList = RES.Domain.BCond.windDirs;   
            var numberOfWindDirs = windDirList.Count;


            // load Reduction data
            // -----------------



            
            // Parse ABL data from simulation directory                    

            double URef = RES.Domain.BCond.URef;
            double zref = RES.Domain.BCond.zref;
            double z0 = RES.Domain.BCond.z0;

          

            Console.WriteLine("Starting UTCI calc...");

            // UTCI here


            var UtciList = new List<double>();
            var MRTList = new List<double>();

          
            for (int p = 0; p< velocityProbes.Count; p++)
            {
                var mrt = UTCI.GetMRT(weather.DryBulbTemp[hour], weather.RelativeHumidity[hour], DiffRad[hour][p], DirRad[hour][p], weather.SolarElevation[hour], weather.DryBulbTemp[hour], weather.Wst, weather.Hst, weather.BodyA, weather.GrRef, 0.95)[0];
                var utci = UTCI.GetUTCI2(weather.DryBulbTemp[hour], weather.RelativeHumidity[hour], velocityProbes[p].Length, mrt);

                if (GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }


                MRTList.Add(mrt);
                UtciList.Add(utci);
            }

            DA.SetDataList(0, UtciList);
            DA.SetDataList(1, MRTList);


        }





        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_parseU;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{FB51794A-B392-45D4-A5C2-3924735CFBB6}");
    }
}



