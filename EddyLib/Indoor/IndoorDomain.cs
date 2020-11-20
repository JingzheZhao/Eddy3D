using EddyLib.Indoor.Dicts;
using Newtonsoft.Json;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

namespace EddyLib.Indoor
{
    public class IndoorDomain
    {
        public BoundingBox BoundingBox;

        //private List<IndoorBC.Wall> Geometry { get; set; } //Surfaces or Volumes. IE Walls, table, whatever

        //private List<IndoorBC.Inlet> Inlets { get; set; } //Surfaces

        //private List<IndoorBC.Outlet> Outlets { get; set; }//Surfaces

        //private List<IndoorBC.Emitter> Emitters { get; set; } //Volumes

        public Point3d[] Edges;

        private readonly double CellSize;

        private Point3d PointInsideDomain;

        public string WorkingDir;

        private readonly List<IndoorBC> AllGeometry = new List<IndoorBC>();

        private readonly List<GenericDict> AllDicts = new List<GenericDict>();

        public List<VolumetricHeatSource> VolumetricHeatSources = new List<VolumetricHeatSource>();

        public List<MomentumSink> MomentumSinks = new List<MomentumSink>();

        public IndoorDomain()
        {
        }

        public IndoorDomain(string WorkingDir, double CellSize, Point3d PointInsideDomain, List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets, List<VolumetricHeatSource> vhs)
        {
            // Give unique index to every object

            this.WorkingDir = WorkingDir;

            this.PointInsideDomain = PointInsideDomain;

            int cnt = 0;

            for (int i = 0; i < RoomGeometry.Count; i++)
            {
                RoomGeometry[i].Id = RoomGeometry[i].Name + cnt;
                AllGeometry.Add(RoomGeometry[i]);
                cnt++;
            }
            for (int i = 0; i < Inlets.Count; i++)
            {
                Inlets[i].Id = Inlets[i].Name + cnt;
                AllGeometry.Add(Inlets[i]);
                cnt++;
            }
            for (int i = 0; i < Outlets.Count; i++)
            {
                Outlets[i].Id = Outlets[i].Name + cnt;
                AllGeometry.Add(Outlets[i]);
                cnt++;
            }

            // Walls

            //this.Geometry = RoomGeometry;

            var b = GetBoundingBox(RoomGeometry);

            var x = Transform.Scale(b.Center, 1.2);
            b.Transform(x);

            this.BoundingBox = b;

            this.Edges = this.BoundingBox.GetCorners();

            // Inlets

            //this.Inlets = Inlets;

            //// Outlets

            //this.Outlets = Outlets;

            // Misc

            //this.WorkingDir = workingDir;

            this.CellSize = CellSize;

            // Dicts

            // Construct all Dicts

            // BCs

            var u = new IndoorBCDict.U(Inlets, Outlets, RoomGeometry);
            var alphat = new IndoorBCDict.alphat(Inlets, Outlets, RoomGeometry);
            var AoA = new IndoorBCDict.AoA(Inlets, Outlets, RoomGeometry);
            var nut = new IndoorBCDict.nut(Inlets, Outlets, RoomGeometry);
            var omega = new IndoorBCDict.omega(Inlets, Outlets, RoomGeometry);
            var k = new IndoorBCDict.k(Inlets, Outlets, RoomGeometry);
            var p = new IndoorBCDict.p(Inlets, Outlets, RoomGeometry);
            var p_rgh = new IndoorBCDict.p_rgh(Inlets, Outlets, RoomGeometry);
            var T = new IndoorBCDict.T(Inlets, Outlets, RoomGeometry);

            AllDicts.Add(u);
            AllDicts.Add(alphat);
            AllDicts.Add(AoA);
            AllDicts.Add(nut);
            AllDicts.Add(omega);
            AllDicts.Add(k);
            AllDicts.Add(p);
            AllDicts.Add(p_rgh);
            AllDicts.Add(T);

            // System

            var controlDict = new ControlDict();
            var blockMeshDict = new BlockMeshDict(this.CellSize, BoundingBox);
            var snappyHextMeshDict = new SnappyHexMeshDict(this.CellSize, this.PointInsideDomain, BoundingBox, Inlets, Outlets, RoomGeometry);
            var fvSchemesDict = new FvSchemesDict();
            var fvSolutionDict = new FvSolutionDict();
            var residualsDict = new ResidualsDict();
            var surfaceFeatureExtractDict = new SurfaceFeatureExtractDict();

            AllDicts.Add(controlDict);
            AllDicts.Add(blockMeshDict);
            AllDicts.Add(snappyHextMeshDict);
            AllDicts.Add(fvSchemesDict);
            AllDicts.Add(fvSolutionDict);
            AllDicts.Add(residualsDict);
            AllDicts.Add(surfaceFeatureExtractDict);

            // Constant

            var g = new GDict();
            var thermoPhysicalProperties = new ThermoPhysicalPropertiesDict();
            var turbulenceProperties = new TurbulencePropertiesDict();

            AllDicts.Add(g);
            AllDicts.Add(thermoPhysicalProperties);
            AllDicts.Add(turbulenceProperties);

            // Function Objects

            // VHS

            for (int i = 0; i < vhs.Count; i++)
            {
                this.VolumetricHeatSources.Add(vhs[i]);

                this.VolumetricHeatSources[i].Name = i.ToString();
            }
            AllDicts.Add(new VolumetricHeatSourceDict(this.VolumetricHeatSources));

            // Momentum Sinks

            for (int i = 0; i < MomentumSinks.Count; i++)
            {
                this.MomentumSinks.Add(MomentumSinks[i]);
                this.MomentumSinks[i].Name = i.ToString();
            }
            AllDicts.Add(new MomentumSinkDict(this));

            ExportGeometryAndDicts(WorkingDir);
        }

        public void ExportGeometryAndDicts(string workingDir)
        {
            foreach (GenericDict dict in AllDicts)
            {
                dict.Export(workingDir);
            }

            // Export Geometry as STL

            var stlDir = Path.Combine(workingDir, "constant", "triSurface");
            Directory.CreateDirectory(stlDir);
            foreach (var geo in AllGeometry)
            {
                EddyLib.STLExport.ExportBinary(stlDir + "\\" + geo.Name + ".stl", geo.Geometry);
            }
        }

        private BoundingBox GetBoundingBox(List<IndoorBC.Wall> RoomGeometry)
        {
            BoundingBox bb = new BoundingBox();
            Mesh RG = new Mesh();

            foreach (IndoorBC m in RoomGeometry)
            {
                if (m != null)
                {
                    RG.Append(m.Geometry);
                }
            }

            bb = RG.GetBoundingBox(false);

            return bb;
        }

        public IndoorDomain Duplicate()
        {
            string json = JsonConvert.SerializeObject(this);

            IndoorDomain dup = JsonConvert.DeserializeObject<IndoorDomain>(json);
            return dup;
        }
    }
}