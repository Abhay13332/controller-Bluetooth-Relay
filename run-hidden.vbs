' Windows VBScript to launch BluetoothGamepadBridge completely in the background (hidden window)
Set WshShell = CreateObject("WScript.Shell")
Set FSO = CreateObject("Scripting.FileSystemObject")

' 1. Gracefully terminate any previous background instance so Bluetooth disconnects cleanly
On Error Resume Next
WshShell.Run "taskkill /IM BluetoothGamepadBridge.exe", 0, True
WScript.Sleep 1000
WshShell.Run "taskkill /F /IM BluetoothGamepadBridge.exe", 0, True
WScript.Sleep 1000
On Error GoTo 0

strScriptDir = FSO.GetParentFolderName(WScript.ScriptFullName)

strExe = ""
If FSO.FileExists(strScriptDir & "\BluetoothGamepadBridge.exe") Then
    strExe = strScriptDir & "\BluetoothGamepadBridge.exe"
ElseIf FSO.FileExists(strScriptDir & "\bin\Publish\BluetoothGamepadBridge.exe") Then
    strExe = strScriptDir & "\bin\Publish\BluetoothGamepadBridge.exe"
ElseIf FSO.FileExists(strScriptDir & "\bin\Release\net8.0-windows10.0.19041.0\BluetoothGamepadBridge.exe") Then
    strExe = strScriptDir & "\bin\Release\net8.0-windows10.0.19041.0\BluetoothGamepadBridge.exe"
Else
    strExe = "dotnet run --project """ & strScriptDir & "\BluetoothGamepadBridge.csproj"" -c Release -- --background"
End If

' Run process hidden (0 = hidden window, False = don't wait for completion)
If InStr(strExe, "dotnet run") > 0 Then
    WshShell.Run strExe, 0, False
Else
    WshShell.Run """" & strExe & """ --background", 0, False
End If

MsgBox "Bluetooth Gamepad Bridge is now running in the background!" & vbCrLf & _
       "Open Bluetooth on your Android device to connect." & vbCrLf & vbCrLf & _
       "Hotkeys if run via run.bat:" & vbCrLf & _
       "  [1] Xbox Standard (Generic.kl)" & vbCrLf & _
       "  [2] PlayStation Layout" & vbCrLf & _
       "  [3] Raw 1:1 Sequential" & vbCrLf & vbCrLf & _
       "To stop the bridge later, run stop-background.bat.", vbInformation, "Gamepad Bridge Active"
