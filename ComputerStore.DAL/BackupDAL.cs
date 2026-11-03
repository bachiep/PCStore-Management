using System.Globalization;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace ComputerStore.DAL;

/// <summary>Sao lưu dữ liệu thành định dạng SQL giới hạn do ứng dụng tạo và chỉ khôi phục DML an toàn.</summary>
public class BackupDAL
{
    private const string Header = "-- ComputerStore backup";
    private static readonly Regex HeaderPattern = new(
        @"\A-- ComputerStore backup \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\z", RegexOptions.CultureInvariant);
    private static readonly Regex SourceDatabasePattern = new(
        @"\A-- Source database \(base64\): (?<value>[A-Za-z0-9+/=]+)\z", RegexOptions.CultureInvariant);
    private static readonly Regex DeletePattern = new(
        @"\ADELETE FROM `(?<table>[A-Za-z_][A-Za-z0-9_]*)`;\z", RegexOptions.CultureInvariant);
    private static readonly Regex InsertPattern = new(
        @"\AINSERT INTO `(?<table>[A-Za-z_][A-Za-z0-9_]*)` VALUES \((?<values>.*)\);\z", RegexOptions.CultureInvariant);

    private sealed class RestoreTable(string name)
    {
        public string Name { get; } = name;
        public List<object?[]> Rows { get; } = new();
    }

    private sealed record ForeignKeyColumn(string ChildColumn, string ParentColumn);
    private sealed record ForeignKey(string ChildTable, string ParentSchema, string ParentTable,
        IReadOnlyList<ForeignKeyColumn> Columns);

