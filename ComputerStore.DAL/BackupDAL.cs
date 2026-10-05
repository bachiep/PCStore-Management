using System.Globalization;
using System.Text;
using MySqlConnector;

namespace ComputerStore.DAL;

/// <summary>Sao lưu / khôi phục dữ liệu (toàn bộ bảng) ra file .sql, mỗi câu lệnh một dòng.</summary>
public class BackupDAL
{
    private const string Header = "-- ComputerStore backup";

    public int Backup(string path)
    {
        using var conn = DbHelper.OpenConnection();
        var tables = new List<string>();
        using (var cmd = new MySqlCommand("SHOW FULL TABLES WHERE Table_type = 'BASE TABLE'", conn))
        using (var rd = cmd.ExecuteReader())
            while (rd.Read()) tables.Add(rd.GetString(0));

        int rows = 0;
        using var w = new StreamWriter(path, false, new UTF8Encoding(false));
        w.WriteLine(Header + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        foreach (var t in tables)
        {
            w.WriteLine($"DELETE FROM `{t}`;");
            using var cmd = new MySqlCommand($"SELECT * FROM `{t}`", conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var vals = new string[rd.FieldCount];
                for (int i = 0; i < vals.Length; i++) vals[i] = Literal(rd.GetValue(i));
                w.WriteLine($"INSERT INTO `{t}` VALUES ({string.Join(",", vals)});");
                rows++;
            }
        }
        return rows;
    }

    /// <summary>Khôi phục từ file sao lưu; toàn bộ chạy trong một transaction (lỗi sẽ rollback).</summary>
    public int Restore(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length == 0 || !lines[0].StartsWith(Header))
            throw new InvalidDataException("File không phải bản sao lưu hợp lệ của hệ thống.");

        using var conn = DbHelper.OpenConnection();
        Exec(conn, null, "SET FOREIGN_KEY_CHECKS=0");
        try
        {
            using var tran = conn.BeginTransaction();
            int n = 0;
            try
            {
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("--")) continue;
                    Exec(conn, tran, line);
                    n++;
                }
                tran.Commit();
            }
            catch { tran.Rollback(); throw; }
            return n;
        }
        finally { Exec(conn, null, "SET FOREIGN_KEY_CHECKS=1"); }
    }

    private static void Exec(MySqlConnection c, MySqlTransaction? t, string sql)
    {
        using var cmd = new MySqlCommand(sql, c, t);
        cmd.ExecuteNonQuery();
    }

    private static string Literal(object v) => v switch
    {
        DBNull => "NULL",
        bool b => b ? "1" : "0",
        byte[] bytes => bytes.Length == 0 ? "''" : "0x" + Convert.ToHexString(bytes),
        DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture) + "'",
        TimeSpan ts => "'" + ts.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) + "'",
        string s => Quote(s),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => Quote(v.ToString() ?? "")
    };

    private static string Quote(string s)
    {
        var sb = new StringBuilder("'");
        foreach (var ch in s)
            sb.Append(ch switch
            {
                '\\' => "\\\\", '\'' => "\\'", '\n' => "\\n", '\r' => "\\r", '\0' => "\\0", '\u001a' => "\\Z",
                _ => ch.ToString()
            });
        return sb.Append('\'').ToString();
    }
}
