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
            if (cpus <= 1)
            {
                return @"blockMesh >> ""blockMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
surfaceFeatures >> ""surfaceFeatures.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
snappyHexMesh -overwrite >> ""snappyHexMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
renumberMesh -overwrite >> ""renumberMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%

topoSet >> ""topoSet.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%

renumberMesh -overwrite >> ""renumberMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
foamRun -solver fluid >> ""foamRun.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%";
            }

            return $@"blockMesh >> ""blockMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
surfaceFeatures >> ""surfaceFeatures.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
decomposePar -force >> ""decomposePar.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
mpiexec -np {cpus} snappyHexMesh -overwrite -parallel >> ""snappyHexMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
reconstructPar -constant -noFields >> ""reconstructParMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
renumberMesh -overwrite >> ""renumberMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%

topoSet >> ""topoSet.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%

renumberMesh -overwrite >> ""renumberMesh.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
decomposePar -force >> ""decomposePar.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
mpiexec -np {cpus} foamRun -solver fluid -parallel >> ""foamRun.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%
reconstructPar >> ""reconstructPar.log"" 2>&1
if errorlevel 1 exit /b %errorlevel%";
        }
    }
}
