using System;

namespace EddyLib
{
    public static class GH_Strings
    {
        public static class Common
        {
            public const string Domain = "Simulation domain";
            public const string DomainNick = "Dom";
            public const string DomainDesc = "CFD domain from Box Domain or Cylindrical Domain component.";

            public const string WorkingDir = "Working directory";
            public const string WorkingDirNick = "Dir";
            public const string WorkingDirDesc = "Folder for simulation files. Use a simple name (e.g., 'MyProject') to create under %LocalAppData%\\Eddy3D\\Cases on Windows (~/Eddy3D/Cases on macOS), or provide a full path.";

            public const string MeshSettings = "Mesh settings";
            public const string MeshSettingsNick = "MSet";
            public const string MeshSettingsDesc = "Mesh settings object to connect to Simulation component";

            public const string RunSettings = "Run settings";
            public const string RunSettingsNick = "RSet";
            public const string RunSettingsDesc = "Solver configuration object to connect to Wind Simulation component";

            public const string RunMeshing = "Run meshing";
            public const string RunMeshingNick = "RunMsh";
            public const string RunMeshingDesc = "Run Meshing";

            public const string MakeTrees = "Make trees";
            public const string MakeTreesNick = "MakeTrees";
            public const string MakeTreesDesc = "Create Tree Topologies";

            public const string RunSimulation = "Run simulation";
            public const string RunSimulationNick = "RunSim";
            public const string RunSimulationDesc = "Run Simulation";

            public const string Result = "Simulation result";
            public const string ResultNick = "Res";
            public const string ResultDesc = "Eddy simulation result";
        }

        public static class SimpleFoam
        {
            public const string Name = "Wind Simulation";
            public const string Nick = "WindSim";
            public const string Desc = "Steady-State Wind Solver (SimpleFoam)\r\n\r\nExecutes the OpenFOAM 'simpleFoam' solver (Steady-state RANS) to calculate mean wind flow patterns.\r\n\r\nWorkflow:\r\n1. Connect Domain and Settings\r\n2. Run Meshing (snappyHexMesh)\r\n3. Run Simulation (simpleFoam)\r\n\r\n";

            public const string MeshDone = "Mesh Done";
            public const string MeshDoneNick = "MeshDone";

            public const string SimDone = "Sim Done";
            public const string SimDoneNick = "SimDone";

            public const string MeshEta = "Mesh ETA";
            public const string MeshEtaNick = "MeshETA";

            public const string SimEta = "Sim ETA";
            public const string SimEtaNick = "SimETA";
        }

        public static class Simulation
        {
            public const string Name = "Simulation (Legacy)";
            public const string Nick = "Simulation";
            public const string Desc = "Docker-based Wind Solver (Legacy)";
        }

        public static class MeshSettings
        {
            public const string Name = "Mesh Settings";
            public const string Nick = "MSet";
            public const string Desc = "Meshing Parameters\r\n\r\nControls the resolution and quality of the simulation grid (mesh). Adjust cell sizes to balance between simulation accuracy and computation time.\r\n\r\n";

            public const string BldMin = "Building Min Level";
            public const string BldMinNick = "BldMin";
            public const string BldMinDesc = "Minimum refinement level for building surfaces. Higher = finer. Typical: 2-3. Default: 2";

            public const string BldMax = "Building Max Level";
            public const string BldMaxNick = "BldMax";
            public const string BldMaxDesc = "Maximum refinement level for building surfaces. Must be >= min. Default: 2";

            public const string Feature = "Feature Level";
            public const string FeatureNick = "Feat";
            public const string FeatureDesc = "Refinement level for building corners and features. Default: 2";

            public const string BBox = "Bounding Box Level";
            public const string BBoxNick = "BBox";
            public const string BBoxDesc = "Refinement level for region around buildings. 0 = no extra refinement. Default: 0";

            public const string Ground = "Ground Level";
            public const string GroundNick = "Gnd";
            public const string GroundDesc = "Refinement level for ground surface. Default: 2";

            public const string Layers = "Boundary Layers";
            public const string LayersNick = "nLay";
            public const string LayersDesc = "Number of mesh layers near walls. More = better boundary layer resolution. Default: 4";

            public const string Cells = "Cells Between Levels";
            public const string CellsNick = "nCells";
            public const string CellsDesc = "Number of cells between refinement levels. More = smoother transition. Default: 5";

