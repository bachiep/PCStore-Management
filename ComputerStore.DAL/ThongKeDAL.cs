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
}
