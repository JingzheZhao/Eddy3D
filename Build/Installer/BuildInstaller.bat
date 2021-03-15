call MountGDrive.bat
echo #define USER "%USERPROFILE%" > Strings/User.txt
del /s /q /f "..\Dotfuscated\*.exe.config"
del /s /q /f "..\Dotfuscated\*.pdb"
del /s /q /f "..\Dotfuscated\*.xml"
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /Qp "C:\Users\%USERNAME%\Documents\GitHub\Eddy3d\Build\Installer\Installer.iss"
robocopy . "X:\Patrick\Eddy\Installer" /E /XD Strings /XD GHLink /XF BuildInstaller.bat /XF Installer.iss /XF MountGDrive.bat /XF UnmountGDrive.bat
call UnmountGDrive.bat
timeout 500