using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Net;
using MySqlConnector;

namespace ComputerStore.DAL;

/// <summary>Creates the demo database only when it is absent; never resets an existing database.</summary>
public static class DatabaseBootstrapper
{
    private const string LockName = "ComputerStore.DatabaseBootstrap";
    private const int RequiredSchemaVersion = 7;
    private static readonly string[] RequiredTables =
    {
        "NhanVien", "TaiKhoan", "DanhMuc", "Hang", "SanPham", "NhaCungCap", "PhieuNhap",
        "ChiTietPhieuNhap", "KhachHang", "HoaDon", "ChiTietHoaDon", "SanPhamSerial",
        "LichSuBanSerial", "PhieuBaoHanh", "AppSchemaVersion"
    };
    private static readonly (string Table, string Column)[] RequiredColumns =
    {
        ("HoaDon", "HinhThucTT"), ("HoaDon", "DaHuy"), ("HoaDon", "LyDoHuy"), ("HoaDon", "NgayHuy"),
        ("ChiTietHoaDon", "TenSPKhiBan"), ("ChiTietHoaDon", "ThoiGianBHKhiBan"),
        ("PhieuBaoHanh", "MaHDLucTiepNhan")
    };
    private static readonly string[] RequiredProcedures =
    {
        "sp_DoanhThuTheoNgay", "sp_DoanhThuTheoThang", "sp_TopSanPhamBanChay", "sp_SanPhamSapHet"
    };
    private sealed record ProcedureParameter(string Mode, string Name, string DataType);
    private sealed record ProcedureDefinition(IReadOnlyList<ProcedureParameter> Parameters, string Body);

    /// <returns>true when the database had to be created from the bundled SQL scripts.</returns>
    public static bool EnsureDatabase(string connectionString, string scriptsDirectory, int serverAttempts = 6)
    {
        var target = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(target.Database))
            throw new InvalidOperationException("Chuỗi kết nối chưa chỉ định tên cơ sở dữ liệu.");
        if (serverAttempts < 1) throw new ArgumentOutOfRangeException(nameof(serverAttempts));

