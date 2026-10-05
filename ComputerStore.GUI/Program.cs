using ComputerStore.BLL;
using ComputerStore.DAL;
using ComputerStore.GUI.Forms;
using Microsoft.Extensions.Configuration;

namespace ComputerStore.GUI;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => Msg.Error("Lỗi không mong muốn: " + e.Exception.Message);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            System.IO.File.WriteAllText("crash_log.txt", e.ExceptionObject.ToString());
            MessageBox.Show("Crash fatal: " + e.ExceptionObject.ToString());
        };

        LoadConfiguration();
        if (!EnsureDatabase()) return 1;

        // Chế độ chụp ảnh màn hình tự động phục vụ báo cáo: ComputerStore.exe --screenshots <thư_mục>
        var shot = Array.IndexOf(args, "--screenshots");
        if (shot >= 0 && shot + 1 < args.Length)
            return Services.ScreenshotService.Run(args[shot + 1]);

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
        new ComputerStore.BLL.TaiKhoanBLL().DangNhap("admin", "123456");
        try
        {
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

    private static bool EnsureDatabase()
    {
        while (true)
        {
            if (DbHelper.TestConnection(out var error)) return true;
            var r = MessageBox.Show(
                "Không kết nối được tới cơ sở dữ liệu MySQL.\r\n\r\n" + error +
                "\r\n\r\nCách khắc phục:\r\n1. Ứng dụng sẽ tự động thử chạy tools\\start_mysql.ps1 nếu bạn ấn Retry.\r\n" +
                "2. Chạy tools\\load_database.ps1 bằng tay nếu chưa tạo CSDL.\r\n3. Hoặc sửa chuỗi kết nối trong appsettings.json.",
                "Lỗi kết nối cơ sở dữ liệu", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            if (r != DialogResult.Retry) return false;

            // Tự động chạy script bật DB
            try
            {
                var toolsDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tools"));
                var script = System.IO.Path.Combine(toolsDir, "start_mysql.ps1");
                if (System.IO.File.Exists(script))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"",
                        UseShellExecute = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    })?.WaitForExit(3000);
                }
            }
            catch { }
        }
    }
}
