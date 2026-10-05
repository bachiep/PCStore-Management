using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class NhaCungCapDAL
{
    public List<NhaCungCapDTO> Search(string keyword) => Map(
        ExecuteQuery("SELECT * FROM NhaCungCap WHERE TenNCC LIKE @k OR SDT LIKE @k ORDER BY MaNCC", P("@k", $"%{keyword}%")),
        r => new NhaCungCapDTO
        {
            MaNCC = r.Int("MaNCC"), TenNCC = r.Str("TenNCC"), SDT = r.StrN("SDT"),
            Email = r.StrN("Email"), DiaChi = r.StrN("DiaChi")
        });

    public void Insert(NhaCungCapDTO n) => ExecuteNonQuery(
        "INSERT INTO NhaCungCap(TenNCC, SDT, Email, DiaChi) VALUES(@t, @s, @e, @d)",
        P("@t", n.TenNCC), P("@s", n.SDT), P("@e", n.Email), P("@d", n.DiaChi));

    public void Update(NhaCungCapDTO n) => ExecuteNonQuery(
        "UPDATE NhaCungCap SET TenNCC=@t, SDT=@s, Email=@e, DiaChi=@d WHERE MaNCC=@id",
        P("@t", n.TenNCC), P("@s", n.SDT), P("@e", n.Email), P("@d", n.DiaChi), P("@id", n.MaNCC));

    public bool HasReceipts(int id)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM PhieuNhap WHERE MaNCC=@id", P("@id", id))) > 0;

    public void Delete(int id) => ExecuteNonQuery("DELETE FROM NhaCungCap WHERE MaNCC=@id", P("@id", id));
}

public class KhachHangDAL
{
    public List<KhachHangDTO> Search(string keyword) => Map(
        ExecuteQuery(@"SELECT kh.*, COUNT(hd.MaHD) AS SoHoaDon, IFNULL(SUM(hd.ThanhToan), 0) AS TongChiTieu
                       FROM KhachHang kh LEFT JOIN HoaDon hd ON hd.MaKH = kh.MaKH AND hd.DaHuy = 0
                       WHERE kh.HoTen LIKE @k OR kh.SDT LIKE @k
                       GROUP BY kh.MaKH, kh.HoTen, kh.SDT, kh.DiaChi, kh.Email
                       ORDER BY kh.MaKH", P("@k", $"%{keyword}%")),
        r => new KhachHangDTO
        {
            MaKH = r.Int("MaKH"), HoTen = r.Str("HoTen"), SDT = r.Str("SDT"), DiaChi = r.StrN("DiaChi"),
            Email = r.StrN("Email"), SoHoaDon = r.Int("SoHoaDon"), TongChiTieu = r.Dec("TongChiTieu")
        });

    public KhachHangDTO? GetByPhone(string sdt)
    {
        var dt = ExecuteQuery("SELECT * FROM KhachHang WHERE SDT=@s", P("@s", sdt));
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new KhachHangDTO { MaKH = r.Int("MaKH"), HoTen = r.Str("HoTen"), SDT = r.Str("SDT"), DiaChi = r.StrN("DiaChi"), Email = r.StrN("Email") };
    }

    public bool PhoneExists(string sdt, int excludeId)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM KhachHang WHERE SDT=@s AND MaKH<>@id",
            P("@s", sdt), P("@id", excludeId))) > 0;

    public int Insert(KhachHangDTO k) => ExecuteInsert(
        "INSERT INTO KhachHang(HoTen, SDT, DiaChi, Email) VALUES(@t, @s, @d, @e)",
        P("@t", k.HoTen), P("@s", k.SDT), P("@d", k.DiaChi), P("@e", k.Email));

    public void Update(KhachHangDTO k) => ExecuteNonQuery(
        "UPDATE KhachHang SET HoTen=@t, SDT=@s, DiaChi=@d, Email=@e WHERE MaKH=@id",
        P("@t", k.HoTen), P("@s", k.SDT), P("@d", k.DiaChi), P("@e", k.Email), P("@id", k.MaKH));

    public bool HasInvoices(int id)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM HoaDon WHERE MaKH=@id", P("@id", id))) > 0;

    public void Delete(int id) => ExecuteNonQuery("DELETE FROM KhachHang WHERE MaKH=@id", P("@id", id));
}
