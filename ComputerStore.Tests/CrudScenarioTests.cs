using ComputerStore.BLL;
using ComputerStore.DAL;
using ComputerStore.DTO;

namespace ComputerStore.Tests;

/// <summary>
/// Kịch bản kiểm thử CRUD danh mục gốc (nhân viên, nhà cung cấp, khách hàng, sản phẩm) và các quy tắc nghiệp vụ vừa vá.
/// Cần MySQL; nếu CSDL không chạy thì tự bỏ qua. Mỗi test dọn dữ liệu đã tạo.
/// </summary>
[Collection("Database")]
public class CrudScenarioTests
{
    private static bool DbUp() => DbHelper.TestConnection(out _);
    private static void LoginAdmin() => new TaiKhoanBLL().DangNhap("admin", "123456");
    private static string Tag => DateTime.Now.ToString("HHmmssfff");

    // TC-01: Thêm nhà cung cấp, tìm thấy, sửa, xóa
    [Fact]
    public void Supplier_Add_Edit_Delete()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var bll = new NhaCungCapBLL();
        var name = "NCC Test " + Tag;
        bll.Save(new NhaCungCapDTO { TenNCC = name, SDT = "0912345678" });
        var created = bll.Search(name).Single();
        created.DiaChi = "Hà Nội";
        bll.Save(created);
        Assert.Equal("Hà Nội", bll.Search(name).Single().DiaChi);
        bll.Delete(created.MaNCC);
        Assert.Empty(bll.Search(name));
        DbHelper.ExecuteNonQuery("ALTER TABLE NhaCungCap AUTO_INCREMENT = 1");
    }

    // TC-02: Dữ liệu nhà cung cấp sai bị từ chối, nhân viên thường không được thêm
    [Fact]
    public void Supplier_InvalidInput_And_Permission()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var bll = new NhaCungCapBLL();
        Assert.Throws<BusinessException>(() => bll.Save(new NhaCungCapDTO { TenNCC = "  " }));
        Assert.Throws<BusinessException>(() => bll.Save(new NhaCungCapDTO { TenNCC = "X" + Tag, SDT = "abc" }));
        Assert.Throws<BusinessException>(() => bll.Save(new NhaCungCapDTO { TenNCC = "X" + Tag, Email = "khong-hop-le" }));
        new TaiKhoanBLL().DangNhap("nhanvien", "123456");
        Assert.Throws<BusinessException>(() => bll.Save(new NhaCungCapDTO { TenNCC = "Y" + Tag }));
    }

    // TC-03: Thêm nhân viên → tạo tài khoản → đăng nhập được → xóa được (chưa có giao dịch)
    [Fact]
    public void Employee_Add_CreateAccount_Login_Delete()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var nvBll = new NhanVienBLL();
        var tkBll = new TaiKhoanBLL();
        var user = "nv" + Tag;
        var id = nvBll.Save(new NhanVienDTO { HoTen = "Nhân viên thử " + Tag, ChucVu = "Nhân viên bán hàng", TrangThai = true });
        Assert.True(id > 0);
        tkBll.TaoTaiKhoan(id, user, "abc123", VaiTroConst.NhanVien);
        Assert.Throws<BusinessException>(() => tkBll.TaoTaiKhoan(id, user + "x", "abc123", VaiTroConst.NhanVien)); // đã có tài khoản
        tkBll.DangNhap(user, "abc123");
        Assert.False(Session.IsAdmin);
        LoginAdmin();
        nvBll.Delete(id);
        Assert.DoesNotContain(nvBll.Search(user), n => n.MaNV == id);
        Assert.Throws<BusinessException>(() => tkBll.DangNhap(user, "abc123"));
        DbHelper.ExecuteNonQuery("ALTER TABLE NhanVien AUTO_INCREMENT = 1");
    }

    // TC-04: Cho nhân viên nghỉ việc → tài khoản bị khóa, không đăng nhập được; không tạo tài khoản mới cho người đã nghỉ
    [Fact]
    public void Employee_Leave_LocksAccount()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var nvBll = new NhanVienBLL();
        var tkBll = new TaiKhoanBLL();
        var user = "nghi" + Tag;
        var id = nvBll.Save(new NhanVienDTO { HoTen = "Sắp nghỉ " + Tag, ChucVu = "Nhân viên bán hàng", TrangThai = true });
        tkBll.TaoTaiKhoan(id, user, "abc123", VaiTroConst.NhanVien);
        nvBll.Save(new NhanVienDTO { MaNV = id, HoTen = "Sắp nghỉ " + Tag, ChucVu = "Nhân viên bán hàng", TrangThai = false });
        var ex = Assert.Throws<BusinessException>(() => tkBll.DangNhap(user, "abc123"));
        Assert.Contains("khóa", ex.Message);
        LoginAdmin();
        var id2 = nvBll.Save(new NhanVienDTO { HoTen = "Đã nghỉ " + Tag, ChucVu = "Tạm", TrangThai = false });
        Assert.Throws<BusinessException>(() => tkBll.TaoTaiKhoan(id2, "x" + Tag, "abc123", VaiTroConst.NhanVien));
        nvBll.Delete(id); nvBll.Delete(id2);
        DbHelper.ExecuteNonQuery("ALTER TABLE NhanVien AUTO_INCREMENT = 1");
    }

    // TC-05: Các chốt chặn nhân viên/tài khoản
    [Fact]
    public void Employee_Safeguards()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var nvBll = new NhanVienBLL();
        Assert.Throws<BusinessException>(() => nvBll.Delete(Session.MaNV));                   // không xóa chính mình
        Assert.Throws<BusinessException>(() => nvBll.Save(new NhanVienDTO { HoTen = "", ChucVu = "x" })); // thiếu tên
        Assert.Throws<BusinessException>(() => nvBll.Save(new NhanVienDTO { HoTen = "Trẻ con", ChucVu = "x", NgaySinh = DateTime.Today.AddYears(-10) }));
        var id = nvBll.Save(new NhanVienDTO { HoTen = "Role test " + Tag, ChucVu = "x", TrangThai = true });
        Assert.Throws<BusinessException>(() => new TaiKhoanBLL().TaoTaiKhoan(id, "r" + Tag, "abc123", "SieuNhanVat")); // vai trò lạ
        Assert.Throws<BusinessException>(() => new TaiKhoanBLL().TaoTaiKhoan(id, "r" + Tag, "123", VaiTroConst.NhanVien)); // mật khẩu ngắn
        Assert.Throws<BusinessException>(() => new TaiKhoanBLL().DatLaiMatKhau("khong_ton_tai_" + Tag, "abc123"));
        nvBll.Delete(id);
        DbHelper.ExecuteNonQuery("ALTER TABLE NhanVien AUTO_INCREMENT = 1");
    }

    // TC-06: Nhân viên đã có giao dịch không được xóa
    [Fact]
    public void Employee_WithTransactions_CannotBeDeleted()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var seeded = new NhanVienBLL().Search("").First(n => n.MaNV == Session.MaNV);
        Assert.Throws<BusinessException>(() => new NhanVienBLL().Delete(seeded.MaNV));
    }

    // TC-07: Khách hàng: thêm, trùng SĐT bị chặn, sửa, xóa
    [Fact]
    public void Customer_Add_DuplicatePhone_Delete()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var bll = new KhachHangBLL();
        var phone = "09" + DateTime.Now.ToString("fffffff").PadLeft(8, '1')[..8];
        var id = bll.Save(new KhachHangDTO { HoTen = "Khách thử " + Tag, SDT = phone });
        Assert.True(id > 0);
        Assert.Throws<BusinessException>(() => bll.Save(new KhachHangDTO { HoTen = "Trùng", SDT = phone }));
        Assert.Throws<BusinessException>(() => bll.Save(new KhachHangDTO { HoTen = "Sai SĐT", SDT = "123" }));
        bll.Delete(id);
        Assert.Null(bll.GetByPhone(phone));
        DbHelper.ExecuteNonQuery("ALTER TABLE KhachHang AUTO_INCREMENT = 1");
    }

    // TC-08: Sản phẩm ngừng kinh doanh không bán được ở tầng nghiệp vụ
    [Fact]
    public void InactiveProduct_CannotBeSold()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var spBll = new SanPhamBLL();
        var id = spBll.Save(new SanPhamDTO
        {
            TenSP = "SP ngừng KD " + Tag, MaDM = new DanhMucBLL().GetAll()[0].MaDM, MaHang = new HangBLL().GetAll()[0].MaHang,
            GiaNhap = 1000, GiaBan = 2000, ThoiGianBH = 12, TrangThai = false
        });
        try
        {
            var hd = new HoaDonDTO { HinhThucTT = HinhThucThanhToan.TienMat };
            var items = new List<ChiTietHoaDonDTO>
            {
                new() { MaSP = id, TenSP = "SP ngừng KD", SoLuong = 1, DonGia = 2000, Serials = new List<string> { "FAKE-" + Tag } }
            };
            var ex = Assert.Throws<BusinessException>(() => new HoaDonBLL().LapHoaDon(hd, items));
            Assert.Contains("ngừng kinh doanh", ex.Message);
        }
        finally {
            spBll.Delete(id);
            DbHelper.ExecuteNonQuery("ALTER TABLE SanPham AUTO_INCREMENT = 1");
        }
    }

    // TC-09: Sản phẩm: tên trống / giá bán 0 / bảo hành quá lớn bị từ chối
    [Fact]
    public void Product_Validation()
    {
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(new SanPhamDTO { TenSP = "", MaDM = 1, MaHang = 1, GiaBan = 1 }));
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(new SanPhamDTO { TenSP = "A", MaDM = 1, MaHang = 1, GiaBan = 0 }));
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(new SanPhamDTO { TenSP = "A", MaDM = 1, MaHang = 1, GiaBan = 1, ThoiGianBH = 999 }));
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(new SanPhamDTO { TenSP = "A", MaDM = 0, MaHang = 1, GiaBan = 1 }));
    }
}
