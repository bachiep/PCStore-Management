using System.Data;
using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class TaiKhoanDAL
{
    public TaiKhoanDTO? GetByUsername(string username)
    {
        var dt = ExecuteQuery(@"SELECT tk.*, nv.HoTen FROM TaiKhoan tk
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
            HoTen = r.Str("HoTen")
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

    public void UpdateRoleStatus(string username, string role, bool active)
        => ExecuteNonQuery("UPDATE TaiKhoan SET VaiTro=@vt, TrangThai=@tt WHERE TenDangNhap=@u",
            P("@vt", role), P("@tt", active), P("@u", username));

    public int CountActiveAdmins()
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM TaiKhoan WHERE VaiTro='Admin' AND TrangThai=1"));

    public bool IsActiveAdminOfEmployee(int maNV)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM TaiKhoan WHERE MaNV=@id AND VaiTro='Admin' AND TrangThai=1", P("@id", maNV))) > 0;

    public void LockByEmployee(int maNV)
        => ExecuteNonQuery("UPDATE TaiKhoan SET TrangThai=0 WHERE MaNV=@id", P("@id", maNV));

    public bool IsEmployeeWorking(int maNV)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM NhanVien WHERE MaNV=@id AND TrangThai=1", P("@id", maNV))) > 0;
}
