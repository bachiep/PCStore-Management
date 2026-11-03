using ComputerStore.BLL;
using ComputerStore.DAL;
using ComputerStore.GUI.Forms;
using MySqlConnector;

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
        var warning = Theme.Caption("Tệp sao lưu SQL chưa được mã hóa; chứa dữ liệu khách hàng, nhân viên, hash mật khẩu và nhật ký. Hãy lưu ở nơi an toàn, hạn chế chia sẻ. Khôi phục sẽ GHI ĐÈ toàn bộ dữ liệu hiện tại.");
        warning.AutoSize = false;
        warning.Height = 64;
        AddRow(warning);
        AddRow(Theme.Caption("Bảng dưới là nhật ký thao tác gần đây."));
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
        var targetDatabase = new MySqlConnectionStringBuilder(DbHelper.ConnectionString).Database;
        string? sourceDatabase;
        try { sourceDatabase = _bll.BackupSourceDatabase(dlg.FileName); }
        catch (Exception ex) { Msg.Error(ex.Message); return; }

        var source = string.IsNullOrWhiteSpace(sourceDatabase) ? "không xác định (backup phiên bản cũ)" : sourceDatabase;
        if (!Ask($"Khôi phục sẽ GHI ĐÈ toàn bộ dữ liệu trong CSDL đích '{targetDatabase}'.\nCSDL nguồn của file: '{source}'. Bạn chắc chắn tiếp tục?")) return;
        var confirmation = InputDialog.Ask(this, "Xác nhận CSDL đích",
            $"Nhập chính xác tên CSDL sẽ bị ghi đè: {targetDatabase}");
        if (!string.Equals(confirmation?.Trim(), targetDatabase, StringComparison.Ordinal))
        {
            Msg.Warn("Tên CSDL xác nhận không khớp. Đã hủy khôi phục.");
            return;
        }
        Msg.Run(() => { var n = _bll.Restore(dlg.FileName); Msg.Info($"Đã khôi phục ({n:N0} câu lệnh). Hãy đăng nhập lại nếu tài khoản thay đổi."); LoadData(); });
    }
}