    public int Backup(string path)
    {
        using var conn = DbHelper.OpenConnection();
        var tables = GetBaseTables(conn);
        var targetPath = Path.GetFullPath(path);
        var temporaryPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            int rows = 0;
            using (var tran = conn.BeginTransaction(IsolationLevel.RepeatableRead))
            using (var w = new StreamWriter(temporaryPath, false, new UTF8Encoding(false)))
            {
                w.WriteLine(Header + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                w.WriteLine("-- Source database (base64): " + Convert.ToBase64String(Encoding.UTF8.GetBytes(conn.Database)));
                foreach (var t in tables)
                {
                    w.WriteLine($"DELETE FROM `{t}`;");
                    using var cmd = new MySqlCommand($"SELECT * FROM `{t}`", conn, tran);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        var vals = new string[rd.FieldCount];
                        for (int i = 0; i < vals.Length; i++) vals[i] = Literal(rd.GetValue(i));
                        w.WriteLine($"INSERT INTO `{t}` VALUES ({string.Join(",", vals)});");
                        rows++;
                    }
                }
                w.Flush();
                tran.Commit();
            }

            File.Move(temporaryPath, targetPath, true);
            return rows;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    /// <summary>Reads the source database from new-format backups; older files have no source metadata.</summary>
    public string? ReadSourceDatabase(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var header = reader.ReadLine();
        if (header is null || !HeaderPattern.IsMatch(header))
            throw new InvalidDataException("File không phải bản sao lưu hợp lệ của hệ thống.");

        var metadata = reader.ReadLine();
        if (metadata is null) return null;
        var match = SourceDatabasePattern.Match(metadata);
        if (!match.Success)
        {
            if (metadata.StartsWith("-- Source database", StringComparison.Ordinal))
                throw new InvalidDataException("Thông tin CSDL nguồn trong file sao lưu không hợp lệ.");
            return null;
        }

        try { return Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups["value"].Value)); }
        catch (FormatException) { throw new InvalidDataException("Thông tin CSDL nguồn trong file sao lưu không hợp lệ."); }
    }

    /// <summary>Khôi phục từ file của ứng dụng; chỉ chấp nhận DELETE/INSERT hợp lệ và chạy trong một transaction.</summary>
    public int Restore(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length == 0 || !HeaderPattern.IsMatch(lines[0]))
            throw new InvalidDataException("File không phải bản sao lưu hợp lệ của hệ thống.");

        using var conn = DbHelper.OpenConnection();
        var tables = GetBaseTables(conn);
        var allowedTables = new HashSet<string>(tables, StringComparer.Ordinal);
        var columnCounts = tables.ToDictionary(t => t, t => GetColumnCount(conn, t), StringComparer.Ordinal);
        var parsedTables = ParseBackup(lines, allowedTables, columnCounts);
        var foreignKeys = GetForeignKeys(conn);

        Exec(conn, null, "SET FOREIGN_KEY_CHECKS=0");
        try
        {
            using var tran = conn.BeginTransaction();
            int statements = 0;
            try
            {
                foreach (var table in parsedTables)
                {
                    Exec(conn, tran, $"DELETE FROM `{table.Name}`");
                    statements++;
                    foreach (var row in table.Rows)
                    {
                        InsertRow(conn, tran, table.Name, row);
                        statements++;
                    }
                }

                ValidateForeignKeys(conn, tran, foreignKeys);
                tran.Commit();
            }
            catch
            {
                try { tran.Rollback(); } catch { /* Preserve the original restore error. */ }
                throw;
            }
            return statements;
        }
        finally { Exec(conn, null, "SET FOREIGN_KEY_CHECKS=1"); }
    }

    private static List<string> GetBaseTables(MySqlConnection conn)
    {
        var tables = new List<string>();
        using var cmd = new MySqlCommand("SHOW FULL TABLES WHERE Table_type = 'BASE TABLE'", conn);
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            var table = rd.GetString(0);
            if (!Regex.IsMatch(table, @"\A[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.CultureInvariant))
                throw new InvalidDataException("Tên bảng hiện tại không được hỗ trợ định dạng sao lưu.");
            tables.Add(table);
        }
        return tables;
    }

    private static int GetColumnCount(MySqlConnection conn, string table)
    {
        using var cmd = new MySqlCommand($"SELECT * FROM `{table}` LIMIT 0", conn);
        using var reader = cmd.ExecuteReader();
        return reader.FieldCount;
    }

    private static void ValidateForeignKeys(MySqlConnection conn, MySqlTransaction tran,
        IReadOnlyList<ForeignKey> foreignKeys)
    {
        foreach (var foreignKey in foreignKeys)
        {
            var childTable = QuoteIdentifier(foreignKey.ChildTable);
            var childNotNull = string.Join(" AND ", foreignKey.Columns.Select(c =>
                $"child.{QuoteIdentifier(c.ChildColumn)} IS NOT NULL"));
            var referencesMatch = string.Join(" AND ", foreignKey.Columns.Select(c =>
                $"parent.{QuoteIdentifier(c.ParentColumn)} = child.{QuoteIdentifier(c.ChildColumn)}"));
            var sql = $"SELECT 1 FROM {QuoteIdentifier(conn.Database)}.{childTable} child " +
                      $"WHERE {childNotNull} AND NOT EXISTS (SELECT 1 FROM " +
                      $"{QuoteIdentifier(foreignKey.ParentSchema)}.{QuoteIdentifier(foreignKey.ParentTable)} parent " +
                      $"WHERE {referencesMatch}) LIMIT 1";

            using var command = new MySqlCommand(sql, conn, tran);
            if (command.ExecuteScalar() != null)
                throw InvalidBackup($"Dữ liệu khôi phục vi phạm khóa ngoại tại bảng {foreignKey.ChildTable}.");
        }
    }

    private static IReadOnlyList<ForeignKey> GetForeignKeys(MySqlConnection conn)
    {
        const string sql = """
            SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME,
                   REFERENCED_TABLE_SCHEMA, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = @schema AND REFERENCED_TABLE_NAME IS NOT NULL
            ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION
            """;

        var grouped = new Dictionary<(string ChildTable, string Constraint, string ParentSchema, string ParentTable),
            List<ForeignKeyColumn>>();
        using var command = new MySqlCommand(sql, conn);
        command.Parameters.AddWithValue("@schema", conn.Database);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var key = (reader.GetString(0), reader.GetString(1), reader.GetString(3), reader.GetString(4));
            if (!grouped.TryGetValue(key, out var columns))
            {
                columns = new List<ForeignKeyColumn>();
                grouped.Add(key, columns);
            }
            columns.Add(new ForeignKeyColumn(reader.GetString(2), reader.GetString(5)));
        }

        return grouped.Select(item => new ForeignKey(item.Key.ChildTable, item.Key.ParentSchema,
            item.Key.ParentTable, item.Value)).ToArray();
    }

    private static string QuoteIdentifier(string identifier) => $"`{identifier.Replace("`", "``")}`";

    private static List<RestoreTable> ParseBackup(string[] lines, HashSet<string> allowedTables,
        Dictionary<string, int> columnCounts)
    {
        var result = new List<RestoreTable>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        RestoreTable? current = null;

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (SourceDatabasePattern.IsMatch(line)) continue;
            var delete = DeletePattern.Match(line);
            if (delete.Success)
            {
                var tableName = delete.Groups["table"].Value;
                if (!allowedTables.Contains(tableName) || !seen.Add(tableName))
                    throw InvalidBackup("Bảng trong bản sao lưu không hợp lệ hoặc bị lặp.");
                current = new RestoreTable(tableName);
                result.Add(current);
                continue;
            }

            var insert = InsertPattern.Match(line);
            if (insert.Success)
            {
                var tableName = insert.Groups["table"].Value;
                if (current == null || !string.Equals(current.Name, tableName, StringComparison.Ordinal))
                    throw InvalidBackup("Dữ liệu INSERT không thuộc bảng đang được khôi phục.");
                var values = ParseValues(insert.Groups["values"].Value);
                if (values.Count != columnCounts[tableName])
                    throw InvalidBackup($"Số cột trong bản sao lưu của bảng {tableName} không khớp schema hiện tại.");
                current.Rows.Add(values.ToArray());
                continue;
            }

            throw InvalidBackup("File chứa câu lệnh không được phép. Chỉ chấp nhận bản sao lưu do ứng dụng tạo.");
        }

        if (seen.Count != allowedTables.Count || !seen.SetEquals(allowedTables))
            throw InvalidBackup("Bản sao lưu không có đầy đủ các bảng của schema hiện tại.");
        return result;
    }

    private static List<object?> ParseValues(string source)
    {
        var values = new List<object?>();
        var i = 0;
        while (true)
        {
            SkipWhitespace(source, ref i);
            if (i >= source.Length) throw InvalidBackup("Danh sách giá trị trong INSERT không hợp lệ.");

            object? value;
            if (source[i] == '\'')
            {
                value = ParseQuotedString(source, ref i);
            }
            else if (source.AsSpan(i).StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                i += 2;
                var start = i;
                while (i < source.Length && Uri.IsHexDigit(source[i])) i++;
                var hex = source[start..i];
                if (hex.Length % 2 != 0) throw InvalidBackup("Giá trị nhị phân trong INSERT không hợp lệ.");
                try { value = Convert.FromHexString(hex); }
                catch (FormatException) { throw InvalidBackup("Giá trị nhị phân trong INSERT không hợp lệ."); }
            }
            else
            {
                var start = i;
                while (i < source.Length && source[i] != ',') i++;
                var token = source[start..i].Trim();
                if (token == "NULL") value = DBNull.Value;
                else if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) value = number;
                else throw InvalidBackup("Giá trị số trong INSERT không hợp lệ.");
            }
            values.Add(value);

            SkipWhitespace(source, ref i);
            if (i == source.Length) return values;
            if (source[i] != ',') throw InvalidBackup("Danh sách giá trị trong INSERT không hợp lệ.");
            i++;
        }
    }

    private static string ParseQuotedString(string source, ref int i)
    {
        var value = new StringBuilder();
        i++; // opening quote
        while (i < source.Length)
        {
            var ch = source[i++];
            if (ch == '\'') return value.ToString();
            if (ch != '\\')
            {
                value.Append(ch);
                continue;
            }

            if (i >= source.Length) throw InvalidBackup("Chuỗi trong INSERT chưa kết thúc.");
            value.Append(source[i++] switch
            {
                '\\' => '\\',
                '\'' => '\'',
                'n' => '\n',
                'r' => '\r',
                '0' => '\0',
                'Z' => '\u001a',
                _ => throw InvalidBackup("Chuỗi trong INSERT có ký tự escape không hợp lệ.")
            });
        }
        throw InvalidBackup("Chuỗi trong INSERT chưa kết thúc.");
    }

    private static void SkipWhitespace(string value, ref int index)
    {
        while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
    }

    private static void InsertRow(MySqlConnection conn, MySqlTransaction tran, string table, object?[] values)
    {
        var parameters = string.Join(",", Enumerable.Range(0, values.Length).Select(i => $"@p{i}"));
        using var cmd = new MySqlCommand($"INSERT INTO `{table}` VALUES ({parameters})", conn, tran);
        for (var i = 0; i < values.Length; i++) cmd.Parameters.AddWithValue($"@p{i}", values[i] ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static InvalidDataException InvalidBackup(string message) => new(message);

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
