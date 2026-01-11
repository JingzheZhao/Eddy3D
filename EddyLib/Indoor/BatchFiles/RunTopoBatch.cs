using System;
using System.Linq;

namespace EddyLib.Indoor.BatchFiles
{
    internal class RunTopoBatch : GenericBatchFile
    {
        public RunTopoBatch(IndoorDomain IndoorDom)
        {
            this.BatchLocation = IndoorDom.WorkingDir;
            this.BatchName = "run_topoSet.bat";
            this.Header = GetHeader();
            //this.RemoveDict();
            //this.Export();

            string[] parts = {
               this.Header, "\n",
               BatchBody()
#if (DEBUG == true)
               ,"\nPAUSE"
#endif
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static string BatchBody()
        {
            return @"
topoSet ";
        }
    }
}