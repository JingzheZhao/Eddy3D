"C:\Users\pkastner\Documents\ConfuserEx_bin\Confuser.CLI.exe" "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\Compile\Eddy_ESL.crproj"
del /s /q /f "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\*.exe.config"
del /s /q /f "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\*.pdb"
del /s /q /f "C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\*.xml"
move /Y *.exe ..\bin
move /Y *.dll ..\bin
PAUSE