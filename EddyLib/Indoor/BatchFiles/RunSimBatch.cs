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
               ,"\ntimeout /t 5"
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static string BatchBody(int cpus)
        {
            return $@"renumberMesh -overwrite 2>&1 | tee -a ""renumberMesh.log""
decomposePar -force 2>&1 | tee -a ""decomposePar.log""
mpiexec -np {cpus} buoyantSimpleFoam -parallel 2>&1 | tee -a ""buoyantSimpleFoam.log""
reconstructPar 2>&1 | tee -a ""reconstructPar.log""";
        }
    }
}