using EddyLib.Properties;
using EddyLib.Radiation;
using EddyLib.UI;
using Medallion.Shell;
using Newtonsoft.Json;
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



        RadiationSimulationSystem RSystem;

        public string ProjectName = "";
        public string BaseWorkingDir = "";
         public RadiositySystem Radio;
        public Weather Weather;
        public ThermalSystem(RadiationSimulationSystem sys)
        {
            RSystem = sys;

            ProjectName = RSystem.ProjectName;
            BaseWorkingDir = RSystem.BaseWorkingDir;
             Radio = RSystem.Radio;
            Weather = RSystem.Weather;

        }

        public List<EsoResult> RunEP(bool run, CancellationToken ct)
        {


            if (run == true)
            {
                // -----------------------------
                // 0 Prepare EPJSON
                // -----------------------------
                var epjsonObject = new EPJson();

                int surfIndex = 0; 

                foreach (var s in this.Radio.polys) {

 
                    if (!s.matName.Contains("Building") && !s.matName.Contains("Ground")) continue;

 
                    var epsurf = new BuildingSurfaceDetailed();
                    epsurf.Vertices = new List<DetailedVertex>();
                    foreach (var v in s.m.Vertices) {

                        var dv = new DetailedVertex();
                        dv.X = v.X;
                        dv.Y = v.Y;
                        dv.Z = v.Z;

                        epsurf.Vertices.Add(dv);

                      
                    }

                    epsurf.NumberOfVertices = s.m.Vertices.Count;

                    epjsonObject.AllThermalSurfaces.Add(s.matName + surfIndex, epsurf);
                    surfIndex++;
                }












                // -----------------------------
                // 1 Write EPJSON
                // -----------------------------
                string epjsonfile = (this.BaseWorkingDir + @"\Ep\" + ProjectName + ".epjson");

                Directory.CreateDirectory(this.BaseWorkingDir + @"\Ep");

                var str = System.Text.Encoding.Default.GetString(Resources.Box);


                string inject = JsonConvert.SerializeObject(epjsonObject, Formatting.Indented).Trim().Trim('{', '}').Trim(); ;
                inject += ",";

                string epjson = str.Replace("\"@@SURFS@@\": null,", inject);
            

                Console.WriteLine("Writing EnergyPlus input files...");

                File.WriteAllText(epjsonfile, epjson);

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
