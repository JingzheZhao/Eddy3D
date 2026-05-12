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
            public const string WorkingDirDesc = "Folder for simulation files. Use a simple name (e.g., 'MyProject') to create under %USERPROFILE%\\Eddy3D\\Cases on Windows (~/Eddy3D/Cases on macOS), or provide a full path.";

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
            public const string Desc = "Steady-State Wind Solver (OpenFOAM 12)\r\n\r\nExecutes OpenFOAM 'foamRun -solver incompressibleFluid' to calculate mean wind flow patterns.\r\n\r\nWorkflow:\r\n1. Connect Domain and Settings\r\n2. Run Meshing (snappyHexMesh)\r\n3. Run Simulation (foamRun)\r\n\r\n";

            public const string MeshDone = "Mesh Done";
            public const string MeshDoneNick = "MeshDone";

            public const string SimDone = "Sim Done";
            public const string SimDoneNick = "SimDone";

            public const string MeshRemainingTime = "Mesh Remaining Time";
            public const string MeshRemainingTimeNick = "MeshRemain";

            public const string SimRemainingTime = "Simulation Remaining Time";
            public const string SimRemainingTimeNick = "SimRemain";

            // Backward-compatible aliases used by older component code.
            public const string MeshEta = MeshRemainingTime;
            public const string MeshEtaNick = MeshRemainingTimeNick;
            public const string SimEta = SimRemainingTime;
            public const string SimEtaNick = SimRemainingTimeNick;
        }

        public static class FluidX3D
        {
           // public const string Name = "FluidX3D ABL (Experimental)";
           // public const string Nick = "FluidX3D";
          //  public const string Desc = "Deprecated standalone FluidX3D workflow.\r\n\r\nUse 'Wind Simulation' and select the FluidX3D engine from the component menu.\r\n\r\n";

            public const string SourceDir = "FluidX3D Source";
            public const string SourceDirNick = "Src";
            public const string SourceDirDesc = "Path to a local FluidX3D source folder (contains src/setup.cpp and src/defines.hpp).";

            public const string Buildings = "Building Geometry";
            public const string BuildingsNick = "Bldg";
            public const string BuildingsDesc = "Optional: Mesh/Brep/Surface/Extrusion geometry to export as binary STL and voxelize in FluidX3D.";

            public const string WorkingDir = "Working Directory";
            public const string WorkingDirNick = "Dir";
            public const string WorkingDirDesc = "Case folder root. If you pass a simple name, it is created under Eddy3D Cases.";

            public const string CloneUpdate = "Clone/Update Source";
            public const string CloneUpdateNick = "Git";
            public const string CloneUpdateDesc = "If true, automatically clone FluidX3D (if missing) and pull latest changes (if git repo exists).";

            public const string MemoryMb = "VRAM Budget (MB)";
            public const string MemoryMbNick = "MemMB";
            public const string MemoryMbDesc = "Approximate GPU memory target for FluidX3D resolution selection.";

            public const string Uref = "Reference Velocity";
            public const string UrefNick = "Uref";
            public const string UrefDesc = "Wind speed at zRef [m/s].";

            public const string Zref = "Reference Height";
            public const string ZrefNick = "Zref";
            public const string ZrefDesc = "Reference height for Uref [m].";

            public const string Z0 = "Roughness Length";
            public const string Z0Nick = "z0";
            public const string Z0Desc = "Aerodynamic roughness length [m] for log-law profile.";

            public const string SimTime = "Simulation Time (s)";
            public const string SimTimeNick = "T";
            public const string SimTimeDesc = "Physical simulation time in seconds.";

            public const string ExportEvery = "Export Every (s)";
            public const string ExportEveryNick = "dT";
            public const string ExportEveryDesc = "Physical export interval in seconds.";

            public const string GroundZ = "Ground Elevation";
            public const string GroundZNick = "Zgnd";
            public const string GroundZDesc = "Ground elevation in model units (assumed meters). Domain bottom is adjusted to include geometry below this level.";

            public const string MeshEdge = "Meshing Edge Length";
            public const string MeshEdgeNick = "Edge";
            public const string MeshEdgeDesc = "Optional max/min edge length for meshing Breps/Surfaces before STL export. 0 uses Rhino defaults.";

            public const string Prepare = "Prepare";
            public const string PrepareNick = "Prep";
            public const string PrepareDesc = "Write/update generated FluidX3D setup and launch scripts.";

            public const string Run = "Run";
            public const string RunNick = "Run";
            public const string RunDesc = "Launch the generated platform script in a terminal.";

            public const string CaseDir = "Case Directory";
            public const string CaseDirNick = "Case";
            public const string CaseDirDesc = "Prepared FluidX3D working directory.";

            public const string LaunchScript = "Launch Script";
            public const string LaunchScriptNick = "Script";
            public const string LaunchScriptDesc = "Platform launch script path (.command on macOS/Linux, .bat on Windows).";

            public const string ExportDir = "Export Directory";
            public const string ExportDirNick = "Export";
            public const string ExportDirDesc = "FluidX3D export directory (VTK output).";

            public const string Status = "Status";
            public const string StatusNick = "Info";
            public const string StatusDesc = "Workflow status and instructions.";
        }

        public static class FluidX3DRunSettings
        {
            public const string Name = "FluidX3D Run Settings";
            public const string Nick = "FxSet";
            public const string Desc = "FluidX3D solver controls for the integrated Wind Simulation component.\r\n\r\nUse this only when engine = FluidX3D.\r\n\r\n";

            public const string SourceDir = "FluidX3D Source (Optional)";
            public const string SourceDirNick = "Src";
            public const string SourceDirDesc = "Optional override for FluidX3D source folder. Leave empty to use EDDY_FLUIDX3D_SOURCE or the Eddy engines install path.";

            public const string MemoryMb = "VRAM Budget (MB)";
            public const string MemoryMbNick = "MemMB";
            public const string MemoryMbDesc = "Approximate GPU memory budget used for FluidX3D resolution selection.";

            public const string SimTime = "Simulation Time (s)";
            public const string SimTimeNick = "T";
            public const string SimTimeDesc = "Physical simulation time in seconds.";

            public const string ExportEvery = "Export Every (s)";
            public const string ExportEveryNick = "dT";
            public const string ExportEveryDesc = "Physical export interval in seconds.";

            public const string GroundZ = "Ground Elevation";
            public const string GroundZNick = "Zgnd";
            public const string GroundZDesc = "Ground elevation in model coordinates (m).";

            public const string Output = "Run settings";
            public const string OutputNick = "RSet";
            public const string OutputDesc = "FluidX3D run settings object for the Wind Simulation component.";
        }

        public static class FluidX3DProbe
        {
            public const string Name = "FluidX3D Probe (VTK)";
            public const string Nick = "FxProbe";
            public const string Desc = "Probe FluidX3D VTK exports at Rhino points with physical-time selection and optional time averaging.\r\n\r\nSupports velocity U (vector) and density rho (scalar).\r\n\r\n";

            public const string Result = "Simulation Result";
            public const string ResultNick = "Res";
            public const string ResultDesc = "OFResult from Wind Simulation. Probe path is resolved from this simulation result.";

            public const string CaseDir = "Case Directory";
            public const string CaseDirNick = "Case";
            public const string CaseDirDesc = "FluidX3D case directory (contains bin/export, VTK, or vtk output folder).";

            public const string Points = "Probe Points";
            public const string PointsNick = "Pts";
            public const string PointsDesc = "Probe points in Rhino model coordinates (meters).";

            public const string Quantity = "Q field";
            public const string QuantityNick = "Q";
            public const string QuantityDesc = "Field to probe: U (velocity vector) or rho (density scalar).";

            public const string TimeMode = "Time Mode";
            public const string TimeModeNick = "Mode";
            public const string TimeModeDesc = "0 Latest, 1 Closest physical time, 2 Average over [T0, T1].";

            public const string TargetTime = "Target Time (s)";
            public const string TargetTimeNick = "T";
            public const string TargetTimeDesc = "Used when Time Mode = Closest physical time.";

            public const string TimeWindow = "Time Interval (s)";
            public const string TimeWindowNick = "T0/T1";
            public const string TimeWindowDesc = "Averaging interval as Domain/Interval (Construct Domain).";

            public const string Run = "Run";
            public const string RunNick = "Run";
            public const string RunDesc = "Execute probing.";

            public const string PointsOut = "Probe Points";
            public const string PointsOutNick = "Pts";
            public const string PointsOutDesc = "Echo of input probe points.";

            public const string VelocityByTime = "U by Time";
            public const string VelocityByTimeNick = "U_t";
            public const string VelocityByTimeDesc = "DataTree of velocity vectors per sampled timestep (one branch per timestep).";

            public const string DensityByTime = "rho by Time";
            public const string DensityByTimeNick = "rho_t";
            public const string DensityByTimeDesc = "DataTree of density values per sampled timestep (one branch per timestep).";

            public const string VelocityAverage = "U Average";
            public const string VelocityAverageNick = "Uavg";
            public const string VelocityAverageDesc = "Time-averaged velocity vectors at probe points.";

            public const string DensityAverage = "rho Average";
            public const string DensityAverageNick = "rhoAvg";
            public const string DensityAverageDesc = "Time-averaged density values at probe points.";

            public const string SampledTimes = "Sampled Times";
            public const string SampledTimesNick = "t_s";
            public const string SampledTimesDesc = "Physical time of each sampled VTK file in seconds.";

            public const string SampledSteps = "Sampled Steps";
            public const string SampledStepsNick = "Step";
            public const string SampledStepsDesc = "LBM timestep id parsed from sampled filenames.";

            public const string SampledFiles = "Sampled Files";
            public const string SampledFilesNick = "Files";
            public const string SampledFilesDesc = "Sampled VTK file paths.";

            public const string OutsideCount = "Outside Count";
            public const string OutsideCountNick = "Out";
            public const string OutsideCountDesc = "Number of points outside the domain (clamped to nearest cell).";

            public const string Status = "Status";
            public const string StatusNick = "Info";
            public const string StatusDesc = "Probe execution status.";
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
            public const string ModeDesc = "Meshing strategy:\n0: No snapping (Fast debug)\n1: With snapping (Standard production)\n2: With layers (Accurate boundary layers, slower)";

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
            public const string TurbDesc = "RANS turbulence model selection.\n- k-epsilon: Fast & robust (Standard for urban flows)\n- RNG k-epsilon: Default, improved for swirling flows\n- k-omega SST: More accurate for wall-bounded flows";

            public const string Relax = "Relaxation Factors";
            public const string RelaxNick = "Relax";
            public const string RelaxDesc = "Under-relaxation factors control solver stability.\n- Fast: Aggressive settings (may diverge)\n- Robust: Stable settings for complex geometry\n- Optimized: Default balanced settings";

            public const string Schemes = "Numerical Schemes";
            public const string SchemesNick = "Schemes";
            public const string SchemesDesc = "Numerical discretization schemes.\n- Default: Standard OpenFOAM schemes\n- Optimized: Enhances stability for urban flows (Recommended)";

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
            public const string StabilityLimiterDesc = "Optional: add conservative OpenFOAM field limiters (k/epsilon/omega and nut) to reduce divergence on poor meshes. Pressure is intentionally not limited by default. Default: true";

            public const string Debug = "Debug Diagnostics";
            public const string DebugNick = "Dbg";
            public const string DebugDesc = "Enable additional diagnostic function objects (field min/max magnitude and volume averages) in solver logs. Keep off for faster runs.";

            public const string SimpleC = "SIMPLEC";
            public const string SimpleCNick = "SPLC";
            public const string SimpleCDesc = "Use SIMPLEC pressure-velocity coupling (consistent yes). Can reduce iterations for steady runs; turn off if convergence becomes oscillatory.";

            public const string BlueCFD = "blueCFD-Core 2024 Folder";
            public const string BlueCFDNick = "CFDFolder";
            public const string BlueCFDDesc = "Optional: Custom blueCFD-Core 2024 installation folder. Defaults to the detected blueCFD-Core 2024 install.";
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

        public static class GanPredict
        {
            public const string Name = "GAN Wind Prediction";
            public const string Nick = "GANPredict";
            public const string Desc = "Predict pedestrian-level wind speeds using a GAN surrogate model.\n\n"
                + "Generates a normalised input array from building geometry and sends it "
                + "to the Eddy3D cloud API for real-time inference.\n\n";

            public const string Building = "Building Geometry";
            public const string BuildingNick = "Bldg";
            public const string BuildingDesc = "Joined mesh of all buildings to include in the prediction.";

            public const string AnalysisPlane = "Analysis Plane";
            public const string AnalysisPlaneNick = "Plane";
            public const string AnalysisPlaneDesc = "Square Rectangle3d defining the analysis area. Must be square.";

            public const string WindDir = "Wind Direction";
            public const string WindDirNick = "WDir";
            public const string WindDirDesc = "Wind direction in degrees clockwise from north (0 = north).";

            public const string Run = "Run";
            public const string RunNick = "Run";
            public const string RunDesc = "Set to true to trigger the prediction.";

            public const string ApiUrl = "API URL";
            public const string ApiUrlNick = "URL";
            public const string ApiUrlDesc = "GAN API endpoint URL. Uses default Eddy3D cloud API if empty.";

            public const string VSize = "Height Scale";
            public const string VSizeNick = "vSize";
            public const string VSizeDesc = "Height coloring scale factor for the input image.";

            public const string ColorSize = "Distance Scale";
            public const string ColorSizeNick = "cSize";
            public const string ColorSizeDesc = "Distance coloring scale factor for the input image.";

            public const string ColorMap = "Color Map";
            public const string ColorMapNick = "CMap";
            public const string ColorMapDesc = "Result mesh color map. Supported values: Viridis, Turbo, Inferno.";

            public const string WindSpeed = "Wind Speed";
            public const string WindSpeedNick = "UMag";
            public const string WindSpeedDesc = "Predicted wind speed magnitude at each pixel (m/s).";

            public const string ResultMesh = "Result Mesh";
            public const string ResultMeshNick = "Mesh";
            public const string ResultMeshDesc = "Coloured mesh showing predicted wind speed distribution, previewed on a horizontal plane at z = 2.0 m.";
        }
    }
}
