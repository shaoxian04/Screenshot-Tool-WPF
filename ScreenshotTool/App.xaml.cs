using System.Windows;
using System.Collections.Generic;
using System.Windows.Forms;
using ScreenshotTool.Services;

namespace ScreenshotTool
{
    public partial class App : System.Windows.Application
    {
        private List<MainWindow> _captureWindows = new List<MainWindow>();
        private HotkeyService? _hotkeyService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Initialize a dummy window to anchor the hotkey
            var dummy = new Window { Visibility = Visibility.Hidden };
            dummy.Show();
            
            _hotkeyService = new HotkeyService(dummy);
            _hotkeyService.HotkeyPressed += StartCapture;
            _hotkeyService.Register(0x0002 | 0x0004, 0x41); // Ctrl+Shift+A
        }

        public void StartCapture()
        {
            CloseWindows();

            // Create a capture window for every screen
            foreach (var screen in Screen.AllScreens)
            {
                var win = new MainWindow(screen);
                _captureWindows.Add(win);
                win.Show();
            }
        }

        public void CloseWindows()
        {
            foreach (var win in _captureWindows) win.Close();
            _captureWindows.Clear();
        }
    }
}
