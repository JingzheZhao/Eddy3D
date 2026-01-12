using EddyLib.Helpers;
using EddyLib.UI;
using Medallion.Shell;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EddyLib; // Added for DefaultDirectoriesAndPaths

[assembly: InternalsVisibleTo("Eddy")]

namespace EddyLib.Radiation
{
    public class RadiationSystem
    {
        public int methodsteps = 18;
        public string BaseWorkingDir = "";
        public List<RSurface> RSurfaces;
        public Mesh UnifiedMeshHighPolyNoSky;
        public List<RProbe> Probes;
        public List<RPolygon> Polys = new List<RPolygon>();
        public Weather Weather;
        public StringBuilder ErrorLog = new StringBuilder();

        // Radiance Output Files
        private readonly string annualR_dc_ill_out = Path.Combine("Rad", "output", "annualR_dc.ill");
        private readonly string annualR_dcd_ill_out = Path.Combine("Rad", "output", "annualR_dcd.ill");
        private readonly string annualR_dir_ill_out = Path.Combine("Rad", "output", "annual_dir.ill");
        private readonly string annualR_total_ill_out = Path.Combine("Rad", "output", "annual_total.ill");

        public RadiationSystem(string baseWorkingDir, Weather weather, List<RSurface> rsurfaces, List<RProbe> probes, List<RPolygon> polys, Mesh unified)
        {
            BaseWorkingDir = baseWorkingDir;
            Weather = weather;
            RSurfaces = rsurfaces;
            Probes = probes;
            Polys = polys;
            UnifiedMeshHighPolyNoSky = unified;
        }

        public bool RunDDS(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            return RunRadianceDDS(run, ct, steps, ref stepCnt);
        }

