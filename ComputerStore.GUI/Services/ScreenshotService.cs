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

        using var main = new frmMain { StartPosition = FormStartPosition.Manual, Location = new Point(0, 0), Size = new Size(1366, 800) };
        main.Show();
        Pump(400);
        foreach (var item in PageRegistry.Items)
        {
            main.Navigate(item.Key);
            Pump(1200);
            using var bmp = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(bmp, new Rectangle(0, 0, main.Width, main.Height));
            var path = Path.Combine(outputDir, item.Key + ".png");
            bmp.Save(path, ImageFormat.Png);
            Console.WriteLine("Saved " + path);
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
