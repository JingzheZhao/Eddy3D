using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Eddy.Properties;
using EddyLib;
using EddyLib.GAN;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Eddy
{
    public class CompGanPredict : GH_Component
    {
        // Async state
        private GanApiClient.GanPredictionResult _cachedResult;
        private GanInputData _cachedInputData;
        private bool _isComputing;
        private string _errorMessage;
        private string _lastInputHash;
        private CancellationTokenSource _cts;
        private bool _wasRun;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        public CompGanPredict()
            : base(
                GH_Strings.GanPredict.Name,
                GH_Strings.GanPredict.Nick,
                GH_Strings.GanPredict.Desc + EddyVersion.toString(),
                EddyVersion.Name,
                "4 | ML")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter(
                GH_Strings.GanPredict.Building,
                GH_Strings.GanPredict.BuildingNick,
                GH_Strings.GanPredict.BuildingDesc,
                GH_ParamAccess.list);

            pManager.AddRectangleParameter(
                GH_Strings.GanPredict.AnalysisPlane,
                GH_Strings.GanPredict.AnalysisPlaneNick,
                GH_Strings.GanPredict.AnalysisPlaneDesc,
                GH_ParamAccess.item);
            pManager[1].Optional = true;

            pManager.AddIntegerParameter(
                GH_Strings.GanPredict.WindDir,
                GH_Strings.GanPredict.WindDirNick,
                GH_Strings.GanPredict.WindDirDesc,
                GH_ParamAccess.item, 0);

            pManager.AddBooleanParameter(
                GH_Strings.GanPredict.Run,
                GH_Strings.GanPredict.RunNick,
                GH_Strings.GanPredict.RunDesc,
                GH_ParamAccess.item, false);

            pManager.AddTextParameter(
                GH_Strings.GanPredict.ApiUrl,
                GH_Strings.GanPredict.ApiUrlNick,
                GH_Strings.GanPredict.ApiUrlDesc,
                GH_ParamAccess.item,
                GanApiClient.DefaultApiUrl);
            pManager[4].Optional = true;

            pManager.AddNumberParameter(
                GH_Strings.GanPredict.VSize,
                GH_Strings.GanPredict.VSizeNick,
                GH_Strings.GanPredict.VSizeDesc,
                GH_ParamAccess.item, 3.0);
            pManager[5].Optional = true;

            pManager.AddNumberParameter(
                GH_Strings.GanPredict.ColorSize,
                GH_Strings.GanPredict.ColorSizeNick,
                GH_Strings.GanPredict.ColorSizeDesc,
                GH_ParamAccess.item, 100.0);
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter(
                GH_Strings.GanPredict.WindSpeed,
                GH_Strings.GanPredict.WindSpeedNick,
                GH_Strings.GanPredict.WindSpeedDesc,
                GH_ParamAccess.list);

            pManager.AddMeshParameter(
                GH_Strings.GanPredict.ResultMesh,
                GH_Strings.GanPredict.ResultMeshNick,
                GH_Strings.GanPredict.ResultMeshDesc,
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Get inputs ---
            var buildings = new List<Mesh>();
            Rectangle3d analysisPlane = default;
            int windDir = 0;
            bool run = false;
            string apiUrl = GanApiClient.DefaultApiUrl;
            double vSize = 3.0;
            double colorSize = 100.0;

            if (!DA.GetDataList(0, buildings)) return;
            
            Mesh building = new Mesh();
            foreach (var b in buildings)
            {
                if (b != null)
                {
                    building.Append(b);
                }
            }

            if (!DA.GetData(1, ref analysisPlane))
            {
                if (building != null && building.Vertices.Count > 0)
                {
                    var bb = building.GetBoundingBox(true);
                    var center = bb.Center;
                    analysisPlane = new Rectangle3d(Plane.WorldXY,
                        new Interval(center.X - 256, center.X + 256),
                        new Interval(center.Y - 256, center.Y + 256));
                }
            }
            DA.GetData(2, ref windDir);
            DA.GetData(3, ref run);
            DA.GetData(4, ref apiUrl);
            DA.GetData(5, ref vSize);
            DA.GetData(6, ref colorSize);

            // Edge detection for the run boolean (button press or toggle false->true)
            bool runPressed = run && !_wasRun;
            _wasRun = run;

            // --- Validation ---
            if (building == null || building.Vertices.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Building mesh is empty.");
                return;
            }

            if (Math.Abs(analysisPlane.Width - analysisPlane.Height) > 0.001)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Analysis plane must be square.");
                return;
            }

            string inputHash = $"{building.Vertices.Count}_{analysisPlane.Width:F3}_{analysisPlane.Height:F3}_{windDir}_{vSize:F3}_{colorSize:F3}";

            // If we are currently computing, exit early but still output any previously cached result
            if (_isComputing)
            {
                Message = "Computing...";
                if (_cachedResult != null) BuildOutputs(DA);
                return;
            }

            // If Run is false, we don't start any new computation. Just output cache or errors.
            if (!run)
            {
                if (_errorMessage != null && inputHash == _lastInputHash)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _errorMessage);
                    Message = "Error";
                }
                else if (_cachedResult != null)
                {
                    BuildOutputs(DA);
                    Message = "Done";
                }
                else
                {
                    Message = "Press Run";
                }
                return;
            }

            // At this point, Run is True.
            // If inputs haven't changed and we didn't just press the button/toggle,
            // we should not re-run (this prevents auto-spam if a Toggle is left True or Button held).
            if (!runPressed && inputHash == _lastInputHash)
            {
                if (_errorMessage != null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _errorMessage);
                    Message = "Error";
                }
                else if (_cachedResult != null)
                {
                    BuildOutputs(DA);
                    Message = "Done";
                }
                return;
            }

            // --- Start async computation ---
            _isComputing = true;
            _errorMessage = null;
            // Notice we do NOT clear _cachedResult here; old results stay visible until the new computation finishes.
            _lastInputHash = inputHash;
            Message = "Generating input...";

            // Cancel any previous in-flight request
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // Clone inputs for thread safety
            var bldgCopy = building.DuplicateMesh();
            var planeCopy = analysisPlane;
            int wdCopy = windDir;
            double vsCopy = vSize;
            double csCopy = colorSize;
            string urlCopy = apiUrl;

            Task.Run(async () =>
            {
                try
                {
                    // Step 0: Check if the server is awake (free Render instances sleep after 15 min)
                    if (!await GanApiClient.CheckHealthAsync(urlCopy, token))
                    {
                        bool woke = await GanApiClient.WaitForServerAsync(
                            urlCopy,
                            maxRetries: 12,
                            retryDelayMs: 5000,
                            onStatusChange: (msg) =>
                            {
                                Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                                {
                                    Message = msg;
                                });
                            },
                            cancellationToken: token);

                        if (!woke)
                        {
                            _errorMessage = "Server did not wake up in time. The free Render instance may be sleeping — try again in a minute.";
                            return;
                        }
                    }

                    if (token.IsCancellationRequested) return;

                    // Step 1: Generate normalised float array from geometry
                    Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { Message = "Generating input..."; });
                    var inputData = GanImageGenerator.GenerateInput(
                        bldgCopy, planeCopy, wdCopy, vsCopy, csCopy);

                    if (token.IsCancellationRequested) return;

                    // Step 2: Send array to API
                    Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { Message = "Predicting..."; });
                    var result = await GanApiClient.PredictArrayAsync(
                        inputData.InputArray, urlCopy, token);

                    _cachedInputData = inputData;
                    _cachedResult = result;
                }
                catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
                {
                    _errorMessage = "API request timed out. The free Render instance may be sleeping (spins down after 15 min of inactivity). Please try again.";
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    _errorMessage = ex.Message.Contains("Rate limit")
                        ? ex.Message
                        : $"Cannot reach the GAN API: {ex.Message}";
                }
                catch (Exception ex)
                {
                    _errorMessage = $"Prediction failed: {ex.Message}";
                }
                finally
                {
                    _isComputing = false;
                    Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
                    {
                        this.ExpireSolution(true);
                    });
                }
            });
        }

        private void BuildOutputs(IGH_DataAccess DA)
        {
            if (_cachedResult == null || _cachedInputData == null) return;

            // Build result mesh from the wind-speed field
            var mesh = GanOutputProcessor.CreateResultMesh(
                _cachedResult.WindSpeeds,
                _cachedInputData.SCorner,
                _cachedInputData.PixelSize,
                _cachedInputData.WindDirection,
                _cachedInputData.Center);

            DA.SetDataList(0, _cachedResult.WindSpeeds);
            DA.SetData(1, mesh);
        }

        protected override Bitmap Icon => Resources.Eddy_misc;

        public override Guid ComponentGuid => new Guid("F3A7B2C1-D4E5-4F6A-8B9C-0D1E2F3A4B5C");
    }
}
