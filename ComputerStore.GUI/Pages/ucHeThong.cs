using ComputerStore.BLL;

namespace ComputerStore.GUI.Pages;

/// <summary>Công cụ hệ thống: sao lưu, khôi phục dữ liệu và nhật ký thao tác (chỉ Admin).</summary>
public class ucHeThong : PageBase
{
    private readonly BackupBLL _bll = new();
    private readonly DataGridView _grid = Theme.Grid();

    public ucHeThong() : base("Hệ thống")
    {
        Name = "ucHeThong";
        var btnBackup = Theme.PrimaryButton("Sao lưu dữ liệu", 160, (_, _) => Backup()); btnBackup.Name = "btnSaoLuu";
        var btnRestore = Theme.DangerButton("Khôi phục dữ liệu", 170, (_, _) => Restore()); btnRestore.Name = "btnKhoiPhuc";
        var btnLog = Theme.GhostButton("Làm mới nhật ký", 140, (_, _) => LoadData());

        _grid.AutoGenerateColumns = false; _grid.Name = "gridNhatKy";
        _grid.Columns.AddRange(
            Theme.Col("ThoiGian", "Thời gian", weight: 1.3f, format: "dd/MM/yyyy HH:mm:ss"),
            Theme.Col("NguoiDung", "Người dùng", weight: 1f),
            Theme.Col("HanhDong", "Hành động", weight: 1.4f),
            Theme.Col("ChiTiet", "Chi tiết", weight: 3f));

        AddRow(Toolbar(btnBackup, btnRestore, ExportButton(_grid, "NhatKyThaoTac"), btnLog));
        AddRow(Theme.Caption("Khôi phục sẽ GHI ĐÈ toàn bộ dữ liệu hiện tại bằng file sao lưu — hãy sao lưu trước. Bảng dưới là nhật ký thao tác gần đây."));
        AddRow(GridCard(_grid), fill: true);
    }

    public override void LoadData() => Msg.Run(() => _grid.DataSource = _bll.NhatKy());

    private void Backup()
    {
        using var dlg = new SaveFileDialog { Filter = "SQL backup|*.sql", FileName = $"backup_{DateTime.Now:yyyyMMdd_HHmm}.sql" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        Msg.Run(() => { var n = _bll.Backup(dlg.FileName); Msg.Info($"Đã sao lưu {n:N0} dòng dữ liệu."); LoadData(); });
    }

    private void Restore()
    {
        using var dlg = new OpenFileDialog { Filter = "SQL backup|*.sql" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        if (!Ask("Khôi phục sẽ GHI ĐÈ toàn bộ dữ liệu hiện tại. Bạn chắc chắn tiếp tục?")) return;
        Msg.Run(() => { var n = _bll.Restore(dlg.FileName); Msg.Info($"Đã khôi phục ({n:N0} câu lệnh). Hãy đăng nhập lại nếu tài khoản thay đổi."); LoadData(); });
    }
}
