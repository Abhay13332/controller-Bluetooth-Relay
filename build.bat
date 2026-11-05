@echo off
setlocal
echo ========================================================
echo Building Xbox Controller -> Bluetooth Gamepad Bridge
echo ========================================================

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK is not installed or not in PATH!
    echo Please install the .NET 8.0 SDK or later from:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo.
echo [1/2] Restoring and Building Release binary...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b %errorlevel%
)

echo.
echo [2/2] Publishing standalone single-file executable...
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true -o ./bin/Publish
if %errorlevel% neq 0 (
    echo [WARNING] Single-file publish failed, but standard build succeeded in bin\Release\net8.0-windows10.0.19041.0.
) else (
    echo [SUCCESS] Standalone executable created at:
    echo           bin\Publish\BluetoothGamepadBridge.exe
    copy /y "bin\Publish\BluetoothGamepadBridge.exe" ".\BluetoothGamepadBridge.exe" >nul
    echo [COPIED]  BluetoothGamepadBridge.exe is now ready in current directory!
)

echo.
echo Build completed successfully!
pause

