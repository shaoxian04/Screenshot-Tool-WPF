using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScreenshotTool.Models
{
    public abstract class AnnotationObject
    {
        public SKColor Color { get; set; } = SKColors.Red;
        public float StrokeWidth { get; set; } = 3.0f;
        public bool IsSelected { get; set; }
        public abstract void Draw(SKCanvas canvas);
        public abstract bool HitTest(SKPoint point);
        public abstract void Move(SKPoint delta);
        public abstract void Resize(SKPoint point, int handleIndex);
        public abstract SKRect GetBounds();

        protected void DrawHandles(SKCanvas canvas, SKRect rect)
        {
            if (!IsSelected) return;
            using (var paint = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                float size = 10;
                canvas.DrawRect(SKRect.Create(rect.Left - size / 2, rect.Top - size / 2, size, size), paint);
                canvas.DrawRect(SKRect.Create(rect.Right - size / 2, rect.Top - size / 2, size, size), paint);
                canvas.DrawRect(SKRect.Create(rect.Left - size / 2, rect.Bottom - size / 2, size, size), paint);
                canvas.DrawRect(SKRect.Create(rect.Right - size / 2, rect.Bottom - size / 2, size, size), paint);
            }
            using (var borderPaint = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 1, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0) })
            {
                canvas.DrawRect(rect, borderPaint);
            }
        }

        public virtual int GetHandleAtPoint(SKPoint point)
        {
            var rect = GetBounds();
            float size = 30; 
            if (SKRect.Create(rect.Left - size / 2, rect.Top - size / 2, size, size).Contains(point)) return 0;
            if (SKRect.Create(rect.Right - size / 2, rect.Top - size / 2, size, size).Contains(point)) return 1;
            if (SKRect.Create(rect.Left - size / 2, rect.Bottom - size / 2, size, size).Contains(point)) return 2;
            if (SKRect.Create(rect.Right - size / 2, rect.Bottom - size / 2, size, size).Contains(point)) return 3;
            return -1;
        }
    }

    public class RectangleAnno : AnnotationObject
    {
        public SKRect Rect { get; set; }
        public override void Draw(SKCanvas canvas)
        {
            using (var paint = new SKPaint { Color = this.Color, Style = SKPaintStyle.Stroke, StrokeWidth = this.StrokeWidth, IsAntialias = true })
            {
                canvas.DrawRect(Rect, paint);
            }
            DrawHandles(canvas, Rect);
        }
        public override bool HitTest(SKPoint point) { var r = Rect; r.Inflate(5, 5); return r.Contains(point); }
        public override void Move(SKPoint delta) => Rect = new SKRect(Rect.Left + delta.X, Rect.Top + delta.Y, Rect.Right + delta.X, Rect.Bottom + delta.Y);
        public override void Resize(SKPoint point, int handleIndex)
        {
            float left = Rect.Left, top = Rect.Top, right = Rect.Right, bottom = Rect.Bottom;
            if (handleIndex == 0) { left = point.X; top = point.Y; }
            else if (handleIndex == 1) { right = point.X; top = point.Y; }
            else if (handleIndex == 2) { left = point.X; bottom = point.Y; }
            else if (handleIndex == 3) { right = point.X; bottom = point.Y; }
            Rect = new SKRect(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
        }
        public override SKRect GetBounds() => Rect;
    }

    public class ArrowAnno : AnnotationObject
    {
        public SKPoint Start { get; set; }
        public SKPoint End { get; set; }
        public override void Draw(SKCanvas canvas)
        {
            using (var paint = new SKPaint { Color = this.Color, Style = SKPaintStyle.Stroke, StrokeWidth = this.StrokeWidth, IsAntialias = true, StrokeCap = SKStrokeCap.Round })
            {
                canvas.DrawLine(Start, End, paint);
                float angle = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
                float headLength = 15.0f; float headAngle = (float)(Math.PI / 6);
                canvas.DrawLine(End, new SKPoint(End.X - headLength * (float)Math.Cos(angle - headAngle), End.Y - headLength * (float)Math.Sin(angle - headAngle)), paint);
                canvas.DrawLine(End, new SKPoint(End.X - headLength * (float)Math.Cos(angle + headAngle), End.Y - headLength * (float)Math.Sin(angle + headAngle)), paint);
            }
            DrawHandles(canvas, GetBounds());
        }
        public override bool HitTest(SKPoint point) { var b = GetBounds(); b.Inflate(10, 10); return b.Contains(point); }
        public override void Move(SKPoint delta) { Start = new SKPoint(Start.X + delta.X, Start.Y + delta.Y); End = new SKPoint(End.X + delta.X, End.Y + delta.Y); }
        public override void Resize(SKPoint point, int handleIndex) { if (handleIndex == 0 || handleIndex == 2) Start = point; else End = point; }
        public override SKRect GetBounds() => new SKRect(Math.Min(Start.X, End.X), Math.Min(Start.Y, End.Y), Math.Max(Start.X, End.X), Math.Max(Start.Y, End.Y));
    }

    public class PathAnno : AnnotationObject
    {
        public List<SKPoint> Points { get; set; } = new List<SKPoint>();
        public override void Draw(SKCanvas canvas)
        {
            if (Points.Count < 2) return;
            using (var paint = new SKPaint { Color = this.Color, Style = SKPaintStyle.Stroke, StrokeWidth = this.StrokeWidth, IsAntialias = true, StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round })
            {
                using (var path = new SKPath()) {
                    path.MoveTo(Points[0]);
                    foreach (var p in Points) path.LineTo(p);
                    canvas.DrawPath(path, paint);
                }
            }
            if (IsSelected) DrawHandles(canvas, GetBounds());
        }
        public override bool HitTest(SKPoint point) { var b = GetBounds(); b.Inflate(10, 10); return b.Contains(point); }
        public override void Move(SKPoint delta) { for (int i = 0; i < Points.Count; i++) Points[i] = new SKPoint(Points[i].X + delta.X, Points[i].Y + delta.Y); }
        public override void Resize(SKPoint point, int handleIndex) { }
        public override SKRect GetBounds() {
            if (Points.Count == 0) return SKRect.Empty;
            return new SKRect(Points.Min(p => p.X), Points.Min(p => p.Y), Points.Max(p => p.X), Points.Max(p => p.Y));
        }
    }

    public class BlurAnno : AnnotationObject
    {
        public SKRect Rect { get; set; }
        public override void Draw(SKCanvas canvas) 
        { 
            // Only draw handles/frame. Pixels are blurred in PaintSurface
            DrawHandles(canvas, Rect); 
        }
        public override bool HitTest(SKPoint point) { var r = Rect; r.Inflate(10, 10); return r.Contains(point); }
        public override void Move(SKPoint delta) => Rect = new SKRect(Rect.Left + delta.X, Rect.Top + delta.Y, Rect.Right + delta.X, Rect.Bottom + delta.Y);
        public override void Resize(SKPoint point, int handleIndex)
        {
            float left = Rect.Left, top = Rect.Top, right = Rect.Right, bottom = Rect.Bottom;
            if (handleIndex == 0) { left = point.X; top = point.Y; }
            else if (handleIndex == 1) { right = point.X; top = point.Y; }
            else if (handleIndex == 2) { left = point.X; bottom = point.Y; }
            else if (handleIndex == 3) { right = point.X; bottom = point.Y; }
            Rect = new SKRect(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
        }
        public override SKRect GetBounds() => Rect;
    }

    public class TextAnno : AnnotationObject
    {
        public SKPoint Position { get; set; }
        public string Text { get; set; } = "";
        public float FontSize { get; set; } = 24.0f;
        public override void Draw(SKCanvas canvas)
        {
            if (string.IsNullOrEmpty(Text)) return;
            using (var paint = new SKPaint { Color = this.Color, IsAntialias = true, TextSize = this.FontSize })
            {
                canvas.DrawText(Text, Position, paint);
            }
            DrawHandles(canvas, GetBounds());
        }
        public override bool HitTest(SKPoint point) { var b = GetBounds(); b.Inflate(10, 10); return b.Contains(point); }
        public override void Move(SKPoint delta) => Position = new SKPoint(Position.X + delta.X, Position.Y + delta.Y);
        public override void Resize(SKPoint point, int handleIndex)
        {
            var bounds = GetBounds();
            float newSize = Math.Abs(point.Y - bounds.Top);
            if (newSize > 8) FontSize = newSize;
        }
        public override SKRect GetBounds()
        {
            using (var paint = new SKPaint { TextSize = this.FontSize })
            {
                SKRect textBounds = new SKRect();
                paint.MeasureText(Text, ref textBounds);
                return new SKRect(Position.X + textBounds.Left, Position.Y + textBounds.Top, Position.X + textBounds.Right, Position.Y + textBounds.Bottom);
            }
        }
    }
}
