using System.Data;
using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class SanPhamDAL
{
    private const string BaseSelect = @"SELECT sp.*, dm.TenDM, h.TenHang FROM SanPham sp
                                        JOIN DanhMuc dm ON dm.MaDM = sp.MaDM
                                        JOIN Hang h ON h.MaHang = sp.MaHang";

    private static SanPhamDTO ToDto(DataRow r) => new()
    {
        MaSP = r.Int("MaSP"),
        TenSP = r.Str("TenSP"),
        MaDM = r.Int("MaDM"),
        TenDM = r.Str("TenDM"),
        MaHang = r.Int("MaHang"),
        TenHang = r.Str("TenHang"),
        GiaNhap = r.Dec("GiaNhap"),
        GiaBan = r.Dec("GiaBan"),
        SoLuongTon = r.Int("SoLuongTon"),
        ThoiGianBH = r.Int("ThoiGianBH"),
        MoTa = r.StrN("MoTa"),
        HinhAnh = r.StrN("HinhAnh"),
        TrangThai = r.Bool("TrangThai")
    };

    /// <param name="maDM">0 = tất cả</param>
    /// <param name="maHang">0 = tất cả</param>
    /// <param name="chiDangBan">true = chỉ lấy sản phẩm đang kinh doanh</param>
    public List<SanPhamDTO> Search(string keyword, int maDM = 0, int maHang = 0, bool chiDangBan = false)
    {
        var sql = BaseSelect + @" WHERE (sp.TenSP LIKE @k OR CAST(sp.MaSP AS CHAR) = @raw)
                                  AND (@dm = 0 OR sp.MaDM = @dm)
                                  AND (@h = 0 OR sp.MaHang = @h)
                                  AND (@db = 0 OR sp.TrangThai = 1)
                                  ORDER BY sp.MaSP";
        return Map(ExecuteQuery(sql, P("@k", $"%{keyword}%"), P("@raw", keyword), P("@dm", maDM), P("@h", maHang), P("@db", chiDangBan)), ToDto);
    }

    public SanPhamDTO? GetById(int id)
    {
        var dt = ExecuteQuery(BaseSelect + " WHERE sp.MaSP=@id", P("@id", id));
        return dt.Rows.Count == 0 ? null : ToDto(dt.Rows[0]);
    }

    public int Insert(SanPhamDTO s)
        => ExecuteInsert(@"INSERT INTO SanPham(TenSP, MaDM, MaHang, GiaNhap, GiaBan, ThoiGianBH, MoTa, HinhAnh, TrangThai)
                                           VALUES(@t, @dm, @h, @gn, @gb, @bh, @mt, @ha, @tt)", Params(s));

    public void Update(SanPhamDTO s)
        => ExecuteNonQuery(@"UPDATE SanPham SET TenSP=@t, MaDM=@dm, MaHang=@h, GiaNhap=@gn, GiaBan=@gb,
                             ThoiGianBH=@bh, MoTa=@mt, HinhAnh=@ha, TrangThai=@tt WHERE MaSP=@id",
            Params(s).Append(P("@id", s.MaSP)).ToArray());

    public bool HasTransactions(int id)
        => Convert.ToInt32(ExecuteScalar(@"SELECT (SELECT COUNT(*) FROM ChiTietHoaDon WHERE MaSP=@id)
                                                + (SELECT COUNT(*) FROM ChiTietPhieuNhap WHERE MaSP=@id)", P("@id", id))) > 0;

    public void Delete(int id) => ExecuteNonQuery("DELETE FROM SanPham WHERE MaSP=@id", P("@id", id));

    public void SetStatus(int id, bool active)
        => ExecuteNonQuery("UPDATE SanPham SET TrangThai=@tt WHERE MaSP=@id", P("@tt", active), P("@id", id));

    private static MySqlConnector.MySqlParameter[] Params(SanPhamDTO s) => new[]
    {
        P("@t", s.TenSP), P("@dm", s.MaDM), P("@h", s.MaHang), P("@gn", s.GiaNhap), P("@gb", s.GiaBan),
        P("@bh", s.ThoiGianBH), P("@mt", s.MoTa), P("@ha", s.HinhAnh), P("@tt", s.TrangThai)
    };
}
