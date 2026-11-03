using System.ComponentModel;
using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Components;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

public class ucNhapKho : PageBase
{
    private readonly PhieuNhapBLL _bll = new();
    private readonly NhaCungCapBLL _nccBll = new();
    private readonly SanPhamBLL _spBll = new();

    private readonly ComboBox _cboNCC = Theme.Combo();
    private readonly TextBox _txtGhiChu = Theme.Input(320);
    private readonly ComboBox _cboSP = Theme.Combo();
    private readonly TextBox _txtSoLuong = Theme.Input(150);
    private readonly TextBox _txtDonGia = Theme.Input(150);
    
    private readonly DataGridView _grid = Theme.Grid();
    private readonly BindingList<ChiTietPhieuNhapDTO> _items = new();
    private readonly Label _lblTongTien = new() { Font = Theme.H2, AutoSize = true, ForeColor = Theme.Accent, Margin = new Padding(0, 10, 0, 0) };

    private List<string> _tempSerials = new();
    private ChiTietPhieuNhapDTO? _editingItem;
    private readonly Components.ModernButton _btnSave;
    private readonly Components.ModernButton _btnRemove;
    private readonly Components.ModernButton _btnEdit;
    private readonly Components.ModernButton _btnCancelEdit;
    private bool _suppressDraftTracking;
    private bool _entryDirty;
    private bool _lookupsLoaded;

    public ucNhapKho() : base("Nhập kho")
    {
        Name = "ucNhapKho";

        _cboNCC.DisplayMember = "TenNCC"; _cboNCC.ValueMember = "MaNCC";
        _cboSP.DisplayMember = "TenSP"; _cboSP.ValueMember = "MaSP";

        _txtDonGia.TextChanged += (_, _) => { if (decimal.TryParse(_txtDonGia.Text.Replace(".", ""), out var v)) _txtDonGia.Text = v.ToString("N0"); _txtDonGia.SelectionStart = _txtDonGia.Text.Length; };
        _cboSP.SelectedIndexChanged += (_, _) =>
        {
            if (_cboSP.SelectedItem is not SanPhamDTO sp) return;
            if (_editingItem != null && sp.MaSP != _editingItem.MaSP) _tempSerials.Clear();
            if (sp.GiaNhap > 0) _txtDonGia.Text = sp.GiaNhap.ToString("N0");
        };
        _txtSoLuong.Text = "1";
        _txtSoLuong.TextChanged += (_, _) => { if (!_suppressDraftTracking) _entryDirty = true; };
        _txtDonGia.TextChanged += (_, _) => { if (!_suppressDraftTracking) _entryDirty = true; };
        _cboNCC.SelectedIndexChanged += (_, _) => { if (!_suppressDraftTracking) _entryDirty = true; };
        _cboSP.SelectedIndexChanged += (_, _) => { if (!_suppressDraftTracking) _entryDirty = true; };
        Theme.EmptyHint(_grid, "Phiếu nhập đang trống — chọn sản phẩm bên phải rồi bấm “Thêm vào phiếu”");

        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            Theme.Col("MaSP", "Mã", weight: 0.5f),
            Theme.Col("TenSP", "Tên sản phẩm", weight: 2f),
            Theme.Col("DonGia", "Đơn giá", weight: 1f, format: "N0", right: true),
            Theme.Col("SoLuong", "SL", weight: 0.5f, right: true),
            Theme.Col("ThanhTien", "Thành tiền", weight: 1.2f, format: "N0", right: true)
        );
        _grid.DataSource = _items;

        // ====================================================
        // PANEL BÊN PHẢI: FORM NHẬP LIỆU PHIẾU NHẬP (CARD ĐẸP)
        // ====================================================
        var editor = new ModernPanel
        {
            Dock = DockStyle.Fill,
            FillColor = Color.White,
            BorderColor = ColorTranslator.FromHtml("#E2E8F0"),
            BorderSize = 1,
            BorderRadius = 10,
            Padding = new Padding(20, 16, 20, 16),
            Margin = new Padding(12, 0, 0, 0)
        };

