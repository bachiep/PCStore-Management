using System.Data;
using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class PhieuNhapDAL
{
    public List<PhieuNhapDTO> Search(DateTime tuNgay, DateTime denNgay) => Map(
        ExecuteQuery(@"SELECT pn.*, ncc.TenNCC, nv.HoTen AS TenNV FROM PhieuNhap pn
                       JOIN NhaCungCap ncc ON ncc.MaNCC = pn.MaNCC
                       JOIN NhanVien nv ON nv.MaNV = pn.MaNV
                       WHERE CAST(pn.NgayNhap AS DATE) BETWEEN @tu AND @den
                       ORDER BY pn.NgayNhap DESC, pn.MaPN DESC", P("@tu", tuNgay.Date), P("@den", denNgay.Date)),
        r => new PhieuNhapDTO
        {
            MaPN = r.Int("MaPN"), NgayNhap = r.Date("NgayNhap"), MaNCC = r.Int("MaNCC"), TenNCC = r.Str("TenNCC"),
            MaNV = r.Int("MaNV"), TenNV = r.Str("TenNV"), TongTien = r.Dec("TongTien"), GhiChu = r.StrN("GhiChu")
        });

    public List<ChiTietPhieuNhapDTO> GetDetails(int maPN) => Map(
        ExecuteQuery(@"SELECT ct.*, sp.TenSP FROM ChiTietPhieuNhap ct JOIN SanPham sp ON sp.MaSP = ct.MaSP
                       WHERE ct.MaPN = @id", P("@id", maPN)),
        r => new ChiTietPhieuNhapDTO
        {
            MaPN = r.Int("MaPN"), MaSP = r.Int("MaSP"), TenSP = r.Str("TenSP"),
            SoLuong = r.Int("SoLuong"), DonGia = r.Dec("DonGia")
        });

    /// <summary>
    /// Tạo phiếu nhập trong một transaction:
    /// thêm phiếu → chi tiết → cộng tồn kho → cập nhật giá nhập → thêm serial.
    /// </summary>
    public int Create(PhieuNhapDTO pn, IList<ChiTietPhieuNhapDTO> items) => InTransaction((conn, tran) =>
    {
        // Khóa tất cả sản phẩm theo cùng một thứ tự trước khi thêm chi tiết.
        // Nếu chỉ khóa ở UPDATE sau INSERT ChiTietPhieuNhap, hai giao dịch có thể
        // cùng giữ shared FK lock trên một sản phẩm rồi deadlock khi nâng lên X lock.
        var orderedItems = items.OrderBy(x => x.MaSP).ToList();
        foreach (var item in orderedItems)
        {
            if (ExecuteScalar(conn, tran, "SELECT MaSP FROM SanPham WHERE MaSP = @sp FOR UPDATE",
                    P("@sp", item.MaSP)) == null)
                throw new InvalidOperationException($"Không tìm thấy sản phẩm {item.MaSP}.");
        }

        var maPN = ExecuteInsert(conn, tran,
            @"INSERT INTO PhieuNhap(NgayNhap, MaNCC, MaNV, TongTien, GhiChu)
              VALUES(@ng, @ncc, @nv, @tt, @gc)",
            P("@ng", pn.NgayNhap), P("@ncc", pn.MaNCC), P("@nv", pn.MaNV), P("@tt", pn.TongTien), P("@gc", pn.GhiChu));

        foreach (var it in orderedItems)
        {
            ExecuteNonQuery(conn, tran, "INSERT INTO ChiTietPhieuNhap(MaPN, MaSP, SoLuong, DonGia) VALUES(@pn, @sp, @sl, @dg)",
                P("@pn", maPN), P("@sp", it.MaSP), P("@sl", it.SoLuong), P("@dg", it.DonGia));

            ExecuteNonQuery(conn, tran, "UPDATE SanPham SET SoLuongTon = SoLuongTon + @sl, GiaNhap = @dg WHERE MaSP = @sp",
                P("@sl", it.SoLuong), P("@dg", it.DonGia), P("@sp", it.MaSP));

            foreach (var serial in it.Serials.OrderBy(s => s, StringComparer.Ordinal))
                ExecuteNonQuery(conn, tran, "INSERT INTO SanPhamSerial(Serial, MaSP, MaPN, TrangThai) VALUES(@s, @sp, @pn, 'TrongKho')",
                    P("@s", serial), P("@sp", it.MaSP), P("@pn", maPN));
        }
        return maPN;
    });
}

public class HoaDonDAL
{
    private const string BaseSelect = @"SELECT hd.*, kh.HoTen AS TenKH, kh.SDT AS SDTKH, nv.HoTen AS TenNV
                                        FROM HoaDon hd
                                        LEFT JOIN KhachHang kh ON kh.MaKH = hd.MaKH
                                        JOIN NhanVien nv ON nv.MaNV = hd.MaNV";

    private static HoaDonDTO ToDto(DataRow r) => new()
    {
        MaHD = r.Int("MaHD"), NgayLap = r.Date("NgayLap"), MaKH = r.IntN("MaKH"), TenKH = r.StrN("TenKH") ?? "Khách lẻ",
        SDTKH = r.StrN("SDTKH"), MaNV = r.Int("MaNV"), TenNV = r.Str("TenNV"), TongTien = r.Dec("TongTien"),
        GiamGia = r.Dec("GiamGia"), ThanhToan = r.Dec("ThanhToan"), GhiChu = r.StrN("GhiChu"),
        HinhThucTT = r.Str("HinhThucTT"), DaHuy = Convert.ToInt32(r["DaHuy"]) != 0, LyDoHuy = r.StrN("LyDoHuy"),
        NgayHuy = r["NgayHuy"] is DateTime nh ? nh : null
    };

    public List<HoaDonDTO> Search(DateTime tuNgay, DateTime denNgay, string keyword) => Map(
        ExecuteQuery(BaseSelect + @" WHERE CAST(hd.NgayLap AS DATE) BETWEEN @tu AND @den
                                     AND (@k = '' OR CAST(hd.MaHD AS CHAR) = @k OR kh.HoTen LIKE @kl OR kh.SDT LIKE @kl)
                                     ORDER BY hd.NgayLap DESC, hd.MaHD DESC",
            P("@tu", tuNgay.Date), P("@den", denNgay.Date), P("@k", keyword), P("@kl", $"%{keyword}%")), ToDto);

    public List<HoaDonDTO> GetByCustomer(int maKH) => Map(
        ExecuteQuery(BaseSelect + " WHERE hd.MaKH = @id ORDER BY hd.NgayLap DESC, hd.MaHD DESC", P("@id", maKH)), ToDto);

    public HoaDonDTO? GetById(int maHD)
    {
        var dt = ExecuteQuery(BaseSelect + " WHERE hd.MaHD = @id", P("@id", maHD));
        return dt.Rows.Count == 0 ? null : ToDto(dt.Rows[0]);
    }

    public List<ChiTietHoaDonDTO> GetDetails(int maHD)
    {
        var items = DbHelper.Map(
            ExecuteQuery(@"SELECT ct.*, ct.TenSPKhiBan AS TenSP,
                                  ct.ThoiGianBHKhiBan AS ThoiGianBH
                           FROM ChiTietHoaDon ct JOIN SanPham sp ON sp.MaSP = ct.MaSP WHERE ct.MaHD = @id", P("@id", maHD)),
            r => new ChiTietHoaDonDTO
            {
                MaHD = r.Int("MaHD"), MaSP = r.Int("MaSP"), TenSP = r.StrN("TenSP") ?? "Không có snapshot lịch sử", SoLuong = r.Int("SoLuong"),
                DonGia = r.Dec("DonGia"), ThoiGianBH = r.IntN("ThoiGianBH"),
                TenSPKhiBan = r.StrN("TenSPKhiBan"), ThoiGianBHKhiBan = r.Int("ThoiGianBHKhiBan")
            });

        var serials = ExecuteQuery("SELECT Serial, MaSP FROM LichSuBanSerial WHERE MaHD = @id ORDER BY Serial", P("@id", maHD));
        foreach (DataRow r in serials.Rows)
            items.FirstOrDefault(i => i.MaSP == r.Int("MaSP"))?.Serials.Add(r.Str("Serial"));
        return items;
    }

    /// <summary>
    /// Lập hóa đơn trong một transaction:
    /// thêm hóa đơn → chi tiết → trừ tồn (kiểm tra đủ hàng) → đánh dấu serial đã bán + hạn bảo hành.
    /// </summary>
    public int Create(HoaDonDTO hd, IList<ChiTietHoaDonDTO> items) => InTransaction((conn, tran) =>
    {
        var maHD = ExecuteInsert(conn, tran,
            @"INSERT INTO HoaDon(NgayLap, MaKH, MaNV, TongTien, GiamGia, ThanhToan, GhiChu, HinhThucTT)
              VALUES(@ng, @kh, @nv, @tt, @gg, @thtt, @gc, @httt)",
            P("@ng", hd.NgayLap), P("@kh", hd.MaKH), P("@nv", hd.MaNV), P("@tt", hd.TongTien),
            P("@gg", hd.GiamGia), P("@thtt", hd.ThanhToan), P("@gc", hd.GhiChu), P("@httt", hd.HinhThucTT));

        foreach (var it in items.OrderBy(x => x.MaSP))
        {
            var product = ExecuteQuery(conn, tran, "SELECT TenSP, ThoiGianBH, GiaBan FROM SanPham WHERE MaSP=@sp AND TrangThai=1 FOR UPDATE", P("@sp", it.MaSP));
            if (product.Rows.Count == 0) throw new InvalidOperationException($"Sản phẩm \"{it.TenSP}\" không còn kinh doanh.");
            var tenSP = product.Rows[0].Str("TenSP");
            var thoiGianBH = product.Rows[0].Int("ThoiGianBH");
            var giaBan = product.Rows[0].Dec("GiaBan");
            if (it.DonGia != giaBan)
                throw new InvalidOperationException($"Giá của sản phẩm \"{tenSP}\" đã thay đổi. Hãy cập nhật giỏ hàng trước khi thanh toán.");
            it.TenSPKhiBan = tenSP;
            it.ThoiGianBHKhiBan = thoiGianBH;
            it.ThoiGianBH = thoiGianBH;

            ExecuteNonQuery(conn, tran, @"INSERT INTO ChiTietHoaDon(MaHD, MaSP, SoLuong, DonGia, TenSPKhiBan, ThoiGianBHKhiBan)
                                          VALUES(@hd, @sp, @sl, @dg, @ten, @bh)",
                P("@hd", maHD), P("@sp", it.MaSP), P("@sl", it.SoLuong), P("@dg", it.DonGia), P("@ten", tenSP), P("@bh", thoiGianBH));

            var affected = ExecuteNonQuery(conn, tran,
                "UPDATE SanPham SET SoLuongTon = SoLuongTon - @sl WHERE MaSP = @sp AND SoLuongTon >= @sl",
                P("@sl", it.SoLuong), P("@sp", it.MaSP));
            if (affected == 0)
                throw new InvalidOperationException($"Sản phẩm \"{it.TenSP}\" không đủ hàng trong kho.");

            foreach (var serial in it.Serials.OrderBy(s => s, StringComparer.Ordinal))
            {
                var ok = ExecuteNonQuery(conn, tran,
                    @"UPDATE SanPhamSerial SET MaHD = @hd, NgayBan = @ng,
                      HanBH = CASE WHEN @bh > 0 THEN DATE_ADD(CAST(@ng AS DATE), INTERVAL @bh MONTH) ELSE NULL END,
                      TrangThai = 'DaBan' WHERE Serial = @s AND MaSP = @sp AND TrangThai = 'TrongKho'",
                    P("@hd", maHD), P("@ng", hd.NgayLap), P("@bh", thoiGianBH), P("@s", serial), P("@sp", it.MaSP));
                if (ok == 0)
                    throw new InvalidOperationException($"Serial {serial} không hợp lệ hoặc đã được bán.");

                ExecuteNonQuery(conn, tran, @"INSERT INTO LichSuBanSerial(MaHD, MaSP, Serial, NgayBan, HanBH)
                                              VALUES(@hd, @sp, @s, @ng,
                                                     CASE WHEN @bh > 0 THEN DATE_ADD(CAST(@ng AS DATE), INTERVAL @bh MONTH) ELSE NULL END)",
                    P("@hd", maHD), P("@sp", it.MaSP), P("@s", serial), P("@ng", hd.NgayLap), P("@bh", thoiGianBH));
            }
        }
        return maHD;
    });

    /// <summary>
    /// Hủy hóa đơn trong một transaction: đánh dấu đã hủy, cộng lại tồn, trả serial về kho.
    /// Serial đang bảo hành thì không cho hủy.
    /// </summary>
    public void Huy(int maHD, string lyDo) => InTransaction((conn, tran) =>
    {
        var daHuy = ExecuteNonQuery(conn, tran,
            "UPDATE HoaDon SET DaHuy = 1, LyDoHuy = @ld, NgayHuy = NOW() WHERE MaHD = @id AND DaHuy = 0",
            P("@ld", lyDo), P("@id", maHD));
        if (daHuy == 0) throw new InvalidOperationException("Hóa đơn không tồn tại hoặc đã bị hủy trước đó.");

        // Khóa sản phẩm theo cùng thứ tự POS/nhập kho trước khi khóa serial để tránh
        // tạo chu trình product -> serial và serial -> product giữa các giao dịch.
        var productIds = ExecuteQuery(conn, tran,
            "SELECT DISTINCT MaSP FROM ChiTietHoaDon WHERE MaHD=@id ORDER BY MaSP", P("@id", maHD));
        foreach (DataRow row in productIds.Rows)
            ExecuteQuery(conn, tran, "SELECT MaSP FROM SanPham WHERE MaSP=@sp FOR UPDATE", P("@sp", row.Int("MaSP")));

        // Khóa serial trước khi kiểm tra: tiếp nhận bảo hành cũng khóa cùng hàng,
        // nên không thể lọt một phiếu mới giữa lúc kiểm tra và trả serial về kho.
        var serials = ExecuteQuery(conn, tran,
            "SELECT Serial, TrangThai FROM SanPhamSerial WHERE MaHD=@id ORDER BY Serial FOR UPDATE", P("@id", maHD));
        if (serials.Rows.Cast<DataRow>().Any(r => r.Str("TrangThai") == TrangThaiSerial.DangBaoHanh))
            throw new InvalidOperationException("Hóa đơn có sản phẩm đang bảo hành, không thể hủy.");
        var warrantyHistory = ExecuteQuery(conn, tran,
            "SELECT MaPBH FROM PhieuBaoHanh WHERE MaHDLucTiepNhan=@id FOR UPDATE", P("@id", maHD));
        if (warrantyHistory.Rows.Count > 0) throw new InvalidOperationException("Hóa đơn có lịch sử bảo hành, không thể hủy.");

        ExecuteNonQuery(conn, tran,
            @"UPDATE SanPham sp JOIN ChiTietHoaDon ct ON ct.MaSP = sp.MaSP
              SET sp.SoLuongTon = sp.SoLuongTon + ct.SoLuong WHERE ct.MaHD = @id", P("@id", maHD));
        ExecuteNonQuery(conn, tran,
            "UPDATE SanPhamSerial SET MaHD = NULL, NgayBan = NULL, HanBH = NULL, TrangThai = 'TrongKho' WHERE MaHD = @id",
            P("@id", maHD));
        return maHD;
    });
}
