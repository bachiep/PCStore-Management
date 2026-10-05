using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

/// <summary>Quản lý nhân viên và tài khoản đăng nhập (chỉ Admin).</summary>
public class ucNhanVien : PageBase
{
    private readonly NhanVienBLL _bll = new();
    private readonly TaiKhoanBLL _tk = new();
    private readonly DataGridView _grid = Theme.Grid();
    private readonly TextBox _search = Theme.Input(260, "Tìm theo tên, SĐT hoặc tài khoản...");
    private readonly TextBox _ten = Theme.Input(), _sdt = Theme.Input(), _email = Theme.Input(), _diaChi = Theme.Input(), _chucVu = Theme.Input();
    private readonly ComboBox _gioiTinh = Theme.Combo();
    private readonly DateTimePicker _ngaySinh = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Font = Theme.Base, Checked = false };
    private readonly CheckBox _dangLam = new() { Text = "Đang làm việc", AutoSize = true, Checked = true, Font = Theme.Base };
    private readonly TextBox _username = Theme.Input(), _password = Theme.Input();
    private readonly ComboBox _role = Theme.Combo();
    private readonly CheckBox _accActive = new() { Text = "Tài khoản hoạt động", AutoSize = true, Checked = true, Font = Theme.Base };
    private NhanVienDTO? _selected;
    private int _id;
    private readonly TabControl _tabs = new();

    public ucNhanVien() : base("Nhân viên & tài khoản")
    {
        Name = "ucNhanVien";
        _grid.AutoGenerateColumns = false; _grid.Name = "gridNhanVien";
        _grid.Columns.AddRange(Theme.Col("MaNV", "Mã", weight: 0.4f), Theme.Col("HoTen", "Họ tên", weight: 1.9f),
            Theme.Col("SDT", "Điện thoại", weight: 1.2f), Theme.Col("ChucVu", "Chức vụ", weight: 1.5f), Theme.Col("TrangThaiText", "Làm việc", "TrangThai", weight: 1f),
            Theme.Col("TenDangNhap", "Tài khoản", weight: 1f), Theme.Col("VaiTro", "Vai trò", weight: 0.9f));
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name == "TrangThaiText" && e.Value is bool b) { e.Value = b ? "Đang làm" : "Đã nghỉ"; e.FormattingApplied = true; }
        };
        _grid.SelectionChanged += (_, _) => ShowSelected();
        _search.Name = "txtTimNhanVien";
        Debounce(_search, LoadList);
        var add = Theme.PrimaryButton("+ Thêm nhân viên", 150, (_, _) => { Clear(); _tabs.SelectedIndex = 0; }); add.Name = "btnThemNhanVien";
        AddRow(Toolbar(add, _search, Theme.GhostButton("Làm mới", 90, (_, _) => LoadList())));

        _gioiTinh.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
        _role.Items.AddRange(new object[] { VaiTroConst.NhanVien, VaiTroConst.Admin }); _role.SelectedIndex = 0;
        _ten.MaxLength = 100; _sdt.MaxLength = 15; _email.MaxLength = 100; _diaChi.MaxLength = 200; _chucVu.MaxLength = 50; _chucVu.Text = "Nhân viên bán hàng";
        _username.MaxLength = 50; _password.UseSystemPasswordChar = true; _password.MaxLength = 100;
        _ten.Name = "txtTenNhanVien"; _username.Name = "txtTenDangNhapMoi"; _password.Name = "txtMatKhauMoi";

        var save = Theme.PrimaryButton("Lưu nhân viên", 130, (_, _) => Save()); save.Name = "btnLuuNhanVien";
        var neu = Theme.GhostButton("Làm mới ô", 90, (_, _) => Clear());
        var delNv = Theme.DangerButton("Xóa", 80, (_, _) => DeleteEmployee()); delNv.Name = "btnXoaNhanVien";
        var create = Theme.PrimaryButton("Tạo tài khoản", 120, (_, _) => CreateAccount()); create.Name = "btnTaoTaiKhoan";
        var update = Theme.GhostButton("Cập nhật quyền", 130, (_, _) => UpdateAccount());
        var reset = Theme.GhostButton("Đặt lại mật khẩu", 140, (_, _) => ResetPassword());

        _dangLam.Dock = DockStyle.Top; _dangLam.Height = 27; _dangLam.Text = "Đang làm việc";
        _accActive.Dock = DockStyle.Top; _accActive.Height = 27; _accActive.Text = "Tài khoản hoạt động";

        var profile = EditorCard("Hồ sơ nhân viên",
            Theme.Field("Họ tên *", _ten, 330),
            Theme.Pair(Theme.Field("Giới tính", _gioiTinh, 150), Theme.Field("Ngày sinh", _ngaySinh, 170)),
            Theme.Pair(Theme.Field("Điện thoại", _sdt, 160), Theme.Field("Chức vụ", _chucVu, 160)),
            Theme.Field("Email", _email, 330), Theme.Field("Địa chỉ", _diaChi, 330),
            Theme.Field("Trạng thái", _dangLam, 330),
            Theme.Row(save, delNv, neu));
        var account = EditorCard("Tài khoản đăng nhập",
            Theme.Caption("Chọn một nhân viên ở bảng bên trái, rồi tạo hoặc chỉnh tài khoản cho người đó."),
            Theme.Field("Tên đăng nhập", _username, 330), 
            Theme.Field("Mật khẩu (tạo mới)", _password, 330),
            Theme.Field("Vai trò", _role, 330), 
            Theme.Field("Trạng thái", _accActive, 330),
            Theme.Row(create, update), Theme.Row(reset));
        profile.Dock = account.Dock = DockStyle.Fill; profile.Margin = account.Margin = new Padding(0);

        var tabs = _tabs; tabs.Dock = DockStyle.Fill; tabs.Font = Theme.Base; tabs.Margin = new Padding(14, 0, 0, 12); tabs.Padding = new Point(14, 6);
        var t1 = new TabPage("Hồ sơ") { BackColor = Theme.Page, Padding = new Padding(0, 6, 0, 0) };
        var t2 = new TabPage("Tài khoản") { BackColor = Theme.Page, Padding = new Padding(0, 6, 0, 0) };
        t1.Controls.Add(profile); t2.Controls.Add(account);
        tabs.TabPages.Add(t1); tabs.TabPages.Add(t2);
        AddRow(TwoCols(GridCard(_grid), tabs, 400), fill: true);
    }

    public override void LoadData() { LoadList(); Clear(); }
    private void LoadList() => Msg.Run(() => _grid.DataSource = _bll.Search(_search.Text));

    private void ShowSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not NhanVienDTO n) return;
        _selected = n; _id = n.MaNV;
        _ten.Text = n.HoTen; _gioiTinh.Text = n.GioiTinh ?? ""; _sdt.Text = n.SDT ?? ""; _email.Text = n.Email ?? ""; _diaChi.Text = n.DiaChi ?? "";
        _chucVu.Text = n.ChucVu; _dangLam.Checked = n.TrangThai;
        if (n.NgaySinh.HasValue) { _ngaySinh.Checked = true; _ngaySinh.Value = n.NgaySinh.Value; } else _ngaySinh.Checked = false;
        _username.Text = n.TenDangNhap ?? ""; _username.ReadOnly = n.TenDangNhap != null; _password.Clear();
        _role.SelectedItem = n.VaiTro ?? VaiTroConst.NhanVien; _accActive.Checked = n.TaiKhoanHoatDong ?? true;
    }

    private void Clear()
    {
        _grid.CurrentCell = null; _selected = null; _id = 0; _ten.Clear(); _gioiTinh.SelectedIndex = -1; _sdt.Clear(); _email.Clear(); _diaChi.Clear();
        _chucVu.Text = "Nhân viên bán hàng"; _dangLam.Checked = true; _ngaySinh.Checked = false; _username.Clear(); _username.ReadOnly = false; _password.Clear();
        _role.SelectedIndex = 0; _accActive.Checked = true; _grid.ClearSelection(); _ten.Focus();
    }

    private void DeleteEmployee()
    {
        if (_selected == null) { Msg.Warn("Hãy chọn nhân viên cần xóa."); return; }
        if (!Ask($"Xóa nhân viên \"{_selected.HoTen}\" cùng tài khoản đăng nhập?")) return;
        if (Msg.Run(() => _bll.Delete(_selected.MaNV))) { LoadList(); Clear(); Msg.Info("Đã xóa nhân viên."); }
    }

    private void Save()
    {
        if (Msg.Run(() => _bll.Save(new NhanVienDTO
        {
            MaNV = _id, HoTen = _ten.Text, GioiTinh = string.IsNullOrWhiteSpace(_gioiTinh.Text) ? null : _gioiTinh.Text, SDT = _sdt.Text, Email = _email.Text,
            DiaChi = _diaChi.Text, ChucVu = string.IsNullOrWhiteSpace(_chucVu.Text) ? "Nhân viên bán hàng" : _chucVu.Text.Trim(),
            NgaySinh = _ngaySinh.Checked ? _ngaySinh.Value.Date : null, TrangThai = _dangLam.Checked
        })) > 0)
        { LoadList(); Clear(); Msg.Info("Đã lưu nhân viên. Chọn nhân viên trong bảng để tạo tài khoản đăng nhập."); }
    }

    private void CreateAccount()
    {
        if (_selected == null) { Msg.Warn("Hãy chọn một nhân viên để tạo tài khoản."); return; }
        if (Msg.Run(() => _tk.TaoTaiKhoan(_selected.MaNV, _username.Text.Trim(), _password.Text, _role.SelectedItem?.ToString() ?? VaiTroConst.NhanVien)))
        { LoadList(); Msg.Info("Đã tạo tài khoản."); }
    }

    private void UpdateAccount()
    {
        if (_selected?.TenDangNhap == null) { Msg.Warn("Nhân viên này chưa có tài khoản."); return; }
        if (Msg.Run(() => _tk.CapNhatQuyen(_selected.TenDangNhap, _role.SelectedItem?.ToString() ?? VaiTroConst.NhanVien, _accActive.Checked)))
        { LoadList(); Msg.Info("Đã cập nhật quyền tài khoản."); }
    }

    private void ResetPassword()
    {
        if (_selected?.TenDangNhap == null) { Msg.Warn("Nhân viên này chưa có tài khoản."); return; }
        var pw = InputDialog.Ask(this, "Đặt lại mật khẩu", $"Mật khẩu mới cho \"{_selected.TenDangNhap}\" (≥ 6 ký tự)", password: true);
        if (pw == null) return;
        if (Msg.Run(() => _tk.DatLaiMatKhau(_selected.TenDangNhap, pw))) Msg.Info("Đã đặt lại mật khẩu.");
    }
}
