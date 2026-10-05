using ComputerStore.DAL;
using ComputerStore.DTO;

namespace ComputerStore.BLL;

/// <summary>Nghiệp vụ bán hàng: kiểm tra giỏ hàng, tính tiền, lập hóa đơn.</summary>
public class HoaDonBLL
{
    private readonly HoaDonDAL _dal = new();
    private readonly SerialDAL _serialDal = new();

    public List<HoaDonDTO> Search(DateTime tuNgay, DateTime denNgay, string keyword = "")
    {
        if (tuNgay.Date > denNgay.Date) throw new BusinessException("Từ ngày không được lớn hơn đến ngày.");
        return _dal.Search(tuNgay, denNgay, keyword.Trim());
    }

    public List<HoaDonDTO> GetByCustomer(int maKH) => _dal.GetByCustomer(maKH);
    public HoaDonDTO? GetById(int maHD) => _dal.GetById(maHD);
    public List<ChiTietHoaDonDTO> GetDetails(int maHD) => _dal.GetDetails(maHD);
    public List<string> GetSerialsInStock(int maSP) => _serialDal.GetInStock(maSP);

    /// <summary>Tổng tiền hàng = Σ (số lượng × đơn giá).</summary>
    public static decimal TinhTongTien(IEnumerable<ChiTietHoaDonDTO> items) => items.Sum(i => i.SoLuong * i.DonGia);

    /// <summary>Thành toán = tổng tiền − giảm giá.</summary>
    public static decimal TinhThanhToan(decimal tongTien, decimal giamGia) => tongTien - giamGia;

    public static void Validate(HoaDonDTO hd, IList<ChiTietHoaDonDTO> items)
    {
        if (items == null || items.Count == 0) throw new BusinessException("Giỏ hàng đang trống.");

        var tong = TinhTongTien(items);
        if (hd.GiamGia < 0) throw new BusinessException("Giảm giá không được âm.");
        if (hd.GiamGia > tong) throw new BusinessException("Giảm giá không được lớn hơn tổng tiền hàng.");
        if (!HinhThucThanhToan.TatCa.Contains(hd.HinhThucTT)) throw new BusinessException("Hình thức thanh toán không hợp lệ.");

        if (items.GroupBy(i => i.MaSP).Any(g => g.Count() > 1))
            throw new BusinessException("Mỗi sản phẩm chỉ được xuất hiện một dòng trong giỏ hàng.");

        var allSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items)
        {
            if (it.SoLuong <= 0) throw new BusinessException($"Số lượng của \"{it.TenSP}\" phải lớn hơn 0.");
            if (it.DonGia < 0) throw new BusinessException($"Đơn giá của \"{it.TenSP}\" không được âm.");
            if (it.Serials.Count != it.SoLuong)
                throw new BusinessException($"\"{it.TenSP}\": cần chọn đúng {it.SoLuong} serial (đã chọn {it.Serials.Count}).");
            foreach (var s in it.Serials)
                if (!allSerials.Add(s.Trim())) throw new BusinessException($"Serial {s} bị chọn trùng.");
        }
    }

    public int LapHoaDon(HoaDonDTO hd, IList<ChiTietHoaDonDTO> items)
    {
        if (!Session.IsLoggedIn) throw new BusinessException("Chưa đăng nhập.");
        Validate(hd, items);
        var spDal = new SanPhamDAL();
        foreach (var it in items)
        {
            var sp = spDal.GetById(it.MaSP);
            if (sp == null) throw new BusinessException($"Sản phẩm \"{it.TenSP}\" không còn tồn tại.");
            if (!sp.TrangThai) throw new BusinessException($"Sản phẩm \"{sp.TenSP}\" đã ngừng kinh doanh, không thể bán.");
        }
        hd.MaNV = Session.MaNV;
        hd.TenNV = Session.CurrentUser?.HoTen;
        hd.NgayLap = DateTime.Now;
        hd.TongTien = TinhTongTien(items);
        hd.ThanhToan = TinhThanhToan(hd.TongTien, hd.GiamGia);
        hd.GhiChu = Validator.Clean(hd.GhiChu);
        try { return _dal.Create(hd, items); }
        catch (InvalidOperationException ex) { throw new BusinessException(ex.Message); }
    }

    /// <summary>Hủy hóa đơn (chỉ Admin, bắt buộc có lý do): hoàn tồn kho, trả serial về kho, loại khỏi doanh thu.</summary>
    public void HuyHoaDon(int maHD, string lyDo)
    {
        Session.RequireAdmin();
        Validator.Require(lyDo, "Lý do hủy");
        try { _dal.Huy(maHD, lyDo.Trim()); Audit.Log("Hủy hóa đơn", $"HĐ #{maHD}: {lyDo.Trim()}"); }
        catch (InvalidOperationException ex) { throw new BusinessException(ex.Message); }
    }
}

