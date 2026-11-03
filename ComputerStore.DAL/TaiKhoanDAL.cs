using System.Data;
using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class TaiKhoanDAL
{
    public const string AdminInvariantLockName = "ComputerStore.AdminInvariant";

    public TaiKhoanDTO? GetByUsername(string username)
    {
        var dt = ExecuteQuery(@"SELECT tk.*, nv.HoTen, nv.TrangThai AS NhanVienHoatDong FROM TaiKhoan tk
                                JOIN NhanVien nv ON nv.MaNV = tk.MaNV
                                WHERE tk.TenDangNhap = @u", P("@u", username));
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new TaiKhoanDTO
        {
            TenDangNhap = r.Str("TenDangNhap"),
            MatKhauHash = r.Str("MatKhauHash"),
            MaNV = r.Int("MaNV"),
            VaiTro = r.Str("VaiTro"),
            TrangThai = r.Bool("TrangThai"),
            HoTen = r.Str("HoTen"),
            NhanVienHoatDong = r.Bool("NhanVienHoatDong")
        };
    }

    public bool Exists(string username)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM TaiKhoan WHERE TenDangNhap=@u", P("@u", username))) > 0;

    public bool ExistsForEmployee(int maNV)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM TaiKhoan WHERE MaNV=@id", P("@id", maNV))) > 0;

    public void Insert(TaiKhoanDTO tk)
        => ExecuteNonQuery(@"INSERT TaiKhoan(TenDangNhap, MatKhauHash, MaNV, VaiTro, TrangThai)
                             VALUES(@u, @h, @nv, @vt, @tt)",
            P("@u", tk.TenDangNhap), P("@h", tk.MatKhauHash), P("@nv", tk.MaNV), P("@vt", tk.VaiTro), P("@tt", tk.TrangThai));

    public void UpdatePassword(string username, string hash)
        => ExecuteNonQuery("UPDATE TaiKhoan SET MatKhauHash=@h WHERE TenDangNhap=@u", P("@h", hash), P("@u", username));

    public bool UpdateRoleStatus(string username, string role, bool active)
        => InTransactionWithNamedLock(AdminInvariantLockName, (conn, tran) =>
        {
            var dt = ExecuteQuery(conn, tran,
                @"SELECT tk.VaiTro, tk.TrangThai, nv.TrangThai AS NhanVienHoatDong
                  FROM TaiKhoan tk JOIN NhanVien nv ON nv.MaNV = tk.MaNV
                  WHERE tk.TenDangNhap=@u FOR UPDATE", P("@u", username));
            if (dt.Rows.Count == 0) return false;
            if (active && !dt.Rows[0].Bool("NhanVienHoatDong")) return false;

            var wasActiveAdmin = dt.Rows[0].Str("VaiTro") == VaiTroConst.Admin && dt.Rows[0].Bool("TrangThai");
            var removesAdmin = wasActiveAdmin && (role != VaiTroConst.Admin || !active);
            if (removesAdmin && Convert.ToInt32(ExecuteScalar(conn, tran,
                    "SELECT COUNT(*) FROM TaiKhoan WHERE VaiTro='Admin' AND TrangThai=1")) <= 1)
                return false;

            ExecuteNonQuery(conn, tran,
                "UPDATE TaiKhoan SET VaiTro=@vt, TrangThai=@tt WHERE TenDangNhap=@u",
                P("@vt", role), P("@tt", active), P("@u", username));
            return true;
        });

    public int CountActiveAdmins()
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM TaiKhoan WHERE VaiTro='Admin' AND TrangThai=1"));

    public bool IsActiveAdminOfEmployee(int maNV)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM TaiKhoan WHERE MaNV=@id AND VaiTro='Admin' AND TrangThai=1", P("@id", maNV))) > 0;

    public bool IsEmployeeWorking(int maNV)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM NhanVien WHERE MaNV=@id AND TrangThai=1", P("@id", maNV))) > 0;

    public bool DeactivateEmployee(NhanVienDTO nv)
        => InTransactionWithNamedLock(AdminInvariantLockName, (conn, tran) =>
        {
            var isActiveAdmin = Convert.ToInt32(ExecuteScalar(conn, tran,
                "SELECT COUNT(*) FROM TaiKhoan WHERE MaNV=@id AND VaiTro='Admin' AND TrangThai=1", P("@id", nv.MaNV))) > 0;
            if (isActiveAdmin && Convert.ToInt32(ExecuteScalar(conn, tran,
                    "SELECT COUNT(*) FROM TaiKhoan WHERE VaiTro='Admin' AND TrangThai=1")) <= 1)
                return false;

            ExecuteNonQuery(conn, tran, "UPDATE TaiKhoan SET TrangThai=0 WHERE MaNV=@id", P("@id", nv.MaNV));
            ExecuteNonQuery(conn, tran, @"UPDATE NhanVien SET HoTen=@ht, GioiTinh=@gt, NgaySinh=@ns, SDT=@sdt, Email=@em,
                DiaChi=@dc, ChucVu=@cv, TrangThai=0 WHERE MaNV=@id",
                P("@ht", nv.HoTen), P("@gt", nv.GioiTinh), P("@ns", nv.NgaySinh), P("@sdt", nv.SDT),
                P("@em", nv.Email), P("@dc", nv.DiaChi), P("@cv", nv.ChucVu), P("@id", nv.MaNV));
            return true;
        });
}
