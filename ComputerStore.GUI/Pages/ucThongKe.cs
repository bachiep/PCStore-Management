using ComputerStore.BLL;
using ComputerStore.DTO;
using ScottPlot.WinForms;
using ScottPlot;

namespace ComputerStore.GUI.Pages;

public class ucThongKe : PageBase
{
    private readonly ThongKeBLL _bll = new();
    
    private readonly ComboBox _cboNam = Theme.Combo();
    private readonly FormsPlot _plot = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _gridTop = Theme.Grid();
    private readonly DataGridView _gridDoanhThu = Theme.Grid();
    private readonly ToolTip _chartTip = new();
    private List<DoanhThuDTO> _currentDataThang = new();
    private int _lastHoverMonth = -1;
    
    public ucThongKe() : base("Thống kê (Admin)")
    {
        Name = "ucThongKe";
        
        _plot.MouseMove += (_, e) =>
        {
            if (_currentDataThang.Count == 0) return;
            var pixel = new ScottPlot.Pixel(e.X, e.Y);
            var coord = _plot.Plot.GetCoordinates(pixel);
            int idx = (int)Math.Round(coord.X);
            if (idx >= 0 && idx < _currentDataThang.Count && coord.Y >= 0)
            {
                var maxVal = Math.Max((double)_currentDataThang[idx].DoanhThu, (double)_currentDataThang[idx].LoiNhuan) / 1000000.0;
                if (coord.Y <= maxVal * 1.15 && Math.Abs(coord.X - idx) <= 0.45)
                {
                    if (_lastHoverMonth != idx)
                    {
                        _lastHoverMonth = idx;
                        var item = _currentDataThang[idx];
                        _chartTip.Show($"{item.Nhan}:\n• Doanh thu: {item.DoanhThu:N0} ₫\n• Lợi nhuận: {item.LoiNhuan:N0} ₫\n• Số HĐ: {item.SoHoaDon}", _plot, e.X + 15, e.Y + 15, 3000);
                    }
                    return;
                }
            }
            if (_lastHoverMonth != -1)
            {
                _lastHoverMonth = -1;
                _chartTip.Hide(_plot);
            }
        };
        _plot.MouseLeave += (_, _) => { _lastHoverMonth = -1; _chartTip.Hide(_plot); };

        for (int i = DateTime.Now.Year; i >= 2020; i--) _cboNam.Items.Add(i);
        _cboNam.SelectedIndex = 0;
        _cboNam.SelectedIndexChanged += (_, _) => LoadData();

        _gridTop.AutoGenerateColumns = false;
        _gridTop.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm", weight: 2f),
            Theme.Col("SoLuongBan", "Đã bán", weight: 0.8f, right: true),
            Theme.Col("DoanhThu", "Doanh thu", weight: 1.2f, format: "N0", right: true)
        );

        _gridDoanhThu.AutoGenerateColumns = false;
        _gridDoanhThu.Columns.AddRange(
            Theme.Col("Nhan", "Tháng", weight: 1f),
            Theme.Col("SoHoaDon", "Số HĐ", weight: 1f, right: true),
            Theme.Col("DoanhThu", "Doanh thu", weight: 1.5f, format: "N0", right: true),
            Theme.Col("LoiNhuan", "Lợi nhuận (Ước tính)", weight: 1.5f, format: "N0", right: true)
        );

        var btnExcel = Theme.PrimaryButton("Xuất Excel", 120, (_, _) => XuatExcel());
        var btnPdf = Theme.DangerButton("Xuất PDF", 120, (_, _) => XuatPdf());

        var pnlL = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        pnlL.Controls.Add(new System.Windows.Forms.Label { Text = "Năm:", AutoSize = true, Margin = new Padding(0, 7, 5, 0) });
        pnlL.Controls.Add(_cboNam);
        
        AddRow(Toolbar(pnlL, btnExcel, btnPdf));

        var topPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        
        var pnlChart = Theme.CardPanel(); pnlChart.Dock = DockStyle.Fill;
        pnlChart.Controls.Add(_plot);
        
