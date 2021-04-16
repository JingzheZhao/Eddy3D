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
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;

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
        public StringBuilder Commands = new StringBuilder();

        public RadiationSystem(string baseWorkingDir, Weather weather, List<RSurface> rsurfaces, List<RProbe> probes, List<RPolygon> polys, Mesh unified)
        {
            BaseWorkingDir = baseWorkingDir;
            Weather = weather;
            RSurfaces = rsurfaces;
            Probes = probes;
            Polys = polys;

            UnifiedMeshHighPolyNoSky = unified;
        }

        private string annualR_dc_ill_out = (@"Rad\output\annualR_dc.ill");
        private string annualR_dcd_ill_out = (@"Rad\output\annualR_dcd.ill");
        private string annualR_dir_ill_out = (@"Rad\output\annual_dir.ill");
        private string annualR_total_ill_out = (@"Rad\output\annual_total.ill");

        public bool RunDDS(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            return RunRadianceDDS(run, ct, steps, ref stepCnt);
        }

        private bool RunRadianceDDS(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Starting DDS Simulation");

            var numberOfProbes = this.Probes.Count;

            var skySubDivDiff = SkySubdivision.r1;
            //var skySubDivDiff = SkySubdivision.r2;
            var skySubDivDir = SkySubdivision.r4;
            int skysubdivdiffuse = 1;
            int skysubdivdirect = 4;

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
            RadianceFiles.writePTS(this.BaseWorkingDir + @"\Rad\sensors.pts", this.Probes.Select(x => x.Point.Value).ToList(), this.Probes.Select(x => x.Normal.Value).ToList());

            // Weather
            var weaname = RadianceFiles.Epw2Wea(this.Weather.epwFilePath, this.BaseWorkingDir + @"\Rad\Output");

            // Sky
            RadianceSkies.Write(this.BaseWorkingDir + @"\Rad\skyglow" + (skySubDivDir) + ".rad", (skySubDivDir));
            RadianceSkies.Write(this.BaseWorkingDir + @"\Rad\skyglow" + (skySubDivDiff) + ".rad", (skySubDivDiff));

            if (run == true)
            {
                string radbin = @"C:\Eddy3D\Common\Radiance\bin";
                string radlib = @"C:\Eddy3D\Common\Radiance\lib";
                char ps = ';';
                Environment.SetEnvironmentVariable("PATH", "." + ps + radlib + ps + radbin + ps + "$PATH");
                Environment.SetEnvironmentVariable("RAYPATH", "." + ps + radlib + ps + radbin + ps + "$RAYPATH");

                // -----------------------------
                // 1 Convert epw to wea tape
                // -----------------------------
                Console.WriteLine("Convert epw to wea tape...");
                var epw2wea = Command.Run(DefaultDirectoriesAndPaths.RadianceDir + @"\epw2wea", new[] { @"""" + this.Weather.epwFilePath + @""" ""Rad/output/" + weaname + @".wea""" },
                  options => options.WorkingDirectory(this.BaseWorkingDir + @"\Rad").CancellationToken(ct));
                epw2wea.Wait();

                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

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

                //#if DEBUG
                Commands.AppendLine(gendaymtxArgs);
                //#endif

                var gendaymtx = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));

                gendaymtx.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                gendaymtx.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
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

                //#if DEBUG
                Commands.AppendLine(gendaymtx2Args);
                //#endif

                var gendaymtx2 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));

                gendaymtx2.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                gendaymtx2.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
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
                    ErrorLog.AppendLine($"oconv command failed with exit code {oconv.Result.ExitCode}: {oconv.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

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
                var CMDrfluxmtx = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                CMDrfluxmtx.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                CMDrfluxmtx.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");

                CMDrfluxmtx.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                CMDrfluxmtx.StandardInput.WriteLine(cmdArgRFLUXMTX);
                CMDrfluxmtx.StandardInput.WriteLine("exit");

                //#if DEBUG
                Commands.AppendLine(cmdArgRFLUXMTX);
                //#endif

                CMDrfluxmtx.Wait();

                if (!CMDrfluxmtx.Result.Success)
                {
                    Debug.WriteLine($"4 command failed with exit code {CMDrfluxmtx.Result.ExitCode}: {CMDrfluxmtx.Result.StandardError}");
                    ErrorLog.AppendLine($"4 command failed with exit code {CMDrfluxmtx.Result.ExitCode}: {CMDrfluxmtx.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // wait for 4 Generate SkyVector Coarse
                // -----------------------------
                gendaymtx.Wait();
                if (!gendaymtx.Result.Success)
                {
                    Debug.WriteLine($"gendaymtx command failed with exit code {gendaymtx.Result.ExitCode}: {gendaymtx.Result.StandardError}");
                    ErrorLog.AppendLine($"gendaymtx command failed with exit code {gendaymtx.Result.ExitCode}: {gendaymtx.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

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
                dctimestep.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dctimestep.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dctimestep.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dctimestep.StandardInput.WriteLine(dctimestepArgs);
                dctimestep.StandardInput.WriteLine("exit");

                //#if DEBUG
                Commands.AppendLine(dctimestepArgs);
                //#endif

                // -----------------------------
                // wait for 5 Create Illum DC
                // -----------------------------

                dctimestep.Wait();

                if (!dctimestep.Result.Success)
                {
                    Debug.WriteLine($"dctimestep command failed with exit code {dctimestep.Result.ExitCode}: {dctimestep.Result.StandardError}");
                    ErrorLog.AppendLine($"dctimestep command failed with exit code {dctimestep.Result.ExitCode}: {dctimestep.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

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

                //#if DEBUG
                Commands.AppendLine(dirCalcArgs1);
                Commands.AppendLine(dirCalcArgs2);
                Commands.AppendLine(dirCalcArgs3);
                //#endif

                var dircalc1 = Command.Run("cmd.exe");

                //Environment.SetEnvironmentVariable("PATH", "." + ps + radlib + ps + radbin +    ps + "$PATH");
                //Environment.SetEnvironmentVariable("RAYPATH", "." + ps + radlib + ps + radbin   + ps + "$RAYPATH");
                //set PATH=%HOME%\msys64\usr\bin;%PATH%

                dircalc1.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dircalc1.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dircalc1.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc1.StandardInput.WriteLine(dirCalcArgs1);
                dircalc1.StandardInput.WriteLine("exit");
                dircalc1.Wait();

                if (!dircalc1.Result.Success)
                {
                    Debug.WriteLine($"dircalc1 command failed with exit code {dircalc1.Result.ExitCode}: {dircalc1.Result.StandardError}");
                    ErrorLog.AppendLine($"dircalc1 command failed with exit code {dircalc1.Result.ExitCode}: {dircalc1.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 7
                // -----------------------------
                var dircalc2 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                dircalc2.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dircalc2.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dircalc2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc2.StandardInput.WriteLine(dirCalcArgs2);
                dircalc2.StandardInput.WriteLine("exit");
                dircalc2.Wait();
                if (!dircalc2.Result.Success)
                {
                    Debug.WriteLine($"dircalc2 command failed with exit code {dircalc2.Result.ExitCode}: {dircalc2.Result.StandardError}");
                    ErrorLog.AppendLine($"dircalc2 command failed with exit code {dircalc2.Result.ExitCode}: {dircalc2.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 8
                // -----------------------------
                var dircalc3 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                dircalc3.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dircalc3.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dircalc3.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dircalc3.StandardInput.WriteLine(dirCalcArgs3);
                dircalc3.StandardInput.WriteLine("exit");
                dircalc3.Wait();
                if (!dircalc3.Result.Success)
                {
                    Debug.WriteLine($"dircalc3 command failed with exit code {dircalc3.Result.ExitCode}: {dircalc3.Result.StandardError}");
                    ErrorLog.AppendLine($"dircalc3 command failed with exit code {dircalc3.Result.ExitCode}: {dircalc3.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

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
                suncoeff.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                suncoeff.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                suncoeff.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                suncoeff.StandardInput.WriteLine(suncoeffArgs1);
                suncoeff.StandardInput.WriteLine(suncoeffArgs2);

                suncoeff.StandardInput.WriteLine("exit");

                suncoeff.Wait();

                //#if DEBUG
                Commands.AppendLine(suncoeffArgs1);
                Commands.AppendLine(suncoeffArgs2);

                //#endif

                if (!suncoeff.Result.Success)
                {
                    Debug.WriteLine($"suncoeff command failed with exit code {suncoeff.Result.ExitCode}: {suncoeff.Result.StandardError}");
                    ErrorLog.AppendLine($"suncoeff command failed with exit code {suncoeff.Result.ExitCode}: {suncoeff.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

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
                    ErrorLog.AppendLine($"oconvdir command failed with exit code {oconvdir.Result.ExitCode}: {oconvdir.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 11 Calculate illuminance sun coefficients
                // -----------------------------
                Console.WriteLine("Calculate illuminance sun coefficients for illuminance calculations...");
                string blackWithSunsin = (@"Rad\output\sceneBlackSuns.oct");
                string cddmtxout = (@"Rad\output\cdsDDS.mtx");
                string rcontribArgs1 = DefaultDirectoriesAndPaths.RadianceDir + @"\rcontrib -I+ -ab 1 -y " + sensorCnt + @" -n 16 -ad 256 -lw 1.0e-3 -dc 1 -dt 0 -dj 0 -faf -e MF:" + skysubdivdirect + @" -f """ + DefaultDirectoriesAndPaths.RadianceLibDir + @"\reinhart.cal"" -b rbin -bn Nrbins -m solar " + blackWithSunsin + @" < " + ptsin + @" > " + cddmtxout;
                var rcontrib = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                rcontrib.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                rcontrib.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                rcontrib.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                rcontrib.StandardInput.WriteLine(rcontribArgs1);
                rcontrib.StandardInput.WriteLine("exit");

                rcontrib.Wait();

                //#if DEBUG
                Commands.AppendLine(rcontribArgs1);

                //#endif

                if (!rcontrib.Result.Success)
                {
                    Debug.WriteLine($"suncoeff command failed with exit code {rcontrib.Result.ExitCode}: {rcontrib.Result.StandardError}");
                    ErrorLog.AppendLine($"suncoeff command failed with exit code {rcontrib.Result.ExitCode}: {rcontrib.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 12 Started earlier
                // -----------------------------

                gendaymtx2.Wait();
                if (!gendaymtx2.Result.Success)
                {
                    Debug.WriteLine($"gendaymtx2 command failed with exit code {gendaymtx2.Result.ExitCode}: {gendaymtx2.Result.StandardError}");
                    ErrorLog.AppendLine($"gendaymtx2 command failed with exit code {gendaymtx2.Result.ExitCode}: {gendaymtx2.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 13 Create Illum Dir
                // -----------------------------
                Console.WriteLine("Create Illum Dir...");
                string dctimestep2Args = DefaultDirectoriesAndPaths.RadianceDir + @"\dctimestep " + cddmtxout + @" " + smxsunout + @" | rmtxop -fa -t -c 0.265 0.670 0.065 - > " + annualR_dir_ill_out;
                var dctimestep2 = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                dctimestep2.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dctimestep2.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                dctimestep2.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                dctimestep2.StandardInput.WriteLine(dctimestep2Args);
                dctimestep2.StandardInput.WriteLine("exit");

                //#if DEBUG
                Commands.AppendLine(dctimestep2Args);

                //#endif

                dctimestep2.Wait();
                if (!dctimestep2.Result.Success)
                {
                    Debug.WriteLine($"dctimestep2 dir command failed with exit code {dctimestep2.Result.ExitCode}: {dctimestep2.Result.StandardError}");
                    ErrorLog.AppendLine($"dctimestep2 dir command failed with exit code {dctimestep2.Result.ExitCode}: {dctimestep2.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

                // -----------------------------
                // 14 Combine Results
                // -----------------------------
                Console.WriteLine("Combine Results...");
                string rmtxopArgs = DefaultDirectoriesAndPaths.RadianceDir + @"\rmtxop " + annualR_dc_ill_out + @" + -s -1 " + annualR_dcd_ill_out + @" + " + annualR_dir_ill_out + @" > " + annualR_total_ill_out;

                var rmtxop = Command.Run("cmd.exe", new[] { "" },
                  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(ct));
                rmtxop.StandardInput.WriteLine("set PATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                rmtxop.StandardInput.WriteLine("set RAYPATH=" + ps + radlib + ps + radbin + ps + "%PATH%");
                rmtxop.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
                rmtxop.StandardInput.WriteLine(rmtxopArgs);
                rmtxop.StandardInput.WriteLine("exit");
                rmtxop.Wait();

                //#if DEBUG
                Commands.AppendLine(rmtxopArgs);

                //#endif

                if (!rmtxop.Result.Success)
                {
                    Debug.WriteLine($"dctimestep dir command failed with exit code {rmtxop.Result.ExitCode}: {rmtxop.Result.StandardError}");
                    ErrorLog.AppendLine($"dctimestep dir command failed with exit code {rmtxop.Result.ExitCode}: {rmtxop.Result.StandardError}");
                    WriteErrorLog();
                    return false;
                }
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
            }

            return true;
        }

        public void LoadDDSData(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            // -----------------------------
            // 15 Compute dMRT
            // -----------------------------
            Console.WriteLine("Compute dMRT");

            var totalIll = LoadDDSIll((this.BaseWorkingDir + @"\" + annualR_total_ill_out));
            //var diffIll = LoadDDSIll((this.BaseWorkingDir + @"\Rad\Output\annual_total.ill"));
            var dirIll = LoadDDSIll((this.BaseWorkingDir + @"\" + annualR_dir_ill_out));

            float[][] dMRT = SolarGain.ComputeStanding(this.Weather, totalIll, dirIll);

            Interlocked.Increment(ref stepCnt);
            Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

            // -----------------------------
            // 16 Load results
            // -----------------------------

            for (int i = 0; i < this.Probes.Count; i++)
            {
                this.Probes[i].TotalRad = new float[totalIll.Length];
                this.Probes[i].DirRad = new float[dirIll.Length];
                this.Probes[i].SolarGain_dMRT = new float[dMRT.Length];
                for (int h = 0; h < totalIll.Length; h++)
                {
                    this.Probes[i].TotalRad[h] = totalIll[h][i];
                    this.Probes[i].DirRad[h] = dirIll[h][i];
                    this.Probes[i].SolarGain_dMRT[h] = dMRT[h][i];
                }
            }
        }

        private void WriteErrorLog()
        {
            // ---------------------
            // Error Logs
            // ---------------------

            //#if DEBUG
            File.WriteAllText(Path.Combine(this.BaseWorkingDir, "RadiationErrorLog.log"), this.ErrorLog.ToString());
            File.WriteAllText(Path.Combine(this.BaseWorkingDir, "Commands.log"), this.Commands.ToString());

            //#endif
        }

        public void RunDirectRayCast(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            RunSimpleRadiation(run, ct, steps, ref stepCnt);
        }

        private void RunSimpleRadiation(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Computing radiation and dMRT...");

            SolarGeometry sg = new SolarGeometry();
            int vcnt = 0;
            Vector3d[] sunPositions = new Vector3d[12 * 24];
            var el = new List<double>();
            for (int m = 0; m < 12; m++)
            {
                for (int h = 0; h < 24; h++)
                {
                    int hourOfYear = sg.HourInYear(m, 0, h);

                    double _el = Weather.SolarElevation[hourOfYear];
                    double _az = Weather.SolarAzi[hourOfYear];

                    if (_el > 3.0)
                    {
                        double x = Math.Cos(sg.deg2rad(90 - _az)) * Math.Cos(sg.deg2rad(_el));
                        double y = Math.Sin(sg.deg2rad(90 - _az)) * Math.Cos(sg.deg2rad(_el));
                        double z = Math.Sin(sg.deg2rad(_el));
                        sunPositions[vcnt] = new Vector3d(x, y, z);
                    }
                    else
                    {
                        sunPositions[vcnt] = Vector3d.Zero;
                    }
                    { }
                    vcnt++;
                }
            }

            //Parallel.For(0, Probes.Count, i =>
            for (int i = 0; i < Probes.Count; i++)
            {
                Probes[i].TotalRad = new float[8760];
                Probes[i].DirRad = new float[8760];

                double[] dotproduct = new double[12 * 24];
                bool[] inDirSunlight = new bool[12 * 24];

                for (int h = 0; h < sunPositions.Length; h++)
                {
                    if (sunPositions[h] == Vector3d.Zero) { continue; }

                    var dt = Rhino.Geometry.Intersect.Intersection.MeshRay(UnifiedMeshHighPolyNoSky, new Ray3d(Probes[i].Point.Value, sunPositions[h]));

                    if (dt < 0.1)
                    {
                        inDirSunlight[h] = true;
                        var dot = Probes[i].Normal.Value * sunPositions[h];

                        if (inDirSunlight[h]) { dotproduct[h] = dot; }
                        else { dotproduct[h] = 0; }
                    }
                    else { inDirSunlight[h] = false; }
                }

                for (int h = 0; h < 8760; h++)
                {
                    int month = 0;
                    int day = 0;
                    int hour = 0;
                    sg.HourOfYear_To_MDH(h, out month, out day, out hour);

                    var scale = dotproduct[(month * 24) + hour];

                    float rad = (float)(Weather.DirectNormalRadiation[h] * scale);

                    float diff = (float)(Weather.DiffuseHorizontalRadiation[h] * Probes[i].VFtoMaterial["Sky"]);

                    Probes[i].TotalRad[h] = rad + diff;
                    Probes[i].DirRad[h] = rad;
                }

                Console.WriteLine("Compute dMRT for probe " + i);
                Probes[i].SolarGain_dMRT = SolarGain.ComputeStanding(Weather, Probes[i].TotalRad, Probes[i].DirRad);

                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
            }//);

            Console.WriteLine("Solar gain finished");
        }

        public MRT_Simulation_ResultProto SaveResults(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            // -----------------------------
            // 16 Write results
            // -----------------------------
            var prep = PrepareProtoBufSingleton.Instance;

            var protoResult = new MRT_Simulation_ResultProto(this.BaseWorkingDir, this.Weather, this.Probes, this.Polys);
            protoResult.WriteToFile(this.BaseWorkingDir + @"\RAD.eddy");

            Console.WriteLine("Results written");
            Interlocked.Increment(ref stepCnt);
            Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

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
    }
}