        private bool RunRadianceDDS(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Starting DDS Simulation");

            // Setup paths and files
            string raddir = Path.Combine(BaseWorkingDir, "Rad");
            string radout = Path.Combine(raddir, "output");
            Directory.CreateDirectory(radout);

            PrepareSimulationFiles();

            string weaname = RadianceFiles.Epw2Wea(Weather.epwFilePath, radout);
            RadianceSkies.Write(Path.Combine(raddir, $"skyglow{SkySubdivision.r4}.rad"), SkySubdivision.r4);
            RadianceSkies.Write(Path.Combine(raddir, $"skyglow{SkySubdivision.r1}.rad"), SkySubdivision.r1);

            if (!run) return true;

            // Setup Environment Variables
            var env = GetRadianceEnvironment();

            // 1. Convert EPW to WEA
            if (!RunCommand("epw2wea", 
                new[] { Weather.epwFilePath, Path.Combine(radout, $"{weaname}.wea") }, 
                raddir, env, ct, ref stepCnt, steps, "Convert Epw to Wea")) return false;

            // 4. Generate SkyVector Coarse (r1)
            string weaFile = Path.Combine(radout, $"{weaname}.wea");
            string smxOut = Path.Combine(radout, $"{weaname}.smx");
            
            // gendaymtx -m 1 -O1 ... > smxOut
            if (!RunCommandRedirect("gendaymtx", 
                new[] { "-m", "1", "-O1", weaFile }, 
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Generate r1 SkyVector", 
                redirectOutput: smxOut)) return false;

            // 12. Generate SkyVector Fine (r4 / skysubdivdirect) - Started early in original logic?
            // Reordering to keep logical flow or preserving concurrency if intended?
            // Original code started step 12 early but waited later. Let's run it now sequentially for simplicity unless parallel needed.
            int skySubDivDirect = 4;
            string smxSunOut = Path.Combine(radout, $"sunM{skySubDivDirect}.smx");
            // gendaymtx -5 0.533 -m 4 -O1 ...
            if (!RunCommandRedirect("gendaymtx",
                new[] { "-5", "0.533", "-m", skySubDivDirect.ToString(), "-O1", weaFile },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Generate SkyVector Fine",
                redirectOutput: smxSunOut)) return false;

            // 2. Make Octree
            string sceneRad = Path.Combine(raddir, "scene.rad");
            string sceneOct = Path.Combine(radout, "scene.oct");
            if (!RunCommandRedirect("oconv",
                new[] { sceneRad },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Make Octree",
                redirectOutput: sceneOct)) return false;

            // 3. Create Daylight Coefficient Matrix
            int sensorCnt = Probes.Count;
            string mtxOut = Path.Combine(radout, $"dc_{SkySubdivision.r1}.mtx");
            string skyGlowRad = Path.Combine(raddir, $"skyglow{SkySubdivision.r1}.rad");
            string sensorsPts = Path.Combine(raddir, "sensors.pts");
            
            // rfluxmtx -I+ ... < sensors.pts > mtxOut
            if (!RunCommandRedirect("rfluxmtx",
                new[] { "-I+", "-y", sensorCnt.ToString(), "-lw", "0.0001", "-ab", "3", "-ad", "2000", "-n", (Environment.ProcessorCount - 1).ToString(), "-", skyGlowRad, "-i", sceneOct },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Create DC Matrix",
                redirectInput: sensorsPts, redirectOutput: mtxOut)) return false;

            // Wait for step 12 was here in original code

            // 5. Create Illum DC
            // pipe: dctimestep ... | rmtxop ... > output
            string annualRdC = Path.Combine(BaseWorkingDir, annualR_dc_ill_out);
            if (!RunPipeline(
                ("dctimestep", new[] { mtxOut, smxOut }),
                ("rmtxop", new[] { "-fa", "-t", "-c", "0.265", "0.670", "0.065", "-" }),
                annualRdC, BaseWorkingDir, env, ct, ref stepCnt, steps, "Create Illum DC")) return false;

            // 6. Direct Only Simulation
            string blackSceneRad = Path.Combine(raddir, "sceneBlack.rad");
            string blackSceneOct = Path.Combine(radout, "sceneBlack.oct");
            if (!RunCommandRedirect("oconv",
                new[] { blackSceneRad },
                BaseWorkingDir, env, ct, ref stepCnt, 0, "Make Black Octree", // steps=0 disables progress reporting
                redirectOutput: blackSceneOct)) return false;

            string dcdMtxOut = Path.Combine(radout, $"dcd_{SkySubdivision.r1}.mtx");
            string dSmxOut = Path.Combine(radout, $"{weaname}d.smx");
            string annualRdCd = Path.Combine(BaseWorkingDir, annualR_dcd_ill_out);

            // rfluxmtx for direct
             if (!RunCommandRedirect("rfluxmtx",
                new[] { "-I+", "-y", sensorCnt.ToString(), "-lw", "0.0001", "-ab", "1", "-ad", "2000", "-n", (Environment.ProcessorCount - 1).ToString(), "-", skyGlowRad, "-i", blackSceneOct },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Direct DC Matrix",
                redirectInput: sensorsPts, redirectOutput: dcdMtxOut)) return false;

            // gendaymtx for direct
            // gendaymtx -m 1 -O1 -d ... > dSmxOut
             if (!RunCommandRedirect("gendaymtx",
                new[] { "-m", "1", "-O1", "-d", weaFile },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Generate Direct SkyVector",
                redirectOutput: dSmxOut)) return false;

             // dctimestep | rmtxop > annualRdCd
             if (!RunPipeline(
                ("dctimestep", new[] { dcdMtxOut, dSmxOut }),
                ("rmtxop", new[] { "-fa", "-t", "-c", "0.265", "0.670", "0.065", "-" }),
                annualRdCd, BaseWorkingDir, env, ct, ref stepCnt, steps, "Create Illum Direct DC")) return false;

            // 9. DDS - Sun Coefficients
            string sunsOut = Path.Combine(radout, "suns.rad");
            
            // Must create suns.rad with header first
            File.WriteAllText(sunsOut, "void light solar 0 0 3 1e6 1e6 1e6\n");
            
            // cnt ... | rcalc ... >> sunsOut
            // cnt 144...
            int cntNum = 144 * skySubDivDirect * skySubDivDirect + 1;
            string reinsrc = Path.Combine(DefaultDirectoriesAndPaths.RadianceLibDir, "reinsrc.cal");
            
            // Using shell for complex pipe append >> ? Or just Command.PipeTo
            // cnt | rcalc
            using (var stream = new FileStream(sunsOut, FileMode.Append, FileAccess.Write))
            {
                var cmdCnt = Command.Run(Path.Combine(DefaultDirectoriesAndPaths.RadianceDir, "cnt"), new[] { cntNum.ToString() }, options => options.WorkingDirectory(BaseWorkingDir));
                var cmdRcalc = Command.Run(Path.Combine(DefaultDirectoriesAndPaths.RadianceDir, "rcalc"), 
                    new[] { "-e", "MF:4", "-f", reinsrc, "-e", "Rbin=recno", "-o", "solar source sun 0 0 4 ${Dx} ${Dy} ${Dz} 0.533" }, 
                    options => options.WorkingDirectory(BaseWorkingDir));

                var pipe = cmdCnt.PipeTo(cmdRcalc);
                var res = pipe.RedirectTo(stream).Result; // Append via stream
                
                if (!res.Success)
                {
                    LogError("SunCoeff Pipeline", res.StandardError); 
                    return false;
                }
            }
            ReportProgress(ref stepCnt, steps);

            // 10. Put suns in scene
            string blackSunsOct = Path.Combine(radout, "sceneBlackSuns.oct");
            if (!RunCommandRedirect("oconv",
                new[] { blackSceneRad, sunsOut },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Make Sun Octree",
                redirectOutput: blackSunsOct)) return false;

            // 11. Calculate Illum Sun Coeffs
            string cddMtxOut = Path.Combine(radout, "cdsDDS.mtx");
            string reinhartCal = Path.Combine(DefaultDirectoriesAndPaths.RadianceLibDir, "reinhart.cal");
            
            // rcontrib ... < sensors.pts > cddMtxOut
            if (!RunCommandRedirect("rcontrib",
                new[] { "-I+", "-ab", "1", "-y", sensorCnt.ToString(), "-n", "16", "-ad", "256", "-lw", "1.0e-3", "-dc", "1", "-dt", "0", "-dj", "0", "-faf", "-e", $"MF:{skySubDivDirect}", "-f", reinhartCal, "-b", "rbin", "-bn", "Nrbins", "-m", "solar", blackSunsOct },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Calculate Sun Coeffs",
                redirectInput: sensorsPts, redirectOutput: cddMtxOut)) return false;

            // 13. Create Illum Dir
            string annualDirIll = Path.Combine(BaseWorkingDir, annualR_dir_ill_out);
            if (!RunPipeline(
                ("dctimestep", new[] { cddMtxOut, smxSunOut }),
                ("rmtxop", new[] { "-fa", "-t", "-c", "0.265", "0.670", "0.065", "-" }),
                annualDirIll, BaseWorkingDir, env, ct, ref stepCnt, steps, "Create Illum Dir")) return false;

            // 14. Combine Results
            string annualTotal = Path.Combine(BaseWorkingDir, annualR_total_ill_out);
            // rmtxop A + -s -1 B + C > Total
            if (!RunCommandRedirect("rmtxop",
                new[] { annualRdC, "+", "-s", "-1", annualRdCd, "+", annualDirIll },
                BaseWorkingDir, env, ct, ref stepCnt, steps, "Combine Results",
                redirectOutput: annualTotal)) return false;

            return true;
        }

