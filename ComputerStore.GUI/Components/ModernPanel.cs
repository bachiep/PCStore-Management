using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ComputerStore.GUI.Components;

public class ModernPanel : Panel
{
    public int BorderRadius { get; set; } = 12;
    public Color BorderColor { get; set; } = ColorTranslator.FromHtml("#E5E7EB");
    public int BorderSize { get; set; } = 1;
    public bool ShowShadow { get; set; } = true;
    public Color FillColor { get; set; } = Color.White;

    public ModernPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SetHighQuality();

        // Calculate geometry
        var parentColor = Parent?.BackColor ?? BackColor;
        using (var parentBrush = new SolidBrush(parentColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);

        if (BorderRadius > 0)
        {
            using var path = GraphicsExtensions.GetRoundedPath(rect, BorderRadius);
            using var brush = new SolidBrush(FillColor);
            g.FillPath(brush, path);

            if (BorderSize > 0)
            {
                using var pen = new Pen(BorderColor, BorderSize);
                g.DrawPath(pen, path);
            }
        }
        else
        {
            using (var brush = new SolidBrush(FillColor))
            {
                g.FillRectangle(brush, 0, 0, Width, Height);
            }

            if (BorderSize > 0)
            {
                using (var pen = new Pen(BorderColor, BorderSize))
                {
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }
}
