using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Pages;

public class ucBaoHanh : PageBase
{
    private sealed record TicketDraft(string KetQua, string TrangThai);

    private readonly BaoHanhBLL _bll = new();
    private readonly Dictionary<int, TicketDraft> _ticketDrafts = new();
    
    private readonly DataGridView _grid = Theme.Grid();
    private readonly DataGridView _gridSales = Theme.Grid();
    private readonly DataGridView _gridWarrantyHistory = Theme.Grid();
    private readonly TextBox _search = Theme.Input(200, "Tìm serial, tên khách...");
    private readonly ComboBox _cboTrangThaiLoc = Theme.Combo();

    private readonly TextBox _txtSerial = Theme.Input(160, "Nhập Serial...");
    private readonly Label _lblInfo = new() { Font = Theme.Base, AutoSize = true, MaximumSize = new Size(320, 0) };
    private readonly TextBox _txtMoTa = Theme.Input(320);
    private readonly Components.ModernButton _btnLapPhieu;
    private readonly TextBox _txtKetQua = Theme.Input(320);
    private readonly ComboBox _cboTrangThaiCapNhat = Theme.Combo();
    private readonly Components.ModernButton _btnCapNhat;
    private readonly TabControl _leftTabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage _ticketsTab = new("Phiếu bảo hành");
    private readonly TabPage _historyTab = new("Lịch sử serial");
    
    private PhieuBaoHanhDTO? _selectedPhieu;
    private SerialDTO? _currentSerial;

