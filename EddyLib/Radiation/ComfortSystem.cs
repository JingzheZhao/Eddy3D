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

            Parallel.For(0, probeCount, i =>
            {
                var probe = Probes[i];
                probe.UTCI = new float[HoursPerYear];
                probe.ComfortHours = 0;

                // Initialize wind speed from weather data
                float[] windSpeed = GetProbeWindSpeed(probe);

                // Initialize MRT from weather or probe data
                double[] mrt = GetProbeMRT(probe);

                // Calculate UTCI for each hour
                for (int h = 0; h < HoursPerYear; h++)
                {
                    double utci = UTCI.CalcUTCICorrectBounds(
                        Weather.DryBulbTemp[h],
                        Weather.RelativeHumidity[h],
                        windSpeed[h],
                        mrt[h],
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
        /// Gets wind speed array for a probe, using CFD data if available.
        /// </summary>
        private float[] GetProbeWindSpeed(RProbe probe)
        {
            int hours = Weather.WindSpeed.Length;
            var windSpeed = new float[hours];

            if (probe.WindSpeed != null)
            {
                // Use CFD data, scaled to 10m height for UTCI
                for (int h = 0; h < hours; h++)
                {
                    windSpeed[h] = (float)UTCI.At10Meters(probe.WindSpeed[h], PedestrianHeight);
                }
            }
            else
            {
                // Use weather data with scaling factor
                for (int h = 0; h < hours; h++)
                {
                    windSpeed[h] = (float)(Weather.WindSpeed[h] * WindScalingFactor);
                }
                probe.WindSpeed = windSpeed;
            }

            return windSpeed;
        }

        /// <summary>
        /// Gets MRT array for a probe, combining longwave and solar components.
        /// </summary>
        private double[] GetProbeMRT(RProbe probe)
        {
            int hours = Weather.DryBulbTemp.Length;
            var mrt = new double[hours];

            // Start with longwave MRT or dry bulb temperature
            if (probe.LongWave_MRT != null)
            {
                for (int h = 0; h < hours; h++)
                {
                    mrt[h] = probe.LongWave_MRT[h];
                }
            }
            else
            {
                Array.Copy(Weather.DryBulbTemp, mrt, hours);
            }

            // Add solar radiation component (delta MRT)
            if (probe.SolarGain_dMRT != null)
            {
                for (int h = 0; h < hours; h++)
                {
                    mrt[h] += probe.SolarGain_dMRT[h];
                }
            }

            return mrt;
        }

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