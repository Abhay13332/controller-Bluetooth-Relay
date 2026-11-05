namespace BluetoothGamepadBridge;

/// <summary>
/// Represents the normalized state of the gamepad ready for HID transmission.
/// Exactly 13 bytes matching HidDescriptors.GamepadReportMap:
/// - Bytes 0..3: Left Stick X, Y (16-bit little-endian) -> Usage X, Y
/// - Bytes 4..7: Right Stick Z, Rz (16-bit little-endian) -> Usage Z, Rz
/// - Bytes 8..9: Triggers Brake, Accelerator (8-bit: 0..255) -> Simulation Brake, Accelerator
/// - Byte 10:   Hat Switch / D-Pad (4-bit + 4-bit padding)
/// - Bytes 11..12: Buttons (16-bit little-endian)
/// </summary>
public struct GamepadState : IEquatable<GamepadState>
{
    // Sticks: 0..65535, Center is 32768
    public ushort LeftStickX;
    public ushort LeftStickY;
    public ushort RightStickX;
    public ushort RightStickY;

    // Triggers: 0..255
    public byte LeftTrigger;
    public byte RightTrigger;

    // Hat switch / D-Pad:
    // 0 = Neutral/Released, 1 = Up, 2 = Up-Right, 3 = Right, 4 = Down-Right,
    // 5 = Down, 6 = Down-Left, 7 = Left, 8 = Up-Left
    public byte HatSwitch;

    // 16 Buttons bitmask
    public ushort Buttons;

    public static GamepadState Default => new()
    {
        LeftStickX = 32768,
        LeftStickY = 32768,
        RightStickX = 32768,
        RightStickY = 32768,
        LeftTrigger = 0,
        RightTrigger = 0,
        HatSwitch = 0,
        Buttons = 0
    };

    /// <summary>
    /// Serializes the state to the exact 13-byte layout defined in HidDescriptors.GamepadReportMap.
    /// Zero offset shifts, zero extra headers.
    /// </summary>
    public readonly byte[] ToBytes()
    {
        byte[] buffer = new byte[13];

        // Left Stick (Little-Endian 16-bit: 0..65535, center = 32768)
        buffer[0] = (byte)(LeftStickX & 0xFF);
        buffer[1] = (byte)((LeftStickX >> 8) & 0xFF);
        buffer[2] = (byte)(LeftStickY & 0xFF);
        buffer[3] = (byte)((LeftStickY >> 8) & 0xFF);

        // Right Stick (Little-Endian 16-bit: 0..65535, center = 32768)
        buffer[4] = (byte)(RightStickX & 0xFF);
        buffer[5] = (byte)((RightStickX >> 8) & 0xFF);
        buffer[6] = (byte)(RightStickY & 0xFF);
        buffer[7] = (byte)((RightStickY >> 8) & 0xFF);

        // Triggers (8-bit: 0..255)
        buffer[8] = LeftTrigger;
        buffer[9] = RightTrigger;

        // Hat Switch / D-Pad (lower 4 bits, upper 4 bits are 0)
        buffer[10] = (byte)(HatSwitch & 0x0F);

        // Buttons (Little-Endian 16-bit)
        buffer[11] = (byte)(Buttons & 0xFF);
        buffer[12] = (byte)((Buttons >> 8) & 0xFF);

        return buffer;
    }

