using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Forms;

public class frmChiTietHoaDon : Form
{
    private readonly DataGridView _grid = Theme.Grid();
    
    public frmChiTietHoaDon(HoaDonDTO hd)
    {
        Text = $"Chi tiết hóa đơn #{hd.MaHD} - Ngày lập: {hd.NgayLap:dd/MM/yyyy HH:mm}";
        Size = new Size(800, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm", weight: 2f),
            Theme.Col("DonGia", "Đơn giá", weight: 1f, format: "N0", right: true),
            Theme.Col("SoLuong", "SL", weight: 0.5f, right: true),
            Theme.Col("ThanhTien", "Thành tiền", weight: 1f, format: "N0", right: true)
        );
        
        var pnlFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        pnlFill.Controls.Add(_grid);
        
        var lblInfo = new Label
        {
            Text = $"Khách hàng: {hd.TenKH ?? "Khách lẻ"} (SĐT: {hd.SDTKH ?? "N/A"})\nNhân viên: {hd.TenNV}\nTổng tiền: {hd.TongTien:N0} ₫\nGiảm giá: {hd.GiamGia:N0} ₫\nThanh toán: {hd.ThanhToan:N0} ₫ ({hd.HinhThucTT})\nGhi chú: {hd.GhiChu}"
                 + (hd.DaHuy ? $"\nHÓA ĐƠN ĐÃ HỦY {hd.NgayHuy:dd/MM/yyyy HH:mm} — Lý do: {hd.LyDoHuy}" : ""),
            Dock = DockStyle.Top,
            Height = hd.DaHuy ? 145 : 120,
            ForeColor = hd.DaHuy ? Theme.Danger : Theme.Text,
            Font = Theme.Base,
            Padding = new Padding(10)
        };
        
        Controls.Add(pnlFill);
        Controls.Add(lblInfo);

        Msg.Run(() => _grid.DataSource = new HoaDonBLL().GetDetails(hd.MaHD));
    }
}
