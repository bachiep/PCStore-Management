using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.Tests;

public class ValidatorTests
{
    [Theory]
    [InlineData("0912345678", true)]
    [InlineData("02438686868", true)]
    [InlineData("912345678", false)]
    [InlineData("091234", false)]
    [InlineData("09123abc78", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPhone(string? s, bool expected) => Assert.Equal(expected, Validator.IsPhone(s));

    [Theory]
    [InlineData("a@b.vn", true)]
    [InlineData("khoa@gmail.com", true)]
    [InlineData("khoa@gmail", false)]
    [InlineData("khoa gmail.com", false)]
    [InlineData("", false)]
    public void IsEmail(string s, bool expected) => Assert.Equal(expected, Validator.IsEmail(s));

    [Theory]
    [InlineData("admin", true)]
    [InlineData("nv.01_a", true)]
    [InlineData("ab", false)]
    [InlineData("co dau", false)]
    [InlineData("tiếng_việt", false)]
    public void IsUsername(string s, bool expected) => Assert.Equal(expected, Validator.IsUsername(s));

    [Theory]
    [InlineData("SN001-0001", true)]
    [InlineData("ABC/123.x", true)]
    [InlineData("a b", false)]
    [InlineData("ab", false)]
    [InlineData("SN'; DROP", false)]
    public void IsSerial(string s, bool expected) => Assert.Equal(expected, Validator.IsSerial(s));

    [Fact]
    public void Require_ThrowsOnBlank() => Assert.Throws<BusinessException>(() => Validator.Require("   ", "Tên"));

    [Fact]
    public void Require_MessageContainsFieldName()
    {
        var ex = Assert.Throws<BusinessException>(() => Validator.Require("", "Tên sản phẩm"));
        Assert.Contains("Tên sản phẩm", ex.Message);
    }

    [Fact]
    public void Clean_TrimsAndNullsBlank()
    {
        Assert.Null(Validator.Clean("  "));
        Assert.Equal("x", Validator.Clean(" x "));
    }

    [Fact]
    public void OptionalPhone_AllowsEmptyButRejectsInvalid()
    {
        Validator.OptionalPhone(null);
        Validator.OptionalPhone("");
        Assert.Throws<BusinessException>(() => Validator.OptionalPhone("123"));
    }
}

[Collection("Database")]
public class PasswordTests
{
    [Fact]
    public void Hash_VerifiesCorrectPassword()
    {
        var h = PasswordHasher.Hash("123456");
        Assert.True(PasswordHasher.Verify("123456", h));
        Assert.False(PasswordHasher.Verify("654321", h));
    }

    [Fact]
    public void Hash_IsSaltedSoDifferentEachTime()
        => Assert.NotEqual(PasswordHasher.Hash("123456"), PasswordHasher.Hash("123456"));

    [Fact]
    public void Verify_InvalidHashReturnsFalse() => Assert.False(PasswordHasher.Verify("x", "not-a-hash"));

    [Fact]
    public void ValidateNewPassword_RejectsShortAndMismatch()
    {
        Assert.Throws<BusinessException>(() => TaiKhoanBLL.ValidateNewPassword("123", "123"));
        Assert.Throws<BusinessException>(() => TaiKhoanBLL.ValidateNewPassword("123456", "654321"));
        TaiKhoanBLL.ValidateNewPassword("123456", "123456");
    }

    [Fact]
    public void RequireAdmin_ThrowsWhenNotLoggedIn()
    {
        Session.End();
        Assert.Throws<BusinessException>(Session.RequireAdmin);
    }
}

public class SanPhamTests
{
    private static SanPhamDTO Valid() => new() { TenSP = "RAM 16GB", MaDM = 1, MaHang = 1, GiaNhap = 800, GiaBan = 1000, ThoiGianBH = 36 };

    [Fact] public void Validate_AcceptsValid() => SanPhamBLL.Validate(Valid());

    [Fact]
    public void Validate_RejectsBlankNameCategoryBrandPriceWarranty()
    {
        var s = Valid(); s.TenSP = " ";
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(s));
        s = Valid(); s.MaDM = 0;
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(s));
        s = Valid(); s.MaHang = 0;
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(s));
        s = Valid(); s.GiaBan = 0;
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(s));
        s = Valid(); s.GiaNhap = -1;
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(s));
        s = Valid(); s.ThoiGianBH = 121;
        Assert.Throws<BusinessException>(() => SanPhamBLL.Validate(s));
    }

    [Fact]
    public void Warning_WhenSellingBelowCost()
    {
        var s = Valid(); s.GiaBan = 500;
        Assert.NotNull(SanPhamBLL.Warning(s));
        Assert.Null(SanPhamBLL.Warning(Valid()));
    }
}

public class HoaDonTests
{
    private static ChiTietHoaDonDTO Line(int sp, int sl, decimal gia, params string[] serials) => new()
    { MaSP = sp, TenSP = "SP" + sp, SoLuong = sl, DonGia = gia, Serials = serials.ToList() };

    [Fact]
    public void TinhTongTien_SumsQuantityTimesPrice()
    {
        var items = new[] { Line(1, 2, 1_000_000, "A1-1", "A1-2"), Line(2, 1, 250_000, "B1-1") };
        Assert.Equal(2_250_000m, HoaDonBLL.TinhTongTien(items));
        Assert.Equal(2_050_000m, HoaDonBLL.TinhThanhToan(2_250_000m, 200_000m));
    }

