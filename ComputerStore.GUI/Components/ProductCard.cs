using System;
using System.Drawing;
using System.Windows.Forms;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Components;

public class ProductCard : ModernPanel
{
    private readonly SanPhamDTO _product;
    private Image? _image;
    private bool _isHovered;

    public event EventHandler<SanPhamDTO>? ProductClicked;

    public ProductCard(SanPhamDTO sp)
    {
        _product = sp;
        Width = 190;
        Height = 225;
        Margin = new Padding(8);
        Cursor = (_product.TrangThai && _product.SoLuongTon > 0) ? Cursors.Hand : Cursors.Default;
        ShowShadow = false;
        BorderColor = Theme.Border;
        BorderSize = 1;
        FillColor = Color.White;

        if (!string.IsNullOrEmpty(sp.HinhAnh))
        {
            _image = Services.ProductImage.Load(sp.HinhAnh);
        }

        MouseEnter += (s, e) => { if (_product.TrangThai && _product.SoLuongTon > 0) { _isHovered = true; Invalidate(); } };
        MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        MouseClick += (s, e) => { if (_product.TrangThai && _product.SoLuongTon > 0) ProductClicked?.Invoke(this, _product); };
    }

    private static string GetCategoryIcon(string? dm) => (dm ?? "").ToLowerInvariant() switch
    {
        var s when s.Contains("cpu") || s.Contains("vi xử lý") => "💻",
        var s when s.Contains("vga") || s.Contains("đồ họa") || s.Contains("card") => "🎮",
        var s when s.Contains("ram") || s.Contains("bộ nhớ") => "⚡",
        var s when s.Contains("main") || s.Contains("bo mạch") => "🖲️",
        var s when s.Contains("ssd") || s.Contains("hdd") || s.Contains("ổ cứng") => "💾",
        var s when s.Contains("nguồn") || s.Contains("psu") => "🔌",
        var s when s.Contains("case") || s.Contains("vỏ") => "📦",
        var s when s.Contains("tản") || s.Contains("fan") => "❄️",
        var s when s.Contains("màn") || s.Contains("monitor") => "🖥️",
        _ => "🔧"
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SetHighQuality();

        // Hover effect border
        if (_isHovered && _product.TrangThai && _product.SoLuongTon > 0)
        {
            using var pHover = new Pen(Theme.Accent, 1.5f);
            g.DrawRectangle(pHover, 0, 0, Width - 1, Height - 1);
        }

        // Draw Image or Category Badge
        var imgRect = new Rectangle(8, 8, Width - 16, 95);
        if (_image != null)
        {
            float ratio = Math.Min((float)imgRect.Width / _image.Width, (float)imgRect.Height / _image.Height);
            int newW = (int)(_image.Width * ratio);
            int newH = (int)(_image.Height * ratio);
            g.DrawImage(_image, imgRect.X + (imgRect.Width - newW) / 2, imgRect.Y + (imgRect.Height - newH) / 2, newW, newH);
        }
        else
        {
            using var bgBrush = new SolidBrush(ColorTranslator.FromHtml("#F1F5F9"));
            g.FillRectangle(bgBrush, imgRect);
            var icon = GetCategoryIcon(_product.TenDM);
            using var iconFont = new Font("Segoe UI Emoji", 24f);
            TextRenderer.DrawText(g, icon, iconFont, new Rectangle(imgRect.X, imgRect.Y + 8, imgRect.Width, 45), Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, _product.TenDM, Theme.Small, new Rectangle(imgRect.X, imgRect.Y + 55, imgRect.Width, 30), Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
        }

        // Draw Name (multiline, up to 2-3 lines)
        var nameRect = new Rectangle(10, 110, Width - 20, 50);
        TextRenderer.DrawText(g, _product.TenSP, Theme.Bold, nameRect, Theme.Text, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        // Draw Price
        var priceRect = new Rectangle(10, 166, Width - 20, 24);
        TextRenderer.DrawText(g, Theme.Money(_product.GiaBan), Theme.H2, priceRect, Theme.Accent, TextFormatFlags.Bottom | TextFormatFlags.Left);

        // Draw Stock Badge
        var stockRect = new Rectangle(10, 194, Width - 20, 20);
        if (!_product.TrangThai)
        {
            TextRenderer.DrawText(g, "Ngừng kinh doanh", Theme.Small, stockRect, Theme.Danger, TextFormatFlags.Top | TextFormatFlags.Left);
        }
        else if (_product.SoLuongTon <= 0)
        {
            TextRenderer.DrawText(g, "Hết hàng", Theme.Small, stockRect, Theme.Danger, TextFormatFlags.Top | TextFormatFlags.Left);
        }
        else
        {
            TextRenderer.DrawText(g, $"Còn: {_product.SoLuongTon} chiếc", Theme.Small, stockRect, Theme.Muted, TextFormatFlags.Top | TextFormatFlags.Left);
        }

        // Out of stock overlay
        if (!_product.TrangThai || _product.SoLuongTon <= 0)
        {
            using var overlay = new SolidBrush(Color.FromArgb(170, 248, 250, 252));
            g.FillRectangle(overlay, 0, 0, Width, Height);

            var badgeRect = new Rectangle(Width / 2 - 50, 60, 100, 28);
            using var bBg = new SolidBrush(Color.FromArgb(220, 220, 38, 38));
            g.FillRectangle(bBg, badgeRect);
            TextRenderer.DrawText(g, !_product.TrangThai ? "NGỪNG BÁN" : "HẾT HÀNG", Theme.Bold, badgeRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
