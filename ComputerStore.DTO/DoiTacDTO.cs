namespace ComputerStore.DTO;

public class NhaCungCapDTO
{
    public int MaNCC { get; set; }
    public string TenNCC { get; set; } = "";
    public string? SDT { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public override string ToString() => TenNCC;
}

public class KhachHangDTO
{
    public int MaKH { get; set; }
    public string HoTen { get; set; } = "";
    public string SDT { get; set; } = "";
    public string? DiaChi { get; set; }
    public string? Email { get; set; }
    public int SoHoaDon { get; set; }
    public decimal TongChiTieu { get; set; }
    public override string ToString() => $"{HoTen} - {SDT}";
}
