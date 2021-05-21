using EddyLib.OutdoorComfort;
using EddyLib.UI;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    public class ComfortSystem
    {
        public int methodsteps = 4; // energyplus prints 52 lines

        //private DateTime winter_start = new DateTime(2004, 1, 1);
        //private DateTime winter_spring = new DateTime(2004, 2, 7);
        //private DateTime spring_summer = new DateTime(2004, 5, 7);
        //private DateTime summer_fall = new DateTime(2004, 8, 6);
        //private DateTime fall_winter = new DateTime(2004, 11, 6);

        private double pct = 0;
        private double steps = 52 + 2;
        private double stepCnt = 0;

        public string BaseWorkingDir = "";
        public Weather Weather;

        public List<RProbe> Probes;
        public List<RPolygon> Polys;

        public string CFDDataPath = "";

        public double WindScalingFactor = 1;
        public ComfortSystem(string baseWorkingDir, Weather weather, List<RProbe> probes, List<RPolygon> polys, string cfdpath, double wsf)
        {
            BaseWorkingDir = baseWorkingDir;
            Weather = weather;
            Probes = probes;
            Polys = polys;
            CFDDataPath = cfdpath;
            WindScalingFactor = wsf;
        }

        public void LoadCFD_ComputeWindfactors(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            // ---------------------
            // 1  Load CFD Result
            // ---------------------

            WProbeResultProto resultProto_CFD = null;

            if (!String.IsNullOrWhiteSpace(CFDDataPath))
            {
                try
                {
                    Stopwatch sp = new Stopwatch();
                    sp.Restart();
                    resultProto_CFD = WProbeResultProto.ReadFromFile(CFDDataPath);
                    sp.Stop();
                    Console.WriteLine("Loading WProbeResultProto: " + sp.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    Console.WriteLine("CFD Result file could not be deserialized. Are you loading a wrong file type? " + Environment.NewLine + e.Message);
                    return;
                }

                Console.WriteLine("Probe Count: " + this.Probes.Count);
                Console.WriteLine("CFD Probe Count: " + resultProto_CFD.Probes.Count);

                if (this.Probes.Count != resultProto_CFD.Probes.Count)
                {
                    Console.WriteLine("CFD and MRT worflows have different probe count... using weather data for prove air velocity");
                    return;
                }

                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // ---------------------
                // 2  Compute Wind Factors
                // ---------------------

                // WindFactorSpatial
                Console.WriteLine("Computing Spatial Wind Factors...");

                foreach (var p in resultProto_CFD.Probes)
                {
                    p.WindFactorsSpatial = EddyLib.OutdoorComfort.WindFactorsSpatial.CalcWindFactorsSpatialSP(p);
                }
                Console.WriteLine("Computing Spatial Wind Factors...");

                WindSystem WS = new WindSystem(this.Weather, resultProto_CFD.Probes[0].WindDirections.ToList());
                Console.WriteLine("Computing Temporal Wind Factors...");

                int pcnt = 0;
                foreach (var p in resultProto_CFD.Probes)
                {
                    p.WindFactorsTemporal = WindFactorsTemporal.CalcWindFactorsTemporalSP(this.Weather, p, WS, true);
                }

                for (int i = 0; i < this.Probes.Count; i++)
                {
                    if (i < resultProto_CFD.Probes.Count)
                    {
                        this.Probes[i].WindSpeed = resultProto_CFD.Probes[i].WindFactorsTemporal;
                    }
                }

                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
            }
        }

        public void ComputeUTCI(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            steps = this.Probes.Count;

            // ---------------------
            // 3 Compute UTCI
            // ---------------------
            Console.WriteLine("Computing UTCI...");

            System.Threading.Tasks.Parallel.For(0, this.Probes.Count, i =>
            {
                var probe = this.Probes[i];
                probe.UTCI = new float[8760];
                probe.ComfortHours = 0;

                // get wind speed data -- init array with wind speed data from weather
                float[] windspeed = new float[this.Weather.WindSpeed.Length];
                for (int h = 0; h < this.Weather.WindSpeed.Length; h++)
                {
                    windspeed[h] = (float)(this.Weather.WindSpeed[h] * WindScalingFactor);
                }
 
                // if CFD wind speed data exsists - then override
                if (probe.WindSpeed != null)
                {
                    for (int h = 0; h < windspeed.Length; h++)
                    {
                        // lift to 10 m height as required

                        // scale up to 10 m
                        //var z0 = 1;
                        //var uref = 2.89;
                        //var zref = 3;

                        //// Act
                        //var res = EddyLib.BCs.BoundaryCondition.ScaleABL(uref, zref, z0, 10);

                        var resultingWindSpeedforUTCI_At10 = UTCI.At10Meters((double)probe.WindSpeed[h], 1.8);
                        //  var resultingWindSpeedforUTCI_At10 = UTCI.At10Meters(resultingWindSpeedforUTCI, probe.Point.Value.Z);

                        windspeed[h] = (float)resultingWindSpeedforUTCI_At10;
                    }
                }
                else {
                    probe.WindSpeed = windspeed;
                }

                // init mrt with dry bulb temperature from weather
                double[] mrt = new double[this.Weather.DryBulbTemp.Length];
                Array.Copy(mrt, this.Weather.DryBulbTemp, this.Weather.DryBulbTemp.Length);

                // if longwave mrt data exsists - then override
                if (probe.LongWave_MRT != null)
                {
                    for (int h = 0; h < mrt.Length; h++)
                    {
                        mrt[h] = (double)probe.LongWave_MRT[h];
                    }
                }
                // if radiation data exsists - add dMRT to mrt
                if (probe.SolarGain_dMRT != null)
                {
                    for (int h = 0; h < mrt.Length; h++)
                    {
                        mrt[h] = mrt[h] + (double)probe.SolarGain_dMRT[h];
                    }
                }

                for (int h = 0; h < 8760; h++)

                {
                    double utci = UTCI.CalcUTCICorrectBounds(this.Weather.DryBulbTemp[h], this.Weather.RelativeHumidity[h], (double)windspeed[h], mrt[h], out bool outOfBounds);

                    var condition = UTCI.CalcConditionOfPerson(utci);

                    if (condition == 0) probe.ComfortHours++;

                    probe.UTCI[h] = (float)utci;
                }

                //stepCnt++;
                //pct = 100 * stepCnt / steps;
                //Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));
            });

            Interlocked.Increment(ref stepCnt);
            Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
        }

        public MRT_Simulation_ResultProto SaveResults(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            // -----------------------------
            // 4 Write results
            // -----------------------------

            Console.WriteLine("Saving results...");

            var prep = PrepareProtoBufSingleton.Instance;

            var protoResult = new MRT_Simulation_ResultProto(this.BaseWorkingDir, this.Weather, this.Probes, this.Polys);

            protoResult.WriteToFile(this.BaseWorkingDir + @"\UTCI.eddy");

            Console.WriteLine("Results written");
            Interlocked.Increment(ref stepCnt);
            Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

            return protoResult;
        }
    }
}