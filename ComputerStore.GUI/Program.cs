using ComputerStore.BLL;
using ComputerStore.DAL;
using ComputerStore.GUI.Forms;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Text.RegularExpressions;

namespace ComputerStore.GUI;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        var headlessStartup = args.Contains("--test-pages") || args.Contains("--screenshots");
        ApplicationConfiguration.Initialize();
        Msg.NonInteractive = headlessStartup;
        Audit.LogWriteFailed += () =>
        {
            const string message = "Thao tác đã hoàn tất, nhưng nhật ký chưa được lưu. Hãy kiểm tra kết nối cơ sở dữ liệu.";
            if (headlessStartup) Console.Error.WriteLine(message);
            else MessageBox.Show(message, "Không lưu được nhật ký", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        Application.ThreadException += (_, e) =>
        {
            var message = "Lỗi không mong muốn: " + e.Exception.Message;
            if (headlessStartup)
            {
                Console.Error.WriteLine(message);
                Environment.ExitCode = 1;
                Application.ExitThread();
            }
            else Msg.Error(message);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            var message = "Crash fatal: " + e.ExceptionObject;
            if (headlessStartup) Console.Error.WriteLine(message);
            else
            {
                System.IO.File.WriteAllText("crash_log.txt", message);
                MessageBox.Show(message);
            }
        };

        LoadConfiguration();
        if (headlessStartup && !IsIsolatedTestDatabase(DbHelper.ConnectionString))
        {
            Console.Error.WriteLine("Chế độ --test-pages/--screenshots chỉ chạy với database ComputerStoreTest* hoặc ComputerStoreAudit*. Hãy cấu hình COMPUTERSTORE_CONNECTION tới database kiểm thử riêng.");
            return 2;
        }
        if (!EnsureDatabase(headlessStartup)) return 1;

        // Chế độ chụp ảnh màn hình tự động phục vụ báo cáo: ComputerStore.exe --screenshots <thư_mục>
        var shot = Array.IndexOf(args, "--screenshots");
        if (shot >= 0 && shot + 1 < args.Length)
        {
            try { return Services.ScreenshotService.Run(args[shot + 1]); }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Screenshot capture failed: " + ex);
                return 1;
            }
        }

        if (args.Contains("--test-pages"))
        {
            return RunAutomatedTests();
        }

        while (true)
        {
            using (var login = new frmDangNhap())
                if (login.ShowDialog() != DialogResult.OK) return 0;

            using var main = new frmMain();
            Application.Run(main);
            if (!main.LogoutRequested) return 0;
        }
    }

    private static int RunAutomatedTests()
    {
        Console.WriteLine("Starting automated page tests...");
        try
        {
            new ComputerStore.BLL.TaiKhoanBLL().DangNhap("admin", "123456");
            var dummyForm = new Form();
            foreach (var item in ComputerStore.GUI.Pages.PageRegistry.Items)
            {
                Console.WriteLine($"Testing: {item.Key}...");
                var page = item.Create(null!);
                dummyForm.Controls.Add(page);
                page.LoadData();
                dummyForm.Controls.Clear();
                page.Dispose();
                Console.WriteLine($"[OK] {item.Key}");
            }
            Console.WriteLine("All pages passed!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRASH: {ex}");
            return 1;
        }
    }

    private static void LoadConfiguration()
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        var cs = Environment.GetEnvironmentVariable("COMPUTERSTORE_CONNECTION") ?? cfg["ConnectionStrings:Default"];
        if (!string.IsNullOrWhiteSpace(cs)) DbHelper.ConnectionString = cs;
    }

    private static bool IsIsolatedTestDatabase(string connectionString)
    {
        try
        {
            var database = new MySqlConnectionStringBuilder(connectionString).Database;
            return Regex.IsMatch(database, @"\AComputerStore(?:Test|Audit)(?:_|\z)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool EnsureDatabase(bool headlessStartup)
    {
        var localServerStartRequested = false;
        while (true)
        {
            try
            {
                var created = DatabaseBootstrapper.EnsureDatabase(DbHelper.ConnectionString, FindDatabaseScripts());
                if (created)
                {
                    const string message = "Không tìm thấy cơ sở dữ liệu. Đã tạo lại schema và dữ liệu mẫu.";
                    if (headlessStartup) Console.WriteLine(message);
                    else MessageBox.Show(message, "Khởi tạo cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return true;
            }
            catch (Exception ex)
            {
                if (headlessStartup)
                {
                    Console.Error.WriteLine("Database startup failed: " + ex.Message);
                    return false;
                }

                var r = MessageBox.Show(
                    "Chưa thể mở cơ sở dữ liệu MySQL. Ứng dụng đã tự thử kết nối lại; nếu cơ sở dữ liệu bị xóa, ứng dụng sẽ tạo lại schema và dữ liệu mẫu khi kết nối được.\r\n\r\n" + ex.Message +
                    "\r\n\r\nChọn Retry để thử lại và khởi động MySQL cục bộ nếu cần. Chọn Cancel để đóng ứng dụng.",
                    "Lỗi kết nối cơ sở dữ liệu", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                if (r != DialogResult.Retry) return false;

                if (!localServerStartRequested)
                {
                    localServerStartRequested = true;
                    try
                    {
                        var packagedScript = System.IO.Path.Combine(AppContext.BaseDirectory, "tools", "start_mysql.ps1");
                        var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
                        var script = System.IO.File.Exists(packagedScript)
                            ? packagedScript
                            : System.IO.Path.Combine(root, "tools", "start_mysql.ps1");
                        if (System.IO.File.Exists(script))
                        {
                            var startProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "powershell.exe",
                                Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"",
                                UseShellExecute = true,
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                            });
                            startProcess?.WaitForExit(3000);
                        }
                    }
                    catch { /* The next retry displays the concrete connection error. */ }
                }
            }
        }
    }

    private static string FindDatabaseScripts()
    {
        var output = System.IO.Path.Combine(AppContext.BaseDirectory, "Database");
        if (System.IO.File.Exists(System.IO.Path.Combine(output, "01_CreateDatabase.sql"))) return output;

        var source = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Database"));
        if (System.IO.File.Exists(System.IO.Path.Combine(source, "01_CreateDatabase.sql"))) return source;
        throw new System.IO.DirectoryNotFoundException("Không tìm thấy các tệp SQL cần để khởi tạo cơ sở dữ liệu.");
    }
}
