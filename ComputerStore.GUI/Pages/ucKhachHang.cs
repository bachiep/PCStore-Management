using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Pages;

public class ucKhachHang : PageBase
{
    private readonly KhachHangBLL _bll = new();
    private readonly DataGridView _grid = Theme.Grid(), _history = Theme.Grid();
    private readonly TextBox _search = Theme.Input(260, "Tìm theo tên hoặc số điện thoại...");
    private readonly TextBox _ten = Theme.Input(), _sdt = Theme.Input(), _email = Theme.Input(), _diaChi = Theme.Input();
    private int _id;

    public ucKhachHang() : base("Khách hàng")
    {
        Name = "ucKhachHang";
        _grid.AutoGenerateColumns = false; _grid.Name = "gridKhachHang";
        _grid.Columns.AddRange(Theme.Col("MaKH", "Mã", weight: 0.4f), Theme.Col("HoTen", "Họ tên", weight: 1.8f), Theme.Col("SDT", "Điện thoại", weight: 1f),
            Theme.Col("DiaChi", "Địa chỉ", weight: 1.6f), Theme.Col("SoHoaDon", "Số HĐ", weight: 0.6f, right: true),
            Theme.Col("TongChiTieu", "Tổng chi tiêu (₫)", weight: 1.2f, format: "N0", right: true));
        _grid.SelectionChanged += (_, _) => ShowSelected();
        _search.Name = "txtTimKhach";
        Debounce(_search, LoadList);
        var add = Theme.PrimaryButton("+ Thêm mới", 110, (_, _) => Clear()); add.Name = "btnThemKhach";
        AddRow(Toolbar(add, _search, Theme.GhostButton("Làm mới", 90, (_, _) => LoadList()), ExportButton(_grid, "DanhSachKhachHang")));

        _history.AutoGenerateColumns = false; _history.Name = "gridLichSuMua";
        _history.Columns.AddRange(Theme.Col("MaHD", "Mã HĐ", weight: 0.6f), Theme.Col("NgayLap", "Ngày lập", weight: 1.2f, format: "dd/MM/yyyy HH:mm"),
            Theme.Col("ThanhToan", "Thanh toán", weight: 1f, format: "N0", right: true));
        _history.Height = 140;

        _ten.MaxLength = 100; _sdt.MaxLength = 15; _email.MaxLength = 100; _diaChi.MaxLength = 200; _ten.Name = "txtTenKhach"; _sdt.Name = "txtSdtKhach";
        var save = Theme.PrimaryButton("Lưu", 80, (_, _) => Save()); save.Name = "btnLuuKhach";
        var del = Theme.DangerButton("Xóa", 80, (_, _) => Delete());
        var neu = Theme.GhostButton("Làm mới ô", 90, (_, _) => Clear());
        var editor = EditorCard("Thông tin khách hàng", Theme.Field("Họ tên *", _ten, 320), Theme.Field("Số điện thoại *", _sdt, 320),
            Theme.Field("Email", _email, 320), Theme.Field("Địa chỉ", _diaChi, 320), Theme.Row(save, del, neu));
        
        var leftSplit = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        leftSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftSplit.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        leftSplit.Controls.Add(GridCard(_grid), 0, 0);
        
        var histContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        var histTitle = Theme.Caption("LỊCH SỬ MUA HÀNG CỦA KHÁCH:"); histTitle.Font = Theme.Bold; histTitle.Dock = DockStyle.Top;
        var histCard = GridCard(_history); histCard.Padding = new Padding(0, 6, 0, 0);
        histContainer.Controls.Add(histCard);
        histContainer.Controls.Add(histTitle);
        leftSplit.Controls.Add(histContainer, 0, 1);

        AddRow(TwoCols(leftSplit, editor, 380), fill: true);
    }

    public override void LoadData() { LoadList(); Clear(); }
    private void LoadList() => Msg.Run(() => _grid.DataSource = _bll.Search(_search.Text));

    private void ShowSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not KhachHangDTO k) return;
        _id = k.MaKH; _ten.Text = k.HoTen; _sdt.Text = k.SDT; _email.Text = k.Email ?? ""; _diaChi.Text = k.DiaChi ?? "";
        Msg.Run(() => _history.DataSource = new HoaDonBLL().GetByCustomer(k.MaKH));
    }

    private void Clear()
    {
        _id = 0; _ten.Clear(); _sdt.Clear(); _email.Clear(); _diaChi.Clear(); _history.DataSource = null; _grid.CurrentCell = null; _grid.ClearSelection(); _ten.Focus();
    }

    private void Save()
    {
        if (Msg.Run(() => _bll.Save(new KhachHangDTO { MaKH = _id, HoTen = _ten.Text, SDT = _sdt.Text, Email = _email.Text, DiaChi = _diaChi.Text })) > 0)
        { LoadList(); Clear(); Msg.Info("Đã lưu khách hàng."); }
    }

    private void Delete()
    {
        if (_id == 0) { Msg.Warn("Hãy chọn khách hàng cần xóa."); return; }
        if (!Ask("Xóa khách hàng này?")) return;
        if (Msg.Run(() => _bll.Delete(_id))) { LoadList(); Clear(); }
    }
}
