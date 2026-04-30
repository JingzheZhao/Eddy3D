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
               BatchBody(CPUs)
               ,"\nping -n 6 127.0.0.1 >nul"
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static string BatchBody(int cpus)
        {
            if (cpus <= 1)
            {
                return @"topoSet >> ""topoSet.log"" 2>&1
renumberMesh -overwrite >> ""renumberMesh.log"" 2>&1
foamRun -solver fluid >> ""foamRun.log"" 2>&1";
            }

            return $@"topoSet >> ""topoSet.log"" 2>&1
renumberMesh -overwrite >> ""renumberMesh.log"" 2>&1
decomposePar -force >> ""decomposePar.log"" 2>&1
mpiexec -np {cpus} foamRun -solver fluid -parallel >> ""foamRun.log"" 2>&1
reconstructPar >> ""reconstructPar.log"" 2>&1";
        }
    }
}
