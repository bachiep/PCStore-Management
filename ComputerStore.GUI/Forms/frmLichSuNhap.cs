using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Forms;

/// <summary>Lịch sử phiếu nhập kho: lọc theo ngày, chọn phiếu để xem chi tiết dòng hàng.</summary>
public class frmLichSuNhap : Form
{
    private readonly PhieuNhapBLL _bll = new();
    private readonly DataGridView _gridPN = Theme.Grid();
    private readonly DataGridView _gridCT = Theme.Grid();
    private readonly DateTimePicker _tu = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Theme.Base, Width = 120 };
    private readonly DateTimePicker _den = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Theme.Base, Width = 120 };
    private readonly Label _lblTong = new() { AutoSize = true, Font = Theme.Bold, Margin = new Padding(20, 7, 0, 0) };

    public frmLichSuNhap()
    {
        Text = "Lịch sử nhập kho";
        Size = new Size(960, 620);
        MinimumSize = new Size(820, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Page;

        _tu.Value = DateTime.Today.AddDays(-90);
        _den.Value = DateTime.Today;

        _gridPN.AutoGenerateColumns = false;
        _gridPN.Columns.AddRange(
            Theme.Col("MaPN", "Mã phiếu", weight: 0.7f),
            Theme.Col("NgayNhap", "Ngày nhập", weight: 1.3f, format: "dd/MM/yyyy HH:mm"),
            Theme.Col("TenNCC", "Nhà cung cấp", weight: 2f),
            Theme.Col("TenNV", "Nhân viên", weight: 1.3f),
            Theme.Col("GhiChu", "Ghi chú", weight: 1.5f),
            Theme.Col("TongTien", "Tổng tiền", weight: 1.1f, format: "N0", right: true));
        _gridCT.AutoGenerateColumns = false;
        _gridCT.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm trong phiếu", weight: 3f),
            Theme.Col("SoLuong", "SL", weight: 0.5f, right: true),
            Theme.Col("DonGia", "Đơn giá nhập", weight: 1.2f, format: "N0", right: true),
            Theme.Col("ThanhTien", "Thành tiền", weight: 1.2f, format: "N0", right: true));
        Theme.EmptyHint(_gridPN, "Không có phiếu nhập trong khoảng ngày đã chọn");

        _gridPN.SelectionChanged += (_, _) => ShowDetail();
        _tu.ValueChanged += (_, _) => Load_();
        _den.ValueChanged += (_, _) => Load_();

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(12, 8, 12, 0) };
        bar.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(0, 7, 5, 0) });
        bar.Controls.Add(_tu);
        bar.Controls.Add(new Label { Text = "Đến:", AutoSize = true, Margin = new Padding(10, 7, 5, 0) });
        bar.Controls.Add(_den);
        bar.Controls.Add(_lblTong);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 280, Padding = new Padding(12, 0, 12, 12) };
        var pnlPN = Theme.CardPanel(new Padding(5)); pnlPN.Dock = DockStyle.Fill;
        _gridPN.BorderStyle = BorderStyle.None; pnlPN.Controls.Add(_gridPN);
        
        var pnlCT = Theme.CardPanel(new Padding(5)); pnlCT.Dock = DockStyle.Fill;
        _gridCT.BorderStyle = BorderStyle.None; pnlCT.Controls.Add(_gridCT);
        
        split.Panel1.Controls.Add(pnlPN);
        split.Panel2.Controls.Add(pnlCT);

        Controls.Add(split);
        Controls.Add(bar);
        Shown += (_, _) => Load_();
    }

    private void Load_() => Msg.Run(() =>
    {
        var list = _bll.Search(_tu.Value, _den.Value);
        _gridPN.DataSource = list;
        _lblTong.Text = $"{list.Count} phiếu · tổng {list.Sum(p => p.TongTien):N0} ₫";
        ShowDetail();
    });

    private void ShowDetail() =>
        _gridCT.DataSource = _gridPN.CurrentRow?.DataBoundItem is PhieuNhapDTO pn ? _bll.GetDetails(pn.MaPN) : null;
}

