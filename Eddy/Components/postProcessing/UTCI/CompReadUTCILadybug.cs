using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using System.Linq;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;
using System.Text.RegularExpressions;
using Grasshopper;
using EddyLib;
using Eddy.Properties;
using System.Threading.Tasks;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class UTCIReaderLadybug : GH_Component
    {




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public UTCIReaderLadybug()
          : base("UTCIReaderLB", "UTCIReaderLB", "UTCIReaderLB", "Eddy", "UTCI")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);

            pManager.AddTextParameter("analysisPeriod", "analysisPeriod", "analysisPeriod", GH_ParamAccess.list);

            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item, false);




        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.tree);
            pManager.AddGenericParameter("U", "U", "Overall Uncertainty in %", GH_ParamAccess.item);
            //pManager.AddGenericParameter("windSpeed", "windSpeed", "windSpeed", GH_ParamAccess.list);
            //pManager.AddGenericParameter("windDir", "windDir", "windDir", GH_ParamAccess.list);
            //pManager.AddGenericParameter("windRed", "windRed", "windRed", GH_ParamAccess.list);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            OFBaseDomain DOM = null;



            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }


            bool Run = false;



            List<string> ladybugAnalysisPeriod = new List<string>();

            DA.GetDataList(1, ladybugAnalysisPeriod);
            DA.GetData(2, ref Run);


            if (ladybugAnalysisPeriod == null)
            { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid analysis periode object."); return; }


           
            


            var allLines = File.ReadAllLines(DOM.baseWorkingDirectory + @"\UTCI.csv");
            var numberOfLines = allLines.Count();


            // -1 because of python

            int month_start = int.Parse(ladybugAnalysisPeriod[0].Split(',')[0].Split('(')[1]) - 1;
            var month_end = int.Parse(ladybugAnalysisPeriod[1].Split(',')[0].Split('(')[1]);
            var day_start = int.Parse(ladybugAnalysisPeriod[0].Split(',')[1]) - 1;
            var day_end = int.Parse(ladybugAnalysisPeriod[1].Split(',')[1]);
            var hour_start = int.Parse(ladybugAnalysisPeriod[0].Split(',')[2].Split(')')[0]) - 1;
            var hour_end = int.Parse(ladybugAnalysisPeriod[1].Split(',')[2].Split(')')[0]);


            var hours = hour_end - hour_start;

            double[,] data = new double[numberOfLines, hours];


            if (Run)
            {


                System.Threading.Tasks.Parallel.For(0, numberOfLines,
                  i =>
                  {

                      for (int h = 0; h < hours; h++)
                      {
                          data[i, h] = double.Parse(allLines[i].Split(',')[h]);
                      }


                  });




                //double[] valueHour = new double[numberOfLines];
                var valueHour = new DataTree<double>();



                for (int m = month_start; m < month_end; m++)
                {
                    for (int d = day_start; d < day_end; d++)
                    {
                        for (int h = hour_start; h < hour_end; h++)
                        {

                            // Check if already gone through month
                            if (m == 2 && d > 29) { continue; }

                            else if (m == 2 && d > 28) { continue; }

                            else if ((m == 4 || m == 6 || m == 9 || m == 10) && d > 30) { continue; }
                            //

                            for (int i = 0; i < numberOfLines; i++)
                            {
                                // TODO: move this out of loop later
                                valueHour.Add(data[i, h], new Grasshopper.Kernel.Data.GH_Path(h));
                            }

                        }
                    }
                }

                // Parse UTCI uncertaintly from file

                var uncertaintyLine = File.ReadLines(DOM.baseWorkingDirectory + @"\UTCI.uncertainty").Last();
                var uncertaintyNUM = double.Parse(uncertaintyLine.Split('%')[0].Split(':')[1].Split('r')[1]);




                DA.SetDataTree(0, valueHour);
                DA.SetData(1, uncertaintyNUM);
            }


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
                return Resources.Eddy_parseU;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{D1F7773E-3B8E-407E-A8DF-BB4B106F28BA}"); }
        }
    }
}



