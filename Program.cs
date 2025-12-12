using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

class Program
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private static bool _winIsDown = false;
    private static long _lastSwitchTick = 0;
    private static int _switchInFlight = 0;

    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    // Сканкоды для левых Alt/Shift: надежнее, чем VK_L*.
    private const ushort SC_LALT = 0x38;
    private const ushort SC_LSHIFT = 0x2A;

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinKeySwitch",
        "WinKeySwitch.log");

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    static void Main()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.WriteAllText(_logPath, $"[{DateTime.Now:O}] Start\r\n");
        }
        catch { }

        _hookID = SetHook(_proc);
        if (_hookID == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            Log($"SetWindowsHookEx failed: {err}");
            MessageBox.Show($"WinKeySwitch: не удалось установить хук клавиатуры (ошибка {err}).\nПопробуйте запустить от имени администратора.", "WinKeySwitch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run();
        UnhookWindowsHookEx(_hookID);
        Log("Exit");
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            // Для WH_KEYBOARD_LL можно передавать hMod = IntPtr.Zero.
            // Это часто надежнее для .NET-приложений.
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0);
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            int vkCode = Marshal.ReadInt32(lParam);

            if (vkCode == VK_LWIN || vkCode == VK_RWIN)
            {
                // Переключаем только при отпускании Win,
                // чтобы не было автоповтора и лагов/зависаний в приложениях.
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    _winIsDown = true;
                    return (IntPtr)1; // глушим Win-down
                }

                if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    if (_winIsDown)
                    {
                        _winIsDown = false;
                        QueueSwitchKeyboardLayout();
                    }

                    return (IntPtr)1; // глушим Win-up
                }
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private static void QueueSwitchKeyboardLayout()
    {
        long now = Environment.TickCount64;
        long last = Interlocked.Read(ref _lastSwitchTick);

        // Дребезг/повтор: игнорируем частые срабатывания
        if (now - last < 150)
            return;

        Interlocked.Exchange(ref _lastSwitchTick, now);

        // Не делаем user32-вызовы прямо из hook callback.
        if (Interlocked.Exchange(ref _switchInFlight, 1) == 1)
            return;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                SwitchKeyboardLayout();
            }
            catch (Exception ex)
            {
                Log($"Switch exception: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _switchInFlight, 0);
            }
        });
    }

    private static void SwitchKeyboardLayout()
    {
        // Эмулируем системную комбинацию Alt+Shift (как в настройках Windows).
        var inputs = new INPUT[]
        {
            ScanDown(SC_LALT),
            ScanDown(SC_LSHIFT),
            ScanUp(SC_LSHIFT),
            ScanUp(SC_LALT),
        };

        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        if (sent != inputs.Length)
        {
            int err = Marshal.GetLastWin32Error();
            Log($"SendInput sent {sent}/{inputs.Length}, err={err}");
        }
        else
        {
            Log("Switch OK");
        }
    }

    private static INPUT ScanDown(ushort scan) => new INPUT
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = scan,
                dwFlags = KEYEVENTF_SCANCODE,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        }
    };

    private static INPUT ScanUp(ushort scan) => new INPUT
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = scan,
                dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        }
    };

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(_logPath, $"[{DateTime.Now:O}] {message}\r\n");
        }
        catch { }
    }
}
