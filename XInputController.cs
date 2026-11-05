using System.Runtime.InteropServices;

namespace BluetoothGamepadBridge;

public enum GamepadLayoutMode
{
    /// <summary>
    /// Standard Android / Linux Generic.kl:
    /// Button 1=A, Button 2=B, Button 4=X, Button 5=Y, Button 7=LB, Button 8=RB,
    /// Button 9=LT, Button 10=RT, Button 11=Back, Button 12=Start, Button 13=Guide,
    /// Button 14=L3, Button 15=R3. (Button 3 is skipped to avoid KEYCODE_BUTTON_C 98!)
    /// </summary>
    XboxStandard = 1,

    /// <summary>
    /// PlayStation / DualShock layout (matches Vendor_054c_Product_05c4.kl):
    /// Square=1 (Xbox X), Cross=2 (Xbox A), Circle=3 (Xbox B), Triangle=4 (Xbox Y),
    /// L1=5 (LB), R1=6 (RB), L2=7 (LT), R2=8 (RT), Share=9 (Back), Options=10 (Start),
    /// L3=11, R3=12, PS=13 (Guide).
    /// </summary>
    PlayStation = 2,

    /// <summary>
    /// Direct sequential mapping (1 to 16).
    /// </summary>
    RawSequential = 3,

    /// <summary>
    /// Inverted X/Y layout (for emulators or apps where X is top and Y is left):
    /// Button 1=A, Button 2=B, Button 4=Y, Button 5=X, LB=7, RB=8, LT=9, RT=10...
    /// </summary>
    XboxSwappedXY = 4
}

/// <summary>
/// Interacts directly with Xbox controllers via XInput.
/// 
/// IMPORTANT:
/// XInput reads ONLY the gamepad hardware state directly via driver calls.
/// It NEVER intercepts, hooks, or blocks keyboard or mouse inputs in any way.
/// </summary>
public sealed class XInputController
{
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_DEVICE_NOT_CONNECTED = 1167;

    // Responsive low-latency deadzones (tighter than default 7849 for instant joystick response)
    private const short LEFT_THUMB_DEADZONE = 3500;
    private const short RIGHT_THUMB_DEADZONE = 3500;
    private const byte TRIGGER_THRESHOLD = 15;

    // XInput button bitmasks
    private const ushort XINPUT_GAMEPAD_DPAD_UP        = 0x0001;
    private const ushort XINPUT_GAMEPAD_DPAD_DOWN      = 0x0002;
    private const ushort XINPUT_GAMEPAD_DPAD_LEFT      = 0x0004;
    private const ushort XINPUT_GAMEPAD_DPAD_RIGHT     = 0x0008;
    private const ushort XINPUT_GAMEPAD_START          = 0x0010;
    private const ushort XINPUT_GAMEPAD_BACK           = 0x0020;
    private const ushort XINPUT_GAMEPAD_LEFT_THUMB     = 0x0040;
    private const ushort XINPUT_GAMEPAD_RIGHT_THUMB    = 0x0080;
    private const ushort XINPUT_GAMEPAD_LEFT_SHOULDER  = 0x0100;
    private const ushort XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200;
    private const ushort XINPUT_GAMEPAD_GUIDE          = 0x0400; // Supported via XInputGetStateEx (ordinal 100)
    private const ushort XINPUT_GAMEPAD_A              = 0x1000;
    private const ushort XINPUT_GAMEPAD_B              = 0x2000;
    private const ushort XINPUT_GAMEPAD_X              = 0x4000;
    private const ushort XINPUT_GAMEPAD_Y              = 0x8000;

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState14(int dwUserIndex, ref XINPUT_STATE pState);

    [DllImport("xinput1_4.dll", EntryPoint = "#100", CallingConvention = CallingConvention.StdCall)]
    private static extern int XInputGetStateEx14(int dwUserIndex, ref XINPUT_STATE pState);

