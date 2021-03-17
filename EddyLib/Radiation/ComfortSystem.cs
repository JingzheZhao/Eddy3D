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
        private DateTime winter_start = new DateTime(2004, 1, 1);
        private DateTime winter_spring = new DateTime(2004, 2, 7);
        private DateTime spring_summer = new DateTime(2004, 5, 7);
        private DateTime summer_fall = new DateTime(2004, 8, 6);
        private DateTime fall_winter = new DateTime(2004, 11, 6);

        private double pct = 0;
        private double steps = 52 + 2;
        private double stepCnt = 0;

        public string ProjectName = "";
        public string BaseWorkingDir = "";
        public Weather Weather;

        public List<RProbe> Probes;
        public List<RPolygon> Polys;

        public ComfortSystem(string filename, string baseWorkingDir, Weather weather, List<RProbe> probes, List<RPolygon> polys)
        {
            ProjectName = filename;
            BaseWorkingDir = baseWorkingDir;
            Weather = weather;
            Probes = probes;
            Polys = polys;
        }

        public void ComputeUTCI(bool run, CancellationToken ct)
        {
            steps = this.Probes.Count;

            System.Threading.Tasks.Parallel.For(0, this.Probes.Count, i =>
            {
                var probe = this.Probes[i];
                probe.UTCI = new float[8760];
                probe.ComfortHours = 0;

                // get wind speed data -- init array with wind speed data from weather
                var windspeed = this.Weather.WindSpeed;
                // if CFD wind speed data exsists - then override
                if (probe.WindSpeed != null)
                {
                    for (int j = 0; j < windspeed.Length; j++)
                    {
                        windspeed[j] = (double)probe.WindSpeed[j];
                    }
                }

                // init mrt with dry bulb temperature from weather
                var mrt = this.Weather.DryBulbTemp;
                // if longwave mrt data exsists - then override
                if (probe.LongWave_MRT != null)
                {
                    for (int j = 0; j < mrt.Length; j++)
                    {
                        mrt[j] = (double)probe.LongWave_MRT[j];
                    }
                }
                // if radiation data exsists - add dMRT to mrt
                if (probe.SolarGain_dMRT != null)
                {
                    for (int j = 0; j < mrt.Length; j++)
                    {
                        mrt[j] = mrt[j] + (double)probe.SolarGain_dMRT[j];
                    }
                }

                for (int h = 0; h < 8760; h++)
                {
                    // Check for extreme MRTs
                    double resultingMRT = mrt[h];

                    if (resultingMRT < this.Weather.DryBulbTemp[h] - 30) { resultingMRT = this.Weather.DryBulbTemp[h] - 30; }
                    if (resultingMRT > this.Weather.DryBulbTemp[h] + 70) { resultingMRT = this.Weather.DryBulbTemp[h] + 70; }

                    // Check for extreme Windspeeds
                    double resultingWindSpeedforUTCI = windspeed[h];

                    if (resultingWindSpeedforUTCI > 17) { resultingWindSpeedforUTCI = 17; }
                    if (resultingWindSpeedforUTCI < 0.5) { resultingWindSpeedforUTCI = 0.5; }

                    // scale up to 10 m
                    //var z0 = 1;
                    //var uref = 2.89;
                    //var zref = 3;

                    //// Act
                    //var res = EddyLib.BCs.BoundaryCondition.ScaleABL(uref, zref, z0, 10);

                    // lift to 10 m height as required
                    var resultingWindSpeedforUTCI_At10 = UTCI.At10Meters(resultingWindSpeedforUTCI, 1.8);
                    //  var resultingWindSpeedforUTCI_At10 = UTCI.At10Meters(resultingWindSpeedforUTCI, probe.Point.Value.Z);

                    double utci = UTCI.CalcUTCI(this.Weather.DryBulbTemp[h], this.Weather.RelativeHumidity[h], resultingWindSpeedforUTCI, resultingMRT);

                    //if (utci < -40) utci = -40;
                    //if (utci > 46) utci = 46;

                    var condition = UTCI.CalcConditionOfPerson(utci);

                    if (condition == 0) probe.ComfortHours++;

                    probe.UTCI[h] = (float)utci;
                }

                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));
            });
        }

        public MRT_Simulation_ResultProto SaveResults(bool run, CancellationToken ct)
        {
            // -----------------------------
            // Write results
            // -----------------------------
            var prep = PrepareProtoBufSingleton.Instance;

            var protoResult = new MRT_Simulation_ResultProto(this.ProjectName, this.BaseWorkingDir, this.Weather, this.Probes, this.Polys);

            protoResult.WriteToFile(this.BaseWorkingDir + @"\" + this.ProjectName + ".utci.eddy");

            Console.WriteLine("Results written");
            stepCnt++;
            pct = 100 * stepCnt / steps;
            Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

            return protoResult;
        }
    }
}