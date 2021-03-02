echo #define USER "%USERPROFILE%" > Strings/User.txt
del /s /q /f "..\Build.Dotfuscated\*.exe.config"
del /s /q /f "..\Build.Dotfuscated\*.pdb"
del /s /q /f "..\Build.Dotfuscated\*.xml"
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /Qp "C:\Users\%USERNAME%\Documents\GitHub\Eddy3d\Build\Build.Installer\Installer.iss"
PAUSE