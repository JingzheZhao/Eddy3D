using System;
using System.Linq;

namespace EddyLib.Indoor.BatchFiles
{
    public class RunMeshBatch : GenericBatchFile
    {
        public RunMeshBatch(IndoorDomain IndoorDom, int CPUs)
        {
            this.BatchLocation = IndoorDom.WorkingDir;
            this.BatchName = "run_mesh.bat";
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

        //**Changed numer of CPUs to 1 instead of 8   (mpiexec -np 8 snappyHexMesh -overwrite -parallel )
        private static string BatchBody(int cpus)
        {
            return $@"blockMesh
if errorlevel 1 exit /b %errorlevel%
surfaceFeatures
if errorlevel 1 exit /b %errorlevel%
decomposePar -force
if errorlevel 1 exit /b %errorlevel%
mpiexec -np {cpus} snappyHexMesh -overwrite -parallel
if errorlevel 1 exit /b %errorlevel%
reconstructPar -constant -noFields
if errorlevel 1 exit /b %errorlevel%
renumberMesh -overwrite
if errorlevel 1 exit /b %errorlevel%";
        }

    }
}
