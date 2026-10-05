using ComputerStore.DAL;

namespace ComputerStore.BLL;

/// <summary>Sao lưu / khôi phục cơ sở dữ liệu (chỉ Admin) và xem nhật ký.</summary>
public class BackupBLL
{
    private readonly BackupDAL _dal = new();
    private readonly NhatKyDAL _log = new();

    public int Backup(string path)
    {
        Session.RequireAdmin();
        if (string.IsNullOrWhiteSpace(path)) throw new BusinessException("Chưa chọn nơi lưu file.");
        var n = _dal.Backup(path);
        Audit.Log("Sao lưu dữ liệu", Path.GetFileName(path));
        return n;
    }

    public int Restore(string path)
    {
        Session.RequireAdmin();
        if (!File.Exists(path)) throw new BusinessException("Không tìm thấy file sao lưu.");
        try
        {
            var n = _dal.Restore(path);
            Audit.Log("Khôi phục dữ liệu", Path.GetFileName(path));
            return n;
        }
        catch (InvalidDataException ex) { throw new BusinessException(ex.Message); }
    }

    public List<NhatKyDTO> NhatKy(int top = 300)
    {
        Session.RequireAdmin();
        return _log.Gan(top);
    }
}
