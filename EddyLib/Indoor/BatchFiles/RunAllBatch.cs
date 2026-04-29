using System;
using System.Linq;

namespace EddyLib.Indoor.BatchFiles
{
    public class RunAllBatch : GenericBatchFile
    {
        public RunAllBatch(IndoorDomain IndoorDom, int CPUs)
        {
            this.BatchLocation = IndoorDom.WorkingDir;
            this.BatchName = "run_all.bat";
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
            return $@"blockMesh 2>&1 | tee -a ""blockMesh.log""
surfaceFeatures 2>&1 | tee -a ""surfaceFeatures.log""
decomposePar -force 2>&1 | tee -a ""decomposePar.log""
mpiexec -np {cpus} snappyHexMesh -overwrite -parallel 2>&1 | tee -a ""snappyHexMesh.log""
reconstructParMesh -constant 2>&1 | tee -a ""reconstructParMesh.log""
renumberMesh -overwrite 2>&1 | tee -a ""renumberMesh.log""

topoSet 2>&1 | tee -a ""topoSet.log""

renumberMesh -overwrite 2>&1 | tee -a ""renumberMesh.log""
decomposePar -force 2>&1 | tee -a ""decomposePar.log""
mpiexec -np {cpus} foamRun -solver fluid -parallel 2>&1 | tee -a ""foamRun.log""
reconstructPar 2>&1 | tee -a ""reconstructPar.log""";
        }
    }
}
