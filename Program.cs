using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BluetoothGamepadBridge;

public static class Program
{
    private static bool _running = true;
    private static bool _showInputMonitor = true;
    private static int _pollIntervalMs = 4; // 250 Hz polling

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint uMilliseconds);

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Force 1ms OS timer resolution on Windows
        TimeBeginPeriod(1);

        bool backgroundMode = args.Any(a => a.Equals("--background", StringComparison.OrdinalIgnoreCase) || a.Equals("-b", StringComparison.OrdinalIgnoreCase));
        bool testOnly = args.Any(a => a.Equals("--test", StringComparison.OrdinalIgnoreCase) || a.Equals("-t", StringComparison.OrdinalIgnoreCase));
        bool infoOnly = args.Any(a => a.Equals("--info", StringComparison.OrdinalIgnoreCase) || a.Equals("-i", StringComparison.OrdinalIgnoreCase));
        bool noInterceptor = args.Any(a => a.Equals("--no-swallow", StringComparison.OrdinalIgnoreCase));

        var initialLayout = GamepadLayoutMode.XboxStandard;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--layout", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                string val = args[i + 1].ToLowerInvariant();
                if (val.Contains("ps") || val.Contains("playstation") || val.Contains("sony"))
                    initialLayout = GamepadLayoutMode.PlayStation;
                else if (val.Contains("raw") || val.Contains("seq"))
                    initialLayout = GamepadLayoutMode.RawSequential;
                else
                    initialLayout = GamepadLayoutMode.XboxStandard;
            }
            if (args[i].Equals("--rate", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[i + 1], out int rate) && rate >= 1)
            {
                _pollIntervalMs = rate;
            }
        }

        if (infoOnly)
        {
            await RunInfoOnlyAsync();
            TimeEndPeriod(1);
            return;
        }

        if (testOnly)
        {
            RunTestOnly();
            TimeEndPeriod(1);
            return;
        }

        PrintHeader(backgroundMode);
        CheckRunningSoftware();

        // 1. Enable Input Interceptor
        if (!noInterceptor)
        {
            InputInterceptor.Enable();
            if (!backgroundMode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Input Interceptor] Active! Swallowing synthetic keystrokes, mouse moves, and clicks.");
                Console.WriteLine("                    Xbox Guide GameBar capture suppressed.");
                Console.WriteLine("                    Physical keyboard & mouse are 100% usable.");
                Console.ResetColor();
            }
        }

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            _running = false;
        };

        BleGamepadServer? bleServerInstance = null;
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            try
            {
                InputInterceptor.Disable();
                bleServerInstance?.Dispose();
                TimeEndPeriod(1);
            }
            catch { }
        };

        // 2. Initialize Controller
        var controller = new XInputController(0)
        {
            CurrentLayout = initialLayout
        };

        if (!backgroundMode)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[Step 1/2] Connecting to Xbox Controller (Layout: {controller.CurrentLayout})...");
            Console.ResetColor();
        }

        // 3. Initialize Bluetooth GATT Server
        if (!backgroundMode)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[Step 2/2] Starting Bluetooth LE HID Gamepad Server (Report ID 1, 250 Hz)...");
            Console.ResetColor();
        }

        using var bleServer = new BleGamepadServer();
        bleServerInstance = bleServer;
        bleServer.LogMessage += msg =>
        {
            if (!backgroundMode)
            {
                Console.WriteLine(msg);
            }
        };

        bool serverStarted = await bleServer.StartAsync();
        if (!serverStarted)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[FAILURE] Could not start Bluetooth Gamepad Server.");
            Console.ResetColor();
            InputInterceptor.Disable();
            TimeEndPeriod(1);
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[READY] PC is broadcasting as a Bluetooth Gamepad!");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(">> Instructions for Android:");
        Console.WriteLine("   1. If previously paired: unpair / forget this PC in Android Bluetooth settings!");
        Console.WriteLine("   2. Scan and re-pair so Android refreshes the HID report layout.");
        Console.WriteLine("   3. On PC Console, press [1] for Xbox layout or [2] for PlayStation layout!");
        Console.ResetColor();
        Console.WriteLine(new string('-', 72));

        if (!backgroundMode)
        {
            Console.WriteLine("Hotkeys: [1] Xbox Standard | [2] PlayStation | [3] Raw Sequential | [4] Swapped X/Y | [Q] Quit\n");
        }

        GamepadState lastState = GamepadState.Default;
        bool lastConnected = false;
        var lastKeepAlive = DateTime.UtcNow;
        var lastUiUpdate = DateTime.UtcNow;

        while (_running)
        {
            // Process hotkeys
            if (!backgroundMode && Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    _running = false;
                    break;
                }
                if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                {
                    controller.CurrentLayout = GamepadLayoutMode.XboxStandard;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n[Layout Switched] -> XBOX STANDARD LAYOUT (A=1, B=2, X=4, Y=5, LB=7, RB=8...)");
                    Console.ResetColor();
                }
                if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                {
                    controller.CurrentLayout = GamepadLayoutMode.PlayStation;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("\n[Layout Switched] -> PLAYSTATION LAYOUT (Square=1, Cross=2, Circle=3, Triangle=4...)");
                    Console.ResetColor();
                }
                if (key.Key == ConsoleKey.D3 || key.Key == ConsoleKey.NumPad3)
                {
                    controller.CurrentLayout = GamepadLayoutMode.RawSequential;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[Layout Switched] -> RAW SEQUENTIAL LAYOUT (1 to 16 direct)");
                    Console.ResetColor();
                }
                if (key.Key == ConsoleKey.D4 || key.Key == ConsoleKey.NumPad4)
                {
                    controller.CurrentLayout = GamepadLayoutMode.XboxSwappedXY;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\n[Layout Switched] -> XBOX SWAPPED X/Y LAYOUT (X=5, Y=4 for Nintendo/Inverted apps)");
                    Console.ResetColor();
                }
                if (key.Key == ConsoleKey.L)
                {
                    controller.CurrentLayout = controller.CurrentLayout switch
                    {
                        GamepadLayoutMode.XboxStandard => GamepadLayoutMode.PlayStation,
                        GamepadLayoutMode.PlayStation  => GamepadLayoutMode.RawSequential,
                        GamepadLayoutMode.RawSequential => GamepadLayoutMode.XboxSwappedXY,
                        _                             => GamepadLayoutMode.XboxStandard
                    };
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n[Layout Switched] -> {controller.CurrentLayout.ToString().ToUpperInvariant()}");
                    Console.ResetColor();
                }
                if (key.Key == ConsoleKey.M)
                {
                    _showInputMonitor = !_showInputMonitor;
                    Console.WriteLine(_showInputMonitor ? "\n[Monitor Enabled]" : "\n[Monitor Disabled]");
                }
            }

            // High-speed controller poll (250 Hz)
            bool stateChanged = controller.Poll(out var currentState);

            if (controller.IsConnected != lastConnected)
            {
                lastConnected = controller.IsConnected;
                if (!backgroundMode)
                {
                    if (controller.IsConnected)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[Xbox Controller] Connected! ({controller.ControllerName})");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n[Xbox Controller] Disconnected. Waiting for controller...");
                        Console.ResetColor();
                    }
                }
            }

            if (controller.IsConnected)
            {
                bool buttonChanged = (currentState.Buttons != lastState.Buttons) || (currentState.HatSwitch != lastState.HatSwitch);
                bool axisSignificant = currentState.HasSignificantChangeFrom(lastState);
                bool keepAliveDue = (DateTime.UtcNow - lastKeepAlive).TotalMilliseconds >= 100;

                if (buttonChanged || axisSignificant || keepAliveDue)
                {
                    bleServer.QueueReport(currentState);
                    lastState = currentState;
                    lastKeepAlive = DateTime.UtcNow;
                }

                if (_showInputMonitor && !backgroundMode && (DateTime.UtcNow - lastUiUpdate).TotalMilliseconds >= 250)
                {
                    lastUiUpdate = DateTime.UtcNow;
                    string layoutTag = controller.CurrentLayout switch
                    {
                        GamepadLayoutMode.PlayStation   => "PS",
                        GamepadLayoutMode.RawSequential => "RAW",
                        GamepadLayoutMode.XboxSwappedXY => "SWAP-XY",
                        _                               => "XBOX"
                    };

                    string interceptInfo = (InputInterceptor.InterceptedKeysCount > 0 || InputInterceptor.InterceptedMouseEventsCount > 0)
                        ? $"|Swallowed: {InputInterceptor.InterceptedKeysCount}k/{InputInterceptor.InterceptedMouseEventsCount}m"
                        : "";

                    string subInfo = bleServer.SubscribedClientCount > 0
                        ? $"[{layoutTag}|Android: {bleServer.SubscribedClientCount}|Sent: #{bleServer.TotalPacketsSent:D5}{interceptInfo}]"
                        : $"[{layoutTag}|Waiting Android...{interceptInfo}]";

                    Console.Write($"\r{subInfo} {currentState,-60}");
                }
            }

            Thread.Sleep(_pollIntervalMs);
        }

        Console.WriteLine("\n[Shutdown] Cleaning up...");
        InputInterceptor.Disable();
        bleServer.Stop();
        TimeEndPeriod(1);
    }

    private static void CheckRunningSoftware()
    {
        try
        {
            var processes = Process.GetProcesses();
            bool steamRunning = processes.Any(p => p.ProcessName.Equals("steam", StringComparison.OrdinalIgnoreCase));
            if (steamRunning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[INFO] Steam is detected in the background.");
                Console.WriteLine("       Our interceptor is actively swallowing its synthetic keystrokes & mouse cursor moves.");
                Console.WriteLine("       (Tip: You can also disable 'Desktop Layout' in Steam Settings -> Controller)");
                Console.ResetColor();
            }
        }
        catch { }
    }

    private static void PrintHeader(bool background)
    {
        if (background) return;

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("======================================================================");
        Console.WriteLine("   Xbox Controller -> Android Bluetooth Gamepad Bridge (Multi-Layout)");
        Console.WriteLine("======================================================================");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(" * Hotkey [1]: Xbox Standard Layout (Android Generic.kl)");
        Console.WriteLine(" * Hotkey [2]: PlayStation / DualShock Layout (PS Mode)");
        Console.WriteLine(" * Hotkey [3]: Raw Sequential Layout (1 to 16 direct)");
        Console.WriteLine(" * Desktop Keystroke Interceptor active: Windows OS will not be affected");
        Console.WriteLine("======================================================================\n");
        Console.ResetColor();
    }

    private static async Task RunInfoOnlyAsync()
    {
        Console.WriteLine("Checking PC Bluetooth Adapter Capabilities...\n");
        var (available, peripheralSupported, adapterInfo) = await BleGamepadServer.CheckAdapterSupportAsync();
        Console.WriteLine(adapterInfo);
        Console.WriteLine($"Bluetooth Available: {available}");
        Console.WriteLine($"BLE Peripheral Role Supported: {peripheralSupported}");
    }

    private static void RunTestOnly()
    {
        Console.WriteLine("Running in Controller Test Mode (Press Ctrl+C to exit)...");
        var controller = new XInputController(0);

        while (true)
        {
            controller.Poll(out var state);
            if (controller.IsConnected)
            {
                Console.Write($"\r[TEST] {state,-80}");
            }
            else
            {
                Console.Write("\r[TEST] Waiting for wired Xbox controller...                         ");
            }
            Thread.Sleep(20);
        }
    }
}
