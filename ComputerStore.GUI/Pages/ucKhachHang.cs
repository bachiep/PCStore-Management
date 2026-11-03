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
    private bool _restoringSelection;

    public ucKhachHang() : base("Khách hàng")
    {
        Name = "ucKhachHang";
        _grid.AutoGenerateColumns = false; _grid.Name = "gridKhachHang";
        _grid.Columns.AddRange(Theme.Col("MaKH", "Mã", weight: 0.4f), Theme.Col("HoTen", "Họ tên", weight: 2.8f), Theme.Col("SDT", "Điện thoại", weight: 1f),
            Theme.Col("DiaChi", "Địa chỉ", weight: 0.7f), Theme.Col("SoHoaDon", "Số HĐ", weight: 0.6f, center: true),
            Theme.Col("TongChiTieu", "Tổng chi tiêu (₫)", weight: 1.1f, format: "N0", right: true));
        _grid.SizeChanged += (_, _) => ApplyResponsiveColumns();
        _grid.SelectionChanged += (_, _) => ShowSelected();
        _search.Name = "txtTimKhach";
        Debounce(_search, LoadList);
        var add = Theme.PrimaryButton("+ Thêm mới", 110, (_, _) => BeginNew()); add.Name = "btnThemKhach";
        AddRow(Toolbar(add, Theme.SearchField(_search, "Tìm theo tên hoặc số điện thoại"), Theme.GhostButton("Làm mới", 90, (_, _) => LoadList()), ExportButton(_grid, "DanhSachKhachHang")));

        _history.AutoGenerateColumns = false; _history.Name = "gridLichSuMua";
        _history.Columns.AddRange(Theme.Col("MaHD", "Mã HĐ", weight: 0.6f), Theme.Col("NgayLap", "Ngày lập", weight: 1.2f, format: "dd/MM/yy HH:mm"),
            Theme.Col("ThanhToan", "Thanh toán", weight: 1f, format: "N0", right: true), Theme.Col("TrangThaiText", "Trạng thái", weight: 0.8f));
        _history.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || _history.Columns[e.ColumnIndex].DataPropertyName != nameof(HoaDonDTO.TrangThaiText)) return;
            if (_history.Rows[e.RowIndex].DataBoundItem is HoaDonDTO { DaHuy: true })
            {
                var cellStyle = e.CellStyle;
                if (cellStyle is null) return;
                cellStyle.ForeColor = Theme.Danger;
                cellStyle.Font = Theme.Bold;
            }
        };
        _history.Height = 200;

        _ten.MaxLength = 100; _sdt.MaxLength = 15; _email.MaxLength = 100; _diaChi.MaxLength = 200; _ten.Name = "txtTenKhach"; _sdt.Name = "txtSdtKhach";
        var save = Theme.PrimaryButton("Lưu", 80, (_, _) => Save()); save.Name = "btnLuuKhach";
        var del = Theme.DangerButton("Xóa", 80, (_, _) => Delete());
        del.Visible = Session.IsAdmin;
        var neu = Theme.GhostButton("Mới", 64, (_, _) => BeginNew());
        var editor = EditorCard("Thông tin khách hàng", Theme.Field("Họ tên *", _ten, 320), Theme.Field("Số điện thoại *", _sdt, 320),
            Theme.Field("Email", _email, 320), Theme.Field("Địa chỉ", _diaChi, 320), Theme.Row(save, del, neu));
        
        var leftSplit = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        leftSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftSplit.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        leftSplit.Controls.Add(GridCard(_grid), 0, 0);
        
        var histContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        var histTitle = Theme.Caption("LỊCH SỬ MUA HÀNG CỦA KHÁCH:"); histTitle.Font = Theme.Bold; histTitle.Dock = DockStyle.Top;
        var histCard = GridCard(_history); histCard.Padding = new Padding(0, 6, 0, 0);
        histContainer.Controls.Add(histCard);
        histContainer.Controls.Add(histTitle);
        leftSplit.Controls.Add(histContainer, 0, 1);

        AddRow(TwoCols(leftSplit, editor, 380), fill: true);
        TrackChanges(_ten, _sdt, _email, _diaChi);
        MarkTrackedChangesClean();
    }

    public override bool HasUnsavedChanges => HasTrackedChanges;
    public override bool ConfirmDiscardOnNavigation => true;

    private void ApplyResponsiveColumns()
    {
        if (_grid.Columns.Count == 0) return;
        // Address remains available in the selected customer's detail form.
        _grid.Columns["DiaChi"].Visible = _grid.ClientSize.Width >= 820;
    }

    public override void LoadData() { Clear(); LoadList(); }
    private void LoadList() => Msg.Run(() =>
    {
        var list = _bll.Search(_search.Text);
        if (HasTrackedChanges && _id > 0 && !list.Any(k => k.MaKH == _id))
        {
            // Keep the row being edited visible; otherwise the editor could appear to belong to another row.
            _restoringSelection = true;
            try
            {
                SetSearchWithoutDebounce(_search, "");
                list = _bll.Search("");
                _grid.DataSource = list;
                SelectById(_id);
            }
            finally { _restoringSelection = false; }
            Msg.Info($"Bản nháp khách hàng #{_id} được giữ. Đã xóa bộ lọc để hiển thị đúng khách hàng đang sửa.");
            return;
        }
        _grid.DataSource = list;
    });

    private void ShowSelected()
    {
        if (_restoringSelection) return;
        if (_grid.CurrentRow?.DataBoundItem is not KhachHangDTO k) return;
        if (HasTrackedChanges && _id == k.MaKH) return; // Rebinding the same row must not replace an in-progress draft.
        if (HasTrackedChanges && _id != k.MaKH)
        {
            if (!Msg.Confirm("Thông tin khách hàng đang sửa chưa lưu. Bỏ thay đổi và chọn khách hàng khác?"))
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
            _id = k.MaKH; _ten.Text = k.HoTen; _sdt.Text = k.SDT; _email.Text = k.Email ?? ""; _diaChi.Text = k.DiaChi ?? "";
        });
        Msg.Run(() => _history.DataSource = new HoaDonBLL().GetByCustomer(k.MaKH));
    }

    private void Clear()
    {
        SetTrackedValues(() =>
        {
            _id = 0; _ten.Clear(); _sdt.Clear(); _email.Clear(); _diaChi.Clear(); _history.DataSource = null; _grid.CurrentCell = null; _grid.ClearSelection();
        });
        _ten.Focus();
    }

    private void BeginNew()
    {
        if (HasTrackedChanges && !Ask("Bỏ thông tin khách hàng chưa lưu và nhập khách hàng mới?")) return;
        Clear();
    }

    private bool SelectById(int id)
    {
        if (id <= 0) { _grid.CurrentCell = null; _grid.ClearSelection(); return true; }
        foreach (DataGridViewRow row in _grid.Rows)
            if (row.DataBoundItem is KhachHangDTO kh && kh.MaKH == id)
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
        if (Msg.Run(() => _bll.Save(new KhachHangDTO { MaKH = _id, HoTen = _ten.Text, SDT = _sdt.Text, Email = _email.Text, DiaChi = _diaChi.Text })) > 0)
        { MarkTrackedChangesClean(); LoadList(); Clear(); Msg.Info("Đã lưu khách hàng."); }
    }

    private void Delete()
    {
        if (_id == 0) { Msg.Warn("Hãy chọn khách hàng cần xóa."); return; }
        if (!Ask("Xóa khách hàng này?")) return;
        if (Msg.Run(() => _bll.Delete(_id))) { MarkTrackedChangesClean(); LoadList(); Clear(); }
    }
}
