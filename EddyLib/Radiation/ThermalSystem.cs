using EddyLib.Properties;
using EddyLib.Radiation;
using EddyLib.UI;
using Medallion.Shell;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    public class ThermalSystem
    {
        public int methodsteps = 52 + 2; // energyplus prints 52 lines

        public double IgnoreSmallFacesCutoff = 0.1;

        public double CummulativeViewFactorCutoff;


        public string ProjectName = "EddySim";
        public string BaseWorkingDir = "";
        public Weather Weather;

        public List<RProbe> Probes;
        public List<RPolygon> Polys;

        public double[] AmbientTemperature;
        public double[] SkyTemperature;


        public Mesh UnifiedMeshLowPolyNoSky;


        public ThermalSystem(string baseWorkingDir, Weather weather, List<RProbe> probes, List<RPolygon> polys, Mesh lowPoly, double vf_cutoff = 0.05, double smallf_cutoff = 0.1)
        {
            BaseWorkingDir = baseWorkingDir;
            Weather = weather;
            Probes = probes;
            Polys = polys;
            UnifiedMeshLowPolyNoSky = lowPoly;

            CummulativeViewFactorCutoff = vf_cutoff;
            IgnoreSmallFacesCutoff = smallf_cutoff;


            AmbientTemperature = Weather.DryBulbTemp;


            // store ambient temperature in surfaces
            foreach (var poly in polys)
            {
                if (poly.SimulationType == SimulationType.Ambient)
                    poly.TemperatureOverride = RPolygon.toFloatArray(AmbientTemperature);
            }

            var sky = new SkyTemperatureModel(Weather.DewPointTemp, Weather.DryBulbTemp, Weather.TotalSkyCover, Weather.RelativeHumidity, true, SkyTemperatureModel.CalculationType.DefaultClarkAllen);
            SkyTemperature = sky.Temp;
        }



        public List<EsoResult> RunEP(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {


            if (run == true)
            {
                // -----------------------------
                // 0 Prepare EPJSON
                // -----------------------------
                var epjsonObject = new EpJsonFormat();

                Dictionary<string, Material> AllMats = new Dictionary<string, Material>();
                Dictionary<string, Construction> AllCons = new Dictionary<string, Construction>();
                Dictionary<string, MaterialRoofVegetation> AllVeget = new Dictionary<string, MaterialRoofVegetation>();


                int surfIndex = 0;
                int groundIndex = 0;
                int shaderIndex = 0;
                foreach (var s in this.Polys)
                {
                    if (s.Type == RadiationSurfaceType.Sky) { continue; }
                    if (s.SeenByProbes < CummulativeViewFactorCutoff) { continue; }

                    else if (s.Type == RadiationSurfaceType.Building && s.SimulationType == SimulationType.Simulated)
                    {
                        var epsurf = new BuildingSurfaceDetailed();
                        epsurf.ConstructionName = "DefaultConstruction";
                        epsurf.Vertices = new List<DetailedVertex>();
                        foreach (var v in s.Mesh.Value.Vertices)
                        {
                            var dv = new DetailedVertex();
                            dv.X = v.X + s.Normal.Value.X * 0.05;
                            dv.Y = v.Y + s.Normal.Value.Y * 0.05;
                            dv.Z = v.Z + s.Normal.Value.Z * 0.05;
                            epsurf.Vertices.Add(dv);
                        }
                        epsurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;


                        // set constructions
                        if (s.Parent != null && s.Parent.Settings != null)
                        {
                            epsurf.ConstructionName = s.Parent.Settings.Name;

                            if (!AllMats.ContainsKey(s.Parent.Settings.Name) && !AllCons.ContainsKey(s.Parent.Settings.Name))
                            {
                                AllMats.Add(s.Parent.Settings.Name, s.Parent.Settings.GetMaterial());
                                AllCons.Add(s.Parent.Settings.Name, s.Parent.Settings.GetConstruction());
                            }
                        }


                        epjsonObject.AllThermalSurfaces.Add("S_" + s.ID.ToString(), epsurf);
                        surfIndex++;
                    }
                    else if (s.Type == RadiationSurfaceType.Ground && s.SimulationType == SimulationType.Simulated)
                    {
                        var epsurf = new BuildingSurfaceDetailed();
                        epsurf.ConstructionName = "DefaultConstruction";
                        epsurf.Vertices = new List<DetailedVertex>();
                        foreach (var v in s.Mesh.Value.Vertices)
                        {
                            var dv = new DetailedVertex();
                            dv.X = v.X + s.Normal.Value.X * 0.05;
                            dv.Y = v.Y + s.Normal.Value.Y * 0.05;
                            dv.Z = v.Z + s.Normal.Value.Z * 0.05;
                            epsurf.Vertices.Add(dv);
                        }
                        epsurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;


                        // set constructions
                        if (s.Parent != null && s.Parent.Settings != null)
                        {
                            epsurf.ConstructionName = s.Parent.Settings.Name;

                            if (!AllMats.ContainsKey(s.Parent.Settings.Name) && !AllCons.ContainsKey(s.Parent.Settings.Name))
                            {
                                AllMats.Add(s.Parent.Settings.Name, s.Parent.Settings.GetMaterial());
                                AllCons.Add(s.Parent.Settings.Name, s.Parent.Settings.GetConstruction());
                            }
                        }


                        epjsonObject.AllThermalSurfaces.Add("G_" + s.ID.ToString(), epsurf);
                        groundIndex++;
                    }
                    else if (s.Type == RadiationSurfaceType.Vegetation && s.SimulationType == SimulationType.Simulated)
                    {
                        var epsurf = new BuildingSurfaceDetailed();
                        epsurf.ConstructionName = "GreenRoofConstruction";
                        epsurf.Vertices = new List<DetailedVertex>();
                        foreach (var v in s.Mesh.Value.Vertices)
                        {
                            var dv = new DetailedVertex();
                            dv.X = v.X + s.Normal.Value.X * 0.05;
                            dv.Y = v.Y + s.Normal.Value.Y * 0.05;
                            dv.Z = v.Z + s.Normal.Value.Z * 0.05;
                            epsurf.Vertices.Add(dv);
                        }
                        epsurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;


                        // set constructions
                        if (s.Parent != null && s.Parent.VegSettings != null)
                        {
                            epsurf.ConstructionName = s.Parent.VegSettings.Name;

                            if (!AllVeget.ContainsKey(s.Parent.VegSettings.Name) && !AllCons.ContainsKey(s.Parent.VegSettings.Name))
                            {
                                AllVeget.Add(s.Parent.VegSettings.Name, s.Parent.VegSettings.GetMaterial());
                                AllCons.Add(s.Parent.VegSettings.Name, s.Parent.VegSettings.GetConstruction());
                            }
                        }



                        epjsonObject.AllThermalSurfaces.Add("V_" + s.ID.ToString(), epsurf);
                        groundIndex++;
                    }
                }

                // -----------------------------
                // 0 Add shaders
                // -----------------------------
                Mesh unifiedMeshPlanar = this.UnifiedMeshLowPolyNoSky.DuplicateMesh();
                int reduced = unifiedMeshPlanar.Faces.ConvertNonPlanarQuadsToTriangles(0.0, Rhino.RhinoMath.UnsetValue, 0);
                int collapsed = unifiedMeshPlanar.CollapseFacesByArea(IgnoreSmallFacesCutoff, 10000);
                foreach (var s in unifiedMeshPlanar.Faces)
                {
                    var epsurf = new ShadingBuildingDetailed();
                    epsurf.Vertices = new List<DetailedVertex>();

                    if (s.IsQuad)
                    {
                        Point3d v0 = new Point3d(unifiedMeshPlanar.Vertices[s.A]);
                        Point3d v1 = new Point3d(unifiedMeshPlanar.Vertices[s.B]);
                        Point3d v2 = new Point3d(unifiedMeshPlanar.Vertices[s.C]);
                        Point3d v3 = new Point3d(unifiedMeshPlanar.Vertices[s.D]);

                        Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                        Vector3d n2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);

                        var facearea = n1.Length * 0.5 + n2.Length * 0.5;

                        if (facearea < IgnoreSmallFacesCutoff + 0.1) continue;

                        epsurf.Vertices.Add(new DetailedVertex()
                        {
                            X = v0.X,
                            Y = v0.Y,
                            Z = v0.Z
                        }
                        );
                        epsurf.Vertices.Add(new DetailedVertex()
                        {
                            X = v1.X,
                            Y = v1.Y,
                            Z = v1.Z
                        }
                       );
                        epsurf.Vertices.Add(new DetailedVertex()
                        {
                            X = v2.X,
                            Y = v2.Y,
                            Z = v2.Z
                        }
                       );
                        epsurf.Vertices.Add(new DetailedVertex()
                        {
                            X = v3.X,
                            Y = v3.Y,
                            Z = v3.Z
                        }
                       );
                        epsurf.NumberOfVertices = 4;
                    }
                    else
                    {
                        Point3d v0 = new Point3d(unifiedMeshPlanar.Vertices[s.A]);
                        Point3d v1 = new Point3d(unifiedMeshPlanar.Vertices[s.B]);
                        Point3d v2 = new Point3d(unifiedMeshPlanar.Vertices[s.C]);

                        Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);

                        var facearea = n1.Length * 0.5;
                        if (facearea < IgnoreSmallFacesCutoff+0.1) continue;

                        epsurf.Vertices.Add(new DetailedVertex()
                        {
                            X = v0.X,
                            Y = v0.Y,
                            Z = v0.Z
                        }
                        );
                        epsurf.Vertices.Add(new DetailedVertex()
                        {
                            X = v1.X,
                            Y = v1.Y,
                            Z = v1.Z
                        }
                       );
                        epsurf.Vertices.Add(new DetailedVertex()
                        {
                            X = v2.X,
                            Y = v2.Y,
                            Z = v2.Z
                        }
                       );

                        epsurf.NumberOfVertices = 3;
                    }
                    epjsonObject.AllShaders.Add("Shader" + shaderIndex, epsurf);
                    shaderIndex++;

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







                // Material and Construction Injection Logic
                string injectMaterials = "";
                string injectConstructions = "";
                string injectVegetation = "";

                injectMaterials += JsonConvert.SerializeObject(AllMats, Formatting.Indented).Trim().Trim('{', '}').Trim(); ;
                if (!String.IsNullOrWhiteSpace(injectMaterials)) injectMaterials += ",";
                injectConstructions += JsonConvert.SerializeObject(AllCons, Formatting.Indented).Trim().Trim('{', '}').Trim(); ;
                if (!String.IsNullOrWhiteSpace(injectConstructions)) injectConstructions += ",";
                epjson = epjson.Replace("\"@@MATERIALS@@\": null,", injectMaterials);
                epjson = epjson.Replace("\"@@CONSTRUCTIONS@@\": null,", injectConstructions);

                injectVegetation += JsonConvert.SerializeObject(AllVeget, Formatting.Indented).Trim().Trim('{', '}').Trim(); ;
                if (!String.IsNullOrWhiteSpace(injectVegetation)) injectVegetation += ",";
                epjson = epjson.Replace("\"@@VEGETATION@@\": null,", injectVegetation);














                Console.WriteLine("Writing EnergyPlus input files...");

                File.WriteAllText(epjsonfile, epjson);

                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 2 Run EnergyPlus
                // -----------------------------
                Console.WriteLine("Run EnergyPlus...");
                var energyPlus = Command.Run(DefaultDirectoriesAndPaths.EnergyPlusDir + @"\energyplus.exe", new[] { "-r", "-w",  Path.GetFullPath(Weather.epwFilePath), "-p", ProjectName, epjsonfile },
                  options => options.WorkingDirectory(this.BaseWorkingDir + @"\Ep").CancellationToken(ct));

                int cnt = 0;

                string line;
                while ((line = energyPlus.StandardOutput.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    Interlocked.Increment(ref stepCnt);
                    Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
                    cnt++;
                }

                energyPlus.Wait();
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));


                // -----------------------------
                // 3 Read data and store with Polygons
                // -----------------------------
                string esofile = (this.BaseWorkingDir + @"\Ep\" + ProjectName + "out.eso");

                if (File.Exists(esofile))
                {
                    Console.WriteLine("Read results...");
                    var res = EsoReader.LoadEsoFile(esofile);

                    foreach (var p in this.Polys)
                    {
                        if (p.Type == RadiationSurfaceType.Vegetation && p.SimulationType == SimulationType.Simulated)
                        {
                            var tag = "Green Roof Vegetation Temperature";
                            if (res.Any(x => x.tag == tag))
                            {
                                p.SurfaceTemperature = RPolygon.toFloatArray(res.First(x => x.tag == tag).values.ToArray());
                            }
                        }
                        else if (p.Type == RadiationSurfaceType.Ground && p.SimulationType == SimulationType.Simulated)
                        {
                            if (res.Any(x => x.zone == "G_" + p.ID.ToString()))
                            {
                                p.SurfaceTemperature = RPolygon.toFloatArray(res.First(x => x.zone == "G_" + p.ID.ToString()).values.ToArray());
                            }
                        }
                        else if (p.Type == RadiationSurfaceType.Building && p.SimulationType == SimulationType.Simulated)
                        {
                            if (res.Any(x => x.zone == "S_" + p.ID.ToString()))
                            {
                                p.SurfaceTemperature = RPolygon.toFloatArray(res.First(x => x.zone == "S_" + p.ID.ToString()).values.ToArray());
                            }
                        }
                    }

                    Interlocked.Increment(ref stepCnt);
                    Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
                    return res;
                }

            }
            return null;

        }

        public void ComputeMRT(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Computing MRT at probe level...");

            Parallel.For(0, Probes.Count, x =>
            //for (int x = 0; x < Probes.Count; x++)
            {

                var p = Probes[x];

                p.LongWave_MRT = new float[8760];

                for (int i = 0; i < p.VFtoPolys.Length; i++)
                {
                    var poly = this.Polys[i];

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
                    else if (poly.SimulationType == SimulationType.TemperatureInput && poly.TemperatureOverride != null)
                    {
                        for (int h = 0; h < p.LongWave_MRT.Length; h++)
                        {
                            p.LongWave_MRT[h] += (float)(poly.TemperatureOverride[h] * p.VFtoPolys[i]);
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


            });

            Console.WriteLine("MRT calculaiton complete...");

        }

        public MRT_Simulation_ResultProto SaveResults(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Writing Results...");

            // -----------------------------
            // Write results
            // -----------------------------
            var prep = PrepareProtoBufSingleton.Instance;

            Stopwatch sp = new Stopwatch();
            sp.Start();

            var protoResult = new MRT_Simulation_ResultProto(this.BaseWorkingDir, this.Weather, this.Probes, this.Polys);

            protoResult.WriteToFile(this.BaseWorkingDir + @"\MRT.eddy");

            Debug.WriteLine("Results Proto: " + sp.ElapsedMilliseconds);



            Console.WriteLine("Results written");
            Interlocked.Increment(ref stepCnt);
            Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

            return protoResult;

        }


    }
}
