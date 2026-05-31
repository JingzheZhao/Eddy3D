using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.Geometry;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Eddy.Properties;

namespace Eddy
{
    /// <summary>
    /// Downloads an ONNX wind-prediction model from the SustainableUrbanSystemsLab
    /// HuggingFace repository.  Supports model selection (Yel, Esen, Poyraz) via a
    /// dropdown input.  Public models (Yel) can be fetched without a token; private
    /// models (Esen, Poyraz) require a valid HuggingFace token.
    /// </summary>
    public class MLModelCMP : GH_Component
    {
        // ──────────────────────────────────────────────
        // Model catalogue
        // ──────────────────────────────────────────────
        private enum ModelId { Yel = 0, Esen = 1, Poyraz = 2 }

        private static readonly string[] ModelNames = { "Yel", "Esen", "Poyraz" };

        private static readonly string[] RepoIds =
        {
            "SustainableUrbanSystemsLab/Yel",
            "SustainableUrbanSystemsLab/Esen-1.0",
            "SustainableUrbanSystemsLab/Poyraz-1.0"
        };

        private static readonly string[] FileNames =
        {
            "Yel.onnx",
            "Esen1-0.onnx",
            "Poyraz1-0.onnx"
        };

        /// <summary>
        /// Whether the model repo is public (no token needed to download).
        /// </summary>
        private static readonly bool[] IsPublic = { true, false, false };

        /// <summary>
        /// Minimum expected file sizes for integrity checks (bytes).
        /// Set conservatively — any file smaller than this is treated as corrupt.
        /// Yel: ~50 MB (placeholder, will accept ≥ 5 MB)
        /// Esen: ~178 MB
        /// Poyraz: ~50 MB (placeholder, will accept ≥ 5 MB)
        /// </summary>
        private static readonly long[] ExpectedMinBytes = { 5_000_000, 180_000_000, 5_000_000 };

        /// <summary>
        /// Approximate sizes used for progress reporting (bytes).
        /// </summary>
        private static readonly long[] ExpectedBytes = { 50_000_000, 186_700_128, 50_000_000 };

        // ──────────────────────────────────────────────
        // Constructor / metadata
        // ──────────────────────────────────────────────
        public MLModelCMP()
            : base("ML Model", "MLModel",
                "Download an ONNX wind-prediction model from HuggingFace.\n" +
                "Select a model (Yel, Esen, Poyraz) and provide a HuggingFace token for private models.\n" +
                "The model is cached locally in ~/SUS_LAB/ and reused on subsequent runs.",
                "Eddy3D", "4 | ML")
        {
        }

        protected override Bitmap Icon => Resources.Eddy_ml_model;
        public override Guid ComponentGuid => new Guid("{A1B2C3D4-E5F6-7890-AB12-CD34EF567890}");

        // ──────────────────────────────────────────────
        // INPUTS
        // ──────────────────────────────────────────────
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "Run",
                "Set to True to validate the token and download the model if needed.",
                GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Model", "Model",
                "Select the ONNX model to download.",
                GH_ParamAccess.item, 0);
            pManager.AddTextParameter("HF Token", "HF_Token",
                "HuggingFace access token (starts with hf_) or a file path to a .txt file " +
                "containing the token. Required for private models (Esen, Poyraz). " +
                "Not required for public models (Yel).",
                GH_ParamAccess.item);

            // Populate the Model dropdown (right-click fallback)
            var modelParam = pManager[1] as Param_Integer;
            for (int i = 0; i < ModelNames.Length; i++)
                modelParam.AddNamedValue(ModelNames[i], i);

