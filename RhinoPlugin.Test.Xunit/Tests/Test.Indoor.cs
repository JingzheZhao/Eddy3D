using EddyLib.Indoor;
using Rhino.Geometry;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using EddyLib.Indoor.FunctionObjects;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class IndoorTests
    {
        private readonly ITestOutputHelper _output;

        public IndoorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateIndoorDomain_SimpleRoom_ReturnsValidDomain()
        {
            // 1. Create Geometry
            // Room: 10m x 10m x 3m
            // Inlet: X=0 face
            // Outlet: X=10 face
            // Walls: Others

            var p0 = new Point3d(0, 0, 0);
            var p1 = new Point3d(10, 0, 0);
            var p2 = new Point3d(10, 10, 0);
            var p3 = new Point3d(0, 10, 0);
            
            var p4 = new Point3d(0, 0, 3);
            var p5 = new Point3d(10, 0, 3);
            var p6 = new Point3d(10, 10, 3);
            var p7 = new Point3d(0, 10, 3);

            var mp = new MeshingParameters();

            // Inlet (X=0 face: p0, p3, p7, p4)
            var inletSrf = NurbsSurface.CreateFromCorners(p0, p3, p7, p4);
            var inletMesh = Mesh.CreateFromBrep(inletSrf.ToBrep(), mp)[0];

            // Outlet (X=10 face: p1, p2, p6, p5)
            var outletSrf = NurbsSurface.CreateFromCorners(p1, p2, p6, p5);
            var outletMesh = Mesh.CreateFromBrep(outletSrf.ToBrep(), mp)[0];

            // Walls
            var wallsMesh = new Mesh();
            
            // Floor (Z=0: p0, p1, p2, p3)
            var floorSrf = NurbsSurface.CreateFromCorners(p0, p1, p2, p3);
            wallsMesh.Append(Mesh.CreateFromBrep(floorSrf.ToBrep(), mp)[0]);

            // Ceiling (Z=3: p4, p5, p6, p7)
            var ceilSrf = NurbsSurface.CreateFromCorners(p4, p5, p6, p7);
            wallsMesh.Append(Mesh.CreateFromBrep(ceilSrf.ToBrep(), mp)[0]);

            // Y=0 (p0, p1, p5, p4)
            var wall1Srf = NurbsSurface.CreateFromCorners(p0, p1, p5, p4);
            wallsMesh.Append(Mesh.CreateFromBrep(wall1Srf.ToBrep(), mp)[0]);

            // Y=10 (p3, p2, p6, p7)
            var wall2Srf = NurbsSurface.CreateFromCorners(p3, p2, p6, p7);
            wallsMesh.Append(Mesh.CreateFromBrep(wall2Srf.ToBrep(), mp)[0]);

            // 2. Create Boundary Conditions
            var walls = new List<IndoorBC.Wall>();
            walls.Add(new IndoorBC.Wall(wallsMesh, 3, 20.0));

            var inlets = new List<IndoorBC.Inlet>();
            inlets.Add(new IndoorBC.Inlet(inletMesh, 20.0, 3, new Vector3d(1, 0, 0))); // Velocity (1,0,0)

            var outlets = new List<IndoorBC.Outlet>();
            outlets.Add(new IndoorBC.Outlet(outletMesh, 3));

            var fos = new List<FunctionObject>(); // Empty function objects for now

            // 3. Setup Domain Parameters
            int endTime = 100;
            string workingDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IndoorTest");
            if (!System.IO.Directory.Exists(workingDir)) System.IO.Directory.CreateDirectory(workingDir);
            
            double cellSize = 0.5;
            Point3d pointInside = new Point3d(5, 5, 1.5);
            int cpus = 2;

            // 4. Instantiate IndoorDomain
            var domain = new IndoorDomain(endTime, workingDir, cellSize, pointInside, walls, inlets, outlets, fos, cpus);

            // 5. Assertions
            Assert.NotNull(domain);
            Assert.NotNull(domain.BoundingBox);
            Assert.Equal(100, domain.endTime);
            Assert.Equal(workingDir, domain.WorkingDir);
            
            // Check if geometry was added
            // Using reflection or checking public properties if available.
            // IndoorDomain has public properties? Not for the lists of geometry directly, but they are used in constructor.
            // But we can check BoundingBox size roughly.
            Assert.True(domain.BoundingBox.Max.X >= 10);
            Assert.True(domain.BoundingBox.Max.Y >= 10);
            Assert.True(domain.BoundingBox.Max.Z >= 3);

            _output.WriteLine("Indoor Domain created successfully.");
        }

        [Fact]
        public void CreateIndoorDomain_WithCO2Emitter_ReturnsValidDomain()
        {
            // 1. Create Geometry (Same as simple room)
            var p0 = new Point3d(0, 0, 0);
            var p1 = new Point3d(10, 0, 0);
            var p2 = new Point3d(10, 10, 0);
            var p3 = new Point3d(0, 10, 0);
            
            var p4 = new Point3d(0, 0, 3);
            var p5 = new Point3d(10, 0, 3);
            var p6 = new Point3d(10, 10, 3);
            var p7 = new Point3d(0, 10, 3);

            var mp = new MeshingParameters();

            // Inlet (X=0 face)
            var inletSrf = NurbsSurface.CreateFromCorners(p0, p3, p7, p4);
            var inletMesh = Mesh.CreateFromBrep(inletSrf.ToBrep(), mp)[0];

            // Outlet (X=10 face)
            var outletSrf = NurbsSurface.CreateFromCorners(p1, p2, p6, p5);
            var outletMesh = Mesh.CreateFromBrep(outletSrf.ToBrep(), mp)[0];

            // Walls (Floor, Ceiling, Y=0, Y=10)
            var wallsMesh = new Mesh();
            var floorSrf = NurbsSurface.CreateFromCorners(p0, p1, p2, p3);
            wallsMesh.Append(Mesh.CreateFromBrep(floorSrf.ToBrep(), mp)[0]);
            var ceilSrf = NurbsSurface.CreateFromCorners(p4, p5, p6, p7);
            wallsMesh.Append(Mesh.CreateFromBrep(ceilSrf.ToBrep(), mp)[0]);
            var wall1Srf = NurbsSurface.CreateFromCorners(p0, p1, p5, p4);
            wallsMesh.Append(Mesh.CreateFromBrep(wall1Srf.ToBrep(), mp)[0]);
            var wall2Srf = NurbsSurface.CreateFromCorners(p3, p2, p6, p7);
            wallsMesh.Append(Mesh.CreateFromBrep(wall2Srf.ToBrep(), mp)[0]);

            // Emitter Geometry (Small box in center)
            var ep0 = new Point3d(4.5, 4.5, 0.5);
            var ep1 = new Point3d(5.5, 4.5, 0.5);
            var ep2 = new Point3d(5.5, 5.5, 0.5);
            var ep3 = new Point3d(4.5, 5.5, 0.5);
            var ep4 = new Point3d(4.5, 4.5, 1.5);
            var ep5 = new Point3d(5.5, 4.5, 1.5);
            var ep6 = new Point3d(5.5, 5.5, 1.5);
            var ep7 = new Point3d(4.5, 5.5, 1.5);
            var emitterBox = new Box(Plane.WorldXY, new List<Point3d> { ep0, ep1, ep2, ep3, ep4, ep5, ep6, ep7 });
            var emitterMesh = Mesh.CreateFromBrep(emitterBox.ToBrep(), mp)[0];

            // 2. Create Boundary Conditions and Function Objects
            var walls = new List<IndoorBC.Wall> { new IndoorBC.Wall(wallsMesh, 3, 20.0) };
            var inlets = new List<IndoorBC.Inlet> { new IndoorBC.Inlet(inletMesh, 20.0, 3, new Vector3d(1, 0, 0)) };
            var outlets = new List<IndoorBC.Outlet> { new IndoorBC.Outlet(outletMesh, 3) };

            var fos = new List<FunctionObject>();
            // Create CO2Emitter (0 = Absolute rate, 0.005 kg/s, "Occupant")
            var co2Emitter = new CO2Emitter(emitterMesh, 0, 0.005, "Occupant");
            fos.Add(co2Emitter);

            // 3. Setup Domain Parameters
            int endTime = 100;
            string workingDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IndoorTest_CO2");
            if (!System.IO.Directory.Exists(workingDir)) System.IO.Directory.CreateDirectory(workingDir);
            
            double cellSize = 0.5;
            Point3d pointInside = new Point3d(2, 2, 1.5); // Adjusted to ensure it's not inside the emitter
            int cpus = 2;

            // 4. Instantiate IndoorDomain
            var domain = new IndoorDomain(endTime, workingDir, cellSize, pointInside, walls, inlets, outlets, fos, cpus);

            // 5. Assertions
            Assert.NotNull(domain);
            Assert.Equal(1, domain.functionObjectCount);
            // Verify CO2 emitter was processed
            Assert.Single(domain.CO2Emitters);
            Assert.Equal("Occupant_0", domain.CO2Emitters[0].ID); // ID is generated as Name + Index

            _output.WriteLine("Indoor Domain with CO2 Emitter created successfully.");
        }

        [Fact]
        public void CreateIndoorDomain_FullSetup_GeneratesCaseFiles()
        {
            // 1. Create Geometry
            var mp = new MeshingParameters();
            
            // Coordinates
            // Room 10x10x3
            // Inlet Window on X=0: Y=4-6, Z=1-2
            // Outlet Window on X=10: Y=4-6, Z=1-2

            // Inlet (X=0)
            var inletSrf = NurbsSurface.CreateFromCorners(
                new Point3d(0, 4, 1), new Point3d(0, 6, 1), 
                new Point3d(0, 6, 2), new Point3d(0, 4, 2));
            var inletMesh = Mesh.CreateFromBrep(inletSrf.ToBrep(), mp)[0];

            // Outlet (X=10)
            var outletSrf = NurbsSurface.CreateFromCorners(
                new Point3d(10, 4, 1), new Point3d(10, 6, 1), 
                new Point3d(10, 6, 2), new Point3d(10, 4, 2));
            var outletMesh = Mesh.CreateFromBrep(outletSrf.ToBrep(), mp)[0];

            // Walls construction
            var wallsMesh = new Mesh();

            // Floor (Z=0)
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(
                new Point3d(0,0,0), new Point3d(10,0,0), new Point3d(10,10,0), new Point3d(0,10,0)).ToBrep(), mp)[0]);
            
            // Ceiling (Z=3)
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(
                new Point3d(0,0,3), new Point3d(10,0,3), new Point3d(10,10,3), new Point3d(0,10,3)).ToBrep(), mp)[0]);

            // Side Wall Y=0
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(
                new Point3d(0,0,0), new Point3d(10,0,0), new Point3d(10,0,3), new Point3d(0,0,3)).ToBrep(), mp)[0]);

            // Side Wall Y=10
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(
                new Point3d(0,10,0), new Point3d(10,10,0), new Point3d(10,10,3), new Point3d(0,10,3)).ToBrep(), mp)[0]);

            // Remaining X=0 Face (Inlet Side) - simplified as 4 surrounding rectangles
            // Bottom strip (Z=0-1), Top strip (Z=2-3), Left strip (Y=0-4, Z=1-2), Right strip (Y=6-10, Z=1-2)
            // Note: X is constant 0
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(0,0,0), new Point3d(0,10,0), new Point3d(0,10,1), new Point3d(0,0,1)).ToBrep(), mp)[0]); // Bottom
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(0,0,2), new Point3d(0,10,2), new Point3d(0,10,3), new Point3d(0,0,3)).ToBrep(), mp)[0]); // Top
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(0,0,1), new Point3d(0,4,1), new Point3d(0,4,2), new Point3d(0,0,2)).ToBrep(), mp)[0]); // Left
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(0,6,1), new Point3d(0,10,1), new Point3d(0,10,2), new Point3d(0,6,2)).ToBrep(), mp)[0]); // Right

            // Remaining X=10 Face (Outlet Side) - similar
            // Note: X is constant 10
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(10,0,0), new Point3d(10,10,0), new Point3d(10,10,1), new Point3d(10,0,1)).ToBrep(), mp)[0]); // Bottom
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(10,0,2), new Point3d(10,10,2), new Point3d(10,10,3), new Point3d(10,0,3)).ToBrep(), mp)[0]); // Top
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(10,0,1), new Point3d(10,4,1), new Point3d(10,4,2), new Point3d(10,0,2)).ToBrep(), mp)[0]); // Left
            wallsMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(new Point3d(10,6,1), new Point3d(10,10,1), new Point3d(10,10,2), new Point3d(10,6,2)).ToBrep(), mp)[0]); // Right

            // 2. Boundary Conditions
            var walls = new List<IndoorBC.Wall> { new IndoorBC.Wall(wallsMesh, 3, 20.0) };
            var inlets = new List<IndoorBC.Inlet> { new IndoorBC.Inlet(inletMesh, 20.0, 3, new Vector3d(1, 0, 0)) };
            var outlets = new List<IndoorBC.Outlet> { new IndoorBC.Outlet(outletMesh, 3) };
            var fos = new List<FunctionObject>();

            // 3. Setup Domain Parameters
            int endTime = 50;
            string workingDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IndoorTest_FullSetup");
            if (System.IO.Directory.Exists(workingDir)) System.IO.Directory.Delete(workingDir, true); // Clean up first
            System.IO.Directory.CreateDirectory(workingDir);
            
            double cellSize = 0.5;
            Point3d pointInside = new Point3d(5, 5, 1.5);
            int cpus = 2;

            // 4. Instantiate IndoorDomain - this triggers file export
            var domain = new IndoorDomain(endTime, workingDir, cellSize, pointInside, walls, inlets, outlets, fos, cpus);

            // 5. Verify File Generation
            Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(workingDir, "0")), "0 directory not created");
            Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(workingDir, "constant")), "constant directory not created");
            Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(workingDir, "system")), "system directory not created");

            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(workingDir, "run_all.bat")), "run_all.bat not created");
                        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(workingDir, "system", "controlDict")), "controlDict not created");
                        
                        // Check specific dictionary content (e.g., endTime in controlDict)
                        string controlDictContent = System.IO.File.ReadAllText(System.IO.Path.Combine(workingDir, "system", "controlDict"));
                        Assert.Matches($"endTime\\s+{endTime};", controlDictContent);
            
                        _output.WriteLine($"Full simulation setup verified in {workingDir}");
                    }


        [NotWindowsServerFact]
        public void IndoorSimpleCase_GeneratesAndExecutesSuccessfully()
        {
            // Arrange
            var caseDir = TestFixtures.CreateTestDirectory("testcase-indoor-simple");

            // Load STLs
            // Note: Paths are relative to the solution root as per GeometryHelpers.LoadMergedMesh
            var envelopeMesh = GeometryHelpers.LoadMergedMesh(@"RhinoPlugin.Test.Xunit\Resources\Wall0.stl");
            var inletMesh = GeometryHelpers.LoadMergedMesh(@"RhinoPlugin.Test.Xunit\Resources\Inlet1.stl");
            var outletMesh = GeometryHelpers.LoadMergedMesh(@"RhinoPlugin.Test.Xunit\Resources\Outlet2.stl");

            // Scale to meters (STL is in mm)
            var scale = Transform.Scale(Point3d.Origin, 0.001);
            envelopeMesh.Transform(scale);
            inletMesh.Transform(scale);
            outletMesh.Transform(scale);

            // Boundary Conditions
            // Envelope: 20C, Refinement 2
            var walls = new List<IndoorBC.Wall>
            {
                new IndoorBC.Wall(envelopeMesh, 2, 20.0) { Name = "Envelope" }
            };

            // Inlet: 20C, 1 m/s (Assuming X direction for now), Refinement 2
            var inlets = new List<IndoorBC.Inlet>
            {
                new IndoorBC.Inlet(inletMesh, 20.0, 2, new Vector3d(1, 0, 0)) { Name = "Inlet" }
            };

            // Outlet: Refinement 2
            var outlets = new List<IndoorBC.Outlet>
            {
                new IndoorBC.Outlet(outletMesh, 2) { Name = "Outlet" }
            };

            // Setup Simulation Parameters
            double cellSize = 0.5; // Meters - Updated as per user request
            int endTime = 500;
            int cpus = 24;
            
            // Point inside domain - using centroid of envelope as a guess, typically indoor geometry is centered or simple enough
            var bbox = envelopeMesh.GetBoundingBox(true);
            var pointInside = bbox.Center; 

            _output.WriteLine($"Meters BBox: {bbox.Min} to {bbox.Max}");
            _output.WriteLine($"PointInside: {pointInside}");
            // Function Objects (None for this simple test)
            var fos = new List<FunctionObject>();

            // Act
            // IndoorDomain constructor generates all files
            var domain = new IndoorDomain(
                endTime, 
                caseDir, 
                cellSize, 
                pointInside, 
                walls, 
                inlets, 
                outlets, 
                fos, 
                cpus
            );

            // Assert: Check if critical files were created
            AssertCaseFilesGenerated(caseDir);

            // Create case.foam
            System.IO.File.Create(System.IO.Path.Combine(caseDir, "case.foam")).Dispose();
            
            _output.WriteLine($"Indoor Simulation Case generated at: {caseDir}");
        }

        private static void AssertCaseFilesGenerated(string caseDir)
        {
            var systemDir = System.IO.Path.Combine(caseDir, "system");
            
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(systemDir, "blockMeshDict")), "blockMeshDict not found");
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(systemDir, "snappyHexMeshDict")), "snappyHexMeshDict not found");
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(systemDir, "controlDict")), "controlDict not found");
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(caseDir, "run_all.bat")), "run_all.bat not found");
        }
    }
}
