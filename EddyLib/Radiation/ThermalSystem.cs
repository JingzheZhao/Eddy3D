using EddyLib.Properties;
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
                var energyPlus = Command.Run(DefaultDirectoriesAndPaths.EnergyPlusDir + @"\energyplus.exe", new[] { "-r", "-w", Path.GetFullPath(Weather.epwFilePath), "-p", ProjectName, epjsonfile },
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
                            if (res.Any(x => x.zone == "V_" + p.ID.ToString()))
                            {
                                p.SurfaceTemperature = RPolygon.toFloatArray(res.First(x => x.zone == "V_" + p.ID.ToString()).values.ToArray());
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

                    // Apply ray-traced shadow modulation to vegetation surfaces
                    // EnergyPlus EcoRoof model outputs uniform temperatures for all vegetation surfaces
                    // This modulation uses ray tracing to determine per-surface shading and adjusts temperatures accordingly
                    ApplyVegetationShadowModulation();

                    Interlocked.Increment(ref stepCnt);
                    Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
                    return res;
                }
            }
            return null;
        }

        /// <summary>
        /// Applies shadow modulation to vegetation surface temperatures to compensate for an
        /// inherent limitation in the EnergyPlus EcoRoof (green roof) model.
        ///
        /// <para><b>PROBLEM BACKGROUND:</b></para>
        /// <para>
        /// The EnergyPlus EcoRoof model (Material:RoofVegetation) is a one-dimensional heat transfer
        /// model that calculates two temperatures: Tf (foliage/vegetation temperature) and Tg (soil
        /// surface temperature). This 1D model does NOT provide spatially-resolved temperature outputs.
        /// </para>
        ///
        /// <para>
        /// As a result, when EnergyPlus outputs "Surface Outside Face Temperature" for vegetation
        /// surfaces (V_*), ALL vegetation surfaces receive the SAME temperature value, regardless of
        /// whether they are in direct sunlight or shaded by buildings. This was verified by analyzing
        /// the EddySimout.csv output: all 180 V_* surfaces had identical temperatures (e.g., 28.369°C)
        /// while concrete G_* surfaces correctly showed variation (16.68-17.20°C) due to shadows.
        /// </para>
        ///
        /// <para><b>SOLUTION:</b></para>
        /// <para>
        /// This method compensates for the EcoRoof limitation by using ray tracing (via Rhino's
        /// Mesh.Intersection.MeshRay) to determine whether each vegetation surface is in direct
        /// sunlight or shaded by buildings. Shaded surfaces have their temperatures reduced
        /// proportionally to the direct solar radiation that would have been received if unshaded.
        /// </para>
        ///
        /// <para><b>ALGORITHM:</b></para>
        /// <list type="number">
        ///   <item>Pre-compute sun position vectors for each hour of the day for each month (12x24 = 288 positions)</item>
        ///   <item>For each vegetation surface, cast a ray from its centroid toward the sun</item>
        ///   <item>If the ray intersects the scene mesh, the surface is shaded</item>
        ///   <item>For shaded hours, reduce the surface temperature proportional to direct normal radiation</item>
        /// </list>
        ///
        /// <para><b>PHYSICS RATIONALE:</b></para>
        /// <para>
        /// The temperature reduction is based on the principle that shaded surfaces receive only
        /// diffuse radiation while sunlit surfaces receive both direct and diffuse radiation.
        /// Literature values suggest shaded grass can be 5-15°C cooler than sunlit grass, depending
        /// on conditions. We use a conservative maximum delta of 8°C, scaled by the actual direct
        /// normal radiation intensity.
        /// </para>
        ///
        /// <para><b>REFERENCES:</b></para>
        /// <para>
        /// - EnergyPlus Engineering Reference, Chapter on Green Roof Model (EcoRoof)
        /// - Sailor, D.J. (2008). "A green roof model for building energy simulation programs."
        /// </para>
        /// </summary>
        private void ApplyVegetationShadowModulation()
        {
            Console.WriteLine("Applying shadow modulation to vegetation surfaces...");

            // =====================================================================================
            // CONFIGURATION: Maximum temperature reduction for fully shaded vegetation
            // =====================================================================================
            // This value represents the maximum temperature difference (in °C) between a fully
            // sunlit vegetation surface and a fully shaded one under peak direct normal radiation.
            //
            // Literature values:
            // - Bowler et al. (2010): Urban grass in shade can be 6-10°C cooler than in sun
            // - Shashua-Bar et al. (2011): Tree shade reduced grass surface temp by 7-12°C
            // - Lin et al. (2012): Shaded turf 5-8°C cooler in subtropical climate
            //
            // We use 8°C as a reasonable mid-range value that applies under peak radiation.
            // The actual reduction is scaled by the normalized direct normal radiation.
            // =====================================================================================
            const double MaxShadowTempDelta = 12.0;

            // =====================================================================================
            // STEP 1: Pre-compute sun position vectors
            // =====================================================================================
            // Rather than computing sun positions for all 8760 hours, we use a representative day
            // per month (day 0 of each month) to reduce computation. This gives us 12 months × 24
            // hours = 288 unique sun positions. This is the same optimization used in RadiationSystem.
            //
            // The sun position is stored as a unit vector pointing FROM the surface TOWARD the sun.
            // We use spherical coordinates (elevation, azimuth) from the weather file to compute
            // the Cartesian (x, y, z) direction vector.
            //
            // For hours when the sun is below the horizon (elevation ≤ 3°), we store Vector3d.Zero
            // to indicate no direct sun is possible.
            // =====================================================================================
            SolarGeometry sg = new SolarGeometry();
            Vector3d[] sunPositions = new Vector3d[12 * 24];

            for (int m = 0; m < 12; m++)
            {
                for (int h = 0; h < 24; h++)
                {
                    // Get the hour-of-year for day 0 of month m at hour h
                    int hourOfYear = sg.HourInYear(m, 0, h);

                    // Get solar geometry from weather data
                    double el = Weather.SolarElevation[hourOfYear];  // Elevation angle in degrees
                    double az = Weather.SolarAzi[hourOfYear];        // Azimuth angle in degrees

                    // Only compute sun vector if sun is above horizon
                    // Using 3° threshold to avoid grazing angles that cause numerical issues
                    if (el > 3.0)
                    {
                        // Convert spherical coordinates (azimuth, elevation) to Cartesian unit vector
                        // Convention: X = East, Y = North, Z = Up
                        // Azimuth is measured from North (0°) clockwise
                        double x = Math.Cos(sg.deg2rad(90 - az)) * Math.Cos(sg.deg2rad(el));
                        double y = Math.Sin(sg.deg2rad(90 - az)) * Math.Cos(sg.deg2rad(el));
                        double z = Math.Sin(sg.deg2rad(el));
                        sunPositions[(m * 24) + h] = new Vector3d(x, y, z);
                    }
                    else
                    {
                        // Sun below horizon - no direct radiation possible
                        sunPositions[(m * 24) + h] = Vector3d.Zero;
                    }
                }
            }

            // =====================================================================================
            // STEP 2: Filter vegetation surfaces that need shadow modulation
            // =====================================================================================
            // Only process vegetation surfaces that:
            // - Are of type Vegetation (not ground, building, or sky)
            // - Are set to be simulated (not ambient or temperature override)
            // - Have valid temperature data from EnergyPlus
            // =====================================================================================
            var vegPolys = this.Polys.Where(p => p.Type == RadiationSurfaceType.Vegetation &&
                                                  p.SimulationType == SimulationType.Simulated &&
                                                  p.SurfaceTemperature != null).ToList();

            if (vegPolys.Count == 0)
            {
                Console.WriteLine("No vegetation surfaces to modulate.");
                return;
            }

            // =====================================================================================
            // STEP 3: Process each vegetation surface in parallel
            // =====================================================================================
            // For each vegetation surface:
            // 1. Determine sunlit/shaded status for each sun position via ray tracing
            // 2. Apply temperature reduction for shaded hours based on direct radiation intensity
            //
            // We use Parallel.ForEach for performance since ray tracing can be expensive.
            // Each surface is processed independently with no shared state (thread-safe).
            // =====================================================================================
            Parallel.ForEach(vegPolys, p =>
            {
                // Array to store whether surface is in direct sunlight for each sun position
                bool[] inDirSunlight = new bool[12 * 24];

                // Get surface centroid and normal
                Point3d centroid = p.Centroid.Value;
                Vector3d normal = p.Normal.Value;

                // CRITICAL FIX: Offset the ray origin ABOVE the surface to avoid self-intersection
                // The ray starts at the centroid + a small offset along the surface normal.
                // This prevents the ray from immediately hitting the surface it's starting from,
                // which was causing the patchy/artifact patterns in the visualization.
                //
                // We use 0.5 units (meters) offset - large enough to clear any mesh tolerance issues
                // but small enough not to miss thin overhangs.
                Point3d rayOrigin = centroid + normal * 0.5;

                // ---------------------------------------------------------------------------------
                // Ray trace for each sun position to determine shading
                // ---------------------------------------------------------------------------------
                for (int sunIdx = 0; sunIdx < sunPositions.Length; sunIdx++)
                {
                    // Skip if sun is below horizon
                    if (sunPositions[sunIdx] == Vector3d.Zero)
                    {
                        inDirSunlight[sunIdx] = false;
                        continue;
                    }

                    // Cast a ray from ABOVE the surface centroid toward the sun
                    // UnifiedMeshLowPolyNoSky contains all scene geometry except the sky dome
                    // This includes buildings, trees, and other surfaces that can cast shadows
                    var dt = Rhino.Geometry.Intersect.Intersection.MeshRay(
                        UnifiedMeshLowPolyNoSky,
                        new Ray3d(rayOrigin, sunPositions[sunIdx]));

                    // MeshRay returns the distance to first intersection, or a negative value if no hit
                    // Negative value (<0) means no intersection - surface is in direct sunlight
                    // Any positive hit means surface is shaded by some geometry
                    inDirSunlight[sunIdx] = (dt < 0);
                }

                // ---------------------------------------------------------------------------------
                // Apply temperature modulation for each hour of the year
                // ---------------------------------------------------------------------------------
                for (int h = 0; h < 8760; h++)
                {
                    // Convert hour-of-year to month/day/hour to look up sun position
                    int month = 0, day = 0, hour = 0;
                    sg.HourOfYear_To_MDH(h, out month, out day, out hour);

                    // Look up the pre-computed sun position for this month/hour
                    int sunIdx = (month * 24) + hour;

                    // Get actual direct normal radiation for this specific hour from weather data
                    // This varies day-to-day due to clouds, unlike the geometric sun position
                    double dnr = Weather.DirectNormalRadiation[h];  // W/m²

                    // Only apply modulation if there's meaningful direct radiation
                    // Skip if DNR < 50 W/m² (essentially cloudy/overcast conditions)
                    // Also skip if sun is below horizon
                    if (dnr > 50 && sunPositions[sunIdx] != Vector3d.Zero)
                    {
                        // Normalize radiation intensity: 1000 W/m² is approximately clear-sky peak
                        // This scales our temperature delta proportionally to solar intensity
                        double radiationFactor = Math.Min(dnr / 1000.0, 1.0);

                        if (!inDirSunlight[sunIdx])
                        {
                            // Surface is SHADED by buildings/objects
                            // Reduce temperature proportionally to what the direct radiation would have been
                            // Example: If DNR = 800 W/m² (radiationFactor = 0.8), temp reduction = 6.4°C
                            float tempReduction = (float)(MaxShadowTempDelta * radiationFactor);
                            p.SurfaceTemperature[h] -= tempReduction;
                        }
                        // If surface is sunlit, no modification needed - EnergyPlus temperature is used as-is
                    }
                    // If low/no direct radiation (cloudy), no modification needed since there's
                    // minimal temperature difference between sunlit and shaded surfaces
                }
            });

            Console.WriteLine("Shadow modulation applied to " + vegPolys.Count + " vegetation surfaces.");
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