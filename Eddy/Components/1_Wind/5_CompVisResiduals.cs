using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Residuals : GH_Component
    {
        private enum ResidualViewerTarget
        {
            GithubPages = 0,
            Streamlit = 1
        }

        private static readonly Uri GithubPagesViewerUri = new Uri("https://eddy3d-dev.github.io/Plot-OpenFOAM-Residuals/");
        private static readonly Uri StreamlitViewerUri = new Uri("https://plot-openfoam-residuals.streamlit.app/");

        private ResidualViewerTarget _viewerTarget = ResidualViewerTarget.GithubPages;
        private bool _lastRunState;

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public Residuals()
            : base("Plot Residuals", "Residuals",
                @"Convergence Monitor

Opens the selected residual viewer and the simulation residuals folder.

" + EddyVersion.toString(),
                EddyVersion.Name, "1 | Wind")
        {
        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Open Plotter (GitHub Pages)", (s, e) => OpenViewerInBrowser(GithubPagesViewerUri));
            Menu_AppendItem(menu, "Open Plotter (Streamlit)", (s, e) => OpenViewerInBrowser(StreamlitViewerUri));

            Menu_AppendSeparator(menu);
            Menu_AppendItem(
                menu,
                "Use GitHub Pages For Plot Input",
                (s, e) => SetViewerTarget(ResidualViewerTarget.GithubPages),
                true,
                _viewerTarget == ResidualViewerTarget.GithubPages);
            Menu_AppendItem(
                menu,
                "Use Streamlit For Plot Input",
                (s, e) => SetViewerTarget(ResidualViewerTarget.Streamlit),
                true,
                _viewerTarget == ResidualViewerTarget.Streamlit);
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("ResidualViewerTarget", (int)_viewerTarget);
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if (reader.ItemExists("ResidualViewerTarget"))
            {
                int value = reader.GetInt32("ResidualViewerTarget");
                if (Enum.IsDefined(typeof(ResidualViewerTarget), value))
                {
                    _viewerTarget = (ResidualViewerTarget)value;
                }
            }

            return base.Read(reader);
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

            pManager.AddBooleanParameter(
                "Plot", "Plot",
                "Set to true to open the residual plot viewer and residuals folder.",
                GH_ParamAccess.item, false);

            pManager[1].Optional = true;
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
            OFResult result = null;
            if (!DA.GetData(0, ref result) || result == null)
            {
                _lastRunState = false;
                return;
            }

            bool run = false;
            DA.GetData(1, ref run);

            if (!run)
            {
                Message = "Toggle 'Run' to view";
                _lastRunState = false;
                return;
            }

            if (_lastRunState)
            {
                Message = "Open";
                return;
            }

            _lastRunState = true;
            Message = "Open";

            try
            {
                OpenViewerInBrowser(GetSelectedViewerUri());
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    string.Format("Could not open residual viewer: {0}", ex.Message));
            }

            TryOpenResidualsFolder(result);
        }

        private void SetViewerTarget(ResidualViewerTarget target)
        {
            _viewerTarget = target;
            ExpireSolution(true);
        }

        private Uri GetSelectedViewerUri()
        {
            return _viewerTarget == ResidualViewerTarget.GithubPages
                ? GithubPagesViewerUri
                : StreamlitViewerUri;
        }

        private void OpenViewerInBrowser(Uri viewerUri)
        {
            if (viewerUri == null)
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(viewerUri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    string.Format("Could not open browser link: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Tries to open the most relevant residuals folder for the given result.
        /// </summary>
        private void TryOpenResidualsFolder(OFResult result)
        {
            string residualsFolder = GetResidualsFolder(result);
            if (string.IsNullOrWhiteSpace(residualsFolder))
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    string.Format("Could not find a residuals folder under \"{0}\".", result.WorkingDirectory));
                return;
            }

            try
            {
                OpenFolder(residualsFolder);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Error,
                    string.Format("Could not open folder: {0}", ex.Message));
            }
        }

        private static string GetResidualsFolder(OFResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.WorkingDirectory))
            {
                return null;
            }

            var candidates = new List<string>();

            var windDirs = result.Domain?.BCond?.WindDirections ?? new List<int>();
            foreach (int dir in windDirs.Distinct())
            {
                candidates.Add(Path.Combine(result.WorkingDirectory, dir.ToString(), "postProcessing", "residuals", "0"));
                candidates.Add(Path.Combine(result.WorkingDirectory, dir.ToString(), "postProcessing", "residuals"));
                candidates.Add(Path.Combine(result.WorkingDirectory, dir.ToString(), "postProcessing"));
            }

            candidates.Add(Path.Combine(result.WorkingDirectory, "postProcessing", "residuals", "0"));
            candidates.Add(Path.Combine(result.WorkingDirectory, "postProcessing", "residuals"));
            candidates.Add(Path.Combine(result.WorkingDirectory, "postProcessing"));

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void OpenFolder(string folderPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var psi = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
                psi.ArgumentList.Add(folderPath);
                Process.Start(psi);
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var psi = new ProcessStartInfo("open") { UseShellExecute = false };
                psi.ArgumentList.Add(Path.GetFullPath(folderPath));
                Process.Start(psi);
                return;
            }

            var psiLinux = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
            psiLinux.ArgumentList.Add(folderPath);
            Process.Start(psiLinux);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
            // You can add image files to your project resources and access them like this:
            Resources.Eddy_stability;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{2936A937-4F42-4873-B65D-D02417D73D06}");
    }
}
