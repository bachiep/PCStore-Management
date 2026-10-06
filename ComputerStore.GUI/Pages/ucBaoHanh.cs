using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Pages;

public class ucBaoHanh : PageBase
{
    private readonly BaoHanhBLL _bll = new();
    
    private readonly DataGridView _grid = Theme.Grid();
    private readonly TextBox _search = Theme.Input(200, "Tìm serial, tên khách...");
    private readonly ComboBox _cboTrangThaiLoc = Theme.Combo();

    private readonly TextBox _txtSerial = Theme.Input(160, "Nhập Serial...");
    private readonly Label _lblInfo = new() { Font = Theme.Base, AutoSize = true, MaximumSize = new Size(320, 0) };
    private readonly TextBox _txtMoTa = Theme.Input(320);
    private readonly Components.ModernButton _btnLapPhieu;
    private readonly TextBox _txtKetQua = Theme.Input(320);
    private readonly ComboBox _cboTrangThaiCapNhat = Theme.Combo();
    private readonly Components.ModernButton _btnCapNhat;
    
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

        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            Theme.Col("MaPBH", "Mã", weight: 0.5f),
            Theme.Col("Serial", "Serial", weight: 1.2f),
            Theme.Col("TenSP", "Sản phẩm", weight: 1.5f),
            Theme.Col("TenKH", "Khách hàng", weight: 1.2f),
            Theme.Col("NgayNhan", "Ngày nhận", weight: 1f, format: "dd/MM/yyyy"),
            Theme.Col("TrangThai", "Trạng thái", weight: 1f)
        );
        _grid.SelectionChanged += (_, _) => ShowSelected();

        Debounce(_search, LoadPhieu);
        _cboTrangThaiLoc.SelectedIndexChanged += (_, _) => LoadPhieu();

        var btnTraCuu = Theme.GhostButton("Tra cứu", 80, (_, _) => TraCuu());
        _txtMoTa.Multiline = true; _txtMoTa.Height = 60;
        _txtKetQua.Multiline = true; _txtKetQua.Height = 60;

        var lapPhieuCard = EditorCard("Lập phiếu mới",
            Theme.Row(_txtSerial, btnTraCuu),
            _lblInfo,
            Theme.Field("Mô tả lỗi *", _txtMoTa, 320),
            Theme.Row(_btnLapPhieu)
        );
        
        var capNhatCard = EditorCard("Cập nhật phiếu đang chọn",
            Theme.Field("Kết quả xử lý", _txtKetQua, 320),
            Theme.Field("Trạng thái mới", _cboTrangThaiCapNhat, 200),
            Theme.Row(_btnCapNhat)
        );

        var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        
        lapPhieuCard.Dock = DockStyle.Fill; lapPhieuCard.Margin = new Padding(14, 0, 0, 12);
        capNhatCard.Dock = DockStyle.Fill; capNhatCard.Margin = new Padding(14, 0, 0, 0);
        
        rightPanel.Controls.Add(lapPhieuCard, 0, 0);
        rightPanel.Controls.Add(capNhatCard, 0, 1);

        var infoBH = Theme.InfoBadge(
            "• Tra cứu bảo hành linh kiện theo số Serial / IMEI đã xuất hóa đơn.\n" +
            "• Thời hạn bảo hành tự động được tính toán dựa vào Ngày xuất và Số tháng BH của sản phẩm.\n" +
            "• Vòng đời tiếp nhận phiếu: Tiếp nhận -> Đang sửa chữa -> Đã xử lý -> Đã trả khách.",
            "Quy trình Bảo hành");

        AddRow(Toolbar(_search, _cboTrangThaiLoc, Theme.GhostButton("Làm mới", 80, (_, _) => LoadPhieu()), infoBH));
        AddRow(TwoCols(GridCard(_grid), rightPanel, 370), fill: true);
        
        ClearLapPhieu();
        ClearCapNhat();
    }

    public override void LoadData() => LoadPhieu();

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
            }
            else
            {
                _lblInfo.Text = kq.ThongBao;
                _lblInfo.ForeColor = Theme.Danger;
                _btnLapPhieu.Enabled = false;
            }
        });
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
        _selectedPhieu = _grid.CurrentRow?.DataBoundItem as PhieuBaoHanhDTO;
        if (_selectedPhieu == null) { ClearCapNhat(); return; }
        
        _txtKetQua.Text = _selectedPhieu.KetQua ?? "";
        _cboTrangThaiCapNhat.SelectedItem = _selectedPhieu.TrangThai;
        
        var isHandling = _selectedPhieu.TrangThai == TrangThaiBaoHanh.DangXuLy;
        _txtKetQua.ReadOnly = !isHandling;
        _cboTrangThaiCapNhat.Enabled = isHandling;
        _btnCapNhat.Enabled = isHandling;
    }

    private void ClearCapNhat()
    {
        _selectedPhieu = null; _txtKetQua.Clear(); _cboTrangThaiCapNhat.SelectedIndex = -1;
        _txtKetQua.ReadOnly = true; _cboTrangThaiCapNhat.Enabled = false; _btnCapNhat.Enabled = false;
    }

    private void CapNhat()
    {
        if (_selectedPhieu == null) return;
        var tt = _cboTrangThaiCapNhat.SelectedItem?.ToString();
        if (tt == null) return;
        if (Msg.Run(() => _bll.CapNhatKetQua(_selectedPhieu, tt, _txtKetQua.Text)))
        {
            Msg.Info("Đã cập nhật kết quả bảo hành.");
            LoadPhieu();
        }
    }
}
