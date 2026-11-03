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
    private readonly DateTimePicker _ngaySinh = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Theme.Base, ShowCheckBox = true, Checked = false };
    private readonly CheckBox _dangLam = new() { Text = "Đang làm việc", AutoSize = true, Checked = true, Font = Theme.Base };
    private readonly TextBox _username = Theme.Input(), _password = Theme.Input();
    private readonly ComboBox _role = Theme.Combo();
    private readonly CheckBox _accActive = new() { Text = "Tài khoản hoạt động", AutoSize = true, Checked = true, Font = Theme.Base };
    private NhanVienDTO? _selected;
    private int _id;
    private readonly TabControl _tabs = new();
    private bool _restoringSelection;

    public ucNhanVien() : base("Nhân viên & tài khoản")
    {
        Name = "ucNhanVien";
        _grid.AutoGenerateColumns = false; _grid.Name = "gridNhanVien";
        var employeeName = Theme.Col("HoTen", "Họ tên", weight: 1.7f);
        employeeName.MinimumWidth = 145;
        var position = Theme.Col("ChucVu", "Chức vụ", weight: 1.5f);
        position.MinimumWidth = 130;
        var workStatus = Theme.Col("TrangThaiText", "Trạng thái", "TrangThai", weight: 1.05f);
        workStatus.MinimumWidth = 96;
        _grid.Columns.AddRange(Theme.Col("MaNV", "Mã", weight: 0.45f), employeeName,
            Theme.Col("SDT", "Điện thoại", weight: 1.3f), position, workStatus);
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name == "TrangThaiText" && e.Value is bool b) { e.Value = b ? "Đang làm" : "Đã nghỉ"; e.FormattingApplied = true; }
        };
        _grid.SelectionChanged += (_, _) => ShowSelected();
        _search.Name = "txtTimNhanVien";
        Debounce(_search, LoadList);
        var add = Theme.PrimaryButton("+ Thêm nhân viên", 150, (_, _) => { if (BeginNew()) _tabs.SelectedIndex = 0; }); add.Name = "btnThemNhanVien";
        AddRow(Toolbar(add, Theme.SearchField(_search, "Tìm tên, số điện thoại hoặc tài khoản"), Theme.GhostButton("Làm mới", 90, (_, _) => LoadList())));

        _gioiTinh.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
        _role.Items.AddRange(new object[] { VaiTroConst.NhanVien, VaiTroConst.Admin }); _role.SelectedIndex = 0;
        _ten.MaxLength = 100; _sdt.MaxLength = 15; _email.MaxLength = 100; _diaChi.MaxLength = 200; _chucVu.MaxLength = 50; _chucVu.Text = "Nhân viên bán hàng";
        _username.MaxLength = 50; _password.UseSystemPasswordChar = true; _password.MaxLength = 100;
        _ten.Name = "txtTenNhanVien"; _username.Name = "txtTenDangNhapMoi"; _password.Name = "txtMatKhauMoi";
        var birthdayEditor = new Panel { Width = 170, Height = 24, Margin = new Padding(0), BackColor = Color.Transparent };
        _ngaySinh.Dock = DockStyle.Fill;
        birthdayEditor.Controls.Add(_ngaySinh);

        var save = Theme.PrimaryButton("Lưu nhân viên", 130, (_, _) => Save()); save.Name = "btnLuuNhanVien";
        var neu = Theme.GhostButton("Mới", 64, (_, _) => BeginNew());
        var delNv = Theme.DangerButton("Xóa", 80, (_, _) => DeleteEmployee()); delNv.Name = "btnXoaNhanVien";
        var create = Theme.PrimaryButton("Tạo tài khoản", 140, (_, _) => CreateAccount()); create.Name = "btnTaoTaiKhoan";
        var update = Theme.GhostButton("Cập nhật quyền", 140, (_, _) => UpdateAccount());
        var reset = Theme.GhostButton("Đặt lại mật khẩu", 140, (_, _) => ResetPassword());

        _dangLam.Dock = DockStyle.Top; _dangLam.Height = 27; _dangLam.Text = "Đang làm việc";
        _accActive.Dock = DockStyle.Top; _accActive.Height = 27; _accActive.Text = "Tài khoản hoạt động";

        var profile = EditorCard("Hồ sơ nhân viên",
            Theme.Field("Họ tên *", _ten, 330),
            Theme.Pair(Theme.Field("Giới tính", _gioiTinh, 150), Theme.Field("Ngày sinh", birthdayEditor, 170)),
            Theme.Pair(Theme.Field("Điện thoại", _sdt, 160), Theme.Field("Chức vụ", _chucVu, 160)),
            Theme.Field("Email", _email, 330), Theme.Field("Địa chỉ", _diaChi, 330),
            Theme.Field("Trạng thái", _dangLam, 330),
            Theme.Row(save, delNv, neu));
        var accountActions = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            Margin = new Padding(0), Padding = new Padding(0), BackColor = Color.Transparent
        };
        foreach (var action in new Control[] { create, update, reset })
        {
            action.Margin = new Padding(0, 0, 0, 6);
            accountActions.Controls.Add(action);
        }

        var account = EditorCard("Tài khoản đăng nhập",
            Theme.Caption("Chọn một nhân viên ở bảng bên trái, rồi tạo hoặc chỉnh tài khoản cho người đó."),
            Theme.Field("Tên đăng nhập", _username, 330), 
            Theme.Field("Mật khẩu (tạo mới)", _password, 330),
            Theme.Field("Vai trò", _role, 330), 
            Theme.Field("Trạng thái", _accActive, 330),
            accountActions);
        profile.Dock = account.Dock = DockStyle.Fill; profile.Margin = account.Margin = new Padding(0);

        var tabs = _tabs; tabs.Name = "tabsNhanVien"; tabs.Dock = DockStyle.Fill; tabs.Font = Theme.Base; tabs.Margin = new Padding(14, 0, 0, 12); tabs.Padding = new Point(14, 6);
        var t1 = new TabPage("Hồ sơ") { BackColor = Theme.Page, Padding = new Padding(0, 6, 0, 0) };
        var t2 = new TabPage("Tài khoản") { BackColor = Theme.Page, Padding = new Padding(0, 6, 0, 0) };
        t1.Controls.Add(profile); t2.Controls.Add(account);
        tabs.TabPages.Add(t1); tabs.TabPages.Add(t2);
        AddRow(TwoCols(GridCard(_grid), tabs, 400), fill: true);
        TrackChanges(_ten, _gioiTinh, _ngaySinh, _sdt, _email, _diaChi, _chucVu, _dangLam,
            _username, _password, _role, _accActive);
        MarkTrackedChangesClean();
    }

    public override bool HasUnsavedChanges => HasTrackedChanges;
    public override bool ConfirmDiscardOnNavigation => true;
    public override void LoadData() { Clear(); LoadList(); }
    private void LoadList() => Msg.Run(() =>
    {
        var list = _bll.Search(_search.Text);
        if (HasTrackedChanges && _id > 0 && !list.Any(nv => nv.MaNV == _id))
        {
            _restoringSelection = true;
            try
            {
                SetSearchWithoutDebounce(_search, "");
                list = _bll.Search("");
                _grid.DataSource = list;
                SelectById(_id);
            }
            finally { _restoringSelection = false; }
            Msg.Info($"Bản nháp nhân viên #{_id} được giữ. Đã xóa bộ lọc để hiển thị đúng nhân viên đang sửa.");
            return;
        }
        _grid.DataSource = list;
    });

    private void ShowSelected()
    {
        if (_restoringSelection || _grid.CurrentRow?.DataBoundItem is not NhanVienDTO n) return;
        if (HasTrackedChanges && _id == n.MaNV) return;
        if (HasTrackedChanges && _id != n.MaNV)
        {
            if (!Msg.Confirm("Thông tin nhân viên hoặc tài khoản đang sửa chưa lưu. Bỏ thay đổi và chọn nhân viên khác?"))
            {
                _restoringSelection = true;
                try
                {
                    if (!SelectById(_id))
                    {
                        SetSearchWithoutDebounce(_search, "");
                        _grid.DataSource = _bll.Search("");
                        SelectById(_id);
                    }
                }
                finally { _restoringSelection = false; }
                return;
            }
        }
        SetTrackedValues(() =>
        {
            _selected = n; _id = n.MaNV;
            _ten.Text = n.HoTen; _gioiTinh.Text = n.GioiTinh ?? ""; _sdt.Text = n.SDT ?? ""; _email.Text = n.Email ?? ""; _diaChi.Text = n.DiaChi ?? "";
            _chucVu.Text = n.ChucVu; _dangLam.Checked = n.TrangThai;
            _ngaySinh.Checked = n.NgaySinh.HasValue;
            if (n.NgaySinh.HasValue) _ngaySinh.Value = n.NgaySinh.Value;
            _username.Text = n.TenDangNhap ?? ""; _username.ReadOnly = n.TenDangNhap != null; _password.Clear();
            _role.SelectedItem = n.VaiTro ?? VaiTroConst.NhanVien; _accActive.Checked = n.TaiKhoanHoatDong ?? true;
        });
    }

    private void Clear()
    {
        SetTrackedValues(() =>
        {
            _grid.CurrentCell = null; _selected = null; _id = 0; _ten.Clear(); _gioiTinh.SelectedIndex = -1; _sdt.Clear(); _email.Clear(); _diaChi.Clear();
            _chucVu.Text = "Nhân viên bán hàng"; _dangLam.Checked = true; _ngaySinh.Checked = false; _username.Clear(); _username.ReadOnly = false; _password.Clear();
            _role.SelectedIndex = 0; _accActive.Checked = true; _grid.ClearSelection();
        });
        _ten.Focus();
    }

    private bool BeginNew()
    {
        if (HasTrackedChanges && !Ask("Bỏ thông tin nhân viên/tài khoản chưa lưu và nhập hồ sơ mới?")) return false;
        Clear();
        return true;
    }

    private bool SelectById(int id)
    {
        if (id <= 0) { _grid.CurrentCell = null; _grid.ClearSelection(); return true; }
        foreach (DataGridViewRow row in _grid.Rows)
            if (row.DataBoundItem is NhanVienDTO nv && nv.MaNV == id)
            {
                _grid.CurrentCell = row.Cells[0];
                row.Selected = true;
                return true;
            }
        _grid.CurrentCell = null;
        _grid.ClearSelection();
        return false;
    }

    private bool HasUnsavedProfileChanges()
    {
        if (_selected == null) return false;
        return _ten.Text != _selected.HoTen || _gioiTinh.Text != (_selected.GioiTinh ?? "")
            || _sdt.Text != (_selected.SDT ?? "") || _email.Text != (_selected.Email ?? "")
            || _diaChi.Text != (_selected.DiaChi ?? "") || _chucVu.Text != _selected.ChucVu
            || _dangLam.Checked != _selected.TrangThai
            || _ngaySinh.Checked != _selected.NgaySinh.HasValue
            || (_ngaySinh.Checked && _selected.NgaySinh.HasValue && _ngaySinh.Value.Date != _selected.NgaySinh.Value.Date);
    }

    private void DeleteEmployee()
    {
        if (_selected == null) { Msg.Warn("Hãy chọn nhân viên cần xóa."); return; }
        if (!Ask($"Xóa nhân viên \"{_selected.HoTen}\" cùng tài khoản đăng nhập?")) return;
        if (Msg.Run(() => _bll.Delete(_selected.MaNV))) { MarkTrackedChangesClean(); LoadList(); Clear(); Msg.Info("Đã xóa nhân viên."); }
    }

    private void Save()
    {
        if (Msg.Run(() => _bll.Save(new NhanVienDTO
        {
            MaNV = _id, HoTen = _ten.Text, GioiTinh = string.IsNullOrWhiteSpace(_gioiTinh.Text) ? null : _gioiTinh.Text, SDT = _sdt.Text, Email = _email.Text,
            DiaChi = _diaChi.Text, ChucVu = string.IsNullOrWhiteSpace(_chucVu.Text) ? "Nhân viên bán hàng" : _chucVu.Text.Trim(),
            NgaySinh = _ngaySinh.Checked ? _ngaySinh.Value.Date : null, TrangThai = _dangLam.Checked
        })) > 0)
        { MarkTrackedChangesClean(); LoadList(); Clear(); Msg.Info("Đã lưu nhân viên. Chọn nhân viên trong bảng để tạo tài khoản đăng nhập."); }
    }

    private void CreateAccount()
    {
        if (_selected == null) { Msg.Warn("Hãy chọn một nhân viên để tạo tài khoản."); return; }
        if (HasUnsavedProfileChanges() && MessageBox.Show("Hồ sơ nhân viên chưa lưu. Tạo tài khoản sẽ không lưu các thay đổi hồ sơ này. Tiếp tục?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        if (Msg.Run(() => _tk.TaoTaiKhoan(_selected.MaNV, _username.Text.Trim(), _password.Text, _role.SelectedItem?.ToString() ?? VaiTroConst.NhanVien)))
        {
            var profileDraftRemains = HasUnsavedProfileChanges();
            _selected.TenDangNhap = _username.Text.Trim();
            _selected.VaiTro = _role.SelectedItem?.ToString() ?? VaiTroConst.NhanVien;
            _selected.TaiKhoanHoatDong = true;
            _username.ReadOnly = true;
            _password.Clear();
            _grid.Refresh();
            if (profileDraftRemains) MarkTrackedChangesDirty();
            else MarkTrackedChangesClean();
            Msg.Info("Đã tạo tài khoản.");
        }
    }

    private void UpdateAccount()
    {
        if (_selected?.TenDangNhap == null) { Msg.Warn("Nhân viên này chưa có tài khoản."); return; }
        if (HasUnsavedProfileChanges() && MessageBox.Show("Hồ sơ nhân viên chưa lưu. Cập nhật quyền sẽ không lưu các thay đổi hồ sơ này. Tiếp tục?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        if (Msg.Run(() => _tk.CapNhatQuyen(_selected.TenDangNhap, _role.SelectedItem?.ToString() ?? VaiTroConst.NhanVien, _accActive.Checked)))
        {
            var hasProfileDraft = HasUnsavedProfileChanges();
            var hasPasswordDraft = !string.IsNullOrWhiteSpace(_password.Text);
            _selected.VaiTro = _role.SelectedItem?.ToString() ?? VaiTroConst.NhanVien;
            _selected.TaiKhoanHoatDong = _accActive.Checked;
            _grid.Refresh();
            if (hasProfileDraft || hasPasswordDraft) MarkTrackedChangesDirty();
            else MarkTrackedChangesClean();
            Msg.Info("Đã cập nhật quyền tài khoản.");
        }
    }

    private void ResetPassword()
    {
        if (_selected?.TenDangNhap == null) { Msg.Warn("Nhân viên này chưa có tài khoản."); return; }
        var pw = InputDialog.Ask(this, "Đặt lại mật khẩu", $"Mật khẩu mới cho \"{_selected.TenDangNhap}\" (≥ 6 ký tự)", password: true);
        if (pw == null) return;
        if (Msg.Run(() => _tk.DatLaiMatKhau(_selected.TenDangNhap, pw))) Msg.Info("Đã đặt lại mật khẩu.");
    }
}
