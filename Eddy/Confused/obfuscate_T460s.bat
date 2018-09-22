"C:\Users\Patrick Kastner\Downloads\ConfuserEx_bin\Confuser.CLI.exe" "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\Confused\Eddy_T460s.crproj"
del /s /q /f "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\bin\*.exe.config"
del /s /q /f "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\bin\*.pdb"
del /s /q /f "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\bin\*.xml"
move /Y *.exe ..\bin
move /Y *.dll ..\bin
PAUSE