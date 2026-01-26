@echo off
cd /d "%~dp0.." 
 for /d %%i in (processor*) do echo Found %%i
