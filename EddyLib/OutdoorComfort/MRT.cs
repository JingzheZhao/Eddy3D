using EddyLib.Radiation;
using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;

namespace EddyLib.OutdoorComfort
{
    public class MRTSimulation
    {
        // Outputs

        public SkyViewFactor svf;

        public SkyTemperatureModel sky;

        public MRT mrt;

        // Inputs

        private OFResult RES; private Weather weather; private Mesh BAG; private Point3d[] probesArr; private MRT.MRTType SimMode; private bool run;

        public MRTSimulation(OFResult RES, Weather weather, Mesh BAG, Point3d[] probesArr, MRT.MRTType SimMode, bool run)
        {
            this.RES = RES;

            this.weather = weather;
            this.BAG = BAG;
            this.probesArr = probesArr;
            this.SimMode = SimMode;
            this.run = run;

            var svf = new SkyViewFactor(RES.WorkingDirectory, BAG, probesArr, run);

            var sky = new SkyTemperatureModel(weather.DewPointTemp, weather.DryBulbTemp, weather.TotalSkyCover, weather.RelativeHumidity, run, SkyTemperatureModel.CalculationType.DefaultClarkAllen);

            var mrt = new MRT(RES.WorkingDirectory, RES.Domain.BuildingGeometry, sky, svf, weather, SimMode, probesArr, run);

            this.svf = svf;
            this.sky = sky;
            this.mrt = mrt;
        }
    }

    public class MRT

    {
        // public static object Options { get; private set; }

        public enum MRTType
        {
            // daysimkessling,

            RadianceTwoPhaseDDS,
        }

        public double[,] Values;

        public bool wrongNumberOfProbes;

        public bool resultPrecalculated;

        public double[][] DiffRad;

        public double[][] DirRad;

        public double[][] TotalRad;

        public double[] ViewFactors;

        public double[] SkyTemp;

        public MRT(string baseWorkingDir, Mesh BuildingGeometry, SkyTemperatureModel sky, SkyViewFactor vf, Weather weather, MRTType type, Point3d[] probes, bool recalc)
        {
            var binMRT = baseWorkingDir + @"MRT.bin";

            var numberOfProbes = probes.Length;

            #region TwoPhaseDDS

            var difillFile = baseWorkingDir + @"\Output\annual_total.ill";
            var dirillFile = baseWorkingDir + @"\Output\annual_dir.ill";

            // Add other files here
            if (recalc == false && File.Exists(binMRT))
            {
                // Load radiation datasets [x][] time [][x] points

                var tempValues = RadianceFiles.loadBinD(binMRT);

                int sensorPointCountExisting = tempValues.GetLength(1);

                if (sensorPointCountExisting != numberOfProbes)
                {
                    this.wrongNumberOfProbes = true;
                    return;
                }
                else
                {
                    this.Values = tempValues;
                }
            }
            else if (recalc == true)
            {
                if (File.Exists(binMRT))
                {
                    File.Delete(binMRT);
                }

                //Utilities.CleanDirectory(baseWorkingDir + @"Rad\");
                //Utilities.CleanDirectory(baseWorkingDir + @"Output\");

                EddyLib.Radiation.TwoPhaseDDS dds = new EddyLib.Radiation.TwoPhaseDDS(baseWorkingDir, BuildingGeometry, probes.ToList(), weather, recalc);

                this.SkyTemp = sky.Temp;

                this.ViewFactors = vf.Values;

                int numberOfHours = 8760;
                int numberOfSensors = probes.Length;

                var DDSTOTAL = dds.totalIll;

                this.Values = new double[numberOfHours, numberOfSensors];

                double sol_trans = 1;
                double f_bes = 0.5;

                System.Threading.Tasks.Parallel.For(0, 8760, h =>
                 {
                     for (int p = 0; p < probes.Length; p++)
                     {
                         double dMRT;
                         double ERF;

                         SolarGain.ERF(weather.SolarElevation[h], weather.SolarAzi[h], SolarGain.Posture.standing, DDSTOTAL[h][p], sol_trans, ViewFactors[p], f_bes, 0.6, out ERF, out dMRT);

                         var surfaceTempBuilding = weather.DryBulbTemp[h] * (1 - ViewFactors[p]);
                         var skyTemp = sky.Temp[h] * ViewFactors[p];

                         this.Values[h, p] = surfaceTempBuilding + dMRT + skyTemp;
                     }
                 });

                RadianceFiles.writeBin(baseWorkingDir + @"\MRT.bin", this.Values);
            }

            #endregion TwoPhaseDDS
        }

