using System;
using System.Linq;

namespace EddyLib.Indoor.BatchFiles
{
    public class DeleteProcessorBatch : GenericBatchFile
    {
        public DeleteProcessorBatch(IndoorDomain IndoorDom)
        {
            this.BatchLocation = IndoorDom.WorkingDir;
            this.BatchName = "delete_processor_folders.bat";
            
            // Navigate up one level from Scripts folder
            this.Header = "@echo off\ncd /d \"%~dp0..\"";

            string body = @"setlocal EnableDelayedExpansion

echo =====================================
echo Recursive OpenFOAM processor cleanup
echo Root: %cd%
echo =====================================

for /d /r %%D in (processor*) do (
    echo Deleting: %%D
    rmdir /s /q ""%%D""
)

echo -------------------------------------
echo Done.
ping -n 6 127.0.0.1 >nul";

            this.FullDictString = this.Header + "\n" + body;
        }
    }
}
