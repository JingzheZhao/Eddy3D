using EddyLib.OutdoorComfort;
using EddyLib.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    /// <summary>
    /// Computes outdoor thermal comfort using UTCI methodology.
    /// </summary>
    public class ComfortSystem
    {
        #region Constants

        /// <summary>Hours in a year.</summary>
        private const int HoursPerYear = 8760;

        /// <summary>Default pedestrian height for wind speed calculations.</summary>
        private const double PedestrianHeight = 1.8;

        #endregion

        #region Properties

        public string BaseWorkingDir { get; set; } = "";
        public Weather Weather { get; set; }
        public List<RProbe> Probes { get; set; }
        public List<RPolygon> Polys { get; set; }
        public string CFDDataPath { get; set; } = "";
        public double WindScalingFactor { get; set; } = 1;

        /// <summary>Number of method steps for progress reporting.</summary>
        public int methodsteps { get; set; } = 4;

        #endregion

        /// <summary>
        /// Creates a comfort analysis system.
        /// </summary>
        public ComfortSystem(string baseWorkingDir, Weather weather, List<RProbe> probes,
                            List<RPolygon> polys, string cfdPath, double windScalingFactor)
        {
            BaseWorkingDir = baseWorkingDir;
            Weather = weather;
            Probes = probes;
            Polys = polys;
            CFDDataPath = cfdPath;
            WindScalingFactor = windScalingFactor;
        }

        /// <summary>
        /// Loads CFD results and computes wind factors.
        /// </summary>
        public void LoadCFD_ComputeWindfactors(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            if (string.IsNullOrWhiteSpace(CFDDataPath)) return;

            WProbeResultProto resultProto_CFD;
            try
            {
                var sw = Stopwatch.StartNew();
                resultProto_CFD = WProbeResultProto.ReadFromFile(CFDDataPath);
                Console.WriteLine($"Loading WProbeResultProto: {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                Console.WriteLine($"CFD Result file could not be deserialized: {e.Message}");
                return;
            }

            Console.WriteLine($"Probe Count: {Probes.Count}, CFD Probe Count: {resultProto_CFD.Probes.Count}");

            if (Probes.Count != resultProto_CFD.Probes.Count)
            {
                Console.WriteLine("CFD and MRT workflows have different probe count. Using weather data for air velocity.");
                return;
            }

            ReportProgress(ref stepCnt, steps);

            // Compute spatial wind factors
            Console.WriteLine("Computing Spatial Wind Factors...");
            foreach (var p in resultProto_CFD.Probes)
            {
                p.WindFactorsSpatial = WindFactorsSpatial.CalcWindFactorsSpatialSP(p);
            }

            // Compute temporal wind factors
            var windSystem = new WindSystem(Weather, resultProto_CFD.Probes[0].WindDirections);
            Console.WriteLine("Computing Temporal Wind Factors...");

            foreach (var p in resultProto_CFD.Probes)
            {
                p.WindFactorsTemporal = WindFactorsTemporal.CalcWindFactorsTemporalSP(Weather, p, windSystem, true);
            }

            // Transfer wind speeds to probes
            for (int i = 0; i < Math.Min(Probes.Count, resultProto_CFD.Probes.Count); i++)
            {
                Probes[i].WindSpeed = resultProto_CFD.Probes[i].WindFactorsTemporal;
            }

            ReportProgress(ref stepCnt, steps);
        }

        /// <summary>
        /// Computes UTCI thermal comfort for all probes.
        /// </summary>
        public void ComputeUTCI(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            int probeCount = Probes.Count;
            Console.WriteLine("Computing UTCI...");

            // Bolt optimization: Precompute the wind scaling multiplier outside the Parallel.For loop
            // to avoid repetitive 8760 * Probes.Count Math.Log evaluations.
            double windProfileMultiplier = Math.Log(10 / 0.01) / Math.Log(PedestrianHeight / 0.01);

            // Bolt optimization: Pre-allocate a shared fallback array for probes lacking explicit wind data
            // to prevent allocating `new float[8760]` individually for each such probe.
            float[] defaultScaledWindSpeed = new float[HoursPerYear];
            for (int h = 0; h < HoursPerYear; h++)
            {
                defaultScaledWindSpeed[h] = (float)(Weather.WindSpeed[h] * WindScalingFactor);
            }

            Parallel.For(0, probeCount, i =>
            {
                var probe = Probes[i];
                probe.UTCI = new float[HoursPerYear];
                probe.ComfortHours = 0;

                if (probe.WindSpeed == null)
                {
                    probe.WindSpeed = defaultScaledWindSpeed;
                }

                // Calculate UTCI for each hour
                for (int h = 0; h < HoursPerYear; h++)
                {
                    // Inline GetProbeWindSpeed
                    float hourlyWindSpeed = (probe.WindSpeed == defaultScaledWindSpeed)
                        ? probe.WindSpeed[h]
                        : (float)(probe.WindSpeed[h] * windProfileMultiplier);

                    // Inline GetProbeMRT
                    double hourlyMrt = (probe.LongWave_MRT != null)
                        ? probe.LongWave_MRT[h]
                        : Weather.DryBulbTemp[h];

                    if (probe.SolarGain_dMRT != null)
                    {
                        hourlyMrt += probe.SolarGain_dMRT[h];
                    }

                    double utci = UTCI.CalcUTCICorrectBounds(
                        Weather.DryBulbTemp[h],
                        Weather.RelativeHumidity[h],
                        hourlyWindSpeed,
                        hourlyMrt,
                        out bool outOfBounds);

                    if (UTCI.CalcConditionOfPerson(utci) == 0)
                    {
                        probe.ComfortHours++;
                    }

                    probe.UTCI[h] = (float)utci;
                }
            });

            ReportProgress(ref stepCnt, steps);
        }

        /// <summary>
        /// Saves comfort results to file.
        /// </summary>
        public MRT_Simulation_ResultProto SaveResults(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Saving results...");

            var prep = PrepareProtoBufSingleton.Instance;
            var protoResult = new MRT_Simulation_ResultProto(BaseWorkingDir, Weather, Probes, Polys);
            protoResult.WriteToFile(System.IO.Path.Combine(BaseWorkingDir, "UTCI.eddy"));

            Console.WriteLine("Results written");
            ReportProgress(ref stepCnt, steps);

            return protoResult;
        }

        #region Private Methods

        /// <summary>
        /// Reports progress to console.
        /// </summary>
        private static void ReportProgress(ref int stepCnt, int steps)
        {
            Interlocked.Increment(ref stepCnt);
            int percent = 100 * stepCnt / steps;
            Console.WriteLine(ProgressWriter.ProgressKey + percent.ToString(CultureInfo.InvariantCulture));
        }

        #endregion
    }
}