using EddyLib.Properties;
using EddyLib.Radiation;
using EddyLib.UI;
using Medallion.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Thermal
{
    public class ThermalSystem
    {

        private double pct = 0;
        private double steps = 18;
        private double stepCnt = 0;





        public string ProjectName = "";
        public string BaseWorkingDir = "";
        public List<RSurface> RSurfaces;
        public RSystem Radio;
        public Weather Weather;
        public ThermalSystem(string filename, string baseWorkingDir, Weather weather, List<RSurface> rsurfaces, RSystem radio)
        {

            ProjectName = filename;
            BaseWorkingDir = baseWorkingDir;
            RSurfaces = rsurfaces;
            Radio = radio;
            Weather = weather;

        }

        public List<EsoResult> RunEP(bool run, CancellationToken ct)
        {


            if (run == true)
            {
                // -----------------------------
                // 1 Write EPJSON
                // -----------------------------
                string epjsonfile = (this.BaseWorkingDir + @"\Ep\" + ProjectName + ".epjson");

                Directory.CreateDirectory(this.BaseWorkingDir + @"\Ep");

                var str = System.Text.Encoding.Default.GetString(Resources.Box);

                Console.WriteLine("Writing EnergyPlus input files...");

                File.WriteAllText(epjsonfile, str);

                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 2 Run EnergyPlus
                // -----------------------------
                Console.WriteLine("Run EnergyPlus...");
                var energyPlus = Command.Run(DefaultDirectoriesAndPaths.EnergyPlusDir + @"\energyplus.exe", new[] { "-w", Path.GetFullPath(Weather.epwFilePath), "-p", ProjectName, epjsonfile },
                  options => options.WorkingDirectory(this.BaseWorkingDir + @"\Ep"));
                energyPlus.Wait();
                if (!energyPlus.Result.Success)
                {
                    Debug.WriteLine($"EnergyPlus command failed with exit code {energyPlus.Result.ExitCode}: {energyPlus.Result.StandardError}");
                    return null;
                }
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 3 Read data
                // -----------------------------
                string esofile = (this.BaseWorkingDir + @"\Ep\" + ProjectName + "out.eso");

                if (File.Exists(esofile))
                {
                    Console.WriteLine("Read results...");
                    var res = EsoReader.LoadEsoFile(esofile);
                    return res;
                }

            }
            return null;

        }

    }
}