        var database = target.Database;
        var requiredProcedureDefinitions = ReadRequiredProcedureDefinitions(scriptsDirectory);
        var server = new MySqlConnectionStringBuilder(connectionString) { Database = "" };
        using var serverConnection = OpenWithRetry(server.ConnectionString, serverAttempts);
        using var acquire = new MySqlCommand("SELECT GET_LOCK(@name, 15)", serverConnection);
        var lockName = LockName + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database.ToLowerInvariant())))[..24];
        acquire.Parameters.AddWithValue("@name", lockName);
        if (Convert.ToInt32(acquire.ExecuteScalar()) != 1)
            throw new InvalidOperationException("Không thể khóa tiến trình khởi tạo cơ sở dữ liệu.");

        try
        {
            using var exists = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME=@database", serverConnection);
            exists.Parameters.AddWithValue("@database", database);
            if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
            {
                ValidateExistingDatabase(serverConnection, database, requiredProcedureDefinitions);
                return false;
            }

            if (!IsBootstrapHostAllowed(server.Server,
                    Environment.GetEnvironmentVariable("COMPUTERSTORE_ALLOW_REMOTE_BOOTSTRAP")))
                throw new InvalidOperationException(
                    $"Từ chối tự tạo và nạp dữ liệu mẫu vào database '{database}' trên MySQL từ xa ({server.Server}). " +
                    "Chỉ bật COMPUTERSTORE_ALLOW_REMOTE_BOOTSTRAP=true khi bạn chủ động khởi tạo máy chủ đó.");

            try
            {
                using var create = new MySqlCommand(
                    $"CREATE DATABASE `{database.Replace("`", "``")}` CHARACTER SET utf8mb4 COLLATE utf8mb4_vietnamese_ci",
                    serverConnection);
                create.ExecuteNonQuery();
            }
            catch (MySqlException ex) when (ex.Number == 1007)
            {
                // A different actor created the schema after our existence check.
                // Do not seed or delete a database we did not create.
                throw new InvalidOperationException(
                    $"Cơ sở dữ liệu '{database}' vừa được tạo bởi tiến trình khác. Hãy kiểm tra schema rồi khởi động lại ứng dụng.", ex);
            }

            target.Pooling = false;
            using var databaseConnection = OpenWithRetry(target.ConnectionString, serverAttempts);
            foreach (var file in new[] { "01_CreateDatabase.sql", "02_StoredProcedures.sql", "03_SeedData.sql", "06_SeedDemoHistory.sql" })
            {
                var path = Path.Combine(scriptsDirectory, file);
                if (!File.Exists(path)) throw new FileNotFoundException("Thiếu tệp khởi tạo CSDL.", path);
                ExecuteScript(databaseConnection, File.ReadAllText(path, Encoding.UTF8));
            }
            return true;
        }
        finally
        {
            try
            {
                using var release = new MySqlCommand("SELECT RELEASE_LOCK(@name)", serverConnection);
                release.Parameters.AddWithValue("@name", lockName);
                release.ExecuteScalar();
            }
            catch { /* Closing the connection also releases the named lock. */ }
        }
    }

    private static void ValidateExistingDatabase(MySqlConnection connection, string database,
        IReadOnlyDictionary<string, ProcedureDefinition> requiredProcedureDefinitions)
    {
        using var command = new MySqlCommand(
            "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA=@database AND TABLE_TYPE='BASE TABLE'",
            connection);
        command.Parameters.AddWithValue("@database", database);
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = command.ExecuteReader())
            while (reader.Read()) existing.Add(reader.GetString(0));

        using var columnCommand = new MySqlCommand(
            "SELECT TABLE_NAME, COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=@database",
            connection);
        columnCommand.Parameters.AddWithValue("@database", database);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = columnCommand.ExecuteReader())
            while (reader.Read()) columns.Add($"{reader.GetString(0)}.{reader.GetString(1)}");

        using var routineCommand = new MySqlCommand(
            "SELECT ROUTINE_NAME, ROUTINE_DEFINITION FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA=@database AND ROUTINE_TYPE='PROCEDURE'",
            connection);
        routineCommand.Parameters.AddWithValue("@database", database);
        var routines = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = routineCommand.ExecuteReader())
            while (reader.Read()) routines[reader.GetString(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);

        var parameters = new Dictionary<string, List<ProcedureParameter>>(StringComparer.OrdinalIgnoreCase);
        using (var parameterCommand = new MySqlCommand(
            "SELECT SPECIFIC_NAME, PARAMETER_MODE, PARAMETER_NAME, DTD_IDENTIFIER " +
            "FROM information_schema.PARAMETERS WHERE SPECIFIC_SCHEMA=@database ORDER BY SPECIFIC_NAME, ORDINAL_POSITION",
            connection))
        {
            parameterCommand.Parameters.AddWithValue("@database", database);
            using var reader = parameterCommand.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (!requiredProcedureDefinitions.ContainsKey(name)) continue;
                if (!parameters.TryGetValue(name, out var list)) parameters[name] = list = new();
                list.Add(new ProcedureParameter(
                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? "" : reader.GetString(3)));
            }
        }

        int? schemaVersion = null;
        if (existing.Contains("AppSchemaVersion"))
        {
            using var versionCommand = new MySqlCommand(
                $"SELECT SchemaVersion FROM `{database.Replace("`", "``")}`.AppSchemaVersion WHERE Id=1", connection);
            var value = versionCommand.ExecuteScalar();
            if (value != null && value != DBNull.Value) schemaVersion = Convert.ToInt32(value);
        }

        var missingTables = RequiredTables.Where(table => !existing.Contains(table)).ToArray();
        var missingColumns = RequiredColumns
            .Where(column => !columns.Contains($"{column.Table}.{column.Column}"))
            .Select(column => $"{column.Table}.{column.Column}")
            .ToArray();
        var missingProcedures = RequiredProcedures.Where(procedure => !routines.ContainsKey(procedure)).ToArray();
        var mismatchedProcedures = RequiredProcedures
            .Where(name => routines.TryGetValue(name, out var body)
                && !ProcedureMatches(requiredProcedureDefinitions[name], body,
                    parameters.TryGetValue(name, out var actualParameters) ? actualParameters : Array.Empty<ProcedureParameter>()))
            .ToArray();
        if (missingTables.Length > 0 || missingColumns.Length > 0 || missingProcedures.Length > 0
            || mismatchedProcedures.Length > 0 || schemaVersion != RequiredSchemaVersion)
        {
            var details = new List<string>();
            if (missingTables.Length > 0) details.Add($"thiếu bảng: {string.Join(", ", missingTables)}");
            if (missingColumns.Length > 0) details.Add($"thiếu cột: {string.Join(", ", missingColumns)}");
            if (missingProcedures.Length > 0) details.Add($"thiếu thủ tục: {string.Join(", ", missingProcedures)}");
            if (mismatchedProcedures.Length > 0)
                details.Add($"thủ tục sai định nghĩa hoặc tham số: {string.Join(", ", mismatchedProcedures)}");
            if (schemaVersion != RequiredSchemaVersion)
                details.Add($"phiên bản schema {(schemaVersion?.ToString() ?? "chưa xác định")}, yêu cầu {RequiredSchemaVersion}");
            throw new InvalidOperationException(
                $"Cơ sở dữ liệu '{database}' chưa đúng phiên bản yêu cầu ({string.Join("; ", details)}). " +
                "Ứng dụng không tự sửa hoặc xóa CSDL hiện có. Hãy sao lưu, xác định phiên bản schema và chỉ chạy script nâng cấp phù hợp; không chạy script cài mới trên dữ liệu đang dùng.");
        }
    }

    private static IReadOnlyDictionary<string, ProcedureDefinition> ReadRequiredProcedureDefinitions(string scriptsDirectory)
    {
        var path = Path.Combine(scriptsDirectory, "07_Migration_NetRevenue.sql");
        if (!File.Exists(path)) throw new FileNotFoundException("Thiếu script chuẩn các thủ tục schema phiên bản 7.", path);
        var script = File.ReadAllText(path, Encoding.UTF8);
        var result = new Dictionary<string, ProcedureDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in RequiredProcedures)
        {
            var pattern = $@"CREATE\s+PROCEDURE\s+{Regex.Escape(name)}\s*\((?<parameters>[^)]*)\)\s*(?<body>BEGIN\b.*?END)\s*\$\$";
            var match = Regex.Match(script, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
            if (!match.Success)
                throw new InvalidDataException($"Không đọc được định nghĩa chuẩn của thủ tục {name} trong script schema phiên bản 7.");

            var parsedParameters = new List<ProcedureParameter>();
            var rawParameters = match.Groups["parameters"].Value.Trim();
            if (rawParameters.Length > 0)
            {
                foreach (var parameter in rawParameters.Split(','))
                {
                    var parameterMatch = Regex.Match(parameter.Trim(),
                        @"\A(?<mode>IN|OUT|INOUT)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s+(?<type>[A-Za-z]+(?:\s*\(\s*\d+(?:\s*,\s*\d+)?\s*\))?)\z",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (!parameterMatch.Success)
                        throw new InvalidDataException($"Không đọc được tham số chuẩn của thủ tục {name} trong script schema phiên bản 7.");
                    parsedParameters.Add(new ProcedureParameter(
                        NormalizeSql(parameterMatch.Groups["mode"].Value),
                        NormalizeSql(parameterMatch.Groups["name"].Value),
                        NormalizeSql(parameterMatch.Groups["type"].Value)));
                }
            }

            result.Add(name, new ProcedureDefinition(parsedParameters, NormalizeProcedureBody(match.Groups["body"].Value)));
        }
        return result;
    }

    private static bool ProcedureMatches(ProcedureDefinition expected, string actualBody,
        IReadOnlyList<ProcedureParameter> actualParameters)
    {
        if (!string.Equals(expected.Body, NormalizeProcedureBody(actualBody), StringComparison.Ordinal)) return false;
        if (expected.Parameters.Count != actualParameters.Count) return false;
        for (var i = 0; i < expected.Parameters.Count; i++)
        {
            var expectedParameter = expected.Parameters[i];
            var actualParameter = actualParameters[i];
            if (!string.Equals(expectedParameter.Mode, NormalizeSql(actualParameter.Mode), StringComparison.Ordinal)
                || !string.Equals(expectedParameter.Name, NormalizeSql(actualParameter.Name), StringComparison.Ordinal)
                || !string.Equals(expectedParameter.DataType, NormalizeSql(actualParameter.DataType), StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static string NormalizeSql(string sql)
        => Regex.Replace(sql.Trim(), @"\s+", " ", RegexOptions.CultureInvariant).ToUpperInvariant();

    private static string NormalizeProcedureBody(string body)
        => Regex.Replace(body.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);

    private static MySqlConnection OpenWithRetry(string connectionString, int attempts)
    {
        for (var attempt = 1; ; attempt++)
        {
            var connection = new MySqlConnection(connectionString);
            try
            {
                connection.Open();
                return connection;
            }
            catch (MySqlException ex) when (attempt < attempts && IsTransient(ex))
            {
                connection.Dispose();
                Thread.Sleep(Math.Min(250 * attempt, 1000));
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }
    }

    private static bool IsTransient(MySqlException ex)
        => ex.IsTransient || ex.Number is 0 or 1042 or 2002 or 2003 or 2006 or 2013;

    internal static bool IsBootstrapHostAllowed(string host, string? allowRemote)
    {
        var normalized = host.Trim().Trim('[', ']');
        var loopback = normalized.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                       (IPAddress.TryParse(normalized, out var address) && IPAddress.IsLoopback(address));
        return loopback || string.Equals(allowRemote, "true", StringComparison.OrdinalIgnoreCase);
    }

    internal static void ExecuteScript(MySqlConnection connection, string script)
    {
        var delimiter = ";";
        var statement = new StringBuilder();
        foreach (var line in script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = trimmed[10..].Trim();
                if (delimiter.Length == 0) throw new InvalidOperationException("Khai báo DELIMITER không hợp lệ.");
                continue;
            }

            statement.AppendLine(line);
            if (!trimmed.EndsWith(delimiter, StringComparison.Ordinal)) continue;

            var sql = statement.ToString();
            statement.Clear();
            sql = sql.Trim();
            sql = sql[..^delimiter.Length].Trim();
            if (sql.Length == 0) continue;

            var executable = RemoveLeadingComments(sql);
            if (executable.StartsWith("USE ", StringComparison.OrdinalIgnoreCase) ||
                executable.StartsWith("DROP DATABASE ", StringComparison.OrdinalIgnoreCase) ||
                executable.StartsWith("CREATE DATABASE ", StringComparison.OrdinalIgnoreCase))
                continue;

            using var command = new MySqlCommand(sql, connection) { CommandTimeout = 180 };
            command.ExecuteNonQuery();
        }

        if (!string.IsNullOrWhiteSpace(statement.ToString()))
            throw new InvalidOperationException("Tệp SQL có câu lệnh chưa kết thúc bằng delimiter.");
    }

    private static string RemoveLeadingComments(string sql)
        => Regex.Replace(sql, @"\A(?:\s|--[^\r\n]*|/\*.*?\*/)*", "", RegexOptions.Singleline);
}
