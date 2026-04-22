namespace EddyLib
{
    public class OFResult
    {
        public readonly OFBaseDomain Domain;

        public readonly OFRunSettings RunSettings;

        public readonly OFMeshSettings MeshSettings;

        public readonly string WorkingDirectory;

        /// <summary>
        /// Optional engine-specific case directory (for example FluidX3D source/case root).
        /// Empty for legacy OpenFOAM-only runs.
        /// </summary>
        public string EngineCaseDirectory { get; set; } = string.Empty;

        public OFResult(OFBaseDomain Domain, OFRunSettings RunSettings, OFMeshSettings MeshSettings, string workDir)
        {
            this.Domain = Domain;
            this.RunSettings = RunSettings;
            this.MeshSettings = MeshSettings;
            this.WorkingDirectory = workDir;
        }
    }
}
