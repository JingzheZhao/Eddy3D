using System.IO;

namespace EddyLib.Docker
{
    /// <summary>
    /// Docker configuration constants for Eddy3D's OpenFOAM runtime.
    /// Ported from OutdoorPlus MetaFOAMLib/UmcfInstallConfig.cs.
    /// </summary>
    public static class DockerConfig
    {
        /// <summary>Docker image with OpenFOAM 8 and urbanMicroclimateFoam.</summary>
        public const string ImageName = "pkastner/openfoam:8-umcf-4856041";

        /// <summary>Named container for interactive runs.</summary>
        public const string ContainerName = "eddy3d-runner";

        /// <summary>Path to OpenFOAM 8 bashrc inside the container.</summary>
        public const string OpenFoamBashrc = "/home/openfoam/OpenFOAM-8/etc/bashrc";

        /// <summary>Path to MPI binaries inside the container.</summary>
        public const string MpiPath = "/opt/amazon/openmpi/bin";

        /// <summary>Container-side mount point for the case directory.</summary>
        public const string CaseMountPoint = "/case";

        /// <summary>Platform flag for multi-arch images (required for Apple Silicon).</summary>
        public const string Platform = "linux/amd64";

        /// <summary>
        /// Common Docker-related paths on macOS. GUI apps like Rhino don't inherit
        /// the shell PATH, so these must be checked explicitly.
        /// </summary>
        public static readonly string[] MacDockerPaths = new[]
        {
            "/usr/local/bin",
            "/opt/homebrew/bin",
            "/Applications/Docker.app/Contents/Resources/bin",
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".docker", "bin")
        };
    }
}
