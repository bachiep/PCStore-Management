using ComputerStore.BLL;
using ComputerStore.DAL;
using ComputerStore.DTO;

namespace ComputerStore.Tests;

/// <summary>
/// Kiểm thử tích hợp với MySQL thật (cổng 3307). Nếu CSDL không chạy, các test tự bỏ qua (pass trống).
/// Các test chạy tuần tự trong cùng một collection vì dùng chung Session và CSDL.
/// </summary>
[Collection("Database")]
public class IntegrationTests
{
    private static bool DbUp() => DbHelper.TestConnection(out _);

    private static void LoginAdmin() => new TaiKhoanBLL().DangNhap("admin", "123456");

    private static int Stock(int maSP) => new SanPhamBLL().GetById(maSP)!.SoLuongTon;

    private static string Tag => DateTime.Now.ToString("HHmmssfff");

    [Fact]
    public void Login_Succeeds_WrongPasswordAndInjectionFail()
    {
        if (!DbUp()) return;
        var bll = new TaiKhoanBLL();
        var u = bll.DangNhap("admin", "123456");
        Assert.Equal(VaiTroConst.Admin, u.VaiTro);
        Assert.True(Session.IsAdmin);

        Assert.Throws<BusinessException>(() => bll.DangNhap("admin", "sai-mat-khau"));
        Assert.Throws<BusinessException>(() => bll.DangNhap("' OR 1=1 --", "x"));
        Assert.Throws<BusinessException>(() => bll.DangNhap("admin' --", "x"));

        var nv = bll.DangNhap("nhanvien", "123456");
        Assert.Equal(VaiTroConst.NhanVien, nv.VaiTro);
        Assert.False(Session.IsAdmin);
        Assert.Throws<BusinessException>(() => new NhaCungCapBLL().Save(new NhaCungCapDTO { TenNCC = "X" }));
    }

    [Fact]
    public void Catalog_Queries_ReturnSeedData()
    {
        if (!DbUp()) return;
        LoginAdmin();
        Assert.True(new SanPhamBLL().Search().Count >= 32);
        Assert.Equal(12, new DanhMucBLL().GetAll().Count);
        Assert.True(new HangBLL().GetAll().Count >= 16);
        Assert.NotEmpty(new SanPhamBLL().Search("RTX"));
        Assert.True(new KhachHangBLL().Search().Count >= 10);
        Assert.True(new NhanVienBLL().Search().Count >= 4);
    }

