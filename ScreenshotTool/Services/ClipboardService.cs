using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenshotTool.Services
{
    public static class ClipboardService
    {
        public static void CopyImage(BitmapSource bitmap)
        {
            if (bitmap != null)
            {
                // Explicitly use WPF Clipboard
                System.Windows.Clipboard.SetImage(bitmap);
            }
        }
    }
}