        var pnlGridTop = Theme.CardPanel(); pnlGridTop.Dock = DockStyle.Fill;
        var lblTop = new System.Windows.Forms.Label { Text = "Top 10 sản phẩm bán chạy", Font = Theme.H2, AutoSize = true, Margin = new Padding(10) };
        lblTop.Dock = DockStyle.Top;
        _gridTop.Dock = DockStyle.Fill;
        pnlGridTop.Controls.Add(_gridTop); pnlGridTop.Controls.Add(lblTop);

        topPanel.Controls.Add(pnlChart, 0, 0);
        topPanel.Controls.Add(pnlGridTop, 1, 0);
        
        AddRow(topPanel, fill: true);

        var pnlGridDt = Theme.CardPanel(); pnlGridDt.Dock = DockStyle.Fill; pnlGridDt.Height = 250;
        var lblDt = new System.Windows.Forms.Label { Text = "Chi tiết doanh thu theo tháng", Font = Theme.H2, AutoSize = true, Margin = new Padding(10) };
        lblDt.Dock = DockStyle.Top;
        _gridDoanhThu.Dock = DockStyle.Fill;
        pnlGridDt.Controls.Add(_gridDoanhThu); pnlGridDt.Controls.Add(lblDt);
        
        AddRow(pnlGridDt);
    }

    public override void LoadData()
    {
        if (_cboNam.SelectedItem == null) return;
        var nam = (int)_cboNam.SelectedItem;
        
        Msg.Run(() =>
        {
            var dataThang = _bll.DoanhThuTheoThang(nam);
            _gridDoanhThu.DataSource = dataThang;
            
            _gridTop.DataSource = _bll.TopBanChay(new DateTime(nam, 1, 1), new DateTime(nam, 12, 31));

            _plot.Plot.Clear();
            _plot.Plot.FigureBackground.Color = ScottPlot.Colors.White;

            double[] dtValues = dataThang.Select(x => (double)x.DoanhThu / 1000000.0).ToArray();
            double[] lnValues = dataThang.Select(x => (double)x.LoiNhuan / 1000000.0).ToArray();

            // Vị trí cột (lệch nhau để hiển thị cạnh nhau)
            double[] positions = dataThang.Select((x, i) => (double)i).ToArray();
            double[] posDt = positions.Select(x => x - 0.2).ToArray();
            double[] posLn = positions.Select(x => x + 0.2).ToArray();

            var barDt = _plot.Plot.Add.Bars(posDt, dtValues);
            foreach (var b in barDt.Bars) { b.FillColor = ScottPlot.Color.FromHex("#3B82F6"); b.Size = 0.35; }
            barDt.LegendText = "Doanh thu";

            var barLn = _plot.Plot.Add.Bars(posLn, lnValues);
            foreach (var b in barLn.Bars) { b.FillColor = ScottPlot.Color.FromHex("#10B981"); b.Size = 0.35; }
            barLn.LegendText = "Lợi nhuận";

            _currentDataThang = dataThang;
            var labels = dataThang.Select(x => x.Nhan).ToArray();
            _plot.Plot.Axes.Bottom.SetTicks(positions, labels);

            _plot.Plot.ShowLegend(ScottPlot.Alignment.UpperRight);
            _plot.Plot.Title($"Biểu đồ doanh thu & lợi nhuận năm {nam}");
            _plot.Plot.YLabel("Số tiền (Triệu ₫)");
            
            _plot.Refresh();
        });
    }

    private void XuatExcel()
    {
        if (_cboNam.SelectedItem == null) return;
        var nam = (int)_cboNam.SelectedItem;
        using var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"ThongKe_{nam}.xlsx" };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Msg.Run(() =>
            {
                ComputerStore.GUI.Services.ExcelExporter.ExportThongKe(nam, dlg.FileName);
                Msg.Info("Đã xuất file Excel thành công.");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            });
        }
    }

    private void XuatPdf()
    {
        if (_cboNam.SelectedItem == null) return;
        var nam = (int)_cboNam.SelectedItem;
        using var dlg = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"ThongKe_{nam}.pdf" };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Msg.Run(() =>
            {
                ComputerStore.GUI.Services.PdfExporter.ExportThongKe(nam, dlg.FileName);
                Msg.Info("Đã xuất file PDF thành công.");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            });
        }
    }
}
