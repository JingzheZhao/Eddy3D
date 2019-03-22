set PATH=%PATH%;C:\Program Files\7-Zip\
cd ..\bin\
7z a Eddy.zip CallBatchRunner.exe CallOC.exe CallOF.exe CallProbes.exe CallRay.exe EddyLib.dll Eddy.gha CsvHelper.dll ConsoleAppLauncher.dll CommandLine.dll
move /Y Eddy.zip ..\Compile\