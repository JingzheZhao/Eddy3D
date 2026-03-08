using EddyLib.Helpers;
using EddyLib.Properties;
using EddyLib.UI;
using Medallion.Shell;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Concurrent;
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
        #region Fields & Properties

        public int methodsteps = 54; // E+ prints ~52 lines + 2 extra steps
        public double IgnoreSmallFacesCutoff = 0.1;
        public double CumulativeViewFactorCutoff;

        // Backward compatibility alias
        public double CummulativeViewFactorCutoff
        {
            get => CumulativeViewFactorCutoff;
            set => CumulativeViewFactorCutoff = value;
        }

        public string ProjectName = "EddySim";
        public string BaseWorkingDir = "";
        public Weather Weather;

        public List<RProbe> Probes;
        public List<RPolygon> Polys;

        public double[] AmbientTemperature;
        public double[] SkyTemperature;

        public Mesh UnifiedMeshLowPolyNoSky;

        // Dictionaries for tracking unique materials/constructions
        private Dictionary<string, Material> _allMats = new Dictionary<string, Material>();
        private Dictionary<string, Construction> _allCons = new Dictionary<string, Construction>();
        private Dictionary<string, MaterialRoofVegetation> _allVeget = new Dictionary<string, MaterialRoofVegetation>();

        #endregion

        public ThermalSystem(string baseWorkingDir, Weather weather, List<RProbe> probes, List<RPolygon> polys, Mesh lowPoly, double vf_cutoff = 0.05, double smallf_cutoff = 0.1)
        {
            BaseWorkingDir = baseWorkingDir;
            Weather = weather;
            Probes = probes;
            Polys = polys;
            UnifiedMeshLowPolyNoSky = lowPoly;

            CumulativeViewFactorCutoff = vf_cutoff;
            IgnoreSmallFacesCutoff = smallf_cutoff;

            AmbientTemperature = Weather.DryBulbTemp;

            // Initialize ambient surfaces
            foreach (var poly in polys)
            {
                if (poly.SimulationType == SimulationType.Ambient)
                    poly.TemperatureOverride = RPolygon.toFloatArray(AmbientTemperature);
            }

            // Initialize sky temperature
            var skyModel = new SkyTemperatureModel(
                Weather.DewPointTemp,
                Weather.DryBulbTemp,
                Weather.TotalSkyCover,
                Weather.RelativeHumidity,
                true,
                SkyTemperatureModel.CalculationType.DefaultClarkAllen);

            SkyTemperature = skyModel.Temp;
        }

        public List<EsoResult> RunEP(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            if (!run) return null;

            // 0. Prepare EPJSON
            var epJsonObject = PrepareEpJson();

            // 1. Write EPJSON
            string epJsonFile = WriteEpJsonFile(epJsonObject, ref stepCnt, steps);

            // 2. Run EnergyPlus
            RunEnergyPlusProcess(epJsonFile, ref stepCnt, steps, ct);

            // 3. Read and Process Results
            return ProcessResults(ref stepCnt, steps);
        }

        #region Preparation Methods

        private EpJsonFormat PrepareEpJson()
        {
            var epJsonObject = new EpJsonFormat();
            _allMats.Clear();
            _allCons.Clear();
            _allVeget.Clear();

            int surfIndex = 0;
            int groundIndex = 0;

            foreach (var s in Polys)
            {
                if (ShouldSkipSurface(s)) continue;

                string prefix = GetSurfacePrefix(s.Type);
                string id = $"{prefix}_{s.ID}";

                var epSurf = CreateDetailedSurface(s);

                // Add construction based on surface type
                if (s.Type == RadiationSurfaceType.Vegetation)
                {
                    AddVegetationConstruction(s, epSurf);
                }
                else
                {
                    AddStandardConstruction(s, epSurf);
                }

                epJsonObject.AllThermalSurfaces.Add(id, epSurf);

                if (s.Type == RadiationSurfaceType.Building) surfIndex++;
                else groundIndex++;
            }

            AddShadingSurfaces(epJsonObject);

            return epJsonObject;
        }

        private bool ShouldSkipSurface(RPolygon s)
        {
            if (s.Type == RadiationSurfaceType.Sky) return true;
            if (s.SeenByProbes < CumulativeViewFactorCutoff) return true;
            if (s.SimulationType != SimulationType.Simulated) return true;
            return false;
        }

        private string GetSurfacePrefix(RadiationSurfaceType type)
        {
            return type switch
            {
                RadiationSurfaceType.Building => "S",
                RadiationSurfaceType.Ground => "G",
                RadiationSurfaceType.Vegetation => "V",
                _ => "X"
            };
        }

        private BuildingSurfaceDetailed CreateDetailedSurface(RPolygon s)
        {
            var epSurf = new BuildingSurfaceDetailed
            {
                Vertices = new List<DetailedVertex>(),
                ConstructionName = s.Type == RadiationSurfaceType.Vegetation ? "GreenRoofConstruction" : "DefaultConstruction"
            };

            var normal = s.Normal.Value;
            foreach (var v in s.Mesh.Value.Vertices)
            {
                epSurf.Vertices.Add(new DetailedVertex
                {
                    X = v.X + normal.X * 0.05,
                    Y = v.Y + normal.Y * 0.05,
                    Z = v.Z + normal.Z * 0.05
                });
            }
            epSurf.NumberOfVertices = s.Mesh.Value.Vertices.Count;
            return epSurf;
        }

        private void AddStandardConstruction(RPolygon s, BuildingSurfaceDetailed epSurf)
        {
            if (s.Parent?.Settings == null) return;

            string name = s.Parent.Settings.Name;
            epSurf.ConstructionName = name;

            if (!_allMats.ContainsKey(name) && !_allCons.ContainsKey(name))
            {
                _allMats.Add(name, s.Parent.Settings.GetMaterial());
                _allCons.Add(name, s.Parent.Settings.GetConstruction());
            }
        }

        private void AddVegetationConstruction(RPolygon s, BuildingSurfaceDetailed epSurf)
        {
            if (s.Parent?.VegSettings == null) return;

            string name = s.Parent.VegSettings.Name;
            epSurf.ConstructionName = name;

            if (!_allVeget.ContainsKey(name) && !_allCons.ContainsKey(name))
            {
                _allVeget.Add(name, s.Parent.VegSettings.GetMaterial());
                _allCons.Add(name, s.Parent.VegSettings.GetConstruction());
            }
        }

        private void AddShadingSurfaces(EpJsonFormat epJsonObject)
        {
            int shaderIndex = 0;
            Mesh unifiedMeshPlanar = UnifiedMeshLowPolyNoSky.DuplicateMesh();
            unifiedMeshPlanar.Faces.ConvertNonPlanarQuadsToTriangles(0.0, Rhino.RhinoMath.UnsetValue, 0);
            unifiedMeshPlanar.CollapseFacesByArea(IgnoreSmallFacesCutoff, 10000);

            foreach (var face in unifiedMeshPlanar.Faces)
            {
                var vertices = GetFaceVertices(unifiedMeshPlanar, face);
                double area = GeometryHelpers.CalculatePolygonArea(vertices);

                if (area < IgnoreSmallFacesCutoff + 0.1) continue;

                var epSurf = new ShadingBuildingDetailed
                {
                    Vertices = vertices.Select(v => new DetailedVertex { X = v.X, Y = v.Y, Z = v.Z }).ToList(),
                    NumberOfVertices = vertices.Count
                };

                epJsonObject.AllShaders.Add($"Shader{shaderIndex++}", epSurf);
            }
        }

        private List<Point3d> GetFaceVertices(Mesh mesh, MeshFace face)
        {
            var verts = new List<Point3d>
            {
                mesh.Vertices[face.A],
                mesh.Vertices[face.B],
                mesh.Vertices[face.C]
            };

            if (face.IsQuad)
                verts.Add(mesh.Vertices[face.D]);

            return verts;
        }

        #endregion

        #region IO Methods

        private string WriteEpJsonFile(EpJsonFormat epJsonObject, ref int stepCnt, int steps)
        {
            string epDir = Path.Combine(BaseWorkingDir, "Ep");
            Directory.CreateDirectory(epDir);
            string epJsonFile = Path.Combine(epDir, $"{ProjectName}.epjson");

            // Load template
            string jsonTemplate = Encoding.Default.GetString(Resources.Box);

            // Serialize main object
            string mainJson = JsonHelper.Serialize(epJsonObject).Trim().Trim('{', '}').Trim() + ",";

            string outputJson = jsonTemplate.Replace("\"@@SURFS@@\": null,", mainJson);

            // Inject dictionaries
            outputJson = InjectDictionary(outputJson, "@@MATERIALS@@", _allMats);
            outputJson = InjectDictionary(outputJson, "@@CONSTRUCTIONS@@", _allCons);
            outputJson = InjectDictionary(outputJson, "@@VEGETATION@@", _allVeget);

            Console.WriteLine("Writing EnergyPlus input files...");
            File.WriteAllText(epJsonFile, outputJson);

            ReportProgress(ref stepCnt, steps);
            return epJsonFile;
        }

        private string InjectDictionary<T>(string json, string placeholder, Dictionary<string, T> dict)
        {
            string content = JsonHelper.Serialize(dict).Trim().Trim('{', '}').Trim();
            if (!string.IsNullOrWhiteSpace(content)) content += ",";
            return json.Replace($"\"{placeholder}\": null,", content);
        }

        private void RunEnergyPlusProcess(string epJsonFile, ref int stepCnt, int steps, CancellationToken ct)
        {
            Console.WriteLine("Run EnergyPlus...");

            var epExe = Path.Combine(DefaultDirectoriesAndPaths.EnergyPlusDir, "energyplus.exe");
            var epDir = Path.GetDirectoryName(epJsonFile);

            var energyPlus = Command.Run(epExe,
                new[] { "-r", "-w", Path.GetFullPath(Weather.epwFilePath), "-p", ProjectName, epJsonFile },
                options => options.WorkingDirectory(epDir).CancellationToken(ct));

            string line;
            while ((line = energyPlus.StandardOutput.ReadLine()) != null)
            {
                Console.WriteLine(line);
                ReportProgress(ref stepCnt, steps); // E+ outputs about 52 lines
            }

            energyPlus.Wait();
            ReportProgress(ref stepCnt, steps);
        }

        private List<EsoResult> ProcessResults(ref int stepCnt, int steps)
        {
            string esoFile = Path.Combine(BaseWorkingDir, "Ep", $"{ProjectName}out.eso");

            if (!File.Exists(esoFile)) return null;

            Console.WriteLine("Read results...");
            var results = EsoReader.LoadEsoFile(esoFile);
            if (results == null) return null;

            // Map results back to polygons
            MapResultsToPolygons(results);

            // Apply special processing
            ApplyVegetationShadowModulation();

            ReportProgress(ref stepCnt, steps);
            return results;
        }

        private void MapResultsToPolygons(List<EsoResult> results)
        {
            // Create a lookup for faster access
            var resultLookup = results.ToLookup(r => r.zone);

            Parallel.ForEach(Polys, p =>
            {
                if (p.SimulationType != SimulationType.Simulated) return;

                string prefix = GetSurfacePrefix(p.Type);
                string key = $"{prefix}_{p.ID}";

                var match = resultLookup[key].FirstOrDefault();
                if (match != null)
                {
                    p.SurfaceTemperature = RPolygon.toFloatArray(match.values.ToArray());
                }
            });
        }

        #endregion

        #region Vegetation Shadow Modulation

        private void ApplyVegetationShadowModulation()
        {
            var vegPolys = Polys.Where(p => p.Type == RadiationSurfaceType.Vegetation &&
                                           p.SimulationType == SimulationType.Simulated &&
                                           p.SurfaceTemperature != null).ToList();

            if (vegPolys.Count == 0)
            {
                Console.WriteLine("No vegetation surfaces to modulate.");
                return;
            }

            Console.WriteLine($"Applying shadow modulation to {vegPolys.Count} vegetation surfaces...");

            const double MaxShadowTempDelta = 12.0;
            var sunPositions = PrecomputeSunPositions();
            var sg = new SolarGeometry();

            Parallel.ForEach(vegPolys, p =>
            {
                bool[] inDirSunlight = RayTraceSunlight(p, sunPositions);
                ApplyTemperatureReduction(p, inDirSunlight, sunPositions, MaxShadowTempDelta, sg);
            });
        }

        private Vector3d[] PrecomputeSunPositions()
        {
            var sg = new SolarGeometry();
            return sg.GetMonthlyRepresentativeSunVectors(Weather.SolarElevation, Weather.SolarAzi);
        }

        private bool[] RayTraceSunlight(RPolygon p, Vector3d[] sunPositions)
        {
            bool[] inDirSunlight = new bool[sunPositions.Length];
            Point3d centroid = p.Centroid.Value;
            Vector3d normal = p.Normal.Value;
            Point3d rayOrigin = centroid + normal * 0.5; // Offset to avoid self-intersection

            for (int i = 0; i < sunPositions.Length; i++)
            {
                if (sunPositions[i] == Vector3d.Zero)
                {
                    inDirSunlight[i] = false;
                    continue;
                }

                var dt = Rhino.Geometry.Intersect.Intersection.MeshRay(
                    UnifiedMeshLowPolyNoSky,
                    new Ray3d(rayOrigin, sunPositions[i]));

                inDirSunlight[i] = (dt < 0);
            }
            return inDirSunlight;
        }

        private void ApplyTemperatureReduction(RPolygon p, bool[] inDirSunlight, Vector3d[] sunPositions, double maxDelta, SolarGeometry sg)
        {
            for (int h = 0; h < 8760; h++)
            {
                sg.HourOfYear_To_MDH(h, out int month, out int day, out int hour);
                int sunIdx = (month * 24) + hour;
                double dnr = Weather.DirectNormalRadiation[h];

                if (dnr > 50 && sunPositions[sunIdx] != Vector3d.Zero)
                {
                    if (!inDirSunlight[sunIdx])
                    {
                        double radiationFactor = Math.Min(dnr / 1000.0, 1.0);
                        float tempReduction = (float)(maxDelta * radiationFactor);
                        p.SurfaceTemperature[h] -= tempReduction;
                    }
                }
            }
        }

        #endregion

        #region MRT Calculation

        public void ComputeMRT(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Computing MRT at probe level...");

            Parallel.For(0, Probes.Count, x =>
            {
                var p = Probes[x];
                p.LongWave_MRT = new float[8760];

                for (int i = 0; i < p.VFtoPolys.Length; i++)
                {
                    var poly = Polys[i];
                    float vf = (float)p.VFtoPolys[i];

                    if (vf <= 0) continue;

                    if (poly.SurfaceTemperature != null)
                    {
                        AddToMRT(p.LongWave_MRT, poly.SurfaceTemperature, vf);
                    }
                    else if (poly.Type == RadiationSurfaceType.Sky)
                    {
                        AddToMRT(p.LongWave_MRT, SkyTemperature, vf);
                    }
                    else if (poly.SimulationType == SimulationType.TemperatureInput && poly.TemperatureOverride != null)
                    {
                        AddToMRT(p.LongWave_MRT, poly.TemperatureOverride, vf);
                    }
                    else
                    {
                        AddToMRT(p.LongWave_MRT, AmbientTemperature, vf);
                    }
                }
            });

            Console.WriteLine("MRT calculation complete...");
        }

        private void AddToMRT(float[] mrt, float[] temps, float vf)
        {
            for (int h = 0; h < mrt.Length; h++)
                mrt[h] += temps[h] * vf;
        }

        private void AddToMRT(float[] mrt, double[] temps, float vf)
        {
            for (int h = 0; h < mrt.Length; h++)
                mrt[h] += (float)(temps[h] * vf);
        }

        #endregion

        #region Results Saving

        public MRT_Simulation_ResultProto SaveResults(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Console.WriteLine("Writing Results...");
            var sp = Stopwatch.StartNew();

            var protoResult = new MRT_Simulation_ResultProto(BaseWorkingDir, Weather, Probes, Polys);
            protoResult.WriteToFile(Path.Combine(BaseWorkingDir, "MRT.eddy"));

            sp.Stop();
            Debug.WriteLine($"Results Proto: {sp.ElapsedMilliseconds} ms");

            Console.WriteLine("Results written");
            ReportProgress(ref stepCnt, steps);

            return protoResult;
        }

        private void ReportProgress(ref int stepCnt, int steps)
        {
            Interlocked.Increment(ref stepCnt);
            int percent = steps > 0 ? 100 * stepCnt / steps : 0;
            Console.WriteLine(ProgressWriter.ProgressKey + percent.ToString(CultureInfo.InvariantCulture));
        }

        #endregion
    }
}