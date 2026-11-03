using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

public class ucSanPham : PageBase
{
    private readonly SanPhamBLL _bll = new();
    private readonly DataGridView _grid = Theme.Grid();
    private readonly TextBox _search = Theme.Input(210, "Tìm theo tên hoặc mã sản phẩm...");
    private readonly ComboBox _filterDM = Theme.Combo(140), _filterHang = Theme.Combo(140);
    private readonly CheckBox _onlyActive = new() { Text = "Đang kinh doanh", AutoSize = true, Font = Theme.Base, Margin = new Padding(4, 6, 6, 0) };

    private readonly TextBox _ten = Theme.Input(), _giaNhap = Theme.Input(), _giaBan = Theme.Input(), _bh = Theme.Input(), _moTa = Theme.Input();
    private readonly ComboBox _dm = Theme.Combo(), _hang = Theme.Combo();
    private readonly CheckBox _active = new() { Text = "Đang kinh doanh", AutoSize = true, Checked = true, Font = Theme.Base };
    private readonly PictureBox _pic = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
    private readonly Panel _picHost = new() { Width = 150, Height = 120, BackColor = ColorTranslator.FromHtml("#F8FAFC"), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0) };
    private readonly Label _picPlaceholder = new() { Dock = DockStyle.Fill, Text = "Chọn sản phẩm để xem ảnh\nhoặc chọn ảnh riêng", TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Muted, Font = Theme.Small, UseMnemonic = false };
    private readonly Label _imageNote = new()
    {
        Dock = DockStyle.Bottom, Height = 20, TextAlign = ContentAlignment.MiddleCenter,
        Font = Theme.Small, UseMnemonic = false, Visible = false
    };
    private readonly ToolTip _imageTip = new();
    private string? _imageName;
    private string? _imageCategory;
    private int _currentId;
    private bool _loading;
    private bool _restoringSelection;
    private Components.ModernButton _btnSave = null!, _btnDelete = null!, _btnNew = null!, _btnImg = null!, _btnIcon = null!;

    public ucSanPham() : base("Sản phẩm")
    {
        Name = "ucSanPham";
        _grid.AutoGenerateColumns = false;
        _grid.Name = "gridSanPham";
        _grid.Columns.AddRange(
            Theme.Col("MaSP", "Mã SP", weight: 0.45f),
            Theme.Col("TenSP", "Tên sản phẩm", weight: 3.2f),
            Theme.Col("TenDM", "Danh mục", weight: 1f),
            Theme.Col("TenHang", "Hãng", weight: 1f),
            Theme.Col("GiaNhap", "Giá nhập", weight: 0.9f, format: "N0", right: true),
            Theme.Col("GiaBan", "Giá bán", weight: 0.9f, format: "N0", right: true),
            Theme.Col("SoLuongTon", "SL", weight: 0.45f, right: true),
            Theme.Col("TrangThaiText", "Trạng thái", "TrangThai", weight: 0.75f));
        if (!IsAdmin) _grid.Columns["GiaNhap"].Visible = false;
        _grid.SizeChanged += (_, _) => ApplyResponsiveColumns();

        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].Name == "TrangThaiText" && e.Value is bool b) { e.Value = b ? "Đang bán" : "Ngừng KD"; e.FormattingApplied = true; }
            if (_grid.Columns[e.ColumnIndex].Name == "SoLuongTon" && e.Value is int t && t <= SanPhamBLL.NguongSapHet)
            { e.CellStyle!.ForeColor = Theme.Danger; e.CellStyle.Font = Theme.Bold; }
        };
        _grid.SelectionChanged += (_, _) => ShowSelected();

        var btnRefresh = Theme.GhostButton("Làm mới", 78, (_, _) => LoadProducts());
        var btnAdd = Theme.PrimaryButton("+ Thêm mới", 100, (_, _) => BeginNewProduct()); btnAdd.Name = "btnThemSanPham"; btnAdd.Enabled = IsAdmin;
        var infoSP = Theme.InfoBadge(
            "• Quản lý thông tin linh kiện, giá niêm yết, thời gian bảo hành.\n" +
            "• Số lượng tồn kho được quản lý tự động: Tăng qua Phiếu nhập, giảm qua Bán hàng POS, hoàn trả khi Hủy hóa đơn.\n" +
            "• Nhân viên bán hàng chỉ xem Giá bán. Giá nhập được bảo mật (chỉ Quản trị viên mới được xem/sửa).\n" +
            "• Ở cửa sổ hẹp, nhóm hàng, hãng và giá xem trong khung chi tiết bên phải để bảng dễ đọc.",
            "Quy tắc Quản lý Sản phẩm");
        AddRow(Toolbar(btnAdd, Theme.SearchField(_search, "Tìm tên hoặc mã SP"), _filterDM, _filterHang, _onlyActive, btnRefresh, ExportButton(_grid, "DanhSachSanPham", 88), infoSP));

        _search.Name = "txtTimSanPham";
        _filterDM.SelectedIndexChanged += (_, _) => { if (!_loading) LoadProducts(); };
        _filterHang.SelectedIndexChanged += (_, _) => { if (!_loading) LoadProducts(); };
        _onlyActive.CheckedChanged += (_, _) => { if (!_loading) LoadProducts(); };
        Debounce(_search, LoadProducts);

        _ten.MaxLength = 200; _moTa.MaxLength = 500; _moTa.Multiline = true; _moTa.Height = 42;
        _active.Dock = DockStyle.Top; _active.Height = 27; _active.Text = "Đang bán";
        _btnNew = Theme.GhostButton("Mới", 64, (_, _) => BeginNewProduct());
        _btnSave = Theme.PrimaryButton("Lưu", 70, (_, _) => Save()); _btnSave.Name = "btnLuuSanPham";
        _btnDelete = Theme.DangerButton("Xóa", 70, (_, _) => Delete());
        _btnImg = Theme.GhostButton("Chọn ảnh", 100, (_, _) => PickImage());
        _btnIcon = Theme.GhostButton("Chọn icon", 100, (_, _) => PickIcon());
        _picHost.Controls.Add(_pic);
        _picHost.Controls.Add(_picPlaceholder);
        _picHost.Controls.Add(_imageNote);
        var btnCol = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(12, 0, 0, 0) };
        btnCol.Controls.Add(_btnImg);
        _btnIcon.Margin = new Padding(0, 8, 0, 0); // space between buttons
        btnCol.Controls.Add(_btnIcon);
        
        var picRow = new FlowLayoutPanel
        {
            Width = 270, Height = 125, AutoSize = false, WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0),
            Padding = new Padding(0), BackColor = Color.Transparent
        };
        picRow.Controls.Add(_picHost);
        picRow.Controls.Add(btnCol);
        Control giaControl = IsAdmin
            ? Theme.Pair(Theme.Field("Giá nhập (₫)", _giaNhap, 150), Theme.Field("Giá bán (₫) *", _giaBan, 150))
            : Theme.Field("Giá bán (₫) *", _giaBan, 310);

        var editor = EditorCard("Thông tin sản phẩm",
            Theme.Field("Tên sản phẩm *", _ten, 310),
            Theme.Pair(Theme.Field("Danh mục *", _dm, 150), Theme.Field("Hãng *", _hang, 150)),
            giaControl,
            Theme.Pair(Theme.Field("Bảo hành (tháng)", _bh, 150), Theme.Field("Trạng thái", _active, 150)),
            Theme.Field("Mô tả", _moTa, 310),
            picRow,
            Theme.Caption("Số lượng tồn không nhập tay: tăng khi Nhập kho, giảm khi Bán hàng."),
            Theme.Row(_btnSave, _btnDelete, _btnNew));
        AddRow(TwoCols(GridCard(_grid), editor, 400), fill: true);
        ApplyResponsiveColumns();

        var canEdit = IsAdmin;
        foreach (var c in new Control[] { _ten, _giaNhap, _giaBan, _bh, _moTa, _dm, _hang, _active, _btnSave, _btnDelete, _btnImg, _btnIcon }) c.Enabled = canEdit;
        if (!canEdit) _btnNew.Enabled = false;
        TrackChanges(_ten, _giaNhap, _giaBan, _bh, _moTa, _dm, _hang, _active);
        MarkTrackedChangesClean();
    }

    public override bool HasUnsavedChanges => HasTrackedChanges;
    public override bool ConfirmDiscardOnNavigation => true;
    public override void LoadData()
    {
        _loading = true;
        var dms = new DanhMucBLL().GetAll();
        var hangs = new HangBLL().GetAll();
        _filterDM.DataSource = new[] { new DanhMucDTO { MaDM = 0, TenDM = "Mọi danh mục" } }.Concat(dms).ToList();
        _filterDM.DisplayMember = "TenDM"; _filterDM.ValueMember = "MaDM";
        _filterHang.DataSource = new[] { new HangDTO { MaHang = 0, TenHang = "Mọi hãng" } }.Concat(hangs).ToList();
        _filterHang.DisplayMember = "TenHang"; _filterHang.ValueMember = "MaHang";
        _dm.DataSource = dms; _dm.DisplayMember = "TenDM"; _dm.ValueMember = "MaDM";
        _hang.DataSource = hangs; _hang.DisplayMember = "TenHang"; _hang.ValueMember = "MaHang";
        _loading = false;
        ClearForm();
        LoadProducts();
    }

    private void LoadProducts()
    {
        if (_loading) return;
        Msg.Run(() =>
        {
            var list = _bll.Search(_search.Text, (int)(_filterDM.SelectedValue ?? 0), (int)(_filterHang.SelectedValue ?? 0), _onlyActive.Checked);
            if (HasTrackedChanges && _currentId > 0 && !list.Any(s => s.MaSP == _currentId))
            {
                // Roll filters back so the edited product stays visible and cannot be confused with another row.
                _loading = true;
                try
                {
                    SetSearchWithoutDebounce(_search, "");
                    _filterDM.SelectedValue = 0;
                    _filterHang.SelectedValue = 0;
                    _onlyActive.Checked = false;
                }
                finally { _loading = false; }
                list = _bll.Search("", 0, 0, false);
                _grid.DataSource = list;
                SelectById(_currentId);
                Msg.Info($"Bản nháp sản phẩm #{_currentId} được giữ. Đã xóa bộ lọc để hiển thị đúng sản phẩm đang sửa.");
                return;
            }
            _grid.DataSource = list;
            if (list.Count == 0 && !HasTrackedChanges) ClearForm();
        });
    }

    private void ApplyResponsiveColumns()
    {
        if (_grid.Columns.Count == 0) return;
        var compact = _grid.ClientSize.Width < 800;
        _grid.Columns["TenDM"].Visible = !compact;
        _grid.Columns["TenHang"].Visible = !compact;
        _grid.Columns["GiaBan"].Visible = !compact;
        _grid.Columns["GiaNhap"].Visible = IsAdmin && !compact;
    }

    private void ShowSelected()
    {
        if (_restoringSelection || _grid.CurrentRow?.DataBoundItem is not SanPhamDTO s) return;
        if (HasTrackedChanges && _currentId == s.MaSP) return;
        if (HasTrackedChanges && _currentId != s.MaSP)
        {
            if (!Msg.Confirm("Thông tin sản phẩm đang sửa chưa lưu. Bỏ thay đổi và chọn sản phẩm khác?"))
            {
                _restoringSelection = true;
                try
                {
                    if (!SelectById(_currentId))
                    {
                        _loading = true;
                        try
                        {
                            SetSearchWithoutDebounce(_search, "");
                            _filterDM.SelectedValue = 0;
                            _filterHang.SelectedValue = 0;
                            _onlyActive.Checked = false;
                        }
                        finally { _loading = false; }
                        _grid.DataSource = _bll.Search("", 0, 0, false);
                        SelectById(_currentId);
                    }
                }
                finally { _restoringSelection = false; }
                return;
            }
        }
        SetTrackedValues(() =>
        {
            _currentId = s.MaSP;
            _ten.Text = s.TenSP;
            _dm.SelectedValue = s.MaDM; _hang.SelectedValue = s.MaHang;
            _giaNhap.Text = IsAdmin ? Theme.Num(s.GiaNhap) : "***";
            _giaBan.Text = Theme.Num(s.GiaBan);
            _bh.Text = s.ThoiGianBH.ToString(); _moTa.Text = s.MoTa ?? "";
            _active.Checked = s.TrangThai;
            _imageName = s.HinhAnh;
            _imageCategory = s.TenDM;
            ShowImage();
        });
    }

    private void ShowImage()
    {
        _pic.Image?.Dispose(); _pic.Image = null;
        _pic.Image = Services.ProductImage.LoadForProduct(_imageName, _imageCategory, _ten.Text);
        _picPlaceholder.Visible = _pic.Image == null;
        _imageNote.Visible = _pic.Image != null;
        if (_pic.Image == null)
        {
            return;
        }

        if (Services.ProductImage.IsIcon(_imageName))
        {
            _imageNote.Text = "Biểu tượng";
            _imageNote.ForeColor = Theme.Accent;
            _imageNote.BackColor = ColorTranslator.FromHtml("#EDE9FE");
            _imageTip.SetToolTip(_imageNote, "Biểu tượng minh họa, không phải ảnh sản phẩm.");
        }
        else if (Services.ProductImage.IsNamedReferencePhoto(_imageName, _imageCategory, _ten.Text))
        {
            _imageNote.Text = "Ảnh tham khảo";
            _imageNote.ForeColor = ColorTranslator.FromHtml("#92400E");
            _imageNote.BackColor = ColorTranslator.FromHtml("#FEF3C7");
            _imageTip.SetToolTip(_imageNote, Services.ProductImage.GetNamedReferenceDescription(_ten.Text));
        }
        else if (Services.ProductImage.IsCategoryFallback(_imageName, _imageCategory, _ten.Text))
        {
            _imageNote.Text = "Ảnh đại diện";
            _imageNote.ForeColor = ColorTranslator.FromHtml("#92400E");
            _imageNote.BackColor = ColorTranslator.FromHtml("#FEF3C7");
            _imageTip.SetToolTip(_imageNote, "Ảnh minh họa theo danh mục, không đúng model SKU.");
        }
        else if (Services.ProductImage.IsSkuPhoto(_imageName, _imageCategory, _ten.Text))
        {
            _imageNote.Text = "Ảnh SKU";
            _imageNote.ForeColor = Theme.Success;
            _imageNote.BackColor = ColorTranslator.FromHtml("#DCFCE7");
            _imageTip.SetToolTip(_imageNote, "Ảnh đúng biến thể sản phẩm đã được đối chiếu.");
        }
        else if (!string.IsNullOrWhiteSpace(_imageName))
        {
            _imageNote.Text = "Ảnh riêng";
            _imageNote.ForeColor = Theme.Success;
            _imageNote.BackColor = ColorTranslator.FromHtml("#DCFCE7");
            _imageTip.SetToolTip(_imageNote, "Ảnh đã chọn riêng cho sản phẩm này.");
        }
        else
        {
            _imageNote.Text = "Ảnh tham khảo";
            _imageNote.ForeColor = ColorTranslator.FromHtml("#92400E");
            _imageNote.BackColor = ColorTranslator.FromHtml("#FEF3C7");
            _imageTip.SetToolTip(_imageNote, "Ảnh minh họa theo danh mục, không đúng model SKU.");
        }
    }

    private void PickImage()
    {
        using var dlg = new OpenFileDialog { Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.bmp", Title = "Chọn ảnh sản phẩm" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        Msg.Run(() =>
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Images");
            Directory.CreateDirectory(dir);
            _imageName = $"sp_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(dlg.FileName).ToLowerInvariant()}";
            File.Copy(dlg.FileName, Path.Combine(dir, _imageName), true);
            MarkTrackedChangesDirty();
            ShowImage();
        });
    }

    private void PickIcon()
    {
        using var f = new frmChonIcon();
        if (f.ShowDialog() == DialogResult.OK)
        {
            _imageName = f.SelectedIcon;
            MarkTrackedChangesDirty();
            ShowImage();
        }
    }

    private void BeginNewProduct()
    {
        if (HasTrackedChanges && !Ask("Bỏ các thay đổi sản phẩm chưa lưu và tạo sản phẩm mới?")) return;
        ClearForm();
    }

    private void ClearForm()
    {
        SetTrackedValues(() =>
        {
            _currentId = 0; _imageName = null; _imageCategory = null;
            _ten.Clear(); _giaNhap.Text = IsAdmin ? "0" : "***"; _giaBan.Text = ""; _bh.Text = "12"; _moTa.Clear(); _active.Checked = true;
            _pic.Image?.Dispose(); _pic.Image = null;
            _picPlaceholder.Text = "Chọn sản phẩm để xem ảnh\nhoặc chọn ảnh riêng";
            _picPlaceholder.Visible = true;
            _imageNote.Visible = false;
            _grid.CurrentCell = null; _grid.ClearSelection();
        });
        _ten.Focus();
    }

    private void Save()
    {
        Msg.Run(() =>
        {
            var s = new SanPhamDTO
            {
                MaSP = _currentId, TenSP = _ten.Text, MaDM = _dm.SelectedValue is int d ? d : 0, MaHang = _hang.SelectedValue is int h ? h : 0,
                GiaNhap = ParseMoney(_giaNhap.Text.Length == 0 ? "0" : _giaNhap.Text, "Giá nhập"), GiaBan = ParseMoney(_giaBan.Text, "Giá bán"),
                ThoiGianBH = ParseInt(_bh.Text.Length == 0 ? "0" : _bh.Text, "Bảo hành"), MoTa = _moTa.Text, HinhAnh = _imageName, TrangThai = _active.Checked
            };
            SanPhamBLL.Validate(s);
            var warn = SanPhamBLL.Warning(s);
            if (warn != null && !Ask(warn)) return;
            var id = _bll.Save(s);
            MarkTrackedChangesClean();
            LoadProducts();
            SelectById(id);
            Msg.Info("Đã lưu sản phẩm.");
        });
    }

    private void Delete()
    {
        if (_currentId == 0) { Msg.Warn("Hãy chọn sản phẩm cần xóa."); return; }
        if (!Ask("Xóa sản phẩm này? Nếu đã có giao dịch, sản phẩm sẽ chuyển sang trạng thái ngừng kinh doanh.")) return;
        Msg.Run(() =>
        {
            var removed = _bll.Delete(_currentId);
            MarkTrackedChangesClean();
            LoadProducts();
            Msg.Info(removed ? "Đã xóa sản phẩm." : "Sản phẩm đã có giao dịch nên được chuyển sang ngừng kinh doanh.");
        });
    }

    private bool SelectById(int id)
    {
        if (id <= 0) { _grid.CurrentCell = null; _grid.ClearSelection(); return true; }
        foreach (DataGridViewRow r in _grid.Rows)
            if (r.DataBoundItem is SanPhamDTO s && s.MaSP == id) { r.Selected = true; _grid.CurrentCell = r.Cells[1]; return true; }
        _grid.CurrentCell = null;
        _grid.ClearSelection();
        return false;
    }
}


