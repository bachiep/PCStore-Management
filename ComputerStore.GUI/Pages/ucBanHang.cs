using System.ComponentModel;
using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

public class ucBanHang : PageBase
{
    private readonly HoaDonBLL _bll = new();
    private readonly SanPhamBLL _spBll = new();
    private readonly KhachHangBLL _khBll = new();

    private readonly TextBox _txtSdtKH = Theme.Input(150, "SĐT khách (bỏ trống = khách lẻ)");
    private readonly Label _lblKhach = new() { AutoSize = true, Font = Theme.Small, ForeColor = Theme.Muted, UseMnemonic = false, Text = "Khách lẻ (không lưu thông tin)", Margin = new Padding(0, 0, 0, 8) };
    private KhachHangDTO? _khachHang;

    private readonly ComboBox _cboSP = Theme.Combo();
    private readonly PictureBox _picSP = new() { Width = 96, Height = 72, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Margin = new Padding(0, 0, 0, 8) };
    private readonly Label _lblGia = new() { AutoSize = true, Font = Theme.Small, ForeColor = Theme.Text, Margin = new Padding(10, 0, 0, 0), UseMnemonic = false };
    private readonly ComboBox _cboGia = Theme.Combo();
    private readonly TextBox _txtSoLuong = Theme.Input(100);

    private readonly DataGridView _grid = Theme.Grid();
    private readonly BindingList<ChiTietHoaDonDTO> _items = new();
    
    private readonly TextBox _txtGiamGia = Theme.Input(150);
    private readonly TextBox _txtGhiChu = Theme.Input(290);
    private readonly ComboBox _cboHTTT = Theme.Combo(290);
    private readonly Label _lblTongTien = new() { Font = Theme.H2, AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(0, 10, 0, 5) };
    private readonly Label _lblThanhToan = new() { Font = Theme.H1, AutoSize = true, ForeColor = Theme.Accent, Margin = new Padding(0, 0, 0, 10) };