    [Fact]
    public void Validate_AcceptsCorrectCart()
        => HoaDonBLL.Validate(new HoaDonDTO { GiamGia = 100 }, new[] { Line(1, 2, 1000, "S-001", "S-002") });

    [Fact]
    public void Validate_RejectsEmptyCart()
        => Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO(), new List<ChiTietHoaDonDTO>()));

    [Fact]
    public void Validate_RejectsNegativeOrTooLargeDiscount()
    {
        var items = new[] { Line(1, 1, 1000, "S-001") };
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO { GiamGia = -1 }, items));
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO { GiamGia = 1001 }, items));
        HoaDonBLL.Validate(new HoaDonDTO { GiamGia = 1000 }, items); // bằng tổng tiền vẫn hợp lệ
    }

    [Fact]
    public void Validate_RequiresExactSerialCount()
    {
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO(), new[] { Line(1, 2, 1000, "S-001") }));
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO(), new[] { Line(1, 1, 1000, "S-001", "S-002") }));
    }

    [Fact]
    public void Validate_RejectsDuplicateSerialAcrossLines()
    {
        var items = new[] { Line(1, 1, 1000, "S-001"), Line(2, 1, 1000, "s-001") };
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO(), items));
    }

    [Fact]
    public void Validate_RejectsDuplicateProductLines()
    {
        var items = new[] { Line(1, 1, 1000, "S-001"), Line(1, 1, 1000, "S-002") };
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO(), items));
    }

    [Fact]
    public void Validate_RejectsZeroQuantityOrNegativePrice()
    {
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO(), new[] { Line(1, 0, 1000) }));
        Assert.Throws<BusinessException>(() => HoaDonBLL.Validate(new HoaDonDTO(), new[] { Line(1, 1, -5, "S-001") }));
    }
}

public class PhieuNhapTests
{
    private static ChiTietPhieuNhapDTO Line(int sp, int sl, decimal gia, params string[] serials) => new()
    { MaSP = sp, TenSP = "SP" + sp, SoLuong = sl, DonGia = gia, Serials = serials.ToList() };

    [Fact]
    public void Validate_AcceptsCorrect()
        => PhieuNhapBLL.Validate(new PhieuNhapDTO { MaNCC = 1 }, new[] { Line(1, 2, 500, "SN-001", "SN-002") });

    [Fact]
    public void Validate_RequiresSupplierAndItems()
    {
        Assert.Throws<BusinessException>(() => PhieuNhapBLL.Validate(new PhieuNhapDTO { MaNCC = 0 }, new[] { Line(1, 1, 5, "SN-001") }));
        Assert.Throws<BusinessException>(() => PhieuNhapBLL.Validate(new PhieuNhapDTO { MaNCC = 1 }, new List<ChiTietPhieuNhapDTO>()));
    }

    [Fact]
    public void Validate_RejectsBadOrDuplicateSerialAndWrongCount()
    {
        Assert.Throws<BusinessException>(() => PhieuNhapBLL.Validate(new PhieuNhapDTO { MaNCC = 1 }, new[] { Line(1, 1, 5, "a b") }));
        Assert.Throws<BusinessException>(() => PhieuNhapBLL.Validate(new PhieuNhapDTO { MaNCC = 1 }, new[] { Line(1, 2, 5, "SN-001", "SN-001") }));
        Assert.Throws<BusinessException>(() => PhieuNhapBLL.Validate(new PhieuNhapDTO { MaNCC = 1 }, new[] { Line(1, 2, 5, "SN-001") }));
    }

    [Fact]
    public void TinhTongTien_Sums()
        => Assert.Equal(1_600_000m, PhieuNhapBLL.TinhTongTien(new[] { Line(1, 2, 500_000, "a-1", "a-2"), Line(2, 3, 200_000, "b-1", "b-2", "b-3") }));
}

public class BaoHanhTests
{
    private static readonly DateTime Today = new(2026, 10, 3);

    private static SerialDTO Sold(DateTime? han, string status = TrangThaiSerial.DaBan, int? maHD = 7)
        => new() { Serial = "SN-1", MaHD = maHD, HanBH = han, TrangThai = status };

    [Fact] public void NotFound() => Assert.False(BaoHanhBLL.KiemTra(null, Today).HopLe);

    [Fact]
    public void NotSold_IsRejected()
        => Assert.False(BaoHanhBLL.KiemTra(Sold(null, TrangThaiSerial.TrongKho, null), Today).HopLe);

    [Fact]
    public void AlreadyInWarranty_IsRejected()
        => Assert.False(BaoHanhBLL.KiemTra(Sold(Today.AddDays(30), TrangThaiSerial.DangBaoHanh), Today).HopLe);

    [Fact]
    public void Expired_IsRejected()
    {
        var r = BaoHanhBLL.KiemTra(Sold(Today.AddDays(-1)), Today);
        Assert.False(r.HopLe);
        Assert.Contains("hết hạn", r.ThongBao);
    }

    [Fact]
    public void LastDay_IsStillValid() => Assert.True(BaoHanhBLL.KiemTra(Sold(Today), Today).HopLe);

    [Fact]
    public void Valid_ReportsRemainingDays()
    {
        var r = BaoHanhBLL.KiemTra(Sold(Today.AddDays(10)), Today);
        Assert.True(r.HopLe);
        Assert.Contains("10 ngày", r.ThongBao);
    }
}
