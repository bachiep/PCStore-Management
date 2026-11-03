using ComputerStore.BLL;
using ComputerStore.DAL;
using ComputerStore.DTO;
using MySqlConnector;
using System.Text.RegularExpressions;

namespace ComputerStore.Tests;

/// <summary>
/// Kiểm thử tích hợp với MySQL thật. Phải cấu hình COMPUTERSTORE_TEST_CONNECTION
/// trỏ tới CSDL riêng biệt dành cho test; thiếu kết nối sẽ làm test thất bại.
/// Các test chạy tuần tự trong cùng một collection vì dùng chung Session và CSDL.
/// </summary>
[Collection("Database")]
public class IntegrationTests
{
    private static bool DbUp()
    {
        var connectionString = Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connectionString),
            "Hãy đặt COMPUTERSTORE_TEST_CONNECTION tới database MySQL riêng cho kiểm thử; không dùng dữ liệu đang vận hành.");
        var database = new MySqlConnectionStringBuilder(connectionString!).Database;
        Assert.True(IsIsolatedTestDatabase(database),
            "Từ chối chạy test ghi dữ liệu vào database không có tiền tố ComputerStoreTest hoặc ComputerStoreAudit.");
        DbHelper.ConnectionString = connectionString!;
        var connected = DbHelper.TestConnection(out var error);
        Assert.True(connected, $"Không kết nối được database kiểm thử: {error}");
        return connected;
    }

    private static bool IsIsolatedTestDatabase(string databaseName)
        => Regex.IsMatch(databaseName, @"(?i)^ComputerStore(?:Test|Audit)(?:_|$)");

    [Theory]
    [InlineData("ComputerStoreTest")]
    [InlineData("ComputerStoreTest_Run42")]
    [InlineData("ComputerStoreAudit_20261103")]
    public void TestDatabaseGuard_AcceptsExplicitTestAndAuditNames(string databaseName)
        => Assert.True(IsIsolatedTestDatabase(databaseName));

    [Theory]
    [InlineData("QLCuaHangLinhKien")]
    [InlineData("ComputerStore")]
    [InlineData("ProductionTest")]
    public void TestDatabaseGuard_RejectsNonIsolatedNames(string databaseName)
        => Assert.False(IsIsolatedTestDatabase(databaseName));

    [Theory]
    [InlineData("127.0.0.1", null, true)]
    [InlineData("::1", null, true)]
    [InlineData("localhost", null, true)]
    [InlineData("db.example.com", null, false)]
    [InlineData("db.example.com", "true", true)]
    public void DatabaseBootstrapper_RestrictsRemoteSeedingUnlessOptedIn(string host, string? optIn, bool expected)
        => Assert.Equal(expected, DatabaseBootstrapper.IsBootstrapHostAllowed(host, optIn));

    private static void LoginAdmin() => new TaiKhoanBLL().DangNhap("admin", "123456");

    private static int Stock(int maSP) => new SanPhamBLL().GetById(maSP)!.SoLuongTon;

    private static string Tag => DateTime.Now.ToString("HHmmssfff");

    private static string DatabaseTableCountFingerprint(string databaseName)
    {
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        using var connection = new MySqlConnection(server.ConnectionString);
        connection.Open();
        using var exists = new MySqlCommand("SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME=@db", connection);
        exists.Parameters.AddWithValue("@db", databaseName);
        if (Convert.ToInt32(exists.ExecuteScalar()) == 0) return "<missing>";

        using var tablesCommand = new MySqlCommand(
            "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA=@db AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME", connection);
        tablesCommand.Parameters.AddWithValue("@db", databaseName);
        using var reader = tablesCommand.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read()) tables.Add(reader.GetString(0));
        reader.Close();

        var counts = new List<string>();
        foreach (var table in tables)
        {
            using var count = new MySqlCommand($"SELECT COUNT(*) FROM `{databaseName.Replace("`", "``")}`.`{table.Replace("`", "``")}`", connection);
            counts.Add($"{table}={count.ExecuteScalar()}");
        }
        return string.Join("|", counts);
    }

    [Fact]
    public void Login_Succeeds_WrongPasswordAndInjectionFail()
    {
        if (!DbUp()) return;
        var bll = new TaiKhoanBLL();
        var u = bll.DangNhap("admin", "123456");
        Assert.Equal(VaiTroConst.Admin, u.VaiTro);
        Assert.True(Session.IsAdmin);

        Assert.Throws<BusinessException>(() => bll.DangNhap("admin", "sai-mat-khau"));
        Assert.Throws<BusinessException>(() => bll.DangNhap("' OR 1=1 --", "x"));
        Assert.Throws<BusinessException>(() => bll.DangNhap("admin' --", "x"));

        var nv = bll.DangNhap("nhanvien", "123456");
        Assert.Equal(VaiTroConst.NhanVien, nv.VaiTro);
        Assert.False(Session.IsAdmin);
        Assert.Throws<BusinessException>(() => new NhaCungCapBLL().Save(new NhaCungCapDTO { TenNCC = "X" }));
    }

    [Fact]
    public void DatabaseBootstrapper_RecreatesOnlyAMissingDatabase()
    {
        if (!DbUp()) return;
        var legacyDatabaseFingerprint = DatabaseTableCountFingerprint("QLCuaHangLinhKien");
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        var databaseName = "ComputerStoreBootstrap_" + Guid.NewGuid().ToString("N")[..12];
        var target = new MySqlConnectionStringBuilder(server.ConnectionString) { Database = databaseName, Pooling = false };
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));

        try
        {
            Assert.True(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            Assert.Equal(legacyDatabaseFingerprint, DatabaseTableCountFingerprint("QLCuaHangLinhKien"));
            DbHelper.ConnectionString = target.ConnectionString;
            Assert.Equal(32, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM SanPham")));
            Assert.True(Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM LichSuBanSerial")) > 0);
            Assert.Equal(72, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'")));
            Assert.Equal(0, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM ChiTietHoaDon WHERE TenSPKhiBan IS NULL OR ThoiGianBHKhiBan IS NULL")));
            Assert.Equal(0, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM HoaDon WHERE NgayLap > NOW()")));
            var months = DbHelper.ExecuteQuery(@"SELECT DATE_FORMAT(NgayLap, '%Y-%m') AS Thang, COUNT(*) AS SoHoaDon
                                                  FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'
                                                  GROUP BY DATE_FORMAT(NgayLap, '%Y-%m') ORDER BY Thang");
            Assert.Equal(12, months.Rows.Count);
            Assert.All(months.Rows.Cast<System.Data.DataRow>(), row => Assert.Equal(6, Convert.ToInt32(row["SoHoaDon"])));
            var distinctDemoDays = Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(DISTINCT DATE(NgayLap)) FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'"));
            Assert.True(distinctDemoDays >= 65);
            Assert.False(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            Assert.Equal(legacyDatabaseFingerprint, DatabaseTableCountFingerprint("QLCuaHangLinhKien"));
            Assert.Equal(32, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM SanPham")));
            Assert.Equal(72, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'")));

            using (var connection = new MySqlConnection(server.ConnectionString))
            {
                connection.Open();
                using var delete = new MySqlCommand($"DROP DATABASE `{databaseName}`", connection);
                delete.ExecuteNonQuery();
            }
            Assert.True(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            Assert.Equal(legacyDatabaseFingerprint, DatabaseTableCountFingerprint("QLCuaHangLinhKien"));
            DbHelper.ConnectionString = target.ConnectionString;
            Assert.Equal(32, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM SanPham")));
            Assert.Equal(72, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'")));
        }
        finally
        {
            using var connection = new MySqlConnection(server.ConnectionString);
            connection.Open();
            using var cleanup = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", connection);
            cleanup.ExecuteNonQuery();
            DbHelper.ConnectionString = Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!;
        }
    }

    [Fact]
    public void LegacyMigrations_04_05_07_AreIdempotentAndPreserveExistingRows()
    {
        if (!DbUp()) return;
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        var databaseName = "ComputerStoreMigrate_" + Guid.NewGuid().ToString("N")[..12];
        var target = new MySqlConnectionStringBuilder(server.ConnectionString) { Database = databaseName, Pooling = false };
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));
        var originalConnection = DbHelper.ConnectionString;

        try
        {
            Assert.True(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            DbHelper.ConnectionString = target.ConnectionString;
            var invoiceCount = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM HoaDon"));
            var detailCount = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM ChiTietHoaDon"));
            var soldSerialCount = Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM SanPhamSerial WHERE MaHD IS NOT NULL"));

            DbHelper.ExecuteNonQuery("ALTER TABLE PhieuBaoHanh DROP FOREIGN KEY FK_PBH_HD");
            DbHelper.ExecuteNonQuery("ALTER TABLE HoaDon DROP COLUMN HinhThucTT, DROP COLUMN DaHuy, DROP COLUMN LyDoHuy, DROP COLUMN NgayHuy");
            DbHelper.ExecuteNonQuery("ALTER TABLE ChiTietHoaDon DROP COLUMN TenSPKhiBan, DROP COLUMN ThoiGianBHKhiBan");
            DbHelper.ExecuteNonQuery("ALTER TABLE PhieuBaoHanh DROP COLUMN MaHDLucTiepNhan");
            DbHelper.ExecuteNonQuery("DROP TABLE LichSuBanSerial");
            DbHelper.ExecuteNonQuery("UPDATE AppSchemaVersion SET SchemaVersion=3 WHERE Id=1");

            using (var connection = new MySqlConnection(target.ConnectionString))
            {
                connection.Open();
                foreach (var migration in new[] { "04_Migration_HoaDon.sql", "05_Migration_LichSuBanSerial.sql", "07_Migration_NetRevenue.sql" })
                {
                    var script = File.ReadAllText(Path.Combine(scripts, migration));
                    DatabaseBootstrapper.ExecuteScript(connection, script);
                    DatabaseBootstrapper.ExecuteScript(connection, script);
                }
            }

            Assert.Equal(invoiceCount, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM HoaDon")));
            Assert.Equal(detailCount, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM ChiTietHoaDon")));
            Assert.Equal(soldSerialCount, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM LichSuBanSerial")));
            Assert.Equal(7, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT SchemaVersion FROM AppSchemaVersion WHERE Id=1")));
            Assert.Equal(0, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM ChiTietHoaDon WHERE TenSPKhiBan IS NOT NULL OR ThoiGianBHKhiBan IS NOT NULL")));
        }
        finally
        {
            using var connection = new MySqlConnection(server.ConnectionString);
            connection.Open();
            using var cleanup = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", connection);
            cleanup.ExecuteNonQuery();
            DbHelper.ConnectionString = originalConnection;
        }
    }

    [Fact]
    public void DatabaseBootstrapper_LeavesAnIncompleteExistingDatabaseUntouched()
    {
        if (!DbUp()) return;
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        var databaseName = "ComputerStorePartial_" + Guid.NewGuid().ToString("N")[..12];
        var target = new MySqlConnectionStringBuilder(server.ConnectionString) { Database = databaseName, Pooling = false };
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));

        try
        {
            using (var connection = new MySqlConnection(server.ConnectionString))
            {
                connection.Open();
                using var create = new MySqlCommand($"CREATE DATABASE `{databaseName}`", connection);
                create.ExecuteNonQuery();
            }
            DbHelper.ConnectionString = target.ConnectionString;
            DbHelper.ExecuteNonQuery("CREATE TABLE Sentinel (ValueText VARCHAR(40) NOT NULL)");
            DbHelper.ExecuteNonQuery("INSERT INTO Sentinel(ValueText) VALUES('preserve-me')");

            var error = Assert.Throws<InvalidOperationException>(() =>
                DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            Assert.Contains("chưa đúng phiên bản yêu cầu", error.Message);
            Assert.Contains("thiếu bảng:", error.Message);
            Assert.Equal("preserve-me", Convert.ToString(DbHelper.ExecuteScalar("SELECT ValueText FROM Sentinel")));
        }
        finally
        {
            using var connection = new MySqlConnection(server.ConnectionString);
            connection.Open();
            using var cleanup = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", connection);
            cleanup.ExecuteNonQuery();
            DbHelper.ConnectionString = Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!;
        }
    }

    [Fact]
    public void DatabaseBootstrapper_RejectsMissingRequiredColumnsAndProceduresWithoutDeletingData()
    {
        if (!DbUp()) return;
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        var databaseName = "ComputerStoreSchemaVersion_" + Guid.NewGuid().ToString("N")[..12];
        var target = new MySqlConnectionStringBuilder(server.ConnectionString) { Database = databaseName, Pooling = false };
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));

        try
        {
            Assert.True(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            DbHelper.ConnectionString = target.ConnectionString;
            Assert.Equal(32, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM SanPham")));
            DbHelper.ExecuteNonQuery("ALTER TABLE ChiTietHoaDon DROP COLUMN TenSPKhiBan");
            DbHelper.ExecuteNonQuery("DROP PROCEDURE sp_DoanhThuTheoNgay");

            var error = Assert.Throws<InvalidOperationException>(() =>
                DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            Assert.Contains("ChiTietHoaDon.TenSPKhiBan", error.Message);
            Assert.Contains("sp_DoanhThuTheoNgay", error.Message);
            Assert.Equal(32, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM SanPham")));
            Assert.Equal(72, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'")));
        }
        finally
        {
            using var connection = new MySqlConnection(server.ConnectionString);
            connection.Open();
            using var cleanup = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", connection);
            cleanup.ExecuteNonQuery();
            DbHelper.ConnectionString = Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!;
        }
    }

    [Fact]
    public void DatabaseBootstrapper_RejectsStaleSameNameProcedureWithoutDeletingData()
    {
        if (!DbUp()) return;
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        var databaseName = "ComputerStoreStaleProcedure_" + Guid.NewGuid().ToString("N")[..12];
        var target = new MySqlConnectionStringBuilder(server.ConnectionString) { Database = databaseName, Pooling = false };
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));

        try
        {
            Assert.True(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            DbHelper.ConnectionString = target.ConnectionString;
            Assert.Equal(7, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT SchemaVersion FROM AppSchemaVersion WHERE Id=1")));
            Assert.Equal(32, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM SanPham")));
            DbHelper.ExecuteNonQuery("CREATE TABLE Sentinel (ValueText VARCHAR(40) NOT NULL)");
            DbHelper.ExecuteNonQuery("INSERT INTO Sentinel(ValueText) VALUES('preserve-me')");

            // Keep the routine name and signature but restore the old gross-revenue logic,
            // which does not allocate invoice discounts to each product.
            DbHelper.ExecuteNonQuery("DROP PROCEDURE sp_TopSanPhamBanChay");
            DbHelper.ExecuteNonQuery(@"CREATE PROCEDURE sp_TopSanPhamBanChay(IN TuNgay DATE, IN DenNgay DATE, IN TopN INT)
                BEGIN
                    SELECT sp.MaSP, sp.TenSP, SUM(ct.SoLuong) AS SoLuongBan,
                           SUM(ct.SoLuong * ct.DonGia) AS DoanhThu
                    FROM ChiTietHoaDon ct
                    JOIN HoaDon hd ON hd.MaHD = ct.MaHD
                    JOIN SanPham sp ON sp.MaSP = ct.MaSP
                    WHERE hd.DaHuy = 0 AND DATE(hd.NgayLap) BETWEEN TuNgay AND DenNgay
                    GROUP BY sp.MaSP, sp.TenSP
                    ORDER BY SoLuongBan DESC
                    LIMIT TopN;
                END");

            var error = Assert.Throws<InvalidOperationException>(() =>
                DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));

            Assert.Contains("thủ tục sai định nghĩa hoặc tham số", error.Message);
            Assert.Contains("sp_TopSanPhamBanChay", error.Message);
            Assert.Equal(7, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT SchemaVersion FROM AppSchemaVersion WHERE Id=1")));
            Assert.Equal("preserve-me", Convert.ToString(DbHelper.ExecuteScalar("SELECT ValueText FROM Sentinel")));
            Assert.Equal(32, Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM SanPham")));
            Assert.Equal(72, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'")));
        }
        finally
        {
            using var connection = new MySqlConnection(server.ConnectionString);
            connection.Open();
            using var cleanup = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", connection);
            cleanup.ExecuteNonQuery();
            DbHelper.ConnectionString = Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!;
        }
    }

    [Fact]
    public void DatabaseBootstrapper_RejectsSchemaThatMissedNetRevenueMigrationWithoutDeletingData()
    {
        if (!DbUp()) return;
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        var databaseName = "ComputerStoreStaleMigration_" + Guid.NewGuid().ToString("N")[..12];
        var target = new MySqlConnectionStringBuilder(server.ConnectionString) { Database = databaseName, Pooling = false };
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));

        try
        {
            Assert.True(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            DbHelper.ConnectionString = target.ConnectionString;
            DbHelper.ExecuteNonQuery("UPDATE AppSchemaVersion SET SchemaVersion=6 WHERE Id=1");
            DbHelper.ExecuteNonQuery("CREATE TABLE Sentinel (ValueText VARCHAR(40) NOT NULL)");
            DbHelper.ExecuteNonQuery("INSERT INTO Sentinel(ValueText) VALUES('preserve-me')");

            var error = Assert.Throws<InvalidOperationException>(() =>
                DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));

            Assert.Contains("phiên bản schema 6, yêu cầu 7", error.Message);
            Assert.Equal("preserve-me", Convert.ToString(DbHelper.ExecuteScalar("SELECT ValueText FROM Sentinel")));
            Assert.Equal(72, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%'")));
        }
        finally
        {
            using var connection = new MySqlConnection(server.ConnectionString);
            connection.Open();
            using var cleanup = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", connection);
            cleanup.ExecuteNonQuery();
            DbHelper.ConnectionString = Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!;
        }
    }

    [Fact]
    public void FreshSeededWarrantyTickets_HaveInvoiceLinks_AndOpenTicketsCanBeCompleted()
    {
        if (!DbUp()) return;
        var server = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!)
        { Database = "" };
        var databaseName = "ComputerStoreWarrantySeed_" + Guid.NewGuid().ToString("N")[..12];
        var target = new MySqlConnectionStringBuilder(server.ConnectionString) { Database = databaseName, Pooling = false };
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));

        try
        {
            Assert.True(DatabaseBootstrapper.EnsureDatabase(target.ConnectionString, scripts));
            DbHelper.ConnectionString = target.ConnectionString;
            LoginAdmin();

            var tickets = DbHelper.ExecuteQuery(
                "SELECT MaPBH, Serial, MaKH, MaHDLucTiepNhan, TrangThai FROM PhieuBaoHanh ORDER BY MaPBH");
            Assert.True(tickets.Rows.Count > 0, "CSDL mới phải có phiếu bảo hành dữ liệu mẫu.");
            Assert.All(tickets.Rows.Cast<System.Data.DataRow>(), row =>
                Assert.NotEqual(DBNull.Value, row["MaHDLucTiepNhan"]));

            var legacyTicket = tickets.Rows.Cast<System.Data.DataRow>().First(row => row["MaKH"] is not DBNull);
            var legacyId = legacyTicket.Int("MaPBH");
            var legacySerial = legacyTicket.Str("Serial");
            var originalInvoiceId = legacyTicket.Int("MaHDLucTiepNhan");
            var customer = DbHelper.ExecuteQuery("SELECT HoTen, SDT FROM KhachHang WHERE MaKH=@kh",
                DbHelper.P("@kh", legacyTicket.Int("MaKH"))).Rows[0];
            try
            {
                DbHelper.ExecuteNonQuery("UPDATE PhieuBaoHanh SET MaHDLucTiepNhan=NULL WHERE MaPBH=@id", DbHelper.P("@id", legacyId));
                var dal = new BaoHanhDAL();
                var found = dal.GetBySerial(legacySerial).Single(ticket => ticket.MaPBH == legacyId);
                Assert.Equal(customer.Str("HoTen"), found.TenKH);
                Assert.Contains(dal.Search(customer.Str("SDT"), ""), ticket => ticket.MaPBH == legacyId);
            }
            finally
            {
                DbHelper.ExecuteNonQuery("UPDATE PhieuBaoHanh SET MaHDLucTiepNhan=@hd WHERE MaPBH=@id",
                    DbHelper.P("@hd", originalInvoiceId), DbHelper.P("@id", legacyId));
            }

            var openTickets = tickets.Rows.Cast<System.Data.DataRow>()
                .Where(row => row.Str("TrangThai") == TrangThaiBaoHanh.DangXuLy)
                .ToArray();
            Assert.NotEmpty(openTickets);

            var bll = new BaoHanhBLL();
            foreach (var row in openTickets)
            {
                bll.CapNhatKetQua(new PhieuBaoHanhDTO
                {
                    MaPBH = row.Int("MaPBH"),
                    Serial = row.Str("Serial"),
                    TrangThai = TrangThaiBaoHanh.DangXuLy
                }, TrangThaiBaoHanh.DaTraKhach, "Đã xử lý phiếu khởi tạo để kiểm thử liên kết hóa đơn.");

                Assert.Equal(1, Convert.ToInt32(DbHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM PhieuBaoHanh WHERE MaPBH=@id AND TrangThai=@tt AND MaHDLucTiepNhan=@hd",
                    DbHelper.P("@id", row.Int("MaPBH")), DbHelper.P("@tt", TrangThaiBaoHanh.DaTraKhach),
                    DbHelper.P("@hd", row.Int("MaHDLucTiepNhan")))));
            }
        }
        finally
        {
            using var connection = new MySqlConnection(server.ConnectionString);
            connection.Open();
            using var cleanup = new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", connection);
            cleanup.ExecuteNonQuery();
            DbHelper.ConnectionString = Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!;
        }
    }

    [Fact]
    public void WarrantyMigration_DoesNotGuessInvoiceFromCurrentSerialSale()
    {
        var scripts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));
        var migration = File.ReadAllText(Path.Combine(scripts, "05_Migration_LichSuBanSerial.sql"));
        Assert.DoesNotMatch(
            @"(?is)UPDATE\s+PhieuBaoHanh\s+pbh\s+JOIN\s+SanPhamSerial\s+ss.*?SET\s+pbh\.MaHDLucTiepNhan\s*=\s*ss\.MaHD",
            migration);
    }

    [Fact]
    public void Catalog_Queries_ReturnSeedData()
    {
        if (!DbUp()) return;
        LoginAdmin();
        Assert.True(new SanPhamBLL().Search().Count >= 32);
        Assert.Equal(12, new DanhMucBLL().GetAll().Count);
        Assert.True(new HangBLL().GetAll().Count >= 16);
        Assert.NotEmpty(new SanPhamBLL().Search("RTX"));
        Assert.True(new KhachHangBLL().Search().Count >= 10);
        Assert.True(new NhanVienBLL().Search().Count >= 4);
    }

    [Fact]
    public void Statistics_ProceduresWork()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var tk = new ThongKeBLL();
        var thang = tk.DoanhThuTheoThang(DateTime.Today.Year);
        Assert.Equal(12, thang.Count);
        var tu = DateTime.Today.AddYears(-1);
        Assert.NotEmpty(tk.DoanhThuTheoNgay(tu, DateTime.Today));
        var top = tk.TopBanChay(tu, DateTime.Today, 5);
        Assert.InRange(top.Count, 1, 5);
        Assert.True(top[0].SoLuongBan >= top[^1].SoLuongBan);
        Assert.NotNull(tk.SanPhamSapHet());
        Assert.Equal(7, tk.DoanhThu7Ngay().Count);
        var tq = tk.TongQuan();
        Assert.True(tq.SoKhachHang >= 10);
    }

    [Fact]
    public void NetRevenueByProductAndCategory_ReconcilesToPaidInvoices_AndProfitSubtractsDiscount()
    {
        if (!DbUp()) return;
        LoginAdmin();

        var stats = new ThongKeBLL();
        var from = DateTime.Today.AddYears(-2);
        var to = DateTime.Today;
        var invoiceRevenue = Convert.ToDecimal(DbHelper.ExecuteScalar(
            "SELECT IFNULL(SUM(ThanhToan),0) FROM HoaDon WHERE DaHuy=0 AND DATE(NgayLap) BETWEEN @from AND @to",
            DbHelper.P("@from", from.Date), DbHelper.P("@to", to.Date)));
        var allProducts = stats.TopBanChay(from, to, 10000);
        Assert.Equal(invoiceRevenue, allProducts.Sum(x => x.DoanhThu));

        var lifetimeRevenue = Convert.ToDecimal(DbHelper.ExecuteScalar(
            "SELECT IFNULL(SUM(ThanhToan),0) FROM HoaDon WHERE DaHuy=0"));
        Assert.Equal(lifetimeRevenue, stats.DoanhThuTheoDanhMuc().Sum(x => x.DoanhThu));

        var monthlyProfit = stats.DoanhThuTheoThang(DateTime.Today.Year)
            .Single(x => x.Nhan == "T" + DateTime.Today.Month);
        var expectedProfit = Convert.ToDecimal(DbHelper.ExecuteScalar(@"
            SELECT
                IFNULL((SELECT SUM(ct.SoLuong * (ct.DonGia - sp.GiaNhap))
                        FROM ChiTietHoaDon ct
                        JOIN HoaDon h ON h.MaHD=ct.MaHD
                        JOIN SanPham sp ON sp.MaSP=ct.MaSP
                        WHERE h.DaHuy=0 AND YEAR(h.NgayLap)=@year AND MONTH(h.NgayLap)=@month),0)
                - IFNULL((SELECT SUM(h.GiamGia) FROM HoaDon h
                          WHERE h.DaHuy=0 AND YEAR(h.NgayLap)=@year AND MONTH(h.NgayLap)=@month),0)",
            DbHelper.P("@year", DateTime.Today.Year), DbHelper.P("@month", DateTime.Today.Month)));
        Assert.Equal(expectedProfit, monthlyProfit.LoiNhuan);
    }

    [Fact]
    public void Customer_Crud_And_DuplicatePhone()
    {
        if (!DbUp()) return;
        LoginAdmin();
        var bll = new KhachHangBLL();
        var phone = "09" + (DateTime.Now.Ticks % 100_000_000).ToString("D8");
        var id = bll.Save(new KhachHangDTO { HoTen = "Khách thử nghiệm", SDT = phone });
        Assert.True(id > 0);
        Assert.Throws<BusinessException>(() => bll.Save(new KhachHangDTO { HoTen = "Trùng", SDT = phone }));
        Assert.NotNull(bll.GetByPhone(phone));
        bll.Delete(id);
        Assert.Null(bll.GetByPhone(phone));
    }

    [Fact]
    public void FullFlow_Import_Sell_Rollback_Warranty()
    {
        if (!DbUp()) return;
        LoginAdmin();
        const int maSP = 8; // Kingston Fury 16GB DDR4 (BH 36 tháng)
        var sp = new SanPhamBLL().GetById(maSP)!;
        var ton0 = Stock(maSP);
        var tag = Tag;
        var serials = new[] { $"IT-{tag}-1", $"IT-{tag}-2", $"IT-{tag}-3" };
        var giaNhap0 = sp.GiaNhap;
        var thoiGianBH0 = sp.ThoiGianBH;

        try
        {
            RunFullFlow(maSP, sp, ton0, tag, serials);
        }
        finally
        {
            Cleanup(maSP, ton0, giaNhap0, thoiGianBH0, serials);
        }
    }

    /// <summary>Xóa toàn bộ dữ liệu test tạo ra và trả tồn kho/giá nhập về nguyên trạng.</summary>
    private static void Cleanup(int maSP, int ton0, decimal giaNhap0, int thoiGianBH0, string[] serials)
    {
        var inList = string.Join(",", serials.Select(s => "'" + s + "'"));
        var hd = DbHelper.ExecuteQuery($"SELECT DISTINCT MaHD FROM SanPhamSerial WHERE Serial IN ({inList}) AND MaHD IS NOT NULL")
            .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToInt32(r[0])).ToList();
        var pn = DbHelper.ExecuteQuery($"SELECT DISTINCT MaPN FROM SanPhamSerial WHERE Serial IN ({inList}) AND MaPN IS NOT NULL")
            .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToInt32(r[0])).ToList();

        DbHelper.ExecuteNonQuery($"DELETE FROM PhieuBaoHanh WHERE Serial IN ({inList})");
        DbHelper.ExecuteNonQuery($"DELETE FROM LichSuBanSerial WHERE Serial IN ({inList})");
        DbHelper.ExecuteNonQuery($"DELETE FROM SanPhamSerial WHERE Serial IN ({inList})");
        foreach (var id in hd)
        {
            DbHelper.ExecuteNonQuery("DELETE FROM ChiTietHoaDon WHERE MaHD=@id", DbHelper.P("@id", id));
            DbHelper.ExecuteNonQuery("DELETE FROM HoaDon WHERE MaHD=@id", DbHelper.P("@id", id));
        }
        foreach (var id in pn)
        {
            DbHelper.ExecuteNonQuery("DELETE FROM ChiTietPhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
            DbHelper.ExecuteNonQuery("DELETE FROM PhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
        }
        DbHelper.ExecuteNonQuery("UPDATE SanPham SET SoLuongTon=@t, GiaNhap=@g, ThoiGianBH=@bh WHERE MaSP=@sp",
            DbHelper.P("@t", ton0), DbHelper.P("@g", giaNhap0), DbHelper.P("@bh", thoiGianBH0), DbHelper.P("@sp", maSP));
    }

    private static void RunFullFlow(int maSP, SanPhamDTO sp, int ton0, string tag, string[] serials)
    {

        // 1) Nhập kho 3 serial -> tồn +3
        var pn = new PhieuNhapBLL();
        var maPN = pn.LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 },
            new[] { new ChiTietPhieuNhapDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 3, DonGia = 900_000, Serials = serials.ToList() } });
        Assert.True(maPN > 0);
        Assert.Equal(ton0 + 3, Stock(maSP));
        Assert.Contains(serials[0], new HoaDonBLL().GetSerialsInStock(maSP));
        Assert.Equal(900_000m, new SanPhamBLL().GetById(maSP)!.GiaNhap);

        // nhập trùng serial -> bị từ chối, tồn không đổi
        Assert.Throws<BusinessException>(() => pn.LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 },
            new[] { new ChiTietPhieuNhapDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = 1, Serials = { serials[0] } } }));
        Assert.Equal(ton0 + 3, Stock(maSP));

        // 2) Bán 2 serial -> tồn -2, serial DaBan, HanBH = hôm nay + 36 tháng
        var hdBll = new HoaDonBLL();
        var invoiceCountBeforeStalePrice = hdBll.Search(DateTime.Today, DateTime.Today).Count;
        Assert.Throws<BusinessException>(() => hdBll.LapHoaDon(new HoaDonDTO(),
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = sp.GiaBan + 1, Serials = { serials[2] } } }));
        Assert.Equal(invoiceCountBeforeStalePrice, hdBll.Search(DateTime.Today, DateTime.Today).Count);
        Assert.Equal(ton0 + 3, Stock(maSP));
        Assert.Contains(serials[2], hdBll.GetSerialsInStock(maSP));

        var maHD = hdBll.LapHoaDon(new HoaDonDTO { GiamGia = 50_000 },
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 2, DonGia = sp.GiaBan, ThoiGianBH = sp.ThoiGianBH,
                                           Serials = { serials[0], serials[1] } } });
        Assert.Equal(ton0 + 1, Stock(maSP));
        var hd = hdBll.GetById(maHD)!;
        Assert.Equal(sp.GiaBan * 2, hd.TongTien);
        Assert.Equal(sp.GiaBan * 2 - 50_000, hd.ThanhToan);
        var s0 = new BaoHanhBLL().TraCuu(serials[0])!;
        Assert.Equal(TrangThaiSerial.DaBan, s0.TrangThai);
        Assert.Equal(DateTime.Today.AddMonths(36), s0.HanBH!.Value.Date);
        var saleDetails = hdBll.GetDetails(maHD);
        Assert.Single(saleDetails);
        Assert.Equal(sp.TenSP, saleDetails[0].TenSPKhiBan);
        Assert.Equal(sp.ThoiGianBH, saleDetails[0].ThoiGianBHKhiBan);
        Assert.Equal(2, saleDetails[0].Serials.Count);
        var saleHistory = new BaoHanhBLL().LichSuBan(serials[0]).Single();
        Assert.Equal(maHD, saleHistory.MaHD);
        Assert.False(saleHistory.DaHuy);

        // A legacy line without a stored snapshot must not inherit mutable catalog data.
        DbHelper.ExecuteNonQuery("UPDATE ChiTietHoaDon SET TenSPKhiBan=NULL, ThoiGianBHKhiBan=NULL WHERE MaHD=@hd",
            DbHelper.P("@hd", maHD));
        try
        {
            DbHelper.ExecuteNonQuery("UPDATE SanPham SET TenSP=@ten, ThoiGianBH=1 WHERE MaSP=@sp",
                DbHelper.P("@ten", "Tên mới " + tag), DbHelper.P("@sp", maSP));
            var legacyDetails = hdBll.GetDetails(maHD).Single();
            Assert.Equal("Không có snapshot lịch sử", legacyDetails.TenSP);
            Assert.Null(legacyDetails.ThoiGianBH);
        }
        finally
        {
            DbHelper.ExecuteNonQuery("UPDATE SanPham SET TenSP=@ten, ThoiGianBH=@bh WHERE MaSP=@sp",
                DbHelper.P("@ten", sp.TenSP), DbHelper.P("@bh", sp.ThoiGianBH), DbHelper.P("@sp", maSP));
        }

        // 3) Bán serial đã bán -> lỗi + rollback (không sinh hóa đơn, tồn không đổi)
        var soHD = hdBll.Search(DateTime.Today, DateTime.Today).Count;
        Assert.Throws<BusinessException>(() => hdBll.LapHoaDon(new HoaDonDTO(),
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = sp.GiaBan, ThoiGianBH = 36, Serials = { serials[0] } } }));
        Assert.Equal(ton0 + 1, Stock(maSP));
        Assert.Equal(soHD, hdBll.Search(DateTime.Today, DateTime.Today).Count);

        // 4) Bán vượt tồn -> lỗi, rollback
        var sp2 = new SanPhamBLL().GetById(maSP)!;
        Assert.Throws<BusinessException>(() => hdBll.LapHoaDon(new HoaDonDTO(),
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = sp2.SoLuongTon + 1, DonGia = 1, ThoiGianBH = 36,
                                           Serials = Enumerable.Range(0, sp2.SoLuongTon + 1).Select(i => $"X-{tag}-{i}").ToList() } }));
        Assert.Equal(ton0 + 1, Stock(maSP));

        // 5) Bảo hành: serial chưa bán bị từ chối; serial đã bán lập phiếu được
        var bh = new BaoHanhBLL();
        Assert.Throws<BusinessException>(() => bh.LapPhieu(serials[2], "Lỗi"));
        var maPBH = bh.LapPhieu(serials[0], "Không nhận RAM");
        Assert.Equal(TrangThaiSerial.DangBaoHanh, bh.TraCuu(serials[0])!.TrangThai);
        Assert.Throws<BusinessException>(() => bh.LapPhieu(serials[0], "Lập lần 2"));
        var phieu = bh.LichSu(serials[0]).Single(p => p.MaPBH == maPBH);
        Assert.Equal(maHD, phieu.MaHDLucTiepNhan);
        Assert.Throws<BusinessException>(() => bh.CapNhatKetQua(phieu, TrangThaiBaoHanh.DaTraKhach, ""));
        bh.CapNhatKetQua(phieu, TrangThaiBaoHanh.DaTraKhach, "Đổi sản phẩm mới");
        Assert.Equal(TrangThaiSerial.DaBan, bh.TraCuu(serials[0])!.TrangThai);
        Assert.Throws<BusinessException>(() => bh.CapNhatKetQua(phieu, TrangThaiBaoHanh.TuChoi, "x"));

        // Zero-month warranty means no warranty period, including on the sale date.
        DbHelper.ExecuteNonQuery("UPDATE SanPham SET ThoiGianBH=0 WHERE MaSP=@sp", DbHelper.P("@sp", maSP));
        var noWarrantyInvoice = hdBll.LapHoaDon(new HoaDonDTO(),
            new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = sp.GiaBan, Serials = { serials[2] } } });
        var noWarrantySerial = bh.TraCuu(serials[2])!;
        Assert.Equal(0, hdBll.GetDetails(noWarrantyInvoice).Single().ThoiGianBHKhiBan);
        Assert.Null(noWarrantySerial.HanBH);
        Assert.False(BaoHanhBLL.KiemTra(noWarrantySerial, DateTime.Today).HopLe);
        Assert.Throws<BusinessException>(() => bh.LapPhieu(serials[2], "Không có bảo hành"));
        DbHelper.ExecuteNonQuery("UPDATE SanPham SET ThoiGianBH=@bh WHERE MaSP=@sp",
            DbHelper.P("@bh", sp.ThoiGianBH), DbHelper.P("@sp", maSP));
    }
    [Fact]
    public void CancelInvoice_RestoresStockSerialsAndRevenue_AdminOnly()
    {
        if (!DbUp()) return;
        LoginAdmin();
        const int maSP = 8;
        var sp = new SanPhamBLL().GetById(maSP)!;
        var ton0 = Stock(maSP);
        var tag = Tag;
        var serials = new[] { $"CX-{tag}-1", $"CX-{tag}-2" };
        var giaNhap0 = sp.GiaNhap;
        var hdBll = new HoaDonBLL();
        var khBll = new KhachHangBLL();
        var phone = "09" + (DateTime.Now.Ticks % 100_000_000).ToString("D8");
        var maKH = 0;
        int maHD = 0, maHD2 = 0;

        try
        {
            maKH = khBll.Save(new KhachHangDTO { HoTen = "Khách hủy hóa đơn " + tag, SDT = phone });
            new PhieuNhapBLL().LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 },
                new[] { new ChiTietPhieuNhapDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 2, DonGia = 900_000, Serials = serials.ToList() } });
            maHD = hdBll.LapHoaDon(new HoaDonDTO { MaKH = maKH, HinhThucTT = HinhThucThanhToan.ChuyenKhoan },
                new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 2, DonGia = sp.GiaBan, ThoiGianBH = sp.ThoiGianBH, Serials = serials.ToList() } });
            Assert.Equal(ton0, Stock(maSP));
            Assert.Equal(HinhThucThanhToan.ChuyenKhoan, hdBll.GetById(maHD)!.HinhThucTT);
            Assert.Equal(hdBll.GetById(maHD)!.ThanhToan, khBll.Search(phone).Single().TongChiTieu);
            Assert.Throws<BusinessException>(() => hdBll.LapHoaDon(new HoaDonDTO { HinhThucTT = "Bitcoin" },
                new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = 1, Serials = { serials[0] } } }));

            // nhân viên không được hủy; thiếu lý do bị từ chối
            new TaiKhoanBLL().DangNhap("nhanvien", "123456");
            Assert.Throws<BusinessException>(() => hdBll.HuyHoaDon(maHD, "thử"));
            LoginAdmin();
            Assert.Throws<BusinessException>(() => hdBll.HuyHoaDon(maHD, "  "));

            var dt0 = new ThongKeBLL().TongQuan().DoanhThuThangNay;
            hdBll.HuyHoaDon(maHD, "Khách đổi ý");

            var hd = hdBll.GetById(maHD)!;
            Assert.True(hd.DaHuy);
            Assert.Equal("Khách đổi ý", hd.LyDoHuy);
            var customerHistoryAfterCancel = hdBll.GetByCustomer(maKH);
            var canceledCustomerInvoice = Assert.Single(customerHistoryAfterCancel);
            Assert.Equal(maHD, canceledCustomerInvoice.MaHD);
            Assert.True(canceledCustomerInvoice.DaHuy);
            Assert.Equal("Đã hủy", canceledCustomerInvoice.TrangThaiText);
            Assert.Equal(0m, khBll.Search(phone).Single().TongChiTieu);
            Assert.Equal(ton0 + 2, Stock(maSP));
            Assert.All(serials, s => Assert.Equal(TrangThaiSerial.TrongKho, new BaoHanhBLL().TraCuu(s)!.TrangThai));
            var bh = new BaoHanhBLL();
            var cancelledHistory = bh.LichSuBan(serials[0]).Single();
            Assert.Equal(maHD, cancelledHistory.MaHD);
            Assert.True(cancelledHistory.DaHuy);
            Assert.Equal(dt0 - hd.ThanhToan, new ThongKeBLL().TongQuan().DoanhThuThangNay);
            Assert.Throws<BusinessException>(() => hdBll.HuyHoaDon(maHD, "Hủy lần 2"));

            // Serial can be sold again after cancellation while preserving the first sale.
            maHD2 = hdBll.LapHoaDon(new HoaDonDTO { MaKH = maKH },
                new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = sp.GiaBan, Serials = { serials[0] } } });
            var history = bh.LichSuBan(serials[0]);
            Assert.Equal(2, history.Count);
            Assert.Contains(history, h => h.MaHD == maHD && h.DaHuy);
            Assert.Contains(history, h => h.MaHD == maHD2 && !h.DaHuy);
            var finalCustomerHistory = hdBll.GetByCustomer(maKH);
            Assert.Contains(finalCustomerHistory, h => h.MaHD == maHD && h.DaHuy && h.TrangThaiText == "Đã hủy");
            Assert.Contains(finalCustomerHistory, h => h.MaHD == maHD2 && !h.DaHuy && h.TrangThaiText == "Hoàn thành");
            Assert.Equal(hdBll.GetById(maHD2)!.ThanhToan, khBll.Search(phone).Single().TongChiTieu);
        }
        finally
        {
            var inList = string.Join(",", serials.Select(s => "'" + s + "'"));
            var pn = DbHelper.ExecuteQuery($"SELECT DISTINCT MaPN FROM SanPhamSerial WHERE Serial IN ({inList}) AND MaPN IS NOT NULL")
                .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToInt32(r[0])).ToList();
            DbHelper.ExecuteNonQuery($"DELETE FROM LichSuBanSerial WHERE Serial IN ({inList})");
            DbHelper.ExecuteNonQuery($"DELETE FROM SanPhamSerial WHERE Serial IN ({inList})");
            if (maHD > 0)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietHoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD));
                DbHelper.ExecuteNonQuery("DELETE FROM HoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD));
            }
            if (maHD2 > 0)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietHoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD2));
                DbHelper.ExecuteNonQuery("DELETE FROM HoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD2));
            }
            foreach (var id in pn)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietPhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
                DbHelper.ExecuteNonQuery("DELETE FROM PhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
            }
            if (maKH > 0) khBll.Delete(maKH);
            DbHelper.ExecuteNonQuery("UPDATE SanPham SET SoLuongTon=@t, GiaNhap=@g WHERE MaSP=@sp",
                DbHelper.P("@t", ton0), DbHelper.P("@g", giaNhap0), DbHelper.P("@sp", maSP));
        }
    }

    [Fact]
    public async Task CancelAndWarrantyIntake_ConcurrentOperationsUseConsistentLockOrder()
    {
        if (!DbUp()) return;
        LoginAdmin();
        const int maSP = 8;
        var sp = new SanPhamBLL().GetById(maSP)!;
        var ton0 = Stock(maSP);
        var tag = Tag;
        var serial = $"LK-{tag}-1";
        var giaNhap0 = sp.GiaNhap;
        var maHD = 0;
        var maPN = 0;

        try
        {
            maPN = new PhieuNhapBLL().LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 },
                new[] { new ChiTietPhieuNhapDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = 900_000, Serials = { serial } } });
            maHD = new HoaDonBLL().LapHoaDon(new HoaDonDTO(),
                new[] { new ChiTietHoaDonDTO { MaSP = maSP, TenSP = sp.TenSP, SoLuong = 1, DonGia = sp.GiaBan, Serials = { serial } } });

            using var start = new Barrier(2);
            var cancel = Task.Run(() =>
            {
                start.SignalAndWait();
                try { new HoaDonDAL().Huy(maHD, "Đồng bộ kiểm thử"); return true; }
                catch (InvalidOperationException) { return false; }
            });
            var warranty = Task.Run(() =>
            {
                start.SignalAndWait();
                try
                {
                    new BaoHanhDAL().Create(new PhieuBaoHanhDTO
                    {
                        Serial = serial, MaNV = Session.MaNV, NgayNhan = DateTime.Now,
                        MoTaLoi = "Kiểm tra đồng bộ", TrangThai = TrangThaiBaoHanh.DangXuLy
                    });
                    return true;
                }
                catch (InvalidOperationException) { return false; }
            });

            var results = await Task.WhenAll(cancel, warranty);
            Assert.Single(results.Where(x => x));
            var invoice = new HoaDonBLL().GetById(maHD)!;
            var serialAfter = new BaoHanhBLL().TraCuu(serial)!;
            if (invoice.DaHuy)
            {
                Assert.False(results[1]);
                Assert.Equal(TrangThaiSerial.TrongKho, serialAfter.TrangThai);
            }
            else
            {
                Assert.False(results[0]);
                Assert.Equal(TrangThaiSerial.DangBaoHanh, serialAfter.TrangThai);
            }
        }
        finally
        {
            DbHelper.ExecuteNonQuery("DELETE FROM PhieuBaoHanh WHERE Serial=@s", DbHelper.P("@s", serial));
            DbHelper.ExecuteNonQuery("DELETE FROM LichSuBanSerial WHERE Serial=@s", DbHelper.P("@s", serial));
            DbHelper.ExecuteNonQuery("DELETE FROM SanPhamSerial WHERE Serial=@s", DbHelper.P("@s", serial));
            if (maHD > 0)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietHoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD));
                DbHelper.ExecuteNonQuery("DELETE FROM HoaDon WHERE MaHD=@id", DbHelper.P("@id", maHD));
            }
            if (maPN > 0)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietPhieuNhap WHERE MaPN=@id", DbHelper.P("@id", maPN));
                DbHelper.ExecuteNonQuery("DELETE FROM PhieuNhap WHERE MaPN=@id", DbHelper.P("@id", maPN));
            }
            DbHelper.ExecuteNonQuery("UPDATE SanPham SET SoLuongTon=@t, GiaNhap=@g WHERE MaSP=@sp",
                DbHelper.P("@t", ton0), DbHelper.P("@g", giaNhap0), DbHelper.P("@sp", maSP));
        }
    }

    [Fact]
    public void Backup_Restore_RoundTrip_And_AdminOnly()
    {
        if (!DbUp()) return;
        var path = Path.Combine(Path.GetTempPath(), $"bk_{Tag}.sql");
        var forgedPath = Path.Combine(Path.GetTempPath(), $"bk_forged_{Tag}.sql");
        var invalidForeignKeyPath = Path.Combine(Path.GetTempPath(), $"bk_invalid_fk_{Tag}.sql");
        try
        {
            new TaiKhoanBLL().DangNhap("nhanvien", "123456");
            Assert.Throws<BusinessException>(() => new BackupBLL().Backup(path));

            LoginAdmin();
            var bll = new BackupBLL();
            Assert.True(bll.Backup(path) > 100);
            Assert.Equal(new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COMPUTERSTORE_TEST_CONNECTION")!).Database,
                bll.BackupSourceDatabase(path));
            Assert.Contains(bll.NhatKy(), x => x.HanhDong == "Sao lưu dữ liệu");
            var spBefore = new SanPhamBLL().Search().Count;

            var invalidForeignKeyLines = File.ReadAllLines(path);
            var productRowIndex = Array.FindIndex(invalidForeignKeyLines,
                line => line.StartsWith("INSERT INTO `SanPham` VALUES ", StringComparison.OrdinalIgnoreCase));
            Assert.True(productRowIndex >= 0,
                "Backup phải có ít nhất một sản phẩm. Dòng INSERT mẫu: " +
                string.Join(" | ", invalidForeignKeyLines.Where(line => line.StartsWith("INSERT INTO", StringComparison.Ordinal)).Take(3)));
            var productRow = invalidForeignKeyLines[productRowIndex];
            var categoryMatch = Regex.Match(productRow,
                @"\A(INSERT INTO `SanPham` VALUES [^,]+,'(?:\\.|[^'\\])*',)\d+,", RegexOptions.IgnoreCase);
            Assert.True(categoryMatch.Success, "Không xác định được cột danh mục trong dòng backup sản phẩm: " + productRow);
            invalidForeignKeyLines[productRowIndex] = categoryMatch.Groups[1].Value + "2147483647," + productRow[categoryMatch.Length..];
            File.WriteAllLines(invalidForeignKeyPath, invalidForeignKeyLines);

            var sentinel = "BKSAFE" + Tag;
            DbHelper.ExecuteNonQuery("INSERT INTO NhaCungCap(TenNCC) VALUES(@t)", DbHelper.P("@t", sentinel));
            Assert.Throws<BusinessException>(() => bll.Restore(invalidForeignKeyPath));
            Assert.NotNull(DbHelper.ExecuteScalar("SELECT MaNCC FROM NhaCungCap WHERE TenNCC=@t", DbHelper.P("@t", sentinel)));
            Assert.Equal(spBefore, new SanPhamBLL().Search().Count);

            File.WriteAllText(forgedPath,
                "-- ComputerStore backup 2026-01-01 00:00:00\nDELETE FROM `NhaCungCap`;\nDROP TABLE `SanPham`;\n");
            Assert.Throws<BusinessException>(() => bll.Restore(forgedPath));
            Assert.NotNull(DbHelper.ExecuteScalar("SELECT MaNCC FROM NhaCungCap WHERE TenNCC=@t", DbHelper.P("@t", sentinel)));
            Assert.Equal(spBefore, new SanPhamBLL().Search().Count);

            var ten = "BK" + Tag;
            DbHelper.ExecuteNonQuery("INSERT INTO NhaCungCap(TenNCC) VALUES(@t)", DbHelper.P("@t", ten));
            Assert.NotNull(DbHelper.ExecuteScalar("SELECT MaNCC FROM NhaCungCap WHERE TenNCC=@t", DbHelper.P("@t", ten)));

            bll.Restore(path);
            Assert.Null(DbHelper.ExecuteScalar("SELECT MaNCC FROM NhaCungCap WHERE TenNCC=@t", DbHelper.P("@t", ten)));
            DbHelper.ExecuteNonQuery("ALTER TABLE NhaCungCap AUTO_INCREMENT = 1");
            Assert.Equal(spBefore, new SanPhamBLL().Search().Count);
            Assert.Throws<BusinessException>(() => bll.Restore(path + ".missing"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(forgedPath)) File.Delete(forgedPath);
            if (File.Exists(invalidForeignKeyPath)) File.Delete(invalidForeignKeyPath);
            DbHelper.ExecuteNonQuery("DELETE FROM NhaCungCap WHERE TenNCC LIKE @t", DbHelper.P("@t", "BKSAFE" + Tag + "%"));
        }
    }

    [Fact]
    public async Task ConcurrentReceipts_WithReversedProductOrder_CommitWithoutDeadlock()
    {
        if (!DbUp()) return;
        LoginAdmin();
        const int maSP1 = 8;
        const int maSP2 = 9;
        var sp1 = new SanPhamBLL().GetById(maSP1)!;
        var sp2 = new SanPhamBLL().GetById(maSP2)!;
        var ton1 = Stock(maSP1);
        var ton2 = Stock(maSP2);
        var giaNhap1 = sp1.GiaNhap;
        var giaNhap2 = sp2.GiaNhap;
        var tag = Tag;
        var serials = new[] { $"DR-{tag}-A1", $"DR-{tag}-B1", $"DR-{tag}-A2", $"DR-{tag}-B2" };

        try
        {
            using var start = new Barrier(2);
            var receiptA = Task.Run(() =>
            {
                start.SignalAndWait();
                return new PhieuNhapBLL().LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 }, new[]
                {
                    new ChiTietPhieuNhapDTO { MaSP = maSP1, TenSP = sp1.TenSP, SoLuong = 1, DonGia = 900_000, Serials = { serials[0] } },
                    new ChiTietPhieuNhapDTO { MaSP = maSP2, TenSP = sp2.TenSP, SoLuong = 1, DonGia = 900_000, Serials = { serials[1] } }
                });
            });
            var receiptB = Task.Run(() =>
            {
                start.SignalAndWait();
                return new PhieuNhapBLL().LapPhieuNhap(new PhieuNhapDTO { MaNCC = 1 }, new[]
                {
                    new ChiTietPhieuNhapDTO { MaSP = maSP2, TenSP = sp2.TenSP, SoLuong = 1, DonGia = 900_000, Serials = { serials[2] } },
                    new ChiTietPhieuNhapDTO { MaSP = maSP1, TenSP = sp1.TenSP, SoLuong = 1, DonGia = 900_000, Serials = { serials[3] } }
                });
            });

            var receiptIds = await Task.WhenAll(receiptA, receiptB);
            Assert.All(receiptIds, id => Assert.True(id > 0));
            Assert.Equal(ton1 + 2, Stock(maSP1));
            Assert.Equal(ton2 + 2, Stock(maSP2));
            Assert.Equal(4, Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM SanPhamSerial WHERE Serial IN (@s1,@s2,@s3,@s4)",
                DbHelper.P("@s1", serials[0]), DbHelper.P("@s2", serials[1]),
                DbHelper.P("@s3", serials[2]), DbHelper.P("@s4", serials[3]))));
        }
        finally
        {
            var placeholders = string.Join(",", serials.Select((_, i) => $"@s{i}"));
            var parameters = serials.Select((s, i) => DbHelper.P($"@s{i}", s)).ToArray();
            var receiptIds = DbHelper.ExecuteQuery(
                $"SELECT DISTINCT MaPN FROM SanPhamSerial WHERE Serial IN ({placeholders}) AND MaPN IS NOT NULL", parameters)
                .Rows.Cast<System.Data.DataRow>().Select(r => Convert.ToInt32(r[0])).ToList();
            DbHelper.ExecuteNonQuery($"DELETE FROM SanPhamSerial WHERE Serial IN ({placeholders})", parameters);
            foreach (var id in receiptIds)
            {
                DbHelper.ExecuteNonQuery("DELETE FROM ChiTietPhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
                DbHelper.ExecuteNonQuery("DELETE FROM PhieuNhap WHERE MaPN=@id", DbHelper.P("@id", id));
            }
            DbHelper.ExecuteNonQuery("UPDATE SanPham SET SoLuongTon=@ton, GiaNhap=@gia WHERE MaSP=@sp",
                DbHelper.P("@ton", ton1), DbHelper.P("@gia", giaNhap1), DbHelper.P("@sp", maSP1));
            DbHelper.ExecuteNonQuery("UPDATE SanPham SET SoLuongTon=@ton, GiaNhap=@gia WHERE MaSP=@sp",
                DbHelper.P("@ton", ton2), DbHelper.P("@gia", giaNhap2), DbHelper.P("@sp", maSP2));
        }
    }
}
