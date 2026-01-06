using System.IO;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Manages OpenFOAM directory paths for mesh and simulation cases.
    /// Consolidates path management and directory creation.
    /// </summary>
    public static partial class OpenFOAMPaths
    {
    }

    /// <summary>
    /// Paths for OpenFOAM mesh generation.
    /// </summary>
    public sealed class MeshPaths
    {
        private readonly OFMeshSettings _settings;

        /// <summary>
        /// Creates mesh paths from settings.
        /// </summary>
        public MeshPaths(OFMeshSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Working directory for mesh.</summary>
        public string WorkingDir => _settings.meshWorkingDir;

        /// <summary>Base working directory.</summary>
        public string BaseWorkingDir => _settings.baseWorkingDir;

        /// <summary>STL files directory.</summary>
        public string StlDir => _settings.meshStlDir;

        /// <summary>System dictionary directory.</summary>
        public string SystemDir => _settings.meshSystemDir;

        /// <summary>Constant directory.</summary>
        public string ConstantDir => _settings.meshConstantDir;

        /// <summary>Boundary conditions directory.</summary>
        public string BoundaryDir => _settings.meshBoundaryConditionsDirectory;

        /// <summary>Buildings STL filename.</summary>
        public string BuildingsStl => _settings.meshStlFilenameBuildings;

        /// <summary>Ground STL filename.</summary>
        public string GroundStl => _settings.meshStlFilenameGround;

        /// <summary>Ground perimeter STL filename.</summary>
        public string GroundPerimStl => _settings.meshStlFilenameGroundPerim;

        /// <summary>
        /// Ensures all mesh directories exist.
        /// </summary>
        public void EnsureDirectories()
        {
            Directory.CreateDirectory(WorkingDir);
            Directory.CreateDirectory(StlDir);
            Directory.CreateDirectory(SystemDir);
            Directory.CreateDirectory(ConstantDir);
            Directory.CreateDirectory(BoundaryDir);
        }
    }

    /// <summary>
    /// Paths for an OpenFOAM simulation case (per wind direction).
    /// </summary>
    public sealed class CasePaths
    {
        /// <summary>
        /// Creates case paths for a specific wind direction.
        /// </summary>
        public CasePaths(string workDir, int windDir)
        {
            WindDir = windDir;
            CaseDir = Path.Combine(workDir, windDir.ToString());
            SystemDir = Path.Combine(CaseDir, "system");
            ConstantDir = Path.Combine(CaseDir, "constant");
            TriSurfaceDir = Path.Combine(ConstantDir, "triSurface");
            BoundaryDir = Path.Combine(CaseDir, "0.org");
            BoundaryDirTemp = Path.Combine(CaseDir, "0");
            PolyMeshLink = Path.Combine(ConstantDir, "polyMesh");
        }

        /// <summary>Wind direction (degrees).</summary>
        public int WindDir { get; }

        /// <summary>Case directory root.</summary>
        public string CaseDir { get; }

        /// <summary>System dictionary directory.</summary>
        public string SystemDir { get; }

        /// <summary>Constant files directory.</summary>
        public string ConstantDir { get; }

        /// <summary>TriSurface directory for STL files.</summary>
        public string TriSurfaceDir { get; }

        /// <summary>Boundary conditions directory (0.org).</summary>
        public string BoundaryDir { get; }

        /// <summary>Temporary boundary directory (0).</summary>
        public string BoundaryDirTemp { get; }

        /// <summary>PolyMesh symlink location.</summary>
        public string PolyMeshLink { get; }

        /// <summary>
        /// Ensures all case directories exist.
        /// </summary>
        public void EnsureDirectories()
        {
            Directory.CreateDirectory(CaseDir);
            Directory.CreateDirectory(SystemDir);
            Directory.CreateDirectory(ConstantDir);
            Directory.CreateDirectory(TriSurfaceDir);
            Directory.CreateDirectory(BoundaryDir);
            Directory.CreateDirectory(BoundaryDirTemp);
        }
    }
}
