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
using ScreenshotTool.Models;
using ScreenshotTool.Services;

namespace ScreenshotTool
{
    public partial class MainWindow : Window
    {
        private BitmapSource? _fullScreenBitmap;
        private SKBitmap? _fullScreenSkia;
        private Rect _selectionRect;
        private bool _isSelecting;
        private bool _isAnnotating;
        private string _activeTool = "Rectangle";
        private SKColor _currentColor = SKColors.Red;
        private List<AnnotationObject> _annotations = new List<AnnotationObject>();
        private Point _startPoint;
        private Point _currentPoint;
        private PathAnno? _currentPath;
        private HotkeyService? _hotkeyService;

        private AnnotationObject? _selectedObject;
        private int _draggingHandle = -1;
        private bool _isMovingObject;
        private Point _lastMousePos;
        private TextBox? _activeTextBox;

        public MainWindow()
        {
            InitializeComponent();
            new WindowInteropHelper(this).EnsureHandle();
            try {
                _hotkeyService = new HotkeyService(this);
                _hotkeyService.HotkeyPressed += () => this.Dispatcher.Invoke(OnStartCapture);
                _hotkeyService.Register(0x0002 | 0x0004, 0x41);
            } catch { }
            this.Hide();
        }

        private void OnDebugTrigger_Click(object sender, RoutedEventArgs e) => OnStartCapture();

        private void OnStartCapture()
        {
            DebugTrigger.Visibility = Visibility.Collapsed;
            ResetCapture();
            _fullScreenBitmap = ScreenCaptureService.CaptureFullScreen();
            BackgroundCapture.Source = _fullScreenBitmap;
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            FullAreaGeometry.Rect = new Rect(0, 0, this.Width, this.Height);
            this.Show(); this.Activate(); this.Focus(); this.Topmost = true;
            UpdateToolbarUI();
        }

        private void ResetCapture()
        {
            _isAnnotating = false; _isSelecting = false; CleanupTextBox();
            _selectionRect = Rect.Empty; _annotations.Clear(); _selectedObject = null;
            _fullScreenSkia?.Dispose(); _fullScreenSkia = null;
            SelectionGeometry.Rect = Rect.Empty; DimmingPath.Visibility = Visibility.Visible;
            SkiaCanvas.Visibility = Visibility.Collapsed; ToolbarCanvas.Visibility = Visibility.Collapsed;
            TextEditCanvas.Children.Clear();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_activeTextBox != null) { FinalizeText(); return; }
            var pos = e.GetPosition(this);
            _startPoint = pos; _currentPoint = pos; _lastMousePos = pos;

            if (_isAnnotating)
            {
                var skPos = new SKPoint((float)pos.X, (float)pos.Y);

                // Check for handle selection
                if (_selectedObject != null)
                {
                    _draggingHandle = _selectedObject.GetHandleAtPoint(skPos);
                    if (_draggingHandle != -1) return;
                }

                // Check for object selection
                AnnotationObject? hit = null;
                for (int i = _annotations.Count - 1; i >= 0; i--) if (_annotations[i].HitTest(skPos)) { hit = _annotations[i]; break; }

                if (hit != null) { SelectObject(hit); _isMovingObject = true; return; }
                else { SelectObject(null); }

                // Drawing triggers
                if (_activeTool == "Text") { CreateTextEditor(pos); return; }
                if (_activeTool == "Freehand") { _currentPath = new PathAnno { Color = _currentColor, StrokeWidth = 3 }; _currentPath.Points.Add(skPos); }
            }
            else { _isSelecting = true; }
        }

        private void SelectObject(AnnotationObject? obj)
        {
            if (_selectedObject != null) _selectedObject.IsSelected = false;
            _selectedObject = obj;
            if (_selectedObject != null) { _selectedObject.IsSelected = true; _currentColor = _selectedObject.Color; }
            UpdateToolbarUI();
            SkiaCanvas.InvalidateVisual();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_activeTextBox != null) return;
            _currentPoint = e.GetPosition(this);
            var skPos = new SKPoint((float)_currentPoint.X, (float)_currentPoint.Y);
            var delta = new SKPoint((float)(_currentPoint.X - _lastMousePos.X), (float)(_currentPoint.Y - _lastMousePos.Y));

