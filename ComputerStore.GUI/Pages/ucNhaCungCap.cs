using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Pages;

public class ucNhaCungCap : PageBase
{
    private readonly NhaCungCapBLL _bll = new();
    private readonly DataGridView _grid = Theme.Grid();
    private readonly TextBox _search = Theme.Input(260, "Tìm theo tên hoặc số điện thoại...");
    private readonly TextBox _ten = Theme.Input(), _sdt = Theme.Input(), _email = Theme.Input(), _diaChi = Theme.Input();
    private int _id;

    public ucNhaCungCap() : base("Nhà cung cấp")
    {
        Name = "ucNhaCungCap";
        _grid.AutoGenerateColumns = false; _grid.Name = "gridNhaCungCap";
        _grid.Columns.AddRange(Theme.Col("MaNCC", "Mã", weight: 0.4f), Theme.Col("TenNCC", "Tên nhà cung cấp", weight: 2.2f),
            Theme.Col("SDT", "Điện thoại", weight: 1f), Theme.Col("Email", "Email", weight: 1.4f), Theme.Col("DiaChi", "Địa chỉ", weight: 1.6f));
        _grid.SelectionChanged += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is not NhaCungCapDTO n) return;
            _id = n.MaNCC; _ten.Text = n.TenNCC; _sdt.Text = n.SDT ?? ""; _email.Text = n.Email ?? ""; _diaChi.Text = n.DiaChi ?? "";
        };
        _search.Name = "txtTimNCC";
        Debounce(_search, LoadList);
        var add = Theme.PrimaryButton("+ Thêm mới", 110, (_, _) => Clear()); add.Name = "btnThemNCC"; add.Enabled = IsAdmin;
        AddRow(Toolbar(add, _search, Theme.GhostButton("Làm mới", 90, (_, _) => LoadList()), ExportButton(_grid, "DanhSachNhaCungCap")));

        _ten.MaxLength = 150; _sdt.MaxLength = 15; _email.MaxLength = 100; _diaChi.MaxLength = 200; _ten.Name = "txtTenNCC";
        var save = Theme.PrimaryButton("Lưu", 80, (_, _) => Save()); save.Name = "btnLuuNCC";
        var del = Theme.DangerButton("Xóa", 80, (_, _) => Delete());
        var neu = Theme.GhostButton("Làm mới ô", 90, (_, _) => Clear());
        var editor = EditorCard("Thông tin nhà cung cấp", Theme.Field("Tên nhà cung cấp *", _ten, 310), Theme.Field("Số điện thoại", _sdt, 310),
            Theme.Field("Email", _email, 310), Theme.Field("Địa chỉ", _diaChi, 310), Theme.Row(save, del, neu));
        if (!IsAdmin) foreach (var c in new Control[] { _ten, _sdt, _email, _diaChi, save, del, neu }) c.Enabled = false;
        AddRow(TwoCols(GridCard(_grid), editor, 370), fill: true);
    }

    public override void LoadData() { LoadList(); Clear(); }
    private void LoadList() => Msg.Run(() => _grid.DataSource = _bll.Search(_search.Text));

    private void Clear() { _id = 0; _ten.Clear(); _sdt.Clear(); _email.Clear(); _diaChi.Clear(); _grid.CurrentCell = null; _grid.ClearSelection(); _ten.Focus(); }

    private void Save()
    {
        if (Msg.Run(() => _bll.Save(new NhaCungCapDTO { MaNCC = _id, TenNCC = _ten.Text, SDT = _sdt.Text, Email = _email.Text, DiaChi = _diaChi.Text })))
        { LoadList(); Clear(); Msg.Info("Đã lưu nhà cung cấp."); }
    }

    private void Delete()
    {
        if (_id == 0) { Msg.Warn("Hãy chọn nhà cung cấp cần xóa."); return; }
        if (!Ask("Xóa nhà cung cấp này?")) return;
        if (Msg.Run(() => _bll.Delete(_id))) { LoadList(); Clear(); }
    }
}
