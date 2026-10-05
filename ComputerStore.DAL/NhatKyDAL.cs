using System.Data;
using MySqlConnector;

namespace ComputerStore.DAL;

public class NhatKyDTO
{
    public int MaNK { get; set; }
    public DateTime ThoiGian { get; set; }
    public string NguoiDung { get; set; } = "";
    public string HanhDong { get; set; } = "";
    public string ChiTiet { get; set; } = "";
}

/// <summary>Nhật ký thao tác quan trọng (đăng nhập, hủy hóa đơn, sao lưu...).</summary>
public class NhatKyDAL
{
    private static bool _ready;
    private static readonly object _lock = new();

    private static void EnsureTable()
    {
        if (_ready) return;
        lock (_lock)
        {
            if (_ready) return;
            DbHelper.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS NhatKy (
                MaNK INT AUTO_INCREMENT PRIMARY KEY,
                ThoiGian DATETIME NOT NULL,
                NguoiDung VARCHAR(50) NOT NULL,
                HanhDong VARCHAR(60) NOT NULL,
                ChiTiet VARCHAR(500) NOT NULL DEFAULT ''
            ) CHARACTER SET utf8mb4");
            _ready = true;
        }
    }

    /// <summary>Ghi nhật ký; không bao giờ làm hỏng nghiệp vụ chính nếu ghi thất bại.</summary>
    public void Ghi(string nguoiDung, string hanhDong, string chiTiet)
    {
        try
        {
            EnsureTable();
            DbHelper.ExecuteNonQuery("INSERT INTO NhatKy(ThoiGian, NguoiDung, HanhDong, ChiTiet) VALUES(@t,@u,@h,@c)",
                DbHelper.P("@t", DateTime.Now), DbHelper.P("@u", nguoiDung),
                DbHelper.P("@h", hanhDong), DbHelper.P("@c", chiTiet.Length > 500 ? chiTiet[..500] : chiTiet));
        }
        catch { /* bỏ qua */ }
    }

    public List<NhatKyDTO> Gan(int top = 300)
    {
        EnsureTable();
        var dt = DbHelper.ExecuteQuery("SELECT * FROM NhatKy ORDER BY MaNK DESC LIMIT @n", DbHelper.P("@n", top));
        return DbHelper.Map(dt, r => new NhatKyDTO
        {
            MaNK = r.Int("MaNK"), ThoiGian = r.Date("ThoiGian"), NguoiDung = r.Str("NguoiDung"),
            HanhDong = r.Str("HanhDong"), ChiTiet = r.Str("ChiTiet")
        });
    }
}
