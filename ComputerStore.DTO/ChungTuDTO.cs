namespace ComputerStore.DTO;

public class PhieuNhapDTO
{
    public int MaPN { get; set; }
    public DateTime NgayNhap { get; set; } = DateTime.Now;
    public int MaNCC { get; set; }
    public string? TenNCC { get; set; }
    public int MaNV { get; set; }
    public string? TenNV { get; set; }
    public decimal TongTien { get; set; }
    public string? GhiChu { get; set; }
}

public class ChiTietPhieuNhapDTO
{
    public int MaPN { get; set; }
    public int MaSP { get; set; }
    public string? TenSP { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
    /// <summary>Danh sách serial nhập cho dòng này (số phần tử = SoLuong).</summary>
    public List<string> Serials { get; set; } = new();
}

public class HoaDonDTO
{
    public int MaHD { get; set; }
    public DateTime NgayLap { get; set; } = DateTime.Now;
    public int? MaKH { get; set; }
    public string? TenKH { get; set; }
    public string? SDTKH { get; set; }
    public int MaNV { get; set; }
    public string? TenNV { get; set; }
    public decimal TongTien { get; set; }
    public decimal GiamGia { get; set; }
    public decimal ThanhToan { get; set; }
    public string? GhiChu { get; set; }
    public string HinhThucTT { get; set; } = HinhThucThanhToan.TienMat;
    public bool DaHuy { get; set; }
    public string? LyDoHuy { get; set; }
    public DateTime? NgayHuy { get; set; }
    public string TrangThaiText => DaHuy ? "Đã hủy" : "Hoàn thành";
}

public static class HinhThucThanhToan
{
    public const string TienMat = "Tiền mặt";
    public const string ChuyenKhoan = "Chuyển khoản";
    public const string The = "Thẻ";
    public static readonly string[] TatCa = { TienMat, ChuyenKhoan, The };
}

public class ChiTietHoaDonDTO
{
    public int MaHD { get; set; }
    public int MaSP { get; set; }
    public string? TenSP { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
    public int ThoiGianBH { get; set; }
    /// <summary>Serial được bán (số phần tử = SoLuong).</summary>
    public List<string> Serials { get; set; } = new();
}
