using System;
using System.Linq;

namespace EddyLib.Indoor.BatchFiles
{
    public class RunSimBatch : GenericBatchFile
    {
        public RunSimBatch(IndoorDomain IndoorDom, int CPUs)
        {
            this.BatchLocation = IndoorDom.WorkingDir;
            this.BatchName = "run_sim.bat";
            this.Header = GetHeader();
            //this.RemoveDict();
            //this.Export();

            string[] parts = {
               this.Header, "\n",
               String.Join("\n", BatchBody(CPUs)
#if (DEBUG == true)
               ,"\nPAUSE"
#endif
               )
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static string BatchBody(int cpus)
        {
            return $@"renumberMesh -overwrite
decomposePar -force
mpiexec -np {cpus} buoyantSimpleFoam -parallel
reconstructPar ";
        }
    }
}