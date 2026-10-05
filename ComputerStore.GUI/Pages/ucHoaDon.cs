using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

public class ucHoaDon : PageBase
{
    private readonly HoaDonBLL _bll = new();
    
    private readonly TextBox _search = Theme.Input(260, "Tìm SĐT, tên khách, ghi chú...");
    private readonly DateTimePicker _tuNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Theme.Base, Width = 120 };
    private readonly DateTimePicker _denNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Theme.Base, Width = 120 };
    private readonly DataGridView _grid = Theme.Grid();

    public ucHoaDon() : base("Hóa đơn bán hàng")
    {
        Name = "ucHoaDon";
        _tuNgay.Value = DateTime.Today.AddDays(-30);
        _denNgay.Value = DateTime.Today;

        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            Theme.Col("MaHD", "Mã HĐ", weight: 0.6f),
            Theme.Col("NgayLap", "Ngày lập", weight: 1.2f, format: "dd/MM/yyyy HH:mm"),
            Theme.Col("TenKH", "Khách hàng", weight: 1.5f),
            Theme.Col("SDTKH", "SĐT", weight: 1f),
            Theme.Col("TenNV", "Nhân viên", weight: 1.4f),
            Theme.Col("HinhThucTT", "Hình thức TT", weight: 1.1f),
            Theme.Col("TrangThaiText", "Trạng thái", weight: 0.9f),
            Theme.Col("ThanhToan", "Thanh toán", weight: 1f, format: "N0", right: true)
        );
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Rows[e.RowIndex].DataBoundItem is HoaDonDTO { DaHuy: true })
            { e.CellStyle!.ForeColor = Theme.Muted; e.CellStyle.Font = new Font(Theme.Base, FontStyle.Strikeout); }
        };
        _grid.CellDoubleClick += (_, _) => XemChiTiet();

        Debounce(_search, LoadList);
        _tuNgay.ValueChanged += (_, _) => LoadList();
        _denNgay.ValueChanged += (_, _) => LoadList();

        var btnDetail = Theme.GhostButton("Xem chi tiết", 120, (_, _) => XemChiTiet());
        var btnPrint = Theme.PrimaryButton("In hóa đơn", 120, (_, _) => InHoaDon());
        var btnRefresh = Theme.GhostButton("Làm mới", 90, (_, _) => LoadList());

        var pnlL = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        pnlL.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(0, 7, 5, 0) });
        pnlL.Controls.Add(_tuNgay);
        pnlL.Controls.Add(new Label { Text = "Đến:", AutoSize = true, Margin = new Padding(10, 7, 5, 0) });
        pnlL.Controls.Add(_denNgay);
        
        var btnCancel = Theme.DangerButton("Hủy hóa đơn", 120, (_, _) => HuyHoaDon());
        btnCancel.Visible = Session.IsAdmin;

        AddRow(Toolbar(_search, pnlL, btnRefresh, btnDetail, btnPrint, ExportButton(_grid, "DanhSachHoaDon"), btnCancel));
        AddRow(GridCard(_grid), fill: true);
    }

    private void HuyHoaDon()
    {
        if (_grid.CurrentRow?.DataBoundItem is not HoaDonDTO hd) { Msg.Warn("Hãy chọn hóa đơn cần hủy."); return; }
        if (hd.DaHuy) { Msg.Warn($"Hóa đơn #{hd.MaHD} đã được hủy trước đó."); return; }
        var lyDo = InputDialog.Ask(this, "Hủy hóa đơn #" + hd.MaHD, "Nhập lý do hủy (tồn kho và serial sẽ được hoàn lại):");
        if (string.IsNullOrWhiteSpace(lyDo)) return;
        if (!Ask($"Xác nhận hủy hóa đơn #{hd.MaHD}? Thao tác không thể hoàn tác.")) return;
        Msg.Run(() => { _bll.HuyHoaDon(hd.MaHD, lyDo); Msg.Info("Đã hủy hóa đơn."); LoadList(); });
    }

    public override void LoadData() => LoadList();
    private void LoadList() => Msg.Run(() => _grid.DataSource = _bll.Search(_tuNgay.Value, _denNgay.Value, _search.Text));

    private void XemChiTiet()
    {
        if (_grid.CurrentRow?.DataBoundItem is not HoaDonDTO hd) { Msg.Warn("Hãy chọn hóa đơn cần xem."); return; }
        using var f = new frmChiTietHoaDon(hd);
        f.ShowDialog();
    }

    private void InHoaDon()
    {
        if (_grid.CurrentRow?.DataBoundItem is not HoaDonDTO hd) { Msg.Warn("Hãy chọn hóa đơn cần in."); return; }
        
        using var dlg = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"HoaDon_{hd.MaHD}.pdf" };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Msg.Run(() =>
            {
                var items = new HoaDonBLL().GetDetails(hd.MaHD);
                ComputerStore.GUI.Services.PdfExporter.ExportHoaDon(hd, items, dlg.FileName);
                Msg.Info("Đã xuất hóa đơn PDF thành công.");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            });
        }
    }
}
