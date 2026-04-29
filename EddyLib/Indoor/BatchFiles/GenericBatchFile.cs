using System.IO;
using System.Text;

namespace EddyLib.Indoor.BatchFiles
{
    public class GenericBatchFile
    {
        public string Header { get; set; }

        //public string Body { get; set; }

        public string BatchName { get; set; }

        //public string BatchLocation { get; set; }

        public string BatchLocation = "\0";

        public string FullDictString;

        public void Export(string baseWorkingDir)
        {
            var path = Path.Combine(baseWorkingDir, BatchLocation);
            Directory.CreateDirectory(path);
            //if (!path.EndsWith("\\")) path += "\\";
            File.WriteAllText(Path.Combine(path, this.BatchName), this.FullDictString);
        }

        public string GetHeader()
        {
            string blueCfdRoot = DefaultDirectoriesAndPaths.BlueCfdDir.TrimEnd('\\', '/');
            string msysUsrBin = Path.Combine(blueCfdRoot, "msys64", "usr", "bin");
            string mpiBin = ResolveBlueCfdMpiBin(blueCfdRoot);
            string pstreamLib = ResolveBlueCfdPstreamLibBin(blueCfdRoot);
            string thirdPartyMpiLib = ResolveBlueCfdThirdPartyMpiLibBin(blueCfdRoot);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"call ""{DefaultDirectoriesAndPaths.BlueCfdSetvarsBat}""");
            sb.AppendLine($@"set ""PATH={msysUsrBin};{mpiBin};{pstreamLib};{thirdPartyMpiLib};%PATH%""");
            sb.AppendLine(ReturnWindowsDrive(BatchLocation.ToString()));
            sb.AppendLine("cd " + "\"" + BatchLocation.ToString() + "\"");

            return sb.ToString();
        }

        private static string ResolveBlueCfdMpiBin(string blueCfdRoot)
        {
            var candidates = new[]
            {
                Path.Combine(blueCfdRoot, "ThirdParty-12", "platforms", "mingw_w64Gcc122", "MS-MPI-10.1.2", "bin"),
                Path.Combine(blueCfdRoot, "ThirdParty-12", "platforms", "mingw_w64Gcc122", "MS-MPI-10.1.2", "PFiles", "Microsoft MPI", "Bin")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private static string ResolveBlueCfdPstreamLibBin(string blueCfdRoot)
        {
            var candidates = new[]
            {
                Path.Combine(blueCfdRoot, "OpenFOAM-12", "platforms", "mingw_w64Gcc122DPInt32Opt", "lib", "MS-MPI-10.1.2"),
                Path.Combine(blueCfdRoot, "OpenFOAM-12", "platforms", "mingw_w64Gcc122DPInt32Opt", "lib", "MS-MPI-10.1")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private static string ResolveBlueCfdThirdPartyMpiLibBin(string blueCfdRoot)
        {
            var candidates = new[]
            {
                Path.Combine(blueCfdRoot, "ThirdParty-12", "platforms", "mingw_w64Gcc122DPInt32", "lib", "MS-MPI-10.1.2"),
                Path.Combine(blueCfdRoot, "ThirdParty-12", "platforms", "mingw_w64Gcc122DPInt32", "lib", "MS-MPI-10.1")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        public string ReturnWindowsDrive(string path)
        {
            var result = path[0];
            return result + ":";
        }

        public void RemoveBatch(string baseWorkingDir)
        {
            string path = Path.Combine(baseWorkingDir, this.BatchName);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
