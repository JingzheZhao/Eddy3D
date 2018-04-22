using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ParseAnnualCP : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ParseAnnualCP()
          : base("ParseAnnualCP", "ParseAnnualCP",  "ParseAnnualCP", "Eddy", "postProcessing")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("out", "out", "out", GH_ParamAccess.list);
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






            List<string> fullProbeFilePath = new List<String>();


            //Build paths as list

            for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
            {
                fullProbeFilePath.Add(DOM.simWorkingDirectory + DOM.BCInflow.windDir[i] + @"\postProcessing\cp_Probes.csv");
            }

            var numberOfWindDirs = DOM.BCInflow.windDir.Count();
            var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count();



            // Array for output data

            var listOfAnnualData = new double[numberOfWindDirs][];

            for (int r = 0; r < numberOfWindDirs; r++)
            {
                listOfAnnualData[r] = new double[numberOfProbes];
                for (int c = 0; c < numberOfProbes; c++)
                {
                    listOfAnnualData[r][c] = double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c]);
                }
            }



            // Write Array to file

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
            {
                sb.Append(DOM.BCInflow.windDir[i] + ",");

            }
            sb.AppendLine("");
            for (int r = 0; r < numberOfProbes; r++)
            {
                for (int c = 0; c < numberOfWindDirs; c++)
                {
                    sb.Append(listOfAnnualData[c][r] + ",");
                }
                sb.AppendLine("");
            }
            File.WriteAllText(DOM.simWorkingDirectory + @"\annualCPData.csv", sb.ToString());

            DA.SetDataList(0, sb.ToString());


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
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{ED246ABD-F3E7-4AEC-A24C-CD6ADA25A389}"); }
        }
    }
}
