using EddyLib.Indoor.BatchFiles;
using EddyLib.Indoor.Dicts;
using EddyLib.Indoor.FunctionObjects;
using Newtonsoft.Json;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

namespace EddyLib.Indoor
{
    public class IndoorDomain : OFBaseDomain
    {
        #region Fields

        public BoundingBox BoundingBox;
        public int endTime { get; set; }
        public Point3d[] Edges { get; set; }
        public string WorkingDir;
        public int functionObjectCount { get; set; }

        private readonly double CellSize;
        private Point3d PointInsideDomain;
        private readonly List<IndoorBC> AllGeometry = new List<IndoorBC>();
        private readonly List<GenericDict> AllDictsWrite2File = new List<GenericDict>();
        private readonly List<GenericDict> AllFunctionObjectInternalDicts = new List<GenericDict>();

        public List<FunctionObject> FOs = new List<FunctionObject>();
        public List<VolumetricHeatSource> VolumetricHeatSources = new List<VolumetricHeatSource>();
        public List<MomentumSinkIndoor> MomentumSinks = new List<MomentumSinkIndoor>();
        public List<MomentumSource> MomentumSources = new List<MomentumSource>();
        public List<CO2Emitter> CO2Emitters = new List<CO2Emitter>();
        public List<ViralEmitter> ViralEmitters = new List<ViralEmitter>();

        #endregion

        #region Constructors

        public IndoorDomain()
        {
        }

        public IndoorDomain(int endTime, string WorkingDir, double CellSize, Point3d PointInsideDomain, 
            List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets, 
            List<FunctionObject> FOs, int CPUs)
        {
            this.WorkingDir = WorkingDir;
            this.endTime = endTime;
            this.PointInsideDomain = PointInsideDomain;
            this.CellSize = CellSize;

            // 1. Assign unique IDs to all geometry
            AssignUniqueIds(RoomGeometry, Inlets, Outlets);

            // 2. Compute bounding box
            ComputeBoundingBox(RoomGeometry);

            // 3. Create boundary condition dictionaries
            CreateBCDicts(Inlets, Outlets, RoomGeometry);

            // 4. Create constant dictionaries
            CreateConstantDicts();

            // 5. Process function objects
            ProcessFunctionObjects(FOs);

            // 6. Export geometry and generic dicts
            ExportGeometryAndGenericDicts(WorkingDir);

            // 7. Export system dictionaries
            ExportSystemDicts(WorkingDir, RoomGeometry, Inlets, Outlets, CPUs);

            // 8. Export batch files
            ExportBatchFiles(WorkingDir, CPUs);
        }

        #endregion

        #region Constructor Helpers

        private void AssignUniqueIds(List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets)
        {
            int cnt = 0;

            foreach (var item in RoomGeometry)
            {
                item.Id = item.Name + cnt;
                AllGeometry.Add(item);
                cnt++;
            }
            foreach (var item in Inlets)
            {
                item.Id = item.Name + cnt;
                AllGeometry.Add(item);
                cnt++;
            }
            foreach (var item in Outlets)
            {
                item.Id = item.Name + cnt;
                AllGeometry.Add(item);
                cnt++;
            }
        }

        private void ComputeBoundingBox(List<IndoorBC.Wall> RoomGeometry)
        {
            var b = GetBoundingBox(RoomGeometry);
            var x = Transform.Scale(b.Center, 1.2);
            b.Transform(x);
            this.BoundingBox = b;
            this.Edges = this.BoundingBox.GetCorners();
        }

