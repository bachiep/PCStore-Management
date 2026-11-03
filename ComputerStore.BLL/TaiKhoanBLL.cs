using ComputerStore.DAL;
using ComputerStore.DTO;

namespace ComputerStore.BLL;

/// <summary>Phiên đăng nhập hiện tại.</summary>
public static class Session
{
    public static TaiKhoanDTO? CurrentUser { get; private set; }

    public static bool IsLoggedIn => CurrentUser != null;
    public static bool IsAdmin => CurrentUser?.VaiTro == VaiTroConst.Admin;
    public static int MaNV => CurrentUser?.MaNV ?? throw new BusinessException("Chưa đăng nhập.");

    internal static void Start(TaiKhoanDTO user) => CurrentUser = user;
    public static void End() => CurrentUser = null;

    public static void RequireLogin()
    {
        var user = CurrentUser ?? throw new BusinessException("Chưa đăng nhập.");
        var current = new TaiKhoanDAL().GetByUsername(user.TenDangNhap);
        if (current == null || !current.TrangThai || !current.NhanVienHoatDong)
        {
            End();
            throw new BusinessException("Tài khoản đã bị khóa hoặc nhân viên đã nghỉ việc. Vui lòng đăng nhập lại.");
        }

        CurrentUser = current;
    }

    public static void RequireAdmin()
    {
        var wasAdmin = IsAdmin;
        RequireLogin();
        if (IsAdmin) return;

        if (wasAdmin)
        {
            End();
            throw new BusinessException("Quyền quản trị đã bị thu hồi. Vui lòng đăng nhập lại.");
        }

        throw new BusinessException("Bạn không có quyền thực hiện chức năng này.");
    }
}

/// <summary>Ghi nhật ký thao tác theo người dùng đang đăng nhập.</summary>
public static class Audit
{
    private static readonly NhatKyDAL _dal = new();
    public static event Action? LogWriteFailed;

    /// <summary>Audit failure must not make an already committed business action look unsuccessful.</summary>
    public static bool Log(string hanhDong, string chiTiet = "")
    {
        try
        {
            _dal.Ghi(Session.CurrentUser?.TenDangNhap ?? "?", hanhDong, chiTiet);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Không ghi được nhật ký '{hanhDong}': {ex}");
            try { LogWriteFailed?.Invoke(); }
            catch (Exception notificationError)
            {
                System.Diagnostics.Debug.WriteLine($"Không thể thông báo lỗi nhật ký: {notificationError}");
            }
            return false;
        }
    }
}

public static class PasswordHasher
{
    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 11);

    public static bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}

public class TaiKhoanBLL
{
    private readonly TaiKhoanDAL _dal = new();
    public const int MinPasswordLength = 6;

    public TaiKhoanDTO DangNhap(string username, string password)
    {
        Validator.Require(username, "Tên đăng nhập");
        Validator.Require(password, "Mật khẩu");

        var tk = _dal.GetByUsername(username.Trim());
        if (tk == null || !PasswordHasher.Verify(password, tk.MatKhauHash))
            throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        if (!tk.TrangThai)
            throw new BusinessException("Tài khoản đã bị khóa. Vui lòng liên hệ quản lý.");
        if (!tk.NhanVienHoatDong)
            throw new BusinessException("Nhân viên đã nghỉ việc. Tài khoản không thể đăng nhập.");

        Session.Start(tk);
        Audit.Log("Đăng nhập", tk.VaiTro);
        return tk;
    }

