"""
Standalone Xbox Controller (XInput) Test Script for Windows
Uses built-in ctypes with zero third-party dependencies.
Verifies your wired Xbox controller is recognized by Windows and displays live inputs.
"""

import sys
import time
import ctypes
from ctypes import wintypes

class XINPUT_GAMEPAD(ctypes.Structure):
    _fields_ = [
        ("wButtons", wintypes.WORD),
        ("bLeftTrigger", wintypes.BYTE),
        ("bRightTrigger", wintypes.BYTE),
        ("sThumbLX", wintypes.SHORT),
        ("sThumbLY", wintypes.SHORT),
        ("sThumbRX", wintypes.SHORT),
        ("sThumbRY", wintypes.SHORT),
    ]

class XINPUT_STATE(ctypes.Structure):
    _fields_ = [
        ("dwPacketNumber", wintypes.DWORD),
        ("Gamepad", XINPUT_GAMEPAD),
    ]

BUTTON_NAMES = {
    0x0001: "DPAD_UP",
    0x0002: "DPAD_DOWN",
    0x0004: "DPAD_LEFT",
    0x0008: "DPAD_RIGHT",
    0x0010: "START",
    0x0020: "BACK",
    0x0040: "LS_CLICK",
    0x0080: "RS_CLICK",
    0x0100: "LB",
    0x0200: "RB",
    0x0400: "GUIDE",
    0x1000: "A",
    0x2000: "B",
    0x4000: "X",
    0x8000: "Y",
}

def load_xinput():
    for dll_name in ("xinput1_4.dll", "xinput1_3.dll", "xinput9_1_0.dll"):
        try:
            xinput = ctypes.windll.LoadLibrary(dll_name)
            return xinput
        except OSError:
            continue
    return None

def main():
    print("=" * 60)
    print(" Xbox Controller Test Utility (XInput via ctypes)")
    print("=" * 60)

    if sys.platform != "win32":
        print("[INFO] This test script is designed to run directly on Windows.")
        return

    xinput = load_xinput()
    if not xinput:
        print("[ERROR] Could not load XInput DLL (xinput1_4 / xinput1_3 / xinput9_1_0).")
        return

    # Try ordinal 100 for Guide button support if available
    get_state = getattr(xinput, "XInputGetState", None)

    state = XINPUT_STATE()
    print("[INFO] Polling controller 0. Press buttons/move sticks (Ctrl+C to quit)...")

    while True:
        res = get_state(0, ctypes.byref(state))
        if res == 0:  # ERROR_SUCCESS
            gp = state.Gamepad
            pressed = [name for mask, name in BUTTON_NAMES.items() if (gp.wButtons & mask)]
            buttons_str = ",".join(pressed) if pressed else "None"
            print(f"\rLS:({gp.sThumbLX:6d},{gp.sThumbLY:6d}) RS:({gp.sThumbRX:6d},{gp.sThumbRY:6d}) "
                  f"LT:{gp.bLeftTrigger:3d} RT:{gp.bRightTrigger:3d} Buttons:[{buttons_str}]   ", end="", flush=True)
        else:
            print("\r[Waiting for wired Xbox controller to connect...]                     ", end="", flush=True)
        time.sleep(0.02)

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n[Exited]")

