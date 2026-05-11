using EddyLib;
using EddyLib.Geometry;
using EddyLib.Radiation;
using Rhino.Geometry;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    // Regression coverage for the view-factor hot path (planned PR #477/#487 hybrid refactor).
    // These tests pin the current behavior of FFactor, FFactorProbe, BuildVFToProbes,
    // BuildVFToProbesByMaterial, and FindPolysSeenByProbes against a deterministic
    // mini-scene so the upcoming kernel simplification can be proven equivalent.
    //
    // Uses [Fact]/[Theory] (not [RhinoRequiredFact]) — these touch RhinoCommon types
    // (Point3d, Vector3d, Mesh) but not the Rhino host process.
    public class ViewFactorTests
    {
        // Helper: build a polygon at a given centroid + outward normal + area + type.
        // Unitize manually (Vector3d.Unitize is P/Invoke; we want this helper to be
        // safe to call from any test runner, not just Windows + Rhino native).
        private static RPolygon MakePoly(Point3d centroid, Vector3d normal, double area,
                                        RadiationSurfaceType type = RadiationSurfaceType.Building,
                                        string name = "P")
        {
            double len = System.Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            var unit = new Vector3d(normal.X / len, normal.Y / len, normal.Z / len);
            return new RPolygon
            {
                Centroid = new EddyPoint(centroid),
                Normal = new EddyVector(unit),
                Area = area,
                Type = type,
                Name = name
            };
        }

        // Build a fresh MRT_Simulation_System with the given polys/probes for kernel tests.
        private static MRT_Simulation_System MakeSystem(List<RPolygon> polys, List<RProbe> probes)
        {
            var sys = new MRT_Simulation_System();
            sys.Polys = polys;
            sys.Probes = probes;
            return sys;
        }

        // -------- FFactor (polygon-to-polygon) --------

        // Two unit-area parallel polygons facing each other across r=1.
        // Old formula: f = (cosThetaI * cosThetaJ) / (PI * r^2)
        //            = (1 * 1) / (PI * 1) = 1/PI = 0.3183098861837907
        [Fact]
        public void FFactor_TwoParallelFacingPolys_ReturnsOneOverPi()
        {
            var p0 = MakePoly(new Point3d(0, 0, 0), Vector3d.ZAxis, 1.0);
            var p1 = MakePoly(new Point3d(0, 0, 1), -Vector3d.ZAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p0, p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactor(p0, p1, emptyObst);
            Assert.Equal(1.0 / System.Math.PI, f, 12);
        }

        // Same polygons but normals reversed — they're back-to-back, so normals
        // facing the same way means `n0 . n1 > 0.0001`: early-exit returns 0.
        [Fact]
        public void FFactor_BackToBackPolys_ReturnsZero()
        {
            var p0 = MakePoly(new Point3d(0, 0, 0), Vector3d.ZAxis, 1.0);
            var p1 = MakePoly(new Point3d(0, 0, 1), Vector3d.ZAxis, 1.0); // same normal direction
            var sys = MakeSystem(new List<RPolygon> { p0, p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactor(p0, p1, emptyObst);
            Assert.Equal(0.0, f);
        }

        // p1 behind p0 (i.e. on the -normal side of p0): old `Plane.DistanceTo(p1.Centroid) < 0`
        // path returns 0. (p0 faces +z but p1 is at z = -1.)
        [Fact]
        public void FFactor_PolyBehindOther_ReturnsZero()
        {
            var p0 = MakePoly(new Point3d(0, 0, 0), Vector3d.ZAxis, 1.0);
            var p1 = MakePoly(new Point3d(0, 0, -1), -Vector3d.ZAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p0, p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactor(p0, p1, emptyObst);
            Assert.Equal(0.0, f);
        }

        // r < 0.1 → return 0 (polys too close).
        [Fact]
        public void FFactor_TooClose_ReturnsZero()
        {
            var p0 = MakePoly(new Point3d(0, 0, 0), Vector3d.ZAxis, 1.0);
            var p1 = MakePoly(new Point3d(0, 0, 0.05), -Vector3d.ZAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p0, p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactor(p0, p1, emptyObst);
            Assert.Equal(0.0, f);
        }

        // Two polygons at oblique angles. cosThetaI = cos(60°) = 0.5, etc.
        // p0 at origin, normal = +z. p1 at (1, 0, 1), normal = (-1, 0, 0) (faces -x).
        // dv = (1, 0, 1), r = sqrt(2), r^2 = 2
        // cosThetaI = (dv . p0Normal) / r = 1 / sqrt(2) = 0.707106781187...
        // cosThetaJ = -(dv . p1Normal) / r = -((-1)) / sqrt(2) = 0.707106781187...
        // f = (0.5 / sqrt(2)*sqrt(2)) / (PI * 2) = 0.5 / (2*PI) = 1/(4*PI) = 0.07957747154594767
        [Fact]
        public void FFactor_ObliquePolys_MatchesAnalyticValue()
        {
            var p0 = MakePoly(new Point3d(0, 0, 0), Vector3d.ZAxis, 1.0);
            var p1 = MakePoly(new Point3d(1, 0, 1), -Vector3d.XAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p0, p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactor(p0, p1, emptyObst);
            Assert.Equal(1.0 / (4.0 * System.Math.PI), f, 12);
        }

        // -------- FFactorProbe (probe-to-polygon) --------

        // Probe at origin, polygon at z=1 facing -z, Area=1.
        // Old: f = (cosThetaI * cosThetaJ) / (4*PI*r^2) * Area = (1*1)/(4*PI*1) * 1 = 1/(4*PI)
        // = 0.07957747154594767
        [Fact]
        public void FFactorProbe_DirectlyInFront_MatchesAnalyticValue()
        {
            var p1 = MakePoly(new Point3d(0, 0, 1), -Vector3d.ZAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactorProbe(new Point3d(0, 0, 0), p1, emptyObst);
            Assert.Equal(1.0 / (4.0 * System.Math.PI), f, 12);
        }

        // Probe behind the polygon: polygon at z=1 facing +z, probe at origin.
        // probe_n = (0,0,1) (toward poly). p1.Normal . probe_n = (0,0,1).(0,0,1) = 1 > 0.0001.
        // Old code returns 0 (normals don't face each other).
        [Fact]
        public void FFactorProbe_ProbeBehindPoly_ReturnsZero()
        {
            var p1 = MakePoly(new Point3d(0, 0, 1), Vector3d.ZAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactorProbe(new Point3d(0, 0, 0), p1, emptyObst);
            Assert.Equal(0.0, f);
        }

        // Probe very close to poly centroid (r < 0.1): old returns 0.
        [Fact]
        public void FFactorProbe_TooClose_ReturnsZero()
        {
            var p1 = MakePoly(new Point3d(0, 0, 0.05), -Vector3d.ZAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactorProbe(new Point3d(0, 0, 0), p1, emptyObst);
            Assert.Equal(0.0, f);
        }

        // Probe at distance r=10 from large poly. f scales as 1/r^2.
        // Area=1, r^2=100, cosThetaI=cosThetaJ=1: f = 1/(4*PI*100) = 0.0007957747...
        // But: f < 0.00001 check? 0.000796 > 0.00001 so OK.
        [Fact]
        public void FFactorProbe_FarAway_MatchesInverseSquare()
        {
            var p1 = MakePoly(new Point3d(0, 0, 10), -Vector3d.ZAxis, 1.0);
            var sys = MakeSystem(new List<RPolygon> { p1 }, new List<RProbe>());
            var emptyObst = new Mesh();

            double f = sys.FFactorProbe(new Point3d(0, 0, 0), p1, emptyObst);
            Assert.Equal(1.0 / (4.0 * System.Math.PI * 100.0), f, 12);
        }

        // -------- BuildVFToProbes (integration) --------

        // Simple scene: a single probe at origin facing +z, with three polygons of
        // unit area arranged so they all see the probe. After normalize, sum = 1.0.
        // Goldens captured from current code -- pin them so the refactor must match.
        [Fact]
        public void BuildVFToProbes_SimpleScene_PinsGoldenValues()
        {
            var polys = new List<RPolygon>
            {
                MakePoly(new Point3d(0, 0, 1),  -Vector3d.ZAxis, 1.0, RadiationSurfaceType.Building, "front"),
                MakePoly(new Point3d(1, 0, 1),  -Vector3d.XAxis, 1.0, RadiationSurfaceType.Ground,   "right"),
                MakePoly(new Point3d(0, 1, 1),  -Vector3d.YAxis, 1.0, RadiationSurfaceType.Vegetation, "back"),
            };
            var probes = new List<RProbe> { new RProbe(new Point3d(0, 0, 0), Vector3d.ZAxis) };
            var sys = MakeSystem(polys, probes);
            var emptyObst = new Mesh();

            sys.BuildVFToProbes(emptyObst);

            // Normalized -- sum must be 1.0 (within FP precision)
            double sum = 0;
            for (int j = 0; j < polys.Count; j++) sum += probes[0].VFtoPolys[j];
            Assert.Equal(1.0, sum, 12);

            // Pinned ratios from analytical derivation:
            //   poly[0] at (0,0,1) facing -z, probe at origin:
            //     dv=(0,0,1), r=1, cosThetaI=1, cosThetaJ=1 ⇒ f0 = 1/(4*PI)
            //   poly[1] at (1,0,1) facing -x:
            //     dv=(1,0,1), r=sqrt(2), probe_n=(1/√2,0,1/√2) (unitized)
            //     cosThetaI = (dv·probe_n)/(|dv|·|probe_n|) = 2/(sqrt(2)*sqrt(2)) = 1
            //     cosThetaJ = -(dv·p1Normal)/(|dv|·|p1Normal|) = -(−1)/sqrt(2) = 1/sqrt(2)
            //     f1 = (1 * 1/sqrt(2)) / (4*PI*2) = 1 / (8*sqrt(2)*PI)
            //   poly[2] symmetric to poly[1] ⇒ f2 = f1
            //   sum = 1/(4*PI) + 2/(8*sqrt(2)*PI) = (1 + 1/sqrt(2)) / (4*PI)
            //   normalized:
            //     vf[0] = 1 / (1 + 1/sqrt(2)) = sqrt(2) / (sqrt(2) + 1) = 2 - sqrt(2)
            //     vf[1] = vf[2] = (sqrt(2) - 1) / 2
            double sqrt2 = System.Math.Sqrt(2.0);
            double vf0 = 2.0 - sqrt2;
            double vf12 = (sqrt2 - 1.0) / 2.0;
            Assert.Equal(vf0,  probes[0].VFtoPolys[0], 12);
            Assert.Equal(vf12, probes[0].VFtoPolys[1], 12);
            Assert.Equal(vf12, probes[0].VFtoPolys[2], 12);

            // BuildVFToProbes also calls FindPolysSeenByProbes -- check SeenByProbes too
            // With one probe: each poly's SeenByProbes = (0 + VFtoPolys[probe]) / 1 = vf.
            Assert.Equal(vf0,  polys[0].SeenByProbes, 12);
            Assert.Equal(vf12, polys[1].SeenByProbes, 12);
            Assert.Equal(vf12, polys[2].SeenByProbes, 12);
        }

        // -------- BuildVFToProbesByMaterial (integration) --------

        // Same scene as above. Three polys of three different material types.
        // After BuildVFToProbesByMaterial, VFtoMaterial sums to 1.0 per probe.
        [Fact]
        public void BuildVFToProbesByMaterial_DistinctTypes_PinsGoldenValues()
        {
            var polys = new List<RPolygon>
            {
                MakePoly(new Point3d(0, 0, 1),  -Vector3d.ZAxis, 1.0, RadiationSurfaceType.Building, "front"),
                MakePoly(new Point3d(1, 0, 1),  -Vector3d.XAxis, 1.0, RadiationSurfaceType.Ground,   "right"),
                MakePoly(new Point3d(0, 1, 1),  -Vector3d.YAxis, 1.0, RadiationSurfaceType.Vegetation, "back"),
            };
            var probes = new List<RProbe> { new RProbe(new Point3d(0, 0, 0), Vector3d.ZAxis) };
            var sys = MakeSystem(polys, probes);
            var emptyObst = new Mesh();

            sys.BuildVFToProbes(emptyObst);          // populates VFtoPolys
            sys.BuildVFToProbesByMaterial();         // aggregates into VFtoMaterial

            // Each type has exactly one poly so material VF == polygon VF.
            // Reuse the analytical values from the BuildVFToProbes test above.
            double sum = 0;
            foreach (var kv in probes[0].VFtoMaterial) sum += kv.Value;
            Assert.Equal(1.0, sum, 12);

            double sqrt2 = System.Math.Sqrt(2.0);
            double vf0 = 2.0 - sqrt2;
            double vf12 = (sqrt2 - 1.0) / 2.0;
            Assert.Equal(vf0,  probes[0].VFtoMaterial["Building"],   12);
            Assert.Equal(vf12, probes[0].VFtoMaterial["Ground"],     12);
            Assert.Equal(vf12, probes[0].VFtoMaterial["Vegetation"], 12);
        }

        // Aggregation: two polys of the same type should sum together.
        [Fact]
        public void BuildVFToProbesByMaterial_SameTypeAggregates()
        {
            // Two ground polys + one building poly, all visible from origin
            var polys = new List<RPolygon>
            {
                MakePoly(new Point3d(0, 0, 1),  -Vector3d.ZAxis, 1.0, RadiationSurfaceType.Ground,   "g1"),
                MakePoly(new Point3d(1, 0, 1),  -Vector3d.XAxis, 1.0, RadiationSurfaceType.Ground,   "g2"),
                MakePoly(new Point3d(0, 1, 1),  -Vector3d.YAxis, 1.0, RadiationSurfaceType.Building, "b1"),
            };
            var probes = new List<RProbe> { new RProbe(new Point3d(0, 0, 0), Vector3d.ZAxis) };
            var sys = MakeSystem(polys, probes);
            var emptyObst = new Mesh();

            sys.BuildVFToProbes(emptyObst);
            sys.BuildVFToProbesByMaterial();

            // Ground = poly[0] + poly[1] = (2 - sqrt(2)) + (sqrt(2) - 1)/2 = (3 - sqrt(2))/2
            // Building = poly[2] = (sqrt(2) - 1)/2
            double sqrt2 = System.Math.Sqrt(2.0);
            Assert.Equal((3.0 - sqrt2) / 2.0, probes[0].VFtoMaterial["Ground"],   12);
            Assert.Equal((sqrt2 - 1.0) / 2.0, probes[0].VFtoMaterial["Building"], 12);
        }

        // -------- FindPolysSeenByProbes (direct) --------

        // Pin the accumulate-then-divide-or-leave semantics. The old code does
        //   for each j: SeenByProbes += sum over probes of VFtoPolys[j];
        //   for each j: if (SeenByProbes != 0) SeenByProbes /= Probes.Count;
        // If SeenByProbes has a non-zero pre-existing value, the accumulator
        // adds to it. The refactor must preserve this.
        [Fact]
        public void FindPolysSeenByProbes_AccumulatesAndAverages()
        {
            var polys = new List<RPolygon>
            {
                MakePoly(new Point3d(0, 0, 1), -Vector3d.ZAxis, 1.0),
                MakePoly(new Point3d(0, 0, 2), -Vector3d.ZAxis, 1.0),
                MakePoly(new Point3d(0, 0, 3), -Vector3d.ZAxis, 1.0),
            };
            var probes = new List<RProbe>
            {
                new RProbe(new Point3d(0, 0, 0), Vector3d.ZAxis),
                new RProbe(new Point3d(1, 0, 0), Vector3d.ZAxis),
            };
            probes[0].VFtoPolys = new double[] { 0.1, 0.2, 0.7 };
            probes[1].VFtoPolys = new double[] { 0.4, 0.3, 0.3 };
            var sys = MakeSystem(polys, probes);

            sys.FindPolysSeenByProbes();

            // Sum across probes / Probes.Count:
            //   poly[0]: (0.1 + 0.4) / 2 = 0.25
            //   poly[1]: (0.2 + 0.3) / 2 = 0.25
            //   poly[2]: (0.7 + 0.3) / 2 = 0.50
            Assert.Equal(0.25, polys[0].SeenByProbes, 12);
            Assert.Equal(0.25, polys[1].SeenByProbes, 12);
            Assert.Equal(0.50, polys[2].SeenByProbes, 12);
        }

        // Zero accumulator: old code skips the divide and leaves SeenByProbes alone.
        // The refactor must preserve this so any pre-set value survives.
        [Fact]
        public void FindPolysSeenByProbes_ZeroAccumulator_LeavesPreExistingValue()
        {
            var polys = new List<RPolygon>
            {
                MakePoly(new Point3d(0, 0, 1), -Vector3d.ZAxis, 1.0),
            };
            polys[0].SeenByProbes = 42.0; // pre-existing value
            var probes = new List<RProbe>
            {
                new RProbe(new Point3d(0, 0, 0), Vector3d.ZAxis),
                new RProbe(new Point3d(1, 0, 0), Vector3d.ZAxis),
            };
            probes[0].VFtoPolys = new double[] { 0.0 };
            probes[1].VFtoPolys = new double[] { 0.0 };
            var sys = MakeSystem(polys, probes);

            sys.FindPolysSeenByProbes();

            // 42 + 0 + 0 = 42 (non-zero) → divide by Probes.Count = 2 → 21
            // NOTE: the != 0 guard refers to the accumulated value, which IS 42 here
            // (because += adds to the existing 42). So this divides.
            Assert.Equal(21.0, polys[0].SeenByProbes, 12);
        }
    }
}
