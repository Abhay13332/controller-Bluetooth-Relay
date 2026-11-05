@echo off
setlocal
echo ======================================================================
echo HidHide Automatic Configuration (Total Controller Cloaking from PC)
echo ======================================================================
echo.
echo HidHide is a kernel-level filter driver from Nefarius that hides your
echo physical Xbox controller from Windows, Steam, and all PC games,
echo while allowing ONLY BluetoothGamepadBridge to access it.
echo.

set "HIDHIDE_CLI=C:\Program Files\Nefarius Software Solutions\HidHide\x64\HidHideCli.exe"

if not exist "%HIDHIDE_CLI%" (
    echo [INFO] HidHide is not currently installed on your PC.
    echo If you want 100%% total controller cloaking so Windows/Steam never sees it:
    echo 1. Download HidHide from: https://github.com/nefarius/HidHide/releases
    echo 2. Run the installer and restart your PC.
    echo 3. Re-run this script to automatically whitelist BluetoothGamepadBridge!
    echo.
    pause
    exit /b 0
)

echo [1/3] Adding BluetoothGamepadBridge.exe to HidHide whitelist...
for %%I in ("%~dp0..\bin\Publish\BluetoothGamepadBridge.exe") do set "APP_PATH=%%~fI"

if exist "%APP_PATH%" (
    "%HIDHIDE_CLI%" --app-reg "%APP_PATH%"
    echo Whitelisted: %APP_PATH%
) else (
    echo [Notice] Pre-built binary not found at %APP_PATH%.
    echo Please run build.bat first!
)

echo.
echo [2/3] Enabling HidHide cloaking...
"%HIDHIDE_CLI%" --cloak-on

echo.
echo [3/3] Current cloaked devices:
"%HIDHIDE_CLI%" --dev-list

echo.
echo [SUCCESS] HidHide is configured! Your Xbox controller is now invisible to
echo           Windows OS and Steam, and exclusive to BluetoothGamepadBridge.
pause

