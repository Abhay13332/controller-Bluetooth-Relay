@echo off
setlocal
echo ======================================================================
echo Alternative: Scrcpy ADB Gamepad Forwarding (Zero Latency via USB/Wi-Fi)
echo ======================================================================
echo.
echo This alternative uses ADB/Scrcpy to forward your wired Xbox controller
echo straight into Android's kernel Linux UHID virtual gamepad subsystem.
echo - Zero Bluetooth requirements
echo - Sub-millisecond latency
echo - Completely isolates PC mouse and keyboard
echo.

where scrcpy >nul 2>nul
if %errorlevel% neq 0 (
    echo [INFO] scrcpy was not found in your PATH.
    echo Download scrcpy from: https://github.com/Genymobile/scrcpy/releases
    echo Place scrcpy.exe in your PATH or in this folder, then re-run this script.
    echo.
    pause
    exit /b 1
)

echo Starting Scrcpy gamepad-only passthrough (no video/audio streaming)...
scrcpy --no-video --no-audio --gamepad=uhid

pause

