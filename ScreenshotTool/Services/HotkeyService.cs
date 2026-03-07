using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenshotTool.Services
{
    public class HotkeyService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private readonly Window _window;
        private HwndSource? _source;
        private readonly int _hotkeyId = 9000;

        public event Action? HotkeyPressed;

        public HotkeyService(Window window)
        {
            _window = window;
        }

        public void Register(uint modifiers, uint key)
        {
            var helper = new WindowInteropHelper(_window);
            var hWnd = helper.Handle;

            Console.WriteLine($">>> HotkeyService: Attempting to register ID {_hotkeyId} on HWND {hWnd}...");

            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine(">>> HotkeyService ERROR: Window Handle is ZERO. Registration will fail.");
                return;
            }

            _source = HwndSource.FromHwnd(hWnd);
            _source.AddHook(HwndHook);

            bool result = RegisterHotKey(hWnd, _hotkeyId, modifiers, key);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($">>> HotkeyService ERROR: RegisterHotKey failed with Win32 Error Code: {error}");
                throw new Exception($"Hotkey already in use (Error {error}).");
            }
            
            Console.WriteLine($">>> HotkeyService SUCCESS: Hotkey registered successfully.");
        }

        public void Unregister()
        {
            var helper = new WindowInteropHelper(_window);
            UnregisterHotKey(helper.Handle, _hotkeyId);
            _source?.RemoveHook(HwndHook);
            Console.WriteLine(">>> HotkeyService: Hotkey unregistered.");
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                Console.WriteLine($">>> HotkeyService: WM_HOTKEY received for ID {id}");
                if (id == _hotkeyId)
                {
                    HotkeyPressed?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
