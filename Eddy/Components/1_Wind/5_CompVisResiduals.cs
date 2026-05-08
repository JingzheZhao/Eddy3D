using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        private static readonly Uri GithubPagesViewerUri = new Uri("https://residuals.eddy3d.com/");
        private static readonly Uri StreamlitViewerUri = new Uri("https://plot-openfoam-residuals.streamlit.app/");

        private ResidualViewerTarget _viewerTarget = ResidualViewerTarget.GithubPages;

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

Opens the selected residual viewer with the simulation data.

" + EddyVersion.toString(),
                EddyVersion.Name, "1 | Wind")
        {
        }

        public override void CreateAttributes()
        {
            Attributes = new ProbeRunButtonAttributes(this);
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

            pManager.AddParameter(
                new GH_ToggleParam("Plot", "Plot", "Click to open the residual plot viewer."),
                "Plot", "Plot",
                "Click to open the residual plot viewer.",
                GH_ParamAccess.item);

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
                return;
            }

            // Support both the built-in round button and an externally wired boolean.
            // Only read DA.GetData when an external source is connected, so the
            // toggle's own persistent data can't cause accidental re-triggers.
            bool run = false;
            if (Params.Input[1].SourceCount > 0)
            {
                DA.GetData(1, ref run);
            }
            run = run || ConsumeToggleRun(1);

            if (!run)
            {
                return;
            }

            try
            {
                string residualsFolder = GetResidualsFolder(result);
                if (_viewerTarget == ResidualViewerTarget.GithubPages && !string.IsNullOrWhiteSpace(residualsFolder))
                {
                    ServeFileAndOpenBrowser(GetSelectedViewerUri(), residualsFolder);
                }
                else
                {
                    OpenViewerInBrowser(GetSelectedViewerUri());
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    string.Format("Could not open residual viewer: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Checks whether the toggle at the given input index was clicked,
        /// and immediately resets it so it behaves like a momentary push-button
        /// rather than a sticky toggle.
        /// </summary>
        private bool ConsumeToggleRun(int inputIndex)
        {
            if (inputIndex < 0
                || inputIndex >= Params.Input.Count
                || !(Params.Input[inputIndex] is GH_ToggleParam toggle)
                || !toggle.Toggle)
            {
                return false;
            }

            // Reset immediately so any re-entrant or subsequent solution
            // does not see the toggle as still "on".
            toggle.Toggle = false;
            toggle.PersistentData.Clear();
            toggle.PersistentData.Append(new Grasshopper.Kernel.Types.GH_Boolean(false));

            // Schedule a lightweight solution to refresh the UI rendering.
            OnPingDocument()?.ScheduleSolution(5, _ => { });
            return true;
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

        private void ServeFileAndOpenBrowser(Uri baseUri, string folderPath)
        {
            string fileToServe = null;
            var candidates = new[] { "residuals.dat", "residuals.log" };
            foreach (var c in candidates)
            {
                string p = Path.Combine(folderPath, c);
                if (File.Exists(p))
                {
                    fileToServe = p;
                    break;
                }
            }

            if (fileToServe == null)
            {
                OpenViewerInBrowser(baseUri);
                return;
            }

            // Read the file and Base64-encode it into the URL hash fragment.
            // This bypasses all CORS / Private Network Access / mixed-content
            // restrictions because hash fragments are handled entirely client-side.
            byte[] fileBytes = File.ReadAllBytes(fileToServe);
            string base64 = Convert.ToBase64String(fileBytes);
            string fileName = Uri.EscapeDataString(Path.GetFileName(fileToServe));

            string targetUrl = $"{baseUri.AbsoluteUri}#data={base64}&name={fileName}";

            OpenViewerInBrowser(new Uri(targetUrl));
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