        private void CreateBCDicts(List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets, List<IndoorBC.Wall> RoomGeometry)
        {
            AllDictsWrite2File.Add(new IndoorBCDict.U(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.alphat(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.AoA(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.Covid(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.nut(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.omega(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.k(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.p(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.p_rgh(Inlets, Outlets, RoomGeometry));
            AllDictsWrite2File.Add(new IndoorBCDict.T(Inlets, Outlets, RoomGeometry));
        }

        private void CreateConstantDicts()
        {
            AllDictsWrite2File.Add(new GDict());
            AllDictsWrite2File.Add(new ThermoPhysicalPropertiesDict());
            AllDictsWrite2File.Add(new TurbulencePropertiesDict());
        }

        private void ProcessFunctionObjects(List<FunctionObject> FOs)
        {
            this.functionObjectCount = FOs.Count;
            int ctVHS = 0, ctMsi = 0, ctMso = 0, ctCo2 = 0, ctVir = 0;

            for (int i = 0; i < FOs.Count; i++)
            {
                var fo = FOs[i];
                string foId = fo.Name + "_" + i.ToString();

                switch (fo)
                {
                    case VolumetricHeatSource vhs:
                        this.FOs.Add(vhs);
                        this.VolumetricHeatSources.Add(vhs);
                        vhs.ID = foId;
                        AllFunctionObjectInternalDicts.Add(new VolumetricHeatSourceInternalDict(this.VolumetricHeatSources, this.PointInsideDomain));
                        ctVHS++;
                        break;

                    case MomentumSinkIndoor msi:
                        this.FOs.Add(msi);
                        this.MomentumSinks.Add(msi);
                        msi.ID = foId;
                        AllFunctionObjectInternalDicts.Add(new MomentumSinkIndoorInternalDict(this.MomentumSinks, this.PointInsideDomain));
                        ctMsi++;
                        break;

                    case MomentumSource mso:
                        this.FOs.Add(mso);
                        this.MomentumSources.Add(mso);
                        mso.ID = foId;
                        AllFunctionObjectInternalDicts.Add(new MomentumSourceInternalDict(this.MomentumSources, this.PointInsideDomain));
                        ctMso++;
                        break;

                    case CO2Emitter co2:
                        this.FOs.Add(co2);
                        this.CO2Emitters.Add(co2);
                        co2.ID = foId;
                        AllFunctionObjectInternalDicts.Add(new CO2EmitterInternalDict(this.CO2Emitters, this.PointInsideDomain));
                        ctCo2++;
                        break;

                    case ViralEmitter vir:
                        this.FOs.Add(vir);
                        this.ViralEmitters.Add(vir);
                        vir.ID = foId;
                        AllFunctionObjectInternalDicts.Add(new ViralEmitterInternalDict(this.ViralEmitters, this.PointInsideDomain));
                        ctVir++;
                        break;
                }
            }
        }

        private void ExportSystemDicts(string workingDir, List<IndoorBC.Wall> RoomGeometry, 
            List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets, int CPUs)
        {
            // TopoSetDict
            var topoSetDict = new TopoSetDict(ViralEmitters, CO2Emitters, VolumetricHeatSources, MomentumSinks, MomentumSources, PointInsideDomain);
            topoSetDict.Export(workingDir);

            // System dictionaries
            new ControlDict(this).Export(workingDir);
            new BlockMeshDict(this.CellSize, BoundingBox).Export(workingDir);
            new SnappyHexMeshDict(this.CellSize, this.PointInsideDomain, BoundingBox, Inlets, Outlets, RoomGeometry).Export(workingDir);
            new FvSchemesDict(this).Export(workingDir);
            new FvSolutionDict().Export(workingDir);
            new ResidualsDict().Export(workingDir);
            new SurfaceFeatureDict(Inlets, Outlets, RoomGeometry).Export(workingDir);
            new DecomposeParDict(CPUs).Export(workingDir);

            // Function Objects
            new FvOptions(this.AllFunctionObjectInternalDicts).Export(workingDir);
        }

        private void ExportBatchFiles(string workingDir, int CPUs)
        {
            new RunMeshBatch(this, CPUs).Export(workingDir);
            new RunSimBatch(this, CPUs).Export(workingDir);
            new RunTopoBatch(this).Export(workingDir);
            new RunAllBatch(this, CPUs).Export(workingDir);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Exports geometry as STL and all generic dictionaries.
        /// </summary>
        public void ExportGeometryAndGenericDicts(string workingDir)
        {
            // Export dictionaries
            foreach (GenericDict dict in AllDictsWrite2File)
            {
                dict.Export(workingDir);
            }

            // Export geometry as STL
            var stlDir = Path.Combine(workingDir, "constant", "triSurface");
            Directory.CreateDirectory(stlDir);

            foreach (var geo in AllGeometry)
            {
                STLExport.ExportBinary(Path.Combine(stlDir, geo.Id + ".stl"), geo.Geometry);
            }

            foreach (var g in this.FOs)
            {
                STLExport.ExportBinary(Path.Combine(stlDir, g.ID + ".stl"), g.Geometry);
            }
        }

        public IndoorDomain Duplicate()
        {
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<IndoorDomain>(json);
        }

        #endregion

        #region Private Methods

        private BoundingBox GetBoundingBox(List<IndoorBC.Wall> RoomGeometry)
        {
            Mesh RG = new Mesh();

            foreach (IndoorBC m in RoomGeometry)
            {
                if (m?.Geometry != null)
                {
                    RG.Append(m.Geometry);
                }
            }

            return RG.GetBoundingBox(false);
        }

        #endregion
    }
}