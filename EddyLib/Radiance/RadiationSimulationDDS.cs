using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace EddyLib.Radiance
{

    [DataContract]
    public class RadiationSimulationDDSResult
    {
        public RadiationSimulationDDSResult(List<Mesh> meshes, double[][] totalRad)
        {
            this.AnalysisMeshes = meshes;
            this.TotalRad = totalRad;
        }



        [DataMember]
        List<Mesh> AnalysisMeshes { get; set; }
        [DataMember]
        public double[][] TotalRad { get; set; }

        //public double[][] DiffRad;
        //public double[][] DirRad;


        public string ToBson()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BsonDataWriter datawriter = new BsonDataWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(datawriter, this);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        public static RadiationSimulationDDSResult FromBson(string base64data)
        {
            byte[] data = Convert.FromBase64String(base64data);

            using (MemoryStream ms = new MemoryStream(data))
            using (BsonDataReader reader = new BsonDataReader(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<RadiationSimulationDDSResult>(reader);
            }
        }

        //public static string ToBson<T>(T value)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    using (BsonDataWriter datawriter = new BsonDataWriter(ms))
        //    {
        //        JsonSerializer serializer = new JsonSerializer();
        //        serializer.Serialize(datawriter, value);
        //        return Convert.ToBase64String(ms.ToArray());
        //    }

        //}
        //public static T FromBson<T>(string base64data)
        //{
        //    byte[] data = Convert.FromBase64String(base64data);

        //    using (MemoryStream ms = new MemoryStream(data))
        //    using (BsonDataReader reader = new BsonDataReader(ms))
        //    {
        //        JsonSerializer serializer = new JsonSerializer();
        //        return serializer.Deserialize<T>(reader);
        //    }
        //}
    }

    public class RadiationSimulationDDS
    {

        private static double[][] LoadDDSIll(string illFileName) // total illuminance data
        {
            // [x][] time
            // [][x] points
            string[] illLines = System.IO.File.ReadAllLines(illFileName).Skip(9).ToArray();
            return illLines.Select(l => Array.ConvertAll<string, double>(l.Split(new[] { ' ' }).Skip(1).ToArray(), Double.Parse)).ToArray();
        }
        private string CommandLineArgsNew(string RadianceDir, string baseWorkingDir, int sensorCnt, string weaname, string epwpath, int ab, int ad, SkySubdivision diffSky, SkySubdivision dirSky, int n)
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
        epw2wea """ + epwpath + @""" ""Rad/output/" + weaname + @".wea""

        REM Make the OCTREE
        REM -----------------------------------
        REM takes an array of rad files and combines them into one octree
        oconv Rad/scene.rad > Rad/output/scene.oct
        oconv Rad/sceneBlack.rad > Rad/output/sceneBlack.oct

        REM ###################################
        REM 1 Perform an annual daylight coefficient simulation.
        REM ###################################

        REM Create daylight coefficient matrix for R1 sky (145patches)
        REM -----------------------------------
        REM -I+ denotes that the simulation is being performed for calculating irradiance instead of radiance
        REM The 48 in -y 48 is equal to the number of lines in the file sensors.pts
        REM The number of processors assigned for the simulation can be set with -n 4
        rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @"  -ad " + ad + @" -n " + n + @" - Rad/skyglowR" + (skysubdiv) + @".rad -i Rad/output/scene.oct < Rad/sensors.pts > Rad/Output/dc_r" + skysubdiv + @".mtx

        REM Generates SkyVector for whole year.
        REM-----------------------------------
        REM -m controls the sky subdivision
        REM Use -O1 to switch to solar rad
        REM The −d option may be used to produce a sun -only matrix, with no sky contributions. Alternatively, the −s option may be used to exclude any direct solar component from the output.
        gendaymtx -m " + skysubdiv + @" -O1 ""Rad/output/" + weaname + @".wea"" > ""Rad/output/" + weaname + @".smx""

        REM Create Illum
        REM Illuminace Weights == 47.4 119.9 11.6  // For Radiation 0.265 0.670 0.065 ???
        REM -----------------------------------
        dctimestep Rad/output/dc_r" + skysubdiv + @".mtx ""Rad/output/" + weaname + @".smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 - > ""Rad/output/annualR_dc.ill""

        REM ###################################
        REM 2 Perform an annual direct-only daylight coefficients simulation.
        REM ###################################

        rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @" -ad " + ad + @" -n " + n + @" - Rad/skyglowR" + (skysubdiv) + @".rad -i Rad/output/sceneBlack.oct < Rad/sensors.pts > Rad/output/dcd_r" + skysubdiv + @".mtx

        gendaymtx -m " + skysubdiv + @" -O1 -d ""Rad/output/" + weaname + @".wea"" > ""Rad/output/" + weaname + @"d.smx""

        dctimestep Rad/output/dcd_r" + skysubdiv + @".mtx ""Rad/output/" + weaname + @"d.smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 -> ""Rad/output/annualR_dcd.ill""

        REM ###################################
        REM 3 Perform an annual sun-coefficients simulation.
        REM ###################################

        echo void light solar 0 0 3 1e6 1e6 1e6 > Rad/output/suns.rad

        REM Create solar discs and corresponding modifiers for 2305 suns corresponding to a Reinhart MF:4 subdivision.
        REM 0.533 solar disc size as angle
        cnt " + (144 * skysubdivdirect * skysubdivdirect + 1) + @" | rcalc -e MF:4 -f " + RadianceDir + @"\lib\reinsrc.cal"" -e Rbin=recno -o ""solar source sun 0 0 4 ${Dx} ${Dy} ${Dz} 0.533"" >> ""Rad/output/suns.rad""

        REM Put suns in scene...
        oconv Rad/sceneBlack.rad Rad/output/suns.rad > Rad/output/sceneBlackSuns.oct

        REM Calculate illuminance sun coefficients for illuminance calculations.
        rcontrib -I+ -ab 1 -y " + sensorCnt + @" -n 16 -ad 256 -lw 1.0e-3 -dc 1 -dt 0 -dj 0 -faf -e MF:" + skysubdivdirect + @" -f """ + RadianceDir + @"\lib\reinhart.cal"" -b rbin -bn Nrbins -m solar ""Rad/output/sceneBlackSuns.oct"" < Rad/sensors.pts > ""Rad/output/cdsDDS.mtx""

        REM - 5 option indicates 5phase method mode - solar disc angele must follow that input
        REM The -d option in the SMX messes it all up-- you can't include -d and -5 together.
        gendaymtx -5 0.533 -m " + skysubdivdirect + @" -O1 ""Rad/output/" + weaname + @".wea"" > ""Rad/output/sunM" + skysubdivdirect + @".smx""

        dctimestep ""Rad/output/cdsDDS.mtx"" ""Rad/output/sunM" + skysubdivdirect + @".smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 - > ""Rad/output/annual_dir.ill""

        REM ###################################
        REM 4 Combine Results
        REM ###################################
        rmtxop ""Rad/output/annualR_dc.ill"" + -s -1 ""Rad/output/annualR_dcd.ill"" + ""Rad/output/annual_dir.ill"" > ""Rad/output/annual_total.ill""

        pause

        ";
            return command;
        }


        public string command;

        public string ProjectName = @"TwoPhaseDDS";

        private string TwoPhaseDDSFolder = @"\TwoPhaseDDS\";

        public string BaseWorkingDir = "";



        Mesh BuildingGeometry;
        List<Mesh> ProbeMeshes;
        List<Point3d> Probes;
        Weather Weather;
        public RadiationSimulationDDS(string filename, string baseWorkingDir, Mesh buildingGeometry, List<Mesh> probe_meshes, Weather weather)
        {
            ProjectName = filename;
            BaseWorkingDir = baseWorkingDir;

            BuildingGeometry = buildingGeometry;
            ProbeMeshes = probe_meshes;
            Probes = new List<Point3d>();
            foreach (var m in probe_meshes)
            {
                foreach (var p in m.Vertices)
                {
                    Probes.Add(p);
                }
            }

            Weather = weather;



            var dirs = new List<String>() { baseWorkingDir + TwoPhaseDDSFolder, baseWorkingDir, baseWorkingDir + @"Rad\" };

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }

        public RadiationSimulationDDSResult RunDDS(bool run)
        {
            var numberOfProbes = this.Probes.Count;

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
            daysimMesh.Append(this.BuildingGeometry);

            // Todo: add ground plane to the above mesh

            // Make sure this understands userdata
            RadianceFiles.MeshProc(daysimMesh, this.BaseWorkingDir + @"\Rad\scene.rad", "Generic_20", radMat);
            RadianceFiles.MeshProc(daysimMesh, this.BaseWorkingDir + @"\Rad\sceneBlack.rad", "Black", radMatBlack);

            // Write Probes
            RadianceFiles.writePTS(this.BaseWorkingDir + @"\Rad\sensors.pts", this.Probes);

            // Weather
            var weaname = RadianceFiles.Epw2Wea(this.Weather.epwFilePath, this.BaseWorkingDir + @"\Rad\Output");

            // Sky
            //Skies.Write(baseWorkingDir + @"Rad\skyglow.rad", skySubDivDiff);
            Skies.Write(this.BaseWorkingDir + @"Rad\skyglow" + (skySubDivDir - 1) + ".rad", (skySubDivDir - 1));
            Skies.Write(this.BaseWorkingDir + @"Rad\skyglow" + (skySubDivDiff - 1) + ".rad", (skySubDivDiff - 1));

            this.command = CommandLineArgsNew(DefaultDirectoriesAndPaths.RadianceDir, this.BaseWorkingDir, this.Probes.Count, weaname, this.Weather.epwFilePath, 3, 5000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1);

            //Utilities.StartProcess.StartProcessCMDNT(arg, false, true, false, true, probingComplete);

            if (run == true)
            {
                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.WorkingDirectory = this.BaseWorkingDir + @"\Rad\";
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
                    process.StandardInput.WriteLine(CommandLineArgsNew(DefaultDirectoriesAndPaths.RadianceDir, this.BaseWorkingDir, this.Probes.Count, weaname, this.Weather.epwFilePath, 3, 5000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1));
                    process.StandardInput.WriteLine("exit");

                    process.WaitForExit();
                }


                var totalIll = LoadDDSIll(this.BaseWorkingDir + @"Rad\Output\annual_total.ill");
                var result = new RadiationSimulationDDSResult(this.ProbeMeshes, totalIll);
                var bson = result.ToBson();

                Console.WriteLine();
                File.WriteAllText(this.BaseWorkingDir + "/" + this.ProjectName + ".Radiation.bin", bson);

                return result;
            }

            // this.dcill = LoadDDSIll(baseWorkingDir + @"\Output\annualR_dc.ill");
            //this.dcdill = LoadDDSIll(baseWorkingDir + @"\Output\annualR_dcd.ill");
            //this.dirill = LoadDDSIll(baseWorkingDir + @"\Output\annual_dir.ill");

            //if (load)
            //{

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

            return null;

        }

    }
}