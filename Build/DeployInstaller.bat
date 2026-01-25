@echo off
setlocal EnableDelayedExpansion

:: ------------------------------------------------
:: 1. Load .env file if it exists
:: ------------------------------------------------
if exist .env (
    for /f "usebackq tokens=1* delims==" %%A in (".env") do (
        if "%%A"=="REMOTE_USER" set REMOTE_USER=%%B
        if "%%A"=="REMOTE_HOST" set REMOTE_HOST=%%B
        if "%%A"=="REMOTE_DIR" set REMOTE_DIR=%%B
        if "%%A"=="SSH_KEY_PATH" set SSH_KEY_PATH=%%B
    )
)

:: ------------------------------------------------
:: 2. Configuration & Defaults
:: ------------------------------------------------
if "%REMOTE_USER%"=="" set REMOTE_USER=eddycfd
if "%REMOTE_HOST%"=="" set REMOTE_HOST=europa.uberspace.de
:: Default to the path specified by the user in the prompt
if "%REMOTE_DIR%"=="" set REMOTE_DIR=/var/www/virtual/eddycfd/html/download/files
if "%SSH_KEY_PATH%"=="" set SSH_KEY_PATH=%USERPROFILE%\.ssh\id_ed25519

:: ------------------------------------------------
:: 3. Get Version Number (to find the output folder)
:: ------------------------------------------------
:: Assuming the GHA file is at ..\Eddy\bin\Eddy.gha relative to this script
set "GHA_PATH=%~dp0..\Eddy\bin\Eddy.gha"

:: Use PowerShell to extract the FileVersion. 
:: We presume the format matches what Inno Setup uses: e.g. 0.5.3.815
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "(Get-Item '%GHA_PATH%').VersionInfo.FileVersion"`) do set "APP_VER=%%v"

echo.
echo Detected App Version: %APP_VER%
echo Installer Folder:     "%~dp0%APP_VER%"

:: ------------------------------------------------
:: 4. Deployment
:: ------------------------------------------------
echo.
echo ------------------------------------------------
echo Deploying Installer to %REMOTE_USER%@%REMOTE_HOST%:%REMOTE_DIR%
echo ------------------------------------------------

:: Check if the versioned folder exists (as created by BuildInstaller.bat)
if not exist "%~dp0%APP_VER%" (
    echo Error: Installer folder '%APP_VER%' not found!
    echo Please run 'BuildInstaller.bat' first.
    pause
    exit /b 1
)

:: Ensure remote directory exists
echo Ensuring remote directory exists...
ssh -i "%SSH_KEY_PATH%" %REMOTE_USER%@%REMOTE_HOST% "mkdir -p %REMOTE_DIR%"

:: Upload the entire version folder via SCP -r (recursive)
echo Uploading folder "%APP_VER%"...
scp -r -i "%SSH_KEY_PATH%" "%~dp0%APP_VER%" %REMOTE_USER%@%REMOTE_HOST%:%REMOTE_DIR%/

if %ERRORLEVEL% equ 0 (
    echo.
    echo Deployment successful!
    echo Uploaded folder: %APP_VER%

    echo.
    echo Setting permissions on remote files...
    ssh -i "%SSH_KEY_PATH%" %REMOTE_USER%@%REMOTE_HOST% "chmod 755 %REMOTE_DIR%/*"

) else (
    echo.
    echo Deployment failed!
)

:: Pause to keep window open
echo.
echo Press any key to close...
pause

