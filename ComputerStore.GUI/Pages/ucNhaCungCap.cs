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
    private bool _restoringSelection;

    public ucNhaCungCap() : base("Nhà cung cấp")
    {
        Name = "ucNhaCungCap";
        _grid.AutoGenerateColumns = false; _grid.Name = "gridNhaCungCap";
        _grid.Columns.AddRange(Theme.Col("MaNCC", "Mã", weight: 0.4f), Theme.Col("TenNCC", "Tên nhà cung cấp", weight: 3.4f),
            Theme.Col("SDT", "Điện thoại", weight: 1f), Theme.Col("Email", "Email", weight: 1f), Theme.Col("DiaChi", "Địa chỉ", weight: 1.2f));
        _grid.SizeChanged += (_, _) => ApplyResponsiveColumns();
        _grid.SelectionChanged += (_, _) => ShowSelected();
        _search.Name = "txtTimNCC";
        Debounce(_search, LoadList);
        var add = Theme.PrimaryButton("+ Thêm mới", 110, (_, _) => BeginNew()); add.Name = "btnThemNCC"; add.Enabled = IsAdmin;
        AddRow(Toolbar(add, Theme.SearchField(_search, "Tìm theo tên hoặc số điện thoại"), Theme.GhostButton("Làm mới", 90, (_, _) => LoadList()), ExportButton(_grid, "DanhSachNhaCungCap")));

        _ten.MaxLength = 150; _sdt.MaxLength = 15; _email.MaxLength = 100; _diaChi.MaxLength = 200; _ten.Name = "txtTenNCC";
        var save = Theme.PrimaryButton("Lưu", 80, (_, _) => Save()); save.Name = "btnLuuNCC";
        var del = Theme.DangerButton("Xóa", 80, (_, _) => Delete());
        var neu = Theme.GhostButton("Mới", 64, (_, _) => BeginNew());
        var editor = EditorCard("Thông tin nhà cung cấp", Theme.Field("Tên nhà cung cấp *", _ten, 310), Theme.Field("Số điện thoại", _sdt, 310),
            Theme.Field("Email", _email, 310), Theme.Field("Địa chỉ", _diaChi, 310), Theme.Row(save, del, neu));
        if (!IsAdmin) foreach (var c in new Control[] { _ten, _sdt, _email, _diaChi, save, del, neu }) c.Enabled = false;
        AddRow(TwoCols(GridCard(_grid), editor, 370), fill: true);
        TrackChanges(_ten, _sdt, _email, _diaChi);
        MarkTrackedChangesClean();
    }

    public override bool HasUnsavedChanges => HasTrackedChanges;
    public override bool ConfirmDiscardOnNavigation => true;

    private void ApplyResponsiveColumns()
    {
        if (_grid.Columns.Count == 0) return;
        // The selected supplier's detail panel shows email and address in full.
        // Hide these secondary columns only when the list pane is narrow so its
        // primary identifiers and supplier names remain readable without ellipses.
        var compact = _grid.ClientSize.Width < 820;
        _grid.Columns["Email"].Visible = !compact;
        _grid.Columns["DiaChi"].Visible = !compact;
    }

    public override void LoadData() { Clear(); LoadList(); }
    private void LoadList() => Msg.Run(() =>
    {
        var list = _bll.Search(_search.Text);
        if (HasTrackedChanges && _id > 0 && !list.Any(n => n.MaNCC == _id))
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
            Msg.Info($"Bản nháp nhà cung cấp #{_id} được giữ. Đã xóa bộ lọc để hiển thị đúng nhà cung cấp đang sửa.");
            return;
        }
        _grid.DataSource = list;
    });

    private void ShowSelected()
    {
        if (_restoringSelection || _grid.CurrentRow?.DataBoundItem is not NhaCungCapDTO n) return;
        if (HasTrackedChanges && _id == n.MaNCC) return;
        if (HasTrackedChanges && _id != n.MaNCC)
        {
            if (!Msg.Confirm("Thông tin nhà cung cấp đang sửa chưa lưu. Bỏ thay đổi và chọn nhà cung cấp khác?"))
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
            _id = n.MaNCC; _ten.Text = n.TenNCC; _sdt.Text = n.SDT ?? ""; _email.Text = n.Email ?? ""; _diaChi.Text = n.DiaChi ?? "";
        });
    }

    private void Clear()
    {
        SetTrackedValues(() => { _id = 0; _ten.Clear(); _sdt.Clear(); _email.Clear(); _diaChi.Clear(); _grid.CurrentCell = null; _grid.ClearSelection(); });
        _ten.Focus();
    }

    private void BeginNew()
    {
        if (HasTrackedChanges && !Ask("Bỏ thông tin nhà cung cấp chưa lưu và nhập nhà cung cấp mới?")) return;
        Clear();
    }

    private bool SelectById(int id)
    {
        if (id <= 0) { _grid.CurrentCell = null; _grid.ClearSelection(); return true; }
        foreach (DataGridViewRow row in _grid.Rows)
            if (row.DataBoundItem is NhaCungCapDTO ncc && ncc.MaNCC == id)
            {
                _grid.CurrentCell = row.Cells[0];
                row.Selected = true;
                return true;
            }
        _grid.CurrentCell = null;
        _grid.ClearSelection();
        return false;
    }

    private void Save()
    {
        if (Msg.Run(() => _bll.Save(new NhaCungCapDTO { MaNCC = _id, TenNCC = _ten.Text, SDT = _sdt.Text, Email = _email.Text, DiaChi = _diaChi.Text })))
        { MarkTrackedChangesClean(); LoadList(); Clear(); Msg.Info("Đã lưu nhà cung cấp."); }
    }

    private void Delete()
    {
        if (_id == 0) { Msg.Warn("Hãy chọn nhà cung cấp cần xóa."); return; }
        if (!Ask("Xóa nhà cung cấp này?")) return;
        if (Msg.Run(() => _bll.Delete(_id))) { MarkTrackedChangesClean(); LoadList(); Clear(); }
    }
}
