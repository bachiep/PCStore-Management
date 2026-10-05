namespace ComputerStore.DTO;

public class NhanVienDTO
{
    public int MaNV { get; set; }
    public string HoTen { get; set; } = "";
    public string? GioiTinh { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string? SDT { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public string ChucVu { get; set; } = "Nhân viên bán hàng";
    public bool TrangThai { get; set; } = true;

    // Thông tin tài khoản (join)
    public string? TenDangNhap { get; set; }
    public string? VaiTro { get; set; }
    public bool? TaiKhoanHoatDong { get; set; }
}
