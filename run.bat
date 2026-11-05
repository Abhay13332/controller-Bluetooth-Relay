@echo off
setlocal
echo ========================================================
echo Starting Xbox Controller -> Bluetooth Gamepad Bridge
echo ========================================================

:: 1. Gracefully close any existing instance so Windows releases Bluetooth cleanly
taskkill /IM BluetoothGamepadBridge.exe >nul 2>nul
timeout /t 1 /nobreak >nul
taskkill /F /IM BluetoothGamepadBridge.exe >nul 2>nul
timeout /t 1 /nobreak >nul

:: 2. Check for executable directly in current directory first
if exist "%~dp0BluetoothGamepadBridge.exe" (
    echo [LAUNCH] Starting %~dp0BluetoothGamepadBridge.exe...
    "%~dp0BluetoothGamepadBridge.exe" %*
    goto :done
)

if exist "BluetoothGamepadBridge.exe" (
    echo [LAUNCH] Starting BluetoothGamepadBridge.exe...
    "BluetoothGamepadBridge.exe" %*
    goto :done
)

if exist "bin\Publish\BluetoothGamepadBridge.exe" (
    echo [LAUNCH] Starting bin\Publish\BluetoothGamepadBridge.exe...
    "bin\Publish\BluetoothGamepadBridge.exe" %*
    goto :done
)

if exist "bin\Release\net8.0-windows10.0.19041.0\BluetoothGamepadBridge.exe" (
    echo [LAUNCH] Starting bin\Release\net8.0-windows10.0.19041.0\BluetoothGamepadBridge.exe...
    "bin\Release\net8.0-windows10.0.19041.0\BluetoothGamepadBridge.exe" %*
    goto :done
)

echo [LAUNCH] Running via dotnet run...
dotnet run -c Release -- %*

:done
pause
