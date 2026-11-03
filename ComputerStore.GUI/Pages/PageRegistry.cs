using ComputerStore.GUI.Forms;
using ComputerStore.BLL;

namespace ComputerStore.GUI.Pages;

/// <summary>Một mục menu: khóa, nhóm, tên hiển thị, quyền và hàm tạo trang.</summary>
public record NavItem(string Key, string Group, string Text, bool AdminOnly, Func<frmMain, PageBase> Create);

/// <summary>Danh sách tất cả màn hình của ứng dụng (thứ tự = thứ tự trên menu).</summary>
public static class PageRegistry
{
    /// <summary>Admin lands on overview; employees start at POS and never enter admin-only reports.</summary>
    public static string DefaultKey => Session.IsAdmin ? "dashboard" : "banhang";

    public static readonly List<NavItem> Items = new()
    {
        new("dashboard", "Báo cáo",   "Dashboard",         true,  _ => new ucDashboard()),
        new("thongke",   "Báo cáo",   "Thống kê",          true,  _ => new ucThongKe()),
        new("sanpham",   "Kho hàng",  "Sản phẩm",          false, _ => new ucSanPham()),
        new("danhmuc",   "Kho hàng",  "Danh mục & Hãng",   true,  _ => new ucDanhMucHang()),
        new("ncc",       "Kho hàng",  "Nhà cung cấp",      true,  _ => new ucNhaCungCap()),
        new("nhapkho",   "Kho hàng",  "Nhập kho",          true,  _ => new ucNhapKho()),
        new("banhang",   "Bán hàng",  "Bán hàng (POS)",    false, _ => new ucBanHang()),
        new("hoadon",    "Bán hàng",  "Hóa đơn",           false, _ => new ucHoaDon()),
        new("baohanh",   "Bán hàng",  "Bảo hành",          false, _ => new ucBaoHanh()),
        new("khachhang", "Bán hàng",  "Khách hàng",        false, _ => new ucKhachHang()),
        new("nhanvien",  "Quản trị",  "Nhân viên",         true,  _ => new ucNhanVien()),
        new("hethong",   "Quản trị",  "Sao lưu / Khôi phục", true, _ => new ucHeThong()),
    };
}
