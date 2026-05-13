using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

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
        private const int MaxViewerUrlLength = 30000;
        private const int MinResidualSampleLines = 50;
        private const int MaxDataLinesForCompression = 20000; // Cap to avoid massive strings before compression

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

            string payload = BuildResidualViewerPayload(fileToServe, baseUri, out bool isCompressed);
            string base64 = isCompressed ? CompressToBase64(payload) : Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            string fileName = Uri.EscapeDataString(Path.GetFileName(fileToServe));

            string dataKey = isCompressed ? "zdata" : "data";
            string targetUrl = $"{baseUri.AbsoluteUri}#{dataKey}={base64}&name={fileName}";

            OpenViewerInBrowser(new Uri(targetUrl));
        }

        private static string CompressToBase64(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (var mso = new MemoryStream())
            {
                using (var gs = new DeflateStream(mso, CompressionMode.Compress))
                {
                    gs.Write(bytes, 0, bytes.Length);
                }
                return Convert.ToBase64String(mso.ToArray());
            }
        }

        private static string BuildResidualViewerPayload(string filePath, Uri baseUri, out bool useCompression)
        {
            useCompression = true; // Default to compression
            string fileName = Uri.EscapeDataString(Path.GetFileName(filePath));
            int overhead = (baseUri?.AbsoluteUri?.Length ?? 0) + fileName.Length + "#zdata=&name=".Length;

            string[] allLines = Utilities.ReadLinesSafe(filePath).ToArray();

            // Try full file first with compression if it's not pathologically large
            if (allLines.Length < MaxDataLinesForCompression)
            {
                string fullPayload = string.Join("\n", allLines);
                string b64 = CompressToBase64(fullPayload);
                if (overhead + b64.Length <= MaxViewerUrlLength)
                {
                    return fullPayload;
                }
            }

            // If too big or already too many lines, fallback to sampling
            int payloadLimit = Math.Max(4096, ((MaxViewerUrlLength - overhead) * 3 / 4) - 1024);

            // Since we use compression, we can actually have a MUCH larger payload limit.
            // Compression ratio for residuals is often 10:1 or better.
            // Let's assume 5:1 safely for the sampling target.
            int compressedPayloadLimit = payloadLimit * 5;

            string payload = ReadResidualPayload(filePath, compressedPayloadLimit);

            // Final check and possible double-sampling if even the compressed version is too long
            while (payload.Length > 4096)
            {
                string base64 = CompressToBase64(payload);
                int urlLength = overhead + base64.Length;
                if (urlLength <= MaxViewerUrlLength)
                {
                    return payload;
                }

                compressedPayloadLimit = Math.Max(4096, compressedPayloadLimit * 3 / 4);
                payload = ReadResidualPayload(filePath, compressedPayloadLimit);
            }

            return payload;
        }

        private static string ReadResidualPayload(string filePath, int maxChars)
        {
            string[] lines = Utilities.ReadLinesSafe(filePath).ToArray();
            string fullText = string.Join("\n", lines);
            if (fullText.Length <= maxChars)
            {
                return fullText;
            }

            var headerLines = lines
                .TakeWhile(line => string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                .ToList();
            var dataLines = lines.Skip(headerLines.Count).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

            if (dataLines.Count <= MinResidualSampleLines)
            {
                return fullText.Length <= maxChars ? fullText : fullText.Substring(0, maxChars);
            }

            string headerText = string.Join("\n", headerLines);
            int availableChars = Math.Max(1024, maxChars - headerText.Length - 2);
            double averageLineLength = Math.Max(1.0, dataLines.Average(line => line.Length + 1));
            int maxDataLines = Math.Max(MinResidualSampleLines, (int)(availableChars / averageLineLength));
            maxDataLines = Math.Min(maxDataLines, dataLines.Count);

            var sampled = new List<string>(headerLines);
            if (maxDataLines <= 1)
            {
                sampled.Add(dataLines[dataLines.Count - 1]);
            }
            else
            {
                double step = (dataLines.Count - 1) / (double)(maxDataLines - 1);
                int previousIndex = -1;
                for (int i = 0; i < maxDataLines; i++)
                {
                    int index = (int)Math.Round(i * step);
                    index = Math.Min(dataLines.Count - 1, Math.Max(0, index));
                    if (index != previousIndex)
                    {
                        sampled.Add(dataLines[index]);
                        previousIndex = index;
                    }
                }
            }

            return string.Join("\n", sampled);
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
                string caseDir = Path.Combine(result.WorkingDirectory, dir.ToString());
                candidates.Add(EddyLib.Strings.PlotResiduals.FindResidualsFolder(caseDir));
                candidates.Add(Path.Combine(result.WorkingDirectory, dir.ToString(), "postProcessing", "residuals"));
                candidates.Add(Path.Combine(result.WorkingDirectory, dir.ToString(), "postProcessing"));
            }

            candidates.Add(EddyLib.Strings.PlotResiduals.FindResidualsFolder(result.WorkingDirectory));
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
