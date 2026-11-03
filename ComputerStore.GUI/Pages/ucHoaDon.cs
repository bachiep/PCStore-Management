using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

public class ucHoaDon : PageBase
{
    private readonly HoaDonBLL _bll = new();
    
    private readonly TextBox _search = Theme.Input(150, "Tìm SĐT hoặc khách...");
    private readonly DateTimePicker _tuNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Theme.Base, Width = 110 };
    private readonly DateTimePicker _denNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Theme.Base, Width = 110 };
    private readonly DataGridView _grid = Theme.Grid();

    public ucHoaDon() : base("Hóa đơn bán hàng")
    {
        Name = "ucHoaDon";
        _tuNgay.Value = DateTime.Today.AddDays(-30);
        _denNgay.Value = DateTime.Today;

        _grid.AutoGenerateColumns = false;
        var invoiceDate = Theme.Col("NgayLap", "Ngày lập", weight: 1.35f, format: "dd/MM/yy HH:mm");
        invoiceDate.MinimumWidth = 120;
        var customer = Theme.Col("TenKH", "Khách hàng", weight: 1.4f);
        customer.MinimumWidth = 130;
        customer.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var phone = Theme.Col("SDTKH", "SĐT", weight: 1f);
        phone.MinimumWidth = 100;
        var employee = Theme.Col("TenNV", "Nhân viên", weight: 1.3f);
        employee.MinimumWidth = 125;
        employee.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var paymentMethod = Theme.Col("HinhThucTT", "Hình thức TT", weight: 1.1f);
        paymentMethod.MinimumWidth = 105;
        paymentMethod.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var status = Theme.Col("TrangThaiText", "Trạng thái", weight: 0.9f);
        status.MinimumWidth = 90;
        status.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var payment = Theme.Col("ThanhToan", "Thanh toán", weight: 1f, format: "N0", right: true);
        payment.MinimumWidth = 100;
        _grid.Columns.AddRange(
            Theme.Col("MaHD", "Mã HĐ", weight: 0.6f),
            invoiceDate,
            customer,
            phone,
            employee,
            paymentMethod,
            status,
            payment
        );
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Rows[e.RowIndex].DataBoundItem is HoaDonDTO { DaHuy: true })
            { e.CellStyle!.ForeColor = Theme.Muted; e.CellStyle.Font = new Font(Theme.Base, FontStyle.Strikeout); }
        };
        _grid.CellDoubleClick += (_, _) => XemChiTiet();

        Debounce(_search, LoadList);
        _tuNgay.ValueChanged += (_, _) => LoadList();
        _denNgay.ValueChanged += (_, _) => LoadList();

        var btnDetail = Theme.GhostButton("Chi tiết", 88, (_, _) => XemChiTiet());
        var btnPrint = Theme.PrimaryButton("In hóa đơn", 90, (_, _) => InHoaDon());
        var btnRefresh = Theme.GhostButton("Làm mới", 68, (_, _) => LoadList());

        var pnlL = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        pnlL.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(0, 7, 5, 0) });
        pnlL.Controls.Add(_tuNgay);
        pnlL.Controls.Add(new Label { Text = "Đến:", AutoSize = true, Margin = new Padding(6, 7, 5, 0) });
        pnlL.Controls.Add(_denNgay);
        var btnCancel = Theme.DangerButton("Hủy HĐ", 72, (_, _) => HuyHoaDon());
        btnCancel.Visible = Session.IsAdmin;

        var infoHD = Theme.InfoBadge(
            "• Nhấp đúp chuột vào một hóa đơn để xem chi tiết danh sách linh kiện và serial đã xuất.\n" +
            "• In hóa đơn PDF: Hỗ trợ xuất file PDF có định dạng chuẩn theo hóa đơn bán hàng.\n" +
            "• Hủy hóa đơn (Chỉ Admin): Yêu cầu nhập lý do; hệ thống tự động hoàn trả số lượng tồn kho và giải phóng serial sản phẩm tương ứng.",
            "Quy tắc Quản lý Hóa đơn");

        var filterRow = new FlowLayoutPanel
        {
            AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0), Padding = new Padding(0), BackColor = Color.Transparent
        };
        filterRow.Controls.Add(Theme.SearchField(_search, "Tìm SĐT hoặc khách"));
        filterRow.Controls.Add(pnlL);

        var actionRow = new FlowLayoutPanel
        {
            AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0), Padding = new Padding(0), BackColor = Color.Transparent
        };
        actionRow.Controls.Add(btnRefresh);
        actionRow.Controls.Add(btnDetail);
        actionRow.Controls.Add(btnPrint);
        actionRow.Controls.Add(ExportButton(_grid, "DanhSachHoaDon", 92));
        actionRow.Controls.Add(btnCancel);
        actionRow.Controls.Add(infoHD);

        var toolbar = new TableLayoutPanel
        {
            ColumnCount = 1, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 10), BackColor = Color.Transparent
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionRow.Margin = new Padding(0, 6, 0, 0);
        toolbar.Controls.Add(filterRow, 0, 0);
        toolbar.Controls.Add(actionRow, 0, 1);
        AddRow(toolbar);
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
