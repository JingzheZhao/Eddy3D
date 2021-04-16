using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

namespace EddyLib.Indoor.BatchFiles
{
    public class RunMeshBatch : GenericBatchFile
    {
        public RunMeshBatch(IndoorDomain IndoorDom)
        {
            this.BatchLocation = IndoorDom.WorkingDir;
            this.BatchName = "run_mesh.bat";
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

        //**Changed numer of CPUs to 1 instead of 8   (mpiexec -np 8 snappyHexMesh -overwrite -parallel )
        private static string BatchBody()
        {
            return @"blockMesh.exe
surfaceFeatureExtract
decomposePar -force
mpiexec -np 1 snappyHexMesh -overwrite -parallel 
reconstructParMesh -constant
renumberMesh -overwrite ";
        }
    }
}