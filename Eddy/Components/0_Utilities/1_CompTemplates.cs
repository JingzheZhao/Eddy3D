using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Newtonsoft.Json;

namespace Eddy
{
    public class SelectTemplate_Component : GH_Component
    {
        // --- ADD YOUR URLS HERE ---
        private static string RepoOwner = "Eddy3D-Dev";
        private static string RepoName = "Eddy3D-OutdoorIndoorTemplates";
        private static string RepoBranch = EddyVersion.ProductVersion;
        // --------------------------

        private TemplateCache cache = new TemplateCache();
        private bool isFetching = false;
        private bool isCheckingForUpdate = false;
        private bool updateAvailable = false;
        private string errorMessage = null;

        private sealed class TemplateCache
        {
            public List<string> Files { get; set; } = new List<string>();
            public string LastSyncedSha { get; set; }
        }

        public SelectTemplate_Component()
              : base("Select Template", "Select", 
@"Load example Grasshopper definitions for common workflows.

Templates include wind comfort studies, MRT analysis, and 
indoor airflow simulations.

" + EddyVersion.toString(),
              EddyVersion.Name, "0 | Utilities")
        {
        }

        public override Guid ComponentGuid => new Guid("{D8E619A8-BF03-422F-962B-0D52A05559DF}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Additional Folders", "Dirs", 
                "Optional: Additional folder paths to search for .ghx templates.", 
                GH_ParamAccess.list);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Template Paths", "Paths", "Full paths to discovered template files (.ghx)", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (cache.Files.Count == 0 && !isFetching && errorMessage == null)
            {
                LoadTemplateCache();
            }

            if (cache.Files.Count > 0 && !isFetching && !isCheckingForUpdate && !updateAvailable)
            {
                CheckForUpdatesAsync();
            }

