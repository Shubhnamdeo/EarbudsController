@echo off
echo Stopping any old EarbudsController processes...
taskkill /F /IM EarbudsController.exe >nul 2>&1
taskkill /F /IM EarbudsController_*.exe >nul 2>&1
taskkill /F /IM .exe >nul 2>&1
timeout /t 1 /nobreak >nul
echo Starting fresh EarbudsController.exe...
start "" "%~dp0EarbudsController.exe"
exit
