using EddyLib.UI;
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

        DateTime winter_start = new DateTime(2004, 1, 1);
        DateTime winter_spring = new DateTime(2004, 2, 7);
        DateTime spring_summer = new DateTime(2004, 5, 7);
        DateTime summer_fall = new DateTime(2004, 8, 6);
        DateTime fall_winter = new DateTime(2004, 11, 6);





        private double pct = 0;
        private double steps = 52 + 2;
        private double stepCnt = 0;

        MRTSimulationResultProto RSystem;

        public string ProjectName = "";
        public string BaseWorkingDir = "";
        public Weather Weather;




        public ComfortSystem(MRTSimulationResultProto sys)
        {

            RSystem = sys;

            ProjectName = RSystem.ProjectName;
            BaseWorkingDir = RSystem.BaseWorkingDir;
            Weather = RSystem.Weather;



        }


        public void ComputeUTCI(bool run, CancellationToken ct)
        {

            steps = RSystem.Probes.Count;

            System.Threading.Tasks.Parallel.For(0, RSystem.Probes.Count, i =>
            {
                var probe = RSystem.Probes[i];
                probe.UTCI = new float[8760];

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
                    double utci = UTCI.CalcUTCI(this.Weather.DryBulbTemp[h], this.Weather.RelativeHumidity[h], windspeed[h], mrt[h]);
                    probe.UTCI[h] = (float) utci;
                }

                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));
            });



        }

        public MRTSimulationResultProto SaveResults(bool run, CancellationToken ct)
        {

            // -----------------------------
            // Write results
            // -----------------------------
            var prep = PrepareProtoBufSingleton.Instance;


            RSystem.WriteToFile(this.BaseWorkingDir + @"\" + this.ProjectName + ".utci.eddy");

            Console.WriteLine("Results written");
            stepCnt++;
            pct = 100 * stepCnt / steps;
            Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

            return RSystem;

        }
    }
}
