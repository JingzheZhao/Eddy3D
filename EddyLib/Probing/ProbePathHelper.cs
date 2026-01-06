using System.IO;

namespace EddyLib
{
    internal static class ProbePathHelper
    {
        internal static string BuildProbeFilePath(string workingDirectory, string probeName, string fieldName, int iteration)
        {
            return Path.Combine(workingDirectory, "postProcessing", probeName, iteration.ToString(), fieldName);
        }

        internal static string EnsurePostProcessingDir(string baseWorkingDirectory)
        {
            var postProcessDir = Path.Combine(baseWorkingDirectory, "postProcessing");
            if (!Directory.Exists(postProcessDir))
            {
                Directory.CreateDirectory(postProcessDir);
            }

            return postProcessDir;
        }

        internal static string BuildProbeBinaryPath(string postProcessDir, string windDir, string probeName, string fieldName)
        {
            return Path.Combine(postProcessDir, windDir + "_" + probeName + "_" + fieldName + ".bin");
        }
    }
}
