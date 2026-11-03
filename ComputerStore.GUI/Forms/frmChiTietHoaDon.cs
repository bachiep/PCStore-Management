using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Forms;

public class frmChiTietHoaDon : Form
{
    private readonly DataGridView _grid = Theme.Grid();
    private readonly ToolTip _summaryToolTip = new();
    
    public frmChiTietHoaDon(HoaDonDTO hd)
    {
        Text = $"Chi tiết hóa đơn #{hd.MaHD} - Ngày lập: {hd.NgayLap:dd/MM/yyyy HH:mm}";
        Size = new Size(800, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Theme.Page;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm", weight: 2f),
            Theme.Col("DonGia", "Đơn giá", weight: 1f, format: "N0", right: true),
            Theme.Col("SoLuong", "SL", weight: 0.5f, right: true),
            Theme.Col("ThanhTien", "Thành tiền", weight: 1f, format: "N0", right: true)
        );
        
        var pnlFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var card = Theme.CardPanel(new Padding(5));
        card.Dock = DockStyle.Fill;
        card.Controls.Add(_grid);
        var summary = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
            BackColor = Color.White, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            Margin = new Padding(0)
        };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        summary.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        for (var row = 1; row < 4; row++) summary.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        summary.Controls.Add(SummaryCell($"Khách hàng: {hd.TenKH ?? "Khách lẻ"}", tooltip: $"Khách hàng: {hd.TenKH ?? "Khách lẻ"}"), 0, 0);
        summary.Controls.Add(SummaryCell($"Nhân viên: {hd.TenNV}", tooltip: $"Nhân viên: {hd.TenNV}"), 1, 0);
        summary.Controls.Add(SummaryCell($"Điện thoại: {hd.SDTKH ?? "N/A"}", tooltip: $"Điện thoại: {hd.SDTKH ?? "N/A"}"), 0, 1);
        summary.Controls.Add(SummaryCell($"Tổng tiền: {hd.TongTien:N0} ₫"), 1, 1);
        summary.Controls.Add(SummaryCell($"Giảm giá: {hd.GiamGia:N0} ₫"), 0, 2);
        summary.Controls.Add(SummaryCell($"Thanh toán: {hd.ThanhToan:N0} ₫"), 1, 2);
        summary.Controls.Add(SummaryCell($"Hình thức: {hd.HinhThucTT}"), 0, 3);
        summary.Controls.Add(SummaryCell($"Trạng thái: {(hd.DaHuy ? "Đã hủy" : "Hoàn thành")}", hd.DaHuy), 1, 3);

        var notesText = $"Ghi chú: {hd.GhiChu}" +
            (hd.DaHuy ? $"{Environment.NewLine}Lý do hủy ({hd.NgayHuy:dd/MM/yyyy HH:mm}): {hd.LyDoHuy}" : "");
        var notes = new TextBox
        {
            Text = notesText, Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, WordWrap = true, BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White, ForeColor = hd.DaHuy ? Theme.Danger : Theme.Text,
            Font = Theme.Small, TabStop = false, Margin = new Padding(0, 8, 0, 0)
        };
        var notesHeight = Math.Clamp(
            TextRenderer.MeasureText(notes.Text, notes.Font, new Size(650, 10000),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + 16,
            64, 120);
        var summaryHeight = Math.Max(202, 144 + notesHeight);
        var summaryLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = new Padding(10), BackColor = Color.White
        };
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        summaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, notesHeight));
        summaryLayout.Controls.Add(summary, 0, 0);
        summaryLayout.Controls.Add(notes, 0, 1);
        var summaryCard = Theme.CardPanel(new Padding(1));
        summaryCard.Dock = DockStyle.Fill;
        summaryCard.Margin = new Padding(0);
        summaryCard.Controls.Add(summaryLayout);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(20), BackColor = Theme.Page };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, summaryHeight));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(summaryCard, 0, 0);
        layout.Controls.Add(card, 0, 1);
        Controls.Add(pnlFill);
        pnlFill.Controls.Add(layout);

        Msg.Run(() => _grid.DataSource = new HoaDonBLL().GetDetails(hd.MaHD));
    }

    private Label SummaryCell(string text, bool isCanceled = false, string? tooltip = null)
    {
        var label = new Label
        {
            Text = text, Dock = DockStyle.Fill, AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 6, 0),
            Font = Theme.Base, ForeColor = isCanceled ? Theme.Danger : Theme.Text,
            BackColor = Color.White, UseMnemonic = false
        };
        if (!string.IsNullOrWhiteSpace(tooltip)) _summaryToolTip.SetToolTip(label, tooltip);
        return label;
    }
}
