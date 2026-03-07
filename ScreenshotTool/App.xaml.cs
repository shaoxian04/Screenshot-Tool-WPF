using System.Windows;

namespace ScreenshotTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Ensure the app doesn't close when we hide the window
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
    }
}
