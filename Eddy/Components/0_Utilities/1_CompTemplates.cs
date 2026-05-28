using Eddy.Properties;
using EddyLib;
using Grasshopper.GUI.Canvas;
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
              : base(EddyLib.GH_Strings.Templates.Name, EddyLib.GH_Strings.Templates.Nick,
              EddyLib.GH_Strings.Templates.Desc + "\n\n" + EddyVersion.toString(),
              EddyVersion.Name, "0 | Utilities")
        {
        }

        public override Guid ComponentGuid => new Guid("{D8E619A8-BF03-422F-962B-0D52A05559DF}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                EddyLib.GH_Strings.Templates.InputName,
                EddyLib.GH_Strings.Templates.InputNick,
                EddyLib.GH_Strings.Templates.InputDesc,
                GH_ParamAccess.list);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(
                EddyLib.GH_Strings.Templates.OutputName,
                EddyLib.GH_Strings.Templates.OutputNick,
                EddyLib.GH_Strings.Templates.OutputDesc,
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var additionalInputs = new List<string>();
            DA.GetDataList(0, additionalInputs);

            // 1. Manage Main Repo Cache
            if (cache.Files.Count == 0 && !isFetching && errorMessage == null)
            {
                LoadTemplateCache();
                if (cache.Files.Count == 0 && !isFetching) FetchGithubFilesAsync();
            }

            // 2. Check Updates
            if (cache.Files.Count > 0 && !isFetching && !isCheckingForUpdate && !updateAvailable)
            {
                CheckForUpdatesAsync();
            }

            // Output Messages
            if (Attributes is EddyLib.UI.Eddy_ComponentButtonAttributes buttonAttributes)
            {
                if (updateAvailable)
                {
                    buttonAttributes.ButtonText = "Update Available!";
                    buttonAttributes.ButtonPalette = GH_Palette.Warning;
                }
                else
                {
                    buttonAttributes.ButtonText = "Select Template";
                    buttonAttributes.ButtonPalette = GH_Palette.Black;
                }
            }

            if (updateAvailable) AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Template update available. Right-click to sync.");
            else if (cache.Files.Count == 0 && errorMessage == null && !isFetching) AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Syncing templates from GitHub...");

            if (errorMessage != null) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, errorMessage);

            // 3. Collect All Files
            var allFiles = new List<string>();

            // Add Main Repo Files
            var localTemplatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub");
            foreach (var file in cache.Files)
            {
                allFiles.Add(Path.Combine(localTemplatesDir, file));
            }

            // Process Additional Inputs (Local Folders or GitHub URLs)
            foreach (var input in additionalInputs)
            {
                if (string.IsNullOrWhiteSpace(input)) continue;

                if (IsGitHubUrl(input, out var ghInfo))
                {
                    // It's a GitHub URL
                    var externalDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\External", ghInfo.Owner, ghInfo.Repo, ghInfo.Branch ?? "HEAD");

                    if (!Directory.Exists(externalDir) || Directory.GetFiles(externalDir, "*.gh*", SearchOption.AllDirectories).Length == 0)
                    {
                        if (!externalFetchStates.ContainsKey(input) || !externalFetchStates[input])
                        {
                            FetchExternalGithubFilesAsync(input, ghInfo);
                        }
                    }

                    if (Directory.Exists(externalDir))
                    {
                        // Filter files based on path if provided in URL (e.g. /tree/main/SubDir)
                        var files = Directory.GetFiles(externalDir, "*.gh*", SearchOption.AllDirectories);
                        foreach (var f in files)
                        {
                            // If URL has a subpath, filter by it
                            if (!string.IsNullOrEmpty(ghInfo.Path) && !f.Replace("\\", "/").Contains(ghInfo.Path)) continue;
                            allFiles.Add(f);
                        }
                    }
                }
                else if (Directory.Exists(input))
                {
                    try
                    {
                        var files = Directory.GetFiles(input, "*.gh*", SearchOption.AllDirectories)
                                             .Where(f => f.EndsWith(".gh", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".ghx", StringComparison.OrdinalIgnoreCase));
                        allFiles.AddRange(files);
                    }
                    catch { /* Ignore access errors */ }
                }
            }

            DA.SetDataList(0, allFiles);
        }

        // --- Helper Structures & Methods ---

        private Dictionary<string, bool> externalFetchStates = new Dictionary<string, bool>();

        private struct GitHubInfo
        {
            public string Owner;
            public string Repo;
            public string Branch;
            public string Path;
        }

        private bool IsGitHubUrl(string url, out GitHubInfo info)
        {
            info = new GitHubInfo();
            if (string.IsNullOrEmpty(url) || !url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
                return false;

            var parts = url.Substring("https://github.com/".Length).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            info.Owner = parts[0];
            info.Repo = parts[1];

            if (parts.Length >= 4 && parts[2] == "tree")
            {
                info.Branch = parts[3];
                if (parts.Length > 4)
                {
                    info.Path = string.Join("/", parts.Skip(4));
                }
            }
            else
            {
                info.Branch = "HEAD";
            }

            return true;
        }

        private async void FetchExternalGithubFilesAsync(string inputUrl, GitHubInfo info)
        {
            if (externalFetchStates.ContainsKey(inputUrl) && externalFetchStates[inputUrl]) return;
            externalFetchStates[inputUrl] = true;

            this.Message = "Downloading...";
            if (Attributes is EddyLib.UI.Eddy_ComponentButtonAttributes buttonAttributes)
            {
                buttonAttributes.ButtonText = "Downloading...";
                buttonAttributes.ButtonPalette = GH_Palette.Blue;
            }
            Grasshopper.Instances.ActiveCanvas?.Refresh();

            try
            {
                using (var lister = new GitHubFileLister())
                {
                    var files = await lister.ListFilesAsync(info.Owner, info.Repo, info.Branch);
                    var validFiles = files.Where(f => f.EndsWith(".ghx", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".gh", StringComparison.OrdinalIgnoreCase));

                    var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\External", info.Owner, info.Repo, info.Branch);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    foreach (var relPath in validFiles)
                    {
                        if (!string.IsNullOrEmpty(info.Path) && !relPath.Replace("\\", "/").StartsWith(info.Path)) continue;

                        // ✅ GOOD: Validate relative path to prevent path traversal.
                        Utilities.ValidateRelativePath(relPath);

                        var localPath = Path.Combine(targetDir, relPath);
                        var localSub = Path.GetDirectoryName(localPath);
                        if (!Directory.Exists(localSub)) Directory.CreateDirectory(localSub);

                        var rawUrl = $"https://raw.githubusercontent.com/{info.Owner}/{info.Repo}/{info.Branch}/{relPath}";
                        using (var client = new System.Net.Http.HttpClient())
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd("Eddy3D");
                            var data = await client.GetByteArrayAsync(rawUrl);
                            File.WriteAllBytes(localPath, data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to fetch external templates from {inputUrl}: {ex.Message}");
            }
            finally
            {
                externalFetchStates[inputUrl] = false;
                this.Message = null;
                if (Attributes is EddyLib.UI.Eddy_ComponentButtonAttributes bAtt)
                {
                    bAtt.ButtonText = "Select Template";
                    bAtt.ButtonPalette = GH_Palette.Black;
                }
                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { this.ExpireSolution(true); });
            }
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
            if (Attributes is EddyLib.UI.Eddy_ComponentButtonAttributes buttonAttributes)
            {
                buttonAttributes.ButtonText = "Syncing...";
                buttonAttributes.ButtonPalette = GH_Palette.Blue;
            }
            try
            {
                using (var lister = new GitHubFileLister())
                {
                    var latestSha = await lister.GetLatestCommitShaAsync(RepoOwner, RepoName, RepoBranch);
                    var files = await lister.ListFilesAsync(RepoOwner, RepoName, RepoBranch);

                    cache.Files = files.Where(f => f.EndsWith(".ghx", StringComparison.OrdinalIgnoreCase)).ToList();
                    cache.LastSyncedSha = latestSha;

                    // Clear old cached files to avoid stale templates
                    var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub");
                    if (Directory.Exists(targetDir))
                    {
                        foreach (var file in Directory.GetFiles(targetDir, "*.ghx", SearchOption.AllDirectories))
                        {
                            try { File.Delete(file); } catch { /* ignore deletion errors */ }
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // Update Cache Disk
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
                if (Attributes is EddyLib.UI.Eddy_ComponentButtonAttributes bAtt)
                {
                    bAtt.ButtonText = "Select Template";
                    bAtt.ButtonPalette = GH_Palette.Black;
                }
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
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to add template.");
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

                // Build nested menu structure from folder paths
                var folderMenus = new Dictionary<string, ToolStripMenuItem>(StringComparer.OrdinalIgnoreCase);
                var rootFiles = new List<string>();

                foreach (var file in cache.Files)
                {
                    var dirName = Path.GetDirectoryName(file)?.Replace("\\", "/");
                    if (string.IsNullOrEmpty(dirName))
                    {
                        // Root-level file
                        rootFiles.Add(file);
                    }
                    else
                    {
                        // File inside a folder
                        if (!folderMenus.ContainsKey(dirName))
                        {
                            folderMenus[dirName] = new ToolStripMenuItem(dirName);
                        }
                        folderMenus[dirName].DropDownItems.Add(CreateTemplateMenuItem(file));
                    }
                }

                // Add folder submenus first
                foreach (var kvp in folderMenus.OrderBy(k => k.Key))
                {
                    menu.Items.Add(kvp.Value);
                }

                // Add separator if we have both folders and root files
                if (folderMenus.Count > 0 && rootFiles.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }

                // Add root-level files
                foreach (var file in rootFiles)
                {
                    menu.Items.Add(CreateTemplateMenuItem(file));
                }
            }

            menu.Items.Add(new ToolStripSeparator());
            Menu_AppendItem(menu, "Force Refresh List", (sender, e) => { FetchGithubFilesAsync(); });
        }

        private ToolStripMenuItem CreateTemplateMenuItem(string file)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Eddy3D\Templates\GitHub", file);
            var isCached = File.Exists(localPath);
            var label = isCached ? fileName : fileName + " (Click to Download)";

            EventHandler ev = async (sender, e) =>
            {
                this.Message = "Downloading...";
                if (Attributes is EddyLib.UI.Eddy_ComponentButtonAttributes buttonAttributes)
                {
                    buttonAttributes.ButtonText = "Downloading...";
                    buttonAttributes.ButtonPalette = GH_Palette.Blue;
                }
                Grasshopper.Instances.ActiveCanvas?.Refresh();

                var success = await EnsureTemplateDownloadedAsync(file);
                if (success)
                {
                    this.Message = "Loaded!";
                    var r = true;
                    CreateTemplateFromXMLString(localPath, ref r);
                    this.ExpireSolution(true);
                }
                else
                {
                    this.Message = "Failed";
                    Grasshopper.Instances.ActiveCanvas?.Refresh();
                }

                await Task.Delay(2000);
                this.Message = null;
                if (Attributes is EddyLib.UI.Eddy_ComponentButtonAttributes buttonAttributes2)
                {
                    buttonAttributes2.ButtonText = "Select Template";
                    buttonAttributes2.ButtonPalette = GH_Palette.Black;
                }
                Grasshopper.Instances.ActiveCanvas?.Refresh();
            };

            return new ToolStripMenuItem(label, null, ev);
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
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to download template: " + ex.Message);
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