    [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState13(int dwUserIndex, ref XINPUT_STATE pState);

    [DllImport("xinput1_3.dll", EntryPoint = "#100", CallingConvention = CallingConvention.StdCall)]
    private static extern int XInputGetStateEx13(int dwUserIndex, ref XINPUT_STATE pState);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState91(int dwUserIndex, ref XINPUT_STATE pState);

    private enum XInputApiVersion
    {
        None,
        XInput14WithEx,
        XInput14,
        XInput13WithEx,
        XInput13,
        XInput91
    }

    private readonly int _userIndex;
    private XInputApiVersion _apiVersion = XInputApiVersion.None;
    private uint _lastPacketNumber = uint.MaxValue;

    public bool IsConnected { get; private set; }
    public string ControllerName => $"Xbox Controller (Index {_userIndex})";
    public GamepadLayoutMode CurrentLayout { get; set; } = GamepadLayoutMode.XboxStandard;

    public XInputController(int userIndex = 0)
    {
        _userIndex = userIndex;
        DetectApiVersion();
    }

    private void DetectApiVersion()
    {
        XINPUT_STATE state = default;
        try
        {
            int res = XInputGetStateEx14(_userIndex, ref state);
            if (res == ERROR_SUCCESS || res == ERROR_DEVICE_NOT_CONNECTED)
            {
                _apiVersion = XInputApiVersion.XInput14WithEx;
                return;
            }
        }
        catch { }

        try
        {
            int res = XInputGetState14(_userIndex, ref state);
            if (res == ERROR_SUCCESS || res == ERROR_DEVICE_NOT_CONNECTED)
            {
                _apiVersion = XInputApiVersion.XInput14;
                return;
            }
        }
        catch { }

        try
        {
            int res = XInputGetStateEx13(_userIndex, ref state);
            if (res == ERROR_SUCCESS || res == ERROR_DEVICE_NOT_CONNECTED)
            {
                _apiVersion = XInputApiVersion.XInput13WithEx;
                return;
            }
        }
        catch { }

        try
        {
            int res = XInputGetState13(_userIndex, ref state);
            if (res == ERROR_SUCCESS || res == ERROR_DEVICE_NOT_CONNECTED)
            {
                _apiVersion = XInputApiVersion.XInput13;
                return;
            }
        }
        catch { }

        try
        {
            int res = XInputGetState91(_userIndex, ref state);
            if (res == ERROR_SUCCESS || res == ERROR_DEVICE_NOT_CONNECTED)
            {
                _apiVersion = XInputApiVersion.XInput91;
                return;
            }
        }
        catch { }

        _apiVersion = XInputApiVersion.None;
    }

    private int QueryState(ref XINPUT_STATE state)
    {
        return _apiVersion switch
        {
            XInputApiVersion.XInput14WithEx => XInputGetStateEx14(_userIndex, ref state),
            XInputApiVersion.XInput14       => XInputGetState14(_userIndex, ref state),
            XInputApiVersion.XInput13WithEx => XInputGetStateEx13(_userIndex, ref state),
            XInputApiVersion.XInput13       => XInputGetState13(_userIndex, ref state),
            XInputApiVersion.XInput91       => XInputGetState91(_userIndex, ref state),
            _ => ERROR_DEVICE_NOT_CONNECTED
        };
    }

    /// <summary>
    /// Polls the controller. Returns true if state changed, false if unchanged or disconnected.
    /// </summary>
    public bool Poll(out GamepadState gamepadState)
    {
        gamepadState = GamepadState.Default;
        if (_apiVersion == XInputApiVersion.None)
        {
            DetectApiVersion();
            if (_apiVersion == XInputApiVersion.None)
            {
                IsConnected = false;
                return false;
            }
        }

        XINPUT_STATE rawState = default;
        int result = QueryState(ref rawState);

        if (result != ERROR_SUCCESS)
        {
            IsConnected = false;
            _lastPacketNumber = uint.MaxValue;
            return false;
        }

        IsConnected = true;

        bool stateChanged = rawState.dwPacketNumber != _lastPacketNumber;
        _lastPacketNumber = rawState.dwPacketNumber;

        // Process Thumbsticks with radial deadzones
        var (lsX, lsY) = ApplyRadialDeadzone(rawState.Gamepad.sThumbLX, rawState.Gamepad.sThumbLY, LEFT_THUMB_DEADZONE);
        var (rsX, rsY) = ApplyRadialDeadzone(rawState.Gamepad.sThumbRX, rawState.Gamepad.sThumbRY, RIGHT_THUMB_DEADZONE);

        // Convert sticks to 0..65535 (center = 32768)
        // Invert Y-axis for standard HID where Up is 0 and Down is 65535
        gamepadState.LeftStickX = (ushort)Math.Clamp(lsX + 32768, 0, 65535);
        gamepadState.LeftStickY = (ushort)Math.Clamp((-lsY) + 32768, 0, 65535);
        gamepadState.RightStickX = (ushort)Math.Clamp(rsX + 32768, 0, 65535);
        gamepadState.RightStickY = (ushort)Math.Clamp((-rsY) + 32768, 0, 65535);

        // Triggers with threshold
        gamepadState.LeftTrigger = rawState.Gamepad.bLeftTrigger < TRIGGER_THRESHOLD ? (byte)0 : rawState.Gamepad.bLeftTrigger;
        gamepadState.RightTrigger = rawState.Gamepad.bRightTrigger < TRIGGER_THRESHOLD ? (byte)0 : rawState.Gamepad.bRightTrigger;

        // D-Pad to Hat Switch (1..8, 0 = neutral)
        gamepadState.HatSwitch = ComputeHatSwitch(rawState.Gamepad.wButtons);

        // Button bitmask mapping based on active layout
        gamepadState.Buttons = MapButtons(rawState.Gamepad.wButtons, rawState.Gamepad.bLeftTrigger, rawState.Gamepad.bRightTrigger, CurrentLayout);

        return stateChanged;
    }

    private static (short x, short y) ApplyRadialDeadzone(short rawX, short rawY, short deadzone)
    {
        double magnitude = Math.Sqrt((double)rawX * rawX + (double)rawY * rawY);
        if (magnitude < deadzone)
        {
            return (0, 0);
        }

        double normalizedMag = Math.Min(1.0, (magnitude - deadzone) / (32767.0 - deadzone));
        double factor = normalizedMag / magnitude;

        short x = (short)Math.Clamp(rawX * factor * 32767.0, -32768.0, 32767.0);
        short y = (short)Math.Clamp(rawY * factor * 32767.0, -32768.0, 32767.0);
        return (x, y);
    }

    private static byte ComputeHatSwitch(ushort buttons)
    {
        bool up = (buttons & XINPUT_GAMEPAD_DPAD_UP) != 0;
        bool down = (buttons & XINPUT_GAMEPAD_DPAD_DOWN) != 0;
        bool left = (buttons & XINPUT_GAMEPAD_DPAD_LEFT) != 0;
        bool right = (buttons & XINPUT_GAMEPAD_DPAD_RIGHT) != 0;

        if (up && right) return 2;   // Up-Right
        if (down && right) return 4; // Down-Right
        if (down && left) return 6;  // Down-Left
        if (up && left) return 8;    // Up-Left
        if (up) return 1;            // Up
        if (right) return 3;         // Right
        if (down) return 5;          // Down
        if (left) return 7;          // Left

        return 0; // Neutral / Released
    }

    private static ushort MapButtons(ushort rawButtons, byte rawLeftTrigger, byte rawRightTrigger, GamepadLayoutMode layout)
    {
        return layout switch
        {
            GamepadLayoutMode.PlayStation   => MapButtonsPlayStation(rawButtons, rawLeftTrigger, rawRightTrigger),
            GamepadLayoutMode.RawSequential => MapButtonsRaw(rawButtons, rawLeftTrigger, rawRightTrigger),
            GamepadLayoutMode.XboxSwappedXY => MapButtonsXboxSwappedXY(rawButtons, rawLeftTrigger, rawRightTrigger),
            _                               => MapButtonsXboxStandard(rawButtons, rawLeftTrigger, rawRightTrigger)
        };
    }

    /// <summary>
    /// Swapped X/Y mapping for Android emulators expecting Nintendo / inverted face buttons
    /// </summary>
    private static ushort MapButtonsXboxSwappedXY(ushort rawButtons, byte rawLeftTrigger, byte rawRightTrigger)
    {
        ushort mapped = MapButtonsXboxStandard(rawButtons, rawLeftTrigger, rawRightTrigger);
        // Clear bits 3 and 4 (Buttons 4 and 5)
        mapped &= unchecked((ushort)~( (1 << 3) | (1 << 4) ));
        // Swap: Xbox X -> Button 5, Xbox Y -> Button 4
        if ((rawButtons & XINPUT_GAMEPAD_X) != 0) mapped |= (1 << 4);
        if ((rawButtons & XINPUT_GAMEPAD_Y) != 0) mapped |= (1 << 3);
        return mapped;
    }

    /// <summary>
    /// Standard Android Generic.kl Mapping:
    /// Button 1=A, Button 2=B, Button 4=X, Button 5=Y, Button 7=LB, Button 8=RB,
    /// Button 9=LT, Button 10=RT, Button 11=Back, Button 12=Start, Button 13=Guide,
    /// Button 14=L3, Button 15=R3. (Skips Button 3 to avoid BUTTON_C 98!)
    /// </summary>
    private static ushort MapButtonsXboxStandard(ushort rawButtons, byte rawLeftTrigger, byte rawRightTrigger)
    {
        ushort mapped = 0;

        // Button 1 (Bit 0): A -> KEYCODE_BUTTON_A (96)
        if ((rawButtons & XINPUT_GAMEPAD_A) != 0) mapped |= (1 << 0);

        // Button 2 (Bit 1): B -> KEYCODE_BUTTON_B (97)
        if ((rawButtons & XINPUT_GAMEPAD_B) != 0) mapped |= (1 << 1);

        // Bit 2 (Button 3): Skips BTN_C (98)

        // Button 4 (Bit 3): X -> KEYCODE_BUTTON_X (99)
        if ((rawButtons & XINPUT_GAMEPAD_X) != 0) mapped |= (1 << 3);

        // Button 5 (Bit 4): Y -> KEYCODE_BUTTON_Y (100)
        if ((rawButtons & XINPUT_GAMEPAD_Y) != 0) mapped |= (1 << 4);

        // Bit 5 (Button 6): Skips BTN_Z (101)

        // Button 7 (Bit 6): LB -> KEYCODE_BUTTON_L1 (102)
        if ((rawButtons & XINPUT_GAMEPAD_LEFT_SHOULDER) != 0) mapped |= (1 << 6);

        // Button 8 (Bit 7): RB -> KEYCODE_BUTTON_R1 (103)
        if ((rawButtons & XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0) mapped |= (1 << 7);

        // Button 9 (Bit 8): LT digital -> KEYCODE_BUTTON_L2 (104)
        if (rawLeftTrigger > TRIGGER_THRESHOLD) mapped |= (1 << 8);

        // Button 10 (Bit 9): RT digital -> KEYCODE_BUTTON_R2 (105)
        if (rawRightTrigger > TRIGGER_THRESHOLD) mapped |= (1 << 9);

        // Button 11 (Bit 10): Back / View -> KEYCODE_BUTTON_SELECT (109)
        if ((rawButtons & XINPUT_GAMEPAD_BACK) != 0) mapped |= (1 << 10);

        // Button 12 (Bit 11): Start / Menu -> KEYCODE_BUTTON_START (108)
        if ((rawButtons & XINPUT_GAMEPAD_START) != 0) mapped |= (1 << 11);

        // Button 13 (Bit 12): Xbox Guide / Home -> KEYCODE_BUTTON_MODE (110)
        if ((rawButtons & XINPUT_GAMEPAD_GUIDE) != 0) mapped |= (1 << 12);

        // Button 14 (Bit 13): L3 -> KEYCODE_BUTTON_THUMBL (106)
        if ((rawButtons & XINPUT_GAMEPAD_LEFT_THUMB) != 0) mapped |= (1 << 13);

        // Button 15 (Bit 14): R3 -> KEYCODE_BUTTON_THUMBR (107)
        if ((rawButtons & XINPUT_GAMEPAD_RIGHT_THUMB) != 0) mapped |= (1 << 14);

        return mapped;
    }

    /// <summary>
    /// PlayStation / DualShock Scancode Mapping:
    /// In PS layout:
    /// Button 1 = Square (Xbox X)
    /// Button 2 = Cross  (Xbox A)
    /// Button 3 = Circle (Xbox B)
    /// Button 4 = Triangle (Xbox Y)
    /// Button 5 = L1 (Xbox LB)
    /// Button 6 = R1 (Xbox RB)
    /// Button 7 = L2 (Xbox LT)
    /// Button 8 = R2 (Xbox RT)
    /// Button 9 = Share (Xbox Back)
    /// Button 10 = Options (Xbox Start)
    /// Button 11 = L3 (Xbox L3)
    /// Button 12 = R3 (Xbox R3)
    /// Button 13 = PS Button (Xbox Guide)
    /// </summary>
    private static ushort MapButtonsPlayStation(ushort rawButtons, byte rawLeftTrigger, byte rawRightTrigger)
    {
        ushort mapped = 0;

        // Button 1 (Bit 0): Square (West = Xbox X)
        if ((rawButtons & XINPUT_GAMEPAD_X) != 0) mapped |= (1 << 0);

        // Button 2 (Bit 1): Cross (South = Xbox A)
        if ((rawButtons & XINPUT_GAMEPAD_A) != 0) mapped |= (1 << 1);

        // Button 3 (Bit 2): Circle (East = Xbox B)
        if ((rawButtons & XINPUT_GAMEPAD_B) != 0) mapped |= (1 << 2);

        // Button 4 (Bit 3): Triangle (North = Xbox Y)
        if ((rawButtons & XINPUT_GAMEPAD_Y) != 0) mapped |= (1 << 3);

        // Button 5 (Bit 4): L1 (Xbox LB)
        if ((rawButtons & XINPUT_GAMEPAD_LEFT_SHOULDER) != 0) mapped |= (1 << 4);

        // Button 6 (Bit 5): R1 (Xbox RB)
        if ((rawButtons & XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0) mapped |= (1 << 5);

        // Button 7 (Bit 6): L2 (Xbox LT digital)
        if (rawLeftTrigger > TRIGGER_THRESHOLD) mapped |= (1 << 6);

        // Button 8 (Bit 7): R2 (Xbox RT digital)
        if (rawRightTrigger > TRIGGER_THRESHOLD) mapped |= (1 << 7);

        // Button 9 (Bit 8): Share (Xbox Back)
        if ((rawButtons & XINPUT_GAMEPAD_BACK) != 0) mapped |= (1 << 8);

        // Button 10 (Bit 9): Options (Xbox Start)
        if ((rawButtons & XINPUT_GAMEPAD_START) != 0) mapped |= (1 << 9);

        // Button 11 (Bit 10): L3 (Left Thumbstick Click)
        if ((rawButtons & XINPUT_GAMEPAD_LEFT_THUMB) != 0) mapped |= (1 << 10);

        // Button 12 (Bit 11): R3 (Right Thumbstick Click)
        if ((rawButtons & XINPUT_GAMEPAD_RIGHT_THUMB) != 0) mapped |= (1 << 11);

        // Button 13 (Bit 12): PS Button (Xbox Guide)
        if ((rawButtons & XINPUT_GAMEPAD_GUIDE) != 0) mapped |= (1 << 12);

        return mapped;
    }

    /// <summary>
    /// Direct raw sequential 1..16 mapping
    /// </summary>
    private static ushort MapButtonsRaw(ushort rawButtons, byte rawLeftTrigger, byte rawRightTrigger)
    {
        ushort mapped = 0;
        if ((rawButtons & XINPUT_GAMEPAD_A) != 0)              mapped |= (1 << 0);
        if ((rawButtons & XINPUT_GAMEPAD_B) != 0)              mapped |= (1 << 1);
        if ((rawButtons & XINPUT_GAMEPAD_X) != 0)              mapped |= (1 << 2);
        if ((rawButtons & XINPUT_GAMEPAD_Y) != 0)              mapped |= (1 << 3);
        if ((rawButtons & XINPUT_GAMEPAD_LEFT_SHOULDER) != 0)  mapped |= (1 << 4);
        if ((rawButtons & XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0) mapped |= (1 << 5);
        if ((rawButtons & XINPUT_GAMEPAD_BACK) != 0)           mapped |= (1 << 6);
        if ((rawButtons & XINPUT_GAMEPAD_START) != 0)          mapped |= (1 << 7);
        if ((rawButtons & XINPUT_GAMEPAD_LEFT_THUMB) != 0)     mapped |= (1 << 8);
        if ((rawButtons & XINPUT_GAMEPAD_RIGHT_THUMB) != 0)    mapped |= (1 << 9);
        if ((rawButtons & XINPUT_GAMEPAD_GUIDE) != 0)          mapped |= (1 << 10);
        if (rawLeftTrigger > TRIGGER_THRESHOLD)                mapped |= (1 << 11);
        if (rawRightTrigger > TRIGGER_THRESHOLD)               mapped |= (1 << 12);
        return mapped;
    }
}