            if (updateAvailable)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Template update available. Right-click to sync.");
            }
            else if (cache.Files.Count == 0 && errorMessage == null && !isFetching)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Right-click to sync templates from GitHub.");
            }

            if (errorMessage != null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, errorMessage);
            }

            var localTemplatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub");
            var displayedFiles = new List<string>();

            foreach (var file in cache.Files)
            {
                displayedFiles.Add(Path.Combine(localTemplatesDir, file));
            }

            DA.SetDataList(0, displayedFiles);
        }

        private void LoadTemplateCache()
        {
            try
            {
                var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub", "template_list.json");
                if (File.Exists(cachePath))
                {
                    var json = File.ReadAllText(cachePath);
                    cache = JsonConvert.DeserializeObject<TemplateCache>(json) ?? new TemplateCache();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to load local template cache: " + ex.Message;
            }
        }

        private async void CheckForUpdatesAsync()
        {
            if (string.IsNullOrEmpty(cache.LastSyncedSha)) return;

            isCheckingForUpdate = true;
            try
            {
                using (var lister = new GitHubFileLister())
                {
                    var latestSha = await lister.GetLatestCommitShaAsync(RepoOwner, RepoName, RepoBranch);
                    if (latestSha != cache.LastSyncedSha)
                    {
                        updateAvailable = true;
                        this.ExpireSolution(true);
                    }
                }
            }
            catch
            {
                // Silently fail for background check
            }
            finally
            {
                isCheckingForUpdate = false;
            }
        }

        private async void FetchGithubFilesAsync()
        {
            isFetching = true;
            errorMessage = null;
            updateAvailable = false;
            try
            {
                using (var lister = new GitHubFileLister())
                {
                    var latestSha = await lister.GetLatestCommitShaAsync(RepoOwner, RepoName, RepoBranch);
                    var files = await lister.ListFilesAsync(RepoOwner, RepoName, RepoBranch);
                    
                    cache.Files = files.Where(f => f.EndsWith(".ghx", StringComparison.OrdinalIgnoreCase)).ToList();
                    cache.LastSyncedSha = latestSha;

                    // Update Cache Disk
                    var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub");
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    var cachePath = Path.Combine(targetDir, "template_list.json");
                    File.WriteAllText(cachePath, JsonConvert.SerializeObject(cache));
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to sync GitHub templates: " + ex.Message;
            }
            finally
            {
                isFetching = false;
                this.ExpireSolution(true);
            }
        }

        private Size GetMoveVector(PointF FromLocation)
        {
            var moveX = this.Attributes.Bounds.Left - 80 - FromLocation.X;
            var moveY = this.Attributes.Bounds.Y + 180 - FromLocation.Y;
            var loc = new Point(Convert.ToInt32(moveX), Convert.ToInt32(moveY));

            return new Size(loc);
        }

        private void CreateTemplateFromXMLString(string FilePath, ref bool Run)
        {
            var canvasCurrent = Grasshopper.Instances.ActiveCanvas;
            var f = canvasCurrent.Focused;
            var isFileExist = File.Exists(FilePath);

            if (Run && f && isFileExist)
            {
                var io = new GH_DocumentIO();

                var success = io.Open(FilePath);

                if (!success)
                {
                    MessageBox.Show("Failed to add template.");
                    return;
                }
                var docTemp = io.Document;

                docTemp.SelectAll();
                docTemp.MutateAllIds();

                //move to where this component is...
                var box = docTemp.BoundingBox(false);
                var vec = GetMoveVector(box.Location);
                docTemp.TranslateObjects(vec, true);

                docTemp.ExpireSolution();

                var docCurrent = canvasCurrent.Document;
                docCurrent.DeselectAll();
                docCurrent.MergeDocument(docTemp);
            }
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            menu.Items.Clear();

            if (isFetching)
            {
                menu.Items.Add("Fetching from GitHub...").Enabled = false;
            }
            else if (cache.Files.Count == 0)
            {
                menu.Items.Add("No templates found on GitHub").Enabled = false;
                Menu_AppendItem(menu, "Retry Fetch", (sender, e) => { FetchGithubFilesAsync(); });
            }
            else
            {
                if (updateAvailable)
                {
                    var updateItem = new ToolStripMenuItem("Update Available! Click to Sync", null, (sender, e) => { FetchGithubFilesAsync(); });
                    updateItem.BackColor = Color.Gold;
                    menu.Items.Add(updateItem);
                    menu.Items.Add(new ToolStripSeparator());
                }

                foreach (var file in cache.Files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub", file);
                    var isCached = File.Exists(localPath);
                    var label = isCached ? fileName : fileName + " (Download)";

                    EventHandler ev = async (sender, e) =>
                    {
                        var success = await EnsureTemplateDownloadedAsync(file);
                        if (success)
                        {
                            var r = true;
                            CreateTemplateFromXMLString(localPath, ref r);
                            this.ExpireSolution(true);
                        }
                    };

                    menu.Items.Add(new ToolStripMenuItem(label, null, ev));
                }
            }

            menu.Items.Add(new ToolStripSeparator());
            Menu_AppendItem(menu, "Force Refresh List", (sender, e) => { FetchGithubFilesAsync(); });
        }

        private async Task<bool> EnsureTemplateDownloadedAsync(string relPath)
        {
            var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub");
            var localPath = Path.Combine(targetDir, relPath);

            if (File.Exists(localPath)) return true;

            try
            {
                var localSubDir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(localSubDir)) Directory.CreateDirectory(localSubDir);

                var rawUrl = $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/{RepoBranch}/{relPath}";
                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "eddy3d-client");
                    await client.DownloadFileTaskAsync(new Uri(rawUrl), localPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to download template: " + ex.Message);
                return false;
            }
        }

        private void SyncTemplatesAsync()
        {
        }

        private ToolStripMenuItem addFromFolder(string rootFolder, List<string> filesPerFolder)
        {
            return null;
        }

        public override void CreateAttributes()
        {
            var att = new EddyLib.UI.Eddy_ComponentButtonAttributes(this);
            this.Attributes = att;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_templates;
            }
        }
    }
}