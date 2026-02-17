using Eddy.Properties;
using EddyLib;
using EddyLib.FluidX3D;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Eddy
{
    public class FluidX3DVtkProbe_Component : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;

        public FluidX3DVtkProbe_Component()
          : base(
              GH_Strings.FluidX3DProbe.Name,
              GH_Strings.FluidX3DProbe.Nick,
              GH_Strings.FluidX3DProbe.Desc + EddyVersion.toString(),
              EddyVersion.Name,
              "1 | Wind")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                GH_Strings.FluidX3DProbe.ExportDir,
                GH_Strings.FluidX3DProbe.ExportDirNick,
                GH_Strings.FluidX3DProbe.ExportDirDesc,
                GH_ParamAccess.item);

            pManager.AddPointParameter(
                GH_Strings.FluidX3DProbe.Points,
                GH_Strings.FluidX3DProbe.PointsNick,
                GH_Strings.FluidX3DProbe.PointsDesc,
                GH_ParamAccess.list);

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3DProbe.Quantity,
                GH_Strings.FluidX3DProbe.QuantityNick,
                GH_Strings.FluidX3DProbe.QuantityDesc,
                GH_ParamAccess.item,
                0);
            Param_Integer quantityParam = pManager[2] as Param_Integer;
            quantityParam?.AddNamedValue("Velocity U", 0);
            quantityParam?.AddNamedValue("Density rho", 1);

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3DProbe.TimeMode,
                GH_Strings.FluidX3DProbe.TimeModeNick,
                GH_Strings.FluidX3DProbe.TimeModeDesc,
                GH_ParamAccess.item,
                0);
            Param_Integer modeParam = pManager[3] as Param_Integer;
            modeParam?.AddNamedValue("Latest", 0);
            modeParam?.AddNamedValue("Closest physical time", 1);
            modeParam?.AddNamedValue("Average over [T0, T1]", 2);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.TargetTime,
                GH_Strings.FluidX3DProbe.TargetTimeNick,
                GH_Strings.FluidX3DProbe.TargetTimeDesc,
                GH_ParamAccess.item,
                0.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.StartTime,
                GH_Strings.FluidX3DProbe.StartTimeNick,
                GH_Strings.FluidX3DProbe.StartTimeDesc,
                GH_ParamAccess.item,
                0.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.EndTime,
                GH_Strings.FluidX3DProbe.EndTimeNick,
                GH_Strings.FluidX3DProbe.EndTimeDesc,
                GH_ParamAccess.item,
                30.0);

            pManager.AddBooleanParameter(
                GH_Strings.FluidX3DProbe.Run,
                GH_Strings.FluidX3DProbe.RunNick,
                GH_Strings.FluidX3DProbe.RunDesc,
                GH_ParamAccess.item,
                false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter(
                GH_Strings.FluidX3DProbe.PointsOut,
                GH_Strings.FluidX3DProbe.PointsOutNick,
                GH_Strings.FluidX3DProbe.PointsOutDesc,
                GH_ParamAccess.list);

            pManager.AddVectorParameter(
                GH_Strings.FluidX3DProbe.VelocityByTime,
                GH_Strings.FluidX3DProbe.VelocityByTimeNick,
                GH_Strings.FluidX3DProbe.VelocityByTimeDesc,
                GH_ParamAccess.tree);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.DensityByTime,
                GH_Strings.FluidX3DProbe.DensityByTimeNick,
                GH_Strings.FluidX3DProbe.DensityByTimeDesc,
                GH_ParamAccess.tree);

            pManager.AddVectorParameter(
                GH_Strings.FluidX3DProbe.VelocityAverage,
                GH_Strings.FluidX3DProbe.VelocityAverageNick,
                GH_Strings.FluidX3DProbe.VelocityAverageDesc,
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.DensityAverage,
                GH_Strings.FluidX3DProbe.DensityAverageNick,
                GH_Strings.FluidX3DProbe.DensityAverageDesc,
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.SampledTimes,
                GH_Strings.FluidX3DProbe.SampledTimesNick,
                GH_Strings.FluidX3DProbe.SampledTimesDesc,
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.SampledSteps,
                GH_Strings.FluidX3DProbe.SampledStepsNick,
                GH_Strings.FluidX3DProbe.SampledStepsDesc,
                GH_ParamAccess.list);

            pManager.AddTextParameter(
                GH_Strings.FluidX3DProbe.SampledFiles,
                GH_Strings.FluidX3DProbe.SampledFilesNick,
                GH_Strings.FluidX3DProbe.SampledFilesDesc,
                GH_ParamAccess.list);

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3DProbe.OutsideCount,
                GH_Strings.FluidX3DProbe.OutsideCountNick,
                GH_Strings.FluidX3DProbe.OutsideCountDesc,
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                GH_Strings.FluidX3DProbe.Status,
                GH_Strings.FluidX3DProbe.StatusNick,
                GH_Strings.FluidX3DProbe.StatusDesc,
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string exportDir = string.Empty;
            List<Point3d> points = new List<Point3d>();
            int quantityInt = 0;
            int timeModeInt = 0;
            double targetTimeSeconds = 0.0;
            double startTimeSeconds = 0.0;
            double endTimeSeconds = 30.0;
            bool run = false;

            if (!DA.GetData(0, ref exportDir))
            {
                return;
            }

            if (!DA.GetDataList(1, points))
            {
                return;
            }

            DA.GetData(2, ref quantityInt);
            DA.GetData(3, ref timeModeInt);
            DA.GetData(4, ref targetTimeSeconds);
            DA.GetData(5, ref startTimeSeconds);
            DA.GetData(6, ref endTimeSeconds);
            DA.GetData(7, ref run);

            DA.SetDataList(0, points);

            GH_Structure<GH_Vector> velocityTree = new GH_Structure<GH_Vector>();
            GH_Structure<GH_Number> rhoTree = new GH_Structure<GH_Number>();
            List<Vector3d> velocityAverage = new List<Vector3d>();
            List<double> rhoAverage = new List<double>();
            List<double> sampledTimes = new List<double>();
            List<double> sampledSteps = new List<double>();
            List<string> sampledFiles = new List<string>();
            int outsideCount = 0;
            string status = "Set Run=true to probe FluidX3D VTK results.";

            if (!run)
            {
                Message = "Idle";
                WriteOutputs(
                    DA,
                    velocityTree,
                    rhoTree,
                    velocityAverage,
                    rhoAverage,
                    sampledTimes,
                    sampledSteps,
                    sampledFiles,
                    outsideCount,
                    status);
                return;
            }

            if (points.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "At least one probe point is required.");
                Message = "No points";
                WriteOutputs(
                    DA,
                    velocityTree,
                    rhoTree,
                    velocityAverage,
                    rhoAverage,
                    sampledTimes,
                    sampledSteps,
                    sampledFiles,
                    outsideCount,
                    "No probe points supplied.");
                return;
            }

            if (string.IsNullOrWhiteSpace(exportDir))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Export directory is required.");
                Message = "Missing dir";
                WriteOutputs(
                    DA,
                    velocityTree,
                    rhoTree,
                    velocityAverage,
                    rhoAverage,
                    sampledTimes,
                    sampledSteps,
                    sampledFiles,
                    outsideCount,
                    "Export directory is empty.");
                return;
            }

            FluidX3DVtkProbeQuantity quantity = quantityInt == 1
                ? FluidX3DVtkProbeQuantity.DensityRho
                : FluidX3DVtkProbeQuantity.VelocityU;

            FluidX3DVtkProbeTimeMode timeMode = timeModeInt switch
            {
                1 => FluidX3DVtkProbeTimeMode.ClosestPhysicalTime,
                2 => FluidX3DVtkProbeTimeMode.AverageOverRange,
                _ => FluidX3DVtkProbeTimeMode.Latest
            };

            Message = string.Format(
                CultureInfo.InvariantCulture,
                "{0} | {1}",
                quantity == FluidX3DVtkProbeQuantity.VelocityU ? "U" : "rho",
                timeMode == FluidX3DVtkProbeTimeMode.Latest
                    ? "Latest"
                    : timeMode == FluidX3DVtkProbeTimeMode.ClosestPhysicalTime
                        ? "Closest T"
                        : "Avg[T0,T1]");

            try
            {
                if (!Directory.Exists(exportDir))
                {
                    throw new DirectoryNotFoundException("Export directory not found: " + exportDir);
                }

                FluidX3DVtkProbeRequest request = new FluidX3DVtkProbeRequest
                {
                    ExportDirectory = exportDir,
                    Quantity = quantity,
                    TimeMode = timeMode,
                    TargetTimeSeconds = targetTimeSeconds,
                    StartTimeSeconds = startTimeSeconds,
                    EndTimeSeconds = endTimeSeconds
                };

                for (int i = 0; i < points.Count; i++)
                {
                    Point3d p = points[i];
                    request.RhinoPoints.Add(new FluidX3DPoint3(p.X, p.Y, p.Z));
                }

                FluidX3DVtkProbeResult result = FluidX3DVtkProber.Probe(request);

                sampledTimes.AddRange(result.SampledTimesSeconds);
                for (int i = 0; i < result.SampledSteps.Count; i++)
                {
                    sampledSteps.Add(result.SampledSteps[i]);
                }

                sampledFiles.AddRange(result.SampledFiles);
                outsideCount = result.OutsideDomainPointCount;
                status = result.Status;

                if (quantity == FluidX3DVtkProbeQuantity.VelocityU)
                {
                    for (int t = 0; t < result.VectorValuesByTime.Count; t++)
                    {
                        GH_Path path = new GH_Path(t);
                        FluidX3DPoint3[] values = result.VectorValuesByTime[t];
                        for (int i = 0; i < values.Length; i++)
                        {
                            FluidX3DPoint3 v = values[i];
                            velocityTree.Append(new GH_Vector(new Vector3d(v.X, v.Y, v.Z)), path);
                        }
                    }

                    if (result.AverageVectors != null)
                    {
                        for (int i = 0; i < result.AverageVectors.Length; i++)
                        {
                            FluidX3DPoint3 v = result.AverageVectors[i];
                            velocityAverage.Add(new Vector3d(v.X, v.Y, v.Z));
                        }
                    }
                }
                else
                {
                    for (int t = 0; t < result.ScalarValuesByTime.Count; t++)
                    {
                        GH_Path path = new GH_Path(t);
                        double[] values = result.ScalarValuesByTime[t];
                        for (int i = 0; i < values.Length; i++)
                        {
                            rhoTree.Append(new GH_Number(values[i]), path);
                        }
                    }

                    if (result.AverageScalars != null)
                    {
                        rhoAverage.AddRange(result.AverageScalars);
                    }
                }

                if (outsideCount > 0)
                {
                    AddRuntimeMessage(
                        GH_RuntimeMessageLevel.Warning,
                        outsideCount.ToString(CultureInfo.InvariantCulture)
                        + " probe point(s) were outside the domain and clamped.");
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                status = "Probe failed: " + ex.Message;
            }

            WriteOutputs(
                DA,
                velocityTree,
                rhoTree,
                velocityAverage,
                rhoAverage,
                sampledTimes,
                sampledSteps,
                sampledFiles,
                outsideCount,
                status);
        }

        private static void WriteOutputs(
            IGH_DataAccess DA,
            GH_Structure<GH_Vector> velocityTree,
            GH_Structure<GH_Number> rhoTree,
            List<Vector3d> velocityAverage,
            List<double> rhoAverage,
            List<double> sampledTimes,
            List<double> sampledSteps,
            List<string> sampledFiles,
            int outsideCount,
            string status)
        {
            DA.SetDataTree(1, velocityTree);
            DA.SetDataTree(2, rhoTree);
            DA.SetDataList(3, velocityAverage);
            DA.SetDataList(4, rhoAverage);
            DA.SetDataList(5, sampledTimes);
            DA.SetDataList(6, sampledSteps);
            DA.SetDataList(7, sampledFiles);
            DA.SetData(8, outsideCount);
            DA.SetData(9, status ?? string.Empty);
        }

        protected override System.Drawing.Bitmap Icon => Resources.Eddy_visualProbs;

        public override Guid ComponentGuid => new Guid("{81A47466-AA49-45C4-8D40-5CBDA5004300}");
    }
}
