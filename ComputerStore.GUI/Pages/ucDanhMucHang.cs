using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Pages;

/// <summary>Quản lý danh mục sản phẩm và hãng sản xuất (2 tab).</summary>
public class ucDanhMucHang : PageBase
{
    private readonly DanhMucBLL _dmBll = new();
    private readonly HangBLL _hangBll = new();

    private readonly DataGridView _gridDM = Theme.Grid(), _gridHang = Theme.Grid();
    private readonly TextBox _tenDM = Theme.Input(), _tenHang = Theme.Input(), _quocGia = Theme.Input();
    private int _idDM, _idHang;

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
        var page = new TabPage("Danh mục") { BackColor = Theme.Page, Padding = new Padding(0, 12, 0, 0) };
        _gridDM.AutoGenerateColumns = false; _gridDM.Name = "gridDanhMuc";
        _gridDM.Columns.AddRange(Theme.Col("MaDM", "Mã", weight: 0.5f), Theme.Col("TenDM", "Tên danh mục", weight: 3f), Theme.Col("SoSanPham", "Số sản phẩm", weight: 1f, right: true));
        _gridDM.SelectionChanged += (_, _) =>
        {
            if (_gridDM.CurrentRow?.DataBoundItem is DanhMucDTO d) { _idDM = d.MaDM; _tenDM.Text = d.TenDM; }
        };
        _tenDM.MaxLength = 100; _tenDM.Name = "txtTenDanhMuc";
        var save = Theme.PrimaryButton("Lưu", 80, (_, _) => SaveCategory()); save.Name = "btnLuuDanhMuc";
        var del = Theme.DangerButton("Xóa", 80, (_, _) => DeleteCategory());
        var neu = Theme.PrimaryButton("+ Thêm mới", 100, (_, _) => { _idDM = 0; _tenDM.Clear(); _gridDM.CurrentCell = null; _gridDM.ClearSelection(); _tenDM.Focus(); }); neu.Name = "btnThemDanhMuc";
        var editor = EditorCard("Thông tin danh mục", Theme.Field("Tên danh mục *", _tenDM, 300), Theme.Row(neu, save, del));
        if (!IsAdmin) foreach (var c in new Control[] { _tenDM, save, del, neu }) c.Enabled = false;
        var t = TwoCols(GridCard(_gridDM), editor, 380);
        page.Controls.Add(t);
        return page;
    }

    private TabPage BuildBrandTab()
    {
        var page = new TabPage("Hãng sản xuất") { BackColor = Theme.Page, Padding = new Padding(0, 12, 0, 0) };
        _gridHang.AutoGenerateColumns = false; _gridHang.Name = "gridHang";
        _gridHang.Columns.AddRange(Theme.Col("MaHang", "Mã", weight: 0.5f), Theme.Col("TenHang", "Tên hãng", weight: 2f), Theme.Col("QuocGia", "Quốc gia", weight: 1.5f), Theme.Col("SoSanPham", "Số sản phẩm", weight: 1f, right: true));
        _gridHang.SelectionChanged += (_, _) =>
        {
            if (_gridHang.CurrentRow?.DataBoundItem is HangDTO h) { _idHang = h.MaHang; _tenHang.Text = h.TenHang; _quocGia.Text = h.QuocGia ?? ""; }
        };
        _tenHang.MaxLength = 100; _quocGia.MaxLength = 50; _tenHang.Name = "txtTenHang";
        var save = Theme.PrimaryButton("Lưu", 80, (_, _) => SaveBrand()); save.Name = "btnLuuHang";
        var del = Theme.DangerButton("Xóa", 80, (_, _) => DeleteBrand());
        var neu = Theme.PrimaryButton("+ Thêm mới", 100, (_, _) => { _idHang = 0; _tenHang.Clear(); _quocGia.Clear(); _gridHang.CurrentCell = null; _gridHang.ClearSelection(); _tenHang.Focus(); }); neu.Name = "btnThemHang";
        var editor = EditorCard("Thông tin hãng", Theme.Field("Tên hãng *", _tenHang, 300), Theme.Field("Quốc gia", _quocGia, 300), Theme.Row(neu, save, del));
        if (!IsAdmin) foreach (var c in new Control[] { _tenHang, _quocGia, save, del, neu }) c.Enabled = false;
        page.Controls.Add(TwoCols(GridCard(_gridHang), editor, 380));
        return page;
    }

    public override void LoadData()
    {
        LoadCategories(); LoadBrands();
        _idDM = 0; _tenDM.Clear(); _gridDM.CurrentCell = null; _gridDM.ClearSelection();
        _idHang = 0; _tenHang.Clear(); _quocGia.Clear(); _gridHang.CurrentCell = null; _gridHang.ClearSelection();
    }
    private void LoadCategories() => Msg.Run(() => _gridDM.DataSource = _dmBll.GetAll());
    private void LoadBrands() => Msg.Run(() => _gridHang.DataSource = _hangBll.GetAll());

    private void SaveCategory()
    {
        if (Msg.Run(() => _dmBll.Save(new DanhMucDTO { MaDM = _idDM, TenDM = _tenDM.Text })))
        { LoadCategories(); _idDM = 0; _tenDM.Clear(); Msg.Info("Đã lưu danh mục."); }
    }

    private void DeleteCategory()
    {
        if (_gridDM.CurrentRow?.DataBoundItem is not DanhMucDTO d) { Msg.Warn("Hãy chọn danh mục cần xóa."); return; }
        if (!Ask($"Xóa danh mục \"{d.TenDM}\"?")) return;
        if (Msg.Run(() => _dmBll.Delete(d))) { LoadCategories(); _idDM = 0; _tenDM.Clear(); }
    }

    private void SaveBrand()
    {
        if (Msg.Run(() => _hangBll.Save(new HangDTO { MaHang = _idHang, TenHang = _tenHang.Text, QuocGia = _quocGia.Text })))
        { LoadBrands(); _idHang = 0; _tenHang.Clear(); _quocGia.Clear(); Msg.Info("Đã lưu hãng."); }
    }

    private void DeleteBrand()
    {
        if (_gridHang.CurrentRow?.DataBoundItem is not HangDTO h) { Msg.Warn("Hãy chọn hãng cần xóa."); return; }
        if (!Ask($"Xóa hãng \"{h.TenHang}\"?")) return;
        if (Msg.Run(() => _hangBll.Delete(h))) { LoadBrands(); _idHang = 0; _tenHang.Clear(); _quocGia.Clear(); }
    }
}