            pManager[2].Optional = true; // HF_Token is optional for public models
        }

        // ──────────────────────────────────────────────
        // Inline Dropdown UI
        // ──────────────────────────────────────────────
        public override void CreateAttributes()
        {
            m_attributes = new DropdownComponentAttributes(this, new DropdownComponentAttributes.DropdownDef[]
            {
                new DropdownComponentAttributes.DropdownDef(1, ModelNames, 0)
            });
        }

        // ──────────────────────────────────────────────
        // OUTPUTS
        // ──────────────────────────────────────────────
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status",
                "Human-readable status / diagnostic message.",
                GH_ParamAccess.item);
            pManager.AddTextParameter("File Path", "FilePath",
                "Full path to the cached ONNX model file, or null on failure.",
                GH_ParamAccess.item);
            pManager.AddNumberParameter("Progress", "Progress",
                "Download progress percentage (0–100).",
                GH_ParamAccess.item);
        }

        // ──────────────────────────────────────────────
        // SolveInstance
        // ──────────────────────────────────────────────
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            int modelIndex = 0;
            string hfTokenInput = null;

            DA.GetData(0, ref run);
            DA.GetData(1, ref modelIndex);
            DA.GetData(2, ref hfTokenInput);

            // Clamp model index
            if (modelIndex < 0 || modelIndex >= ModelNames.Length)
                modelIndex = 0;

            string modelName = ModelNames[modelIndex];
            string repoId = RepoIds[modelIndex];
            string modelFileName = FileNames[modelIndex];
            bool isPublic = IsPublic[modelIndex];
            long expectedMin = ExpectedMinBytes[modelIndex];
            long expectedTotal = ExpectedBytes[modelIndex];

            string modelUrl = $"https://huggingface.co/{repoId}/resolve/main/{modelFileName}";
            string tokenCheckUrl = $"https://huggingface.co/api/models/{repoId}";

            DA.SetData(2, 0.0); // Progress

            try
            {
                // ──────────────────────────────────────
                // 0. Locate cache folder
                // ──────────────────────────────────────
                string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrWhiteSpace(userFolder) || !Directory.Exists(userFolder))
                {
                    SetOutputs(DA, "Could not locate the user home folder.", null, 0.0);
                    Message = "Error";
                    return;
                }

                string modelFolder = Path.Combine(userFolder, "SUS_LAB");
                if (!Directory.Exists(modelFolder))
                    Directory.CreateDirectory(modelFolder);

                string destinationPath = Path.Combine(modelFolder, modelFileName);
                string tempPath = destinationPath + ".download";
                string tokenCheckTemp = Path.Combine(modelFolder, "token_check.json");

                if (!run)
                {
                    SetOutputs(DA, $"Set Run to True to download {modelName}.", null, 0.0);
                    Message = $"{modelName} | Idle";
                    return;
                }

                // ──────────────────────────────────────
                // 1. Resolve HF token
                // ──────────────────────────────────────
                string hfToken = ResolveToken(hfTokenInput);
                bool hasToken = !string.IsNullOrEmpty(hfToken);

                if (!isPublic && !hasToken)
                {
                    // Private model requires a token — delete cache and abort
                    DeleteCachedFiles(destinationPath, tempPath, out bool deletedCache);
                    SetOutputs(DA,
                        $"Access denied.\n\n" +
                        $"{modelName} is a private model and requires a HuggingFace token.\n" +
                        "Provide a valid HF_Token (starts with hf_) or a file path to a .txt file containing one.\n\n" +
                        (deletedCache ? "Cached model was deleted because no valid token was provided."
                                      : "No cached model was found to delete."),
                        null, 0.0);
                    Message = "No Token";
                    return;
                }

                if (!isPublic && !hfToken.StartsWith("hf_"))
                {
                    DeleteCachedFiles(destinationPath, tempPath, out bool deletedCache);
                    SetOutputs(DA,
                        "Access denied.\n\n" +
                        "HF_Token is not valid.\n" +
                        "It must be either:\n" +
                        "1. A Hugging Face token starting with hf_\n" +
                        "or\n" +
                        "2. A valid filepath to a text file containing a token starting with hf_.\n\n" +
                        (deletedCache ? "Cached model was deleted because the token format is invalid."
                                      : "No cached model was found to delete."),
                        null, 0.0);
                    Message = "Bad Token";
                    return;
                }

                // ──────────────────────────────────────
                // 2. Determine curl path
                // ──────────────────────────────────────
                string curlPath = GetCurlPath();
                if (curlPath == null)
                {
                    SetOutputs(DA, "Unsupported operating system. This component currently supports Windows and macOS.", null, 0.0);
                    Message = "Error";
                    return;
                }

                // ──────────────────────────────────────
                // 3. Validate token (private models only)
                // ──────────────────────────────────────
                if (!isPublic && hasToken)
                {
                    Message = $"{modelName} | Checking Token";

                    string validateResult = ValidateToken(curlPath, hfToken, tokenCheckUrl, tokenCheckTemp);
                    if (validateResult != null)
                    {
                        // Token check failed — delete cache for auth failures
                        if (validateResult.StartsWith("AUTH_FAIL"))
                        {
                            DeleteCachedFiles(destinationPath, tempPath, out bool deletedCache);
                            string reason = validateResult.Substring("AUTH_FAIL:".Length);
                            SetOutputs(DA,
                                $"Access denied.\n\n" +
                                $"HF_Token is wrong, expired, or does not have Read access to:\n{repoId}\n\n" +
                                $"HTTP code: {reason}\n\n" +
                                (deletedCache ? "Cached model was deleted because the token is invalid."
                                              : "No cached model was found to delete."),
                                null, 0.0);
                            Message = "Denied";
                        }
                        else
                        {
                            // Network error — do NOT delete cache
                            SetOutputs(DA, validateResult, null, 0.0);
                            Message = "Net Error";
                        }
                        return;
                    }
                }

                // ──────────────────────────────────────
                // 4. Check cache
                // ──────────────────────────────────────
                if (File.Exists(destinationPath))
                {
                    var existingFile = new FileInfo(destinationPath);
                    if (existingFile.Length >= expectedMin)
                    {
                        SetOutputs(DA,
                            $"Using cached {modelName} model.\n" +
                            (hasToken ? "Valid HF_Token confirmed.\n" : "") +
                            $"\nPath: {destinationPath}\n" +
                            $"Size: {Math.Round(existingFile.Length / 1024.0 / 1024.0, 2)} MB",
                            destinationPath, 100.0);
                        Message = $"{modelName} | Cached";
                        return;
                    }
                    else
                    {
                        File.Delete(destinationPath);
                    }
                }

                // ──────────────────────────────────────
                // 5. Download
                // ──────────────────────────────────────
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                Message = $"{modelName} | Downloading";

                var psi = new ProcessStartInfo
                {
                    FileName = curlPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                psi.ArgumentList.Add("-L");
                psi.ArgumentList.Add("--fail");
                psi.ArgumentList.Add("--show-error");
                psi.ArgumentList.Add("--http1.1");
                psi.ArgumentList.Add("--tlsv1.2");
                psi.ArgumentList.Add("--retry");
                psi.ArgumentList.Add("3");
                psi.ArgumentList.Add("--retry-delay");
                psi.ArgumentList.Add("2");
                psi.ArgumentList.Add("--connect-timeout");
                psi.ArgumentList.Add("30");
                psi.ArgumentList.Add("--max-time");
                psi.ArgumentList.Add("0");
                psi.ArgumentList.Add("-A");
                psi.ArgumentList.Add("Grasshopper-SUSLAB-Model-Downloader");

                if (hasToken)
                {
                    psi.ArgumentList.Add("-H");
                    psi.ArgumentList.Add($"Authorization: Bearer {hfToken}");
                }

                psi.ArgumentList.Add(modelUrl);
                psi.ArgumentList.Add("-o");
                psi.ArgumentList.Add(tempPath);

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();

                    while (!process.HasExited)
                    {
                        if (File.Exists(tempPath))
                        {
                            var partialFile = new FileInfo(tempPath);
                            double downloadedMB = partialFile.Length / 1024.0 / 1024.0;
                            double progress = Math.Min(99.0, partialFile.Length * 100.0 / expectedTotal);

                            DA.SetData(0, $"Downloading {modelName}...\nDownloaded so far: {Math.Round(downloadedMB, 2)} MB");
                            DA.SetData(2, progress);
                            Message = $"{modelName} | Downloading";
                        }

                        RhinoApp.Wait();
                        System.Threading.Thread.Sleep(300);
                    }

                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode != 0)
                    {
                        SetOutputs(DA,
                            $"Download of {modelName} failed.\n" +
                            $"Exit code: {process.ExitCode}\n" +
                            $"Error: {error}",
                            null, 0.0);
                        Message = "Error";
                        return;
                    }
                }

                // ──────────────────────────────────────
                // 6. Validate downloaded file
                // ──────────────────────────────────────
                string validationError = ValidateDownload(tempPath, expectedMin, modelName);
                if (validationError != null)
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                    SetOutputs(DA, validationError, null, 0.0);
                    Message = "Error";
                    return;
                }

                // ──────────────────────────────────────
                // 7. Move to final location
                // ──────────────────────────────────────
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                File.Move(tempPath, destinationPath);

                var finalFile = new FileInfo(destinationPath);
                SetOutputs(DA,
                    $"{modelName} model downloaded successfully.\n\n" +
                    $"Path: {destinationPath}\n" +
                    $"Size: {Math.Round(finalFile.Length / 1024.0 / 1024.0, 2)} MB",
                    destinationPath, 100.0);
                Message = $"{modelName} | Done";
            }
            catch (Exception ex)
            {
                SetOutputs(DA, $"Operation failed.\nError: {ex.Message}", null, 0.0);
                Message = "Error";
            }
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static void SetOutputs(IGH_DataAccess DA, string status, string filePath, double progress)
        {
            DA.SetData(0, status);
            DA.SetData(1, filePath);
            DA.SetData(2, progress);
        }

        /// <summary>
        /// Resolve the HF_Token input: can be a raw token string or a path to a .txt file.
        /// Returns null/empty if no valid token was found.
        /// </summary>
        private static string ResolveToken(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string trimmed = input.Trim().Trim('"');

            string token = trimmed;
            if (File.Exists(trimmed))
            {
                token = File.ReadAllText(trimmed).Trim();
            }

            if (string.IsNullOrWhiteSpace(token))
                return null;

            // ✅ GOOD: Reject tokens containing CRLF or tabs to prevent HTTP header injection
            // when the token is passed to curl via 'Authorization: Bearer'
            if (token.IndexOfAny(new[] { '\r', '\n', '\t' }) != -1)
            {
                throw new ArgumentException("HuggingFace token contains invalid characters (newline or tab).");
            }

            return token;
        }

        private static string GetCurlPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "curl.exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "/usr/bin/curl";
            return null;
        }

        /// <summary>
        /// Validate HF token against the API endpoint. Returns null on success,
        /// or an error string on failure.  Strings prefixed with "AUTH_FAIL:" indicate
        /// confirmed authentication failure (caller should delete cache).
        /// </summary>
        private static string ValidateToken(string curlPath, string token, string apiUrl, string tempFile)
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);

            var psi = new ProcessStartInfo
            {
                FileName = curlPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            psi.ArgumentList.Add("-sS");
            psi.ArgumentList.Add("-L");
            psi.ArgumentList.Add("--http1.1");
            psi.ArgumentList.Add("--tlsv1.2");
            psi.ArgumentList.Add("--retry");
            psi.ArgumentList.Add("2");
            psi.ArgumentList.Add("--retry-delay");
            psi.ArgumentList.Add("2");
            psi.ArgumentList.Add("--connect-timeout");
            psi.ArgumentList.Add("20");
            psi.ArgumentList.Add("--max-time");
            psi.ArgumentList.Add("60");
            psi.ArgumentList.Add("-A");
            psi.ArgumentList.Add("Grasshopper-SUSLAB-Token-Validator");
            psi.ArgumentList.Add("-H");
            psi.ArgumentList.Add($"Authorization: Bearer {token}");
            psi.ArgumentList.Add(apiUrl);
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add(tempFile);
            psi.ArgumentList.Add("-w");
            psi.ArgumentList.Add("%{http_code}");

            using (var proc = new Process { StartInfo = psi })
            {
                proc.Start();
                string httpCode = proc.StandardOutput.ReadToEnd().Trim();
                string curlError = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (File.Exists(tempFile))
                    File.Delete(tempFile);

                if (proc.ExitCode != 0)
                {
                    return
                        "Token check failed because of a network/SSL connection problem.\n\n" +
                        "This does NOT necessarily mean the token is wrong.\n" +
                        "The cached model was NOT deleted because the token could not be verified.\n\n" +
                        $"Curl exit code: {proc.ExitCode}\n" +
                        $"Curl error:\n{curlError}";
                }

                if (httpCode == "200")
                    return null; // success

                if (httpCode == "401" || httpCode == "403" || httpCode == "404")
                    return $"AUTH_FAIL:{httpCode}";

                return
                    "Token check returned an unexpected Hugging Face HTTP response.\n\n" +
                    $"HTTP code: {httpCode}\n\n" +
                    "The cached model was NOT deleted because this was not a confirmed token failure.";
            }
        }

        /// <summary>
        /// Validate the downloaded temp file. Returns null if OK, or an error string.
        /// </summary>
        private static string ValidateDownload(string tempPath, long expectedMin, string modelName)
        {
            var info = new FileInfo(tempPath);
            if (!info.Exists || info.Length == 0)
                return $"Download failed: {modelName} file is empty.";

            // Check for HTML response
            using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fs))
            {
                char[] buffer = new char[256];
                int read = reader.Read(buffer, 0, buffer.Length);
                string head = new string(buffer, 0, read);

                if (head.StartsWith("<"))
                    return $"Download failed: HuggingFace returned HTML instead of the {modelName} ONNX model.\n" +
                           "This usually means authentication or URL resolution failed.";

                if (head.StartsWith("version https://git-lfs.github.com/spec/v1"))
                    return $"Download failed: downloaded Git LFS pointer instead of the real {modelName} ONNX model.\n" +
                           "Check that the token has Read access to the repo.";
            }

            if (info.Length < expectedMin)
                return $"Download failed: {modelName} file is too small.\n" +
                       $"Expected at least {Math.Round(expectedMin / 1024.0 / 1024.0, 0)} MB, " +
                       $"but got {Math.Round(info.Length / 1024.0 / 1024.0, 2)} MB.";

            return null;
        }

        private static void DeleteCachedFiles(string destinationPath, string tempPath, out bool deletedCache)
        {
            deletedCache = false;
            try
            {
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                    deletedCache = true;
                }
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Silently continue — deletion is best-effort
            }
        }
    }
}
