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

            if (Run)
            {

                int hour = 0;

                DA.GetData(1, ref hour);


                if (hour > 8759)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please connect a number slider that represent 8760 hours of the year as a maximum value."); return;
                }





                var allLines = File.ReadAllLines(DOM.baseWorkingDirectory + @"\UTCI.csv");
                var numberOfLines = allLines.Count();




                double[] valueHours = new double[numberOfLines];
                for (int i = 0; i < numberOfLines && hour < 8760; i++)
                {
                    valueHours[i] = double.Parse(allLines[i].Split(',')[hour]);
                }



                // Parse UTCI uncertaintly from file

                var uncertaintyLine = File.ReadLines(DOM.baseWorkingDirectory + @"\UTCI.uncertainty").Last();
                var uncertaintyNUM = double.Parse(uncertaintyLine.Split('%')[0].Split(':')[1].Split('r')[1]);





                DA.SetDataList(0, valueHours);
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
            get { return new Guid("{404BC568-09DC-490A-9637-BC0BB7AA6B5F}"); }
        }
    }
}