    public void DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhan)
    {
        Session.RequireLogin();
        var user = Session.CurrentUser ?? throw new BusinessException("Chưa đăng nhập.");
        var tk = _dal.GetByUsername(user.TenDangNhap)!;
        if (!PasswordHasher.Verify(matKhauCu, tk.MatKhauHash))
            throw new BusinessException("Mật khẩu hiện tại không đúng.");
        ValidateNewPassword(matKhauMoi, xacNhan);
        _dal.UpdatePassword(user.TenDangNhap, PasswordHasher.Hash(matKhauMoi));
        Audit.Log("Đổi mật khẩu");
    }

    public static void ValidateNewPassword(string matKhauMoi, string xacNhan)
    {
        if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < MinPasswordLength)
            throw new BusinessException($"Mật khẩu mới phải có ít nhất {MinPasswordLength} ký tự.");
        if (matKhauMoi != xacNhan)
            throw new BusinessException("Xác nhận mật khẩu không khớp.");
    }

    public void TaoTaiKhoan(int maNV, string username, string password, string vaiTro)
    {
        Session.RequireAdmin();
        username = (username ?? "").Trim();
        if (vaiTro != VaiTroConst.Admin && vaiTro != VaiTroConst.NhanVien) throw new BusinessException("Vai trò không hợp lệ.");
        if (!_dal.IsEmployeeWorking(maNV)) throw new BusinessException("Không thể tạo tài khoản cho nhân viên đã nghỉ việc.");
        if (!Validator.IsUsername(username))
            throw new BusinessException("Tên đăng nhập 3-50 ký tự, chỉ gồm chữ không dấu, số, dấu _ hoặc dấu chấm.");
        if (password.Length < MinPasswordLength)
            throw new BusinessException($"Mật khẩu phải có ít nhất {MinPasswordLength} ký tự.");
        if (_dal.Exists(username)) throw new BusinessException("Tên đăng nhập đã tồn tại.");
        if (_dal.ExistsForEmployee(maNV)) throw new BusinessException("Nhân viên này đã có tài khoản.");

        try
        {
            _dal.Insert(new TaiKhoanDTO
            {
                TenDangNhap = username, MatKhauHash = PasswordHasher.Hash(password), MaNV = maNV, VaiTro = vaiTro, TrangThai = true
            });
        }
        catch (InvalidOperationException ex)
        {
            // The DAL rechecks employee/account state inside the transaction to close
            // the race with employee deactivation; surface that conflict as a business error.
            throw new BusinessException(ex.Message);
        }
    }

    public void CapNhatQuyen(string username, string vaiTro, bool hoatDong)
    {
        Session.RequireAdmin();
        username = (username ?? "").Trim();
        if (vaiTro != VaiTroConst.Admin && vaiTro != VaiTroConst.NhanVien) throw new BusinessException("Vai trò không hợp lệ.");
        var target = _dal.GetByUsername(username);
        if (target == null) throw new BusinessException("Không tìm thấy tài khoản.");
        if (target.MaNV == Session.MaNV && (vaiTro != VaiTroConst.Admin || !hoatDong))
            throw new BusinessException("Không thể tự hạ quyền hoặc khóa tài khoản đang đăng nhập.");
        if (hoatDong && !target.NhanVienHoatDong)
            throw new BusinessException("Không thể kích hoạt tài khoản của nhân viên đã nghỉ việc.");
        if (!_dal.UpdateRoleStatus(username, vaiTro, hoatDong))
        {
            if (hoatDong && !_dal.IsEmployeeWorking(target.MaNV))
                throw new BusinessException("Không thể kích hoạt tài khoản của nhân viên đã nghỉ việc.");
            throw new BusinessException("Hệ thống phải có ít nhất một quản trị viên đang hoạt động.");
        }
    }

    public void DatLaiMatKhau(string username, string matKhauMoi)
    {
        Session.RequireAdmin();
        if (_dal.GetByUsername(username) == null) throw new BusinessException("Không tìm thấy tài khoản.");
        if (matKhauMoi == null || matKhauMoi.Length < MinPasswordLength)
            throw new BusinessException($"Mật khẩu phải có ít nhất {MinPasswordLength} ký tự.");
        _dal.UpdatePassword(username, PasswordHasher.Hash(matKhauMoi));
    }
}
