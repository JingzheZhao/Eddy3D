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
using Microsoft.VisualBasic.FileIO;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ReadUTCIHourly : GH_Component
    {

        // exposure
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }


        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReadUTCIHourly()
          : base("ReadUTCIByHour", "ReadUTCIByHour", "ReadUTCIByHour", "Eddy", "6 | Outdoor Comfort")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation", "Sim", "Sim", GH_ParamAccess.item);

            pManager.AddTextParameter("Interval", "Int", "It may either be a single hour or a multiline Ladybug analysisPeriod formatted as (6, 15, 1) (6, 15, 24) for beginning and end respectively.", GH_ParamAccess.list);

            //pManager.AddIntegerParameter("Mode", "Mode", "Interval to be used for evaluation. It may either be a single hour (mode 1) or a multiline Ladybug analysisPeriod formatted as (6, 15, 1) (6, 15, 24) for beginning and end respectively (mode 2).", GH_ParamAccess.item, 0);
            //Param_Integer evaluationMode = pManager[2] as Param_Integer;
            //evaluationMode.AddNamedValue("single hour", 0);
            //evaluationMode.AddNamedValue("LB analysisPeriod", 1);

            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.tree);
            //pManager.AddGenericParameter("UTCIT", "UTCIT", "UTCIT in °C", GH_ParamAccess.tree);        
            //pManager.AddGenericParameter("HumanConditions", "HC", "HumanConditions", GH_ParamAccess.tree);
            //pManager.AddGenericParameter("ComfortHours", "CH", "ComfortHours in %", GH_ParamAccess.list);     

            pManager.AddGenericParameter("U", "U", "Overall Uncertainty in %", GH_ParamAccess.item);



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




            bool Run = false;

            //int evaluationMode = 0;
            //DA.GetData(2, ref evaluationMode);


            List<string> dateTimeInput = new List<string>();
            DA.GetDataList(1, dateTimeInput);


            DA.GetData(2, ref Run);

            int annualHours = 8760;



            if (!Run) { return; }


            if (dateTimeInput.Count == 1)
            {

                //if (!DA.GetDataList(1, gobj2)) { }

                //if ((gobj2[0].Value is Int16 || gobj2[0].Value is Int32 || gobj2[0].Value is Int64 || gobj2[0].Value is Double))
                //{
                //    IntervalAsNumber = (int)gobj2[0].Value;
                //}
                //if (IntervalAsNumber == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }

                

                var IntervalAsNumber = (int)double.Parse(dateTimeInput[0]);


                //if (inputHour > 8759)
                //{
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please connect a number slider that represent 8760 hours of the year as a maximum range."); return;
                //}


                //if (dateTimeInput.Count > 1)
                //{
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please connect a number slider that represent 8760 hours of the year as a maximum range."); return;
                //}

                var path = RES.WorkingDirectory + @"\UTCI.csv";

                var allLines = File.ReadAllLines(path);
                var numberOfProbes = allLines.Count();

                //int inputHour = Convert.ToInt32(IntervalAsNumber);

                double[] HourlyUTCI = new double[numberOfProbes];

                for (int i = 0; i < numberOfProbes && IntervalAsNumber < annualHours; i++)
                {
                    HourlyUTCI[i] = double.Parse(allLines[i].Split(',')[IntervalAsNumber]);

                }


                DA.SetDataList(0, HourlyUTCI);


            }


            if (dateTimeInput.Count == 2)
            {

                //// Fill datatrees from CSV

                //List<string> ladybugAnalysisPeriod = new List<string>();

                //DA.GetDataList(1, ladybugAnalysisPeriod);
                //List<string> inputInterval = new List<string>();
                //DA.GetDataList(1, inputInterval);

                //if (!DA.GetDataList(1, gobj2)) { }

                //foreach (GH_ObjectWrapper o in gobj2)
                //{

                //    if (o is String)
                //    {
                //        IntervalAsString.Add((String)o.Value);
                //    }

                //}
                //if (IntervalAsString == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid Int object"); return; }

                //DA.GetDataList(1, dateTimeInput);

                var ladybugAnalysisPeriod = dateTimeInput;



                if (ladybugAnalysisPeriod == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid analysis periode object."); return;
                }



                var allLines = File.ReadAllLines(RES.WorkingDirectory + @"\UTCI.csv");
                var numberOfProbes = allLines.Count();


                // -1 because of python

                int month_start = int.Parse(ladybugAnalysisPeriod[0].Split(',')[0].Split('(')[1]) - 1;
                var month_end = int.Parse(ladybugAnalysisPeriod[1].Split(',')[0].Split('(')[1]);
                var day_start = int.Parse(ladybugAnalysisPeriod[0].Split(',')[1]) - 1;
                var day_end = int.Parse(ladybugAnalysisPeriod[1].Split(',')[1]);
                var hour_start = int.Parse(ladybugAnalysisPeriod[0].Split(',')[2].Split(')')[0]) - 1;
                var hour_end = int.Parse(ladybugAnalysisPeriod[1].Split(',')[2].Split(')')[0]);



                var hours = hour_end - hour_start;

                // Fill array once

                double[,] HourlyUTCI = new double[numberOfProbes, hours];


                System.Threading.Tasks.Parallel.For(0, numberOfProbes,
                  i =>
                  {

                      for (int h = 0; h < hours; h++)
                      {
                          HourlyUTCI[i, h] = double.Parse(allLines[i].Split(',')[h]);
                      }


                  });



                //double[] valueHour = new double[numberOfLines];
                var valueHour = new DataTree<double>();


                // Fill datatrees from array that has been filled before

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

                            for (int i = 0; i < numberOfProbes; i++)
                            {
                                // TODO: move this out of loop later

                                if (GH_Document.IsEscapeKeyDown())
                                {
                                    GH_Document GHDocument = OnPingDocument();
                                    GHDocument.RequestAbortSolution();
                                }

                                valueHour.Add(HourlyUTCI[i, h], new Grasshopper.Kernel.Data.GH_Path(h));



                            }

                        }
                    }


                    DA.SetDataTree(0, valueHour);



                    //    var UTCITree = new DataTree<double>();
                    ////var HumanConditionsTree = new DataTree<int>();
                    ////var ComfortHoursList = new List<double>();


                    //using (Microsoft.VisualBasic.FileIO.TextFieldParser csvParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(path))
                    //{
                    //    csvParser.CommentTokens = new string[] { "#" };
                    //    csvParser.SetDelimiters(new string[] { "," });
                    //    csvParser.HasFieldsEnclosedInQuotes = false;

                    //    // Skip the row with the column names
                    //    //csvParser.ReadLine();
                    //    int cnt = 0;


                    //    while (!csvParser.EndOfData)
                    //    {
                    //        // Read current line fields, pointer moves to the next line.
                    //        string[] fields = csvParser.ReadFields();



                    //        int comfortCnt = 0;

                    //        for (int i = 0; i < annualHours; i++)
                    //        {
                    //            UTCITree.Add(double.Parse(fields[i]), new Grasshopper.Kernel.Data.GH_Path(cnt));
                    //            //HumanConditionsTree.Add(UTCI.GetConditionOfPerson(double.Parse(fields[i])), new Grasshopper.Kernel.Data.GH_Path(cnt));

                    //            //if (UTCI.GetConditionOfPerson(double.Parse(fields[i])) == 0)
                    //            //{
                    //                //comfortCnt++;
                    //            //}


                    //        }

                    //        //double cmftPercentage = Math.Round((double)comfortCnt * 100 / 8760, 1);

                    //        //ComfortHoursList.Add(cmftPercentage);

                    //        cnt++;
                    //    }
                    //}




                }



                // Read uncertainty file

                var uncertaintyLine = File.ReadLines(RES.WorkingDirectory + @"\UTCI.uncertainty").Last();
                var uncertaintyVal = double.Parse(uncertaintyLine.Split('%')[0].Split(':')[1].Split('r')[1]);


                //DA.SetDataTree(1, UTCITree);
                //DA.SetDataTree(2, HumanConditionsTree);
                //DA.SetDataList(3, ComfortHoursList);
                DA.SetData(1, uncertaintyVal);


            }


        }




        //for (int i = 0; i < numberOfProbes; i++)
        //{
        //    for (int h = 0; h < 8759; h++)
        //    {

        //        ConditionOfPerson.Add(UTCI.GetConditionOfPerson(HourlyUTCI[i, h]), new Grasshopper.Kernel.Data.GH_Path(h));
        //    }

        //}






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
            get { return new Guid("{404BC568-09DC-490A-9637-BC0BB7AA6B5F}"); }
        }
    }
}

