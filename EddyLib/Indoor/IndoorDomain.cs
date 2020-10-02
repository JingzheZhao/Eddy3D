using EddyLib.Indoor.Dicts;
using Newtonsoft.Json;
using Rhino.Geometry;
using System.Collections.Generic;

namespace EddyLib.Indoor
{
    public class IndoorDomain
    {
        public BoundingBox BoundingBox;

        private List<IndoorBC.Wall> Geometry; //Surfaces or Volumes. IE Walls, table, whatever

        private List<IndoorBC.Inlet> Inlets; //Surfaces

        private List<IndoorBC.Outlet> Outlets;//Surfaces

        private List<IndoorBC.Emitter> Emitters; //Volumes

        public Point3d[] Edges;

        private double CellSize;

        private string WorkingDir;

        public IndoorDomain()
        {
        }

        public IndoorDomain(string workingDir, double CellSize, List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets)
        {
            // Give unique index to every object

            int cnt = 0;

            for (int i = 0; i < RoomGeometry.Count; i++)
            {
                RoomGeometry[i].Id = RoomGeometry[i].Name + cnt;
                cnt++;
            }
            for (int i = 0; i < Inlets.Count; i++)
            {
                Inlets[i].Id = Inlets[i].Name + cnt;
                cnt++;
            }
            for (int i = 0; i < Outlets.Count; i++)
            {
                Outlets[i].Id = Outlets[i].Name + cnt;
                cnt++;
            }

            // Walls

            this.BoundingBox = GetBoundingBox(RoomGeometry);
            this.Edges = this.BoundingBox.GetCorners();

            // Inlets

            this.Inlets = Inlets;

            // Outlets

            this.Outlets = Outlets;

            // Misc

            this.WorkingDir = workingDir;

            this.CellSize = CellSize;

            // Dicts

            List<GenericDict> allDicts = new List<GenericDict>();

            // Construct all Dicts

            // BCs

            var u = new IndoorBCDict.U(Inlets, Outlets, RoomGeometry);
            var alphat = new IndoorBCDict.alphat(Inlets, Outlets, RoomGeometry);
            var AoA = new IndoorBCDict.AoA(Inlets, Outlets, RoomGeometry);
            var nut = new IndoorBCDict.nut(Inlets, Outlets, RoomGeometry);
            var omega = new IndoorBCDict.omega(Inlets, Outlets, RoomGeometry);
            var p = new IndoorBCDict.p(Inlets, Outlets, RoomGeometry);
            var p_rgh = new IndoorBCDict.p_rgh(Inlets, Outlets, RoomGeometry);
            var T = new IndoorBCDict.T(Inlets, Outlets, RoomGeometry);

            allDicts.Add(u);
            allDicts.Add(alphat);
            allDicts.Add(AoA);
            allDicts.Add(nut);
            allDicts.Add(omega);
            allDicts.Add(p);
            allDicts.Add(p_rgh);
            allDicts.Add(T);

            // System

            var controlDict = new ControlDict();
            var blockMeshDict = new BlockMeshDict(CellSize, BoundingBox);
            var snappyHextMeshDict = new SnappyHexMeshDict(CellSize, BoundingBox, Inlets, Outlets, RoomGeometry);
            var fvSchemesDict = new FvSchemesDict();
            var fvSolutionDict = new FvSolutionDict();
            var residualsDict = new ResidualsDict();
            var surfaceFeatureExtractDict = new SurfaceFeatureExtractDict();

            allDicts.Add(controlDict);
            allDicts.Add(blockMeshDict);
            allDicts.Add(snappyHextMeshDict);
            allDicts.Add(fvSchemesDict);
            allDicts.Add(fvSolutionDict);
            allDicts.Add(residualsDict);
            allDicts.Add(surfaceFeatureExtractDict);

            // Constant

            var g = new GDict();
            var thermoPhysicalProperties = new ThermoPhysicalPropertiesDict();
            var turbulenceProperties = new TurbulencePropertiesDict();

            allDicts.Add(g);
            allDicts.Add(thermoPhysicalProperties);
            allDicts.Add(turbulenceProperties);

            foreach (GenericDict dict in allDicts)
            {
                dict.Export(workingDir);
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