            public const string Mode = "Mesh Mode";
            public const string ModeNick = "Mode";
            public const string ModeDesc = "0: No snapping (fast debug), 1: With snapping (production), 2: With layers (accurate but slow)";

            public const string Preset = "Preset";
            public const string PresetNick = "Preset";
            public const string PresetDesc = "Optional preset. 0: Default, 1: GPT-53 Codex (more robust snappy settings).";
        }

        public static class RunSettings
        {
            public const string Name = "Run Settings";
            public const string Nick = "RSet";
            public const string Desc = "Solver Control\r\n\r\nConfigures the simulation engine, including calculation iterations, convergence criteria, and parallel processing options (CPUs).\r\n\r\n";

            public const string Iterations = "Iterations";
            public const string IterationsNick = "Iter";
            public const string IterationsDesc = "Maximum solver iterations. Higher = more accurate but slower. Typical: 500-2000. Default: 1000";

            public const string WriteInterval = "Write Interval";
            public const string WriteIntervalNick = "Write";
            public const string WriteIntervalDesc = "Save results every N iterations. Lower = more disk space. Typical: 10-50. Default: 20";

            public const string Keep = "Timesteps to Keep";
            public const string KeepNick = "Keep";
            public const string KeepDesc = "Number of saved timesteps to retain on disk. Older saves are deleted. Default: 3";

            public const string Turb = "Turbulence Model";
            public const string TurbNick = "Turb";
            public const string TurbDesc = "RANS turbulence model. k-epsilon is fast and robust for urban flows. k-omega SST is more accurate near walls.";

            public const string Relax = "Relaxation Factors";
            public const string RelaxNick = "Relax";
            public const string RelaxDesc = "Under-relaxation for solver stability. Robust is safer for complex geometry. Default: Optimized";

            public const string Schemes = "Numerical Schemes";
            public const string SchemesNick = "Schemes";
            public const string SchemesDesc = "Discretization schemes for equations. Optimized balances accuracy and stability.";

            public const string PotInit = "Potential Flow Init";
            public const string PotInitNick = "PotInit";
            public const string PotInitDesc = "Initialize with potentialFoam for faster convergence. Recommended for new simulations. Default: false";

            public const string AoA = "Age of Air";
            public const string AoANick = "AoA";
            public const string AoADesc = "Calculate mean age of air (ventilation effectiveness). Must be enabled before running simulation.";

            public const string CPUs = "CPU Cores";
            public const string CPUsNick = "CPUs";
            public const string CPUsDesc = "Parallel processing cores. -1 = Auto (75% of physical cores, not logical threads). More cores = faster but needs more RAM. Default: -1";

            public const string StabilityLimiter = "Stability Limiter";
            public const string StabilityLimiterNick = "Stab";
            public const string StabilityLimiterDesc = "Optional: add conservative OpenFOAM field limiters (k/epsilon/omega and nut) to reduce divergence on poor meshes. Pressure is intentionally not limited by default.";

            public const string Debug = "Debug Diagnostics";
            public const string DebugNick = "Dbg";
            public const string DebugDesc = "Enable additional diagnostic function objects (fieldMinMax and volume averages) in solver logs. Keep off for faster runs.";

            public const string SimpleC = "SIMPLEC";
            public const string SimpleCNick = "SPLC";
            public const string SimpleCDesc = "Use SIMPLEC pressure-velocity coupling (consistent yes). Can reduce iterations for steady runs; turn off if convergence becomes oscillatory.";

            public const string BlueCFD = "BlueCFD Folder";
            public const string BlueCFDNick = "CFDFolder";
            public const string BlueCFDDesc = "Optional: Custom BlueCFD installation folder.";
        }

        public static class ABL
        {
            public const string Name = "ABL Flow";
            public const string Nick = "ABL";
            public const string Desc = "Atmospheric Boundary Layer (ABL) Inlet\r\n\r\nSets up a logarithmic wind profile based on aerodynamic roughness length (z0). Essential for accurate urban wind flow simulation, representing the friction of the upwind terrain.\r\n\r\n";

            public const string WindDirs = "Wind Directions";
            public const string WindDirsNick = "Dir";
            public const string WindDirsDesc = "Wind directions to simulate. Units: degrees (0-359). 0° = North, 90° = East. Use multiple for annual studies.";

