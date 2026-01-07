using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.IO;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompParaview : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.senary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompParaview()
          : base("Open ParaView", "ParaView", 
@"Launch ParaView for 3D CFD result visualization.

Opens simulation results in ParaView for visualizing velocity fields, 
pressure distributions, and streamlines. ParaView must be installed.

" + EddyVersion.toString(),
              EddyVersion.Name, "5 | Post-Processing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Result", "Res", 
                "Simulation result from Wind Simulation component.", 
                GH_ParamAccess.item);

            pManager.AddIntegerParameter(
                "Wind Directions", "Dir", 
                "Wind directions to visualize (subset or all).", 
                GH_ParamAccess.list);
            pManager.AddIntegerParameter("Paraview version", "Ver", "Paraview version", GH_ParamAccess.item, 2);
            Param_Integer param = pManager[2] as Param_Integer;
            param.AddNamedValue("Windows V4", 0);
            param.AddNamedValue("Windows V5", 1);
            param.AddNamedValue("BlueCFD", 2);
            pManager.AddBooleanParameter("Run", "Run", "Start Paraview", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
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
            // mode to select simulation environment
            //if (!paraViewVersion5) { Message = "ParaView 4"; version = 4; }
            //else { Message = "ParaView 5"; version = 5; }

            // read inputs
            //------------

            OFResult RES = null;
            DA.GetData(0, ref RES);

            List<int> dirs = new List<int>();
            DA.GetDataList("Wind directions", dirs);

            if (dirs.Count == 0)
            {
                dirs.Add(RES.Domain.BCond.WindDirections[0]);
            }

            int version = 0;
            DA.GetData("Paraview version", ref version);

            bool run = false;
            DA.GetData("Run", ref run);

            // Write load script

            var scriptPath = RES.WorkingDirectory + "openParaview.py";
            var scriptContent = Utilities.PrepareParaviewLoadScript(RES.WorkingDirectory, dirs);

            File.WriteAllText(scriptPath, scriptContent);

            if (!run)
            {
                return;
            }

            // "C:\\Program Files\\ParaView 5.6.0-Windows-msvc2015-64bit\\bin\\paraview.exe\" \"C:\\testDomain\\259\\259.foam
            string paraViewPath = "\"" + EddyLib.Utilities.GetParaviewPath(version) + "\" " + @"--script=" + "\"" + scriptPath + "\"";
            EddyLib.Utilities.StartProcess.StartProcessCMDNT(paraViewPath, true, false, true, true);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_paraview;//return null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{2FEE37D4-A096-4F3C-9972-0EB3BB3A9CF1}");
    }
}