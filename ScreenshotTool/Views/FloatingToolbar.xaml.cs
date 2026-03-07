using System.Windows;
using System.Windows.Controls;

namespace ScreenshotTool.Views
{
    public partial class FloatingToolbar : Window
    {
        public event Action<string>? ToolSelected;
        public event Action? UndoRequested;
        public event Action? DoneClicked;
        public event Action? ExitClicked;

        public FloatingToolbar()
        {
            InitializeComponent();
        }

        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tool)
            {
                ToolSelected?.Invoke(tool);
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            UndoRequested?.Invoke();
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            DoneClicked?.Invoke();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitClicked?.Invoke();
        }
    }
}
