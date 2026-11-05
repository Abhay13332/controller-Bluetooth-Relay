namespace BluetoothGamepadBridge;

/// <summary>
/// Defines standard Bluetooth HID (Human Interface Device) descriptors for gamepads.
/// Fully matches Linux/Android kernel (hid-input.c) and Generic.kl mappings:
/// - Left Stick:     Usage X (0x30), Usage Y (0x31)       -> Linux ABS_X, ABS_Y (Left Thumbstick)
/// - Right Stick:    Usage Z (0x32), Usage Rz (0x35)      -> Linux ABS_Z, ABS_RZ (Right Thumbstick)
/// - Analog Triggers: Simulation Brake (0xC5), Gas (0xC4) -> Linux ABS_BRAKE, ABS_GAS (Android AXIS_BRAKE / AXIS_GAS / AXIS_LTRIGGER / AXIS_RTRIGGER)
/// - Hat Switch:     Usage Hat Switch (0x39)              -> Linux ABS_HAT0X, ABS_HAT0Y (D-Pad)
/// - 16 Buttons:     Button 1..16                         -> Linux BTN_A, BTN_B, BTN_X, BTN_Y, etc.
/// </summary>
public static class HidDescriptors
{
    /// <summary>
    /// HID Report Map Descriptor for a standard Gamepad (13 bytes total payload):
    /// - Offset 0..1: Left Stick X  (16-bit: 0..65535, Center = 32768) -> Usage X (0x30)
    /// - Offset 2..3: Left Stick Y  (16-bit: 0..65535, Center = 32768) -> Usage Y (0x31)
    /// - Offset 4..5: Right Stick X (16-bit: 0..65535, Center = 32768) -> Usage Z (0x32)
    /// - Offset 6..7: Right Stick Y (16-bit: 0..65535, Center = 32768) -> Usage Rz (0x35)
    /// - Offset 8:    Left Trigger  (8-bit:  0..255, Analog Degree)   -> Simulation Brake (0xC5 / LTRIGGER)
    /// - Offset 9:    Right Trigger (8-bit:  0..255, Analog Degree)   -> Simulation Accelerator (0xC4 / RTRIGGER)
    /// - Offset 10:   Hat Switch    (4-bit: 1..8 directional, 0 = neutral, 4-bit padding)
    /// - Offset 11..12: Buttons     (16-bit bitmask: A, B, X, Y, LB, RB, LT, RT, Back, Start, Guide, L3, R3)
    /// </summary>
    public static readonly byte[] GamepadReportMap = new byte[]
    {
        0x05, 0x01,        // USAGE_PAGE (Generic Desktop)
        0x09, 0x05,        // USAGE (Game Pad)
        0xA1, 0x01,        // COLLECTION (Application)
        0x85, 0x01,        //   REPORT_ID (1)

        // --- Left Thumbstick: X, Y (16-bit: 0..65535, center = 32768) -> 4 bytes ---
        0x05, 0x01,        //   USAGE_PAGE (Generic Desktop)
        0x09, 0x01,        //   USAGE (Pointer)
        0xA1, 0x00,        //   COLLECTION (Physical)
        0x09, 0x30,        //     USAGE (X - Left Stick X)
        0x09, 0x31,        //     USAGE (Y - Left Stick Y)
        0x15, 0x00,        //     LOGICAL_MINIMUM (0)
        0x27, 0xFF, 0xFF, 0x00, 0x00, // LOGICAL_MAXIMUM (65535)
        0x75, 0x10,        //     REPORT_SIZE (16)
        0x95, 0x02,        //     REPORT_COUNT (2)
        0x81, 0x02,        //     INPUT (Data, Var, Abs)
        0xC0,              //   END_COLLECTION

        // --- Right Thumbstick: Z, Rz (16-bit: 0..65535, center = 32768) -> 4 bytes ---
        0x05, 0x01,        //   USAGE_PAGE (Generic Desktop)
        0x09, 0x01,        //   USAGE (Pointer)
        0xA1, 0x00,        //   COLLECTION (Physical)
        0x09, 0x32,        //     USAGE (Z - Right Stick X)
        0x09, 0x35,        //     USAGE (Rz - Right Stick Y)
        0x15, 0x00,        //     LOGICAL_MINIMUM (0)
        0x27, 0xFF, 0xFF, 0x00, 0x00, // LOGICAL_MAXIMUM (65535)
        0x75, 0x10,        //     REPORT_SIZE (16)
        0x95, 0x02,        //     REPORT_COUNT (2)
        0x81, 0x02,        //     INPUT (Data, Var, Abs)
        0xC0,              //   END_COLLECTION

        // --- Analog Triggers: Left (Brake / 0xC5), Right (Accelerator / 0xC4) (8-bit: 0..255) -> 2 bytes ---
        0x05, 0x02,        //   USAGE_PAGE (Simulation Controls)
        0x09, 0xC5,        //   USAGE (Brake - Left Trigger / LTRIGGER)
        0x09, 0xC4,        //   USAGE (Accelerator - Right Trigger / RTRIGGER)
        0x15, 0x00,        //   LOGICAL_MINIMUM (0)
        0x26, 0xFF, 0x00,  //   LOGICAL_MAXIMUM (255)
        0x75, 0x08,        //   REPORT_SIZE (8)
        0x95, 0x02,        //   REPORT_COUNT (2)
        0x81, 0x02,        //   INPUT (Data, Var, Abs)

        // --- Hat Switch / D-Pad (4-bit value, 4-bit padding) -> 1 byte ---
        0x05, 0x01,        //   USAGE_PAGE (Generic Desktop)
        0x09, 0x39,        //   USAGE (Hat switch)
        0x15, 0x01,        //   LOGICAL_MINIMUM (1)
        0x25, 0x08,        //   LOGICAL_MAXIMUM (8)
        0x35, 0x00,        //   PHYSICAL_MINIMUM (0)
        0x46, 0x3B, 0x01,  //   PHYSICAL_MAXIMUM (315)
        0x66, 0x14, 0x00,  //   UNIT (English Rotation: Degrees)
        0x75, 0x04,        //   REPORT_SIZE (4)
        0x95, 0x01,        //   REPORT_COUNT (1)
        0x81, 0x42,        //   INPUT (Data, Var, Abs, Null)
        // 4-bit alignment padding
        0x75, 0x04,        //   REPORT_SIZE (4)
        0x95, 0x01,        //   REPORT_COUNT (1)
        0x15, 0x00,        //   LOGICAL_MINIMUM (0)
        0x25, 0x00,        //   LOGICAL_MAXIMUM (0)
        0x35, 0x00,        //   PHYSICAL_MINIMUM (0)
        0x45, 0x00,        //   PHYSICAL_MAXIMUM (0)
        0x65, 0x00,        //   UNIT (None)
        0x81, 0x03,        //   INPUT (Const, Var, Abs)

        // --- 16 Controller Buttons (16 bits) -> 2 bytes ---
        0x05, 0x09,        //   USAGE_PAGE (Button)
        0x19, 0x01,        //   USAGE_MINIMUM (Button 1)
        0x29, 0x10,        //   USAGE_MAXIMUM (Button 16)
        0x15, 0x00,        //   LOGICAL_MINIMUM (0)
        0x25, 0x01,        //   LOGICAL_MAXIMUM (1)
        0x75, 0x01,        //   REPORT_SIZE (1)
        0x95, 0x10,        //   REPORT_COUNT (16)
        0x81, 0x02,        //   INPUT (Data, Var, Abs)

        0xC0               // END_COLLECTION
    };

    // Bluetooth GATT UUID Constants
    public static readonly Guid HumanInterfaceDeviceServiceUuid = new("00001812-0000-1000-8000-00805F9B34FB");
    public static readonly Guid ReportCharacteristicUuid          = new("00002A4D-0000-1000-8000-00805F9B34FB");
    public static readonly Guid ReportMapCharacteristicUuid       = new("00002A4B-0000-1000-8000-00805F9B34FB");
    public static readonly Guid HidInformationCharacteristicUuid  = new("00002A4A-0000-1000-8000-00805F9B34FB");
    public static readonly Guid HidControlPointCharacteristicUuid = new("00002A4C-0000-1000-8000-00805F9B34FB");
    public static readonly Guid ProtocolModeCharacteristicUuid    = new("00002A4E-0000-1000-8000-00805F9B34FB");
    public static readonly Guid ReportReferenceDescriptorUuid     = new("00002908-0000-1000-8000-00805F9B34FB");
}
