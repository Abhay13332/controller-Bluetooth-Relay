@echo off
echo Stopping background Bluetooth Gamepad Bridge...
taskkill /F /IM BluetoothGamepadBridge.exe 2>nul
if %errorlevel% equ 0 (
    echo [SUCCESS] Bluetooth Gamepad Bridge stopped.
) else (
    echo [INFO] No running instance of BluetoothGamepadBridge.exe was found.
)
timeout /t 2 >nul

