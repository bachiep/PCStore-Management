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

    public int NextSequence(int maSP)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM SanPhamSerial WHERE MaSP = @sp", P("@sp", maSP))) + 1;
}

public class BaoHanhDAL
{
    private const string BaseSelect = @"SELECT b.*, sp.TenSP, kh.HoTen AS TenKH, nv.HoTen AS TenNV
                                        FROM PhieuBaoHanh b
                                        JOIN SanPhamSerial s ON s.Serial = b.Serial
                                        JOIN SanPham sp ON sp.MaSP = s.MaSP
                                        LEFT JOIN KhachHang kh ON kh.MaKH = b.MaKH
                                        JOIN NhanVien nv ON nv.MaNV = b.MaNV";

    private static PhieuBaoHanhDTO ToDto(DataRow r) => new()
    {
        MaPBH = r.Int("MaPBH"), Serial = r.Str("Serial"), TenSP = r.Str("TenSP"), MaKH = r.IntN("MaKH"),
        TenKH = r.StrN("TenKH"), MaNV = r.Int("MaNV"), TenNV = r.Str("TenNV"), NgayNhan = r.Date("NgayNhan"),
        MoTaLoi = r.Str("MoTaLoi"), NgayTra = r.DateN("NgayTra"), KetQua = r.StrN("KetQua"), TrangThai = r.Str("TrangThai")
    };

    /// <param name="trangThai">rỗng = tất cả</param>
    public List<PhieuBaoHanhDTO> Search(string keyword, string trangThai) => Map(
        ExecuteQuery(BaseSelect + @" WHERE (b.Serial LIKE @k OR sp.TenSP LIKE @k OR kh.HoTen LIKE @k OR kh.SDT LIKE @k)
                                     AND (@tt = '' OR b.TrangThai = @tt) ORDER BY b.NgayNhan DESC",
            P("@k", $"%{keyword}%"), P("@tt", trangThai)), ToDto);

    public List<PhieuBaoHanhDTO> GetBySerial(string serial) => Map(
        ExecuteQuery(BaseSelect + " WHERE b.Serial = @s ORDER BY b.NgayNhan DESC", P("@s", serial)), ToDto);

    public int Create(PhieuBaoHanhDTO p) => InTransaction((conn, tran) =>
    {
        var id = ExecuteInsert(conn, tran,
            @"INSERT INTO PhieuBaoHanh(Serial, MaKH, MaNV, NgayNhan, MoTaLoi, TrangThai)
              VALUES(@s, @kh, @nv, @ng, @mt, @tt)",
            P("@s", p.Serial), P("@kh", p.MaKH), P("@nv", p.MaNV), P("@ng", p.NgayNhan), P("@mt", p.MoTaLoi), P("@tt", p.TrangThai));
        ExecuteNonQuery(conn, tran, "UPDATE SanPhamSerial SET TrangThai = 'DangBaoHanh' WHERE Serial = @s", P("@s", p.Serial));
        return id;
    });

    /// <summary>Cập nhật kết quả; khi phiếu kết thúc thì trả serial về trạng thái "Đã bán".</summary>
    public void UpdateResult(PhieuBaoHanhDTO p) => InTransaction((conn, tran) =>
    {
        ExecuteNonQuery(conn, tran, "UPDATE PhieuBaoHanh SET KetQua = @kq, TrangThai = @tt, NgayTra = @nt WHERE MaPBH = @id",
            P("@kq", p.KetQua), P("@tt", p.TrangThai), P("@nt", p.NgayTra), P("@id", p.MaPBH));
        var serialStatus = p.TrangThai == TrangThaiBaoHanh.DangXuLy ? TrangThaiSerial.DangBaoHanh : TrangThaiSerial.DaBan;
        ExecuteNonQuery(conn, tran, "UPDATE SanPhamSerial SET TrangThai = @st WHERE Serial = @s", P("@st", serialStatus), P("@s", p.Serial));
        return 0;
    });
}
