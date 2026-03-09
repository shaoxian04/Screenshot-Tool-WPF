using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Media.Effects;
using System.Runtime.InteropServices;
using ScreenshotTool.Models;
using ScreenshotTool.Services;
using Screen = System.Windows.Forms.Screen;

namespace ScreenshotTool
{
    public partial class MainWindow : Window
    {
        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(System.Drawing.Point pt, uint dwFlags);

        private BitmapSource? _bgBitmap;
        private SKBitmap? _fullScreenSkia;
        private SKBitmap? _blurredSkia;
        private Rect _selectionRect; 
        private bool _isSelecting;
        private bool _isAnnotating;
        private string _activeTool = "Rectangle";
        private SKColor _currentColor = SKColors.Red;
        private List<AnnotationObject> _annotations = new List<AnnotationObject>();
        private System.Windows.Point _startPoint;
        private System.Windows.Point _currentPoint;
        private PathAnno? _currentPath;

        private AnnotationObject? _selectedObject;
        private int _draggingHandle = -1;
        private bool _isMovingObject;
        private System.Windows.Point _lastMousePos;
        private System.Windows.Controls.TextBox? _activeTextBox;

        private bool _isDraggingToolbar;
        private System.Windows.Point _toolbarDragStart;
        private Screen _screen;

        public MainWindow(Screen screen)
        {
            InitializeComponent();
            _screen = screen;
            
            IntPtr hMonitor = MonitorFromPoint(new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1), 2);
            GetDpiForMonitor(hMonitor, 0, out uint dpiX, out _);
            double scale = dpiX / 96.0;

            this.Left = screen.Bounds.Left / scale;
            this.Top = screen.Bounds.Top / scale;
            this.Width = screen.Bounds.Width / scale;
            this.Height = screen.Bounds.Height / scale;

            InitializeCapture();
        }

        private void OnDebugTrigger_Click(object sender, RoutedEventArgs e) => ((App)System.Windows.Application.Current).StartCapture();

        private void InitializeCapture()
        {
            ResetCapture();
            _bgBitmap = ScreenCaptureService.CaptureScreen(_screen);
            BackgroundCapture.Source = _bgBitmap;
            BackgroundCapture.Width = this.Width;
            BackgroundCapture.Height = this.Height;
            FullAreaGeometry.Rect = new Rect(0, 0, this.Width, this.Height);
            UpdateToolbarUI();
        }