        var flowRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };

        var lblCardTitle = new Label
        {
            Text = "Phiếu nhập mới",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#0F172A"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
        };
        flowRight.Controls.Add(lblCardTitle);

        // --- KHỐI 1: THÔNG TIN PHIẾU NHẬP ---
        var pnlSec1Header = new Label
        {
            Text = "THÔNG TIN PHIẾU NHẬP",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#64748B"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        flowRight.Controls.Add(pnlSec1Header);

        // Trường Nhà cung cấp: [Nhà cung cấp *]              [+ Thêm NCC]
        var pnlNccHeader = new Panel { Width = 368, Height = 22, Margin = new Padding(0, 0, 0, 4), BackColor = Color.Transparent };
        var lblNcc = new Label { Text = "Nhà cung cấp *", Font = Theme.Small, ForeColor = Theme.Text, AutoSize = true, Dock = DockStyle.Left, TextAlign = ContentAlignment.BottomLeft };
        var btnNewNcc = Theme.LinkButton("+ Thêm NCC", (_, _) => QuickAddNcc());
        btnNewNcc.Name = "btnThemNCCNhanh";
        btnNewNcc.Dock = DockStyle.Right;
        pnlNccHeader.Controls.Add(lblNcc);
        pnlNccHeader.Controls.Add(btnNewNcc);
        flowRight.Controls.Add(pnlNccHeader);

        _cboNCC.Width = 368;
        _cboNCC.Height = 32;
        _cboNCC.Font = new Font("Segoe UI", 10f);
        _cboNCC.Margin = new Padding(0, 0, 0, 12);
        flowRight.Controls.Add(_cboNCC);

        // Trường Ghi chú: [Ghi chú phiếu:]
        var lblGhiChu = new Label { Text = "Ghi chú phiếu:", Font = Theme.Small, ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        _txtGhiChu.Width = 368;
        _txtGhiChu.Font = new Font("Segoe UI", 10f);
        _txtGhiChu.BorderStyle = BorderStyle.FixedSingle;
        _txtGhiChu.PlaceholderText = "Nhập ghi chú phiếu nhập (tùy chọn)...";
        _txtGhiChu.Margin = new Padding(0, 0, 0, 16);
        flowRight.Controls.Add(lblGhiChu);
        flowRight.Controls.Add(_txtGhiChu);

        // Đường phân cách nhẹ giữa 2 khối
        var divider = new Panel
        {
            Width = 368,
            Height = 1,
            BackColor = ColorTranslator.FromHtml("#E2E8F0"),
            Margin = new Padding(0, 0, 0, 16)
        };
        flowRight.Controls.Add(divider);

        // --- KHỐI 2: THÊM SẢN PHẨM VÀO PHIẾU ---
        var pnlSec2Header = new Label
        {
            Text = "THÊM LINH KIỆN VÀO PHIẾU",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#64748B"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        flowRight.Controls.Add(pnlSec2Header);

        // Trường Sản phẩm: [Chọn linh kiện *]                 [+ Thêm SP]
        var pnlSpHeader = new Panel { Width = 368, Height = 22, Margin = new Padding(0, 0, 0, 4), BackColor = Color.Transparent };
        var lblSp = new Label { Text = "Chọn linh kiện *", Font = Theme.Small, ForeColor = Theme.Text, AutoSize = true, Dock = DockStyle.Left, TextAlign = ContentAlignment.BottomLeft };
        var btnNewSp = Theme.LinkButton("+ Thêm SP", (_, _) => QuickAddSp());
        btnNewSp.Name = "btnThemSPNhanh";
        btnNewSp.Dock = DockStyle.Right;
        pnlSpHeader.Controls.Add(lblSp);
        pnlSpHeader.Controls.Add(btnNewSp);
        flowRight.Controls.Add(pnlSpHeader);

        _cboSP.Width = 368;
        _cboSP.Height = 32;
        _cboSP.Font = new Font("Segoe UI", 10f);
        _cboSP.Margin = new Padding(0, 0, 0, 12);
        flowRight.Controls.Add(_cboSP);

        // Cặp ô: Số lượng & Đơn giá nhập
        var tblPair = new TableLayoutPanel
        {
            Width = 368,
            Height = 60,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 16),
            BackColor = Color.Transparent
        };
        tblPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        tblPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        tblPair.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        tblPair.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));

        var lblSl = new Label { Text = "Số lượng *", Font = Theme.Small, ForeColor = Theme.Text, AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        var lblDg = new Label { Text = "Đơn giá nhập (₫) *", Font = Theme.Small, ForeColor = Theme.Text, AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };

        _txtSoLuong.Dock = DockStyle.Fill;
        _txtSoLuong.Font = new Font("Segoe UI", 10f);
        _txtSoLuong.BorderStyle = BorderStyle.FixedSingle;
        _txtSoLuong.TextAlign = HorizontalAlignment.Right;
        _txtSoLuong.Margin = new Padding(0, 0, 6, 0);

        _txtDonGia.Dock = DockStyle.Fill;
        _txtDonGia.Font = new Font("Segoe UI", 10f);
        _txtDonGia.BorderStyle = BorderStyle.FixedSingle;
        _txtDonGia.TextAlign = HorizontalAlignment.Right;
        _txtDonGia.Margin = new Padding(6, 0, 0, 0);

        tblPair.Controls.Add(lblSl, 0, 0);
        tblPair.Controls.Add(lblDg, 1, 0);
        tblPair.Controls.Add(_txtSoLuong, 0, 1);
        tblPair.Controls.Add(_txtDonGia, 1, 1);
        flowRight.Controls.Add(tblPair);

        // Nút Thêm vào phiếu: Chiều rộng 100%, Chiều cao 42px, Font Đậm, Không bị cắt chữ
        var btnAdd = new ModernButton
        {
            Text = "+ THÊM VÀO PHIẾU (NHẬP SERIAL)",
            Width = 368,
            Height = 42,
            BorderRadius = 8,
            NormalColor = ColorTranslator.FromHtml("#2563EB"),
            HoverColor = ColorTranslator.FromHtml("#1D4ED8"),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 12)
        };
        btnAdd.Click += (_, _) => AddItem();
        flowRight.Controls.Add(btnAdd);

        // Hộp chú thích bo góc về Serial
        var pnlNote = new ModernPanel
        {
            Width = 368,
            Height = 52,
            FillColor = ColorTranslator.FromHtml("#F8FAFC"),
            BorderColor = ColorTranslator.FromHtml("#E2E8F0"),
            BorderSize = 1,
            BorderRadius = 6,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0)
        };
        var lblNote = new Label
        {
            Text = "Lưu ý: mỗi thiết bị cần một serial riêng.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorTranslator.FromHtml("#64748B"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlNote.Controls.Add(lblNote);
        flowRight.Controls.Add(pnlNote);

        // Tự động kéo rộng các controls theo bề ngang khung
        flowRight.SizeChanged += (_, _) =>
        {
            var w = Math.Max(300, flowRight.ClientSize.Width - 12);
            pnlNccHeader.Width = w;
            _cboNCC.Width = w;
            _txtGhiChu.Width = w;
            divider.Width = w;
            pnlSpHeader.Width = w;
            _cboSP.Width = w;
            tblPair.Width = w;
            btnAdd.Width = w;
            pnlNote.Width = w;
        };

        editor.Controls.Add(flowRight);

        // ====================================================
        // PANEL BÊN DƯỚI: NÚT THAO TÁC & TỔNG TIỀN PHIẾU
        // ====================================================
        _btnRemove = Theme.DangerButton("Xóa dòng", 88, (_, _) => RemoveItem());
        _btnEdit = Theme.GhostButton("Sửa dòng", 88, (_, _) => EditItem());
        _btnCancelEdit = Theme.GhostButton("Hủy sửa", 78, (_, _) => CancelEdit());
        _btnSave = Theme.PrimaryButton("Lưu phiếu", 120, (_, _) => Save());
        var btnLichSu = Theme.GhostButton("Lịch sử", 92, (_, _) => { using var f = new frmLichSuNhap(); f.ShowDialog(this); });
        _grid.SelectionChanged += (_, _) => UpdateActionState();

        var pnlLeftBot = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };
        _btnSave.Margin = new Padding(0, 0, 8, 0);
        _btnRemove.Margin = new Padding(0, 0, 8, 0);
        _btnEdit.Margin = new Padding(0, 0, 8, 0);
        _btnCancelEdit.Margin = new Padding(0, 0, 8, 0);
        _lblTongTien.Margin = new Padding(0, 6, 8, 0);
        btnLichSu.Margin = new Padding(0);

        pnlLeftBot.Controls.Add(_btnSave);
        pnlLeftBot.Controls.Add(_btnRemove);
        pnlLeftBot.Controls.Add(_btnEdit);
        pnlLeftBot.Controls.Add(_btnCancelEdit);
        pnlLeftBot.Controls.Add(_lblTongTien);
        pnlLeftBot.Controls.Add(btnLichSu);

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        pnlLeft.Controls.Add(GridCard(_grid));
        pnlLeft.Controls.Add(pnlLeftBot);

        AddRow(TwoCols(pnlLeft, editor, 410), fill: true);
        UpdateTongTien();
    }

    public override bool HasUnsavedChanges => _items.Count > 0 || _tempSerials.Count > 0 || _editingItem != null
        || _entryDirty || !string.IsNullOrWhiteSpace(_txtGhiChu.Text);

    public override void LoadData()
    {
        var selectedSupplier = _lookupsLoaded ? _cboNCC.SelectedValue : null;
        var selectedProduct = _lookupsLoaded ? _cboSP.SelectedValue : null;
        var quantity = _txtSoLuong.Text;
        var unitPrice = _txtDonGia.Text;
        var preserveEntry = _lookupsLoaded;

        Msg.Run(() =>
        {
            _suppressDraftTracking = true;
            try
            {
                _cboNCC.DataSource = _nccBll.Search("");
                _cboSP.DataSource = _spBll.Search("");
                if (selectedSupplier != null) _cboNCC.SelectedValue = selectedSupplier;
                if (selectedProduct != null) _cboSP.SelectedValue = selectedProduct;
                if (preserveEntry)
                {
                    _txtSoLuong.Text = quantity;
                    _txtDonGia.Text = unitPrice;
                }
            }
            finally { _suppressDraftTracking = false; }
            _lookupsLoaded = true;
        });
    }

    private void QuickAddNcc()
    {
        var ten = InputDialog.Ask(this, "Thêm nhà cung cấp", "Tên nhà cung cấp mới:");
        if (string.IsNullOrWhiteSpace(ten)) return;
        var sdt = InputDialog.Ask(this, "Thêm nhà cung cấp", "Số điện thoại (có thể để trống):") ?? "";
        if (Msg.Run(() => _nccBll.Save(new NhaCungCapDTO { TenNCC = ten, SDT = sdt })))
        {
            LoadData();
            for (var i = 0; i < _cboNCC.Items.Count; i++)
                if (_cboNCC.Items[i] is NhaCungCapDTO n && n.TenNCC == ten.Trim()) { _cboNCC.SelectedIndex = i; break; }
            Msg.Info("Đã thêm nhà cung cấp.");
        }
    }

    private void QuickAddSp()
    {
        var ten = InputDialog.Ask(this, "Thêm sản phẩm mới", "Tên sản phẩm mới:");
        if (string.IsNullOrWhiteSpace(ten)) return;
        var giaNhap = InputDialog.Ask(this, "Thêm sản phẩm mới", "Giá nhập (₫):") ?? "0";
        var giaBan = InputDialog.Ask(this, "Thêm sản phẩm mới", "Giá bán (₫):") ?? "0";
        var warrantyText = InputDialog.Ask(this, "Thêm sản phẩm mới", "Thời hạn bảo hành (tháng, 0-120):");
        if (warrantyText == null) return;

        Msg.Run(() =>
        {
            var gn = ParseMoney(giaNhap == "" ? "0" : giaNhap, "Giá nhập");
            var gb = ParseMoney(giaBan == "" ? "0" : giaBan, "Giá bán");
            if (!int.TryParse(warrantyText, out var warranty) || warranty < 0 || warranty > 120)
                throw new BusinessException("Thời hạn bảo hành phải từ 0 đến 120 tháng.");
            var danhMuc = new DanhMucBLL().GetAll();
            var hang = new HangBLL().GetAll();
            var idDM = SelectReference("Chọn danh mục", danhMuc, d => d.TenDM, d => d.MaDM);
            var idHang = SelectReference("Chọn hãng", hang, h => h.TenHang, h => h.MaHang);
            if (idDM == null || idHang == null) return;

            var sp = new SanPhamDTO { TenSP = ten, GiaNhap = gn, GiaBan = gb, ThoiGianBH = warranty, MaDM = idDM.Value, MaHang = idHang.Value, TrangThai = true };
            _spBll.Save(sp);
            LoadData();
            for (var i = 0; i < _cboSP.Items.Count; i++)
                if (_cboSP.Items[i] is SanPhamDTO s && s.TenSP == ten.Trim()) { _cboSP.SelectedIndex = i; break; }
            Msg.Info("Đã thêm sản phẩm. Vui lòng vào màn hình Sản phẩm để cập nhật thêm ảnh, mô tả nếu cần.");
        });
    }

    private int? SelectReference<T>(string title, List<T> values, Func<T, string> display, Func<T, int> key)
    {
        if (values.Count == 0) throw new BusinessException($"Chưa có dữ liệu để {title.ToLowerInvariant()}. Hãy thêm trước ở màn hình Danh mục & Hãng.");
        using var dialog = new Form
        {
            Text = title, Width = 360, Height = 150, StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false
        };
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 16, Top = 14, Width = 312, DisplayMember = "Text", ValueMember = "Value" };
        combo.DataSource = values.Select(value => new { Text = display(value), Value = key(value) }).ToList();
        var ok = new Button { Text = "Chọn", DialogResult = DialogResult.OK, Left = 168, Top = 58, Width = 76 };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 252, Top = 58, Width = 76 };
        dialog.Controls.AddRange(new Control[] { combo, ok, cancel });
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog(this) == DialogResult.OK ? (int?)combo.SelectedValue : null;
    }



    private void AddItem()
    {
        if (_cboSP.SelectedItem is not SanPhamDTO sp) { Msg.Warn("Hãy chọn sản phẩm."); return; }
        if (!int.TryParse(_txtSoLuong.Text, out var sl) || sl <= 0) { Msg.Warn("Số lượng hợp lệ > 0."); return; }
        if (_items.Any(i => i.MaSP == sp.MaSP && !ReferenceEquals(i, _editingItem))) { Msg.Warn("Sản phẩm này đã có trong phiếu. Hãy sửa dòng hiện tại."); return; }
        if (!decimal.TryParse(_txtDonGia.Text.Replace(".", ""), out var dgCheck) || dgCheck <= 0) { Msg.Warn("Hãy nhập đơn giá nhập lớn hơn 0."); return; }
        using (var f = new frmNhapSerial(sp, sl, _tempSerials))
        {
            if (f.ShowDialog() != DialogResult.OK) return;
            _tempSerials = f.ResultSerials;
        }
        if (_tempSerials.Count != sl) { Msg.Warn($"Bạn cần nhập chính xác {sl} serial cho sản phẩm này."); return; }
        
        try
        {
            var dg = ParseMoney(_txtDonGia.Text == "" ? "0" : _txtDonGia.Text, "Đơn giá");
            var updated = new ChiTietPhieuNhapDTO { MaSP = sp.MaSP, TenSP = sp.TenSP, SoLuong = sl, DonGia = dg, Serials = _tempSerials.ToList() };
            if (_editingItem != null)
            {
                var index = _items.IndexOf(_editingItem);
                if (index >= 0) _items[index] = updated;
                _editingItem = null;
                _btnCancelEdit.Enabled = false;
            }
            else _items.Add(updated);
            _tempSerials.Clear();
            ResetEntryFields();
            UpdateTongTien();
        }
        catch (Exception ex) { Msg.Warn(ex.Message); }
    }

    private void RemoveItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is ChiTietPhieuNhapDTO i)
        {
            if (ReferenceEquals(i, _editingItem)) CancelEdit();
            _items.Remove(i); UpdateTongTien();
        }
    }

    private void EditItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is not ChiTietPhieuNhapDTO item) { Msg.Warn("Hãy chọn dòng cần sửa."); return; }
        _editingItem = item;
        _cboSP.SelectedValue = item.MaSP;
        _txtSoLuong.Text = item.SoLuong.ToString();
        _txtDonGia.Text = item.DonGia.ToString("N0", Theme.Vi);
        _tempSerials = item.Serials.ToList();
        UpdateActionState();
    }

    private void CancelEdit()
    {
        _editingItem = null;
        _tempSerials.Clear();
        ResetEntryFields();
        UpdateActionState();
    }

    private void ResetEntryFields()
    {
        _suppressDraftTracking = true;
        try
        {
            _txtSoLuong.Text = "1";
            _txtDonGia.Clear();
        }
        finally
        {
            _suppressDraftTracking = false;
            _entryDirty = false;
        }
    }

    private void UpdateTongTien()
    {
        _lblTongTien.Text = $"Tổng tiền: {PhieuNhapBLL.TinhTongTien(_items):N0} ₫";
        UpdateActionState();
    }

    private void UpdateActionState()
    {
        var hasSelectedLine = _grid.CurrentRow?.DataBoundItem is ChiTietPhieuNhapDTO;
        _btnEdit.Enabled = hasSelectedLine && _editingItem == null;
        _btnRemove.Enabled = hasSelectedLine && _editingItem == null;
        _btnCancelEdit.Enabled = _editingItem != null;
        _btnSave.Enabled = _items.Count > 0 && _editingItem == null;
    }

    private void Save()
    {
        if (_editingItem != null)
        {
            Msg.Warn("Dòng nhập đang được sửa nhưng chưa xác nhận. Hãy bấm “Thêm vào phiếu” để lưu thay đổi vào danh sách, hoặc “Hủy sửa”.");
            return;
        }
        if (Msg.Run(() => _bll.LapPhieuNhap(new PhieuNhapDTO
        {
            MaNCC = _cboNCC.SelectedValue is int n ? n : 0, GhiChu = _txtGhiChu.Text
        }, _items.ToList())) > 0)
        {
            Msg.Info("Đã lập phiếu nhập thành công.");
            _items.Clear(); _txtGhiChu.Clear(); _tempSerials.Clear(); UpdateTongTien();
            ResetEntryFields();
        }
    }
}
