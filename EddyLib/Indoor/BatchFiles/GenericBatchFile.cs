using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.BatchFiles
{
    public class GenericBatchFile
    {
        public string Header { get; set; }

        //public string Body { get; set; }

        public string BatchName { get; set; }

        public string BatchLocation { get; set; }

        public string FullDictString;

        public void Export()
        {
            var path = Path.GetDirectoryName(this.BatchLocation);
            Directory.CreateDirectory(path);
            //if (!path.EndsWith("\\")) path += "\\";
            File.WriteAllText(path + this.BatchName, this.FullDictString);
        }

        public string GetHeader()
        {
            return
                   @"call ""C:\Program Files\blueCFD-Core-2017\\setvars.bat""
                    //set PATH=%HOME%msys64\usr\bin;%PATH%
                    //cd " + BatchLocation.ToString();
        }

        public void RemoveDict()
        {
            string path = this.BatchLocation + "\\" + this.BatchName;

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
