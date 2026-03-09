using SkiaSharp;
using System;
using System.Collections.Generic;

namespace ScreenshotTool.Models
{
    public abstract class AnnotationObject
    {
        public SKColor Color { get; set; } = SKColors.Red;
        public float StrokeWidth { get; set; } = 3;
        public bool IsSelected { get; set; }

        public abstract void Draw(SKCanvas canvas);
        public abstract bool HitTest(SKPoint point);
        public abstract void Move(SKPoint delta);
        public abstract void Resize(SKPoint newPoint, int handleIndex);
        public abstract int GetHandleAtPoint(SKPoint point);

        protected void DrawHandle(SKCanvas canvas, SKPoint p)
        {
            using (var paint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
            using (var stroke = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 1 })
            {
                var rect = new SKRect(p.X - 5, p.Y - 5, p.X + 5, p.Y + 5);
                canvas.DrawRect(rect, paint);
                canvas.DrawRect(rect, stroke);
            }
        }

        protected bool HitTestHandle(SKPoint p, SKPoint target)
        {
            var rect = new SKRect(target.X - 8, target.Y - 8, target.X + 8, target.Y + 8);
            return rect.Contains(p.X, p.Y);
        }

        protected bool IsPointNearSegment(SKPoint p, SKPoint start, SKPoint end, float threshold)
        {
            float x1 = start.X, y1 = start.Y, x2 = end.X, y2 = end.Y;
            float px = p.X, py = p.Y;
            float dx = x2 - x1;
            float dy = y2 - y1;
            if (dx == 0 && dy == 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1)) < threshold;
            float t = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            float nearestX = x1 + t * dx;
            float nearestY = y1 + t * dy;
            float distSq = (px - nearestX) * (px - nearestX) + (py - nearestY) * (py - nearestY);
            return distSq < threshold * threshold;
        }
    }

    public class RectangleAnno : AnnotationObject
    {
        public SKRect Rect { get; set; }
        public override void Draw(SKCanvas canvas)
        {
            using (var paint = new SKPaint { Color = Color, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, IsAntialias = true })
            {
                canvas.DrawRect(Rect, paint);
            }
            if (IsSelected)
            {
                DrawHandle(canvas, new SKPoint(Rect.Left, Rect.Top));
                DrawHandle(canvas, new SKPoint(Rect.Right, Rect.Bottom));
            }
        }
        public override bool HitTest(SKPoint point)
        {
            var rect = Rect;
            rect.Inflate(10, 10);
            return rect.Contains(point);
        }
        public override void Move(SKPoint delta) { Rect = new SKRect(Rect.Left + delta.X, Rect.Top + delta.Y, Rect.Right + delta.X, Rect.Bottom + delta.Y); }
        public override int GetHandleAtPoint(SKPoint p)
        {
            if (HitTestHandle(p, new SKPoint(Rect.Left, Rect.Top))) return 0;
            if (HitTestHandle(p, new SKPoint(Rect.Right, Rect.Bottom))) return 1;
            return -1;
        }
        public override void Resize(SKPoint p, int index)
        {
            if (index == 0) Rect = new SKRect(p.X, p.Y, Rect.Right, Rect.Bottom);
            else if (index == 1) Rect = new SKRect(Rect.Left, Rect.Top, p.X, p.Y);
        }
    }

    public class ArrowAnno : AnnotationObject
    {
        public SKPoint Start { get; set; }
        public SKPoint End { get; set; }
        public override void Draw(SKCanvas canvas)
        {
            using (var paint = new SKPaint { Color = Color, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, IsAntialias = true })
            {
                canvas.DrawLine(Start, End, paint);
                float angle = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
                float headLen = 15;
                SKPoint p1 = new SKPoint(End.X - headLen * (float)Math.Cos(angle - Math.PI / 6), End.Y - headLen * (float)Math.Sin(angle - Math.PI / 6));
                SKPoint p2 = new SKPoint(End.X - headLen * (float)Math.Cos(angle + Math.PI / 6), End.Y - headLen * (float)Math.Sin(angle + Math.PI / 6));
                canvas.DrawLine(End, p1, paint);
                canvas.DrawLine(End, p2, paint);
            }
            if (IsSelected) { DrawHandle(canvas, Start); DrawHandle(canvas, End); }
        }
        public override bool HitTest(SKPoint p) => IsPointNearSegment(p, Start, End, 10);
        public override void Move(SKPoint d) { Start = new SKPoint(Start.X + d.X, Start.Y + d.Y); End = new SKPoint(End.X + d.X, End.Y + d.Y); }
        public override int GetHandleAtPoint(SKPoint p)
        {
            if (HitTestHandle(p, Start)) return 0;
            if (HitTestHandle(p, End)) return 1;
            return -1;
        }
        public override void Resize(SKPoint p, int index) { if (index == 0) Start = p; else End = p; }
    }

    public class PathAnno : AnnotationObject
    {
        public List<SKPoint> Points { get; } = new List<SKPoint>();
        public override void Draw(SKCanvas canvas)
        {
            if (Points.Count < 2) return;
            using (var paint = new SKPaint { Color = Color, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round })
            {
                using (var path = new SKPath())
                {
                    path.MoveTo(Points[0]);
                    for (int i = 1; i < Points.Count; i++) path.LineTo(Points[i]);
                    canvas.DrawPath(path, paint);
                }
            }
        }
        public override bool HitTest(SKPoint p)
        {
            if (Points.Count < 2) return false;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                if (IsPointNearSegment(p, Points[i], Points[i + 1], 10)) return true;
            }
            return false;
        }
        public override void Move(SKPoint d) { for (int i = 0; i < Points.Count; i++) Points[i] = new SKPoint(Points[i].X + d.X, Points[i].Y + d.Y); }
        public override int GetHandleAtPoint(SKPoint p) => -1;
        public override void Resize(SKPoint p, int i) { }
    }

    public class BlurAnno : AnnotationObject
    {
        public SKRect Rect { get; set; }
        public override void Draw(SKCanvas canvas)
        {
            if (IsSelected)
            {
                using (var paint = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 1, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0) })
                {
                    canvas.DrawRect(Rect, paint);
                }
                DrawHandle(canvas, new SKPoint(Rect.Left, Rect.Top));
                DrawHandle(canvas, new SKPoint(Rect.Right, Rect.Bottom));
            }
        }
        public override bool HitTest(SKPoint p) => Rect.Contains(p);
        public override void Move(SKPoint d) { Rect = new SKRect(Rect.Left + d.X, Rect.Top + d.Y, Rect.Right + d.X, Rect.Bottom + d.Y); }
        public override int GetHandleAtPoint(SKPoint p)
        {
            if (HitTestHandle(p, new SKPoint(Rect.Left, Rect.Top))) return 0;
            if (HitTestHandle(p, new SKPoint(Rect.Right, Rect.Bottom))) return 1;
            return -1;
        }
        public override void Resize(SKPoint p, int i)
        {
            if (i == 0) Rect = new SKRect(p.X, p.Y, Rect.Right, Rect.Bottom);
            else if (i == 1) Rect = new SKRect(Rect.Left, Rect.Top, p.X, p.Y);
        }
    }

    public class TextAnno : AnnotationObject
    {
        public string Text { get; set; } = "";
        public SKPoint Position { get; set; }
        public float FontSize { get; set; } = 24;

        public override void Draw(SKCanvas canvas)
        {
            using (var paint = new SKPaint { Color = Color, IsAntialias = true })
            using (var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), FontSize))
            {
                canvas.DrawText(Text, Position.X, Position.Y, font, paint);
                
                if (IsSelected)
                {
                    SKRect bounds;
                    font.MeasureText(Text, out bounds, paint);
                    SKRect box = new SKRect(Position.X + bounds.Left - 5, Position.Y + bounds.Top - 5, Position.X + bounds.Right + 5, Position.Y + bounds.Bottom + 5);
                    
                    using (var dash = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 1, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0) })
                    {
                        canvas.DrawRect(box, dash);
                    }
                    DrawHandle(canvas, new SKPoint(box.Right, box.Bottom));
                }
            }
        }

        public override bool HitTest(SKPoint p)
        {
            using (var paint = new SKPaint())
            using (var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), FontSize))
            {
                SKRect bounds;
                font.MeasureText(Text, out bounds, paint);
                SKRect box = new SKRect(Position.X + bounds.Left - 10, Position.Y + bounds.Top - 10, Position.X + bounds.Right + 10, Position.Y + bounds.Bottom + 10);
                return box.Contains(p);
            }
        }

        public override void Move(SKPoint delta)
        {
            Position = new SKPoint(Position.X + delta.X, Position.Y + delta.Y);
        }

        public override int GetHandleAtPoint(SKPoint p)
        {
            using (var paint = new SKPaint())
            using (var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), FontSize))
            {
                SKRect bounds;
                font.MeasureText(Text, out bounds, paint);
                if (HitTestHandle(p, new SKPoint(Position.X + bounds.Right + 5, Position.Y + bounds.Bottom + 5))) return 1;
            }
            return -1;
        }

        public override void Resize(SKPoint newPoint, int handleIndex)
        {
            if (handleIndex == 1)
            {
                // Simple font size scaling based on horizontal drag
                float dist = newPoint.X - Position.X;
                if (dist > 10) FontSize = dist / (Text.Length * 0.6f);
            }
        }
    }
}
