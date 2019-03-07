
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.
namespace Eddy
{
    public class ReadComfortHours : GH_Component
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
        public ReadComfortHours()
          : base("ReadComfortHours", "ReadComfortHours", "Read annual accumulated comfort hours in % from UTCI.", "Eddy", "UTCI")
        {
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        /// 

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Use Ladybug Analysis period.", Menu_DoClick, true, !LadybugAnalysisPeriod);
        }

        private void Menu_DoClick(object sender, EventArgs e)
        {
            LadybugAnalysisPeriod = !LadybugAnalysisPeriod;
            ExpireSolution(true);

        }
        public bool LadybugAnalysisPeriod = true;


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation", "Sim", "Sim", GH_ParamAccess.item);

            pManager.AddGenericParameter("Interval", "Int", "Interval to be avaluated. May either be intervals of hours or Ladybug analysisPeriods.", GH_ParamAccess.list);
            pManager.AddNumberParameter("UTCI", "UTCI", "List of UTCI values.", GH_ParamAccess.list);
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
            DA.GetData(2, ref Run);

            if (!Run)
            {
                return;
            }




            double[] ComfortHours = new double[numberOfProbes];


            if (LadybugAnalysisPeriod)
            {

                List<List<string>> ladybugAnalysisPeriod = new List<List<string>>();
                // Get interval
                DA.GetDataList(1, ladybugAnalysisPeriod);                

                if (ladybugAnalysisPeriod == null || ladybugAnalysisPeriod.Count != 2)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid analysis periode object."); return;
                }

            }

            else
            {
                List<Interval> intervals = new List<Interval>();
                DA.GetDataList(1, intervals);

                List<List<int>> fullInputList = new List<List<int>>();

                // Make list of list of hours

                foreach (Interval inter in intervals)
                {
                    fullInputList.Add(Utilities.GetEvalHoursFromInterval(inter));
                }

               



            }


    

            DA.SetDataList(0, ComfortHours);
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
        public override Guid ComponentGuid => new Guid("{91BD9BAF-A114-4062-AA82-30E9C82CE36E}");
    }
}