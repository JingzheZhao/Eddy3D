"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy.sln"
"C:\Users\pkastner\Documents\ConfuserEx_bin\Confuser.CLI.exe" "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\Compile\Eddy_ESL.crproj"
del /s /q /f "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\*.exe.config"
del /s /q /f "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\*.pdb"
del /s /q /f "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\*.xml"
move /Y *.exe ..\bin
move /Y *.dll ..\bin
PAUSE