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
        new TaiKhoanBLL().DangNhap("admin", "123456");

        var screenArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        using var main = new frmMain { StartPosition = FormStartPosition.Manual, Location = screenArea.Location, Size = screenArea.Size, WindowState = FormWindowState.Maximized };
        main.Show();
        Pump(400);
        foreach (var item in PageRegistry.Items)
        {
            main.Navigate(item.Key);
            Pump(1200);
            if (item.Key == "banhang" && main.ContentHost.Controls.Count > 0 && main.ContentHost.Controls[0] is ucBanHang bh)
            {
                bh.AddSampleItemsForDemo();
                Pump(400);
            }
            using var bmp = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(bmp, new Rectangle(0, 0, main.Width, main.Height));
            var path = Path.Combine(outputDir, item.Key + ".png");
            bmp.Save(path, ImageFormat.Png);
            Console.WriteLine("Saved " + path);
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

        main.Close();
        return 0;
    }

    private static void Pump(int ms)
    {
        var end = DateTime.Now.AddMilliseconds(ms);
        while (DateTime.Now < end) { Application.DoEvents(); Thread.Sleep(20); }
    }
}
