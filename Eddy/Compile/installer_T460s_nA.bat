"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy.sln"
"C:\Users\Patrick Kastner\Downloads\ConfuserEx_bin\Confuser.CLI.exe" "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\Compile\Eddy_T460s.crproj"
del /s /q /f "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\bin\*.exe.config"
del /s /q /f "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\bin\*.pdb"
del /s /q /f "C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\Eddy\bin\*.xml"
move /Y *.exe ..\bin
move /Y *.dll ..\bin
"C:\Program Files (x86)\Inno Setup 5\ISCC.exe" /Qp "C:\Users\%USERNAME%\Documents\Github\Windtunnel\Eddy\Compile\installer_T460s_nA.iss"
PAUSE