@echo off
cd /d "%~dp0.."
setlocal EnableDelayedExpansion

echo =====================================
echo Recursive OpenFOAM processor cleanup
echo Root: %cd%
echo =====================================

for /d /r %%D in (processor*) do (
    echo Deleting: %%D
    rmdir /s /q "%%D"
)

echo -------------------------------------
echo Done.
exit /b 0
