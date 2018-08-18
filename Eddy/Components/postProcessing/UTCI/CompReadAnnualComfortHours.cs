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
    public class ReadComfortHours : GH_Component
    {




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReadComfortHours()
          : base("ReadComfortHours", "ReadComfortHours", "Read annual accumulated comfort hours in % from UTCI.", "Eddy", "UTCI")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation", "Sim", "Sim", GH_ParamAccess.item);

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


            DA.GetData(1, ref Run);

            int annualHours = 8760;


            if (Run)
            {

                //// Fill datatrees from CSV


                var path = DOM.baseWorkingDirectory + @"\UTCI.csv";
                List<double> ComfortHoursList = new List<double>();



                using (Microsoft.VisualBasic.FileIO.TextFieldParser csvParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(path))
                {
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = false;

                    // Skip the row with the column names
                    //csvParser.ReadLine();
                    int cnt = 0;


                    while (!csvParser.EndOfData)
                    {
                        // Read current line fields, pointer moves to the next line.
                        string[] fields = csvParser.ReadFields();



                        int comfortCnt = 0;

                        for (int i = 0; i < annualHours; i++)
                        {
                            //UTCITree.Add(double.Parse(fields[i]), new Grasshopper.Kernel.Data.GH_Path(cnt));
                            //HumanConditionsTree.Add(UTCI.GetConditionOfPerson(double.Parse(fields[i])), new Grasshopper.Kernel.Data.GH_Path(cnt));

                            if (UTCI.GetConditionOfPerson(double.Parse(fields[i])) == 0)
                            {
                            comfortCnt++;
                            }


                        }

                        double cmftPercentage = Math.Round((double)comfortCnt * 100 / 8760, 1);

                        ComfortHoursList.Add(cmftPercentage);

                        cnt++;
                    }
                }


                DA.SetDataList(0, ComfortHoursList);


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
            get { return new Guid("{91BD9BAF-A114-4062-AA82-30E9C82CE36E}"); }
        }
    }
}


