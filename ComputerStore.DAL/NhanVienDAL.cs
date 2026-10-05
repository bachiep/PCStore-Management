using System.Data;
using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class NhanVienDAL
{
    private static NhanVienDTO ToDto(DataRow r) => new()
    {
        MaNV = r.Int("MaNV"),
        HoTen = r.Str("HoTen"),
        GioiTinh = r.StrN("GioiTinh"),
        NgaySinh = r.DateN("NgaySinh"),
        SDT = r.StrN("SDT"),
        Email = r.StrN("Email"),
        DiaChi = r.StrN("DiaChi"),
        ChucVu = r.Str("ChucVu"),
        TrangThai = r.Bool("TrangThai"),
        TenDangNhap = r.StrN("TenDangNhap"),
        VaiTro = r.StrN("VaiTro"),
        TaiKhoanHoatDong = r["TkTrangThai"] == DBNull.Value ? null : r.Bool("TkTrangThai")
    };

    public List<NhanVienDTO> Search(string keyword)
    {
        var dt = ExecuteQuery(@"SELECT nv.*, tk.TenDangNhap, tk.VaiTro, tk.TrangThai AS TkTrangThai
                                FROM NhanVien nv LEFT JOIN TaiKhoan tk ON tk.MaNV = nv.MaNV
                                WHERE nv.HoTen LIKE @k OR nv.SDT LIKE @k OR tk.TenDangNhap LIKE @k
                                ORDER BY nv.MaNV", P("@k", $"%{keyword}%"));
        return Map(dt, ToDto);
    }

    public int Insert(NhanVienDTO nv)
        => ExecuteInsert(@"INSERT INTO NhanVien(HoTen, GioiTinh, NgaySinh, SDT, Email, DiaChi, ChucVu, TrangThai)
                                           VALUES(@ht, @gt, @ns, @sdt, @em, @dc, @cv, @tt)", Params(nv));

    public void Update(NhanVienDTO nv)
    {
        var ps = Params(nv).Append(P("@id", nv.MaNV)).ToArray();
        ExecuteNonQuery(@"UPDATE NhanVien SET HoTen=@ht, GioiTinh=@gt, NgaySinh=@ns, SDT=@sdt, Email=@em,
                          DiaChi=@dc, ChucVu=@cv, TrangThai=@tt WHERE MaNV=@id", ps);
    }

    public bool HasActivity(int id)
        => Convert.ToInt32(ExecuteScalar(@"SELECT (SELECT COUNT(*) FROM HoaDon WHERE MaNV=@id) + (SELECT COUNT(*) FROM PhieuNhap WHERE MaNV=@id)
                                          + (SELECT COUNT(*) FROM PhieuBaoHanh WHERE MaNV=@id)", P("@id", id))) > 0;

    public void Delete(int id)
    {
        ExecuteNonQuery("DELETE FROM TaiKhoan WHERE MaNV=@id", P("@id", id));
        ExecuteNonQuery("DELETE FROM NhanVien WHERE MaNV=@id", P("@id", id));
    }

    private static MySqlConnector.MySqlParameter[] Params(NhanVienDTO nv) => new[]
    {
        P("@ht", nv.HoTen), P("@gt", nv.GioiTinh), P("@ns", nv.NgaySinh), P("@sdt", nv.SDT),
        P("@em", nv.Email), P("@dc", nv.DiaChi), P("@cv", nv.ChucVu), P("@tt", nv.TrangThai)
    };
}
