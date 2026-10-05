namespace ComputerStore.DTO;

public class SanPhamDTO
{
    public int MaSP { get; set; }
    public string TenSP { get; set; } = "";
    public int MaDM { get; set; }
    public string? TenDM { get; set; }
    public int MaHang { get; set; }
    public string? TenHang { get; set; }
    public decimal GiaNhap { get; set; }
    public decimal GiaBan { get; set; }
    public int SoLuongTon { get; set; }
    /// <summary>Thời gian bảo hành (tháng).</summary>
    public int ThoiGianBH { get; set; } = 12;
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public bool TrangThai { get; set; } = true;

    public override string ToString() => TenSP;
}
