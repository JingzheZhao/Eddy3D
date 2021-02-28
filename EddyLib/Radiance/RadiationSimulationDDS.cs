using EddyLib.UI;
using Medallion.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace EddyLib.Radiance
{

    [DataContract]
    public class RadiationSimulationDDSResult
    {
        public RadiationSimulationDDSResult(List<Mesh> meshes, float[][] totalRad)
        {
            this.AnalysisMeshes = meshes;
            this.TotalRad = totalRad;
        }



        [DataMember]
        List<Mesh> AnalysisMeshes { get; set; }
        [DataMember]
        public float[][] TotalRad { get; set; }

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

        private static float[][] LoadDDSIll(string illFileName) // total illuminance data
        {
            // [x][] time
            // [][x] points
            string[] illLines = System.IO.File.ReadAllLines(illFileName).Skip(9).ToArray();
            return illLines.Select(l => Array.ConvertAll<string, float>(l.Split(new[] { ' ' }).Skip(1).ToArray(), float.Parse)).ToArray();
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



        public string ProjectName = "";

        //private string TwoPhaseDDSFolder = @"\TwoPhaseDDS\";

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



            var dirs = new List<String>() { baseWorkingDir, baseWorkingDir + @"\Rad\" };

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }

        public RadiationSimulationDDSResult RunDDS(bool run, CancellationToken ct)
        {

            double pct = 0;
            double steps = 15;
            double stepCnt = 0;

            var numberOfProbes = this.Probes.Count;

            var skySubDivDiff = SkySubdivision.r1;
            //var skySubDivDiff = SkySubdivision.r2;
            var skySubDivDir = SkySubdivision.r4;
            int skysubdivdiffuse = 1;
            int skysubdivdirect = 4;
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



            Console.WriteLine("Writing files...");
            // Make sure this understands userdata
            RadianceFiles.MeshProc(daysimMesh, this.BaseWorkingDir + @"\Rad\scene.rad", "Generic_20", radMat);
            RadianceFiles.MeshProc(daysimMesh, this.BaseWorkingDir + @"\Rad\sceneBlack.rad", "Black", radMatBlack);

            // Write Probes
            RadianceFiles.writePTS(this.BaseWorkingDir + @"\Rad\sensors.pts", this.Probes);

            // Weather
            var weaname = RadianceFiles.Epw2Wea(this.Weather.epwFilePath, this.BaseWorkingDir + @"\Rad\Output");

            // Sky
            Skies.Write(this.BaseWorkingDir + @"\Rad\skyglow" + (skySubDivDir) + ".rad", (skySubDivDir));
            Skies.Write(this.BaseWorkingDir + @"\Rad\skyglow" + (skySubDivDiff) + ".rad", (skySubDivDiff));




            //var command = CommandLineArgsNew(DefaultDirectoriesAndPaths.RadianceDir, this.BaseWorkingDir, this.Probes.Count, weaname, this.Weather.epwFilePath, 3, 5000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1);




            if (run == true)
            {

                // -----------------------------
                // 1 Convert epw to wea tape
                // -----------------------------
                Console.WriteLine("Convert epw to wea tape...");
                //string epwin = AddQuotesIfRequired();
                //string weaout = AddQuotesIfRequired();
                var epw2wea = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\epw2wea", @"""" + this.Weather.epwFilePath + @""" ""Rad/output/" + weaname + @".wea""");
                epw2wea.Wait();

                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 2 Make Octree
                // -----------------------------
                Console.WriteLine("Make the Octree...");
                string radin = (this.BaseWorkingDir + @"\Rad\scene.rad");
                string octout = (this.BaseWorkingDir + @"\Rad\output\scene.oct");
                var oconv = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\oconv", new[] { radin },
                    options => options.WorkingDirectory(this.BaseWorkingDir)).RedirectTo(new FileInfo(octout));
                oconv.Wait();
                if (!oconv.Result.Success)
                {
                    Debug.WriteLine($"oconv command failed with exit code {oconv.Result.ExitCode}: {oconv.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));




                // -----------------------------
                // 3 Create daylight coefficient matrix
                // -----------------------------
                Console.WriteLine("Create daylight coefficient matrix...");
                // -I+ denotes that the simulation is being performed for calculating irradiance instead of radiance
                // The 48 in -y 48 is equal to the number of lines in the file sensors.pts
                // The number of processors assigned for the simulation can be set with - n 4
                // rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @"  -ad " + ad + @" -n " + n + @" - Rad/skyglowR" + (skysubdiv) + @".rad -i Rad/output/scene.oct < Rad/sensors.pts > Rad/Output/dc_r" + skysubdiv + @".mtx
                int ab = 2;
                int ad = 1000;
                int n = (Environment.ProcessorCount - 1);
                int sensorCnt = this.Probes.Count;

                //string skyglowrad = (this.BaseWorkingDir + @"\Rad\skyglow" + skySubDivDiff + @".rad");
                //string inputoct = (this.BaseWorkingDir + @"\Rad\output\scene.oct");
                //string ptsin = (this.BaseWorkingDir + @"\Rad\sensors.pts");
                //string mtxout = (this.BaseWorkingDir + @"\Rad\Output\dc_" + skySubDivDiff + @".mtx");

                string skyglowrad = (@"Rad\skyglow" + skySubDivDiff + @".rad");
                string inputoct = (@"Rad\output\scene.oct");
                string ptsin = (@"Rad\sensors.pts");
                string mtxout = (@"Rad\Output\dc_" + skySubDivDiff + @".mtx");

                //these dont works and I have no idea why
                #region whydoesthisnotwork

                ////////V1
                //////var rfluxmtx = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx.exe",
                //////   new[] { "-I+ -y " + sensorCnt + " -lw 0.0001 -ab " + ab + @" -ad " + ad + " -n " + n + @" - " + skyglowrad +" -i " + inputoct + " < " + ptsin + " > " + mtxout });
                //////rfluxmtx.Wait();

                //////if (!rfluxmtx.Result.Success)
                //////{
                //////    Debug.WriteLine($"1 command failed with exit code {rfluxmtx.Result.ExitCode}: {rfluxmtx.Result.StandardError}");
                //////}

                ////////V2
                //////var rfluxmtx2 = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx.exe",
                //////   new[] { "-I+ -y " + sensorCnt + " -lw 0.0001 -ab " + ab + @" -ad " + ad + " -n " + n + @" - " + skyglowrad +  " -i " + inputoct })
                //////    .RedirectFrom(new FileInfo(ptsin)).RedirectTo(new FileInfo(mtxout));
                //////rfluxmtx2.Wait();
                //////if (!rfluxmtx2.Result.Success)
                //////{
                //////    Debug.WriteLine($"2 command failed with exit code {rfluxmtx2.Result.ExitCode}: {rfluxmtx2.Result.StandardError}");
                //////}

                ////////V3
                //////var rfluxmtx3 = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx.exe", new[] { "-I+ -y " + sensorCnt + " -lw 0.0001 -ab " + ab + @" -ad " + ad + " -n " + n+ " - " + skyglowrad + " -i " + inputoct });
                //////rfluxmtx3.StandardInput.PipeFromAsync(new FileInfo(ptsin));
                //////rfluxmtx3.StandardOutput.PipeToAsync(new FileInfo(mtxout));
                //////rfluxmtx3.Wait();
                //////if (!rfluxmtx3.Result.Success)
                //////{
                //////    Debug.WriteLine($"3 command failed with exit code {rfluxmtx3.Result.ExitCode}: {rfluxmtx3.Result.StandardError}");
                //////}

                #endregion

                //V4
                // string cmdArgRFLUXMTX = DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @"  -ad " + ad + @" -n " + n + @" - """ + skyglowrad + @""" -i """ + inputoct + @""" < """ + ptsin + @""" > """ + mtxout + @"""";
                string cmdArgRFLUXMTX = DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @"  -ad " + ad + @" -n " + n + @" - " + skyglowrad + @" -i " + inputoct + @" < " + ptsin + @" > " + mtxout ;
                var CMDrfluxmtx = Command.Run("cmd.exe");
                CMDrfluxmtx.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                CMDrfluxmtx.StandardInput.WriteLine(cmdArgRFLUXMTX);
                CMDrfluxmtx.StandardInput.WriteLine("exit");

                CMDrfluxmtx.Wait();

                if (!CMDrfluxmtx.Result.Success)
                {
                    Debug.WriteLine($"4 command failed with exit code {CMDrfluxmtx.Result.ExitCode}: {CMDrfluxmtx.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 4
                // -----------------------------

                Console.WriteLine("Generate SkyVector for whole year...");
                /*
                  REM -m controls the sky subdivision
                  REM Use -O1 to switch to solar rad
                  REM The −d option may be used to produce a sun -only matrix, with no sky contributions. Alternatively, the −s option may be used to exclude any direct solar component from the output.
                  gendaymtx -m " + skysubdiv + @" -O1 ""Rad/output/" + weaname + @".wea"" > ""Rad/output/" + weaname + @".smx""
                 */
                string weain = ( @"Rad\output\" + weaname + @".wea");
                string smxout = ( @"Rad\output\" + weaname + @".smx");

                string gendaymtxArgs = DefaultDirectoriesAndPaths.RadianceDir + @"\gendaymtx.exe -m " + 1 + @" -O1 " + weain + @" > " + smxout;

                var gendaymtx = Command.Run("cmd.exe");
                gendaymtx.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                gendaymtx.StandardInput.WriteLine(gendaymtxArgs);
                gendaymtx.StandardInput.WriteLine("exit");
                gendaymtx.Wait();

                if (!gendaymtx.Result.Success)
                {
                    Debug.WriteLine($"gendaymtx command failed with exit code {gendaymtx.Result.ExitCode}: {gendaymtx.Result.StandardError}");
                    return null;
                }

                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 5
                // -----------------------------

                Console.WriteLine("Create Illum DC...");
                /*
                 REM Create Illum
                 REM Illuminace Weights == 47.4 119.9 11.6  // For Radiation 0.265 0.670 0.065 ???
                 REM -----------------------------------
                 dctimestep Rad/output/dc_r" + skysubdiv + @".mtx ""Rad/output/" + weaname + @".smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 - > ""Rad/output/annualR_dc.ill""
                 */
                string annualR_dc_ill_out =  (@"Rad\output\annualR_dc.ill");
                string dctimestepArgs = DefaultDirectoriesAndPaths.RadianceDir + @"\dctimestep " + mtxout + @" " + smxout + @" | rmtxop -fa -t -c 0.265 0.670 0.065 - > " + annualR_dc_ill_out;
                var dctimestep = Command.Run("cmd.exe");
                dctimestep.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dctimestep.StandardInput.WriteLine(dctimestepArgs);
                dctimestep.StandardInput.WriteLine("exit");
                dctimestep.Wait();

                if (!dctimestep.Result.Success)
                {
                    Debug.WriteLine($"dctimestep command failed with exit code {dctimestep.Result.ExitCode}: {dctimestep.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 6 ComputeDirectOnlyForLowResSky
                // -----------------------------

                Console.WriteLine("Perform an annual direct-only daylight coefficients simulation...");

                //Create black octree for direct sun calculations.
                string radinblack =  (this.BaseWorkingDir + @"\Rad\sceneBlack.rad");
                string octoutblack =  (this.BaseWorkingDir + @"\Rad\output\sceneBlack.oct");

                var oconvBlack = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\oconv", new[] { radinblack },
                    options => options.WorkingDirectory(this.BaseWorkingDir)).RedirectTo(new FileInfo(octoutblack));
                oconvBlack.Wait();

                string octblackin = (@"Rad\output\sceneBlack.oct");
                string dcd_mtxout =  (  @"Rad\output\dcd_" + skySubDivDiff + @".mtx");
                string d_smxout =  (  @"Rad\output\" + weaname + @"d.smx");
                string annualR_dcd_ill_out =  ( @"Rad\output\annualR_dcd.ill");

                string dirCalcArgs1 = DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab 1 -ad " + ad + @" -n " + n + @" - " + skyglowrad + @" -i " + octblackin + @" < " + ptsin + @" > " + dcd_mtxout ;
                string dirCalcArgs2 = DefaultDirectoriesAndPaths.RadianceDir + @"\gendaymtx -m " + skysubdivdiffuse + @" -O1 -d " + weain + @" > " + d_smxout ;
                string dirCalcArgs3 = DefaultDirectoriesAndPaths.RadianceDir + @"\dctimestep " + dcd_mtxout + @" " + d_smxout + @" | rmtxop -fa -t -c 0.265 0.670 0.065 -> " + annualR_dcd_ill_out;


                var dircalc1 = Command.Run("cmd.exe");
                dircalc1.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc1.StandardInput.WriteLine(dirCalcArgs1);
                dircalc1.StandardInput.WriteLine("exit");
                dircalc1.Wait();

                if (!dircalc1.Result.Success)
                {
                    Debug.WriteLine($"dircalc1 command failed with exit code {dircalc1.Result.ExitCode}: {dircalc1.Result.StandardError}");
                    return null;

                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 7 
                // -----------------------------
                var dircalc2 = Command.Run("cmd.exe");
                dircalc2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc2.StandardInput.WriteLine(dirCalcArgs2);
                dircalc2.StandardInput.WriteLine("exit");
                dircalc2.Wait();
                if (!dircalc2.Result.Success)
                {
                    Debug.WriteLine($"dircalc2 command failed with exit code {dircalc2.Result.ExitCode}: {dircalc2.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 8
                // -----------------------------
                var dircalc3 = Command.Run("cmd.exe");
                dircalc3.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc3.StandardInput.WriteLine(dirCalcArgs3);
                dircalc3.StandardInput.WriteLine("exit");
                dircalc3.Wait();
                if (!dircalc3.Result.Success)
                {
                    Debug.WriteLine($"dircalc3 command failed with exit code {dircalc3.Result.ExitCode}: {dircalc3.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));







                // -----------------------------
                // 9 DDS - Higher resolution sun positions
                // -----------------------------
                // Create solar discs and corresponding modifiers for 2305 suns corresponding to a Reinhart MF:4 subdivision.
                // 0.533 solar disc size as angle

                Console.WriteLine("Perform an annual sun-coefficients simulation...");
                string sunsOut =  (@"Rad\output\suns.rad");

                string suncoeffArgs1 = @"echo void light solar 0 0 3 1e6 1e6 1e6 > """ + sunsOut + @"""";
                string suncoeffArgs2 = "cnt " + (144 * skysubdivdirect * skysubdivdirect + 1) + @" | rcalc -e MF:4 -f """ + DefaultDirectoriesAndPaths.RadianceLibDir + @"\reinsrc.cal"" -e Rbin=recno -o ""solar source sun 0 0 4 ${Dx} ${Dy} ${Dz} 0.533"" >> " + sunsOut ;

                var suncoeff = Command.Run("cmd.exe");
                suncoeff.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                suncoeff.StandardInput.WriteLine(suncoeffArgs1);
                suncoeff.StandardInput.WriteLine(suncoeffArgs2);

                suncoeff.StandardInput.WriteLine("exit");

                suncoeff.Wait();

                if (!suncoeff.Result.Success)
                {
                    Debug.WriteLine($"suncoeff command failed with exit code {suncoeff.Result.ExitCode}: {suncoeff.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 10 Put suns in scene..
                // -----------------------------

                Console.WriteLine("Make the Octree... adding suns to scene...");
                string blackWithSuns = (this.BaseWorkingDir + @"\Rad\output\sceneBlackSuns.oct");

                var oconvdir = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\oconv", new[] { radinblack, sunsOut },
                    options => options.WorkingDirectory(this.BaseWorkingDir)).RedirectTo(new FileInfo(blackWithSuns));
                oconvdir.Wait();
                if (!oconvdir.Result.Success)
                {
                    Debug.WriteLine($"oconvdir command failed with exit code {oconvdir.Result.ExitCode}: {oconvdir.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 11
                // -----------------------------
                Console.WriteLine(" Calculate illuminance sun coefficients for illuminance calculations...");
                string blackWithSunsin = ( @"Rad\output\sceneBlackSuns.oct");
                string cddmtxout =  ( @"Rad\output\cdsDDS.mtx");
                string rcontribArgs1 = DefaultDirectoriesAndPaths.RadianceDir + @"\rcontrib -I+ -ab 1 -y " + sensorCnt + @" -n 16 -ad 256 -lw 1.0e-3 -dc 1 -dt 0 -dj 0 -faf -e MF:" + skysubdivdirect + @" -f """ + DefaultDirectoriesAndPaths.RadianceLibDir + @"\reinhart.cal"" -b rbin -bn Nrbins -m solar " + blackWithSunsin + @" < " + ptsin + @" > " + cddmtxout;
                var rcontrib = Command.Run("cmd.exe");
                rcontrib.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                rcontrib.StandardInput.WriteLine(rcontribArgs1);
                rcontrib.StandardInput.WriteLine("exit");

                rcontrib.Wait();

                if (!rcontrib.Result.Success)
                {
                    Debug.WriteLine($"suncoeff command failed with exit code {rcontrib.Result.ExitCode}: {rcontrib.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                //12
                // -----------------------------
                Console.WriteLine("Generate SkyVector for whole year...");
                //REM - 5 option indicates 5phase method mode - solar disc angele must follow that input
                //REM The -d option in the SMX messes it all up-- you can't include -d and -5 together.
                string smxsunout =  (@"Rad\output\sunM" + skysubdivdirect + @".smx");
                string gendaymtx2Args = DefaultDirectoriesAndPaths.RadianceDir + @"\gendaymtx -5 0.533 -m " + skysubdivdirect + @" -O1 " + weain + @" > " + smxsunout;

                var gendaymtx2 = Command.Run("cmd.exe");
                gendaymtx2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                gendaymtx2.StandardInput.WriteLine(gendaymtx2Args);
                gendaymtx2.StandardInput.WriteLine("exit");
                gendaymtx2.Wait();

                if (!gendaymtx2.Result.Success)
                {
                    Debug.WriteLine($"gendaymtx2 command failed with exit code {gendaymtx2.Result.ExitCode}: {gendaymtx2.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));



                // -----------------------------
                //13
                // -----------------------------
                Console.WriteLine("Create Illum Dir...");
                string annualR_dir_ill_out = (@"Rad\output\annual_dir.ill");
                string dctimestep2Args = DefaultDirectoriesAndPaths.RadianceDir + @"\dctimestep " + cddmtxout + @" " + smxsunout + @" | rmtxop -fa -t -c 0.265 0.670 0.065 - > " + annualR_dir_ill_out;

                var dctimestep2 = Command.Run("cmd.exe");
                dctimestep2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dctimestep2.StandardInput.WriteLine(dctimestep2Args);
                dctimestep2.StandardInput.WriteLine("exit");


                dctimestep2.Wait();

                if (!dctimestep2.Result.Success)
                {
                    Debug.WriteLine($"dctimestep2 dir command failed with exit code {dctimestep2.Result.ExitCode}: {dctimestep2.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));



                // -----------------------------
                // 14 Combine Results
                // -----------------------------

                Console.WriteLine("Combine Results...");
                string annualR_total_ill_out = (@"Rad\output\annual_total.ill");
                string rmtxopArgs = DefaultDirectoriesAndPaths.RadianceDir + @"\rmtxop " + annualR_dc_ill_out + @" + -s -1 " + annualR_dcd_ill_out + @" + " + annualR_dir_ill_out + @" > " + annualR_total_ill_out;

                var rmtxop = Command.Run("cmd.exe");
                rmtxop.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                rmtxop.StandardInput.WriteLine(rmtxopArgs);
                rmtxop.StandardInput.WriteLine("exit");
                rmtxop.Wait();

                if (!rmtxop.Result.Success)
                {
                    Debug.WriteLine($"dctimestep dir command failed with exit code {rmtxop.Result.ExitCode}: {rmtxop.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));





                //using (Process process = new Process())
                //{
                //    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                //    process.StartInfo.UseShellExecute = false;
                //    process.StartInfo.RedirectStandardOutput = true;
                //    process.StartInfo.RedirectStandardError = true;
                //    process.StartInfo.WorkingDirectory = this.BaseWorkingDir + @"\Rad\";
                //    process.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                //    process.StartInfo.RedirectStandardError = true;

                //    // Redirects the standard input so that commands can be sent to the shell.
                //    process.StartInfo.RedirectStandardInput = true;

                //    Console.WriteLine("Starting simulation");

                //    process.Start();
                //    process.BeginOutputReadLine();
                //    process.BeginErrorReadLine();
                //    // Send a directory command and an exit command to the shell
                //    process.StandardInput.WriteLine(CommandLineArgsNew(DefaultDirectoriesAndPaths.RadianceDir, this.BaseWorkingDir, this.Probes.Count, weaname, this.Weather.epwFilePath, 3, 5000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1));
                //    process.StandardInput.WriteLine("exit");

                //    process.WaitForExit();

                //    Console.WriteLine("Starting completed");

                //}




                // -----------------------------
                // 15
                // -----------------------------
                var totalIll = LoadDDSIll((this.BaseWorkingDir + @"\Rad\Output\annual_total.ill"));
                var result = new RadiationSimulationDDSResult(this.ProbeMeshes, totalIll);
                var bson = result.ToBson();

                Console.WriteLine();
                File.WriteAllText(this.BaseWorkingDir + @"\" + this.ProjectName + ".Radiation.bin", bson);
                Console.WriteLine("Results written");
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

                return result;

            }


            return null;

        }
        //public string AddQuotesIfRequired(string path)
        //{
        //    return !string.IsNullOrWhiteSpace(path) ?
        //        path.Contains(" ") && (!path.StartsWith("\"") && !path.EndsWith("\"")) ?
        //            "\"" + path + "\"" : path :
        //            string.Empty;
        //}
        
    }
}