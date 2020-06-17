using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EddyLib.Radiance
{
    public class TwoPhaseDDS
    {
        public double[][] dcill;

        public double[][] dcdill;

        public double[][] dirill;

        public double[][] totalIll;

        public string command;

        private readonly string fileName = @"TwoPhaseDDS";

        private readonly string fileNameCSVExtension = ".csv";

        private readonly string fileNameBinExtension = ".bin";

        private readonly string del = "_";

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        private string TwoPhaseDDSFolder = @"\TwoPhaseDDS\";

        public TwoPhaseDDS(string baseWorkingDir, Mesh BuildingGeometry, List<Point3d> probes, Weather weather, bool recalc, EventHandler eh, string RadianceDir = @"C:\Program Files\Radiance")

        {
            string csvDDS = Path.Combine(baseWorkingDir + fileName + del + fileNameCSVExtension);
            string binDDS = Path.Combine(baseWorkingDir + fileName + del + fileNameBinExtension);

            var dirs = new List<String>() { baseWorkingDir + TwoPhaseDDSFolder, baseWorkingDir, baseWorkingDir + @"Rad\", baseWorkingDir + @"Output\" };

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }

            if (File.Exists(binDDS) && !recalc)
            {
                try
                {
                    this.totalIll = LoadDDSIll(TwoPhaseDDSFolder + @"totalIll.ill");
                    this.resultPrecalculated = true;
                    this.wrongNumberOfProbes = false;
                }
                catch (Exception e)
                {
                    this.resultPrecalculated = false;
                    this.wrongNumberOfProbes = true;
                    throw e;
                }
            }
            else
            {
                var files = new List<string>()
                {
                     csvDDS,
                     binDDS
                };

                foreach (string s in files)
                {
                    if (File.Exists(s))
                    {
                        File.Delete(s);
                    }
                }

                RunDDS(baseWorkingDir, TwoPhaseDDSFolder, BuildingGeometry, probes, weather, recalc, RadianceDir);

                RadianceFiles.writeBin(baseWorkingDir + TwoPhaseDDSFolder + @"\totalIll.bin", this.totalIll);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        protected void RunDDS(string baseWorkingDir, string subfolder, Mesh BuildingGeometry, List<Point3d> probes, Weather weather, bool run, string RadianceDir = @"C:\Program Files\Radiance")
        {
            var numberOfProbes = probes.Count;

            //var skySubDivDiff = SkySubdivision.r1;
            var skySubDivDiff = SkySubdivision.r2;
            var skySubDivDir = SkySubdivision.r4;

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

            this.command = CommandLineArgsNew(RadianceDir, baseWorkingDir, probes.Count, weaname, weather.epwFilePath, 3, 5000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1);

            //Utilities.StartProcess.StartProcessCMDNT(arg, false, true, false, true, probingComplete);

            if (run == true)
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

            // this.dcill = LoadDDSIll(baseWorkingDir + @"\Output\annualR_dc.ill");
            //this.dcdill = LoadDDSIll(baseWorkingDir + @"\Output\annualR_dcd.ill");
            //this.dirill = LoadDDSIll(baseWorkingDir + @"\Output\annual_dir.ill");

            //if (load)
            //{
            this.totalIll = LoadDDSIll(baseWorkingDir + @"\Output\annual_total.ill");

            //}

            //  this.Values = new double[8760, numberOfProbes];

            //  this.dcill = new double[8760, numberOfProbes];
            //  this.dcdill = new double[8760, numberOfProbes];
            //  this.dirill = new double[8760, numberOfProbes];
            //  this.totalIll = new double[8760, numberOfProbes];

            /*

            System.Threading.Tasks.Parallel.For(0, 20, h =>
            {
            for (int p = 0; p < ; p++)
            {
            //this.Values[h, p] = GetMRTForPointViaKessling(weather, h, DiffRad[h][p], DirRad[h][p])[0];

            // this.dcill[h, p] = Ldcill[h][p];
            // this.dcdill[h, p] = Ldcdill[h][p];
            // this.dirill[h, p] = Ldirill[h][p];
            //  this.totalIll[h, p] = LtotalIll[h][p];
            }
            });

            */
        }

        private static double[][] LoadDDSIll(string illFileName) // total illuminance data
        {
            // [x][] time
            // [][x] points
            string[] illLines = System.IO.File.ReadAllLines(illFileName).Skip(9).ToArray();
            return illLines.Select(l => Array.ConvertAll<string, double>(l.Split(new[] { ' ' }).Skip(1).ToArray(), Double.Parse)).ToArray();

            //string[] lines = System.IO.File.ReadAllLines(illFileName);
            //double[][] values = new double[lines.Length][];

            /* for (int h = 0; h < lines.Length; h++)
            {
            string[] hourData = lines[h].Split(' ').Skip(4).ToArray();
            double[] hourDataDouble = Array.ConvertAll<string, double>(hourData, Double.Parse);
            values[h] = hourDataDouble;
            }
            return values;*/
        }

        public string CommandLineArgsNew(string RadianceDir, string baseWorkingDir, int sensorCnt, string weaname, string epwpath, int ab, int ad, SkySubdivision diffSky, SkySubdivision dirSky, int n)
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
        cnt " + (144 * skysubdivdirect * skysubdivdirect + 1) + @" | rcalc -e MF:4 -f " + RadianceDir + @"\lib\reinsrc.cal"" -e Rbin=recno -o ""solar source sun 0 0 4 ${Dx} ${Dy} ${Dz} 0.533"" >> ""output/suns.rad""

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
    }
}