            if (_isSelecting) { _selectionRect = new Rect(_startPoint, _currentPoint); SelectionGeometry.Rect = _selectionRect; }
            else if (_isAnnotating)
            {
                if (_draggingHandle != -1 && _selectedObject != null) _selectedObject.Resize(skPos, _draggingHandle);
                else if (_isMovingObject && _selectedObject != null) _selectedObject.Move(delta);
                else if (Mouse.LeftButton == MouseButtonState.Pressed && _activeTool == "Freehand" && _currentPath != null) _currentPath.Points.Add(skPos);
                SkiaCanvas.InvalidateVisual();
            }
            _lastMousePos = _currentPoint;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting) {
                _isSelecting = false;
                if (_selectionRect.Width > 5 && _selectionRect.Height > 5) EnterAnnotationMode();
                else { this.Hide(); DebugTrigger.Visibility = Visibility.Visible; }
            } else if (_isAnnotating) {
                if (_draggingHandle == -1 && !_isMovingObject && Mouse.LeftButton == MouseButtonState.Released && _activeTool != "Select") FinalizeDrawing();
                _draggingHandle = -1; _isMovingObject = false;
                SkiaCanvas.InvalidateVisual();
            }
        }

        private void FinalizeDrawing()
        {
            if (_activeTool == "Text") return;
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
            if (_fullScreenBitmap != null) {
                using (var stream = new MemoryStream()) {
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(_fullScreenBitmap)); encoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin); _fullScreenSkia = SKBitmap.Decode(stream);
                }
            }
            SkiaCanvas.Visibility = Visibility.Visible; ShowToolbar();
        }

        private void ShowToolbar()
        {
            ToolbarCanvas.Visibility = Visibility.Visible;
            double left = _selectionRect.Left; double top = _selectionRect.Bottom + 10;
            if (top + 80 > this.Height) top = _selectionRect.Top - 80;
            Canvas.SetLeft(ToolbarBorder, left); Canvas.SetTop(ToolbarBorder, top);
        }

        private void SkiaCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas; canvas.Clear(SKColors.Transparent);
            if (_fullScreenSkia == null) return;
            float scaleX = (float)(e.Info.Width / this.Width); float scaleY = (float)(e.Info.Height / this.Height);
            canvas.Scale(scaleX, scaleY);
            canvas.Save(); canvas.ClipRect(new SKRect((float)_selectionRect.Left, (float)_selectionRect.Top, (float)_selectionRect.Right, (float)_selectionRect.Bottom));
            
            // Draw Blur Layer
            foreach (var anno in _annotations) if (anno is BlurAnno blur) DrawBlurEffect(canvas, blur.Rect);
            if (IsDrawing() && _activeTool == "Blur") DrawBlurEffect(canvas, GetCurrentSkRect());
            
            // Draw Objects (Rectangle, Arrow, Text, Pen)
            foreach (var anno in _annotations) anno.Draw(canvas);
            
            // Draw Preview
            if (IsDrawing()) DrawPreview(canvas);
            
            canvas.Restore();
        }

        private void DrawBlurEffect(SKCanvas canvas, SKRect rect)
        {
            if (_fullScreenSkia == null) return;
            using (var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(15, 15) }) {
                canvas.Save(); canvas.ClipRect(rect); canvas.DrawBitmap(_fullScreenSkia, new SKRect(0, 0, (float)this.Width, (float)this.Height), paint); canvas.Restore();
            }
        }

        private void DrawPreview(SKCanvas canvas)
        {
            using (var paint = new SKPaint { Color = _currentColor, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true }) {
                SKRect current = GetCurrentSkRect(); SKPoint start = new SKPoint((float)_startPoint.X, (float)_startPoint.Y); SKPoint end = new SKPoint((float)_currentPoint.X, (float)_currentPoint.Y);
                switch (_activeTool) {
                    case "Rectangle": canvas.DrawRect(current, paint); break;
                    case "Arrow": new ArrowAnno { Start = start, End = end, Color = _currentColor }.Draw(canvas); break;
                    case "Freehand": _currentPath?.Draw(canvas); break;
                }
            }
        }

        private bool IsDrawing() => (_isAnnotating && Mouse.LeftButton == MouseButtonState.Pressed && _activeTool != "Text" && _draggingHandle == -1 && !_isMovingObject);
        private SKRect GetCurrentSkRect() => new SKRect((float)Math.Min(_startPoint.X, _currentPoint.X), (float)Math.Min(_startPoint.Y, _currentPoint.Y), (float)Math.Max(_startPoint.X, _currentPoint.X), (float)Math.Max(_startPoint.Y, _currentPoint.Y));

        private void CopyToClipboard()
        {
            SelectObject(null);
            Matrix transform = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            Point physicalTopLeft = transform.Transform(new Point(_selectionRect.Left, _selectionRect.Top));
            Point physicalSize = transform.Transform(new Point(_selectionRect.Width, _selectionRect.Height));
            var info = new SKImageInfo((int)physicalSize.X, (int)physicalSize.Y);
            using (var surface = SKSurface.Create(info)) {
                var canvas = surface.Canvas; canvas.Translate((float)-physicalTopLeft.X, (float)-physicalTopLeft.Y);
                canvas.DrawBitmap(_fullScreenSkia, 0, 0); canvas.Scale((float)transform.M11, (float)transform.M22);
                foreach (var anno in _annotations) if (anno is BlurAnno blur) DrawBlurEffect(canvas, blur.Rect);
                foreach (var anno in _annotations) anno.Draw(canvas);
                using (var image = surface.Snapshot()) using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) using (var stream = new MemoryStream()) {
                    data.SaveTo(stream); stream.Seek(0, SeekOrigin.Begin);
                    var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.StreamSource = stream; bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit(); bitmap.Freeze();
                    ClipboardService.CopyImage(bitmap);
                }
            }
        }

        private void Tool_Click(object sender, RoutedEventArgs e) { 
            CleanupTextBox();
            if (sender is Button b && b.Tag is string tool) { _activeTool = tool; UpdateToolbarUI(); } 
        }
        private void Undo_Click(object sender, RoutedEventArgs e) { if (_annotations.Count > 0) _annotations.RemoveAt(_annotations.Count - 1); SelectObject(null); SkiaCanvas.InvalidateVisual(); }
        private void Done_Click(object sender, RoutedEventArgs e) { CleanupTextBox(); CopyToClipboard(); this.Hide(); DebugTrigger.Visibility = Visibility.Visible; }
        private void Exit_Click(object sender, RoutedEventArgs e) { CleanupTextBox(); this.Hide(); DebugTrigger.Visibility = Visibility.Visible; }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string colorName) {
                _currentColor = colorName switch { "Red" => SKColors.Red, "Green" => SKColors.Green, "Blue" => SKColors.Blue, "Yellow" => SKColors.Yellow, "Black" => SKColors.Black, _ => SKColors.Red };
                if (_selectedObject != null) { _selectedObject.Color = _currentColor; SkiaCanvas.InvalidateVisual(); }
                UpdateToolbarUI();
            }
        }

        private void UpdateToolbarUI()
        {
            var toolButtons = new Dictionary<string, Button> { { "Arrow", ToolArrow }, { "Rectangle", ToolRect }, { "Freehand", ToolPen }, { "Blur", ToolBlur }, { "Text", ToolText } };
            foreach (var kvp in toolButtons) kvp.Value.Effect = (_activeTool == kvp.Key) ? new DropShadowEffect { BlurRadius = 8, Color = Colors.Blue, ShadowDepth = 0, Opacity = 0.6 } : null;
            var colorButtons = new[] { ColorRed, ColorGreen, ColorBlue, ColorYellow, ColorBlack };
            foreach (var btn in colorButtons) {
                bool isMatch = (btn.Tag.ToString() == "Red" && _currentColor == SKColors.Red) || (btn.Tag.ToString() == "Green" && _currentColor == SKColors.Green) || (btn.Tag.ToString() == "Blue" && _currentColor == SKColors.Blue) || (btn.Tag.ToString() == "Yellow" && _currentColor == SKColors.Yellow) || (btn.Tag.ToString() == "Black" && _currentColor == SKColors.Black);
                btn.Effect = isMatch ? new DropShadowEffect { BlurRadius = 12, Color = Colors.Black, ShadowDepth = 0, Opacity = 0.9 } : null;
            }
        }

        private void CreateTextEditor(Point position)
        {
            CleanupTextBox();
            _activeTextBox = new TextBox { Width = 200, Background = Brushes.White, Foreground = new SolidColorBrush(Color.FromArgb(_currentColor.Alpha, _currentColor.Red, _currentColor.Green, _currentColor.Blue)), BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray, FontSize = 24 };
            Canvas.SetLeft(_activeTextBox, position.X); Canvas.SetTop(_activeTextBox, position.Y); TextEditCanvas.Children.Add(_activeTextBox); _activeTextBox.Focus();
            _activeTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) FinalizeText(); else if (e.Key == Key.Escape) CleanupTextBox(); };
            _activeTextBox.LostFocus += (s, e) => FinalizeText();
        }

        private void FinalizeText()
        {
            if (_activeTextBox != null) {
                if (!string.IsNullOrWhiteSpace(_activeTextBox.Text)) {
                    var pos = new Point(Canvas.GetLeft(_activeTextBox), Canvas.GetTop(_activeTextBox));
                    var anno = new TextAnno { Text = _activeTextBox.Text, Position = new SKPoint((float)pos.X, (float)pos.Y + 24), Color = _currentColor, FontSize = 24 };
                    _annotations.Add(anno); SelectObject(anno); SkiaCanvas.InvalidateVisual();
                }
                CleanupTextBox();
            }
        }

        private void CleanupTextBox()
        {
            if (_activeTextBox != null) { TextEditCanvas.Children.Remove(_activeTextBox); _activeTextBox = null; }
            this.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_activeTextBox != null) return;
            if (e.Key == Key.Escape) { if (this.Visibility == Visibility.Visible) this.Hide(); else Application.Current.Shutdown(); }
            else if (e.Key == Key.Enter && _isAnnotating) { CopyToClipboard(); this.Hide(); }
            else if (e.Key == Key.Delete && _selectedObject != null) { _annotations.Remove(_selectedObject); SelectObject(null); }
            base.OnKeyDown(e);
        }
    }
}
