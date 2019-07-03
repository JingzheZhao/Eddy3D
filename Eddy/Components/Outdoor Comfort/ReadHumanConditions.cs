using System;
using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ReadHumanConditions : GH_Component
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
        public ReadHumanConditions()
          : base("ReadHumanConditions", "ReadHumanConditions", "Read annual human conditions from UTCI.", "Eddy", "6 | Outdoor Comfort")
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
            pManager.AddGenericParameter("HumanConditions", "HC", "HumanConditions accodring to the UTCI scale.", GH_ParamAccess.tree);
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

            DA.GetData(1, ref Run);

            int annualHours = 8760;

            if (Run)
            {
                //// Fill datatrees from CSV

                var path = RES.WorkingDirectory + @"\UTCI.csv";

                var HumanConditionsTree = new DataTree<int>();

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

                        //int comfortCnt = 0;

                        for (int i = 0; i < annualHours; i++)
                        {
                            //UTCITree.Add(double.Parse(fields[i]), new Grasshopper.Kernel.Data.GH_Path(cnt));
                            HumanConditionsTree.Add(UTCI.CalcConditionOfPerson(double.Parse(fields[i])), new Grasshopper.Kernel.Data.GH_Path(cnt));

                            //if (UTCI.GetConditionOfPerson(double.Parse(fields[i])) == 0)
                            //{
                            //comfortCnt++;
                            //}
                        }

                        //double cmftPercentage = Math.Round((double)comfortCnt * 100 / 8760, 1);

                        //ComfortHoursList.Add(cmftPercentage);

                        cnt++;
                    }
                }

                DA.SetDataTree(0, HumanConditionsTree);
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
                return Resources.Eddy_readUTCI;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{4689EFB2-5B8D-41CC-8264-1125BC3BDA05}"); }
        }
    }
}