using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace Eddy
{
    public class MetaBlockComponent : GH_Component
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        private static readonly HttpClient HealthHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        private static Process _serverProcess;
        private static readonly System.Text.StringBuilder _serverLog = new System.Text.StringBuilder();
        private static readonly object _serverLogLock = new object();

        private bool _working;
        private Mesh _resultMesh;
        private string _resultStatus;
        private string _resultError;
        private bool _serverStarting;
        private string _serverStartStatus;

        public MetaBlockComponent()
            : base("MetaBlock", "MetaBlock", "Combines a multi-part mesh into a single CFD-ready solid via the MetaBlock API", "Eddy3D", "1 | Wind")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Eddy_metaBlock;


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Input meshes (will be merged and combined)", GH_ParamAccess.list);
            pManager.AddTextParameter("API URL", "URL", "MetaBlock API base URL", GH_ParamAccess.item, "http://localhost:8000");
            pManager.AddBooleanParameter("Start Server", "Start", "Start the local MetaBlock server via uv (uses MetaBlock folder bundled next to Eddy.gha)", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Run", "Run", "Send the mesh to the API and retrieve the combined solid", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Combined", "M", "Combined CFD-ready solid mesh", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var meshes = new System.Collections.Generic.List<Mesh>();
            string url = "http://localhost:8000";
            bool startServer = false;
            bool run = false;

            if (!DA.GetDataList(0, meshes) || meshes.Count == 0) return;

            Mesh mesh = new Mesh();
            foreach (var m in meshes) if (m != null) mesh.Append(m);

            // Mirror Rhino's "Join" behavior — weld matching vertices across appended meshes
            // so trimesh.split() sees properly connected components rather than isolated islands.
            mesh.Vertices.CombineIdentical(true, true);
            mesh.Weld(Math.PI);
            mesh.UnifyNormals();
            mesh.Compact();
            DA.GetData(1, ref url);
            DA.GetData(2, ref startServer);
            DA.GetData(3, ref run);

            if (string.IsNullOrWhiteSpace(url))
                url = "http://localhost:8000";
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "http://" + url;

            if (startServer && !_serverStarting && (_serverProcess == null || _serverProcess.HasExited))
            {
                string apiPath = ResolveBundledMetaBlockPath();
                if (apiPath == null)
                {
                    DA.SetData(1, "Error: bundled MetaBlock folder not found next to Eddy.gha.");
                    return;
                }

                bool firstRun = !Directory.Exists(Path.Combine(apiPath, ".venv"));
                if (firstRun)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "First run: installing Python environment (this can take 1–2 minutes). Subsequent starts are instant.");
                }

                _serverStarting = true;
                _serverStartStatus = firstRun
                    ? $"Starting server (first-time install at {apiPath}\\.venv) ..."
                    : $"Starting server from {apiPath} ...";
                DA.SetData(1, _serverStartStatus);

                var startDoc = OnPingDocument();
                Task.Run(() =>
                {
                    try
                    {
                        StartServer(apiPath);
                        _serverStartStatus = $"Server started from {apiPath}";
                    }
                    catch (Exception ex)
                    {
                        _serverStartStatus = $"Server start failed: {ex.Message}";
                    }
                    finally
                    {
                        _serverStarting = false;
                        if (startDoc != null)
                            Rhino.RhinoApp.InvokeOnUiThread(new Action(() =>
                            {
                                startDoc.ScheduleSolution(1, d => ExpireSolution(false));
                            }));
                    }
                });
                return;
            }

            if (_serverStarting)
            {
                string tail = GetServerLogTail();
                string status = _serverStartStatus ?? "Starting server...";
                if (!string.IsNullOrWhiteSpace(tail))
                    status += "\n--- log ---\n" + tail;
                DA.SetData(1, status);
                return;
            }

            if (!run)
            {
                bool serverUp = _serverProcess != null && !_serverProcess.HasExited;
                if (startServer && serverUp)
                    DA.SetData(1, _serverStartStatus ?? "Server running. Set Run to true to process.");
                else if (!startServer)
                    DA.SetData(1, "Set Run to true to process.");
                else
                    DA.SetData(1, _serverStartStatus ?? "Server is not running.");
                return;
            }

            // If a previous async run completed, surface its result and clear.
            if (_resultMesh != null || _resultError != null)
            {
                if (_resultError != null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _resultError);
                    DA.SetData(1, $"Error: {_resultError}");
                }
                else
                {
                    DA.SetData(0, _resultMesh);
                    DA.SetData(1, _resultStatus);
                }
                _resultMesh = null;
                _resultError = null;
                _resultStatus = null;
                return;
            }

            if (_working)
            {
                DA.SetData(1, "Processing on server... (Grasshopper stays responsive)");
                return;
            }

            string baseUrl = url.TrimEnd('/');
            bool serverWeStarted = _serverProcess != null && !_serverProcess.HasExited;
            TimeSpan healthTimeout = serverWeStarted ? TimeSpan.FromSeconds(60) : TimeSpan.FromSeconds(3);
            byte[] stlBytes = MeshToStl(mesh);

            _working = true;
            DA.SetData(1, "Sending mesh to server...");

            var doc = OnPingDocument();
            Task.Run(async () =>
            {
                try
                {
                    if (!await WaitForHealthAsync(baseUrl + "/health", healthTimeout))
                    {
                        string tail = GetServerLogTail();
                        string msg = serverWeStarted
                            ? $"Server did not respond on /health within {(int)healthTimeout.TotalSeconds}s."
                            : $"No server reachable at {baseUrl}. Toggle Start first, or point URL at a running instance.";
                        if (!string.IsNullOrWhiteSpace(tail))
                            msg += "\nServer log tail:\n" + tail;
                        _resultError = msg;
                        return;
                    }

                    var (resultBytes, statsHeader) = await PostStlWithStatsAsync(baseUrl + "/combine?mode=urban", stlBytes);
                    Mesh combined = StlToMesh(resultBytes);
                    _resultMesh = combined;
                    _resultStatus = string.IsNullOrEmpty(statsHeader)
                        ? $"OK — {combined.Faces.Count} faces"
                        : $"OK — {combined.Faces.Count} faces\nStats: {statsHeader}";

                    Analytics.Analytics.TrackEvent("MetaBlock", "combine");
                }
                catch (Exception ex)
                {
                    string tail = GetServerLogTail();
                    _resultError = string.IsNullOrWhiteSpace(tail) ? ex.Message : $"{ex.Message}\nServer log tail:\n{tail}";
                }
                finally
                {
                    _working = false;
                    if (doc != null)
                        Rhino.RhinoApp.InvokeOnUiThread(new Action(() => { doc.ScheduleSolution(1, d => ExpireSolution(false)); }));
                }
            });
        }

        private static string ResolveBundledMetaBlockPath()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(assemblyDir)) return null;

            string bundled = Path.Combine(assemblyDir, "MetaBlock");
            return File.Exists(Path.Combine(bundled, "api.py")) ? bundled : null;
        }

        private void StartServer(string projectPath)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
                return;

            string uv = ResolveExecutable("uv");
            if (uv == null)
                throw new Exception("'uv' was not found on PATH. Install uv (https://astral.sh/uv) or add it to PATH.");

            lock (_serverLogLock) _serverLog.Clear();

            _serverStartStatus = "Syncing Python environment (uv sync)...";
            ScheduleStatusRefresh();
            // Force venv to match pyproject (handles new deps after a plugin update)
            RunBlocking(uv, "sync", projectPath, 180000);

            _serverStartStatus = "Launching uvicorn server...";
            ScheduleStatusRefresh();

            var psi = new ProcessStartInfo
            {
                FileName = uv,
                Arguments = "run api.py",
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            _serverProcess = Process.Start(psi)
                ?? throw new Exception("Failed to start uv process.");

            _serverProcess.OutputDataReceived += (_, e) => { if (e.Data != null) AppendServerLog(e.Data); };
            _serverProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendServerLog(e.Data); };
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            _serverStartStatus = "Waiting for /health to come up...";
            ScheduleStatusRefresh();
            string baseUrl = "http://localhost:8000";
            bool ready = Task.Run(() => WaitForHealthAsync(baseUrl + "/health", TimeSpan.FromSeconds(120))).GetAwaiter().GetResult();
            _serverStartStatus = ready
                ? $"Server ready at {baseUrl} (PID {_serverProcess.Id})"
                : "Server process started but /health did not respond within 120s. See log tail.";
        }

        private void ScheduleStatusRefresh()
        {
            var doc = OnPingDocument();
            if (doc == null) return;
            Rhino.RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                doc.ScheduleSolution(1, d => ExpireSolution(false));
            }));
        }

        private static void AppendServerLog(string line)
        {
            lock (_serverLogLock)
            {
                _serverLog.AppendLine(line);
                if (_serverLog.Length > 4000)
                    _serverLog.Remove(0, _serverLog.Length - 4000);
            }
        }

        private static string GetServerLogTail()
        {
            lock (_serverLogLock)
                return _serverLog.ToString();
        }

        private static string ResolveExecutable(string name)
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            string[] exts = OperatingSystem.IsWindows()
                ? new[] { ".exe", ".cmd", ".bat", "" }
                : new[] { "" };

            foreach (string dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                foreach (string ext in exts)
                {
                    string full = Path.Combine(dir, name + ext);
                    if (File.Exists(full)) return full;
                }
            }
            return null;
        }

        private static void RunBlocking(string fileName, string args, string workingDir, int timeoutMs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return;
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) AppendServerLog(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendServerLog(e.Data); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit(timeoutMs);
        }

        private static void GitPull(string projectPath)
        {
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                return;

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "pull --ff-only",
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null) return;
            proc.WaitForExit(30000);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess = null;
            }
            base.RemovedFromDocument(document);
        }

        private static async Task<bool> WaitForHealthAsync(string healthUrl, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var resp = await HealthHttp.GetAsync(healthUrl);
                    if (resp.IsSuccessStatusCode) return true;
                }
                catch { }
                await Task.Delay(500);
            }
            return false;
        }

        private static async Task<byte[]> PostStlAsync(string endpoint, byte[] stlBytes)
        {
            var (bytes, _) = await PostStlWithStatsAsync(endpoint, stlBytes);
            return bytes;
        }

        private static async Task<(byte[] bytes, string stats)> PostStlWithStatsAsync(string endpoint, byte[] stlBytes)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(stlBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", "input.stl");

            var response = await Http.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"API returned {(int)response.StatusCode}: {body}");
            }

            string stats = response.Headers.TryGetValues("X-MetaBlock-Stats", out var values)
                ? string.Join("", values)
                : "";

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            return (bytes, stats);
        }

        private static byte[] MeshToStl(Mesh mesh)
        {
            mesh.Faces.ConvertQuadsToTriangles();
            mesh.Normals.ComputeNormals();

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write(new byte[80]);
            bw.Write((uint)mesh.Faces.Count);

            foreach (var face in mesh.Faces)
            {
                var a = mesh.Vertices[face.A];
                var b = mesh.Vertices[face.B];
                var c = mesh.Vertices[face.C];

                var normal = Vector3d.CrossProduct(b - a, c - a);
                normal.Unitize();

                bw.Write((float)normal.X); bw.Write((float)normal.Y); bw.Write((float)normal.Z);
                bw.Write((float)a.X); bw.Write((float)a.Y); bw.Write((float)a.Z);
                bw.Write((float)b.X); bw.Write((float)b.Y); bw.Write((float)b.Z);
                bw.Write((float)c.X); bw.Write((float)c.Y); bw.Write((float)c.Z);
                bw.Write((ushort)0);
            }

            return ms.ToArray();
        }

        private static Mesh StlToMesh(byte[] stlBytes)
        {
            var mesh = new Mesh();

            using var ms = new MemoryStream(stlBytes);
            using var br = new BinaryReader(ms);

            br.ReadBytes(80);
            uint faceCount = br.ReadUInt32();

            for (uint i = 0; i < faceCount; i++)
            {
                br.ReadBytes(12);

                int baseIndex = mesh.Vertices.Count;
                for (int v = 0; v < 3; v++)
                {
                    float x = br.ReadSingle();
                    float y = br.ReadSingle();
                    float z = br.ReadSingle();
                    mesh.Vertices.Add(x, y, z);
                }

                mesh.Faces.AddFace(baseIndex, baseIndex + 1, baseIndex + 2);
                br.ReadUInt16();
            }

            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }

        public override Guid ComponentGuid => new Guid("7e3f1a2b-9c4d-4f8a-b1e6-2d5a8f3c9b41");
    }
}
