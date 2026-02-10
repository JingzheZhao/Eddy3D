@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildInstaller.ps1" %*
exit /b %ERRORLEVEL%