        private void ResetCapture()
        {
            _isAnnotating = false; _isSelecting = false; CleanupTextBox();
            _selectionRect = Rect.Empty; _annotations.Clear(); _selectedObject = null;
            _fullScreenSkia?.Dispose(); _fullScreenSkia = null;
            _blurredSkia?.Dispose(); _blurredSkia = null;
            SelectionGeometry.Rect = Rect.Empty; DimmingPath.Visibility = Visibility.Visible;
            SkiaCanvas.Visibility = Visibility.Collapsed; ToolbarCanvas.Visibility = Visibility.Collapsed;
            TextEditCanvas.Children.Clear(); TextEditCanvas.Visibility = Visibility.Collapsed;
            SelectionFrame.Visibility = Visibility.Collapsed; SelectionFrameInner.Visibility = Visibility.Collapsed;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_activeTextBox != null) { FinalizeText(); return; }
            var pos = e.GetPosition(this);
            _startPoint = pos; _currentPoint = pos; _lastMousePos = pos;
            if (_isAnnotating) {
                // THE TEXT FIX: If text tool is active, immediately create editor here
                if (_activeTool == "Text") { CreateTextEditor(pos); return; }

                var skPos = new SKPoint((float)pos.X, (float)pos.Y);
                if (_selectedObject != null) { _draggingHandle = _selectedObject.GetHandleAtPoint(skPos); if (_draggingHandle != -1) return; }
                AnnotationObject? hit = null;
                for (int i = _annotations.Count - 1; i >= 0; i--) if (_annotations[i].HitTest(skPos)) { hit = _annotations[i]; break; }
                if (hit != null) { SelectObject(hit); _isMovingObject = true; return; }
                else { SelectObject(null); }
                if (_activeTool == "Freehand") { _currentPath = new PathAnno { Color = _currentColor, StrokeWidth = 3 }; _currentPath.Points.Add(skPos); }
            } else { 
                _isSelecting = true;
                SelectionFrame.Visibility = Visibility.Visible;
                SelectionFrameInner.Visibility = Visibility.Visible;
            }
        }

        private void SelectObject(AnnotationObject? obj)
        {
            if (_selectedObject != null) _selectedObject.IsSelected = false;
            _selectedObject = obj;
            if (_selectedObject != null) { _selectedObject.IsSelected = true; _currentColor = _selectedObject.Color; }
            UpdateToolbarUI();
            SkiaCanvas.InvalidateVisual();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_activeTextBox != null || _isDraggingToolbar) return;
            _currentPoint = e.GetPosition(this);
            var skPos = new SKPoint((float)_currentPoint.X, (float)_currentPoint.Y);
            var delta = new SKPoint((float)(_currentPoint.X - _lastMousePos.X), (float)(_currentPoint.Y - _lastMousePos.Y));
            if (_isSelecting) { _selectionRect = new Rect(_startPoint, _currentPoint); SelectionGeometry.Rect = _selectionRect; UpdateSelectionFrame(); }
            else if (_isAnnotating) {
                if (_draggingHandle != -1 && _selectedObject != null) _selectedObject.Resize(skPos, _draggingHandle);
                else if (_isMovingObject && _selectedObject != null) _selectedObject.Move(delta);
                else if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed && _activeTool == "Freehand" && _currentPath != null) _currentPath.Points.Add(skPos);
                SkiaCanvas.InvalidateVisual();
            }
            _lastMousePos = _currentPoint;
        }

        private void UpdateSelectionFrame()
        {
            Canvas.SetLeft(SelectionFrame, _selectionRect.Left); Canvas.SetTop(SelectionFrame, _selectionRect.Top);
            SelectionFrame.Width = _selectionRect.Width; SelectionFrame.Height = _selectionRect.Height;
            Canvas.SetLeft(SelectionFrameInner, _selectionRect.Left); Canvas.SetTop(SelectionFrameInner, _selectionRect.Top);
            SelectionFrameInner.Width = _selectionRect.Width; SelectionFrameInner.Height = _selectionRect.Height;
        }

        private void Window_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isSelecting) {
                _isSelecting = false;
                if (_selectionRect.Width < 5 && _selectionRect.Height < 5) { _selectionRect = new Rect(0, 0, this.Width, this.Height); SelectionGeometry.Rect = _selectionRect; UpdateSelectionFrame(); }
                SelectionFrame.Visibility = Visibility.Visible; SelectionFrameInner.Visibility = Visibility.Visible;
                if (_selectionRect.Width >= 5 && _selectionRect.Height >= 5) EnterAnnotationMode();
                else ((App)System.Windows.Application.Current).CloseWindows();
            } else if (_isAnnotating) {
                if (_draggingHandle == -1 && !_isMovingObject && System.Windows.Input.Mouse.LeftButton == MouseButtonState.Released && _activeTool != "Select" && _activeTool != "Text") FinalizeDrawing();
                _draggingHandle = -1; _isMovingObject = false;
                SkiaCanvas.InvalidateVisual();
            }
        }

        private void FinalizeDrawing()
        {
            SKRect rect = GetCurrentSkRect(); SKPoint start = new SKPoint((float)_startPoint.X, (float)_startPoint.Y); SKPoint end = new SKPoint((float)_currentPoint.X, (float)_currentPoint.Y);
            AnnotationObject? newObj = _activeTool switch {
                "Rectangle" => new RectangleAnno { Rect = rect, Color = _currentColor },
                "Arrow" => new ArrowAnno { Start = start, End = end, Color = _currentColor },
                "Blur" => new BlurAnno { Rect = rect },
                "Freehand" => _currentPath,
                _ => null
            };
            if (newObj != null) { _annotations.Add(newObj); _currentPath = null; SelectObject(newObj); }
        }

        private void EnterAnnotationMode()
        {
            _isAnnotating = true;
            if (_bgBitmap != null) {
                using (var stream = new MemoryStream()) {
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(_bgBitmap)); encoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin); _fullScreenSkia = SKBitmap.Decode(stream);
                }
                _blurredSkia = new SKBitmap(_fullScreenSkia.Info);
                using (var canvas = new SKCanvas(_blurredSkia))
                using (var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(15, 15) }) { canvas.DrawBitmap(_fullScreenSkia, 0, 0, paint); }
            }
            SkiaCanvas.Visibility = Visibility.Visible;
            TextEditCanvas.Visibility = Visibility.Visible; 
            ShowToolbar();
        }

        private void ShowToolbar()
        {
            ToolbarCanvas.Visibility = Visibility.Visible;
            double left = _selectionRect.Left + (_selectionRect.Width / 2) - 260; 
            double top = _selectionRect.Bottom + 10; 
            if (left < 10) left = 10; if (left + 520 > this.Width) left = this.Width - 530;
            if (top < 10) top = 10; if (top + 60 > this.Height) top = this.Height - 70;
            Canvas.SetLeft(ToolbarBorder, left); Canvas.SetTop(ToolbarBorder, top);
        }

        private void Toolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { _isDraggingToolbar = true; _toolbarDragStart = e.GetPosition(ToolbarBorder); ToolbarBorder.CaptureMouse(); e.Handled = true; }
        private void Toolbar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) { if (_isDraggingToolbar) { var pos = e.GetPosition(this); Canvas.SetLeft(ToolbarBorder, pos.X - _toolbarDragStart.X); Canvas.SetTop(ToolbarBorder, pos.Y - _toolbarDragStart.Y); } }
        private void Toolbar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { _isDraggingToolbar = false; ToolbarBorder.ReleaseMouseCapture(); }

        private void SkiaCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas; canvas.Clear(SKColors.Transparent);
            if (_fullScreenSkia == null) return;
            float scaleX = (float)(e.Info.Width / this.Width); float scaleY = (float)(e.Info.Height / this.Height);
            canvas.Scale(scaleX, scaleY);
            canvas.Save(); canvas.ClipRect(new SKRect((float)_selectionRect.Left, (float)_selectionRect.Top, (float)_selectionRect.Right, (float)_selectionRect.Bottom));
            if (_blurredSkia != null) { foreach (var anno in _annotations) if (anno is BlurAnno blur) { canvas.Save(); canvas.ClipRect(blur.Rect); canvas.DrawBitmap(_blurredSkia, 0, 0); canvas.Restore(); } if (IsDrawing() && _activeTool == "Blur") { canvas.Save(); canvas.ClipRect(GetCurrentSkRect()); canvas.DrawBitmap(_blurredSkia, 0, 0); canvas.Restore(); } }
            foreach (var anno in _annotations) anno.Draw(canvas);
            if (IsDrawing()) DrawPreview(canvas);
            canvas.Restore();
        }

        private void DrawPreview(SKCanvas canvas)
        {
            using (var paint = new SKPaint { Color = _currentColor, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true }) {
                SKRect current = GetCurrentSkRect(); SKPoint start = new SKPoint((float)_startPoint.X, (float)_startPoint.Y); SKPoint end = new SKPoint((float)_currentPoint.X, (float)_currentPoint.Y);
                switch (_activeTool) { case "Rectangle": canvas.DrawRect(current, paint); break; case "Arrow": new ArrowAnno { Start = start, End = end, Color = _currentColor }.Draw(canvas); break; case "Freehand": _currentPath?.Draw(canvas); break; }
            }
        }

        private bool IsDrawing() => (_isAnnotating && System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed && _activeTool != "Text" && _activeTool != "Select" && _draggingHandle == -1 && !_isMovingObject);
        private SKRect GetCurrentSkRect() => new SKRect((float)Math.Min(_startPoint.X, _currentPoint.X), (float)Math.Min(_startPoint.Y, _currentPoint.Y), (float)Math.Max(_startPoint.X, _currentPoint.X), (float)Math.Max(_startPoint.Y, _currentPoint.Y));

        private void CopyToClipboard()
        {
            SelectObject(null);
            double scaleX = _fullScreenSkia!.Width / this.Width; double scaleY = _fullScreenSkia!.Height / this.Height;
            var info = new SKImageInfo((int)(_selectionRect.Width * scaleX), (int)(_selectionRect.Height * scaleY));
            using (var surface = SKSurface.Create(info)) {
                var canvas = surface.Canvas; canvas.Translate((float)-(_selectionRect.Left * scaleX), (float)-(_selectionRect.Top * scaleY));
                canvas.DrawBitmap(_fullScreenSkia, 0, 0); canvas.Scale((float)scaleX, (float)scaleY);
                if (_blurredSkia != null) foreach (var anno in _annotations) if (anno is BlurAnno b) { canvas.Save(); canvas.ClipRect(b.Rect); canvas.DrawBitmap(_blurredSkia, 0, 0); canvas.Restore(); }
                foreach (var anno in _annotations) anno.Draw(canvas);
                using (var image = surface.Snapshot()) using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) using (var stream = new MemoryStream()) { data.SaveTo(stream); stream.Seek(0, SeekOrigin.Begin); var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.StreamSource = stream; bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit(); bitmap.Freeze(); ClipboardService.CopyImage(bitmap); }
            }
        }

        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            CleanupTextBox();
            if (sender is System.Windows.Controls.Button b && b.Tag is string tool)
            {
                _activeTool = tool;
                UpdateToolbarUI();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e) { if (_annotations.Count > 0) _annotations.RemoveAt(_annotations.Count - 1); SelectObject(null); SkiaCanvas.InvalidateVisual(); }
        private void Done_Click(object sender, RoutedEventArgs e) { CleanupTextBox(); CopyToClipboard(); ((App)System.Windows.Application.Current).CloseWindows(); }
        private void Exit_Click(object sender, RoutedEventArgs e) { CleanupTextBox(); ((App)System.Windows.Application.Current).CloseWindows(); }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Tag is string colorName) {
                _currentColor = colorName switch { "Red" => SKColors.Red, "Green" => SKColors.Green, "Blue" => SKColors.Blue, "Yellow" => SKColors.Yellow, "Black" => SKColors.Black, _ => SKColors.Red };
                if (_selectedObject != null) { _selectedObject.Color = _currentColor; SkiaCanvas.InvalidateVisual(); }
                UpdateToolbarUI();
            }
        }

        private void UpdateToolbarUI()
        {
            var toolButtons = new Dictionary<string, System.Windows.Controls.Button> { { "Arrow", ToolArrow }, { "Rectangle", ToolRect }, { "Freehand", ToolPen }, { "Blur", ToolBlur }, { "Text", ToolText } };
            foreach (var kvp in toolButtons) if (kvp.Value != null) kvp.Value.Background = (_activeTool == kvp.Key) ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 120, 215)) : System.Windows.Media.Brushes.Transparent;
        }

        private void CreateTextEditor(System.Windows.Point position)
        {
            CleanupTextBox();
            _activeTextBox = new System.Windows.Controls.TextBox { 
                MinWidth = 100, MinHeight = 40, AcceptsReturn = true, Background = System.Windows.Media.Brushes.White,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(_currentColor.Alpha, _currentColor.Red, _currentColor.Green, _currentColor.Blue)), 
                BorderThickness = new Thickness(2), BorderBrush = System.Windows.Media.Brushes.DodgerBlue,
                FontSize = 24, FontWeight = FontWeights.Bold, Padding = new Thickness(5), CaretBrush = System.Windows.Media.Brushes.Black
            };
            Canvas.SetLeft(_activeTextBox, position.X); Canvas.SetTop(_activeTextBox, position.Y); 
            TextEditCanvas.Children.Add(_activeTextBox); 
            _activeTextBox.Focus();
            _activeTextBox.KeyDown += (s, ev) => { if (ev.Key == Key.Enter && (System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) FinalizeText(); else if (ev.Key == Key.Escape) CleanupTextBox(); };
            _activeTextBox.LostFocus += (s, ev) => { if (_activeTextBox != null) FinalizeText(); };
        }

        private void FinalizeText()
        {
            if (_activeTextBox != null) {
                if (!string.IsNullOrWhiteSpace(_activeTextBox.Text)) {
                    var pos = new System.Windows.Point(Canvas.GetLeft(_activeTextBox), Canvas.GetTop(_activeTextBox));
                    var anno = new TextAnno { Text = _activeTextBox.Text, Position = new SKPoint((float)pos.X + 5, (float)pos.Y + 30), Color = _currentColor, FontSize = 24 };
                    _annotations.Add(anno); SelectObject(anno); SkiaCanvas.InvalidateVisual();
                }
                CleanupTextBox();
            }
        }

        private void CleanupTextBox() { if (_activeTextBox != null) { TextEditCanvas.Children.Remove(_activeTextBox); _activeTextBox = null; } this.Focus(); }

        private void TextEditCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { } // Not used anymore

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (_activeTextBox != null) return;
            if (e.Key == Key.Escape) ((App)System.Windows.Application.Current).CloseWindows();
            else if (e.Key == Key.Enter && _isAnnotating) { CopyToClipboard(); ((App)System.Windows.Application.Current).CloseWindows(); }
            else if (e.Key == Key.Delete && _selectedObject != null) { _annotations.Remove(_selectedObject); SelectObject(null); }
            else if (e.Key == Key.Z && (System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) { Undo_Click(null!, null!); }
            base.OnKeyDown(e);
        }
    }
}
