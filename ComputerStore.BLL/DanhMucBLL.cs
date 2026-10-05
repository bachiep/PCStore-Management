using ComputerStore.DAL;
using ComputerStore.DTO;

namespace ComputerStore.BLL;

public class DanhMucBLL
{
    private readonly DanhMucDAL _dal = new();
    public List<DanhMucDTO> GetAll() => _dal.GetAll();

    public void Save(DanhMucDTO d)
    {
        Session.RequireAdmin();
        Validator.Require(d.TenDM, "Tên danh mục");
        d.TenDM = d.TenDM.Trim();
        if (_dal.NameExists(d.TenDM, d.MaDM)) throw new BusinessException("Tên danh mục đã tồn tại.");
        if (d.MaDM == 0) _dal.Insert(d); else _dal.Update(d);
    }

    public void Delete(DanhMucDTO d)
    {
        Session.RequireAdmin();
        if (d.SoSanPham > 0) throw new BusinessException($"Danh mục đang có {d.SoSanPham} sản phẩm, không thể xóa.");
        _dal.Delete(d.MaDM);
    }
}

public class HangBLL
{
    private readonly HangDAL _dal = new();
    public List<HangDTO> GetAll() => _dal.GetAll();

    public void Save(HangDTO h)
    {
        Session.RequireAdmin();
        Validator.Require(h.TenHang, "Tên hãng");
        h.TenHang = h.TenHang.Trim();
        h.QuocGia = Validator.Clean(h.QuocGia);
        if (_dal.NameExists(h.TenHang, h.MaHang)) throw new BusinessException("Tên hãng đã tồn tại.");
        if (h.MaHang == 0) _dal.Insert(h); else _dal.Update(h);
    }

    public void Delete(HangDTO h)
    {
        Session.RequireAdmin();
        if (h.SoSanPham > 0) throw new BusinessException($"Hãng đang có {h.SoSanPham} sản phẩm, không thể xóa.");
        _dal.Delete(h.MaHang);
    }
}

public class SanPhamBLL
{
    private readonly SanPhamDAL _dal = new();
    public const int NguongSapHet = 5;

    public List<SanPhamDTO> Search(string keyword = "", int maDM = 0, int maHang = 0, bool chiDangBan = false)
        => _dal.Search(keyword.Trim(), maDM, maHang, chiDangBan);

    public SanPhamDTO? GetById(int id) => _dal.GetById(id);

    public static void Validate(SanPhamDTO s)
    {
        Validator.Require(s.TenSP, "Tên sản phẩm");
        if (s.MaDM <= 0) throw new BusinessException("Vui lòng chọn danh mục.");
        if (s.MaHang <= 0) throw new BusinessException("Vui lòng chọn hãng.");
        if (s.GiaNhap < 0 || s.GiaBan < 0) throw new BusinessException("Giá không được âm.");
        if (s.GiaBan <= 0) throw new BusinessException("Giá bán phải lớn hơn 0.");
        if (s.ThoiGianBH < 0 || s.ThoiGianBH > 120) throw new BusinessException("Thời gian bảo hành từ 0 đến 120 tháng.");
    }

    /// <summary>Trả về cảnh báo (nếu có) để GUI hỏi xác nhận, ví dụ giá bán thấp hơn giá nhập.</summary>
    public static string? Warning(SanPhamDTO s)
        => s.GiaBan < s.GiaNhap ? "Giá bán đang thấp hơn giá nhập. Bạn có chắc muốn lưu?" : null;

    public int Save(SanPhamDTO s)
    {
        Session.RequireAdmin();
        s.TenSP = s.TenSP.Trim();
        s.MoTa = Validator.Clean(s.MoTa);
        Validate(s);
        if (s.MaSP == 0) { var id = _dal.Insert(s); Audit.Log("Thêm sản phẩm", $"#{id} {s.TenSP}"); return id; }
        _dal.Update(s);
        Audit.Log("Sửa sản phẩm", $"#{s.MaSP} {s.TenSP}");
        return s.MaSP;
    }

    /// <returns>true nếu xóa hẳn, false nếu chỉ ngừng kinh doanh (do đã có giao dịch).</returns>
    public bool Delete(int id)
    {
        Session.RequireAdmin();
        if (_dal.HasTransactions(id))
        {
            _dal.SetStatus(id, false);
            Audit.Log("Ngừng kinh doanh SP", $"#{id}");
            return false;
        }
        _dal.Delete(id);
        Audit.Log("Xóa sản phẩm", $"#{id}");
        return true;
    }
}