/// <summary>Nghiệp vụ nhập kho: phiếu nhập, serial.</summary>
public class PhieuNhapBLL
{
    private readonly PhieuNhapDAL _dal = new();
    private readonly SerialDAL _serialDal = new();

    public List<PhieuNhapDTO> Search(DateTime tuNgay, DateTime denNgay)
    {
        if (tuNgay.Date > denNgay.Date) throw new BusinessException("Từ ngày không được lớn hơn đến ngày.");
        return _dal.Search(tuNgay, denNgay);
    }

    public List<ChiTietPhieuNhapDTO> GetDetails(int maPN) => _dal.GetDetails(maPN);

    public static decimal TinhTongTien(IEnumerable<ChiTietPhieuNhapDTO> items) => items.Sum(i => i.SoLuong * i.DonGia);

    /// <summary>Tự sinh serial dạng PREFIX-yyMMdd-0001 (không trùng với serial đang có).</summary>
    public List<string> TaoSerialTuDong(int maSP, string prefix, int soLuong)
    {
        prefix = (prefix ?? "").Trim().ToUpperInvariant();
        if (!Validator.IsSerial(prefix + "-000000-0000")) throw new BusinessException("Tiền tố serial chỉ gồm chữ không dấu, số, dấu - _ .");
        if (soLuong <= 0) throw new BusinessException("Số lượng phải lớn hơn 0.");
        var seq = _serialDal.NextSequence(maSP);
        var list = new List<string>();
        while (list.Count < soLuong)
        {
            var candidate = $"{prefix}-{DateTime.Now:yyMMdd}-{seq++:D4}";
            if (_serialDal.FindExisting(new[] { candidate }).Count == 0) list.Add(candidate);
        }
        return list;
    }

    public static void Validate(PhieuNhapDTO pn, IList<ChiTietPhieuNhapDTO> items)
    {
        if (pn.MaNCC <= 0) throw new BusinessException("Vui lòng chọn nhà cung cấp.");
        if (items == null || items.Count == 0) throw new BusinessException("Phiếu nhập chưa có sản phẩm.");
        if (items.GroupBy(i => i.MaSP).Any(g => g.Count() > 1))
            throw new BusinessException("Mỗi sản phẩm chỉ được xuất hiện một dòng trong phiếu nhập.");

        var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items)
        {
            if (it.SoLuong <= 0) throw new BusinessException($"Số lượng của \"{it.TenSP}\" phải lớn hơn 0.");
            if (it.DonGia < 0) throw new BusinessException($"Đơn giá nhập của \"{it.TenSP}\" không được âm.");
            if (it.Serials.Count != it.SoLuong)
                throw new BusinessException($"\"{it.TenSP}\": cần nhập đúng {it.SoLuong} serial (hiện có {it.Serials.Count}).");
            foreach (var s in it.Serials)
            {
                if (!Validator.IsSerial(s))
                    throw new BusinessException($"Serial \"{s}\" không hợp lệ (3-50 ký tự: chữ, số, - _ . /).");
                if (!all.Add(s.Trim())) throw new BusinessException($"Serial {s} bị nhập trùng.");
            }
        }
    }

    public int LapPhieuNhap(PhieuNhapDTO pn, IList<ChiTietPhieuNhapDTO> items)
    {
        Session.RequireAdmin();
        Validate(pn, items);
        foreach (var it in items) it.Serials = it.Serials.Select(s => s.Trim()).ToList();

        var trung = _serialDal.FindExisting(items.SelectMany(i => i.Serials));
        if (trung.Count > 0) throw new BusinessException("Serial đã tồn tại trong hệ thống: " + string.Join(", ", trung));

        pn.MaNV = Session.MaNV;
        pn.NgayNhap = DateTime.Now;
        pn.TongTien = TinhTongTien(items);
        pn.GhiChu = Validator.Clean(pn.GhiChu);
        return _dal.Create(pn, items);
    }
}

/// <summary>Kết quả kiểm tra bảo hành theo serial.</summary>
public record KetQuaBaoHanh(bool HopLe, string ThongBao);

public class BaoHanhBLL
{
    private readonly BaoHanhDAL _dal = new();
    private readonly SerialDAL _serialDal = new();

    public SerialDTO? TraCuu(string serial)
    {
        Validator.Require(serial, "Serial");
        return _serialDal.Lookup(serial.Trim());
    }

    public List<PhieuBaoHanhDTO> LichSu(string serial) => _dal.GetBySerial(serial);
    public List<PhieuBaoHanhDTO> Search(string keyword = "", string trangThai = "") => _dal.Search(keyword.Trim(), trangThai);

