using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.BatchFiles
{
    class RunTopoBatch:GenericBatchFile
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
               String.Join("\n", BatchBody()
#if (DEBUG == true)
               ,"\nPAUSE"
#endif
               )
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
