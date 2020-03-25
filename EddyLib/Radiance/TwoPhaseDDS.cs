using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiance
{
    public class TwoPhaseDDS
    {
        //annualR_dc.ill + -s -1 output/annualR_dcd.ill + output/annual_dir.ill > output/annual_total.ill

        public double[][] dcill;

        public double[][] dcdill;

        public double[][] dirill;

        public double[][] totalIll;

        public string command;

        public TwoPhaseDDS(string baseWorkingDir, Mesh BuildingGeometry, List<Point3d> probes, Weather weather, bool run, string RadianceDir = @"C:\Program Files\Radiance")

        {
            //var skySubDivDiff = SkySubdivision.r1;
            var skySubDivDiff = SkySubdivision.r2;
            var skySubDivDir = SkySubdivision.r4;

            if (!Directory.Exists(baseWorkingDir))
            {
                Directory.CreateDirectory(baseWorkingDir);
            }

            //export RAD for DAYSIM
            if (!Directory.Exists(baseWorkingDir + @"Rad\"))
            {
                Directory.CreateDirectory(baseWorkingDir + @"Rad\");
            }

            if (!Directory.Exists(baseWorkingDir + @"Output\"))
            {
                Directory.CreateDirectory(baseWorkingDir + @"Output\");
            }

            // User geometry data here

            string radMat = @"
        void plastic Generic_20
        0
        0
        5 0.2 0.2 0.2 0 0
        ";

            string radMatBlack = @"
        void plastic Black
        0
        0
        5 0 0 0 0 0
        ";
            Mesh daysimMesh = new Mesh();
            daysimMesh.Append(BuildingGeometry);

            // Todo: add ground plane to the above mesh

            // Make sure this understands userdata

            RadianceFiles.MeshProc(daysimMesh, baseWorkingDir + @"\scene.rad", "Generic_20", radMat);
            RadianceFiles.MeshProc(daysimMesh, baseWorkingDir + @"\sceneBlack.rad", "Black", radMatBlack);

            // Write Probes

            RadianceFiles.writePTS(baseWorkingDir + @"\sensors.pts", probes);

            // Weather
            var weaname = RadianceFiles.Epw2Wea(weather.epwFilePath, baseWorkingDir + @"\Rad\Output");

            // Sky
            //Skies.Write(baseWorkingDir + @"Rad\skyglow.rad", skySubDivDiff);
            Skies.Write(baseWorkingDir + @"Rad\skyglow" + (skySubDivDir - 1) + ".rad", (skySubDivDir - 1));
            Skies.Write(baseWorkingDir + @"Rad\skyglow" + (skySubDivDiff - 1) + ".rad", (skySubDivDiff - 1));

            //Utilities.StartProcess.StartProcessCMDNT(arg, false, true, false, true, probingComplete);

            if (run)
            {
                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.WorkingDirectory = baseWorkingDir + @"\Rad\";
                    process.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                    process.StartInfo.RedirectStandardError = true;

                    // Redirects the standard input so that commands can be sent to the shell.
                    process.StartInfo.RedirectStandardInput = true;

                    // Runs the specified command and exits the shell immediately.
                    //process.StartInfo.Arguments = @"/c ""dir""";

                    //process.OutputDataReceived += ProcessOutputDataHandler;
                    //process.ErrorDataReceived += ProcessErrorDataHandler;

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Send a directory command and an exit command to the shell
                    process.StandardInput.WriteLine(CommandLineArgsNew(RadianceDir, baseWorkingDir, probes.Count, weaname, weather.epwFilePath, 3, 5000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1));
                    process.StandardInput.WriteLine("exit");

                    process.WaitForExit();
                }
            }

            this.command = CommandLineArgsNew(RadianceDir, baseWorkingDir, probes.Count, weaname, weather.epwFilePath, 3, 5000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1);

            this.dcill = LoadDDSIll(baseWorkingDir + @"\Output\annualR_dc.ill");
            this.dcdill = LoadDDSIll(baseWorkingDir + @"\Output\annualR_dcd.ill");
            this.dirill = LoadDDSIll(baseWorkingDir + @"\Output\annual_dir.ill");
            this.totalIll = LoadDDSIll(baseWorkingDir + @"\Output\annual_total.ill");
        }

        private static double[][] LoadDDSIll(string illFileName)
        {
            // [x][] time
            // [][x] points
            //string[] illLines = System.IO.File.ReadAllLines(illFileName);
            //return illLines.Select(l => Array.ConvertAll<string, double>(l.Split(new[] { ' ' }).Skip(4).ToArray(), Double.Parse)).ToArray();

            string[] lines = System.IO.File.ReadAllLines(illFileName).Skip(7).ToArray();
            double[][] values = new double[lines.Length][];

            for (int h = 0; h < lines.Length; h++)
            {
                string[] hourData = lines[h].Split(' ').Skip(4).ToArray();
                double[] hourDataDouble = Array.ConvertAll<string, double>(hourData, Double.Parse);
                values[h] = hourDataDouble;
            }
            return values;
        }

        public static string CommandLineArgsNew(string RadianceDir, string baseWorkingDir, int sensorCnt, string weaname, string epwpath, int ab, int ad, SkySubdivision diffSky, SkySubdivision dirSky, int n)
        {
            int skysubdiv = (int)diffSky;
            int skysubdivdirect = (int)diffSky;

            string command = @"

        cd " + baseWorkingDir + @"

        REM ###################################
        REM Pre
        REM ###################################

        REM Convert epw to wea tape
        REM -----------------------------------
        epw2wea """ + epwpath + @""" ""output/" + weaname + @".wea""

        REM Make the OCTREE
        REM -----------------------------------
        REM takes an array of rad files and combines them into one octree
        oconv scene.rad > output/scene.oct
        oconv sceneBlack.rad > output/sceneBlack.oct

        REM ###################################
        REM 1 Perform an annual daylight coefficient simulation.
        REM ###################################

        REM Create daylight coefficient matrix for R1 sky (145patches)
        REM -----------------------------------
        REM -I+ denotes that the simulation is being performed for calculating irradiance instead of radiance
        REM The 48 in -y 48 is equal to the number of lines in the file sensors.pts
        REM The number of processors assigned for the simulation can be set with -n 4
        rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @"  -ad " + ad + @" -n " + n + @" - Rad/skyglowR" + (skysubdiv) + @".rad -i output/scene.oct < sensors.pts > output/dc_r" + skysubdiv + @".mtx

        REM Generates SkyVector for whole year.
        REM-----------------------------------
        REM -m controls the sky subdivision
        REM Use -O1 to switch to solar rad
        REM The −d option may be used to produce a sun -only matrix, with no sky contributions. Alternatively, the −s option may be used to exclude any direct solar component from the output.
        gendaymtx -m " + skysubdiv + @" -O1 ""output/" + weaname + @".wea"" > ""output/" + weaname + @".smx""

        REM Create Illum
        REM Illuminace Weights == 47.4 119.9 11.6  // For Radiation 0.265 0.670 0.065 ???
        REM -----------------------------------
        dctimestep output/dc_r" + skysubdiv + @".mtx ""output/" + weaname + @".smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 - > ""output/annualR_dc.ill""

        REM ###################################
        REM 2 Perform an annual direct-only daylight coefficients simulation.
        REM ###################################

        rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @" -ad " + ad + @" -n " + n + @" - Rad/skyglowR" + (skysubdiv) + @".rad -i output/sceneBlack.oct < sensors.pts > output/dcd_r" + skysubdiv + @".mtx

        gendaymtx -m " + skysubdiv + @" -O1 -d ""output/" + weaname + @".wea"" > ""output/" + weaname + @"d.smx""

        dctimestep output/dcd_r" + skysubdiv + @".mtx ""output/" + weaname + @"d.smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 -> ""output/annualR_dcd.ill""

        REM ###################################
        REM 3 Perform an annual sun-coefficients simulation.
        REM ###################################
        echo void light solar 0 0 3 1e6 1e6 1e6 > output/suns.rad
        REM Create solar discs and corresponding modifiers for 2305 suns corresponding to a Reinhart MF:4 subdivision.
        REM 0.533 solar disc size as angle
        cnt " + (144 * skysubdivdirect * skysubdivdirect + 1) + @" | rcalc -e MF:4 -f ""C:\DIVA\Radiance\lib\reinsrc.cal"" -e Rbin=recno -o ""solar source sun 0 0 4 ${Dx} ${Dy} ${Dz} 0.533"" >> ""output/suns.rad""

        REM Put suns in scene...
        oconv sceneBlack.rad output/suns.rad > output/sceneBlackSuns.oct

        REM Calculate illuminance sun coefficients for illuminance calculations.
        rcontrib -I+ -ab 1 -y " + sensorCnt + @" -n 16 -ad 256 -lw 1.0e-3 -dc 1 -dt 0 -dj 0 -faf -e MF:" + skysubdivdirect + @" -f """ + RadianceDir + @"\lib\reinhart.cal"" -b rbin -bn Nrbins -m solar ""output/sceneBlackSuns.oct"" < sensors.pts > ""output/cdsDDS.mtx""

        REM - 5 option indicates 5phase method mode - solar disc angele must follow that input
        REM The -d option in the SMX messes it all up-- you can't include -d and -5 together.
        gendaymtx -5 0.533 -m " + skysubdivdirect + @" -O1 ""output/" + weaname + @".wea"" > ""output/sunM" + skysubdivdirect + @".smx""

        dctimestep ""output/cdsDDS.mtx"" ""output/sunM" + skysubdivdirect + @".smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 - > ""output/annual_dir.ill""

        REM ###################################
        REM 4 Combine Results
        REM ###################################
        rmtxop ""output/annualR_dc.ill"" + -s -1 ""output/annualR_dcd.ill"" + ""output/annual_dir.ill"" > ""output/annual_total.ill""

        pause

        ";
            return command;
        }

        public class Sky

        {
            private double Sigma = 5.670374419e-8;

            public double[] Emissivity;

            public double[] Temp;

            public double[] HZ_IR;

            private double Kelvin = 273.15;

            public Sky(double[] T_dew, double[] T_DryBulb, double[] SkyCover, double[] RelHum, double SourceEmissivity = 1)
            {
                var numberOfHours = T_DryBulb.Length;

                this.Emissivity = new double[numberOfHours];
                this.Temp = new double[numberOfHours];
                this.HZ_IR = new double[numberOfHours];

                for (int h = 0; h < numberOfHours; h++)
                {
                    this.Emissivity[h] = CalcEmissivity(T_dew[h], SkyCover[h]);

                    // this.Emissivity[h] = CalcEmissivityEnergyPlus(3, T_dew[h], T_DryBulb[h],SkyCover[h],RelHum[h]);
                    this.HZ_IR[h] = CalcHZ_IR(this.Emissivity[h], this.Sigma, T_DryBulb[h]);
                    this.Temp[h] = CalcTemp(HZ_IR[h], SourceEmissivity);
                }
            }

            private double CalcHZ_IR(double Emissivity, double Sigma, double T_drybulb)
            {
                return Emissivity * Sigma * Math.Pow((T_drybulb + 273.15), 4);
            }

            private double CalcEmissivity(double T_dew, double N)
            {
                // N = SkyCover
                return (0.787 + 0.764 * Math.Log((T_dew + Kelvin) / Kelvin)) * (1 + (0.0224 * N) - (0.0035 * Math.Pow(N, 2)) + (0.00028 * Math.Pow(N, 3)));
            }

            private double CalcEs(double T_celcius)
            {
                //!~ **********************************************
                //!~calculates saturation vapour pressure over water in hPa for input air temperature(ta) in celsius according to:
                //!~Hardy, R.; ITS-90 Formulations for Vapor Pressure, Frostpoint Temperature, Dewpoint Temperature and Enhancement Factors in the Range -100 to 100 °C;
                //!~Proceedings of Third International Symposium on Humidity and Moisture; edited by National Physical Laboratory(NPL), London, 1998, pp. 214-221
                //!~http://www.thunderscientific.com/tech_info/reflibrary/its90formulas.pdf (retrieved 2008-10-01)

                // es = saturation vapour pressure in Pa // T is temperature in K // g is list of
                // coefficients for curve fit

                double T_kelvin; //int I;
                double[] g = {
    -2.8365744E3,
    -6.028076559E3, 1.954263612E1,
    -2.737830188E-2, 1.6261698E-5, 7.0229056E-10,
    -1.8680009E-13, 2.7150305 };

                T_kelvin = T_celcius + 273.15; //! air temp in K double
                var es = g[7] * Math.Log(T_kelvin);

                // do i=0,6
                for (int i = 0; i < 6; i++)
                {
                    es = es + g[i] * Math.Pow(T_kelvin, (i - 2));
                }

                //end do

                es = Math.Exp(es) * 0.01; //! *0.01: convert Pa to hPa

                return es;
            }

            private double CalcEmissivityEnergyPlus(int ESkyCalcType, double OSky, double DryBulb, double DewPoint, double RelHum)
            {
                // Calculate Sky Emissivity
                // References:
                // M. Li, Y. Jiang and C. F. M. Coimbra,
                // "On the determination of atmospheric longwave irradiance under all-sky conditions,"
                // Solar Energy 144, 2017, pp. 40–48,
                // G. Clark and C. Allen, "The Estimation of Atmospheric Radiation for Clear and
                // Cloudy Skies," Proc. 2nd National Passive Solar Conference (AS/ISES), 1978, pp. 675-678.

                // var Pvsk = 6.105 * Math.Exp((17.27 * ((double)DryBulb + 273.15) - 4717.03) / (237.7 + (double)DryBulb));

                var TKelvin = 273.15;

                var ESky = 0.0;
                if (ESkyCalcType == 1)
                {
                    double PartialPress = RelHum * CalcEs(DryBulb) * 0.01;
                    ESky = 0.618 + 0.056 * Math.Pow(PartialPress, 0.5);
                }
                else if (ESkyCalcType == 2)
                {
                    double PartialPress = RelHum * CalcEs(DryBulb) * 0.01;
                    ESky = 0.685 + 0.000032 * PartialPress * Math.Exp(1699 / (DryBulb + TKelvin));
                }
                else if (ESkyCalcType == 3)
                {
                    double TDewC = new List<double>() { DryBulb, DewPoint }.Min();
                    ESky = 0.758 + 0.521 * (TDewC / 100) + 0.625 * Math.Pow((TDewC / 100), 2);
                }
                else
                {
                    ESky = 0.787 + 0.764 * Math.Log((new List<double>() { DryBulb, DewPoint }.Min() + TKelvin) / TKelvin);
                }
                ESky = ESky * (1 + (0.0224 * OSky) - (0.0035 * Math.Pow(OSky, 2)) + (0.00028 * Math.Pow(OSky, 3)));
                return ESky;
            }

            private double CalcTemp(double HZ_IR, double SourceEmissivity)
            {
                return Math.Pow((HZ_IR / (SourceEmissivity * this.Sigma)), 0.25) - Kelvin;
            }
        }

        /*public static string CommandLineArgs(string RadianceDir, string baseWorkingDir, int sensorCnt, string weaname, int ab, int ad, SkySubdivision diffSky, SkySubdivision dirSky, int n)
        {
          int skysubdiv = (int) diffSky;
          int skysubdivdirect = (int) diffSky;

          string command = @"

            oconv scene.rad > output/scene.oct

            oconv sceneBlack.rad > output/sceneBlack.oct

            REM ###################################
            REM 1 Perform an annual daylight coefficient simulation.
            REM ###################################

            rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @" -ad " + ad + @" -n " + n + @" - skyglow.rad" + @" -i output/scene.oct<sensors.pts> output/dc_r" + skysubdiv + @".mtx

            gendaymtx -m " + skysubdiv + @" -O1 output/" + weaname + @".wea > output/total.smx

            dctimestep output/dc_r" + skysubdiv + @".mtx output/total.smx | rmtxop -fa -t -c 0.265 0.670 0.065 - > output/annual_total.ill

            REM ###################################
            REM 2 Perform an annual direct-only daylight coefficients simulation.
            REM ###################################

            rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab 1 -ad " + ad + @" -n " + n + @" - skyglow.rad -i output/sceneBlack.oct<sensors.pts> output/dcd_r" + skysubdiv + @".mtx

            gendaymtx -m " + skysubdiv + @" -O1 -d output/" + weaname + @".wea > output/dirOnly.smx

            dctimestep output/dcd_r" + skysubdiv + @".mtx output/dirOnly.smx | rmtxop -fa -t -c 0.265 0.670 0.065 - > output/annualR_dcd.ill

            REM ###################################
            REM 3 Perform an annual sun-coefficients simulation.
            REM ###################################

            echo void light solar 0 0 3 1e6 1e6 1e6 > output/suns.rad

            cnt " + (144 * skysubdivdirect * skysubdivdirect + 1) + @" | rcalc -e MF:" + skysubdivdirect + @" -f " + RadianceDir + @"\lib\reinsrc.cal -e Rbin = recno -o ""solar source sun 0 0 4 ${ Dx} ${ Dy} ${ Dz} 0.533"" >> output/suns.rad

            oconv sceneBlack.rad output/suns.rad > output/sceneBlackSuns.oct

            rcontrib -I + -ab 1 -y " + sensorCnt + @" - n " + n + @" -ad 256 -lw 1.0e-3 -dc 1 -dt 0 -dj 0 -faf -e MF:" + skysubdivdirect + @" -f " + RadianceDir + @"\lib\reinhart.cal -b rbin -bn Nrbins -m solar output/sceneBlackSuns.oct < sensors.pts > output/cdsDDS.mtx

            gendaymtx -5 0.533 -m " + skysubdivdirect + @" -O1 output/" + weaname + @".wea > output/sunM" + skysubdivdirect + @".smx

            dctimestep output/cdsDDS.mtx output/sunM" + skysubdivdirect + @".smx | rmtxop -fa -t -c 0.265 0.670 0.065 - > output/annual_dir.ill

            REM ###################################
            REM 4 Combine Results
            REM ###################################

            rmtxop output/annual_total.ill + -s -1 output/annualR_dcd.ill > output/annual_diff.ill

            REM rmtxop annual_diff.ill + output/annualR_dir.ill > output/annual_total.ill

            ";
          return command;
        }*/
    }
}