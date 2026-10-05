namespace ComputerStore.DTO;

public class DoanhThuDTO
{
    /// <summary>Nhãn: ngày (dd/MM) hoặc tháng (T1..T12).</summary>
    public string Nhan { get; set; } = "";
    public DateTime? Ngay { get; set; }
    public int SoHoaDon { get; set; }
    public decimal DoanhThu { get; set; }
    public decimal LoiNhuan { get; set; }
}

public class TopSanPhamDTO
{
    public int MaSP { get; set; }
    public string TenSP { get; set; } = "";
    public int SoLuongBan { get; set; }
    public decimal DoanhThu { get; set; }
}

public class TongQuanDTO
{
    public decimal DoanhThuHomNay { get; set; }
    public int SoHoaDonHomNay { get; set; }
    public decimal DoanhThuThangNay { get; set; }
    public int SoSanPhamSapHet { get; set; }
    public int SoKhachHang { get; set; }
    public int SoPhieuBaoHanhDangXuLy { get; set; }
}
