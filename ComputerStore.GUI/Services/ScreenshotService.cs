using System.Drawing.Imaging;
using ComputerStore.BLL;
using ComputerStore.GUI.Forms;
using ComputerStore.GUI.Pages;

namespace ComputerStore.GUI.Services;

/// <summary>Chụp ảnh các màn hình thật của phần mềm phục vụ báo cáo: ComputerStore.exe --screenshots &lt;thư_mục&gt;.</summary>
public static class ScreenshotService
{
    public static int Run(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var screenshotRole = Environment.GetEnvironmentVariable("COMPUTERSTORE_SCREENSHOT_ROLE")?.Trim();
        var username = screenshotRole?.ToLowerInvariant() switch
        {
            null or "" or "admin" => "admin",
            "nhanvien" or "employee" => "nhanvien",
            _ => throw new InvalidOperationException("COMPUTERSTORE_SCREENSHOT_ROLE chỉ nhận admin hoặc nhanvien.")
        };
        new TaiKhoanBLL().DangNhap(username, "123456");

        var screenArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        var captureSize = ParseCaptureSize(Environment.GetEnvironmentVariable("COMPUTERSTORE_SCREENSHOT_SIZE"));
        using var main = new frmMain
        {
            StartPosition = FormStartPosition.Manual,
            Location = screenArea.Location,
            Size = captureSize ?? screenArea.Size,
            WindowState = captureSize.HasValue ? FormWindowState.Normal : FormWindowState.Maximized
        };
        main.Show();
        Pump(400);
        var captureScreen = Screen.FromControl(main);
        LogCapture(outputDir, $"Capture display={captureScreen.DeviceName}; dpi={main.DeviceDpi}; bounds={captureScreen.Bounds.Width}x{captureScreen.Bounds.Height}; client={main.ClientSize.Width}x{main.ClientSize.Height}");
        foreach (var item in PageRegistry.Items.Where(item => !item.AdminOnly || Session.IsAdmin))
        {
            main.Navigate(item.Key);
            Pump(1200);
            if (item.Key == "banhang" && main.ContentHost.Controls.Count > 0 && main.ContentHost.Controls[0] is ucBanHang bh)
            {
                bh.AddSampleItemsForDemo();
                Pump(400);
            }
            EnsureMainCapture(main, item.Key, outputDir);
            using var bmp = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(bmp, new Rectangle(0, 0, main.Width, main.Height));
            var path = Path.Combine(outputDir, item.Key + ".png");
            bmp.Save(path, ImageFormat.Png);
            LogCapture(outputDir, $"Saved {path}; window={main.WindowState}; size={main.Size.Width}x{main.Size.Height}; client={main.ClientSize.Width}x{main.ClientSize.Height}; dpi={main.DeviceDpi}");

            if (item.Key == "nhanvien" && Session.IsAdmin && main.ContentHost.Controls.Count > 0)
            {
                var accountTab = main.ContentHost.Controls.Find("tabsNhanVien", true).OfType<TabControl>().FirstOrDefault();
                if (accountTab != null && accountTab.TabPages.Count > 1)
                {
                    accountTab.SelectedIndex = 1;
                    Pump(300);
                    SaveMainScreenshot(main, outputDir, "nhanvien_taikhoan.png");
                    accountTab.SelectedIndex = 0;
                }
            }

            if (item.Key == "banhang" && main.ContentHost.Controls.Count > 0)
            {
                var toggle = main.ContentHost.Controls[0].Controls.Find("btnToggleView", true).OfType<IButtonControl>().FirstOrDefault();
                if (toggle != null)
                {
                    toggle.PerformClick();
                    Pump(500);
                    using var cardView = new Bitmap(main.Width, main.Height);
                    main.DrawToBitmap(cardView, new Rectangle(0, 0, main.Width, main.Height));
                    var cardsPath = Path.Combine(outputDir, "banhang_cards.png");
                    cardView.Save(cardsPath, ImageFormat.Png);
                    LogCapture(outputDir, $"Saved {cardsPath}; window={main.WindowState}; size={main.Size.Width}x{main.Size.Height}; client={main.ClientSize.Width}x{main.ClientSize.Height}; dpi={main.DeviceDpi}");
                }
            }
        }

        // Chụp dialog Nhập Serial phục vụ kiểm tra
        var sampleSp = new ComputerStore.DTO.SanPhamDTO { MaSP = 16, TenSP = "WD Blue SN580 1TB NVMe" };
        using (var fSerial = new frmNhapSerial(sampleSp, 12, new List<string>()))
        {
            fSerial.StartPosition = FormStartPosition.CenterScreen;
            fSerial.Show();
            Pump(300);
            using var bmpSerial = new Bitmap(fSerial.Width, fSerial.Height);
            fSerial.DrawToBitmap(bmpSerial, new Rectangle(0, 0, fSerial.Width, fSerial.Height));
            var pathSerial = Path.Combine(outputDir, "nhap_serial_dialog.png");
            bmpSerial.Save(pathSerial, ImageFormat.Png);
            Console.WriteLine("Saved " + pathSerial);
            fSerial.Close();
        }

        var inStockProduct = new SanPhamBLL().GetById(1);
        if (inStockProduct != null && inStockProduct.SoLuongTon > 0)
        {
            using var fChooseSerial = new frmChonSerial(inStockProduct, 1)
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent
            };
            fChooseSerial.Show(main);
            Pump(300);
            SaveFormScreenshot(fChooseSerial, outputDir, "chon_serial_dialog.png");
            fChooseSerial.Close();
        }