    /// <summary>
    /// Checks if state changed significantly compared to previous report.
    /// Filters out electrical potentiometer ADC jitter (< 200 counts out of 65535, ~0.3%)
    /// to avoid flooding the Bluetooth radio buffer and introducing queue latency.
    /// </summary>
    public readonly bool HasSignificantChangeFrom(GamepadState other)
    {
        // 1. Digital buttons or D-pad changes are always transmitted immediately
        if (Buttons != other.Buttons || HatSwitch != other.HatSwitch)
            return true;

        // 2. Analog sticks: ignore micro-noise below 100 counts (~0.15% of range)
        const int STICK_NOISE_DEADBAND = 100;
        if (Math.Abs((int)LeftStickX - other.LeftStickX) > STICK_NOISE_DEADBAND) return true;
        if (Math.Abs((int)LeftStickY - other.LeftStickY) > STICK_NOISE_DEADBAND) return true;
        if (Math.Abs((int)RightStickX - other.RightStickX) > STICK_NOISE_DEADBAND) return true;
        if (Math.Abs((int)RightStickY - other.RightStickY) > STICK_NOISE_DEADBAND) return true;

        // 3. Triggers: 1 unit sensitivity for full analog degree graduation
        const int TRIGGER_NOISE_DEADBAND = 1;
        if (Math.Abs((int)LeftTrigger - other.LeftTrigger) > TRIGGER_NOISE_DEADBAND) return true;
        if (Math.Abs((int)RightTrigger - other.RightTrigger) > TRIGGER_NOISE_DEADBAND) return true;

        return false;
    }

    public readonly bool Equals(GamepadState other)
    {
        return LeftStickX == other.LeftStickX &&
               LeftStickY == other.LeftStickY &&
               RightStickX == other.RightStickX &&
               RightStickY == other.RightStickY &&
               LeftTrigger == other.LeftTrigger &&
               RightTrigger == other.RightTrigger &&
               HatSwitch == other.HatSwitch &&
               Buttons == other.Buttons;
    }

    public override readonly bool Equals(object? obj) => obj is GamepadState other && Equals(other);

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(LeftStickX);
        hash.Add(LeftStickY);
        hash.Add(RightStickX);
        hash.Add(RightStickY);
        hash.Add(LeftTrigger);
        hash.Add(RightTrigger);
        hash.Add(HatSwitch);
        hash.Add(Buttons);
        return hash.ToHashCode();
    }

    public static bool operator ==(GamepadState left, GamepadState right) => left.Equals(right);
    public static bool operator !=(GamepadState left, GamepadState right) => !left.Equals(right);

    public override readonly string ToString()
    {
        var pressed = new List<string>();
        if ((Buttons & (1 << 0)) != 0) pressed.Add("A");
        if ((Buttons & (1 << 1)) != 0) pressed.Add("B");
        if ((Buttons & (1 << 2)) != 0) pressed.Add("Btn3");
        if ((Buttons & (1 << 3)) != 0) pressed.Add("X");
        if ((Buttons & (1 << 4)) != 0) pressed.Add("Y");
        if ((Buttons & (1 << 5)) != 0) pressed.Add("Btn6");
        if ((Buttons & (1 << 6)) != 0) pressed.Add("LB");
        if ((Buttons & (1 << 7)) != 0) pressed.Add("RB");
        if ((Buttons & (1 << 8)) != 0) pressed.Add("LT");
        if ((Buttons & (1 << 9)) != 0) pressed.Add("RT");
        if ((Buttons & (1 << 10)) != 0) pressed.Add("Back");
        if ((Buttons & (1 << 11)) != 0) pressed.Add("Start");
        if ((Buttons & (1 << 12)) != 0) pressed.Add("Guide");
        if ((Buttons & (1 << 13)) != 0) pressed.Add("L3");
        if ((Buttons & (1 << 14)) != 0) pressed.Add("R3");

        string hatStr = HatSwitch switch
        {
            1 => "Up",
            2 => "Up-Right",
            3 => "Right",
            4 => "Down-Right",
            5 => "Down",
            6 => "Down-Left",
            7 => "Left",
            8 => "Up-Left",
            _ => "Neutral"
        };

        string btnStr = pressed.Count > 0 ? string.Join("+", pressed) : "None";
        return $"LS:({LeftStickX:D5},{LeftStickY:D5}) RS:({RightStickX:D5},{RightStickY:D5}) LT:{LeftTrigger:D3} RT:{RightTrigger:D3} DPad:{hatStr,-8} Btns:[{btnStr}]";
    }
}
