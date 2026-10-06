using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ComputerStore.GUI.Components;

public class ModernButton : Control, IButtonControl
{
    private bool _isHovered = false;
    private bool _isPressed = false;
    private DialogResult _dialogResult = DialogResult.None;
    
    public int BorderRadius { get; set; } = 8;
    public Color NormalColor { get; set; } = Color.FromArgb(37, 99, 235);
    public Color HoverColor { get; set; } = Color.FromArgb(29, 78, 216);
    public Color BorderColor { get; set; } = Color.Transparent;
    public int BorderSize { get; set; } = 0;
    public AutoSizeMode AutoSizeMode { get; set; } = AutoSizeMode.GrowAndShrink;

    // IButtonControl implementation
    public DialogResult DialogResult { get => _dialogResult; set => _dialogResult = value; }
    public void NotifyDefault(bool value) { /* Do nothing, no thick border */ }
    public void PerformClick() { if (Enabled) InvokeOnClick(this, EventArgs.Empty); }

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    protected override bool ShowFocusCues => false;

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SetHighQuality();

        var parentColor = Parent?.BackColor ?? BackColor;
        using (var parentBrush = new SolidBrush(parentColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);

        Color currentBg;
        if (!Enabled) currentBg = Color.FromArgb(209, 213, 219); // Muted gray
        else if (_isPressed) currentBg = ControlPaint.Dark(HoverColor, 0.1f);
        else if (_isHovered) currentBg = HoverColor;
        else currentBg = NormalColor;

        if (BorderRadius > 0)
        {
            using var path = GraphicsExtensions.GetRoundedPath(rect, BorderRadius);
            using var brush = new SolidBrush(currentBg);
            g.FillPath(brush, path);

            if (BorderSize > 0)
            {
                using var pen = new Pen(BorderColor, BorderSize);
                g.DrawPath(pen, path);
            }
        }
        else
        {
            using (var brush = new SolidBrush(currentBg))
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

        // Draw Text
        TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor, flags);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var textSize = TextRenderer.MeasureText(Text, Font, proposedSize, TextFormatFlags.NoPrefix);
        int w = textSize.Width + Padding.Left + Padding.Right + 12;
        int h = Math.Max(Height, textSize.Height + Padding.Top + Padding.Bottom + 6);
        return new Size(w, h);
    }
}
