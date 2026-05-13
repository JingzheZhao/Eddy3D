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
                return @"topoSet 2>&1 | tee -a ""topoSet.log""
if errorlevel 1 exit /b %errorlevel%
renumberMesh -constant -overwrite 2>&1 | tee -a ""renumberMesh.log""
if errorlevel 1 exit /b %errorlevel%
foamRun -solver fluid 2>&1 | tee -a ""foamRun.log""
if errorlevel 1 exit /b %errorlevel%";
            }

            return $@"topoSet 2>&1 | tee -a ""topoSet.log""
if errorlevel 1 exit /b %errorlevel%
renumberMesh -constant -overwrite 2>&1 | tee -a ""renumberMesh.log""
if errorlevel 1 exit /b %errorlevel%
decomposePar -force 2>&1 | tee -a ""decomposePar.log""
if errorlevel 1 exit /b %errorlevel%
mpiexec -np {cpus} foamRun -solver fluid -parallel 2>&1 | tee -a ""foamRun.log""
if errorlevel 1 exit /b %errorlevel%
reconstructPar 2>&1 | tee -a ""reconstructPar.log""
if errorlevel 1 exit /b %errorlevel%";
        }
    }
}
