using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ReadComfortHours : GH_Component
    {
        // exposure
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public ReadComfortHours()
          : base("ReadComfortHours", "ReadComfortHours", "Read annual accumulated comfort hours in % from UTCI.", "Eddy", "6 | Outdoor Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddTextParameter("Interval", "Int", "Interval to be avaluated. May either be a single hour (mode 1) or a Ladybug analysisPeriod (mode 2).", GH_ParamAccess.list);

            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ComfortHours", "CH", "Annual comfort hours in %.", GH_ParamAccess.list);
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
            OFResult RES = null;
            DA.GetData(0, ref RES);

            bool Run = false;

            //int annualHours = 8760;

            List<string> dateTimeInput = new List<string>();

            // Get interval

            DA.GetDataList(1, dateTimeInput);

            var ladybugAnalysisPeriod = dateTimeInput;

            DA.GetData(2, ref Run);

            if (!Run)
            {
                return;
            }

            //// Fill datatrees from CSV

            var path = RES.WorkingDirectory + @"\UTCI.csv";
            List<double> ComfortHoursList = new List<double>();

            if (ladybugAnalysisPeriod == null || ladybugAnalysisPeriod.Count != 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid analysis periode object."); return;
            }

            var allLines = File.ReadAllLines(RES.WorkingDirectory + @"\UTCI.csv");
            var numberOfProbes = allLines.Count();

            // Fill array once; fastest method so far

            double[,] HourlyUTCI = new double[numberOfProbes, 8760];
            //double[,] HourlyHumanConditions = new double[numberOfProbes, 8760];
            double[] ComfortHours = new double[numberOfProbes];

            System.Threading.Tasks.Parallel.For(0, numberOfProbes,

            i =>
              {
                  if (GH_Document.IsEscapeKeyDown())
                  {
                      GH_Document GHDocument = OnPingDocument();
                      GHDocument.RequestAbortSolution();
                  }

                  for (int h = 0; h < 8760; h++)
                  {
                      HourlyUTCI[i, h] = double.Parse(allLines[i].Split(',')[h]);
                  }
              });

            var hoursToEvaluate = Utilities.GetEvalHoursFromLB(ladybugAnalysisPeriod);

            //System.Threading.Tasks.Parallel.For(0, numberOfProbes,
            //  i =>
            //  {
            for (int probes = 0; probes < numberOfProbes; probes++)
            {
                int comfortCnt = 0;
                foreach (int hour in hoursToEvaluate)
                {
                    // Todo: this should be [hour, probe]), identical to everywhere else

                    if (UTCI.CalcConditionOfPerson(HourlyUTCI[probes, hour]) == 0)
                    {
                        //HourlyHumanConditions[i,hour]=UTCI.GetConditionOfPerson(HourlyUTCI[i, hour]);
                        comfortCnt++;
                    }
                }
                ComfortHours[probes] = Math.Round((double)comfortCnt * 100 / hoursToEvaluate.Count, 1);
                //});
            }

            DA.SetDataList(0, ComfortHours);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_readUTCI;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{91BD9BAF-A114-4062-AA82-30E9C82CE36E}");
    }
}