        private void PrepareSimulationFiles()
        {
            Console.WriteLine("Writing geometry files...");
            RadianceFiles.MeshProc(RSurfaces, Path.Combine(BaseWorkingDir, "Rad", "scene.rad"));
            
            string radMatBlack = "\nvoid plastic Black\n0\n0\n5 0 0 0 0 0\n";
            RadianceFiles.MeshProc(UnifiedMeshHighPolyNoSky, Path.Combine(BaseWorkingDir, "Rad", "sceneBlack.rad"), "Black", radMatBlack);

            RadianceFiles.writePTS(
                Path.Combine(BaseWorkingDir, "Rad", "sensors.pts"), 
                Probes.Select(x => x.Point.Value).ToList(), 
                Probes.Select(x => x.Normal.Value).ToList());
        }

        private Dictionary<string, string> GetRadianceEnvironment()
        {
             
             // Using DefaultDirectoriesAndPaths
             string radbin = DefaultDirectoriesAndPaths.RadianceBinDir; 
             string radlib = DefaultDirectoriesAndPaths.RadianceLibDir;

             var env = new Dictionary<string, string>();
             string path = Environment.GetEnvironmentVariable("PATH") ?? "";
             env["PATH"] = $".;{radlib};{radbin};{path}";
             env["RAYPATH"] = $".;{radlib};{radbin};" + (Environment.GetEnvironmentVariable("RAYPATH") ?? "");
             return env;
        }