    /// <summary>Kiểm tra serial có đủ điều kiện nhận bảo hành tại ngày <paramref name="homNay"/> hay không.</summary>
    public static KetQuaBaoHanh KiemTra(SerialDTO? s, DateTime homNay)
    {
        if (s == null) return new(false, "Không tìm thấy serial trong hệ thống.");
        if (s.TrangThai == TrangThaiSerial.TrongKho || s.MaHD == null)
            return new(false, "Sản phẩm chưa được bán nên không có bảo hành.");
        if (s.TrangThai == TrangThaiSerial.DangBaoHanh)
            return new(false, "Sản phẩm đang trong quá trình bảo hành.");
        if (s.HanBH == null) return new(false, "Sản phẩm không có thông tin hạn bảo hành.");
        if (s.HanBH.Value.Date < homNay.Date)
            return new(false, $"Đã hết hạn bảo hành từ {s.HanBH.Value:dd/MM/yyyy}.");
        var conLai = (s.HanBH.Value.Date - homNay.Date).Days;
        return new(true, $"Còn hạn bảo hành đến {s.HanBH.Value:dd/MM/yyyy} (còn {conLai} ngày).");
    }

    public int LapPhieu(string serial, string moTaLoi)
    {
        if (!Session.IsLoggedIn) throw new BusinessException("Chưa đăng nhập.");
        Validator.Require(moTaLoi, "Mô tả lỗi");
        var s = _serialDal.Lookup(serial.Trim());
        var kq = KiemTra(s, DateTime.Today);
        if (!kq.HopLe) throw new BusinessException(kq.ThongBao);

        return _dal.Create(new PhieuBaoHanhDTO
        {
            Serial = s!.Serial, MaKH = s.MaKH, MaNV = Session.MaNV, NgayNhan = DateTime.Now,
            MoTaLoi = moTaLoi.Trim(), TrangThai = TrangThaiBaoHanh.DangXuLy
        });
    }

    public void CapNhatKetQua(PhieuBaoHanhDTO p, string trangThaiMoi, string? ketQua)
    {
        if (!Session.IsLoggedIn) throw new BusinessException("Chưa đăng nhập.");
        if (p.TrangThai != TrangThaiBaoHanh.DangXuLy)
            throw new BusinessException("Phiếu đã kết thúc, không thể thay đổi.");
        if (!TrangThaiBaoHanh.TatCa.Contains(trangThaiMoi)) throw new BusinessException("Trạng thái không hợp lệ.");
        if (trangThaiMoi != TrangThaiBaoHanh.DangXuLy) Validator.Require(ketQua, "Kết quả xử lý");

        p.TrangThai = trangThaiMoi;
        p.KetQua = Validator.Clean(ketQua);
        p.NgayTra = trangThaiMoi == TrangThaiBaoHanh.DangXuLy ? null : DateTime.Now;
        _dal.UpdateResult(p);
    }
}

public class ThongKeBLL
{
    private readonly ThongKeDAL _dal = new();

    public TongQuanDTO TongQuan() => _dal.TongQuan(SanPhamBLL.NguongSapHet);

    public List<DoanhThuDTO> DoanhThuTheoNgay(DateTime tu, DateTime den)
    {
        Session.RequireAdmin();
        if (tu.Date > den.Date) throw new BusinessException("Từ ngày không được lớn hơn đến ngày.");
        return _dal.DoanhThuTheoNgay(tu, den);
    }

    /// <summary>Doanh thu 7 ngày gần nhất (đủ 7 điểm, ngày không có hóa đơn = 0) cho dashboard.</summary>
    public List<DoanhThuDTO> DoanhThu7Ngay()
    {
        var den = DateTime.Today;
        var tu = den.AddDays(-6);
        var data = _dal.DoanhThuTheoNgay(tu, den).ToDictionary(x => x.Ngay!.Value.Date);
        return Enumerable.Range(0, 7).Select(i =>
        {
            var d = tu.AddDays(i);
            return data.TryGetValue(d, out var x) ? x : new DoanhThuDTO { Ngay = d, Nhan = d.ToString("dd/MM") };
        }).ToList();
    }

    public List<DoanhThuDTO> DoanhThuTheoThang(int nam)
    {
        Session.RequireAdmin();
        if (nam < 2000 || nam > 2100) throw new BusinessException("Năm không hợp lệ.");
        return _dal.DoanhThuTheoThang(nam);
    }

    public List<TopSanPhamDTO> TopBanChay(DateTime tu, DateTime den, int top = 10)
    {
        if (tu.Date > den.Date) throw new BusinessException("Từ ngày không được lớn hơn đến ngày.");
        return _dal.TopBanChay(tu, den, top);
    }

    public System.Data.DataTable SanPhamSapHet(int nguong = SanPhamBLL.NguongSapHet) => _dal.SanPhamSapHet(nguong);
}
