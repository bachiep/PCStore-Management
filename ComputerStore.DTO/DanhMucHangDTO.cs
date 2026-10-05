namespace ComputerStore.DTO;

public class DanhMucDTO
{
    public int MaDM { get; set; }
    public string TenDM { get; set; } = "";
    public int SoSanPham { get; set; }
    public override string ToString() => TenDM;
}

public class HangDTO
{
    public int MaHang { get; set; }
    public string TenHang { get; set; } = "";
    public string? QuocGia { get; set; }
    public int SoSanPham { get; set; }
    public override string ToString() => TenHang;
}