        var sampleInvoice = new HoaDonBLL()
            .Search(DateTime.Today.AddYears(-2), DateTime.Today, string.Empty)
            .FirstOrDefault();
        if (sampleInvoice != null)
        {
            using var fInvoiceDetails = new frmChiTietHoaDon(sampleInvoice)
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent
            };
            fInvoiceDetails.Show(main);
            Pump(300);
            SaveFormScreenshot(fInvoiceDetails, outputDir, "hoadon_chitiet.png");
            fInvoiceDetails.Close();

            sampleInvoice.GhiChu = string.Join(" ", Enumerable.Repeat(
                "Ghi chú dài để kiểm tra thanh cuộn trong chi tiết hóa đơn.", 8));
            using var fLongNotes = new frmChiTietHoaDon(sampleInvoice)
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent
            };
            fLongNotes.Show(main);
            Pump(300);
            SaveFormScreenshot(fLongNotes, outputDir, "hoadon_chitiet_ghichu_dai.png");
            fLongNotes.Close();
        }

        // This form holds deliberate demo drafts (for the POS screenshot). Dispose it
        // directly so headless capture does not ask an interactive discard question.
        return 0;
    }

    private static void Pump(int ms)
    {
        var end = DateTime.Now.AddMilliseconds(ms);
        while (DateTime.Now < end) { Application.DoEvents(); Thread.Sleep(20); }
    }

    private static void SaveMainScreenshot(Form form, string outputDir, string fileName)
    {
        EnsureMainCapture(form, fileName, outputDir);
        SaveFormScreenshot(form, outputDir, fileName);
    }

    private static void EnsureMainCapture(Form form, string page, string outputDir)
    {
        if (form.WindowState == FormWindowState.Minimized)
        {
            form.WindowState = FormWindowState.Normal;
            form.Show();
            form.BringToFront();
            form.PerformLayout();
            Pump(250);
        }

        if (!form.Visible || form.Width < 800 || form.Height < 600 ||
            form.ClientSize.Width < 760 || form.ClientSize.Height < 560)
            throw new InvalidOperationException(
                $"Không thể chụp trang '{page}': cửa sổ đang ẩn/thu nhỏ hoặc quá nhỏ ({form.WindowState}, {form.Size.Width}x{form.Size.Height}, client {form.ClientSize.Width}x{form.ClientSize.Height}).");

        LogCapture(outputDir, $"Capture page={page}; window={form.WindowState}; visible={form.Visible}; size={form.Size.Width}x{form.Size.Height}; client={form.ClientSize.Width}x{form.ClientSize.Height}; dpi={form.DeviceDpi}");
    }

    private static void SaveFormScreenshot(Form form, string outputDir, string fileName)
    {
        using var bmp = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        var path = Path.Combine(outputDir, fileName);
        bmp.Save(path, ImageFormat.Png);
        LogCapture(outputDir, $"Saved {path}; formDpi={form.DeviceDpi}; size={form.ClientSize.Width}x{form.ClientSize.Height}");
    }

    private static void LogCapture(string outputDir, string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}";
        File.AppendAllText(Path.Combine(outputDir, "capture.log"), line + Environment.NewLine, System.Text.Encoding.UTF8);
        Console.WriteLine(line);
    }

    private static Size? ParseCaptureSize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split('x', 'X');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height)
            || width < 1100 || height < 680)
            throw new ArgumentException("COMPUTERSTORE_SCREENSHOT_SIZE phải có dạng W×H, tối thiểu 1100×680.");
        return new Size(width, height);
    }
}
