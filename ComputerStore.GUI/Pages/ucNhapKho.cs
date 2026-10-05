using System.ComponentModel;
using ComputerStore.BLL;
using ComputerStore.DTO;
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

    public ucNhapKho() : base("Nhập kho")
    {
        Name = "ucNhapKho";

        _cboNCC.DisplayMember = "TenNCC"; _cboNCC.ValueMember = "MaNCC";
        _cboSP.DisplayMember = "TenSP"; _cboSP.ValueMember = "MaSP";

        _txtDonGia.TextChanged += (_, _) => { if (decimal.TryParse(_txtDonGia.Text.Replace(".", ""), out var v)) _txtDonGia.Text = v.ToString("N0"); _txtDonGia.SelectionStart = _txtDonGia.Text.Length; };
        _cboSP.SelectedIndexChanged += (_, _) => { if (_cboSP.SelectedItem is SanPhamDTO sp && sp.GiaNhap > 0) _txtDonGia.Text = sp.GiaNhap.ToString("N0"); };
        _txtSoLuong.Text = "1";
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

        var btnAdd = Theme.PrimaryButton("Thêm vào phiếu (nhập serial)", 140, (_, _) => AddItem());
        btnAdd.Tag = "fill";
        var btnRemove = Theme.DangerButton("Xóa dòng đang chọn", 90, (_, _) => RemoveItem());

        var btnNewNcc = Theme.LinkButton("+ Thêm NCC mới", (_, _) => QuickAddNcc()); btnNewNcc.Name = "btnThemNCCNhanh";
        var btnNewSp = Theme.LinkButton("+ Thêm SP mới", (_, _) => QuickAddSp()); btnNewSp.Name = "btnThemSPNhanh";
        var editor = EditorCard("Phiếu nhập mới",
            Theme.Caption("Nhà cung cấp", false), Theme.Row(_cboNCC, btnNewNcc),
            Theme.Field("Ghi chú phiếu", _txtGhiChu, 320),
            Theme.Caption("Thêm sản phẩm vào phiếu", false),
            Theme.Row(_cboSP, btnNewSp),
            Theme.Pair(Theme.Field("Số lượng *", _txtSoLuong, 120), Theme.Field("Đơn giá nhập (₫) *", _txtDonGia, 160)),
            btnAdd,
            Theme.Caption("Mỗi sản phẩm nhập vào phải có số serial riêng để theo dõi bảo hành.")
        );

        var btnSave = Theme.PrimaryButton("LƯU PHIẾU NHẬP", 200, (_, _) => Save());
        var pnlLeftBot = new FlowLayoutPanel { Height = 52, Dock = DockStyle.Bottom, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 8, 0, 0) };
        btnSave.Margin = new Padding(0, 0, 18, 0); btnRemove.Margin = new Padding(0, 0, 18, 0);
        _lblTongTien.Margin = new Padding(0, 6, 18, 0);
        pnlLeftBot.Controls.Add(btnSave);
        pnlLeftBot.Controls.Add(btnRemove);
        pnlLeftBot.Controls.Add(_lblTongTien);
        var btnLichSu = Theme.GhostButton("Lịch sử nhập kho", 140, (_, _) => { using var f = new frmLichSuNhap(); f.ShowDialog(this); });
        btnLichSu.Margin = new Padding(0, 0, 0, 0);
        pnlLeftBot.Controls.Add(btnLichSu);

        var pnlLeft = new Panel { Dock = DockStyle.Fill };
        pnlLeft.Controls.Add(GridCard(_grid));
        pnlLeft.Controls.Add(pnlLeftBot);

        AddRow(TwoCols(pnlLeft, editor, 360), fill: true);
        UpdateTongTien();
    }

    public override void LoadData()
    {
        Msg.Run(() =>
        {
            _cboNCC.DataSource = _nccBll.Search("");
            _cboSP.DataSource = _spBll.Search("");
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

        Msg.Run(() =>
        {
            var gn = ParseMoney(giaNhap == "" ? "0" : giaNhap, "Giá nhập");
            var gb = ParseMoney(giaBan == "" ? "0" : giaBan, "Giá bán");
            var idDM = new DanhMucBLL().GetAll().FirstOrDefault()?.MaDM ?? 0;
            var idHang = new HangBLL().GetAll().FirstOrDefault()?.MaHang ?? 0;
            if (idDM == 0 || idHang == 0) throw new BusinessException("Chưa có danh mục hoặc hãng nào. Hãy thêm ở màn hình Danh mục & Hãng trước.");

            var sp = new SanPhamDTO { TenSP = ten, GiaNhap = gn, GiaBan = gb, ThoiGianBH = 12, MaDM = idDM, MaHang = idHang, TrangThai = true };
            _spBll.Save(sp);
            LoadData();
            for (var i = 0; i < _cboSP.Items.Count; i++)
                if (_cboSP.Items[i] is SanPhamDTO s && s.TenSP == ten.Trim()) { _cboSP.SelectedIndex = i; break; }
            Msg.Info("Đã thêm sản phẩm. Vui lòng vào màn hình Sản phẩm để cập nhật thêm ảnh, mô tả nếu cần.");
        });
    }



    private void AddItem()
    {
        if (_cboSP.SelectedItem is not SanPhamDTO sp) { Msg.Warn("Hãy chọn sản phẩm."); return; }
        if (!int.TryParse(_txtSoLuong.Text, out var sl) || sl <= 0) { Msg.Warn("Số lượng hợp lệ > 0."); return; }
        if (_items.Any(i => i.MaSP == sp.MaSP)) { Msg.Warn("Sản phẩm này đã có trong phiếu. Hãy xóa dòng cũ rồi thêm lại nếu muốn đổi."); return; }
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
            _items.Add(new ChiTietPhieuNhapDTO { MaSP = sp.MaSP, TenSP = sp.TenSP, SoLuong = sl, DonGia = dg, Serials = _tempSerials.ToList() });
            _tempSerials.Clear();
            _txtSoLuong.Text = "1"; _txtDonGia.Clear();
            UpdateTongTien();
        }
        catch (Exception ex) { Msg.Warn(ex.Message); }
    }

    private void RemoveItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is ChiTietPhieuNhapDTO i) { _items.Remove(i); UpdateTongTien(); }
    }

    private void UpdateTongTien()
    {
        _lblTongTien.Text = $"Tổng: {PhieuNhapBLL.TinhTongTien(_items):N0} ₫";
    }

    private void Save()
    {
        if (Msg.Run(() => _bll.LapPhieuNhap(new PhieuNhapDTO
        {
            MaNCC = _cboNCC.SelectedValue is int n ? n : 0, GhiChu = _txtGhiChu.Text
        }, _items.ToList())) > 0)
        {
            Msg.Info("Đã lập phiếu nhập thành công.");
            _items.Clear(); _txtGhiChu.Clear(); _tempSerials.Clear(); UpdateTongTien();
        }
    }
}