public class NhaCungCapBLL
{
    private readonly NhaCungCapDAL _dal = new();
    public List<NhaCungCapDTO> Search(string keyword = "") => _dal.Search(keyword.Trim());

    public void Save(NhaCungCapDTO n)
    {
        Session.RequireAdmin();
        Validator.Require(n.TenNCC, "Tên nhà cung cấp");
        Validator.OptionalPhone(n.SDT);
        Validator.OptionalEmail(n.Email);
        n.TenNCC = n.TenNCC.Trim(); n.SDT = Validator.Clean(n.SDT); n.Email = Validator.Clean(n.Email); n.DiaChi = Validator.Clean(n.DiaChi);
        if (n.MaNCC == 0) _dal.Insert(n); else _dal.Update(n);
    }

    public void Delete(int id)
    {
        Session.RequireAdmin();
        if (_dal.HasReceipts(id)) throw new BusinessException("Nhà cung cấp đã có phiếu nhập, không thể xóa.");
        _dal.Delete(id);
    }
}

public class KhachHangBLL
{
    private readonly KhachHangDAL _dal = new();
    public List<KhachHangDTO> Search(string keyword = "") => _dal.Search(keyword.Trim());
    public KhachHangDTO? GetByPhone(string sdt) => _dal.GetByPhone(sdt.Trim());

    public static void Validate(KhachHangDTO k)
    {
        Validator.Require(k.HoTen, "Họ tên khách hàng");
        Validator.Require(k.SDT, "Số điện thoại");
        if (!Validator.IsPhone(k.SDT)) throw new BusinessException("Số điện thoại không hợp lệ (bắt đầu bằng 0, 10-11 chữ số).");
        Validator.OptionalEmail(k.Email);
    }

    public int Save(KhachHangDTO k)
    {
        k.HoTen = k.HoTen.Trim(); k.SDT = k.SDT.Trim(); k.Email = Validator.Clean(k.Email); k.DiaChi = Validator.Clean(k.DiaChi);
        Validate(k);
        if (_dal.PhoneExists(k.SDT, k.MaKH)) throw new BusinessException("Số điện thoại đã được dùng cho khách hàng khác.");
        if (k.MaKH == 0) return _dal.Insert(k);
        _dal.Update(k);
        return k.MaKH;
    }

    public void Delete(int id)
    {
        if (_dal.HasInvoices(id)) throw new BusinessException("Khách hàng đã có hóa đơn, không thể xóa.");
        _dal.Delete(id);
    }
}

public class NhanVienBLL
{
    private readonly NhanVienDAL _dal = new();
    public List<NhanVienDTO> Search(string keyword = "") => _dal.Search(keyword.Trim());

    public int Save(NhanVienDTO nv)
    {
        Session.RequireAdmin();
        Validator.Require(nv.HoTen, "Họ tên nhân viên");
        Validator.OptionalPhone(nv.SDT);
        Validator.OptionalEmail(nv.Email);
        if (nv.NgaySinh.HasValue && nv.NgaySinh.Value > DateTime.Today.AddYears(-16))
            throw new BusinessException("Nhân viên phải từ 16 tuổi trở lên.");
        if (nv.MaNV == Session.MaNV && !nv.TrangThai)
            throw new BusinessException("Không thể chuyển trạng thái nghỉ việc cho chính mình.");
        nv.HoTen = nv.HoTen.Trim(); nv.SDT = Validator.Clean(nv.SDT); nv.Email = Validator.Clean(nv.Email); nv.DiaChi = Validator.Clean(nv.DiaChi);
        if (nv.MaNV == 0) return _dal.Insert(nv);
        if (!nv.TrangThai)
        {
            var tk = new TaiKhoanDAL();
            if (tk.IsActiveAdminOfEmployee(nv.MaNV) && tk.CountActiveAdmins() <= 1)
                throw new BusinessException("Đây là quản trị viên hoạt động cuối cùng, không thể cho nghỉ việc.");
            tk.LockByEmployee(nv.MaNV);
        }
        _dal.Update(nv);
        return nv.MaNV;
    }

    public void Delete(int id)
    {
        Session.RequireAdmin();
        if (id == Session.MaNV) throw new BusinessException("Không thể xóa chính tài khoản đang đăng nhập.");
        if (_dal.HasActivity(id)) throw new BusinessException("Nhân viên đã có giao dịch (hóa đơn/phiếu nhập/bảo hành) nên không thể xóa. Hãy bỏ chọn \"Đang làm việc\" để cho nghỉ việc.");
        _dal.Delete(id);
        Audit.Log("Xóa nhân viên", $"#{id}");
    }
}