    public ucBanHang() : base("Bán hàng (POS)")
    {
        Name = "ucBanHang";

        _cboSP.DisplayMember = "TenSP"; _cboSP.ValueMember = "MaSP"; _cboSP.FormattingEnabled = true;
        _cboSP.Format += (_, e) => { if (e.ListItem is SanPhamDTO p) e.Value = $"{p.TenSP} (tồn {p.SoLuongTon})"; };
        _cboSP.DropDownWidth = 520;
        _cboSP.SelectedIndexChanged += (_, _) =>
        {
            _picSP.Image?.Dispose();
            _picSP.Image = _cboSP.SelectedItem is SanPhamDTO p ? Services.ProductImage.Load(p.HinhAnh) : null;
            _lblGia.Text = _cboSP.SelectedItem is SanPhamDTO q ? $"Đơn giá: {q.GiaBan:N0} ₫\nBảo hành: {q.ThoiGianBH} tháng\nCòn lại: {q.SoLuongTon}" : "";
        };
        Theme.EmptyHint(_grid, "Giỏ hàng trống — chọn sản phẩm bên phải rồi bấm “Thêm vào giỏ”");
        _txtSoLuong.Text = "1";
        _cboHTTT.DataSource = HinhThucThanhToan.TatCa.ToList();
        _txtSdtKH.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; TimKH(); } };
        _txtSdtKH.TextChanged += (_, _) => { if (_khachHang != null && _khachHang.SDT != _txtSdtKH.Text.Trim()) { _khachHang = null; _lblKhach.Text = "Khách lẻ (không lưu thông tin)"; _lblKhach.ForeColor = Theme.Muted; } };
        _txtGiamGia.Text = "0";
        _txtGiamGia.TextChanged += (_, _) => { if (decimal.TryParse(_txtGiamGia.Text.Replace(".", ""), out var v)) _txtGiamGia.Text = v.ToString("N0"); _txtGiamGia.SelectionStart = _txtGiamGia.Text.Length; UpdateTien(); };

        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            Theme.Col("MaSP", "Mã", weight: 0.5f),
            Theme.Col("TenSP", "Tên sản phẩm", weight: 2f),
            Theme.Col("DonGia", "Đơn giá", weight: 1f, format: "N0", right: true),
            Theme.Col("SoLuong", "SL", weight: 0.5f, right: true),
            Theme.Col("ThanhTien", "Thành tiền", weight: 1.2f, format: "N0", right: true)
        );
        _grid.DataSource = _items;

        var btnTimKH = Theme.PrimaryButton("Tìm khách", 100, (_, _) => TimKH());
        btnTimKH.Margin = new Padding(8, 0, 0, 0); btnTimKH.Dock = DockStyle.Fill; btnTimKH.AutoSize = false; btnTimKH.MinimumSize = Size.Empty; btnTimKH.Padding = Padding.Empty; btnTimKH.Text = "Tìm KH";
        _txtSdtKH.Dock = DockStyle.Fill; _txtSdtKH.Margin = new Padding(0, 3, 0, 0);
        var rowKhach = new TableLayoutPanel { Height = 36, Margin = new Padding(0, 0, 12, 4), ColumnCount = 2, RowCount = 1, Tag = "fill", Width = 300 };
        rowKhach.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); rowKhach.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        rowKhach.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rowKhach.Controls.Add(_txtSdtKH, 0, 0); rowKhach.Controls.Add(btnTimKH, 1, 0);

        var btnAdd = Theme.PrimaryButton("Thêm vào giỏ (F4)", 140, (_, _) => AddItem()); btnAdd.Tag = "fill";
        var btnRemove = Theme.DangerButton("Xóa dòng đang chọn", 90, (_, _) => RemoveItem());
        var btnPay = Theme.PrimaryButton("THANH TOÁN (F9)", 290, (_, _) => ThanhToan()); btnPay.Tag = "fill"; btnPay.Height = 42; btnPay.MinimumSize = new Size(290, 42);

        var editor = EditorCard("Thông tin bán hàng",
            Theme.Caption("Khách hàng", true), rowKhach, _lblKhach,
            Theme.Field("Sản phẩm *", _cboSP, 290),
            Theme.Row(_picSP, _lblGia),
            Theme.Field("Số lượng", _txtSoLuong, 100),
            btnAdd,
            _lblTongTien,
            Theme.Field("Giảm giá (₫)", _txtGiamGia, 150),
            _lblThanhToan,
            Theme.Field("Hình thức thanh toán", _cboHTTT, 290),
            Theme.Field("Ghi chú", _txtGhiChu, 290),
            btnPay
        );

        var pnlLeftBot = new FlowLayoutPanel { Height = 50, Dock = DockStyle.Bottom };
        pnlLeftBot.Controls.Add(btnRemove); btnRemove.Margin = new Padding(0, 8, 0, 0);

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        var gridCard = GridCard(_grid);
        gridCard.Dock = DockStyle.Fill;
        pnlLeft.Controls.Add(gridCard);
        pnlLeft.Controls.Add(pnlLeftBot);

        AddRow(TwoCols(pnlLeft, editor, 360), fill: true);
        UpdateTien();
    }

    public override void LoadData()
    {
        Msg.Run(() => { _cboSP.DataSource = _spBll.Search(""); });
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F2: _txtSdtKH.Focus(); return true;
            case Keys.F3: _cboSP.Focus(); return true;
            case Keys.F4: AddItem(); return true;
            case Keys.F9: ThanhToan(); return true;
            case Keys.Delete when _grid.Focused: RemoveItem(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void TimKH()
    {
        var sdt = _txtSdtKH.Text.Trim();
        if (string.IsNullOrWhiteSpace(sdt)) { Msg.Info("Bỏ trống SĐT = bán cho khách lẻ. Nhập SĐT nếu muốn gắn hóa đơn vào khách hàng."); return; }
        
        // Prevent user from typing arbitrary string like "Lee Manh Tun" as a phone number
        if (!System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^[0-9\-\+\s]+$"))
        {
            Msg.Warn("Vui lòng nhập SỐ ĐIỆN THOẠI (không nhập chữ hay tên) vào ô tìm kiếm.");
            _txtSdtKH.SelectAll();
            _txtSdtKH.Focus();
            return;
        }

        Msg.Run(() =>
        {
            _khachHang = _khBll.GetByPhone(sdt);
            if (_khachHang != null)
                ShowKhach();
            else
            {
                if (Ask("Không tìm thấy khách hàng. Tạo mới?"))
                {
                    using var f = new frmKhachHangNhanh(sdt);
                    if (f.ShowDialog() == DialogResult.OK && f.Result != null)
                    {
                        _khachHang = f.Result;
                        _txtSdtKH.Text = _khachHang.SDT;
                        ShowKhach();
                    }
                }
            }
        });
    }

    private void ShowKhach()
    {
        _lblKhach.Text = "✔ Khách hàng: " + _khachHang!.HoTen;
        _lblKhach.ForeColor = Theme.Success;
    }

    private void AddItem()
    {
        if (_cboSP.SelectedItem is not SanPhamDTO sp) { Msg.Warn("Hãy chọn sản phẩm."); return; }
        if (!int.TryParse(_txtSoLuong.Text, out var sl) || sl <= 0) { Msg.Warn("Số lượng hợp lệ > 0."); return; }
        if (_items.Any(i => i.MaSP == sp.MaSP)) { Msg.Warn("Sản phẩm đã có trong giỏ, hãy xóa dòng cũ đi thêm lại."); return; }
        if (sl > sp.SoLuongTon) { Msg.Warn($"Sản phẩm này chỉ còn {sp.SoLuongTon} sản phẩm trong kho. Không đủ để bán {sl}."); return; }
        
        using var f = new frmChonSerial(sp, sl);
        if (f.ShowDialog() == DialogResult.OK)
        {
            _items.Add(new ChiTietHoaDonDTO { MaSP = sp.MaSP, TenSP = sp.TenSP, SoLuong = sl, DonGia = sp.GiaBan, ThoiGianBH = sp.ThoiGianBH, Serials = f.ResultSerials });
            UpdateTien();
        }
    }

    private void RemoveItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is ChiTietHoaDonDTO i) { _items.Remove(i); UpdateTien(); }
    }

    private void UpdateTien()
    {
        var tong = HoaDonBLL.TinhTongTien(_items);
        _lblTongTien.Text = $"Tổng tiền hàng: {tong:N0} ₫";
        decimal giamGia = 0;
        if (decimal.TryParse(_txtGiamGia.Text.Replace(".", ""), out var g)) giamGia = g;
        _lblThanhToan.Text = $"Thanh toán: {HoaDonBLL.TinhThanhToan(tong, giamGia):N0} ₫";
    }

    private void ThanhToan()
    {
        if (_items.Count == 0) { Msg.Warn("Giỏ hàng đang trống."); return; }
        decimal giamGia = 0;
        if (decimal.TryParse(_txtGiamGia.Text.Replace(".", ""), out var g)) giamGia = g;
        var tong = HoaDonBLL.TinhTongTien(_items);

        var hd = new HoaDonDTO
        {
            MaKH = _khachHang?.MaKH, TenKH = _khachHang?.HoTen, SDTKH = _khachHang?.SDT, 
            TongTien = tong, GiamGia = giamGia, GhiChu = _txtGhiChu.Text, HinhThucTT = _cboHTTT.SelectedItem as string ?? HinhThucThanhToan.TienMat
        };
        
        var idHD = Msg.Run(() => _bll.LapHoaDon(hd, _items.ToList()));
        if (idHD > 0)
        {
            Msg.Info("Đã thanh toán hóa đơn.");
            if (Ask("Bạn có muốn in hóa đơn không?"))
            {
                using var dlg = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"HoaDon_{idHD}.pdf" };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Msg.Run(() =>
                    {
                        hd.MaHD = idHD;
                        ComputerStore.GUI.Services.PdfExporter.ExportHoaDon(hd, _items.ToList(), dlg.FileName);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                    });
                }
            }

            _items.Clear();
            _txtGhiChu.Clear();
            _txtSdtKH.Clear();
            _khachHang = null; _lblKhach.Text = "Khách lẻ (không lưu thông tin)"; _lblKhach.ForeColor = Theme.Muted;
            LoadData();
            _txtGiamGia.Text = "0";
            UpdateTien();
        }
    }
}
