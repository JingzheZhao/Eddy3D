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




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReadUTCIHourly()
          : base("ReadUTCIHourly", "ReadUTCIHourly", "ReadUTCIHourly", "Eddy", "UTCI")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Hour", "Hour", "Hour", GH_ParamAccess.item, 0);

            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("AnnEx", "AnnEx", "Annual Exceedance in %", GH_ParamAccess.list);
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


            int inputHour = 0;
            DA.GetData(1, ref inputHour);
            DA.GetData(2, ref Run);


            if (Run)
            {


                if (inputHour > 8759)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please connect a number slider that represent 8760 hours of the year as a maximum range."); return;
                }


                var path = DOM.baseWorkingDirectory + @"\UTCI.csv";

                var allLines = File.ReadAllLines(path);
                var numberOfProbes = allLines.Count();


                int annualHours = 8760;



                double[] HourlyUTCI = new double[numberOfProbes];

                for (int i = 0; i < numberOfProbes && inputHour < annualHours; i++)
                {
                    HourlyUTCI[i] = double.Parse(allLines[i].Split(',')[inputHour]);

                }



                double[,] FullUTCI = new double[numberOfProbes, annualHours];

                //System.Threading.Tasks.Parallel.For(0, numberOfProbes,
                //i =>
                //{

                //    for (int h = 0; h < annualHours - 1; h++)
                //    {
                //        FullUTCI[i, h] = double.Parse(allLines[i].Split(',')[h]);
                //    }


                //});



                

                
                //using (TextFieldParser csvParser = new TextFieldParser(path))
                //{
                //    csvParser.CommentTokens = new string[] { "#" };
                //    csvParser.SetDelimiters(new string[] { "," });
                //    csvParser.HasFieldsEnclosedInQuotes = false;

                //    // Skip the row with the column names
                //    csvParser.ReadLine();

                //    while (!csvParser.EndOfData)
                //    {
                //        // Read current line fields, pointer moves to the next line.
                //        string[] fields = csvParser.ReadFields();
                //        string Name = fields[0];
                //        string Address = fields[1];
                //    }
                //}



                ////double[] valueHour = new double[numberOfLines];
                //var UTCITree = new DataTree<double>();


                //// Fill datatrees

                var ConditionOfPerson = new DataTree<int>();
                var AnnEx = new List<double>();


                //System.Threading.Tasks.Parallel.For(0, numberOfProbes,
                //j =>
                //{

                //    for (int h = 0; h < annualHours - 1; h++)
                //    {

                //        //ConditionOfPerson.Add(UTCI.GetConditionOfPerson(FullUTCI[i, h]), new Grasshopper.Kernel.Data.GH_Path(h));
                //        UTCITree.Add(UTCITree[UTCITree.Path(j), h], new Grasshopper.Kernel.Data.GH_Path(h));

                //        // Evaluate annual conditions

                //        int conditionCounter = 0;

                //        if (ConditionOfPerson[ConditionOfPerson.Path(j), h] != 0)
                //        {
                //            conditionCounter += 1;
                //            AnnEx.Add(conditionCounter / 8760 * 100);
                //        }
                //    }
                //});


                var uncertaintyLine = File.ReadLines(DOM.baseWorkingDirectory + @"\UTCI.uncertainty").Last();
                var uncertaintyNUM = double.Parse(uncertaintyLine.Split('%')[0].Split(':')[1].Split('r')[1]);





                DA.SetDataList(0, HourlyUTCI);
                DA.SetDataList(1, AnnEx);
                DA.SetData(2, uncertaintyNUM);
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


