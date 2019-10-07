using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Clean : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public Clean()
          : base("Clean", "Clean",
              "Clean",
              "Eddy", "3 | PreProcessing")
        {
        }

        private IGH_DocumentObject[] AllCanvasObjects()
        {
            var doc = OnPingDocument();
            if (doc == null)
                return new IGH_DocumentObject[0];
            return doc.Objects.ToArray();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Directory", "Dir", "Working directory", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mode", "Mode", "Directories to delete", GH_ParamAccess.item, 1);
            Param_Integer param = pManager[1] as Param_Integer;
            param.AddNamedValue("Mesh Directory", 0);
            param.AddNamedValue("Simulation Directories", 1);
            param.AddNamedValue("Both", 2);
            pManager.AddBooleanParameter("Run", "Run", "Clean the directory", GH_ParamAccess.item, false);
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
            bool Run = false;
            string workingDirectory = "";
            int Mode = 1;

            DA.GetData(0, ref workingDirectory);
            DA.GetData(1, ref Mode);
            DA.GetData(2, ref Run);

            if (Run)
            {
                List<string> windDirDirectories = Directory.GetDirectories(workingDirectory, "*",
        SearchOption.TopDirectoryOnly)
        .Where(f => Regex.IsMatch(f, @"[\\/]\d+$")).ToList();

                string meshDirectory = workingDirectory + @"\mesh";

                if (Mode == 0)
                {
                    Utilities.Directories.processDirectory(meshDirectory, false);
                }
                else if (Mode == 1)
                {
                    foreach (string directory in windDirDirectories)
                    {
                        Utilities.Directories.processDirectory(directory, true);
                    }
                }
                else
                {
                    Utilities.Directories.processDirectory(meshDirectory, false);

                    foreach (string directory in windDirDirectories)
                    {
                        Utilities.Directories.processDirectory(directory, true);
                    }
                }

                foreach (IGH_DocumentObject obj in Grasshopper.Instances.ActiveCanvas.Document.ActiveObjects())
                {
                    //var slider = obj as Grasshopper.Kernel.Special.GH_NumberSlider;
                    if (obj == null) continue;
                    //slider.Attributes.Selected = true;
                    obj.ExpireSolution(true);
                }
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                    // You can add image files to your project resources and access them like this:
                    Resources.Eddy_clean;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{EE4594E9-FF2E-4E07-8156-957F1F9E08EE}");
    }
}