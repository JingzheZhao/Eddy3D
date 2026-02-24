using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Clean : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.quarternary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public Clean()
          : base(GH_Strings.Clean.Name, GH_Strings.Clean.Nick,
GH_Strings.Clean.Desc + EddyVersion.toString(),
              EddyVersion.Name, "3 | Pre-Processing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                GH_Strings.Clean.ResDir, GH_Strings.Clean.ResDirNick, 
                GH_Strings.Clean.ResDirDesc, 
                GH_ParamAccess.item);

            pManager.AddIntegerParameter(
                GH_Strings.Clean.Mode, GH_Strings.Clean.ModeNick, 
                GH_Strings.Clean.ModeDesc, 
                GH_ParamAccess.item, 0);
            Param_Integer param = pManager[1] as Param_Integer;
            param.AddNamedValue("Mesh Directory", 0);
            param.AddNamedValue("Simulation Directories", 1);
            param.AddNamedValue("Both", 2);

            pManager.AddBooleanParameter(
                GH_Strings.Clean.Run, GH_Strings.Clean.RunNick, 
                "Set True to delete directories. CAUTION: Cannot be undone.", 
                GH_ParamAccess.item, false);
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

            int Mode = 1;

            OFResult RES = null;
            String workingDirectory = null;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(GH_Strings.Clean.ResDir, ref gobj)) { return; }

            if ((gobj.Value is OFResult))
            {
                RES = (OFResult)gobj.Value;
                workingDirectory = RES.WorkingDirectory;
            }
            else if (gobj.Value is GH_String)
            {
                var conversion = GH_Convert.ToString(gobj.Value, out workingDirectory, GH_Conversion.Both);
                workingDirectory = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(workingDirectory);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide either a result object of path to a simulation folder"); return;
            }

            DA.GetData(GH_Strings.Clean.Mode, ref Mode);
            DA.GetData(GH_Strings.Clean.Run, ref Run);

            if (Run)
            {
                bool cleanSucceeded = true;

                List<string> windDirDirectories =
                    Directory.GetDirectories(workingDirectory, "*", SearchOption.TopDirectoryOnly)
                             .Where(f => Regex.IsMatch(f, @"[\\/]\d+$"))
                             .ToList();

                string meshDirectory = Path.Combine(workingDirectory, "mesh");

                if (Mode == 0)
                {
                    // Mesh-only clean
                    cleanSucceeded &= EddyLib.Utilities.FoamCleaner.CleanCase(meshDirectory);
                }
                else if (Mode == 1)
                {
                    // Clean OpenFOAM cases (preserve system/constant/0)
                    foreach (string directory in windDirDirectories)
                    {
                        cleanSucceeded &= EddyLib.Utilities.FoamCleaner.CleanCase(directory);
                    }

                    // Remove root-level probe cache binaries (<workingDir>/postProcessing/*.bin)
                    cleanSucceeded &= EddyLib.Utilities.FoamCleaner.CleanProbeCacheFiles(workingDirectory);
                }
                else
                {
                    // Clean mesh and cases
                    cleanSucceeded &= EddyLib.Utilities.FoamCleaner.CleanCase(meshDirectory);

                    foreach (string directory in windDirDirectories)
                    {
                        cleanSucceeded &= EddyLib.Utilities.FoamCleaner.CleanCase(directory);
                    }

                    // Remove root-level probe cache binaries (<workingDir>/postProcessing/*.bin)
                    cleanSucceeded &= EddyLib.Utilities.FoamCleaner.CleanProbeCacheFiles(workingDirectory);
                }

                if (!cleanSucceeded)
                {
                    AddRuntimeMessage(
                        GH_RuntimeMessageLevel.Warning,
                        "Clean completed with some deletion errors (possibly locked files).");
                }

                foreach (IGH_DocumentObject obj in Grasshopper.Instances.ActiveCanvas.Document.ActiveObjects())
                {
                    if (obj == null) continue;
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
