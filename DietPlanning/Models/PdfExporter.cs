using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace DietPlanning.Models
{
    public static class PngExporter
    {
        public static void ExportToPng(ScrollViewer scrollViewer, string filename, int dpi = 300)
        {
            if (scrollViewer == null) return;

            // Access the scrollable content (the child Grid)
            if (!(scrollViewer.Content is FrameworkElement content))
                return;

            // 1) Temporarily disable scrolling to measure full content
            var oldHorizontal = scrollViewer.HorizontalScrollBarVisibility;
            var oldVertical = scrollViewer.VerticalScrollBarVisibility;
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            // 2) Measure the content at infinite size => get full DesiredSize
            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desired = content.DesiredSize; // The full size needed

            // 3) Arrange the content at (0,0)
            content.Arrange(new Rect(0, 0, desired.Width, desired.Height));

            // 4) Render the entire content at chosen DPI
            double scale = dpi / 96.0; // Convert from default 96 DPI to the chosen DPI
            int renderWidth = (int)(desired.Width * scale);
            int renderHeight = (int)(desired.Height * scale);

            // Create RenderTargetBitmap
            var renderBitmap = new RenderTargetBitmap(
                renderWidth, renderHeight, dpi, dpi, PixelFormats.Pbgra32);

            // Use a DrawingVisual + DrawingContext to apply the scale transform
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(scale, scale));
                dc.DrawRectangle(new VisualBrush(content), null, new Rect(new Size(desired.Width, desired.Height)));
                dc.Pop();
            }
            renderBitmap.Render(dv);

            // 5) Restore original scroll settings
            scrollViewer.HorizontalScrollBarVisibility = oldHorizontal;
            scrollViewer.VerticalScrollBarVisibility = oldVertical;

            // 6) Encode as PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }
    }
}
