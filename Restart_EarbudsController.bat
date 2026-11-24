@echo off
echo Stopping any old EarbudsController processes...
taskkill /F /IM EarbudsController.exe >nul 2>&1
taskkill /F /IM EarbudsController_*.exe >nul 2>&1
ping 127.0.0.1 -n 2 >nul
echo Starting fresh EarbudsController.exe...
start "" "%~dp0EarbudsController.exe"
exit
