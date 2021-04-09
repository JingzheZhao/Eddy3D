using EddyLib.Indoor.BatchFiles;
using EddyLib.Indoor.Dicts;
using EddyLib.Indoor.FunctionObjects;
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

        public int endTime { get; set; }

        public Point3d[] Edges { get; set; }

        private readonly double CellSize;

        private Point3d PointInsideDomain;

        public string WorkingDir;

        private readonly List<IndoorBC> AllGeometry = new List<IndoorBC>();

        //private readonly List<GenericBatchFile> AllBatsWrite2File = new List<GenericBatchFile>();

        private readonly List<GenericDict> AllDictsWrite2File = new List<GenericDict>();

        //private readonly List<FunctionObjectDict> AllFOWrite2File = new List<FunctionObjectDict>();

        //private readonly List<FunctionObjectDict> AllFOWrite2File = new List<FunctionObjectDict>();

        private readonly List<GenericDict> AllFunctionObjectInternalDicts = new List<GenericDict>();

        public List<FunctionObject> FOs = new List<FunctionObject>();

        public List<VolumetricHeatSource> VolumetricHeatSources = new List<VolumetricHeatSource>();
        //public List<TopoSetSubDict> VHSID = new List<TopoSetSubDict>();

        public List<MomentumSinkIndoor> MomentumSinks = new List<MomentumSinkIndoor>();
       // public List<TopoSetSubDict> MSinkID = new List<TopoSetSubDict>();

        public List<MomentumSource> MomentumSources = new List<MomentumSource>();
        //public List<TopoSetSubDict> MSourceID = new List<TopoSetSubDict>();

        public List<CO2Emitter> CO2Emitters = new List<CO2Emitter>();
        //public List<TopoSetSubDict> CO2ID = new List<TopoSetSubDict>();

        public List<ViralEmitter> ViralEmitters = new List<ViralEmitter>();
        //public List<TopoSetSubDict> ViralID = new List<TopoSetSubDict>();

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


            //Iterate through function objects
            //Name each item per FO type
            //Create a new dictionary for each FO type with  entires for each item.


            //FOREACH IMPLEMENTATION

            //int vhs = 0;
            //foreach (VolumetricHeatSource element in FOs)
            //{
            //    this.FOs.Add((VolumetricHeatSource)FOs[vhs]);
            //    this.VolumetricHeatSources.Add((VolumetricHeatSource)FOs[vhs]);
            //    this.VolumetricHeatSources[vhs].ID = FOs[vhs].Name + vhs.ToString();
            //    //AllFunctionObjectInternalDicts.Add(new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[i], this.PointInsideDomain));

            //    var dict = new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[vhs], this.PointInsideDomain);
            //    dict.Export(WorkingDir);

            //    AllFunctionObjectInternalDicts.Add(dict);
            //    //VHSID.Add(dict);

            //    vhs++;

            //}

            //int j = 0;
            //foreach (MomentumSink element in FOs)
            //{
            //    this.FOs.Add((MomentumSink)FOs[j]);
            //    this.MomentumSinks.Add((MomentumSink)FOs[j]);
            //    this.MomentumSinks[j].ID = FOs[j].Name + j.ToString();

            //    var dict = new MomentumSinkInternalDict(this.MomentumSinks[j], this.PointInsideDomain);
            //    dict.Export(WorkingDir);

            //    AllFunctionObjectInternalDicts.Add(dict);

            //    j++;
            //}

            //int ms = 0;
            //foreach (MomentumSource element in FOs)
            //{
            //    this.FOs.Add((MomentumSource)FOs[ms]);
            //    this.MomentumSources.Add((MomentumSource)FOs[ms]);
            //    this.MomentumSources[ms].ID = FOs[ms].Name + ms.ToString();
            //    //AllFunctionObjectInternalDicts.Add(new MomentumSourceInternalDict(this.MomentumSources[i], this.PointInsideDomain));

            //    var dict = new MomentumSourceInternalDict(this.MomentumSources[ms], this.PointInsideDomain);
            //    dict.Export(WorkingDir);

            //    AllFunctionObjectInternalDicts.Add(dict);
            //    //MSourceID.Add(dict);

            //    ms++;
            //}

            //int c = 0;
            //foreach (CO2Emitter element in FOs)
            //{
            //    this.FOs.Add((CO2Emitter)FOs[c]);
            //    this.CO2Emitters.Add((CO2Emitter)FOs[c]);
            //    this.CO2Emitters[c].ID = FOs[c].Name + c.ToString();

            //    var dict = new CO2EmitterInternalDict(this.CO2Emitters[c], this.PointInsideDomain);
            //    dict.Export(WorkingDir);

            //    AllFunctionObjectInternalDicts.Add(dict);

            //    c++;
            //}

            //int v = 0;
            //foreach (ViralEmitter element in FOs)
            //{
            //    this.FOs.Add((ViralEmitter)FOs[v]);
            //    this.ViralEmitters.Add((ViralEmitter)FOs[v]);
            //    this.ViralEmitters[v].ID = FOs[v].Name + v.ToString();

            //    var dict = new ViralEmitterInternalDict(this.ViralEmitters[v], this.PointInsideDomain);
            //    dict.Export(WorkingDir);

            //    AllFunctionObjectInternalDicts.Add(dict);

            //    v++;
            //}


            
            //iterate through FOs and count each type of fo per type
            int ctVHS = 0;
            int ctMsi = 0;
            int ctMso = 0;
            int ctCo2 = 0;
            int ctVir = 0;

            for (int i = 0; i < FOs.Count; i++)
            {

                //Voluetric Heat Source
                if (FOs[i] is VolumetricHeatSource)
                {
                    this.FOs.Add((VolumetricHeatSource)FOs[i]);

                    //timur's implementation
                    //var myob = (VolumetricHeatSource)FOs[i];

                    //myob.ID = FOs[i].Name + i.ToString();

                    //this.VolumetricHeatSources.Add(myob);

                    //var dict = new VolumetricHeatSourceInternalDict(myob, this.PointInsideDomain);
                    //dict.Export(WorkingDir);

                    this.VolumetricHeatSources.Add((VolumetricHeatSource)FOs[i]);
                    this.VolumetricHeatSources[ctVHS].ID = FOs[i].Name + "_"+ i.ToString();

                    //var dict = new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[ctVHS], this.PointInsideDomain);
                    var dict = new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources, this.PointInsideDomain);

                    //dict.Export(WorkingDir);

                    AllFunctionObjectInternalDicts.Add(dict);
                    ctVHS++; 
                }

                else if (FOs[i] is MomentumSinkIndoor)
                {
                    this.FOs.Add((MomentumSinkIndoor)FOs[i]);
                    this.MomentumSinks.Add((MomentumSinkIndoor)FOs[i]);
                    this.MomentumSinks[ctMsi].ID = FOs[i].Name + "_" + i.ToString();

                    //var dict = new MomentumSinkIndoorInternalDict(this.MomentumSinks[ctMsi], this.PointInsideDomain);
                    var dict = new MomentumSinkIndoorInternalDict(this.MomentumSinks, this.PointInsideDomain);
                    //dict.Export(WorkingDir);

                    AllFunctionObjectInternalDicts.Add(dict);

                    ctMsi++; 
                }

                else if (FOs[i] is MomentumSource)
                {
                    this.FOs.Add((MomentumSource)FOs[i]);
                    this.MomentumSources.Add((MomentumSource)FOs[i]);
                    this.MomentumSources[ctMso].ID = FOs[i].Name + "_" + i.ToString();

                    // var dict = new MomentumSourceInternalDict(this.MomentumSources[ctMso], this.PointInsideDomain);
                    var dict = new MomentumSourceInternalDict(this.MomentumSources, this.PointInsideDomain);
                    //dict.Export(WorkingDir);

                    AllFunctionObjectInternalDicts.Add(dict);

                    ctMso++; 
                }

                else if (FOs[i] is CO2Emitter)
                {
                    this.FOs.Add((CO2Emitter)FOs[i]);
                    this.CO2Emitters.Add((CO2Emitter)FOs[i]);
                    this.CO2Emitters[ctCo2].ID = FOs[i].Name + "_" + i.ToString();

                    //var dict = new CO2EmitterInternalDict(this.CO2Emitters[ctCo2], this.PointInsideDomain);
                    var dict = new CO2EmitterInternalDict(this.CO2Emitters, this.PointInsideDomain);
                    //dict.Export(WorkingDir);

                    AllFunctionObjectInternalDicts.Add(dict);

                    ctCo2++; 
                }

                else if (FOs[i] is ViralEmitter)
                {

                    this.FOs.Add((ViralEmitter)FOs[i]);
                    this.ViralEmitters.Add((ViralEmitter)FOs[i]);
                    this.ViralEmitters[ctVir].ID = FOs[i].Name + "_" + i.ToString();

                    //var dict = new ViralEmitterInternalDict(this.ViralEmitters[ctVir], this.PointInsideDomain);
                    var dict = new ViralEmitterInternalDict(this.ViralEmitters, this.PointInsideDomain);

                    //dict.Export(WorkingDir);

                    AllFunctionObjectInternalDicts.Add(dict);

                    ctVir++; 
                }

            }


            ExportGeometryAndDicts(WorkingDir);


            ////iterate through each of the fos

            //for (int i = 0; i < FOs.Count; i++) 
            //{
            //    if (FOs[i] is VolumetricHeatSource)
            //    {
            //        this.FOs.Add((VolumetricHeatSource)FOs[i]);
            //        this.VolumetricHeatSources.Add((VolumetricHeatSource)FOs[i]);
            //        this.VolumetricHeatSources[i].ID = FOs[i].Name + i.ToString();
            //        //AllFunctionObjectInternalDicts.Add(new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[i], this.PointInsideDomain));

            //        var dict = new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[i], this.PointInsideDomain);
            //        dict.Export(WorkingDir);

            //        AllFunctionObjectInternalDicts.Add(dict);
            //        //VHSID.Add(dict);
            //    }

            //}



            //    for (int i = 0; i < FOs.Count; i++)
            //{

            //    //Voluetric Heat Source
            //    if (FOs[i] is VolumetricHeatSource)
            //    {
            //        this.FOs.Add((VolumetricHeatSource)FOs[i]);
            //        this.VolumetricHeatSources.Add((VolumetricHeatSource)FOs[i]);
            //        this.VolumetricHeatSources[i].ID = FOs[i].Name + i.ToString();
            //        //AllFunctionObjectInternalDicts.Add(new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[i], this.PointInsideDomain));

            //        var dict = new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources[i], this.PointInsideDomain);
            //        dict.Export(WorkingDir);

            //        AllFunctionObjectInternalDicts.Add(dict);
            //        //VHSID.Add(dict);
            //    }

            //    // Momentum Sinks
            //    if (FOs[i] is MomentumSink)
            //    {
            //        this.FOs.Add((MomentumSink)FOs[i]);
            //        this.MomentumSinks.Add((MomentumSink)FOs[i]);
            //        this.MomentumSinks[i].ID = FOs[i].Name + i.ToString();

            //        var dict = new MomentumSinkInternalDict(this.MomentumSinks[i], this.PointInsideDomain);
            //        dict.Export(WorkingDir);

            //        AllFunctionObjectInternalDicts.Add(dict);
            //        //MSinkID.Add(dict);

            //    }

            //    // Momentum Source
            //    if (FOs[i] is MomentumSource)
            //    {
            //        this.FOs.Add((MomentumSource)FOs[i]);
            //        this.MomentumSources.Add((MomentumSource)FOs[i]);
            //        this.MomentumSources[i].ID = FOs[i].Name + i.ToString();
            //        //AllFunctionObjectInternalDicts.Add(new MomentumSourceInternalDict(this.MomentumSources[i], this.PointInsideDomain));

            //        var dict = new MomentumSourceInternalDict(this.MomentumSources[i], this.PointInsideDomain);
            //        dict.Export(WorkingDir);

            //        AllFunctionObjectInternalDicts.Add(dict);
            //        //MSourceID.Add(dict);

            //    }

            //    //CO2 Emitters
            //    if (FOs[i] is CO2Emitter)
            //    {
            //        this.FOs.Add((CO2Emitter)FOs[i]);
            //        this.CO2Emitters.Add((CO2Emitter)FOs[i]);
            //        this.CO2Emitters[i].ID = FOs[i].Name + i.ToString();

            //        var dict = new CO2EmitterInternalDict(this.CO2Emitters[i], this.PointInsideDomain);
            //        dict.Export(WorkingDir);

            //        AllFunctionObjectInternalDicts.Add(dict);
            //        //CO2ID.Add(dict);

            //    }

            //    //Viral Emitters
            //    if (FOs[i] is ViralEmitter)
            //    {
            //        this.FOs.Add((ViralEmitter)FOs[i]);
            //        this.ViralEmitters.Add((ViralEmitter)FOs[i]);
            //        this.ViralEmitters[i].ID = FOs[i].Name + i.ToString();

            //        var dict = new ViralEmitterInternalDict(this.ViralEmitters[i], this.PointInsideDomain);
            //        dict.Export(WorkingDir);

            //        AllFunctionObjectInternalDicts.Add(dict);
            //        //ViralID.Add(dict);

            //    }
            //}


            // TopoSetDict - pass a list of Function Objects with updated IDs.

            var topoSetDict = new TopoSetDict(ViralEmitters,CO2Emitters,VolumetricHeatSources,MomentumSinks,MomentumSources, PointInsideDomain);
            topoSetDict.Export(WorkingDir);

            // System

            var controlDict = new ControlDict(this);
            controlDict.Export(WorkingDir);

            var blockMeshDict = new BlockMeshDict(this.CellSize, BoundingBox);
            blockMeshDict.Export(WorkingDir);

            var snappyHextMeshDict = new SnappyHexMeshDict(this.CellSize, this.PointInsideDomain, BoundingBox, Inlets, Outlets, RoomGeometry);
            snappyHextMeshDict.Export(WorkingDir);

            var fvSchemesDict = new FvSchemesDict(this);
            fvSchemesDict.Export(WorkingDir);

            var fvSolutionDict = new FvSolutionDict();
            fvSolutionDict.Export(WorkingDir);

            var residualsDict = new ResidualsDict();
            residualsDict.Export(WorkingDir);

            var surfaceFeatureExtractDict = new SurfaceFeatureExtractDict(Inlets, Outlets, RoomGeometry);
            surfaceFeatureExtractDict.Export(WorkingDir);

            var decomposeParDict = new DecomposeParDict();
            decomposeParDict.Export(WorkingDir);

            //fOs
            var fvOpt = new FvOptions(this.AllFunctionObjectInternalDicts);
            fvOpt.Export(WorkingDir);






            //FOS -     OLD IMPLEMENTATION  - NOT NECESSARY?

            //var momentumSinkDict = new FunctionObjectDict(MSinkID, "MomentumSink");
            //var momentumSourceDict = new FunctionObjectDict(MSourceID, "MomentumSource");
            //var co2EmittersDict = new FunctionObjectDict(CO2ID, "CO2Emitters");
            //var viralEmittersDict = new FunctionObjectDict(ViralID, "ViralEmitters");
            //var volumetricHeatSourceDict = new FunctionObjectDict(VHSID, "VolumetricHeatSource");

            //var momentumSinkDict = new FunctionObjectDict(MSinkID, "momentumSinkDict");






            //AllDictsWrite2File.Add(controlDict);

            //AllDictsWrite2File.Add(blockMeshDict);
            //AllDictsWrite2File.Add(snappyHextMeshDict);
            //AllDictsWrite2File.Add(fvSchemesDict);
            //AllDictsWrite2File.Add(fvSolutionDict);
            //AllDictsWrite2File.Add(residualsDict);
            //AllDictsWrite2File.Add(surfaceFeatureExtractDict);
            //AllDictsWrite2File.Add(decomposeParDict);

            //ExportGeometryAndDicts(Changethislater WorkingDir);

            // FOs
            //AllFOWrite2File.Add(momentumSinkDict);
            //AllFOWrite2File.Add(momentumSourceDict);
            //AllFOWrite2File.Add(co2EmittersDict);
            //AllFOWrite2File.Add(viralEmittersDict);
            //AllFOWrite2File.Add(volumetricHeatSourceDict);

            //ExportFO(WorkingDir);




            //AllFOWrite2File.Add(topoSetDict);


            // AllFOWrite2File.Add(fvOptionsDict);




            //Exoprt Batch Files

            var runMeshBatch = new RunMeshBatch(this);
            runMeshBatch.Export(WorkingDir);

            var runSimBatch = new RunSimBatch(this);
            runSimBatch.Export(WorkingDir);

            var runTopoBatch = new RunTopoBatch(this);
            runTopoBatch.Export(WorkingDir);

            //AllBatsWrite2File.Add(runMeshBatch);
            //AllBatsWrite2File.Add(runSimBatch);
            //AllBatsWrite2File.Add(runTopoBatch);

            //ExportBatch(WorkingDir);
        }

        //OLD IMPLEMENTATION


        //export geomety and dictionaries of 0 folders and STLs
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

            
            foreach (var g in this.FOs)
            {
                EddyLib.STLExport.ExportBinary(stlDir + "\\" + g.ID + ".stl", g.Geometry);
            }
        }

        //export FOs - OLD IMPLEMENTATION -  NOT NECESSARY?

        //public void ExportFO(string workingDir)
        //{
        //    foreach (FunctionObjectDict dict in AllFOWrite2File)
        //    {
        //        dict.Export(workingDir);
        //    }
        //}


        //export batch files
        //public void ExportBatch(string workingDir)
        //{
        //    foreach (GenericBatchFile dict in AllBatsWrite2File)
        //    {
        //        dict.Export(workingDir);
        //    }
        //}

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