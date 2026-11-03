using System.Data;
using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class ThongKeDAL
{
    public List<DoanhThuDTO> DoanhThuTheoNgay(DateTime tu, DateTime den) => Map(
        ExecuteProcedure("sp_DoanhThuTheoNgay", P("@TuNgay", tu.Date), P("@DenNgay", den.Date)),
        r => new DoanhThuDTO
        {
            Ngay = r.Date("Ngay"), Nhan = r.Date("Ngay").ToString("dd/MM"),
            SoHoaDon = r.Int("SoHoaDon"), DoanhThu = r.Dec("DoanhThu")
        });

    public List<DoanhThuDTO> DoanhThuTheoThang(int nam) => Map(
        ExecuteProcedure("sp_DoanhThuTheoThang", P("@Nam", nam)),
        r => new DoanhThuDTO
        {
            Nhan = "T" + r.Int("Thang"), SoHoaDon = r.Int("SoHoaDon"),
            DoanhThu = r.Dec("DoanhThu"), LoiNhuan = r.Dec("LoiNhuanUocTinh")
        });

    public List<TopSanPhamDTO> TopBanChay(DateTime tu, DateTime den, int top) => Map(
        ExecuteProcedure("sp_TopSanPhamBanChay", P("@TuNgay", tu.Date), P("@DenNgay", den.Date), P("@TopN", top)),
        r => new TopSanPhamDTO
        {
            MaSP = r.Int("MaSP"), TenSP = r.Str("TenSP"), SoLuongBan = r.Int("SoLuongBan"), DoanhThu = r.Dec("DoanhThu")
        });

    public DataTable SanPhamSapHet(int nguong) => ExecuteProcedure("sp_SanPhamSapHet", P("@Nguong", nguong));

    public TongQuanDTO TongQuan(int nguongSapHet)
    {
        var dt = ExecuteQuery(@"SELECT
              (SELECT IFNULL(SUM(ThanhToan),0) FROM HoaDon WHERE DaHuy = 0 AND DATE(NgayLap) = CURDATE()) AS DTHomNay,
              (SELECT COUNT(*) FROM HoaDon WHERE DaHuy = 0 AND DATE(NgayLap) = CURDATE()) AS HDHomNay,
              (SELECT IFNULL(SUM(ThanhToan),0) FROM HoaDon WHERE DaHuy = 0 AND YEAR(NgayLap) = YEAR(CURDATE()) AND MONTH(NgayLap) = MONTH(CURDATE())) AS DTThang,
              (SELECT COUNT(*) FROM SanPham WHERE TrangThai = 1 AND SoLuongTon <= @n) AS SapHet,
              (SELECT COUNT(*) FROM KhachHang) AS SoKH,
              (SELECT COUNT(*) FROM PhieuBaoHanh WHERE TrangThai = 'Đang xử lý') AS BHDangXuLy", P("@n", nguongSapHet));
        var r = dt.Rows[0];
        return new TongQuanDTO
        {
            DoanhThuHomNay = r.Dec("DTHomNay"), SoHoaDonHomNay = r.Int("HDHomNay"), DoanhThuThangNay = r.Dec("DTThang"),
            SoSanPhamSapHet = r.Int("SapHet"), SoKhachHang = r.Int("SoKH"), SoPhieuBaoHanhDangXuLy = r.Int("BHDangXuLy")
        };
    }

    public List<DoanhThuDanhMucDTO> DoanhThuTheoDanhMuc()
    {
        var dt = ExecuteQuery(@"WITH DongHoaDon AS (
                                   SELECT ct.MaHD, ct.MaSP,
                                          CAST(ct.SoLuong * ct.DonGia AS DECIMAL(18,0)) AS TienHang,
                                          hd.GiamGia,
                                          SUM(ct.SoLuong * ct.DonGia) OVER (PARTITION BY ct.MaHD) AS TongTienHang
                                   FROM ChiTietHoaDon ct
                                   JOIN HoaDon hd ON hd.MaHD = ct.MaHD
                                   WHERE hd.DaHuy = 0
                               ),
                               TyLeGiam AS (
                                   SELECT d.*,
                                          CASE WHEN d.TongTienHang > 0 THEN FLOOR(d.GiamGia * d.TienHang / d.TongTienHang) ELSE 0 END AS GiamGiaCoBan,
                                          CASE WHEN d.TongTienHang > 0 THEN MOD(d.GiamGia * d.TienHang, d.TongTienHang) ELSE 0 END AS PhanDu
                                   FROM DongHoaDon d
                               ),
                               XepHangPhanDu AS (
                                   SELECT d.*,
                                          ROW_NUMBER() OVER (PARTITION BY d.MaHD ORDER BY d.PhanDu DESC, d.MaSP) AS HangPhanDu,
                                          SUM(d.GiamGiaCoBan) OVER (PARTITION BY d.MaHD) AS TongGiamGiaCoBan
                                   FROM TyLeGiam d
                               ),
                               DongDaPhanBo AS (
                                   SELECT d.*,
                                          d.GiamGiaCoBan + CASE WHEN d.HangPhanDu <= d.GiamGia - d.TongGiamGiaCoBan THEN 1 ELSE 0 END AS GiamGiaPhanBo
                                   FROM XepHangPhanDu d
                               )
                               SELECT dm.TenDM, SUM(d.TienHang - d.GiamGiaPhanBo) AS DoanhThu
                               FROM DongDaPhanBo d
                               JOIN SanPham sp ON sp.MaSP = d.MaSP
                               JOIN DanhMuc dm ON dm.MaDM = sp.MaDM
                               GROUP BY dm.MaDM, dm.TenDM
                               HAVING DoanhThu > 0
                               ORDER BY DoanhThu DESC");
        var list = Map(dt, r => new DoanhThuDanhMucDTO
        {
            TenDM = r.Str("TenDM"),
            DoanhThu = r.Dec("DoanhThu")
        });
        var total = (double)list.Sum(x => x.DoanhThu);
        if (total > 0)
        {
            foreach (var item in list) item.TyLe = (double)item.DoanhThu / total * 100.0;
        }
        return list;
    }

    public List<HoaDonGanNhatDTO> HoaDonGanNhat(int top = 5) => Map(
        ExecuteQuery(@"SELECT hd.MaHD, IFNULL(kh.HoTen, 'Khách lẻ') AS TenKH, hd.NgayLap, hd.ThanhToan
                       FROM HoaDon hd
                       LEFT JOIN KhachHang kh ON hd.MaKH = kh.MaKH
                       WHERE hd.DaHuy = 0
                       ORDER BY hd.NgayLap DESC, hd.MaHD DESC
                       LIMIT @n", P("@n", top)),
        r => new HoaDonGanNhatDTO
        {
            MaHD = r.Int("MaHD"),
            TenKH = r.Str("TenKH"),
            NgayLap = r.Date("NgayLap"),
            ThanhToan = r.Dec("ThanhToan")
        });
}
