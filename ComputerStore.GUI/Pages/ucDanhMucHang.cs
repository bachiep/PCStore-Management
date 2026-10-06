using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

/// <summary>Quản lý danh mục sản phẩm và hãng sản xuất (2 tab).</summary>
public class ucDanhMucHang : PageBase
{
    private readonly DanhMucBLL _dmBll = new();
    private readonly HangBLL _hangBll = new();

    private readonly DataGridView _gridDM = Theme.Grid(), _gridHang = Theme.Grid();

    public ucDanhMucHang() : base("Danh mục & Hãng")
    {
        Name = "ucDanhMucHang";
        var tabs = new TabControl { Font = Theme.Base, Dock = DockStyle.Fill, Padding = new Point(18, 6) };
        tabs.TabPages.Add(BuildCategoryTab());
        tabs.TabPages.Add(BuildBrandTab());
        AddRow(tabs, fill: true);
    }

    private TabPage BuildCategoryTab()
    {
        var page = new TabPage("Danh mục") { BackColor = Theme.Page, Padding = new Padding(0, 10, 0, 0) };
        _gridDM.AutoGenerateColumns = false; _gridDM.Name = "gridDanhMuc";
        _gridDM.Columns.AddRange(
            Theme.Col("MaDM", "Mã", weight: 0.5f),
            Theme.Col("TenDM", "Tên danh mục", weight: 3f),
            Theme.Col("SoSanPham", "Số sản phẩm", weight: 1.2f, right: true));
        _gridDM.CellDoubleClick += (_, _) => EditCategory();

        var btnAdd = Theme.PrimaryButton("+ Thêm danh mục", 140, (_, _) => AddCategory()); btnAdd.Name = "btnThemDanhMuc"; btnAdd.Enabled = IsAdmin;
        var btnEdit = Theme.GhostButton("Sửa tên", 90, (_, _) => EditCategory()); btnEdit.Enabled = IsAdmin;
        var btnDel = Theme.DangerButton("Xóa", 80, (_, _) => DeleteCategory()); btnDel.Enabled = IsAdmin;
        var btnRefresh = Theme.GhostButton("Làm mới", 90, (_, _) => LoadCategories());

        var bar = Toolbar(btnAdd, btnEdit, btnDel, btnRefresh, ExportButton(_gridDM, "DanhSachDanhMuc"));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(bar, 0, 0);
        layout.Controls.Add(GridCard(_gridDM), 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildBrandTab()
    {
        var page = new TabPage("Hãng sản xuất") { BackColor = Theme.Page, Padding = new Padding(0, 10, 0, 0) };
        _gridHang.AutoGenerateColumns = false; _gridHang.Name = "gridHang";
        _gridHang.Columns.AddRange(
            Theme.Col("MaHang", "Mã", weight: 0.5f),
            Theme.Col("TenHang", "Tên hãng", weight: 2.2f),
            Theme.Col("QuocGia", "Quốc gia", weight: 1.5f),
            Theme.Col("SoSanPham", "Số sản phẩm", weight: 1f, right: true));
        _gridHang.CellDoubleClick += (_, _) => EditBrand();

        var btnAdd = Theme.PrimaryButton("+ Thêm hãng", 120, (_, _) => AddBrand()); btnAdd.Name = "btnThemHang"; btnAdd.Enabled = IsAdmin;
        var btnEdit = Theme.GhostButton("Sửa hãng", 90, (_, _) => EditBrand()); btnEdit.Enabled = IsAdmin;
        var btnDel = Theme.DangerButton("Xóa", 80, (_, _) => DeleteBrand()); btnDel.Enabled = IsAdmin;
        var btnRefresh = Theme.GhostButton("Làm mới", 90, (_, _) => LoadBrands());

        var bar = Toolbar(btnAdd, btnEdit, btnDel, btnRefresh, ExportButton(_gridHang, "DanhSachHang"));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(bar, 0, 0);
        layout.Controls.Add(GridCard(_gridHang), 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    public override void LoadData()
    {
        LoadCategories();
        LoadBrands();
    }

    private void LoadCategories() => Msg.Run(() => _gridDM.DataSource = _dmBll.GetAll());
    private void LoadBrands() => Msg.Run(() => _gridHang.DataSource = _hangBll.GetAll());

    private void AddCategory()
    {
        var ten = InputDialog.Ask(this, "Thêm danh mục", "Tên danh mục *:");
        if (string.IsNullOrWhiteSpace(ten)) return;
        if (Msg.Run(() => _dmBll.Save(new DanhMucDTO { TenDM = ten.Trim() })))
        {
            LoadCategories();
            Msg.Info("Đã thêm danh mục thành công.");
        }
    }

    private void EditCategory()
    {
        if (!IsAdmin) return;
        if (_gridDM.CurrentRow?.DataBoundItem is not DanhMucDTO d) { Msg.Warn("Hãy chọn danh mục cần sửa."); return; }
        var ten = InputDialog.Ask(this, "Sửa danh mục #" + d.MaDM, "Tên danh mục *:", false, d.TenDM);
        if (string.IsNullOrWhiteSpace(ten) || ten.Trim() == d.TenDM) return;
        if (Msg.Run(() => _dmBll.Save(new DanhMucDTO { MaDM = d.MaDM, TenDM = ten.Trim() })))
        {
            LoadCategories();
            Msg.Info("Đã cập nhật danh mục.");
        }
    }

    private void DeleteCategory()
    {
        if (_gridDM.CurrentRow?.DataBoundItem is not DanhMucDTO d) { Msg.Warn("Hãy chọn danh mục cần xóa."); return; }
        if (!Ask($"Xóa danh mục \"{d.TenDM}\"?")) return;
        if (Msg.Run(() => _dmBll.Delete(d))) { LoadCategories(); Msg.Info("Đã xóa danh mục."); }
    }

    private void AddBrand()
    {
        using var f = new frmThemHang();
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            LoadBrands();
            Msg.Info("Đã thêm hãng sản xuất.");
        }
    }

    private void EditBrand()
    {
        if (!IsAdmin) return;
        if (_gridHang.CurrentRow?.DataBoundItem is not HangDTO h) { Msg.Warn("Hãy chọn hãng cần sửa."); return; }
        using var f = new frmThemHang(h);
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            LoadBrands();
            Msg.Info("Đã cập nhật hãng sản xuất.");
        }
    }

    private void DeleteBrand()
    {
        if (_gridHang.CurrentRow?.DataBoundItem is not HangDTO h) { Msg.Warn("Hãy chọn hãng cần xóa."); return; }
        if (!Ask($"Xóa hãng \"{h.TenHang}\"?")) return;
        if (Msg.Run(() => _hangBll.Delete(h))) { LoadBrands(); Msg.Info("Đã xóa hãng."); }
    }
}