        public static double[] GetMRTForPointViaKessling(Weather weather, int hour, double DiffRad, double DirRad)
        {
            // Deconstruct Weather

            if (hour > 8759)
            {
                throw new System.ArgumentException(@"Calculation of MRT for hours > 8759 not possible.");
            }

            double Tair = weather.DryBulbTemp[hour]; double RelHum = weather.RelativeHumidity[hour]; double SolarElev = weather.SolarElevation[hour]; double T_celsius = weather.DryBulbTemp[hour];
            double Wst = weather.Wst; double Hst = weather.Hst; double BodyA = weather.BodyA; double GrRef = weather.GrRef; double Eb = 0.95;

            //Standard call
            //UTCI.GetMRT(weather.DryBulbTemp[i], weather.RelativeHumidity[i], DiffRad[i][j], DirRad[i][j], weather.SolarElevation[i], weather.DryBulbTemp[i], weather.Wst, weather.Hst, weather.BodyA, weather.GrRef, 0.95)[0];

            // Why do we assume Eb = 0.95 when calling function? Is Eb the same as Es/Ec? What is Eb?
            // What is Hst and Wst?

            double[] MRT = new double[2];

            MRT[0] = Tair;
            MRT[1] = Tair;

            //Reference: [1] http://www.academia.edu/13838171/The_Human_Bio-Meteorological_Chart_A_design_tool_for_outdoor_thermal_comfort
            //Reference: [2] The calculation of the mean radiant temperature of a subject exposed to the solar radiation—a generalised algorithm
            //Reference: [3] The Computation of Equivalent Potential Temperature - David Bolton

            double SBConst = 5.67E-8;

            double es = Math.Log(RelHum / 100) + 17.67 * Tair / (243.5 + Tair); // [3] for -30 -- 35°C
            double T_dewP = 243.5 * es / (17.67 - es); // [3]
            double e = 0.7122 + 0.0056 * T_dewP + 0.000073 * Math.Pow(T_dewP, 2) + 0.00884; // polinomial for curve fit [1]
            double TSkyKelvin = (Tair + 273) * Math.Pow(e, 0.25);  // [1]
            double T_celsius_kelvin = T_celsius + 273;

            double Fs = (Math.Atan(0.5 * Wst / (Hst - 1))) * 180 / Math.PI * 0.0056; // where does this come from?

            // where FiS is
            // the angle
            // factor
            // between the
            // ith internal
            // surface of
            // the envelope
            // and the
            // subject, ei
            // is its
            // emissivity,
            // Ai is the
            // area of the
            // interested
            // surface, Ti
            // the
            // temperature,
            // ri the
            // reflection
            // coefficient
            // of the ith
            // surface and
            // Gi the
            // radiation
            // reaching the
            // ith internal surface.
            double Fc = 1 - Fs;  // remaining angle factor

            double Es = 0.95;  // Emissivities? Why 0.95?
            double Ec = 0.95;  // Emissivities?

            double Fd = 0.50;  // Does this account for 50 % sky and 50 % ground? if yes then this should be an input that changes with respect to the canyon
            double f = 0.00000043 * Math.Pow(SolarElev, 3) - 0.000068 * Math.Pow(SolarElev, 2) + 0.0003 * SolarElev + 0.3081; // Where does this come from?

            double IR = Math.Pow((1 / Eb * (Fs * Math.Pow(TSkyKelvin, 4) * Es + Fc * Math.Pow(T_celsius_kelvin, 4) * Ec)), 0.25);
            double DF = Math.Pow(((DiffRad * Fd + (DiffRad + DirRad * Math.Sin(SolarElev * Math.PI / 180)) * GrRef) * BodyA * 0.725 / (Eb * SBConst)), 0.25);
            double DR = Math.Pow((DirRad * f * BodyA * 0.725 / (Eb * SBConst)), 0.25);

            double MRTKelvin = Math.Pow(Math.Pow(IR, 4) + Math.Pow(DF, 4) + Math.Pow(DR, 4), 0.25);
            double MRTCelsius = MRTKelvin - 273;

            MRT[0] = MRTCelsius;
            MRT[1] = IR - 273;
            return MRT;
        }
    }
}