        private bool RunCommand(string command, string[] args, string workingDir, Dictionary<string, string> env, CancellationToken ct, ref int stepCnt, int steps, string desc)
        {
            return RunCommandRedirect(command, args, workingDir, env, ct, ref stepCnt, steps, desc);
        }

        private bool RunCommandRedirect(string command, string[] args, string workingDir, Dictionary<string, string> env, CancellationToken ct, ref int stepCnt, int steps, string desc, string redirectInput = null, string redirectOutput = null)
        {
            Console.WriteLine($"{desc}...");
            string exePath = Path.Combine(DefaultDirectoriesAndPaths.RadianceBinDir, command + ".exe");
            if (!File.Exists(exePath)) exePath = command; // Fallback or global

            var cmd = Command.Run(exePath, args, options => {
                options.WorkingDirectory(workingDir).CancellationToken(ct);
                foreach(var kvp in env) options.EnvironmentVariable(kvp.Key, kvp.Value);
            });

            if (redirectInput != null) cmd.RedirectFrom(new FileInfo(redirectInput));
            if (redirectOutput != null) cmd.RedirectTo(new FileInfo(redirectOutput));

            cmd.Wait();
            if (!cmd.Result.Success)
            {
                LogError(desc, cmd.Result.StandardError);
                return false;
            }

            if (steps > 0) ReportProgress(ref stepCnt, steps);
            return true;
        }

        private bool RunPipeline(
            (string cmd, string[] args) stage1,
            (string cmd, string[] args) stage2,
            string outputFile,
            string workingDir, Dictionary<string, string> env, CancellationToken ct, ref int stepCnt, int steps, string desc)
        {
            Console.WriteLine($"{desc}...");
            
            Action<Shell.Options> opts = o => {
                o.WorkingDirectory(workingDir).CancellationToken(ct);
                foreach(var kvp in env) o.EnvironmentVariable(kvp.Key, kvp.Value);
            };

            string exe1 = Path.Combine(DefaultDirectoriesAndPaths.RadianceBinDir, stage1.cmd + ".exe");
            string exe2 = Path.Combine(DefaultDirectoriesAndPaths.RadianceBinDir, stage2.cmd + ".exe");

            var c1 = Command.Run(exe1, stage1.args, opts);
            var c2 = Command.Run(exe2, stage2.args, opts);
            
            var pipe = c1.PipeTo(c2);
            var res = pipe.RedirectTo(new FileInfo(outputFile)).Result;

            if (!res.Success)
            {
                LogError(desc, res.StandardError);
                return false;
            }
            if (steps > 0) ReportProgress(ref stepCnt, steps);
            return true;
        }

        public void LoadDDSData(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Compute dMRT");
            // Load paths using helper fields
            var totalIll = LoadDDSIll(Path.Combine(BaseWorkingDir, annualR_total_ill_out));
            var dirIll = LoadDDSIll(Path.Combine(BaseWorkingDir, annualR_dir_ill_out));

            float[][] dMRT = SolarGain.ComputeStanding(Weather, totalIll, dirIll);

            ReportProgress(ref stepCnt, steps);

            for (int i = 0; i < Probes.Count; i++)
            {
                // Initialize arrays
                Probes[i].TotalRad = new float[totalIll.Length];
                Probes[i].DirRad = new float[dirIll.Length];
                Probes[i].SolarGain_dMRT = new float[dMRT.Length];

                 // Parallel copy if large data? Inner loop is 8760. Outer is probe count.
                 // Manual copy is fast enough usually.
                for (int h = 0; h < totalIll.Length; h++)
                {
                    Probes[i].TotalRad[h] = totalIll[h][i];
                    Probes[i].DirRad[h] = dirIll[h][i];
                    Probes[i].SolarGain_dMRT[h] = dMRT[h][i];
                }
            }
        }

