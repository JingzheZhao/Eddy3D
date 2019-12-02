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
        // static string RadianceDir = @"C:\Eddy3d\Radiance";

        public double[][] dirIll;
        public double[][] difIll;

        public TwoPhaseDDS(string baseWorkingDir, Mesh BuildingGeometry, List<Point3d> probes, Weather weather, string RadianceDir = @"C:\Eddy3d\Radiance")
        {
            var skySubDivDiff = SkySubdivision.r1;
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

            RadianceFiles.MeshProc(daysimMesh, baseWorkingDir + @"Rad\scene.rad", "Generic_20", radMat);
            RadianceFiles.MeshProc(daysimMesh, baseWorkingDir + @"Rad\sceneBlack.rad", "Black", radMatBlack);

            // Write Probes
            RadianceFiles.writePTS(baseWorkingDir + @"\Rad\sensors.pts", probes);
            // Weather
            var weaname = RadianceFiles.Epw2Wea(weather.epwFilePath, baseWorkingDir + @"\Rad\Output");
            // Sky
            Skies.Write(baseWorkingDir + @"Rad\skyglow.rad", skySubDivDiff);

            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.WorkingDirectory = baseWorkingDir + @"\Rad\";
                process.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");

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
                process.StandardInput.WriteLine(CommandLineArgs(RadianceDir, baseWorkingDir, probes.Count, weaname, 3, 10000, skySubDivDiff, skySubDivDir, Environment.ProcessorCount - 1));
                process.StandardInput.WriteLine("exit");

                process.WaitForExit();
            }

            //TDOD: Parse ILL files
        }

        public static string CommandLineArgs(string RadianceDir, string baseWorkingDir, int sensorCnt, string weaname, int ab, int ad, SkySubdivision diffSky, SkySubdivision dirSky, int n)
        {
            int skysubdiv = (int)diffSky;
            int skysubdivdirect = (int)diffSky;

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
        }
    }
}