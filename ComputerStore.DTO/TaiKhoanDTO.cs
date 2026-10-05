namespace ComputerStore.DTO;

public class TaiKhoanDTO
{
    public string TenDangNhap { get; set; } = "";
    public string MatKhauHash { get; set; } = "";
    public int MaNV { get; set; }
    public string VaiTro { get; set; } = VaiTroConst.NhanVien;
    public bool TrangThai { get; set; } = true;
    public string? HoTen { get; set; }
}

public static class VaiTroConst
{
    public const string Admin = "Admin";
    public const string NhanVien = "NhanVien";
}
