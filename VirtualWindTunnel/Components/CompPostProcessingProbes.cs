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

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class PostProcessingProbes : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public PostProcessingProbes()
          : base("Probes", "Probes", "postProcessing", "Eddy", "postProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddPointParameter("points", "points", "points", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 0);

            Param_Integer param = pManager[2] as Param_Integer;
            param.AddNamedValue("cp_Probes", 0);
            param.AddNamedValue("U_Probes", 1);
            pManager.AddBooleanParameter("Run", "Run", "Clean the directory", GH_ParamAccess.item, false);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Result", "Result", GH_ParamAccess.list);
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




            int mode = 0;            
            List<Point3d> listOfPoints = new List<Point3d>();
            bool run = false;

            DA.GetDataList(1, listOfPoints);
            //DA.GetData(2, ref pointName);
            DA.GetData(2, ref mode);
            DA.GetData(3, ref run);






            if (run == true)
            {


                if (mode == 0) // cp
                {
                    string pointName = "cp_Probes";
                    DOM.postProcessingDirectory = DOM.workingDirectory + @"\postProcessing\";

                    if (!Directory.Exists(DOM.postProcessingDirectory))
                    {
                        Directory.CreateDirectory(DOM.postProcessingDirectory);
                    }
                    //Write sampleDict

                    File.WriteAllText(Path.Combine(DOM.systemDirectory + "controlDict"), StringTemplates.controlDict(DOM.iter, DOM.writeInterval, DOM.keepTimeSteps, null));
                    File.WriteAllText(Path.Combine(DOM.systemDirectory + pointName), StringTemplates.sampleProbes(listOfPoints, pointName, mode));


                    //Start sample process                
                    string command = @"""postProcess -func " + pointName + @" -latestTime""";


                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.hardcodedAssemblyDir + @"\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();

                    //Parse file

                    ParsingValues values = new ParsingValues(listOfPoints, pointName, DOM.workingDirectory);

                    File.WriteAllText(DOM.postProcessingDirectory + pointName + ".csv", values.ToString());
                    DA.SetData(0, values);

                }

                if (mode == 1) // U
                {
                    string pointName = "U_Probes";
                    string postProcessDirectory = DOM.workingDirectory + @"\postProcessing\";

                    if (!Directory.Exists(DOM.postProcessingDirectory))
                    {
                        Directory.CreateDirectory(DOM.postProcessingDirectory);
                    }

                    File.WriteAllText(Path.Combine(DOM.systemDirectory + pointName), StringTemplates.sampleProbes(listOfPoints, pointName, mode));



                    string command = @"""postProcess -func " + pointName + @" -latestTime""";

                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.hardcodedAssemblyDir + @"\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();

                    p.WaitForExit();

                    ParsingValues values = new ParsingValues(listOfPoints, pointName, DOM.workingDirectory);

                    File.WriteAllText(postProcessDirectory + pointName + ".csv", values.ToString());


                    //DA.SetDataList(0, values);





                }
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
            get { return new Guid("{D39A60E1-7086-4C6F-BFF1-492D84910227}"); }
        }
    }
}