    [Fact]
    public void Statistics_ProceduresWork()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var tk = new ThongKeBLL();
        var thang = tk.DoanhThuTheoThang(DateTime.Today.Year);
        Assert.Equal(12, thang.Count);
        var tu = DateTime.Today.AddYears(-1);
        Assert.NotEmpty(tk.DoanhThuTheoNgay(tu, DateTime.Today));
        var top = tk.TopBanChay(tu, DateTime.Today, 5);
        Assert.InRange(top.Count, 1, 5);
        Assert.True(top[0].SoLuongBan >= top[^1].SoLuongBan);
        Assert.NotNull(tk.SanPhamSapHet());
        Assert.Equal(7, tk.DoanhThu7Ngay().Count);
        var tq = tk.TongQuan();
        Assert.True(tq.SoKhachHang >= 10);
    }

    [Fact]
    public void Customer_Crud_And_DuplicatePhone()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var bll = new KhachHangBLL();
        var phone = "09" + (DateTime.Now.Ticks % 100_000_000).ToString("D8");
        var id = bll.Save(new KhachHangDTO { HoTen = "Khách thử nghiệm", SDT = phone });
        Assert.True(id > 0);
        Assert.Throws<BusinessException>(() => bll.Save(new KhachHangDTO { HoTen = "Trùng", SDT = phone }));
        Assert.NotNull(bll.GetByPhone(phone));
        bll.Delete(id);
        Assert.Null(bll.GetByPhone(phone));
    }

    [Fact]
    public void FullFlow_Import_Sell_Rollback_Warranty()
    {
        if (!DbUp()) return;
        LoginAdmin();
        const int maSP = 8; // Kingston Fury 16GB DDR4 (BH 36 tháng)
        var sp = new SanPhamBLL().GetById(maSP)!;
        var ton0 = Stock(maSP);
        var tag = Tag;
        var serials = new[] { $"IT-{tag}-1", $"IT-{tag}-2", $"IT-{tag}-3" };
        var giaNhap0 = sp.GiaNhap;

        try
        {
            RunFullFlow(maSP, sp, ton0, tag, serials);
        }
        finally
        {
            Cleanup(maSP, ton0, giaNhap0, serials);
        }
    }

    /// <summary>Xóa toàn bộ dữ liệu test tạo ra và trả tồn kho/giá nhập về nguyên trạng.</summary>
    private static void Cleanup(int maSP, int ton0, decimal giaNhap0, string[] serials)
    {
        var inList = string.Join(",", serials.Select(s => "'" + s + "'"));
        var hd = DbHelper.ExecuteQuery($"SELECT DISTINCT MaHD FROM SanPhamSerial WHERE Serial IN ({inList}) AND MaHD IS NOT NULL")
            .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToInt32(r[0])).ToList();
        var pn = DbHelper.ExecuteQuery($"SELECT DISTINCT MaPN FROM SanPhamSerial WHERE Serial IN ({inList}) AND MaPN IS NOT NULL")
            .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToInt32(r[0])).ToList();

        DbHelper.ExecuteNonQuery($"DELETE FROM PhieuBaoHanh WHERE Serial IN ({inList})");
        DbHelper.ExecuteNonQuery($"DELETE FROM SanPhamSerial WHERE Serial IN ({inList})");
        foreach (var id in hd)
        {
            DbHelper.ExecuteNonQuery("DELETE FROM ChiTietHoaDon WHERE MaHD=@id", DbHelper.P("@id", id));
            DbHelper.ExecuteNonQuery("DELETE FROM HoaDon WHERE MaHD=@id", DbHelper.P("@id", id));
        }
        foreach (var id in pn)
        {
            DbHelper.ExecuteNonQuery("DELETE FROM ChiTietPhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
            DbHelper.ExecuteNonQuery("DELETE FROM PhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
        }
        DbHelper.ExecuteNonQuery("UPDATE SanPham SET SoLuongTon=@t, GiaNhap=@g WHERE MaSP=@sp",
            DbHelper.P("@t", ton0), DbHelper.P("@g", giaNhap0), DbHelper.P("@sp", maSP));
    }

    private static void RunFullFlow(int maSP, SanPhamDTO sp, int ton0, string tag, string[] serials)
    {

        // 1) Nhập kho 3 serial -> tồn +3
        var pn = new PhieuNhapBLL();
        var maPN = pn.LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 },
            new[] { new ChiTietPhieuNhapDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 3, DonGia = 900_000, Serials = serials.ToList() } });
        Assert.True(maPN > 0);
        Assert.Equal(ton0 + 3, Stock(maSP));
        Assert.Contains(serials[0], new HoaDonBLL().GetSerialsInStock(maSP));
        Assert.Equal(900_000m, new SanPhamBLL().GetById(maSP)!.GiaNhap);

        // nhập trùng serial -> bị từ chối, tồn không đổi
        Assert.Throws<BusinessException>(() => pn.LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 },
            new[] { new ChiTietPhieuNhapDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = 1, Serials = { serials[0] } } }));
        Assert.Equal(ton0 + 3, Stock(maSP));

        // 2) Bán 2 serial -> tồn -2, serial DaBan, HanBH = hôm nay + 36 tháng
        var hdBll = new HoaDonBLL();
        var maHD = hdBll.LapHoaDon(new HoaDonDTO { GiamGia = 50_000 },
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 2, DonGia = sp.GiaBan, ThoiGianBH = sp.ThoiGianBH,
                                           Serials = { serials[0], serials[1] } } });
        Assert.Equal(ton0 + 1, Stock(maSP));
        var hd = hdBll.GetById(maHD)!;
        Assert.Equal(sp.GiaBan * 2, hd.TongTien);
        Assert.Equal(sp.GiaBan * 2 - 50_000, hd.ThanhToan);
        var s0 = new BaoHanhBLL().TraCuu(serials[0])!;
        Assert.Equal(TrangThaiSerial.DaBan, s0.TrangThai);
        Assert.Equal(DateTime.Today.AddMonths(36), s0.HanBH!.Value.Date);
        Assert.Single(hdBll.GetDetails(maHD));
        Assert.Equal(2, hdBll.GetDetails(maHD)[0].Serials.Count);

        // 3) Bán serial đã bán -> lỗi + rollback (không sinh hóa đơn, tồn không đổi)
        var soHD = hdBll.Search(DateTime.Today, DateTime.Today).Count;
        Assert.Throws<BusinessException>(() => hdBll.LapHoaDon(new HoaDonDTO(),
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = sp.GiaBan, ThoiGianBH = 36, Serials = { serials[0] } } }));
        Assert.Equal(ton0 + 1, Stock(maSP));
        Assert.Equal(soHD, hdBll.Search(DateTime.Today, DateTime.Today).Count);

        // 4) Bán vượt tồn -> lỗi, rollback
        var sp2 = new SanPhamBLL().GetById(maSP)!;
        Assert.Throws<BusinessException>(() => hdBll.LapHoaDon(new HoaDonDTO(),
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = sp2.SoLuongTon + 1, DonGia = 1, ThoiGianBH = 36,
                                           Serials = Enumerable.Range(0, sp2.SoLuongTon + 1).Select(i => $"X-{tag}-{i}").ToList() } }));
        Assert.Equal(ton0 + 1, Stock(maSP));

        // 5) Bảo hành: serial chưa bán bị từ chối; serial đã bán lập phiếu được
        var bh = new BaoHanhBLL();
        Assert.Throws<BusinessException>(() => bh.LapPhieu(serials[2], "Lỗi"));
        var maPBH = bh.LapPhieu(serials[0], "Không nhận RAM");
        Assert.Equal(TrangThaiSerial.DangBaoHanh, bh.TraCuu(serials[0])!.TrangThai);
        Assert.Throws<BusinessException>(() => bh.LapPhieu(serials[0], "Lập lần 2"));
        var phieu = bh.LichSu(serials[0]).Single(p => p.MaPBH == maPBH);
        Assert.Throws<BusinessException>(() => bh.CapNhatKetQua(phieu, TrangThaiBaoHanh.DaTraKhach, ""));
        bh.CapNhatKetQua(phieu, TrangThaiBaoHanh.DaTraKhach, "Đổi sản phẩm mới");
        Assert.Equal(TrangThaiSerial.DaBan, bh.TraCuu(serials[0])!.TrangThai);
        Assert.Throws<BusinessException>(() => bh.CapNhatKetQua(phieu, TrangThaiBaoHanh.TuChoi, "x"));
    }
    [Fact]
    public void CancelInvoice_RestoresStockSerialsAndRevenue_AdminOnly()
    {
        if (!DbUp()) return;
        LoginAdmin();
        const int maSP = 8;
        var sp = new SanPhamBLL().GetById(maSP)!;
        var ton0 = Stock(maSP);
        var tag = Tag;
        var serials = new[] { $"CX-{tag}-1", $"CX-{tag}-2" };
        var giaNhap0 = sp.GiaNhap;
        var hdBll = new HoaDonBLL();
        int maHD = 0;

        try
        {
            new PhieuNhapBLL().LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 },
                new[] { new ChiTietPhieuNhapDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 2, DonGia = 900_000, Serials = serials.ToList() } });
            maHD = hdBll.LapHoaDon(new HoaDonDTO { HinhThucTT = HinhThucThanhToan.ChuyenKhoan },
                new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 2, DonGia = sp.GiaBan, ThoiGianBH = sp.ThoiGianBH, Serials = serials.ToList() } });
            Assert.Equal(ton0, Stock(maSP));
            Assert.Equal(HinhThucThanhToan.ChuyenKhoan, hdBll.GetById(maHD)!.HinhThucTT);
            Assert.Throws<BusinessException>(() => hdBll.LapHoaDon(new HoaDonDTO { HinhThucTT = "Bitcoin" },
                new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = 1, Serials = { serials[0] } } }));

            // nhân viên không được hủy; thiếu lý do bị từ chối
            new TaiKhoanBLL().DangNhap("nhanvien", "123456");
            Assert.Throws<BusinessException>(() => hdBll.HuyHoaDon(maHD, "thử"));
            LoginAdmin();
            Assert.Throws<BusinessException>(() => hdBll.HuyHoaDon(maHD, "  "));

            var dt0 = new ThongKeBLL().TongQuan().DoanhThuThangNay;
            hdBll.HuyHoaDon(maHD, "Khách đổi ý");

            var hd = hdBll.GetById(maHD)!;
            Assert.True(hd.DaHuy);
            Assert.Equal("Khách đổi ý", hd.LyDoHuy);
            Assert.Equal(ton0 + 2, Stock(maSP));
            Assert.All(serials, s => Assert.Equal(TrangThaiSerial.TrongKho, new BaoHanhBLL().TraCuu(s)!.TrangThai));
            Assert.Equal(dt0 - hd.ThanhToan, new ThongKeBLL().TongQuan().DoanhThuThangNay);
            Assert.Throws<BusinessException>(() => hdBll.HuyHoaDon(maHD, "Hủy lần 2"));
        }
        finally
        {
            var inList = string.Join(",", serials.Select(s => "'" + s + "'"));
            var pn = DbHelper.ExecuteQuery($"SELECT DISTINCT MaPN FROM SanPhamSerial WHERE Serial IN ({inList}) AND MaPN IS NOT NULL")
                .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToInt32(r[0])).ToList();
            DbHelper.ExecuteNonQuery($"DELETE FROM SanPhamSerial WHERE Serial IN ({inList})");
            if (maHD > 0)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietHoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD));
                DbHelper.ExecuteNonQuery("DELETE FROM HoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD));
            }
            foreach (var id in pn)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietPhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
                DbHelper.ExecuteNonQuery("DELETE FROM PhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
            }
            DbHelper.ExecuteNonQuery("UPDATE SanPham SET SoLuongTon=@t, GiaNhap=@g WHERE MaSP=@sp",
                DbHelper.P("@t", ton0), DbHelper.P("@g", giaNhap0), DbHelper.P("@sp", maSP));
        }
    }

    [Fact]
    public void Backup_Restore_RoundTrip_And_AdminOnly()
    {
        if (!DbUp()) return;
        var path = Path.Combine(Path.GetTempPath(), $"bk_{Tag}.sql");
        try
        {
            new TaiKhoanBLL().DangNhap("nhanvien", "123456");
            Assert.Throws<BusinessException>(() => new BackupBLL().Backup(path));

            LoginAdmin();
            var bll = new BackupBLL();
            Assert.True(bll.Backup(path) > 100);
            Assert.Contains(bll.NhatKy(), x => x.HanhDong == "Sao lưu dữ liệu");
            var spBefore = new SanPhamBLL().Search().Count;

            var ten = "BK" + Tag;
            DbHelper.ExecuteNonQuery("INSERT INTO NhaCungCap(TenNCC) VALUES(@t)", DbHelper.P("@t", ten));
            Assert.NotNull(DbHelper.ExecuteScalar("SELECT MaNCC FROM NhaCungCap WHERE TenNCC=@t", DbHelper.P("@t", ten)));

            bll.Restore(path);
            Assert.Null(DbHelper.ExecuteScalar("SELECT MaNCC FROM NhaCungCap WHERE TenNCC=@t", DbHelper.P("@t", ten)));
            Assert.Equal(spBefore, new SanPhamBLL().Search().Count);
            Assert.Throws<BusinessException>(() => bll.Restore(path + ".missing"));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
