using SkiaSharp;
using ScreenshotTool.Models;
using ScreenshotTool.Views;
using Xunit;

namespace ScreenshotTool.Tests
{
    public class ModelTests
    {
        [Fact]
        public void RectangleAnno_DrawsCorrectly()
        {
            // Arrange
            var rectAnno = new RectangleAnno
            {
                Rect = new SKRect(10, 10, 100, 100),
                Color = SKColors.Blue,
                StrokeWidth = 5.0f
            };
            
            var info = new SKImageInfo(200, 200);
            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;
                
                // Act
                rectAnno.Draw(canvas);
                
                // Assert
                Assert.Equal(SKColors.Blue, rectAnno.Color);
                Assert.Equal(5.0f, rectAnno.StrokeWidth);
            }
        }

        [Fact]
        public void AnnotationObject_DefaultValues()
        {
            // Arrange & Act
            var rectAnno = new RectangleAnno();
            
            // Assert
            Assert.Equal(SKColors.Red, rectAnno.Color);
            Assert.Equal(3.0f, rectAnno.StrokeWidth);
        }

        [Fact]
        public void ArrowAnno_Properties_SetCorrectly()
        {
            var arrow = new ArrowAnno
            {
                Start = new SKPoint(0, 0),
                End = new SKPoint(100, 100),
                Color = SKColors.Green
            };

            Assert.Equal(0, arrow.Start.X);
            Assert.Equal(100, arrow.End.Y);
            Assert.Equal(SKColors.Green, arrow.Color);
        }
    }
}
