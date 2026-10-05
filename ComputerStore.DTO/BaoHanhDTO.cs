namespace ComputerStore.DTO;

public static class TrangThaiSerial
{
    public const string TrongKho = "TrongKho";
    public const string DaBan = "DaBan";
    public const string DangBaoHanh = "DangBaoHanh";
    public const string Loi = "Loi";

    public static string HienThi(string s) => s switch
    {
        TrongKho => "Trong kho",
        DaBan => "Đã bán",
        DangBaoHanh => "Đang bảo hành",
        Loi => "Lỗi",
        _ => s
    };
}

public class SerialDTO
{
    public string Serial { get; set; } = "";
    public int MaSP { get; set; }
    public string? TenSP { get; set; }
    public int? MaPN { get; set; }
    public int? MaHD { get; set; }
    public DateTime? NgayBan { get; set; }
    public DateTime? HanBH { get; set; }
    public string TrangThai { get; set; } = TrangThaiSerial.TrongKho;
    public int? MaKH { get; set; }
    public string? TenKH { get; set; }
    public string? SDTKH { get; set; }
}

public static class TrangThaiBaoHanh
{
    public const string DangXuLy = "Đang xử lý";
    public const string DaTraKhach = "Đã trả khách";
    public const string TuChoi = "Từ chối";
    public static readonly string[] TatCa = { DangXuLy, DaTraKhach, TuChoi };
}

public class PhieuBaoHanhDTO
{
    public int MaPBH { get; set; }
    public string Serial { get; set; } = "";
    public string? TenSP { get; set; }
    public int? MaKH { get; set; }
    public string? TenKH { get; set; }
    public int MaNV { get; set; }
    public string? TenNV { get; set; }
    public DateTime NgayNhan { get; set; } = DateTime.Now;
    public string MoTaLoi { get; set; } = "";
    public DateTime? NgayTra { get; set; }
    public string? KetQua { get; set; }
    public string TrangThai { get; set; } = TrangThaiBaoHanh.DangXuLy;
}
