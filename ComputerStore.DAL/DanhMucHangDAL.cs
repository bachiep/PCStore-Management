using ComputerStore.DTO;
using static ComputerStore.DAL.DbHelper;

namespace ComputerStore.DAL;

public class DanhMucDAL
{
    public List<DanhMucDTO> GetAll() => Map(
        ExecuteQuery(@"SELECT dm.MaDM, dm.TenDM, COUNT(sp.MaSP) AS SoSanPham
                       FROM DanhMuc dm LEFT JOIN SanPham sp ON sp.MaDM = dm.MaDM
                       GROUP BY dm.MaDM, dm.TenDM ORDER BY dm.MaDM"),
        r => new DanhMucDTO { MaDM = r.Int("MaDM"), TenDM = r.Str("TenDM"), SoSanPham = r.Int("SoSanPham") });

    public bool NameExists(string ten, int excludeId)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM DanhMuc WHERE TenDM=@t AND MaDM<>@id",
            P("@t", ten), P("@id", excludeId))) > 0;

    public void Insert(DanhMucDTO d) => ExecuteNonQuery("INSERT INTO DanhMuc(TenDM) VALUES(@t)", P("@t", d.TenDM));
    public void Update(DanhMucDTO d) => ExecuteNonQuery("UPDATE DanhMuc SET TenDM=@t WHERE MaDM=@id", P("@t", d.TenDM), P("@id", d.MaDM));
    public void Delete(int id) => ExecuteNonQuery("DELETE FROM DanhMuc WHERE MaDM=@id", P("@id", id));
}

public class HangDAL
{
    public List<HangDTO> GetAll() => Map(
        ExecuteQuery(@"SELECT h.MaHang, h.TenHang, h.QuocGia, COUNT(sp.MaSP) AS SoSanPham
                       FROM Hang h LEFT JOIN SanPham sp ON sp.MaHang = h.MaHang
                       GROUP BY h.MaHang, h.TenHang, h.QuocGia ORDER BY h.MaHang"),
        r => new HangDTO { MaHang = r.Int("MaHang"), TenHang = r.Str("TenHang"), QuocGia = r.StrN("QuocGia"), SoSanPham = r.Int("SoSanPham") });

    public bool NameExists(string ten, int excludeId)
        => Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Hang WHERE TenHang=@t AND MaHang<>@id",
            P("@t", ten), P("@id", excludeId))) > 0;

    public void Insert(HangDTO h) => ExecuteNonQuery("INSERT INTO Hang(TenHang, QuocGia) VALUES(@t, @q)", P("@t", h.TenHang), P("@q", h.QuocGia));
    public void Update(HangDTO h) => ExecuteNonQuery("UPDATE Hang SET TenHang=@t, QuocGia=@q WHERE MaHang=@id",
        P("@t", h.TenHang), P("@q", h.QuocGia), P("@id", h.MaHang));
    public void Delete(int id) => ExecuteNonQuery("DELETE FROM Hang WHERE MaHang=@id", P("@id", id));
}
