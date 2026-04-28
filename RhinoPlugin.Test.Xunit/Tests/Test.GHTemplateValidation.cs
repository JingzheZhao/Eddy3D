using EddyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Integration tests that validate Grasshopper template files from the GitHub repository.
    /// Uses XML parsing (no Grasshopper runtime required) to check for:
    /// - Component library references
    /// - Known vs unknown component GUIDs
    /// - File structure validity
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Category", "Templates")]
    public class GHTemplateValidationTests
    {
        private readonly ITestOutputHelper _output;

        // --- GitHub Repository Configuration ---
        private const string RepoOwner = "Eddy3D-Dev";
        private const string RepoName = "Eddy3D-OutdoorIndoorTemplates";
        private static readonly string RepoBranch = EddyVersion.ProductVersion;

        // --- Test Configuration ---
        /// <summary>
        /// Set to true to only test the first template (for quick debugging).
        /// </summary>
        private const bool TestOnlyFirstTemplate = false;

        private static string LocalTemplatesDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Eddy3D\Templates\GitHub_Test");

        // Known Grasshopper core component GUIDs that are always available
        private static readonly HashSet<string> KnownCoreComponentGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Params - Geometry
            "919e146f-30ae-4aae-be34-4d72f555e7da", // Brep
            "fbac3e32-f100-4292-8692-77240a42fd1a", // Point
            "8fa84b94-3ca0-4af8-b74f-8c20a492c54e", // Curve
            "d93c9462-c72b-4ef2-8f5c-b099a9a72b92", // Mesh
            "2e78987b-9dfb-42a2-8b76-3923ac8bd91a", // Surface
            "8b5bb3b5-7560-4b74-98f6-43a50b954a1e", // Plane
            "3e8ca6be-fda8-4aaf-b5c0-3c54c8bb7312", // Number
            "57da07bd-ecab-415d-9d86-af36d7073abc", // Number Slider
            "c552a431-af5b-46a9-a8a4-0fcbc27ef596", // Group
            "f31d8d7a-7536-4ac8-9c96-fde6ecda4d0a", // Cluster
            "59e0b89a-e487-49f8-bab8-b5bab16be14c", // Panel
            "15c37cd3-ab5c-4f62-a165-f4ab1ddf5b55", // Boolean
            "4c4e56eb-2f04-43f9-95a3-cc46a14f495a", // Text Panel
            "cb95db89-b166-41a6-a0dc-6c4a8749c6b4", // Geometry
            "d1a28e95-cf96-4936-bf34-8bf142d731bf", // Vector
            // Add more as needed
        };

        public GHTemplateValidationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Ensures the template repo has a branch matching EddyVersion.ProductVersion
        /// and that this branch is not empty (has at least one file/folder in root).
        /// </summary>
        [Fact]
        public async Task TemplateBranch_Exists_And_IsNotEmpty()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("eddy3d-template-test", "1.0"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                var branchUrl =
                    $"https://api.github.com/repos/{RepoOwner}/{RepoName}/branches/{Uri.EscapeDataString(RepoBranch)}";
                var branchResp = await client.GetAsync(branchUrl);

                Assert.True(
                    branchResp.IsSuccessStatusCode,
                    $"Branch '{RepoBranch}' does not exist in {RepoOwner}/{RepoName}. HTTP {(int)branchResp.StatusCode}.");

                var contentsUrl =
                    $"https://api.github.com/repos/{RepoOwner}/{RepoName}/contents?ref={Uri.EscapeDataString(RepoBranch)}";
                var contentsResp = await client.GetAsync(contentsUrl);

                Assert.True(
                    contentsResp.IsSuccessStatusCode,
                    $"Could not read branch root contents for '{RepoBranch}'. HTTP {(int)contentsResp.StatusCode}.");

                var body = await contentsResp.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(body))
                {
                    Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
                    Assert.True(
                        doc.RootElement.GetArrayLength() > 0,
                        $"Branch '{RepoBranch}' is empty in {RepoOwner}/{RepoName}.");
                }
            }
        }

        /// <summary>
        /// Result of validating a single GH/GHX template file.
        /// </summary>
        public class TemplateValidationResult
        {
            public string TemplatePath { get; set; }
            public bool IsValid { get; set; } = true;
            public int ComponentCount { get; set; }
            public List<string> ComponentNames { get; set; } = new List<string>();
            public List<string> UnknownComponents { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
        }

        /// <summary>
        /// Validates all Grasshopper template files from the GitHub repository using XML parsing.
        /// </summary>
        [Fact]
        public async Task ValidateAllGitHubTemplates()
        {
            // 1. Fetch the list of template files from GitHub
            var templateFiles = await FetchTemplateListAsync();
            Assert.NotEmpty(templateFiles);

            // Apply test configuration filter
            if (TestOnlyFirstTemplate)
            {
                templateFiles = templateFiles.Take(1).ToList();
                _output.WriteLine("*** TestOnlyFirstTemplate is enabled - testing only first template ***");
                _output.WriteLine("");
            }

            _output.WriteLine($"Found {templateFiles.Count} template files to validate:");
            foreach (var file in templateFiles)
            {
                _output.WriteLine($"  - {Path.GetFileName(file)}");
            }
            _output.WriteLine("");

            // 2. Validate each template using XML parsing
            var results = new List<TemplateValidationResult>();
            foreach (var templatePath in templateFiles)
            {
                var result = await ValidateTemplateXmlAsync(templatePath, LocalTemplatesDir);
                results.Add(result);

                var status = result.IsValid ? "✓" : "✗";
                var fileName = Path.GetFileName(result.TemplatePath);

                if (result.IsValid)
                {
                    _output.WriteLine($"{status} {fileName}: VALID ({result.ComponentCount} components)");
                    if (result.UnknownComponents.Count > 0)
                    {
                        _output.WriteLine($"    Note: {result.UnknownComponents.Count} plugin/unknown component(s)");
                    }
                }
                else
                {
                    _output.WriteLine($"{status} {fileName}: INVALID");
                    foreach (var err in result.Errors)
                    {
                        _output.WriteLine($"    Error: {err}");
                    }
                }
                foreach (var warn in result.Warnings)
                {
                    _output.WriteLine($"    Warning: {warn}");
                }
            }

            // 3. Report summary
            var passed = results.Count(r => r.IsValid);
            var failed = results.Count - passed;

            _output.WriteLine("");
            _output.WriteLine("=== VALIDATION SUMMARY ===");
            _output.WriteLine($"Valid: {passed}/{results.Count}");
            _output.WriteLine($"Invalid: {failed}/{results.Count}");

            // List all unknown components across all templates
            var allUnknown = results.SelectMany(r => r.UnknownComponents).Distinct().ToList();
            if (allUnknown.Count > 0)
            {
                _output.WriteLine("");
                _output.WriteLine($"Plugin/Unknown Components Found ({allUnknown.Count}):");
                foreach (var comp in allUnknown.OrderBy(c => c))
                {
                    _output.WriteLine($"  - {comp}");
                }
            }

            // 4. Assert based on results
            if (failed > 0)
            {
                _output.WriteLine("");
                _output.WriteLine("Invalid Templates:");
                foreach (var result in results)
                {
                    if (!result.IsValid)
                    {
                        _output.WriteLine($"\n  {Path.GetFileName(result.TemplatePath)}:");
                        foreach (var err in result.Errors)
                        {
                            _output.WriteLine($"    {err}");
                        }
                    }
                }
            }

            Assert.True(failed == 0, $"{failed} template(s) are invalid. See output for details.");
        }

        /// <summary>
        /// Validates a single template file by parsing its XML structure.
        /// </summary>
        private async Task<TemplateValidationResult> ValidateTemplateXmlAsync(string relativePath, string localDir)
        {
            var result = new TemplateValidationResult { TemplatePath = relativePath };

            try
            {
                // Download the template
                var localPath = await DownloadTemplateAsync(relativePath, localDir);

                if (!File.Exists(localPath))
                {
                    result.IsValid = false;
                    result.Errors.Add("Failed to download template file.");
                    return result;
                }

                // Determine if it's a binary .gh or XML .ghx file
                var extension = Path.GetExtension(localPath).ToLowerInvariant();

                if (extension == ".gh")
                {
                    // Binary GH files cannot be parsed as XML
                    result.Warnings.Add("Binary .gh file - cannot parse with XML validator. Consider converting to .ghx for validation.");
                    return result;
                }

                // Parse as XML
                XDocument doc;
                try
                {
                    doc = XDocument.Load(localPath);
                }
                catch (Exception parseEx)
                {
                    result.IsValid = false;
                    result.Errors.Add($"XML parse error: {parseEx.Message}");
                    return result;
                }

                // Check for Archive root element
                var root = doc.Root;
                if (root?.Name.LocalName != "Archive")
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid GHX file: Missing 'Archive' root element.");
                    return result;
                }

                // Find DefinitionObjects chunk
                var definitionObjects = root.Descendants("chunk")
                    .FirstOrDefault(c => c.Attribute("name")?.Value == "DefinitionObjects");

                if (definitionObjects == null)
                {
                    result.Warnings.Add("No DefinitionObjects chunk found - empty canvas?");
                    return result;
                }

                // Extract all component objects
                var objectChunks = definitionObjects.Elements("chunks")
                    .SelectMany(c => c.Elements("chunk"))
                    .Where(c => c.Attribute("name")?.Value == "Object");

                foreach (var objChunk in objectChunks)
                {
                    // Get GUID and Name from items
                    var items = objChunk.Element("items");
                    if (items == null) continue;

                    var guidItem = items.Elements("item")
                        .FirstOrDefault(i => i.Attribute("name")?.Value == "GUID");
                    var nameItem = items.Elements("item")
                        .FirstOrDefault(i => i.Attribute("name")?.Value == "Name");

                    var guid = guidItem?.Value ?? "unknown";
                    var name = nameItem?.Value ?? "Unknown Component";

                    result.ComponentCount++;
                    result.ComponentNames.Add($"{name} ({guid})");

                    // Check if this is a known core component
                    if (!KnownCoreComponentGuids.Contains(guid))
                    {
                        result.UnknownComponents.Add($"{name} ({guid})");
                    }
                }

                _output.WriteLine($"  Parsed {result.ComponentCount} components from {Path.GetFileName(localPath)}");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Unexpected error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Fetches the list of GH/GHX template files from the GitHub repository.
        /// </summary>
        private static async Task<List<string>> FetchTemplateListAsync()
        {
            using (var lister = new GitHubFileLister())
            {
                var files = await lister.ListFilesAsync(RepoOwner, RepoName, RepoBranch);
                return files
                    .Where(f => f.EndsWith(".gh", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".ghx", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f)
                    .ToList();
            }
        }

        /// <summary>
        /// Downloads a template file to the local cache.
        /// </summary>
        private static async Task<string> DownloadTemplateAsync(string relativePath, string localDir)
        {
            if (!Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }

            var localPath = Path.Combine(localDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var localSubDir = Path.GetDirectoryName(localPath);

            if (!Directory.Exists(localSubDir))
            {
                Directory.CreateDirectory(localSubDir);
            }

            var rawUrl = $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/{RepoBranch}/{relativePath}";

            using (var client = new WebClient())
            {
                client.Headers.Add("user-agent", "eddy3d-template-test");
                await client.DownloadFileTaskAsync(new Uri(rawUrl), localPath);
            }

            return localPath;
        }
    }
}
