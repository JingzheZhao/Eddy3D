using Newtonsoft.Json;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

using EddyLib.Indoor.Dicts;
using EddyLib.Indoor.BatchFiles;
using Grasshopper.Kernel.Types;
using EddyLib.Indoor.FunctionObjects;

namespace EddyLib.Indoor
{
    public class IndoorDomain
    {
        public BoundingBox BoundingBox;

        //private List<IndoorBC.Wall> Geometry { get; set; } //Surfaces or Volumes. IE Walls, table, whatever

        //private List<IndoorBC.Inlet> Inlets { get; set; } //Surfaces

        //private List<IndoorBC.Outlet> Outlets { get; set; }//Surfaces

        //private List<IndoorBC.Emitter> Emitters { get; set; } //Volumes

        public int endTime { get; set; }

        public Point3d[] Edges { get; set; }

        private readonly double CellSize;

        private Point3d PointInsideDomain;

        public string WorkingDir;

        private readonly List<IndoorBC> AllGeometry = new List<IndoorBC>();

        private readonly List<GenericBatchFile> AllBatsWrite2File = new List<GenericBatchFile>();

        private readonly List<GenericDict> AllDictsWrite2File = new List<GenericDict>();

        private readonly List<FunctionObjectDict> AllFOWrite2File = new List<FunctionObjectDict>();

        //private readonly List<FunctionObjectDict> AllFOWrite2File = new List<FunctionObjectDict>();

        private readonly List<FunctionObjectDictInternal> AllFunctionObjectInternalDicts = new List<FunctionObjectDictInternal>();

        public List<FunctionObject> FOs = new List<FunctionObject>();

        public List<VolumetricHeatSource> VolumetricHeatSources = new List<VolumetricHeatSource>();
        public List<FunctionObjectDictInternal> VHSID = new List<FunctionObjectDictInternal>();

        public List<MomentumSink> MomentumSinks = new List<MomentumSink>();
        public List<FunctionObjectDictInternal> MSinkID = new List<FunctionObjectDictInternal>();

        public List<MomentumSource> MomentumSources = new List<MomentumSource>();
        public List<FunctionObjectDictInternal> MSourceID = new List<FunctionObjectDictInternal>();

        public List<CO2Emitter> CO2Emitters = new List<CO2Emitter>();
        public List<FunctionObjectDictInternal> CO2ID = new List<FunctionObjectDictInternal>();

        public List<ViralEmitter> ViralEmitters = new List<ViralEmitter>();
        public List<FunctionObjectDictInternal> ViralID = new List<FunctionObjectDictInternal>();

        public IndoorDomain()
        {
        }

        public IndoorDomain(int endTime, string WorkingDir, double CellSize, Point3d PointInsideDomain, List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets, List<FunctionObject> FOs)
        {
            // Give unique index to every object

            this.WorkingDir = WorkingDir;
            this.endTime = endTime;

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

            var b = GetBoundingBox(RoomGeometry);

            var x = Transform.Scale(b.Center, 1.2);
            b.Transform(x);

            this.BoundingBox = b;

            this.Edges = this.BoundingBox.GetCorners();

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

            AllDictsWrite2File.Add(u);
            AllDictsWrite2File.Add(alphat);
            AllDictsWrite2File.Add(AoA);
            AllDictsWrite2File.Add(nut);
            AllDictsWrite2File.Add(omega);
            AllDictsWrite2File.Add(k);
            AllDictsWrite2File.Add(p);
            AllDictsWrite2File.Add(p_rgh);
            AllDictsWrite2File.Add(T);

            // Constant

            var g = new GDict();
            var thermoPhysicalProperties = new ThermoPhysicalPropertiesDict();
            var turbulenceProperties = new TurbulencePropertiesDict();

            AllDictsWrite2File.Add(g);
            AllDictsWrite2File.Add(thermoPhysicalProperties);
            AllDictsWrite2File.Add(turbulenceProperties);

            // Function Objects

            // VHS

            //var VHSInternalDicts = new List<FunctionObjectDictInternal>();

            for (int i = 0; i < FOs.Count; i++)
            {
                if (FOs[i] is VolumetricHeatSource)
                {
                    this.FOs.Add((VolumetricHeatSource)FOs[i]);
                    this.VolumetricHeatSources.Add((VolumetricHeatSource)FOs[i]);
                    this.VolumetricHeatSources[i].ID = FOs[i].Name + i.ToString();
                    //AllFunctionObjectInternalDicts.Add(new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[i], this.PointInsideDomain));

                    var dict = new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[i], this.PointInsideDomain);
                    AllFunctionObjectInternalDicts.Add(dict);
                    VHSID.Add(dict);
                }
            }

            // Momentum Sinks

            for (int i = 0; i < FOs.Count; i++)
            {
                if (FOs[i] is MomentumSink)
                {
                    this.FOs.Add(MomentumSinks[i]);
                    this.MomentumSinks.Add(MomentumSinks[i]);
                    this.MomentumSinks[i].ID = FOs[i].Name + i.ToString();

                    var dict = new MomentumSinkInternalDict(this.MomentumSinks[i], this.PointInsideDomain);
                    AllFunctionObjectInternalDicts.Add(dict);
                    MSinkID.Add(dict);


                }
            }

