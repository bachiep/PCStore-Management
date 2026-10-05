namespace ComputerStore.GUI.Services;

/// <summary>Đọc ảnh sản phẩm từ thư mục Images cạnh file chạy (không khóa file).</summary>
public static class ProductImage
{
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "Images");

    public static Bitmap? Load(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        var path = Path.Combine(Folder, fileName);
        if (!File.Exists(path)) return null;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var img = Image.FromStream(fs);
            return new Bitmap(img);
        }
        catch { return null; }
    }
}
