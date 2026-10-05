using System.Data;
using MySqlConnector;

namespace ComputerStore.DAL;

/// <summary>
/// Lớp tiện ích truy cập CSDL MySQL bằng ADO.NET (MySqlConnector).
/// Mọi câu lệnh đều dùng tham số (MySqlParameter) để chống SQL Injection.
/// </summary>
public static class DbHelper
{
    public static string ConnectionString { get; set; } =
        "Server=127.0.0.1;Port=3307;Database=QLCuaHangLinhKien;User ID=root;Password=;CharSet=utf8mb4;" +
        "AllowUserVariables=True;ConnectionTimeout=10";

    public static MySqlConnection OpenConnection()
    {
        var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public static bool TestConnection(out string error)
    {
        try
        {
            using var conn = OpenConnection();
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static MySqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);

    private static MySqlCommand BuildCommand(string sql, MySqlConnection conn, MySqlTransaction? tran,
        CommandType type, MySqlParameter[] ps)
    {
        var cmd = new MySqlCommand(sql, conn, tran) { CommandType = type };
        if (ps.Length > 0) cmd.Parameters.AddRange(ps);
        return cmd;
    }

    public static DataTable ExecuteQuery(string sql, params MySqlParameter[] ps)
        => ExecuteQuery(sql, CommandType.Text, ps);

    public static DataTable ExecuteProcedure(string procName, params MySqlParameter[] ps)
        => ExecuteQuery(procName, CommandType.StoredProcedure, ps);

    private static DataTable ExecuteQuery(string sql, CommandType type, MySqlParameter[] ps)
    {
        using var conn = OpenConnection();
        using var cmd = BuildCommand(sql, conn, null, type, ps);
        using var da = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    public static int ExecuteNonQuery(string sql, params MySqlParameter[] ps)
    {
        using var conn = OpenConnection();
        using var cmd = BuildCommand(sql, conn, null, CommandType.Text, ps);
        return cmd.ExecuteNonQuery();
    }

    public static object? ExecuteScalar(string sql, params MySqlParameter[] ps)
    {
        using var conn = OpenConnection();
        using var cmd = BuildCommand(sql, conn, null, CommandType.Text, ps);
        var r = cmd.ExecuteScalar();
        return r == DBNull.Value ? null : r;
    }

    /// <summary>Thực thi INSERT và trả về giá trị khóa AUTO_INCREMENT vừa sinh.</summary>
    public static int ExecuteInsert(string sql, params MySqlParameter[] ps)
    {
        using var conn = OpenConnection();
        using var cmd = BuildCommand(sql, conn, null, CommandType.Text, ps);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    // ----- Phiên bản dùng trong transaction -----
    public static int ExecuteNonQuery(MySqlConnection conn, MySqlTransaction tran, string sql, params MySqlParameter[] ps)
    {
        using var cmd = BuildCommand(sql, conn, tran, CommandType.Text, ps);
        return cmd.ExecuteNonQuery();
    }

    public static object? ExecuteScalar(MySqlConnection conn, MySqlTransaction tran, string sql, params MySqlParameter[] ps)
    {
        using var cmd = BuildCommand(sql, conn, tran, CommandType.Text, ps);
        var r = cmd.ExecuteScalar();
        return r == DBNull.Value ? null : r;
    }

    public static int ExecuteInsert(MySqlConnection conn, MySqlTransaction tran, string sql, params MySqlParameter[] ps)
    {
        using var cmd = BuildCommand(sql, conn, tran, CommandType.Text, ps);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    /// <summary>Chạy một khối lệnh trong transaction; lỗi sẽ tự rollback và ném lại exception.</summary>
    public static T InTransaction<T>(Func<MySqlConnection, MySqlTransaction, T> work)
    {
        using var conn = OpenConnection();
        using var tran = conn.BeginTransaction();
        try
        {
            var result = work(conn, tran);
            tran.Commit();
            return result;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    /// <summary>Ánh xạ DataTable sang danh sách đối tượng.</summary>
    public static List<T> Map<T>(DataTable dt, Func<DataRow, T> map)
    {
        var list = new List<T>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows) list.Add(map(r));
        return list;
    }
}

/// <summary>Extension đọc giá trị an toàn từ DataRow.</summary>
public static class DataRowExt
{
    public static string Str(this DataRow r, string col) => r[col] == DBNull.Value ? "" : r[col].ToString()!;
    public static string? StrN(this DataRow r, string col) => r[col] == DBNull.Value ? null : r[col].ToString();
    public static int Int(this DataRow r, string col) => r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
    public static int? IntN(this DataRow r, string col) => r[col] == DBNull.Value ? null : Convert.ToInt32(r[col]);
    public static decimal Dec(this DataRow r, string col) => r[col] == DBNull.Value ? 0 : Convert.ToDecimal(r[col]);
    public static bool Bool(this DataRow r, string col) => r[col] != DBNull.Value && Convert.ToBoolean(r[col]);
    public static DateTime Date(this DataRow r, string col) => Convert.ToDateTime(r[col]);
    public static DateTime? DateN(this DataRow r, string col) => r[col] == DBNull.Value ? null : Convert.ToDateTime(r[col]);
}