            // Momentum Source

            for (int i = 0; i < FOs.Count; i++)
            {
                if (FOs[i] is MomentumSource)
                {
                    this.FOs.Add(MomentumSources[i]);
                    this.MomentumSources.Add(MomentumSources[i]);
                    this.MomentumSources[i].ID = FOs[i].Name + i.ToString();
                    //AllFunctionObjectInternalDicts.Add(new MomentumSourceInternalDict(this.MomentumSources[i], this.PointInsideDomain));

                    var dict = new MomentumSourceInternalDict(this.MomentumSources[i], this.PointInsideDomain);
                    AllFunctionObjectInternalDicts.Add(dict);
                    MSourceID.Add(dict);

                }
            }


            //CO2 Emitters

            for (int i = 0; i < FOs.Count; i++)
            {
                if (FOs[i] is CO2Emitter)
                {
                    this.FOs.Add(CO2Emitters[i]);
                    this.CO2Emitters.Add(CO2Emitters[i]);
                    this.CO2Emitters[i].ID = FOs[i].Name + i.ToString();

                    var dict = new CO2EmitterInternalDict(this.CO2Emitters[i], this.PointInsideDomain);
                    AllFunctionObjectInternalDicts.Add(dict);
                    CO2ID.Add(dict);

                }
            }


            //Viral Emitters

            for (int i = 0; i < FOs.Count; i++)
            {
                if (FOs[i] is ViralEmitter)
                {
                    this.FOs.Add(ViralEmitters[i]);
                    this.ViralEmitters.Add(ViralEmitters[i]);
                    this.ViralEmitters[i].ID = FOs[i].Name + i.ToString();

                    var dict = new ViralEmitterInternalDict(this.ViralEmitters[i], this.PointInsideDomain);
                    AllFunctionObjectInternalDicts.Add(dict);
                    ViralID.Add(dict);

                }
            }

            // TopoSet

            var topoSetDict = new TopoSetDict(AllFunctionObjectInternalDicts, PointInsideDomain);

            // System

            var controlDict = new ControlDict(this);
            var blockMeshDict = new BlockMeshDict(this.CellSize, BoundingBox);
            var snappyHextMeshDict = new SnappyHexMeshDict(this.CellSize, this.PointInsideDomain, BoundingBox, Inlets, Outlets, RoomGeometry);
            var fvSchemesDict = new FvSchemesDict(this);
            var fvSolutionDict = new FvSolutionDict();
            var residualsDict = new ResidualsDict();
            var surfaceFeatureExtractDict = new SurfaceFeatureExtractDict(Inlets, Outlets, RoomGeometry);
            var decomposeParDict = new DecomposeParDict();

            var momentumSinkDict = new FunctionObjectDict(MSinkID);
            var momentumSourceDict = new FunctionObjectDict(MSourceID);
            var co2EmittersDict = new FunctionObjectDict(CO2ID);
            var viralEmittersDict = new FunctionObjectDict(ViralID);

            //var momentumSinkDict = new FunctionObjectDict(MSinkID, "momentumSinkDict");






            AllDictsWrite2File.Add(controlDict);
            AllDictsWrite2File.Add(blockMeshDict);
            AllDictsWrite2File.Add(snappyHextMeshDict);
            AllDictsWrite2File.Add(fvSchemesDict);
            AllDictsWrite2File.Add(fvSolutionDict);
            AllDictsWrite2File.Add(residualsDict);
            AllDictsWrite2File.Add(surfaceFeatureExtractDict);
            AllDictsWrite2File.Add(decomposeParDict);

            // FOs
            AllFOWrite2File.Add(momentumSinkDict);
            AllFOWrite2File.Add(momentumSourceDict);
            AllFOWrite2File.Add(co2EmittersDict);
            AllFOWrite2File.Add(viralEmittersDict);

            // AllFOWrite2File.Add(fvOptionsDict);


            ExportGeometryAndDicts(WorkingDir);

            //Batch Files

            var runMeshBatch = new RunMeshBatch(this);
            var runSimBatch = new RunSimBatch(this);
            var runTopoBatch = new RunTopoBatch(this);

            AllBatsWrite2File.Add(runMeshBatch);
            AllBatsWrite2File.Add(runSimBatch);
            AllBatsWrite2File.Add(runTopoBatch);


            ExportBatch(WorkingDir);
        }


        //export geomety and dictionaries
        public void ExportGeometryAndDicts(string workingDir)
        {
            foreach (GenericDict dict in AllDictsWrite2File)
            {
                dict.Export(workingDir);
            }
            // Export Geometry as STL

            var stlDir = Path.Combine(workingDir, "constant", "triSurface");
            Directory.CreateDirectory(stlDir);
            foreach (var geo in AllGeometry)
            {
                EddyLib.STLExport.ExportBinary(stlDir + "\\" + geo.Id + ".stl", geo.Geometry);
            }
        }

        //export FOs

        public void ExportFO(string workingDir)
        {
            foreach (FunctionObjectDict dict in AllFOWrite2File)
            {
                dict.Export(workingDir);
            }
        }


        //export batch files
        public void ExportBatch(string workingDir) 
        {
            foreach (GenericBatchFile dict in AllBatsWrite2File)
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