namespace ComputerStore.GUI.Services;

/// <summary>Đọc ảnh sản phẩm từ thư mục Images cạnh file chạy (không khóa file).</summary>
public static class ProductImage
{
    private static readonly HashSet<string> CategoryPhotoFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "product_case.jpg", "product_cooler.jpg", "product_cpu.jpg",
        "product_gpu.jpg", "product_headset.jpg", "product_keyboard.jpg", "product_mainboard.jpg",
        "product_monitor.jpg", "product_mouse.jpg", "product_psu.jpg", "product_ram.jpg", "product_ssd.jpg"
    };

    private static readonly IReadOnlyDictionary<string, string> ReferencePhotoFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["logitech g pro x tkl"] = "ref_logitech_gpro_tkl.jpg",
            ["samsung 990 pro 1tb nvme"] = "ref_samsung_990_pro_4tb.jpg"
        };

    public static string Folder => Path.Combine(AppContext.BaseDirectory, "Images");

    /// <summary>Use a saved custom image, an exact SKU photo, a labeled variant reference, then the category representative.</summary>
    public static string? ResolveFileName(string? savedFileName, string? category, string? productName = null)
    {
        // A photo explicitly selected for this product must always win over the demo SKU map.
        // Otherwise an administrator cannot replace a built-in photo for a matching CPU.
        if (!string.IsNullOrWhiteSpace(savedFileName) && !IsCategoryPhoto(savedFileName))
        {
            var customFileName = Path.GetFileName(savedFileName);
            if (File.Exists(Path.Combine(Folder, customFileName))) return customFileName;
        }

        var skuPhoto = ResolveSkuPhotoFileName(productName);
        if (skuPhoto is not null && File.Exists(Path.Combine(Folder, skuPhoto))) return skuPhoto;

        var representativePhoto = ResolveRepresentativePhotoFileName(productName);
        if (representativePhoto is not null && File.Exists(Path.Combine(Folder, representativePhoto))) return representativePhoto;

        var referencePhoto = ResolveReferencePhotoFileName(productName);
        if (referencePhoto is not null && File.Exists(Path.Combine(Folder, referencePhoto))) return referencePhoto;

        var normalized = (category ?? "").Trim().ToLowerInvariant();
        if (normalized.Contains("cpu") || normalized.Contains("vi xử lý")) return "product_cpu.jpg";
        if (normalized.Contains("main") || normalized.Contains("bo mạch")) return "product_mainboard.jpg";
        if (normalized.Contains("ram") || normalized.Contains("bộ nhớ")) return "product_ram.jpg";
        if (normalized.Contains("vga") || normalized.Contains("đồ họa") || normalized.Contains("card")) return "product_gpu.jpg";
        if (normalized.Contains("ssd") || normalized.Contains("hdd") || normalized.Contains("ổ cứng")) return "product_ssd.jpg";
        if (normalized.Contains("nguồn") || normalized.Contains("psu")) return "product_psu.jpg";
        if (normalized.Contains("case") || normalized.Contains("vỏ")) return "product_case.jpg";
        if (normalized.Contains("tản") || normalized.Contains("fan")) return "product_cooler.jpg";
        if (normalized.Contains("màn") || normalized.Contains("monitor")) return "product_monitor.jpg";
        if (normalized.Contains("bàn phím") || normalized.Contains("keyboard")) return "product_keyboard.jpg";
        if (normalized.Contains("chuột") || normalized.Contains("mouse")) return "product_mouse.jpg";
        if (normalized.Contains("tai nghe") || normalized.Contains("headset")) return "product_headset.jpg";
        return null;
    }

    public static bool IsCategoryFallback(string? savedFileName, string? category, string? productName = null)
    {
        var fileName = ResolveFileName(savedFileName, category, productName);
        var representativeIntelPhoto = string.Equals(
            Path.GetFileName(fileName), "product_cpu_intel_i5.jpg", StringComparison.OrdinalIgnoreCase);
        return fileName is not null && (IsCategoryPhoto(fileName) || representativeIntelPhoto) &&
               File.Exists(Path.Combine(Folder, fileName));
    }

    public static bool IsSkuPhoto(string? savedFileName, string? category, string? productName)
    {
        var mappedFileName = ResolveSkuPhotoFileName(productName);
        var resolvedFileName = ResolveFileName(savedFileName, category, productName);
        return mappedFileName is not null &&
               string.Equals(mappedFileName, resolvedFileName, StringComparison.OrdinalIgnoreCase) &&
               File.Exists(Path.Combine(Folder, mappedFileName)) &&
               !IsCategoryPhoto(mappedFileName);
    }

    public static bool IsNamedReferencePhoto(string? savedFileName, string? category, string? productName)
    {
        var mappedFileName = ResolveReferencePhotoFileName(productName);
        var resolvedFileName = ResolveFileName(savedFileName, category, productName);
        return mappedFileName is not null &&
               string.Equals(mappedFileName, resolvedFileName, StringComparison.OrdinalIgnoreCase) &&
               File.Exists(Path.Combine(Folder, mappedFileName));
    }

    public static bool IsReferenceImage(string? savedFileName, string? category, string? productName)
        => IsCategoryFallback(savedFileName, category, productName) ||
           IsNamedReferencePhoto(savedFileName, category, productName);

    public static string GetNamedReferenceDescription(string? productName)
        => (productName ?? "").Trim().ToLowerInvariant() switch
        {
            "logitech g pro x tkl" => "Ảnh Logitech G PRO TKL dùng để tham khảo; biến thể X chưa được xác minh.",
            "samsung 990 pro 1tb nvme" => "Ảnh Samsung 990 PRO 4TB dùng để tham khảo; khác dung lượng 1TB.",
            _ => "Ảnh tham khảo của biến thể gần giống, không xác nhận chính xác SKU."
        };

    public static bool IsIcon(string? fileName)
        => Path.GetFileName(fileName ?? "").StartsWith("icon_", StringComparison.OrdinalIgnoreCase);

    public static Bitmap? LoadForProduct(string? savedFileName, string? category)
        => Load(ResolveFileName(savedFileName, category));

    public static Bitmap? LoadForProduct(string? savedFileName, string? category, string? productName)
        => Load(ResolveFileName(savedFileName, category, productName));

    private static string? ResolveSkuPhotoFileName(string? productName)
        => (productName ?? "").Trim().ToLowerInvariant() switch
        {
            "intel core i7-13700k" => "product_cpu_intel_i7.jpg",
            "amd ryzen 5 7600" => "product_cpu_amd_r5_7600.jpg",
            "amd ryzen 7 7800x3d" => "product_cpu_amd_r7_7800x3d.jpg",
            "deepcool ak400" => "sku_deepcool_ak400.jpg",
            "corsair vengeance 32gb ddr5 6000" => "sku_corsair_vengeance_32gb_ddr5.jpg",
            "logitech g pro x superlight 2" => "sku_logitech_gpro_x_superlight2.jpg",
            _ => null
        };

    private static string? ResolveRepresentativePhotoFileName(string? productName)
        => (productName ?? "").Trim().ToLowerInvariant() switch
        {
            // The source shows a generic Intel CPU, not a verified i5-12400F.
            "intel core i5-12400f" => "product_cpu_intel_i5.jpg",
            _ => null
        };

    private static string? ResolveReferencePhotoFileName(string? productName)
        => ReferencePhotoFiles.TryGetValue((productName ?? "").Trim(), out var fileName) ? fileName : null;

    private static bool IsCategoryPhoto(string fileName)
        => CategoryPhotoFiles.Contains(Path.GetFileName(fileName));

    public static Bitmap? Load(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        var path = Path.Combine(Folder, fileName);
        if (!File.Exists(path)) return null;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var img = Image.FromStream(fs);
            if (string.Equals(Path.GetFileName(fileName), "product_cpu_intel_i5.jpg", StringComparison.OrdinalIgnoreCase))
            {
                // This licensed Intel CPU reference photo has large wood margins.
                // Crop only for display so the component is legible in the small
                // product preview; keep the source asset and its attribution intact.
                var crop = new Rectangle(
                    (int)(img.Width * 0.30), (int)(img.Height * 0.25),
                    (int)(img.Width * 0.50), (int)(img.Height * 0.48));
                var focused = new Bitmap(crop.Width, crop.Height);
                using (var g = Graphics.FromImage(focused))
                    g.DrawImage(img, new Rectangle(Point.Empty, focused.Size), crop, GraphicsUnit.Pixel);
                return focused;
            }

            return new Bitmap(img);
        }
        catch { return null; }
    }
}