        public void RunDirectRayCast(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Computing radiation and dMRT (Simple RayCast)...");

            SolarGeometry sg = new SolarGeometry();
            
            // USE NEW HELPER METHOD
            Vector3d[] sunPositions = sg.GetMonthlyRepresentativeSunVectors(Weather.SolarElevation, Weather.SolarAzi);
            
            Console.WriteLine("Raycasting...");

            Parallel.For(0, Probes.Count, i =>
            {
                var probe = Probes[i];
                probe.TotalRad = new float[8760];
                probe.DirRad = new float[8760];

                double[] dotproduct = new double[sunPositions.Length];
                
                // Precompute visibility for 288 sun positions
                for (int h = 0; h < sunPositions.Length; h++)
                {
                    if (sunPositions[h] == Vector3d.Zero) continue;

                    var ray = new Ray3d(probe.Point.Value, sunPositions[h]);
                    // Offset ray origin slightly? Original code didn't. ThermalSystem did.
                    // Original code: new Ray3d(Probes[i].Point.Value, sunPositions[h])
                    
                    var dt = Rhino.Geometry.Intersect.Intersection.MeshRay(UnifiedMeshHighPolyNoSky, ray);

                    // Original logic correct check
                    if (dt < 0) 
                    {
                        // Visible
                        dotproduct[h] = probe.Normal.Value * sunPositions[h];
                         if (dotproduct[h] < 0) dotproduct[h] = 0; // Backface check
                    }
                    else
                    {
                        // Blocked
                        dotproduct[h] = 0;
                    }
                }

                // Map 288 positions to 8760 hours
                for (int h = 0; h < 8760; h++)
                {
                    sg.HourOfYear_To_MDH(h, out int month, out int day, out int hour);
                    var scale = dotproduct[(month * 24) + hour]; // Uses same index logic

                    float rad = (float)(Weather.DirectNormalRadiation[h] * scale);
                    float diff = (float)(Weather.DiffuseHorizontalRadiation[h] * probe.VFtoMaterial["Sky"]);

                    probe.TotalRad[h] = rad + diff;
                    probe.DirRad[h] = rad;
                }

            });

            Console.WriteLine("Compute dMRT...");
            for(int i=0; i<Probes.Count; i++)
            {
                 Probes[i].SolarGain_dMRT = SolarGain.ComputeStanding(Weather, Probes[i].TotalRad, Probes[i].DirRad);
            }
            
            ReportProgress(ref stepCnt, steps);
            Console.WriteLine("Solar gain finished");
        }

        public MRT_Simulation_ResultProto SaveResults(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            var protoResult = new MRT_Simulation_ResultProto(BaseWorkingDir, Weather, Probes, Polys);
            protoResult.WriteToFile(Path.Combine(BaseWorkingDir, "RAD.eddy"));

            Console.WriteLine("Results written");
            ReportProgress(ref stepCnt, steps);

            return protoResult;
        }

        private static float[][] LoadDDSIll(string illFileName)
        {
            if (!File.Exists(illFileName)) return new float[0][]; // Safety handle
            
            string[] illLines = File.ReadAllLines(illFileName);
            int skip = 0;
            for (int i = 0; i < illLines.Length; i++)
            {
                if (illLines[i].Contains("FORMAT")) { skip = i + 2; break; }
            }

            var data = new float[illLines.Length - skip][];
            Parallel.For(skip, illLines.Length, i => {
                var parts = illLines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                data[i - skip] = parts.Skip(1).Select(float.Parse).ToArray();
            });
            
            return data;
        }

        private void LogError(string context, string error)
        {
            Debug.WriteLine($"{context} error: {error}");
            ErrorLog.AppendLine($"{context} error: {error}");
            WriteErrorLog();
        }

        private void WriteErrorLog()
        {
            File.WriteAllText(Path.Combine(BaseWorkingDir, "RadiationErrorLog.log"), ErrorLog.ToString());
        }

        private void ReportProgress(ref int stepCnt, int steps)
        {
            Interlocked.Increment(ref stepCnt);
            int percent = steps > 0 ? 100 * stepCnt / steps : 0;
            Console.WriteLine(ProgressWriter.ProgressKey + percent.ToString(CultureInfo.InvariantCulture));
        }
    }
}