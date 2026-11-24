@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] csc.exe not found at %CSC%
    pause
    exit /b 1
)

echo Building EarbudsController.exe...
"%CSC%" /nologo /t:winexe /optimize+ ^
    /r:System.dll ^
    /r:System.Core.dll ^
    /r:System.Drawing.dll ^
    /r:System.Windows.Forms.dll ^
    /r:System.Web.Extensions.dll ^
    /r:System.Net.Http.dll ^
    /out:"%~dp0EarbudsController.exe" ^
    "%~dp0AudioEngine.cs" ^
    "%~dp0BatteryService.cs" ^
    "%~dp0BluetoothProtocolManager.cs" ^
    "%~dp0GeminiService.cs" ^
    "%~dp0MainFlyoutForm.cs" ^
    "%~dp0TrayApplicationContext.cs" ^
    "%~dp0Program.cs"

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] EarbudsController.exe built successfully!
) else (
    echo [FAIL] Build failed with error code %ERRORLEVEL%
)
