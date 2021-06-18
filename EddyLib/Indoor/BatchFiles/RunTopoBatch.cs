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
               String.Join("\n", BatchBody(),"\n","PAUSE")
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