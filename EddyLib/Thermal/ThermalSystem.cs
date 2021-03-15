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
        private double steps = 52 + 2; // energyplus prints 52 lines
        private double stepCnt = 0;


        MRTSimulationResultProto RSystem;

        public string ProjectName = "";
        public string BaseWorkingDir = "";
        public Weather Weather;

        public double[] AmbientTemperature;
        public double[] SkyTemperature;

        public ThermalSystem(MRTSimulationResultProto sys)
        {
            RSystem = sys;

            ProjectName = RSystem.ProjectName;
            BaseWorkingDir = RSystem.BaseWorkingDir;
            Weather = RSystem.Weather;

            AmbientTemperature = Weather.DryBulbTemp;

            var sky = new SkyTemperatureModel(Weather.DewPointTemp, Weather.DryBulbTemp, Weather.TotalSkyCover, Weather.RelativeHumidity, true, SkyTemperatureModel.CalculationType.DefaultClarkAllen);
            SkyTemperature = sky.Temp;
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
                int groundIndex = 0;
                int shaderIndex = 0;
                foreach (var s in this.RSystem.Polys)
                {
                    if (s.Type == RadiationSurfaceType.Sky) { continue; }

                    if (s.SeenByProbes < 0.05) { continue; }


                    else if (s.Type == RadiationSurfaceType.Building)
                    {
                        var epsurf = new BuildingSurfaceDetailed();
                        epsurf.ConstructionName = "RedBrick";
                        epsurf.Vertices = new List<DetailedVertex>();
                        foreach (var v in s.Mesh.Value.Vertices)
                        {
                            var dv = new DetailedVertex();
                            dv.X = v.X;
                            dv.Y = v.Y;
                            dv.Z = v.Z;
                            epsurf.Vertices.Add(dv);
                        }
                        epsurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;
                        epjsonObject.AllThermalSurfaces.Add(s.ID.ToString(), epsurf);
                        surfIndex++;
                    }
                    else if (s.Type == RadiationSurfaceType.Ground)
                    {
                        var epsurf = new BuildingSurfaceDetailed();
                        epsurf.ConstructionName = "Asphalt";
                        epsurf.Vertices = new List<DetailedVertex>();
                        foreach (var v in s.Mesh.Value.Vertices)
                        {
                            var dv = new DetailedVertex();
                            dv.X = v.X;
                            dv.Y = v.Y;
                            dv.Z = v.Z;
                            epsurf.Vertices.Add(dv);
                        }
                        epsurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;
                        epjsonObject.AllThermalSurfaces.Add(s.ID.ToString(), epsurf);
                        groundIndex++;
                    }
                    else if (s.Type == RadiationSurfaceType.Vegetation)
                    {
                        var epsurf = new BuildingSurfaceDetailed();
                        epsurf.ConstructionName = "GreenRoofConstruction";
                        epsurf.Vertices = new List<DetailedVertex>();
                        foreach (var v in s.Mesh.Value.Vertices)
                        {
                            var dv = new DetailedVertex();
                            dv.X = v.X;
                            dv.Y = v.Y;
                            dv.Z = v.Z;
                            epsurf.Vertices.Add(dv);
                        }
                        epsurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;
                        epjsonObject.AllThermalSurfaces.Add(s.ID.ToString(), epsurf);
                        groundIndex++;
                    }
                    else if (s.Type == RadiationSurfaceType.Tree)
                    {
                        var epsurf = new ShadingBuildingDetailed();
                        epsurf.Vertices = new List<DetailedVertex>();
                        foreach (var v in s.Mesh.Value.Vertices)
                        {
                            var dv = new DetailedVertex();
                            dv.X = v.X;
                            dv.Y = v.Y;
                            dv.Z = v.Z;
                            epsurf.Vertices.Add(dv);
                        }
                        epsurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;
                        epjsonObject.AllShaders.Add("Shader" + shaderIndex, epsurf);
                        shaderIndex++;
                    }
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

                int cnt = 0;

                string line;
                while ((line = energyPlus.StandardOutput.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    stepCnt++;
                    pct = 100 * stepCnt / steps;
                    Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));
                    cnt++;
                }


                energyPlus.Wait();
                //if (!energyPlus.Result.Success)
                //{
                //    Debug.WriteLine($"EnergyPlus command failed with exit code {energyPlus.Result.ExitCode}: {energyPlus.Result.StandardError}");
                //    return null;
                //}
                stepCnt++;
                pct = 100 * stepCnt / steps;
                Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 3 Read data and store with Polygons
                // -----------------------------
                string esofile = (this.BaseWorkingDir + @"\Ep\" + ProjectName + "out.eso");

                if (File.Exists(esofile))
                {
                    Console.WriteLine("Read results...");
                    var res = EsoReader.LoadEsoFile(esofile);

                    foreach (var p in this.RSystem.Polys)
                    {

                        if (res.Any(x => x.zone == p.ID.ToString()))
                        {

                            p.SurfaceTemperature = res.First(x => x.zone == p.ID.ToString()).values.ToArray();


                        }
                    }



                    stepCnt++;
                    pct = 100 * stepCnt / steps;
                    Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));
                    return res;
                }

            }
            return null;

        }



        public void ComputeMRT(bool run, CancellationToken ct)
        {


            foreach (var p in RSystem.Probes)
            {

                p.LongWave_MRT = new float[8760];

                for (int i = 0; i < p.VFtoPolys.Length; i++)
                {


                    var poly = RSystem.Polys[i];



                    /// ------------------
                    /// Logic for picking surface temperatures. Come from different sources depending on the object type.
                    /// ------------------

                    if (poly.SurfaceTemperature != null)
                    {
                        for (int h = 0; h < p.LongWave_MRT.Length; h++)
                        {
                            p.LongWave_MRT[h] += (float)(poly.SurfaceTemperature[h] * p.VFtoPolys[i]);
                        }
                    }
                    else if (poly.Type == RadiationSurfaceType.Sky)
                    {
                        for (int h = 0; h < p.LongWave_MRT.Length; h++)
                        {
                            p.LongWave_MRT[h] += (float)(SkyTemperature[h] * p.VFtoPolys[i]);
                        }
                    }
                    else
                    {
                        for (int h = 0; h < p.LongWave_MRT.Length; h++)
                        {
                            p.LongWave_MRT[h] += (float)(AmbientTemperature[h] * p.VFtoPolys[i]);
                        }
                    }


                }


            }


        }

        public MRTSimulationResultProto SaveResults(bool run, CancellationToken ct)
        {

            // -----------------------------
            // Write results
            // -----------------------------
            var prep = PrepareProtoBufSingleton.Instance;

            Stopwatch sp = new Stopwatch();
            sp.Start();


            RSystem.WriteToFile(this.BaseWorkingDir + @"\" + this.ProjectName + ".mrt.eddy");

            Debug.WriteLine("Results Proto: " + sp.ElapsedMilliseconds);



            Console.WriteLine("Results written");
            stepCnt++;
            pct = 100 * stepCnt / steps;
            Console.WriteLine(ProgressWriter.ProgressKey + pct.ToString(CultureInfo.InvariantCulture));

            return RSystem;

        }


    }
}