            public const string Uref = "Reference Velocity";
            public const string UrefNick = "Uref";
            public const string UrefDesc = "Wind speed at reference height. Units: m/s. Typical urban: 3-8 m/s. Default: 5 m/s";

            public const string Zref = "Reference Height";
            public const string ZrefNick = "Zref";
            public const string ZrefDesc = "Height where velocity is measured (weather station height). Units: m. Standard: 10m. Default: 10m";

            public const string Z0 = "Aerodynamic roughness length";
            public const string Z0Nick = "z0";
            public const string Z0Desc = "Aerodynamic roughness length. Units: m. Examples: 0.01 (open terrain), 0.3 (suburban), 1.0 (urban). Default: 1m";

            public const string Zgnd = "Ground Level";
            public const string ZgndNick = "Zgnd";
            public const string ZgndDesc = "Height of the ground plane in the simulation. Default: 0";

            public const string EPW = "Weather File";
            public const string EPWNick = "EPW";
            public const string EPWDesc = "Optional: EnergyPlus Weather file (.epw) for annual wind analysis.";

            public const string BC = "Boundary Condition";
            public const string BCNick = "BC";
            public const string BCDesc = "Boundary condition object to connect to Domain component.";
        }

        public static class Clean
        {
            public const string Name = "Clean Directories";
            public const string Nick = "Clean";
            public const string Desc = "Project Cleaner\r\n\r\nUtility to remove generated simulation files and free up disk space. Use with caution as it deletes results.\r\n\r\n";

            public const string ResDir = "Result/Directory";
            public const string ResDirNick = "Res";
            public const string ResDirDesc = "Simulation result or working directory path.";

            public const string Mode = "Mode";
            public const string ModeNick = "Mode";
            public const string ModeDesc = "0: Delete everything, 1: Keep mesh, 2: Keep results";

            public const string Run = "Run";
            public const string RunNick = "Run!";
            public const string RunDesc = "Execute the cleaning operation.";
        }

        public static class Cluster
        {
            public const string Name = "Wind Rose Cluster";
            public const string Nick = "Cluster";
            public const string Desc = "Wind Rose Clustering\r\n\r\nGroups wind directions into representative clusters to reduce simulation time. Essential for performing annual wind comfort analysis efficiently without simulating every single direction.\r\n\r\n";

            public const string Directions = "Directions";
            public const string DirectionsNick = "Dir";
            public const string DirectionsDesc = "Wind directions (0-360°) from weather data.";

            public const string Budget = "Budget";
            public const string BudgetNick = "N";
            public const string BudgetDesc = "Target number of wind directions to simulate. Default: 8";

            public const string Centroids = "Centroids";
            public const string CentroidsNick = "Cent";
            public const string CentroidsDesc = "Cluster centroid directions.";

            public const string DistinctCentroids = "Distinct Centroids";
            public const string DistinctCentroidsNick = "Dcent";
            public const string DistinctCentroidsDesc = "Sorted list of unique cluster centroids.";

            public const string Clusters = "Clusters";
            public const string ClustersNick = "Clus";
            public const string ClustersDesc = "Data tree of points in each cluster.";

            public const string Breaks = "Breaks";
            public const string BreaksNick = "Brk";
            public const string BreaksDesc = "Jenks-Fisher breaks for wind directions.";

            public const string Distance = "Total Distance";
            public const string DistanceNick = "Dist";
            public const string DistanceDesc = "Total distance between points and centroids.";
        }

        public static class Templates
        {
            public const string Name = "Select Template";
            public const string Nick = "Select";
            public const string Desc = "Load example Grasshopper definitions for common workflows.\n\nTemplates include wind comfort studies, MRT analysis, and \nindoor airflow simulations.";
            
            public const string InputName = "Additional Folders";
            public const string InputNick = "Dirs";
            public const string InputDesc = "Optional: Additional folder paths or GitHub URLs to search for .gh/.ghx templates.\nExample URL: https://github.com/Startraders/Eddy3D-Templates/tree/main/Indoor";
            
            public const string OutputName = "Template Paths";
            public const string OutputNick = "Paths";
            public const string OutputDesc = "Full paths to discovered template files (.gh/.ghx)";
        }
    }
}
