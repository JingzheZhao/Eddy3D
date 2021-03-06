using EddyLib.UI;
using Medallion.Shell;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace EddyLib.Radiation
{



    public class RadiationSimulationSystem
    {

        private static float[][] LoadDDSIll(string illFileName) // total illuminance data
        {
            string[] illLines = System.IO.File.ReadAllLines(illFileName).ToArray();
            int skip = 0;

            for (int i = 0; i < illLines.Length; i++)
            {
                if (illLines[i].Contains("FORMAT")) { skip = i + 2; break; }
            }


            return illLines.Skip(skip).Select(l => Array.ConvertAll<string, float>(l.Split(new[] { ' ' }).Skip(1).ToArray(), float.Parse)).ToArray();

            // [x][] time
            // [][x] points
        }


        public string ProjectName = "";

        //private string TwoPhaseDDSFolder = @"\TwoPhaseDDS\";

        public string BaseWorkingDir = "";


        public List<RSurface> RSurfaces;
        public Mesh SkyDomeForVF;
        public Mesh UnifiedMeshLowPolyNoSky;
        public List<Mesh> ProbeMeshes;
        public List<Point3d> Probes;
        public Weather Weather;
        public RadiationSimulationSystem(string filename, string baseWorkingDir, List<RSurface> rsurfaces, List<Mesh> probe_meshes, Weather weather)
        {
            ProjectName = filename;
            BaseWorkingDir = baseWorkingDir;
            RSurfaces = rsurfaces;



            // ---------------------
            // Make a unified mesh radiance
            // ---------------------
            UnifiedMeshLowPolyNoSky = new Mesh();
            foreach (var rs in RSurfaces)
            {
                if (rs == null) continue;
                //if (rs.Type == RSurface.RadiationSurfaceType.Sky) continue;
                if (rs.LowPoly != null)
                {
                    rs.LowPoly.Vertices.CullUnused();
                    UnifiedMeshLowPolyNoSky.Append(rs.LowPoly);
                }
            }

            // ---------------------
            // Make a sky dome for VF calculation
            // ---------------------

            var bb = UnifiedMeshLowPolyNoSky.GetBoundingBox(false);
            var center = new Point3d(bb.Center.X, bb.Center.Y, bb.Min.Z);
            var radius = bb.Diagonal.Length;

            Sphere sphere = new Sphere(center, radius);
            var sphereM = Mesh.CreateQuadSphere(sphere, 4);
            SkyDomeForVF = new Mesh();

            if (sphereM != null)
            {
                sphereM.FaceNormals.ComputeFaceNormals();
                var findex = new List<int>();
                for (int i = 0; i < sphereM.Faces.Count; i++)
                {
                    var dot = sphereM.FaceNormals[i] * Vector3d.ZAxis;
                    if (dot > 0 - 0.0001)
                    {
                        findex.Add(i);
                    }
                }

                SkyDomeForVF.Append(sphereM.Faces.ExtractFaces(findex));
                SkyDomeForVF.FaceNormals.ComputeFaceNormals();

            }
            // Mesh.CreateFromSphere(sphere, 40, 20);

             

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

        public RadiationSimulationResult RunDDS(bool run, CancellationToken ct)
        {

            double pct = 0;
            double steps = 16;
            double stepCnt = 0;

            var numberOfProbes = this.Probes.Count;

            var skySubDivDiff = SkySubdivision.r1;
            //var skySubDivDiff = SkySubdivision.r2;
            var skySubDivDir = SkySubdivision.r4;
            int skysubdivdiffuse = 1;
            int skysubdivdirect = 4;
            // User geometry data here

            //    string radMat = @"
            //void plastic Generic_20
            //0
            //0
            //5 0.2 0.2 0.2 0 0
            //";

            string radMatBlack = @"
        void plastic Black
        0
        0
        5 0 0 0 0 0
        ";



            Console.WriteLine("Writing files...");
            // Make sure this understands userdata
            //RadianceFiles.MeshProc(this.UnifiedMeshLowPolyNoSky, this.BaseWorkingDir + @"\Rad\scene.rad", "Generic_20", radMat);
            RadianceFiles.MeshProc(RSurfaces, this.BaseWorkingDir + @"\Rad\scene.rad");

            RadianceFiles.MeshProc(this.UnifiedMeshLowPolyNoSky, this.BaseWorkingDir + @"\Rad\sceneBlack.rad", "Black", radMatBlack);

            // Write Probes
            RadianceFiles.writePTS(this.BaseWorkingDir + @"\Rad\sensors.pts", this.Probes);

            // Weather
            var weaname = RadianceFiles.Epw2Wea(this.Weather.epwFilePath, this.BaseWorkingDir + @"\Rad\Output");

            // Sky
            RadianceSkies.Write(this.BaseWorkingDir + @"\Rad\skyglow" + (skySubDivDir) + ".rad", (skySubDivDir));
            RadianceSkies.Write(this.BaseWorkingDir + @"\Rad\skyglow" + (skySubDivDiff) + ".rad", (skySubDivDiff));


            if (run == true)
            {

                // -----------------------------
                // 1 Convert epw to wea tape
                // -----------------------------
                Console.WriteLine("Convert epw to wea tape...");
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
                int ab = 3;
                int ad = 2000;
                int n = (Environment.ProcessorCount - 1);
                int sensorCnt = this.Probes.Count;

                string skyglowrad = (@"Rad\skyglow" + skySubDivDiff + @".rad");
                string inputoct = (@"Rad\output\scene.oct");
                string ptsin = (@"Rad\sensors.pts");
                string mtxout = (@"Rad\Output\dc_" + skySubDivDiff + @".mtx");


                string cmdArgRFLUXMTX = DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @"  -ad " + ad + @" -n " + n + @" - " + skyglowrad + @" -i " + inputoct + @" < " + ptsin + @" > " + mtxout;
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
                // 4 Generate SkyVector
                // -----------------------------

                Console.WriteLine("Generate SkyVector for whole year...");
                /*
                  REM -m controls the sky subdivision
                  REM Use -O1 to switch to solar rad
                  REM The −d option may be used to produce a sun -only matrix, with no sky contributions. Alternatively, the −s option may be used to exclude any direct solar component from the output.
                  gendaymtx -m " + skysubdiv + @" -O1 ""Rad/output/" + weaname + @".wea"" > ""Rad/output/" + weaname + @".smx""
                 */
                string weain = (@"Rad\output\" + weaname + @".wea");
                string smxout = (@"Rad\output\" + weaname + @".smx");

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
                // 5 Create Illum DC
                // -----------------------------

                Console.WriteLine("Create Illum DC...");
                // Create Illum
                // Illuminace Weights == 47.4 119.9 11.6  // For Radiation 0.265 0.670 0.065 ???
                // The * "global horizontal radiation" *in the epw file is a total solar
                // radiation value* NOT yet* integrated over the visible spectral range
                // (380 - 780 nm)(from gendaylit man page), so we can't simply multiply it by
                // 179 to get the illuminance value.We need to "break" the * "global
                // horizontal radiation" *into its RGB components and then use
                // (R * 0.265 + G * 0.670 + B * 0.065) * 179 to convert it into a illuminance value.
                // dctimestep Rad/output/dc_r" + skysubdiv + @".mtx ""Rad/output/" + weaname + @".smx"" | rmtxop -fa -t -c 0.265 0.670 0.065 - > ""Rad/output/annualR_dc.ill""

                string annualR_dc_ill_out = (@"Rad\output\annualR_dc.ill");
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
                string radinblack = (this.BaseWorkingDir + @"\Rad\sceneBlack.rad");
                string octoutblack = (this.BaseWorkingDir + @"\Rad\output\sceneBlack.oct");

                var oconvBlack = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\oconv", new[] { radinblack },
                    options => options.WorkingDirectory(this.BaseWorkingDir)).RedirectTo(new FileInfo(octoutblack));
                oconvBlack.Wait();

                string octblackin = (@"Rad\output\sceneBlack.oct");
                string dcd_mtxout = (@"Rad\output\dcd_" + skySubDivDiff + @".mtx");
                string d_smxout = (@"Rad\output\" + weaname + @"d.smx");
                string annualR_dcd_ill_out = (@"Rad\output\annualR_dcd.ill");

                string dirCalcArgs1 = DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab 1 -ad " + ad + @" -n " + n + @" - " + skyglowrad + @" -i " + octblackin + @" < " + ptsin + @" > " + dcd_mtxout;
                string dirCalcArgs2 = DefaultDirectoriesAndPaths.RadianceDir + @"\gendaymtx -m " + skysubdivdiffuse + @" -O1 -d " + weain + @" > " + d_smxout;
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
                string sunsOut = (@"Rad\output\suns.rad");

                string suncoeffArgs1 = @"echo void light solar 0 0 3 1e6 1e6 1e6 > """ + sunsOut + @"""";
                string suncoeffArgs2 = "cnt " + (144 * skysubdivdirect * skysubdivdirect + 1) + @" | rcalc -e MF:4 -f """ + DefaultDirectoriesAndPaths.RadianceLibDir + @"\reinsrc.cal"" -e Rbin=recno -o ""solar source sun 0 0 4 ${Dx} ${Dy} ${Dz} 0.533"" >> " + sunsOut;

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
                // 11 Calculate illuminance sun coefficients
                // -----------------------------
                Console.WriteLine("Calculate illuminance sun coefficients for illuminance calculations...");
                string blackWithSunsin = (@"Rad\output\sceneBlackSuns.oct");
                string cddmtxout = (@"Rad\output\cdsDDS.mtx");
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
                // 12
                // -----------------------------
                Console.WriteLine("Generate SkyVector for whole year...");
                // - 5 option indicates 5phase method mode - solar disc angele must follow that input
                // The -d option in the SMX messes it all up-- you can't include -d and -5 together.
                string smxsunout = (@"Rad\output\sunM" + skysubdivdirect + @".smx");
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
                // 13 Create Illum Dir
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




                // -----------------------------
                // 15 Compute dMRT
                // -----------------------------
                Console.WriteLine("Compute dMRT");


                var totalIll = LoadDDSIll((this.BaseWorkingDir + @"\" + annualR_total_ill_out));
                //var diffIll = LoadDDSIll((this.BaseWorkingDir + @"\Rad\Output\annual_total.ill"));
                var dirIll = LoadDDSIll((this.BaseWorkingDir + @"\" + annualR_dir_ill_out));


                float[][] dMRT = SolarGain.ComputeStanding(this.Weather, totalIll, dirIll);

                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 16 
                // -----------------------------



                var result = new RadiationSimulationResult(this.ProbeMeshes, totalIll, dirIll, dMRT);
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





    }
}