namespace EddyLib
{
    public class OFResult
    {
        public readonly OFBaseDomain Domain;

        public readonly OFRunSettings RunSettings;

        public readonly OFMeshSettings MeshSettings;

        public readonly string WorkingDirectory;

        public OFResult(OFBaseDomain Domain, OFRunSettings RunSettings, OFMeshSettings MeshSettings, string workDir)
        {
            this.Domain = Domain;
            this.RunSettings = RunSettings;
            this.MeshSettings = MeshSettings;
            this.WorkingDirectory = workDir;
        }
    }
}