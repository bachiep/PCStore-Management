using System.Data;
using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class SerialDAL
{
    public List<string> GetInStock(int maSP) => Map(
        ExecuteQuery("SELECT Serial FROM SanPhamSerial WHERE MaSP = @sp AND TrangThai = 'TrongKho' ORDER BY Serial", P("@sp", maSP)),
        r => r.Str("Serial"));

    public List<string> FindExisting(IEnumerable<string> serials)
    {
        var list = serials.ToList();
        if (list.Count == 0) return new();
        var ps = list.Select((s, i) => P("@s" + i, s)).ToArray();
        var sql = $"SELECT Serial FROM SanPhamSerial WHERE Serial IN ({string.Join(",", ps.Select(p => p.ParameterName))})";
        return Map(ExecuteQuery(sql, ps), r => r.Str("Serial"));
    }

    public SerialDTO? Lookup(string serial)
    {
        var dt = ExecuteQuery(@"SELECT s.*, sp.TenSP, hd.MaKH, kh.HoTen AS TenKH, kh.SDT AS SDTKH
                                FROM SanPhamSerial s
                                JOIN SanPham sp ON sp.MaSP = s.MaSP
                                LEFT JOIN HoaDon hd ON hd.MaHD = s.MaHD
                                LEFT JOIN KhachHang kh ON kh.MaKH = hd.MaKH
                                WHERE s.Serial = @s", P("@s", serial));
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new SerialDTO
        {
            Serial = r.Str("Serial"), MaSP = r.Int("MaSP"), TenSP = r.Str("TenSP"), MaPN = r.IntN("MaPN"),
            MaHD = r.IntN("MaHD"), NgayBan = r.DateN("NgayBan"), HanBH = r.DateN("HanBH"), TrangThai = r.Str("TrangThai"),
            MaKH = r.IntN("MaKH"), TenKH = r.StrN("TenKH"), SDTKH = r.StrN("SDTKH")
        };
    }

    public List<LichSuBanSerialDTO> GetSalesHistory(string serial) => Map(
        ExecuteQuery(@"SELECT ls.MaHD, ls.NgayBan, COALESCE(kh.HoTen, 'Khách lẻ') AS TenKH,
                              ct.DonGia, ls.HanBH, hd.DaHuy
                       FROM LichSuBanSerial ls
                       JOIN HoaDon hd ON hd.MaHD = ls.MaHD
                       LEFT JOIN KhachHang kh ON kh.MaKH = hd.MaKH
                       LEFT JOIN ChiTietHoaDon ct ON ct.MaHD = ls.MaHD AND ct.MaSP = ls.MaSP
                       WHERE ls.Serial = @s
                       ORDER BY ls.NgayBan DESC, ls.MaHD DESC", P("@s", serial)),
        r => new LichSuBanSerialDTO
        {
            MaHD = r.Int("MaHD"), NgayBan = r.Date("NgayBan"), TenKH = r.Str("TenKH"),
            DonGia = r.Dec("DonGia"), HanBH = r.DateN("HanBH"), DaHuy = r.Bool("DaHuy")
        });

    public int NextSequence(int maSP)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM SanPhamSerial WHERE MaSP = @sp", P("@sp", maSP))) + 1;
}

public class BaoHanhDAL
{
    private const string BaseSelect = @"SELECT b.*, sp.TenSP, kh.HoTen AS TenKH, nv.HoTen AS TenNV
                                        FROM PhieuBaoHanh b
                                        JOIN SanPhamSerial s ON s.Serial = b.Serial
                                        JOIN SanPham sp ON sp.MaSP = s.MaSP
                                        LEFT JOIN HoaDon hdBH ON hdBH.MaHD = b.MaHDLucTiepNhan
                                        LEFT JOIN KhachHang kh ON kh.MaKH = COALESCE(b.MaKH, hdBH.MaKH)
                                        JOIN NhanVien nv ON nv.MaNV = b.MaNV";

    private static PhieuBaoHanhDTO ToDto(DataRow r) => new()
    {
        MaPBH = r.Int("MaPBH"), Serial = r.Str("Serial"), MaHDLucTiepNhan = r.IntN("MaHDLucTiepNhan"), TenSP = r.Str("TenSP"), MaKH = r.IntN("MaKH"),
        TenKH = r.StrN("TenKH"), MaNV = r.Int("MaNV"), TenNV = r.Str("TenNV"), NgayNhan = r.Date("NgayNhan"),
        MoTaLoi = r.Str("MoTaLoi"), NgayTra = r.DateN("NgayTra"), KetQua = r.StrN("KetQua"), TrangThai = r.Str("TrangThai")
    };

    /// <param name="trangThai">rỗng = tất cả</param>
    public List<PhieuBaoHanhDTO> Search(string keyword, string trangThai) => Map(
        ExecuteQuery(BaseSelect + @" WHERE (b.Serial LIKE @k OR sp.TenSP LIKE @k OR kh.HoTen LIKE @k OR kh.SDT LIKE @k)
                                     AND (@tt = '' OR b.TrangThai = @tt) ORDER BY b.NgayNhan DESC, b.MaPBH DESC",
            P("@k", $"%{keyword}%"), P("@tt", trangThai)), ToDto);

    public List<PhieuBaoHanhDTO> GetBySerial(string serial) => Map(
        ExecuteQuery(BaseSelect + " WHERE b.Serial = @s ORDER BY b.NgayNhan DESC, b.MaPBH DESC", P("@s", serial)), ToDto);

    public int Create(PhieuBaoHanhDTO p) => InTransaction((conn, tran) =>
    {
        // Read the invoice link first, then lock invoice -> serial. Huy() uses the same
        // order, preventing a cancel/accept deadlock while still revalidating under lock.
        var link = ExecuteQuery(conn, tran, "SELECT MaHD FROM SanPhamSerial WHERE Serial=@s", P("@s", p.Serial));
        if (link.Rows.Count == 0) throw new InvalidOperationException("Không tìm thấy serial.");
        var maHD = link.Rows[0].IntN("MaHD");
        if (maHD == null) throw new InvalidOperationException("Serial không còn đủ điều kiện bảo hành.");

        var invoice = ExecuteQuery(conn, tran, "SELECT DaHuy FROM HoaDon WHERE MaHD=@hd FOR UPDATE", P("@hd", maHD));
        if (invoice.Rows.Count == 0 || invoice.Rows[0].Bool("DaHuy"))
            throw new InvalidOperationException("Hóa đơn đã bị hủy hoặc không còn tồn tại.");

        var serial = ExecuteQuery(conn, tran,
            "SELECT MaHD, HanBH, TrangThai FROM SanPhamSerial WHERE Serial=@s FOR UPDATE", P("@s", p.Serial));
        if (serial.Rows.Count == 0) throw new InvalidOperationException("Không tìm thấy serial.");
        var row = serial.Rows[0];
        if (row.IntN("MaHD") != maHD || row.Str("TrangThai") != TrangThaiSerial.DaBan
            || row.DateN("HanBH") is not DateTime hanBH || hanBH.Date < DateTime.Today)
            throw new InvalidOperationException("Serial không còn đủ điều kiện bảo hành.");

        var pending = Convert.ToInt32(ExecuteScalar(conn, tran,
            "SELECT COUNT(*) FROM PhieuBaoHanh WHERE Serial=@s AND TrangThai=@tt", P("@s", p.Serial), P("@tt", TrangThaiBaoHanh.DangXuLy)));
        if (pending > 0) throw new InvalidOperationException("Serial đã có phiếu bảo hành đang xử lý.");

        var id = ExecuteInsert(conn, tran,
            @"INSERT INTO PhieuBaoHanh(Serial, MaKH, MaNV, NgayNhan, MoTaLoi, TrangThai, MaHDLucTiepNhan)
              VALUES(@s, @kh, @nv, @ng, @mt, @tt, @hd)",
            P("@s", p.Serial), P("@kh", p.MaKH), P("@nv", p.MaNV), P("@ng", p.NgayNhan), P("@mt", p.MoTaLoi), P("@tt", p.TrangThai), P("@hd", maHD));
        var affected = ExecuteNonQuery(conn, tran,
            "UPDATE SanPhamSerial SET TrangThai='DangBaoHanh' WHERE Serial=@s AND MaHD=@hd AND TrangThai='DaBan'",
            P("@s", p.Serial), P("@hd", maHD));
        if (affected == 0) throw new InvalidOperationException("Trạng thái serial đã thay đổi; hãy tra cứu lại.");
        return id;
    });

    /// <summary>Cập nhật kết quả; khi phiếu kết thúc thì trả serial về trạng thái "Đã bán".</summary>
    public void UpdateResult(PhieuBaoHanhDTO p) => InTransaction((conn, tran) =>
    {
        var link = ExecuteQuery(conn, tran,
            "SELECT Serial, MaHDLucTiepNhan FROM PhieuBaoHanh WHERE MaPBH=@id", P("@id", p.MaPBH));
        if (link.Rows.Count == 0) throw new InvalidOperationException("Không tìm thấy phiếu bảo hành.");
        var serialNumber = link.Rows[0].Str("Serial");
        var maHD = link.Rows[0].IntN("MaHDLucTiepNhan");
        if (maHD == null) throw new InvalidOperationException("Phiếu bảo hành thiếu liên kết hóa đơn.");

        var invoice = ExecuteQuery(conn, tran, "SELECT DaHuy FROM HoaDon WHERE MaHD=@hd FOR UPDATE", P("@hd", maHD));
        if (invoice.Rows.Count == 0 || invoice.Rows[0].Bool("DaHuy"))
            throw new InvalidOperationException("Hóa đơn đã bị hủy hoặc không còn tồn tại.");

        var serial = ExecuteQuery(conn, tran,
            "SELECT MaHD, TrangThai FROM SanPhamSerial WHERE Serial=@s FOR UPDATE", P("@s", serialNumber));
        if (serial.Rows.Count == 0 || serial.Rows[0].IntN("MaHD") != maHD
            || serial.Rows[0].Str("TrangThai") != TrangThaiSerial.DangBaoHanh)
            throw new InvalidOperationException("Phiếu hoặc serial đã thay đổi; hãy tải lại dữ liệu.");

        var current = ExecuteQuery(conn, tran,
            "SELECT TrangThai FROM PhieuBaoHanh WHERE MaPBH=@id AND Serial=@s FOR UPDATE",
            P("@id", p.MaPBH), P("@s", serialNumber));
        if (current.Rows.Count == 0 || current.Rows[0].Str("TrangThai") != TrangThaiBaoHanh.DangXuLy)
            throw new InvalidOperationException("Phiếu hoặc serial đã thay đổi; hãy tải lại dữ liệu.");
        var affected = ExecuteNonQuery(conn, tran,
            "UPDATE PhieuBaoHanh SET KetQua=@kq, TrangThai=@tt, NgayTra=@nt WHERE MaPBH=@id AND TrangThai=@dangXuLy",
            P("@kq", p.KetQua), P("@tt", p.TrangThai), P("@nt", p.NgayTra), P("@id", p.MaPBH), P("@dangXuLy", TrangThaiBaoHanh.DangXuLy));
        if (affected == 0) throw new InvalidOperationException("Phiếu đã được cập nhật ở phiên khác.");
        if (p.TrangThai != TrangThaiBaoHanh.DangXuLy)
        {
            affected = ExecuteNonQuery(conn, tran,
                "UPDATE SanPhamSerial SET TrangThai='DaBan' WHERE Serial=@s AND MaHD=@hd AND TrangThai='DangBaoHanh'",
                P("@s", serialNumber), P("@hd", maHD));
            if (affected == 0) throw new InvalidOperationException("Serial đã thay đổi; không thể kết thúc phiếu.");
        }
        return 0;
    });
}
