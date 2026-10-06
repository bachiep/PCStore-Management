using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ComputerStore.GUI.Components;

public static class GraphicsExtensions
{
    public static void SetHighQuality(this Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.CompositingQuality = CompositingQuality.HighQuality;
    }

    public static GraphicsPath GetRoundedPath(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0f)
        {
            path.AddRectangle(bounds);
            return path;
        }

        float d = radius * 2f;
        var arc = new RectangleF(bounds.X, bounds.Y, d, d);

        // Top Left
        path.AddArc(arc, 180, 90);
        // Top Right
        arc.X = bounds.Right - d;
        path.AddArc(arc, 270, 90);
        // Bottom Right
        arc.Y = bounds.Bottom - d;
        path.AddArc(arc, 0, 90);
        // Bottom Left
        arc.X = bounds.X;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}
