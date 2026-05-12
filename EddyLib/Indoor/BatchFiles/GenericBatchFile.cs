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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"call ""{DefaultDirectoriesAndPaths.BlueCfdSetvarsBat}""");
            sb.AppendLine(ReturnWindowsDrive(BatchLocation.ToString()));
            sb.AppendLine("cd " + "\"" + BatchLocation.ToString() + "\"");

            return sb.ToString();
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
