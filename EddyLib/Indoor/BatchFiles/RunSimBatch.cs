using System;
using System.Linq;

namespace EddyLib.Indoor.BatchFiles
{
    public class RunSimBatch : GenericBatchFile
    {
        public RunSimBatch(IndoorDomain IndoorDom)
        {
            this.BatchLocation = IndoorDom.WorkingDir;
            this.BatchName = "run_sim.bat";
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

        renumberMesh -overwrite
        decomposePar -force
        mpiexec -np 8 buoyantSimpleFoam -parallel
        reconstructPar ";
        }
    }
}