    public ucBaoHanh() : base("Bảo hành")
    {
        Name = "ucBaoHanh";
        _btnLapPhieu = Theme.PrimaryButton("Lập phiếu", 120, (_, _) => LapPhieu());
        _btnCapNhat = Theme.PrimaryButton("Cập nhật", 120, (_, _) => CapNhat());
        
        _cboTrangThaiLoc.Items.Add("Tất cả");
        _cboTrangThaiLoc.Items.AddRange(TrangThaiBaoHanh.TatCa);
        _cboTrangThaiLoc.SelectedIndex = 0;
        
        _cboTrangThaiCapNhat.Items.AddRange(TrangThaiBaoHanh.TatCa);
        _cboTrangThaiCapNhat.DropDownStyle = ComboBoxStyle.DropDownList;
        TrackChanges(_txtKetQua, _cboTrangThaiCapNhat);

        _grid.AutoGenerateColumns = false;
        var ticketProduct = Theme.Col("TenSP", "Sản phẩm", weight: 2f);
        ticketProduct.MinimumWidth = 140;
        ticketProduct.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var ticketCustomer = Theme.Col("TenKH", "Khách hàng", weight: 1.8f);
        ticketCustomer.MinimumWidth = 125;
        ticketCustomer.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var ticketStatus = Theme.Col("TrangThai", "Trạng thái", weight: 1.1f);
        ticketStatus.MinimumWidth = 100;
        ticketStatus.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var ticketSerial = Theme.Col("Serial", "Serial", weight: 1.5f);
        ticketSerial.MinimumWidth = 125;
        var ticketDate = Theme.Col("NgayNhan", "Ngày nhận", weight: 0.8f, format: "dd/MM/yy");
        ticketDate.MinimumWidth = 86;
        _grid.Columns.AddRange(
            Theme.Col("MaPBH", "Mã", weight: 0.4f),
            ticketSerial,
            ticketProduct,
            ticketCustomer,
            ticketDate,
            ticketStatus
        );
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
        _grid.SelectionChanged += (_, _) => ShowSelected();

        _gridSales.AutoGenerateColumns = false;
        var salesCustomer = Theme.Col("TenKH", "Khách hàng", weight: 1.4f);
        salesCustomer.MinimumWidth = 125;
        salesCustomer.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var salesStatus = Theme.Col("TrangThaiText", "Trạng thái", weight: 0.9f);
        salesStatus.MinimumWidth = 100;
        salesStatus.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var salesDate = Theme.Col("NgayBan", "Ngày bán", weight: 0.8f, format: "dd/MM/yyyy");
        salesDate.MinimumWidth = 98;
        var warrantyExpiry = Theme.Col("HanBH", "Hạn BH", weight: 0.8f, format: "dd/MM/yyyy");
        warrantyExpiry.MinimumWidth = 98;
        _gridSales.Columns.AddRange(
            Theme.Col("MaHD", "Mã HĐ", weight: 0.55f),
            salesDate,
            salesCustomer,
            Theme.Col("DonGia", "Đơn giá", weight: 0.8f, format: "N0", right: true),
            warrantyExpiry,
            salesStatus
        );
        _gridSales.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;

        _gridWarrantyHistory.AutoGenerateColumns = false;
        var historyDescription = Theme.Col("MoTaLoi", "Mô tả lỗi", weight: 1.5f);
        historyDescription.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var historyResult = Theme.Col("KetQua", "Kết quả", weight: 1.5f);
        historyResult.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var historyStatus = Theme.Col("TrangThai", "Trạng thái", weight: 1f);
        historyStatus.MinimumWidth = 100;
        historyStatus.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var historyReceived = Theme.Col("NgayNhan", "Ngày nhận", weight: 0.8f, format: "dd/MM/yyyy");
        historyReceived.MinimumWidth = 98;
        var historyReturned = Theme.Col("NgayTra", "Ngày trả", weight: 0.8f, format: "dd/MM/yyyy");
        historyReturned.MinimumWidth = 98;
        _gridWarrantyHistory.Columns.AddRange(
            Theme.Col("MaPBH", "Mã phiếu", weight: 0.6f),
            Theme.Col("MaHDLucTiepNhan", "Mã HĐ", weight: 0.6f),
            historyReceived,
            historyDescription,
            historyResult,
            historyStatus,
            historyReturned
        );
        _gridWarrantyHistory.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;

        Debounce(_search, LoadPhieu);
        _cboTrangThaiLoc.SelectedIndexChanged += (_, _) => LoadPhieu();

        var btnTraCuu = Theme.GhostButton("Tra cứu", 80, (_, _) => TraCuu());
        _txtSerial.TextChanged += (_, _) => ClearLookupResult();
        _txtSerial.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TraCuu();
            }
        };
        _txtMoTa.Multiline = true; _txtMoTa.Height = 44;
        _txtKetQua.Multiline = true; _txtKetQua.Height = 44;

        var lapPhieuCard = EditorCard("Lập phiếu mới",
            Theme.Row(Theme.SearchField(_txtSerial, "Nhập serial để tra cứu"), btnTraCuu),
            _lblInfo,
            Theme.Field("Mô tả lỗi *", _txtMoTa, 320),
            Theme.Row(_btnLapPhieu)
        );
        
        var capNhatCard = EditorCard("Cập nhật phiếu đang chọn",
            Theme.Field("Kết quả xử lý", _txtKetQua, 320),
            Theme.Field("Trạng thái mới", _cboTrangThaiCapNhat, 200),
            Theme.Row(_btnCapNhat)
        );

        CompactEditorCard(lapPhieuCard);
        CompactEditorCard(capNhatCard);

        var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        
        lapPhieuCard.Dock = DockStyle.Fill; lapPhieuCard.Margin = new Padding(14, 0, 0, 12);
        capNhatCard.Dock = DockStyle.Fill; capNhatCard.Margin = new Padding(14, 0, 0, 0);
        
        rightPanel.Controls.Add(lapPhieuCard, 0, 0);
        rightPanel.Controls.Add(capNhatCard, 0, 1);

        var infoBH = Theme.InfoBadge(
            "• Tra cứu bảo hành linh kiện theo số Serial / IMEI đã xuất hóa đơn.\n" +
            "• Thời hạn bảo hành tự động được tính toán dựa vào Ngày xuất và Số tháng BH của sản phẩm.\n" +
            "• Vòng đời tiếp nhận phiếu: Tiếp nhận -> Đang sửa chữa -> Đã xử lý -> Đã trả khách.",
            "Quy trình Bảo hành");

        AddRow(Toolbar(Theme.SearchField(_search, "Tìm serial hoặc tên khách"), _cboTrangThaiLoc, Theme.GhostButton("Làm mới", 80, (_, _) => LoadPhieu()), infoBH));
        _ticketsTab.Controls.Add(GridCard(_grid));
        var historyTabs = new TabControl { Dock = DockStyle.Fill };
        historyTabs.TabPages.Add(new TabPage("Lịch sử bán") { Controls = { GridCard(_gridSales) } });
        historyTabs.TabPages.Add(new TabPage("Lịch sử bảo hành") { Controls = { GridCard(_gridWarrantyHistory) } });
        _historyTab.Controls.Add(historyTabs);
        _leftTabs.TabPages.Add(_ticketsTab);
        _leftTabs.TabPages.Add(_historyTab);

        AddRow(TwoCols(_leftTabs, rightPanel, 340), fill: true);
        
        ClearLapPhieu();
        ClearCapNhat();
    }

    private static void CompactEditorCard(Panel card)
    {
        card.Padding = new Padding(16, 12, 16, 12);
        foreach (var flow in card.Controls.OfType<FlowLayoutPanel>())
        {
            if (flow.Dock == DockStyle.Bottom)
            {
                flow.Padding = new Padding(0, 4, 0, 0);
                continue;
            }

            foreach (Control control in flow.Controls)
            {
                var bottom = control is Label ? 8 : 4;
                control.Margin = new Padding(control.Margin.Left, control.Margin.Top, control.Margin.Right, Math.Min(control.Margin.Bottom, bottom));
            }
        }
    }

    public override void LoadData() => LoadPhieu();

    public override bool HasUnsavedChanges => _ticketDrafts.Count > 0 || HasTrackedChanges
        || !string.IsNullOrWhiteSpace(_txtSerial.Text) || !string.IsNullOrWhiteSpace(_txtMoTa.Text);
    public override bool ConfirmDiscardOnNavigation => true;

    public override void AcknowledgeDiscardedChanges()
    {
        _ticketDrafts.Clear();
        ClearLapPhieu();
        base.AcknowledgeDiscardedChanges();
    }

    private void LoadPhieu()
    {
        Msg.Run(() =>
        {
            var tt = _cboTrangThaiLoc.SelectedIndex == 0 ? "" : _cboTrangThaiLoc.SelectedItem?.ToString();
            _grid.DataSource = _bll.Search(_search.Text, tt ?? "");
        });
    }

    private void TraCuu()
    {
        if (string.IsNullOrWhiteSpace(_txtSerial.Text)) { Msg.Warn("Hãy nhập serial."); return; }
        Msg.Run(() =>
        {
            _currentSerial = _bll.TraCuu(_txtSerial.Text);
            var kq = BaoHanhBLL.KiemTra(_currentSerial, DateTime.Today);
            if (_currentSerial != null)
            {
                _lblInfo.Text = $"SP: {_currentSerial.TenSP}\nKhách: {_currentSerial.TenKH ?? "N/A"} ({_currentSerial.SDTKH ?? "N/A"})\nNgày mua: {_currentSerial.NgayBan:dd/MM/yyyy}\nKết quả: {kq.ThongBao}";
                _lblInfo.ForeColor = kq.HopLe ? Theme.Success : Theme.Danger;
                _btnLapPhieu.Enabled = kq.HopLe;
                _gridSales.DataSource = _bll.LichSuBan(_currentSerial.Serial);
                _gridWarrantyHistory.DataSource = _bll.LichSu(_currentSerial.Serial);
                _leftTabs.SelectedTab = _historyTab;
            }
            else
            {
                _lblInfo.Text = kq.ThongBao;
                _lblInfo.ForeColor = Theme.Danger;
                _btnLapPhieu.Enabled = false;
                ClearHistory();
            }
        });
    }

    private void ClearLookupResult()
    {
        _currentSerial = null;
        _lblInfo.Text = "";
        _btnLapPhieu.Enabled = false;
        ClearHistory();
    }

    private void ClearHistory()
    {
        _gridSales.DataSource = null;
        _gridWarrantyHistory.DataSource = null;
        _leftTabs.SelectedTab = _ticketsTab;
    }

    private void LapPhieu()
    {
        if (_currentSerial == null || !_btnLapPhieu.Enabled) return;
        if (Msg.Run(() => _bll.LapPhieu(_currentSerial.Serial, _txtMoTa.Text)) > 0)
        {
            Msg.Info("Đã lập phiếu bảo hành.");
            ClearLapPhieu();
            LoadPhieu();
        }
    }

    private void ClearLapPhieu()
    {
        _currentSerial = null; _txtSerial.Clear(); _lblInfo.Text = ""; _txtMoTa.Clear(); _btnLapPhieu.Enabled = false;
    }

    private void ShowSelected()
    {
        SaveCurrentDraft();
        _selectedPhieu = _grid.CurrentRow?.DataBoundItem as PhieuBaoHanhDTO;
        if (_selectedPhieu == null) { ClearCapNhat(); return; }

        var draft = _ticketDrafts.GetValueOrDefault(_selectedPhieu.MaPBH);
        var ketQua = draft?.KetQua ?? _selectedPhieu.KetQua ?? "";
        var trangThai = draft?.TrangThai ?? _selectedPhieu.TrangThai;
        var isHandling = _selectedPhieu.TrangThai == TrangThaiBaoHanh.DangXuLy;
        SetTrackedValues(() =>
        {
            _txtKetQua.Text = ketQua;
            _cboTrangThaiCapNhat.SelectedItem = trangThai;
            _txtKetQua.ReadOnly = !isHandling;
            _cboTrangThaiCapNhat.Enabled = isHandling;
            _btnCapNhat.Enabled = isHandling;
        });
    }

    private void SaveCurrentDraft()
    {
        if (_selectedPhieu == null || !HasTrackedChanges) return;

        var ketQua = _txtKetQua.Text;
        var trangThai = _cboTrangThaiCapNhat.SelectedItem?.ToString() ?? _selectedPhieu.TrangThai;
        if (ketQua == (_selectedPhieu.KetQua ?? "") && trangThai == _selectedPhieu.TrangThai)
            _ticketDrafts.Remove(_selectedPhieu.MaPBH);
        else
            _ticketDrafts[_selectedPhieu.MaPBH] = new TicketDraft(ketQua, trangThai);
    }

    private void ClearCapNhat()
    {
        SetTrackedValues(() =>
        {
            _selectedPhieu = null; _txtKetQua.Clear(); _cboTrangThaiCapNhat.SelectedIndex = -1;
            _txtKetQua.ReadOnly = true; _cboTrangThaiCapNhat.Enabled = false; _btnCapNhat.Enabled = false;
        });
    }

    private void CapNhat()
    {
        if (_selectedPhieu == null) return;
        var tt = _cboTrangThaiCapNhat.SelectedItem?.ToString();
        if (tt == null) return;
        var maPBH = _selectedPhieu.MaPBH;
        var update = new PhieuBaoHanhDTO
        {
            MaPBH = maPBH,
            Serial = _selectedPhieu.Serial,
            TrangThai = _selectedPhieu.TrangThai
        };
        if (Msg.Run(() => _bll.CapNhatKetQua(update, tt, _txtKetQua.Text)))
        {
            _ticketDrafts.Remove(maPBH);
            MarkTrackedChangesClean();
            Msg.Info("Đã cập nhật kết quả bảo hành.");
            LoadPhieu();
        }
    }
}
