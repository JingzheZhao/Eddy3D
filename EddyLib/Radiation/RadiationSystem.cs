using EddyLib.UI;
using Medallion.Shell;
using ProtoBuf;
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


    public class RadiationSystem
    {
        public string ProjectName = "";
        public string BaseWorkingDir = "";


        public List<RSurface> RSurfaces;
        public Mesh SkyDomeForVF;
        public Mesh UnifiedMeshHighPolyNoSky;
        public List<Mesh> ProbeMeshes;

        public List<RProbe> RProbes;
        public List<RPolygon> Polys = new List<RPolygon>();

        public Weather Weather;
        public RadiationSystem(string filename, string baseWorkingDir, Weather weather, List<RSurface> rsurfaces, List<Mesh> probe_meshes, List<RProbe> rprobes)
        {
            ProjectName = filename;
            BaseWorkingDir = baseWorkingDir;
            RSurfaces = rsurfaces;



            // ---------------------
            // Make a unified mesh radiance
            // ---------------------
            UnifiedMeshHighPolyNoSky = new Mesh();
            foreach (var rs in RSurfaces)
            {
                if (rs == null) continue;
                //if (rs.Type == RSurface.RadiationSurfaceType.Sky) continue;
                if (rs.HighPoly != null)
                {
                    rs.LowPoly.Vertices.CullUnused();
                    UnifiedMeshHighPolyNoSky.Append(rs.HighPoly);
                }
            }

            // ---------------------
            // Make a sky dome for VF calculation
            // ---------------------

            var bb = UnifiedMeshHighPolyNoSky.GetBoundingBox(false);
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

                SkyDomeForVF.Flip(true, true, true);

            }
            // Mesh.CreateFromSphere(sphere, 40, 20);



            ProbeMeshes = probe_meshes;
            RProbes = new List<RProbe>();

            foreach (var m in probe_meshes)
            {
                m.Normals.ComputeNormals();
                for (int i = 0; i < m.Vertices.Count; i++)
                {
                    var p = m.Vertices[i];
                    var v = m.Normals[i];
                    RProbes.Add(new RProbe(p, v));
                }
            }

            foreach (var m in rprobes)
            {
                RProbes.Add(m);
            }

            Weather = weather;



            // ---------------------
            // Setup the radiosity system
            // ---------------------
            foreach (var rs in this.RSurfaces)
            {
                this.Polys.AddRange(rs.Polys);
            }
            this.Polys.AddRange(MakeRPolygons(SkyDomeForVF, RadiationSurfaceType.Sky, "SKY", SimulationType.Ignore));
            // set unique ids
            int idcnt = 0;
            foreach (var p in this.Polys)
            { p.ID = idcnt; idcnt++; }



            var dirs = new List<String>() { baseWorkingDir, baseWorkingDir + @"\Rad\" };

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }



        private double pct = 0;
        private double steps = 18;
        private double stepCnt = 0;

        private string annualR_dc_ill_out = (@"Rad\output\annualR_dc.ill");
        private string annualR_dcd_ill_out = (@"Rad\output\annualR_dcd.ill");
        private string annualR_dir_ill_out = (@"Rad\output\annual_dir.ill");
        private string annualR_total_ill_out = (@"Rad\output\annual_total.ill");


        public bool RunDDS(bool run, CancellationToken ct)
        {

            var numberOfProbes = this.RProbes.Count;

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

            RadianceFiles.MeshProc(this.UnifiedMeshHighPolyNoSky, this.BaseWorkingDir + @"\Rad\sceneBlack.rad", "Black", radMatBlack);

            // Write Probes
            RadianceFiles.writePTS(this.BaseWorkingDir + @"\Rad\sensors.pts", this.RProbes.Select(x => x.Point.Value).ToList(), this.RProbes.Select(x => x.Normal.Value).ToList());

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
                var epw2wea = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\epw2wea", new[] { @"""" + this.Weather.epwFilePath + @""" ""Rad/output/" + weaname + @".wea""" },
                  options => options.WorkingDirectory(this.BaseWorkingDir + @"\Rad").CancellationToken(ct));
                epw2wea.Wait();

                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));




                // -----------------------------
                // 4 Generate SkyVector Coarse
                // -----------------------------

                Console.WriteLine("Generate r" + 1 + " SkyVector for whole year...");
                /*
                  REM -m controls the sky subdivision
                  REM Use -O1 to switch to solar rad
                  REM The −d option may be used to produce a sun -only matrix, with no sky contributions. Alternatively, the −s option may be used to exclude any direct solar component from the output.
                  gendaymtx -m " + skysubdiv + @" -O1 ""Rad/output/" + weaname + @".wea"" > ""Rad/output/" + weaname + @".smx""
                 */
                string weain = (@"Rad\output\" + weaname + @".wea");
                string smxout = (@"Rad\output\" + weaname + @".smx");

                string gendaymtxArgs = DefaultDirectoriesAndPaths.RadianceDir + @"\gendaymtx.exe -m " + 1 + @" -O1 " + weain + @" > " + smxout;

                var gendaymtx = Command.Run("cmd.exe", new []{ "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                gendaymtx.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                gendaymtx.StandardInput.WriteLine(gendaymtxArgs);
                gendaymtx.StandardInput.WriteLine("exit");




                // -----------------------------
                // 12 Generate SkyVector Fine
                // -----------------------------
                Console.WriteLine("Generate r" + skysubdivdirect + " SkyVector for whole year...");
                // - 5 option indicates 5phase method mode - solar disc angele must follow that input
                // The -d option in the SMX messes it all up-- you can't include -d and -5 together.
                string smxsunout = (@"Rad\output\sunM" + skysubdivdirect + @".smx");
                string gendaymtx2Args = DefaultDirectoriesAndPaths.RadianceDir + @"\gendaymtx -5 0.533 -m " + skysubdivdirect + @" -O1 " + weain + @" > " + smxsunout;

                var gendaymtx2 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                gendaymtx2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                gendaymtx2.StandardInput.WriteLine(gendaymtx2Args);
                gendaymtx2.StandardInput.WriteLine("exit");




                // -----------------------------
                // 2 Make Octree
                // -----------------------------
                Console.WriteLine("Make the Octree...");
                string radin = (this.BaseWorkingDir + @"\Rad\scene.rad");
                string octout = (this.BaseWorkingDir + @"\Rad\output\scene.oct");
                var oconv = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\oconv", new[] { radin },
                    options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct)).RedirectTo(new FileInfo(octout));
                oconv.Wait();
                if (!oconv.Result.Success)
                {
                    Debug.WriteLine($"oconv command failed with exit code {oconv.Result.ExitCode}: {oconv.Result.StandardError}");
                    return false;
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
                int sensorCnt = this.RProbes.Count;

                string skyglowrad = (@"Rad\skyglow" + skySubDivDiff + @".rad");
                string inputoct = (@"Rad\output\scene.oct");
                string ptsin = (@"Rad\sensors.pts");
                string mtxout = (@"Rad\Output\dc_" + skySubDivDiff + @".mtx");


                string cmdArgRFLUXMTX = DefaultDirectoriesAndPaths.RadianceDir + @"\rfluxmtx -I+ -y " + sensorCnt + @" -lw 0.0001 -ab " + ab + @"  -ad " + ad + @" -n " + n + @" - " + skyglowrad + @" -i " + inputoct + @" < " + ptsin + @" > " + mtxout;
                var CMDrfluxmtx = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                CMDrfluxmtx.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                CMDrfluxmtx.StandardInput.WriteLine(cmdArgRFLUXMTX);
                CMDrfluxmtx.StandardInput.WriteLine("exit");

                CMDrfluxmtx.Wait();

                if (!CMDrfluxmtx.Result.Success)
                {
                    Debug.WriteLine($"4 command failed with exit code {CMDrfluxmtx.Result.ExitCode}: {CMDrfluxmtx.Result.StandardError}");
                    return false;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));




                // -----------------------------
                // wait for 4 Generate SkyVector Coarse
                // -----------------------------
                gendaymtx.Wait();
                if (!gendaymtx.Result.Success)
                {
                    Debug.WriteLine($"gendaymtx command failed with exit code {gendaymtx.Result.ExitCode}: {gendaymtx.Result.StandardError}");
                    return false;
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

                string dctimestepArgs = DefaultDirectoriesAndPaths.RadianceDir + @"\dctimestep " + mtxout + @" " + smxout + @" | rmtxop -fa -t -c 0.265 0.670 0.065 - > " + annualR_dc_ill_out;
                var dctimestep = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                dctimestep.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dctimestep.StandardInput.WriteLine(dctimestepArgs);
                dctimestep.StandardInput.WriteLine("exit");


                // -----------------------------
                // wait for 5 Create Illum DC
                // -----------------------------

                dctimestep.Wait();

                if (!dctimestep.Result.Success)
                {
                    Debug.WriteLine($"dctimestep command failed with exit code {dctimestep.Result.ExitCode}: {dctimestep.Result.StandardError}");
                    return false;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 6 Compute Direct Only for LowResSky
                // -----------------------------

                Console.WriteLine("Perform an annual direct-only daylight coefficients simulation...");

                //Create black octree for direct sun calculations.
                string radinblack = (this.BaseWorkingDir + @"\Rad\sceneBlack.rad");
                string octoutblack = (this.BaseWorkingDir + @"\Rad\output\sceneBlack.oct");

                var oconvBlack = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\oconv", new[] { radinblack },
                    options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct)).RedirectTo(new FileInfo(octoutblack));
                oconvBlack.Wait();

                string octblackin = (@"Rad\output\sceneBlack.oct");
                string dcd_mtxout = (@"Rad\output\dcd_" + skySubDivDiff + @".mtx");
                string d_smxout = (@"Rad\output\" + weaname + @"d.smx");

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
                    return false;

                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 7 
                // -----------------------------
                var dircalc2 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                dircalc2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc2.StandardInput.WriteLine(dirCalcArgs2);
                dircalc2.StandardInput.WriteLine("exit");
                dircalc2.Wait();
                if (!dircalc2.Result.Success)
                {
                    Debug.WriteLine($"dircalc2 command failed with exit code {dircalc2.Result.ExitCode}: {dircalc2.Result.StandardError}");
                    return false;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 8
                // -----------------------------
                var dircalc3 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                dircalc3.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc3.StandardInput.WriteLine(dirCalcArgs3);
                dircalc3.StandardInput.WriteLine("exit");
                dircalc3.Wait();
                if (!dircalc3.Result.Success)
                {
                    Debug.WriteLine($"dircalc3 command failed with exit code {dircalc3.Result.ExitCode}: {dircalc3.Result.StandardError}");
                    return false;
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

                var suncoeff = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                suncoeff.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                suncoeff.StandardInput.WriteLine(suncoeffArgs1);
                suncoeff.StandardInput.WriteLine(suncoeffArgs2);

                suncoeff.StandardInput.WriteLine("exit");

                suncoeff.Wait();

                if (!suncoeff.Result.Success)
                {
                    Debug.WriteLine($"suncoeff command failed with exit code {suncoeff.Result.ExitCode}: {suncoeff.Result.StandardError}");
                    return false;
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
                    options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct)).RedirectTo(new FileInfo(blackWithSuns));
                oconvdir.Wait();
                if (!oconvdir.Result.Success)
                {
                    Debug.WriteLine($"oconvdir command failed with exit code {oconvdir.Result.ExitCode}: {oconvdir.Result.StandardError}");
                    return false;
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
                var rcontrib = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                rcontrib.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                rcontrib.StandardInput.WriteLine(rcontribArgs1);
                rcontrib.StandardInput.WriteLine("exit");

                rcontrib.Wait();

                if (!rcontrib.Result.Success)
                {
                    Debug.WriteLine($"suncoeff command failed with exit code {rcontrib.Result.ExitCode}: {rcontrib.Result.StandardError}");
                    return false;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 12 Started earlier
                // -----------------------------

                gendaymtx2.Wait();
                if (!gendaymtx2.Result.Success)
                {
                    Debug.WriteLine($"gendaymtx2 command failed with exit code {gendaymtx2.Result.ExitCode}: {gendaymtx2.Result.StandardError}");
                    return false;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 13 Create Illum Dir
                // -----------------------------
                Console.WriteLine("Create Illum Dir...");
                string dctimestep2Args = DefaultDirectoriesAndPaths.RadianceDir + @"\dctimestep " + cddmtxout + @" " + smxsunout + @" | rmtxop -fa -t -c 0.265 0.670 0.065 - > " + annualR_dir_ill_out;
                var dctimestep2 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                dctimestep2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dctimestep2.StandardInput.WriteLine(dctimestep2Args);
                dctimestep2.StandardInput.WriteLine("exit");


                dctimestep2.Wait();
                if (!dctimestep2.Result.Success)
                {
                    Debug.WriteLine($"dctimestep2 dir command failed with exit code {dctimestep2.Result.ExitCode}: {dctimestep2.Result.StandardError}");
                    return false;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));



                // -----------------------------
                // 14 Combine Results
                // -----------------------------
                Console.WriteLine("Combine Results...");
                string rmtxopArgs = DefaultDirectoriesAndPaths.RadianceDir + @"\rmtxop " + annualR_dc_ill_out + @" + -s -1 " + annualR_dcd_ill_out + @" + " + annualR_dir_ill_out + @" > " + annualR_total_ill_out;

                var rmtxop = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                rmtxop.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                rmtxop.StandardInput.WriteLine(rmtxopArgs);
                rmtxop.StandardInput.WriteLine("exit");
                rmtxop.Wait();

                if (!rmtxop.Result.Success)
                {
                    Debug.WriteLine($"dctimestep dir command failed with exit code {rmtxop.Result.ExitCode}: {rmtxop.Result.StandardError}");
                    return false;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));





            }


            return true;

        }
        public bool RunVF(bool run, CancellationToken ct)
        {

            this.BuildVFToProbes(UnifiedMeshHighPolyNoSky);

            this.BuildVFToProbesByMaterial();

            stepCnt++;
            pct = 100 * stepCnt / steps;
            Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

            this.BuildFFMatrix(UnifiedMeshHighPolyNoSky);

            stepCnt++;
            pct = 100 * stepCnt / steps;
            Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

            return true;
        }
        public MRTSimulationResultProto SaveResults(bool run, CancellationToken ct)
        {
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
            // 16 Write results
            // -----------------------------
            var prep = PrepareProtoBufSingleton.Instance;

            Stopwatch sp = new Stopwatch();
            sp.Start();
            for (int i = 0; i < this.RProbes.Count; i++)
            {
                this.RProbes[i].TotalRad = new float[totalIll.Length];
                this.RProbes[i].DirRad = new float[dirIll.Length];
                this.RProbes[i].SolarGain_dMRT = new float[dMRT.Length];
                for (int h = 0; h < totalIll.Length; h++)
                {
                    this.RProbes[i].TotalRad[h] = totalIll[h][i];
                    this.RProbes[i].DirRad[h] = dirIll[h][i];
                    this.RProbes[i].SolarGain_dMRT[h] = dMRT[h][i];
                }
            }
            sp.Stop();
            Debug.WriteLine("Reformat results : " + sp.ElapsedMilliseconds);
            sp.Restart();


            var protoResult = new MRTSimulationResultProto(this.ProjectName, this.BaseWorkingDir, this.Weather, this.RProbes, this.ProbeMeshes, this.Polys);
            protoResult.WriteToFile(this.BaseWorkingDir + @"\" + this.ProjectName + ".rad.eddy");

            sp.Stop();
            Debug.WriteLine("Results Proto: " + sp.ElapsedMilliseconds);
            sp.Restart();



            Console.WriteLine("Results written");
            stepCnt++;
            pct = 100 * stepCnt / steps;
            Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

            return protoResult;
        }







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






        #region VIEW FACTOR SYSTEM 

        public static List<RPolygon> MakeRPolygons(Mesh _ms, RadiationSurfaceType type, string matName, SimulationType simtype, double rad = 0, double refl = 0.5)
        {
            List<RPolygon> polys = new List<RPolygon>();
            if (_ms == null) return polys;

            _ms.FaceNormals.ComputeFaceNormals();

            for (int i = 0; i < _ms.Faces.Count; ++i)
            {
                RPolygon pg = new RPolygon();
                polys.Add(pg);

                pg.Centroid.Value = _ms.Faces.GetFaceCenter(i);
                pg.Normal.Value = _ms.FaceNormals[i];
                pg.Normal.Value.Unitize();

                pg.rin = rad;
                pg.rout = 0.0;
                pg.refl = refl;


                pg.Name = matName;
                pg.Type = type;
                pg.SimulationType = simtype;



                if (_ms.Faces[i].IsQuad)
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);
                    Point3d v3 = new Point3d(_ms.Vertices[_ms.Faces[i].D]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                    Vector3d n2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);

                    pg.Area = n1.Length * 0.5 + n2.Length * 0.5;

                    pg.Mesh.Value = new Mesh();
                    pg.Mesh.Value.Vertices.Add(v0);
                    pg.Mesh.Value.Vertices.Add(v1);
                    pg.Mesh.Value.Vertices.Add(v2);
                    pg.Mesh.Value.Vertices.Add(v3);
                    pg.Mesh.Value.Faces.AddFace(0, 1, 2, 3);

                }
                else
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);

                    pg.Area = n1.Length * 0.5;

                    pg.Mesh.Value = new Mesh();
                    pg.Mesh.Value.Vertices.Add(v0);
                    pg.Mesh.Value.Vertices.Add(v1);
                    pg.Mesh.Value.Vertices.Add(v2);
                    pg.Mesh.Value.Faces.AddFace(0, 1, 2);
                }
            }
            return polys;
        }

        public List<string> UniqueSurfaceTypesInModel = new List<string>();

        //Sum up view factors to the different materials in the model
        public void BuildVFToProbesByMaterial()
        {

            UniqueSurfaceTypesInModel = Polys.Select(s => s.Type.ToString()).ToHashSet().ToList();

            // set up dictionary
            for (int i = 0; i < RProbes.Count; i++)
            {
                RProbes[i].VFtoMaterial = new Dictionary<string, double>();
                for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                {
                    RProbes[i].VFtoMaterial.Add(UniqueSurfaceTypesInModel[j], 0);
                }
            }

            for (int i = 0; i < RProbes.Count; i++)
            {
                for (int j = 0; j < Polys.Count; j++)
                {
                    RProbes[i].VFtoMaterial[Polys[j].Type.ToString()] += RProbes[i].VFtoPolys[j];
                }
            }

            // normalize results
            for (int i = 0; i < RProbes.Count; i++)
            {
                double total = 0;
                for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                {
                    total += RProbes[i].VFtoMaterial[UniqueSurfaceTypesInModel[j]];
                }
                double scale = 1 / total;
                for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                {
                    RProbes[i].VFtoMaterial[UniqueSurfaceTypesInModel[j]] *= scale;
                }


            }

        }


        //Compute Form factors taking into account occlusions from a list of meshes
        public void BuildVFToProbes(Mesh Obst)
        {
            foreach (var p in RProbes)
            {
                p.VFtoPolys = new double[Polys.Count];
            }

            System.Threading.Tasks.Parallel.For(0, RProbes.Count, i =>
            {
                for (int j = 0; j < Polys.Count; j++)
                {
                    Point3d probe_pt = RProbes[i].Point.Value;
                    RProbes[i].VFtoPolys[j] = FFactorProbe(probe_pt, Polys[j], Obst);
                }
            });

            // normalize results
            for (int i = 0; i < RProbes.Count; i++)
            {
                double total = 0;
                for (int j = 0; j < RProbes[i].VFtoPolys.Length; j++)
                {
                    total += RProbes[i].VFtoPolys[j];
                }
                double scale = 1 / total;
                for (int j = 0; j < RProbes[i].VFtoPolys.Length; j++)
                {
                    RProbes[i].VFtoPolys[j] *= scale;
                }
            }
            FindPolysSeenByProbes();
        }
        public double FFactorProbe(Point3d probe_pt, RPolygon p1, Mesh Obst)
        {
            Vector3d probe_n = p1.Centroid.Value - probe_pt;
            probe_n.Unitize();
            if (p1.Normal.Value * probe_n > 0.0001) return 0.0; //if normals don't face each other return 0

            Plane pl = new Plane(p1.Centroid.Value, p1.Normal.Value);
            if (pl.DistanceTo(probe_pt) < 0) return 0.0; // if the other face is behind the test face return 0

            double f = 0.0;

            Vector3d dv;
            dv = p1.Centroid.Value - probe_pt;
            double r = dv.Length;
            if (r < 0.1) return 0.0;


            double cosThetaI = dv * probe_n / (dv.Length * probe_n.Length);
            double cosThetaJ = -dv * p1.Normal.Value / (dv.Length * p1.Normal.Value.Length);
            f = ((cosThetaI * cosThetaJ) / (4 * Math.PI * r * r)) * p1.Area;



            //only do occlusion test for large view factors -- zero all others
            if (f < 0.00001) return 0.0;

            Vector3d dv_forRaycast = (p1.Centroid.Value + (0.01 * p1.Normal.Value)) - (probe_pt + (0.01 * probe_n));
            Line line = new Line(p1.Centroid.Value + (0.01 * p1.Normal.Value), probe_pt + (0.01 * probe_n));
            int[] fid;
            var pts = Rhino.Geometry.Intersect.Intersection.MeshLine(Obst, line, out fid);
            if (pts.Length > 0) return 0.0;

            return f;
        }
        private void FindPolysSeenByProbes()
        {
            for (int j = 0; j < Polys.Count; j++)
            {
                for (int i = 0; i < RProbes.Count; i++)
                {
                    Polys[j].SeenByProbes += RProbes[i].VFtoPolys[j];
                }
            }

        }

        public double maxv = 0.0;
        public double[][] F;
        public double[] xk0;
        public double[] xk1;
        public double[] b;
        //Compute Form factors taking into account occlusions from a list of meshes
        public void BuildFFMatrix(Mesh Obst)
        {
            int Ps = Polys.Count;
            //F = new double[Ps, Ps];

            F = new double[Ps][];
            for (int i = 0; i < Ps; i++)
            {
                F[i] = new double[Ps];
            }

            xk0 = new double[Ps];
            xk1 = new double[Ps];
            b = new double[Ps];

            for (int i = 0; i < Ps; ++i)
            {
                xk0[i] = 0.0;
                xk1[i] = 0.0;
                b[i] = Polys[i].rin;
            }

            double Fij = 0.0;


            // DO NOT USE THIS - THE F[i][j] IS NOT THREAD SAFE
            // System.Threading.Tasks.Parallel.For(0, Ps, j =>
            //  {
            for (int j = 0; j < Ps; ++j)
            {
                for (int i = j; i < Ps; ++i)
                {

                    if (i == j)
                    {
                        F[j][i] = 0.0;
                    }
                    else
                    {
                        Fij = FFactor(Polys[i], Polys[j], Obst);
                        F[j][i] = Fij * Polys[i].Area;
                        F[i][j] = Fij * Polys[j].Area;
                    }
                }
            }
            // });
        }

        //Computes the form factor between two polygons. It returns
        // 0.0 if the polygons are facing in opposite ways or are nearly coplanar or too close to each other
        public double FFactor(RPolygon p0, RPolygon p1, Mesh Obst)
        {

            // --- 6/25/2020
            if (p0.Normal.Value * p1.Normal.Value > 0.0001) return 0.0; //if normals don't face each other return 0
            Plane pl = new Plane(p0.Centroid.Value, p0.Normal.Value);
            if (pl.DistanceTo(p1.Centroid.Value) < 0) return 0.0; // if the other face is behind the test face return 0
                                                                  // ---

            double f = 0.0;

            Vector3d dv;
            dv = p1.Centroid.Value - p0.Centroid.Value;
            double r = dv.Length;
            if (r < 0.1) return 0.0;
            //dv *= (1.0 / r);


            double cosThetaI = dv * p0.Normal.Value / (dv.Length * p0.Normal.Value.Length);
            double cosThetaJ = -dv * p1.Normal.Value / (dv.Length * p1.Normal.Value.Length);

            //if (Math.Abs(dv * p0.n) < 0.0001 && Math.Abs(dv * p1.n) < 0.0001) return 0.0;
            //if ((dv * p0.n) < 0.0001 && -(dv * p1.n) < 0.0001) return 0.0;

            // if (cosThetaI < 0.0001 && cosThetaJ < 0.0001) return 0.0;

            f = ((cosThetaI * cosThetaJ) / (Math.PI * r * r));//*p0.area * p1.area;

            //if (f < 0.0) return 0.0;



            //only do occlusion test for large view factors -- zero all others
            if (f < 0.0000001) return 0.0;


            Vector3d dv_forRaycast = (p1.Centroid.Value + (0.01 * p1.Normal.Value)) - (p0.Centroid.Value + (0.01 * p0.Normal.Value));
            Line line = new Line(p1.Centroid.Value + (0.01 * p1.Normal.Value), p0.Centroid.Value + (0.01 * p0.Normal.Value));
            int[] fid;
            var pts = Rhino.Geometry.Intersect.Intersection.MeshLine(Obst, line, out fid);
            if (pts.Length > 0) return 0.0;

            //Ray3d ry = new Ray3d(p0.cen + 0.01 * p0.n, dv_forRaycast);
            //double il = Rhino.Geometry.Intersect.Intersection.MeshRay(Obst, ry);
            //if (il > 0.0 && il < dv_forRaycast.Length) return 0.0;



            return f;
        }
        //Do one iteration step of Gauss-Seidel method.
        public void Iterate()
        {
            if (F == null) return;


            for (int i = 0; i < Polys.Count; ++i)
            {
                xk1[i] = b[i];


                for (int j = i + 1; j < Polys.Count; ++j)
                {
                    xk1[i] -= F[j][i] * xk0[j];
                }
                for (int j = 0; j < i; ++j)
                {
                    xk1[i] -= F[j][i] * xk1[j];
                }
                xk1[i] /= F[i][i];
            }


            maxv = 0.0;
            for (int i = 0; i < Polys.Count; ++i)
            {
                xk0[i] = xk1[i];

                Polys[i].rout = xk0[i];
                if (Math.Abs(Polys[i].rout) > maxv) maxv = Math.Abs(Polys[i].rout);
            }


        }

        #endregion
    }
}