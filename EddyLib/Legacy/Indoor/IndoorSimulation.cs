namespace EddyLib.Indoor
{
    public class IndoorResult
    {
        public readonly IndoorDomain Domain;

        public readonly OFRunSettings RunSettings;

        public readonly string WorkingDirectory;

        public IndoorResult()
        {
        }

        public IndoorResult(IndoorDomain Domain, OFRunSettings RunSettings, string workDir)
        {
            this.Domain = Domain;
            this.RunSettings = RunSettings;

            this.WorkingDirectory = workDir;
        }
    }
}