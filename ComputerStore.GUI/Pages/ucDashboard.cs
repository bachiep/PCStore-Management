using ComputerStore.BLL;
using ScottPlot.WinForms;
using ScottPlot;

namespace ComputerStore.GUI.Pages;

public class ucDashboard : PageBase
{
    private readonly ThongKeBLL _bll = new();
    private readonly FormsPlot _plot = new() { Dock = DockStyle.Fill };
    private readonly FormsPlot _plotDonut = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _gridSapHet = Theme.Grid();
    private readonly DataGridView _gridTop = Theme.Grid();
    private readonly DataGridView _gridRecent = Theme.Grid();
    private readonly TableLayoutPanel _pnlCards = new() { Dock = DockStyle.Top, Height = 106, ColumnCount = 4, RowCount = 1, Margin = new Padding(0) };

    public ucDashboard() : base("Dashboard")
    {
        Name = "ucDashboard";
        for (var i = 0; i < 4; i++) _pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        _pnlCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        
        _gridSapHet.AutoGenerateColumns = false;
        _gridSapHet.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm", weight: 2f),
            Theme.Col("TenDM", "Danh mục", weight: 1f),
            Theme.Col("SoLuongTon", "SL", weight: 0.5f, right: true));
        _gridSapHet.AllowUserToAddRows = false;
        _gridSapHet.ReadOnly = true;

        var pnlChart = Theme.CardPanel();
        pnlChart.Dock = DockStyle.Fill;
        pnlChart.Margin = new Padding(0, 0, 14, 12);
        pnlChart.Controls.Add(_plot);
        
        var pnlGrid = Theme.CardPanel(new Padding(1));
        pnlGrid.Dock = DockStyle.Fill;
        pnlGrid.Margin = new Padding(0, 0, 0, 12);
        var lblSapHet = new System.Windows.Forms.Label { Text = "Sản phẩm sắp hết hàng", Font = Theme.H2, AutoSize = false, Height = 40, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
        lblSapHet.Dock = DockStyle.Top;
        _gridSapHet.Dock = DockStyle.Fill;
        pnlGrid.Controls.Add(_gridSapHet);
        pnlGrid.Controls.Add(lblSapHet);

        _gridTop.AutoGenerateColumns = false;
        _gridTop.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm", weight: 2.2f),
            Theme.Col("SoLuongBan", "Đã bán", weight: 0.8f, right: true));
        _gridTop.AllowUserToAddRows = false;
        _gridTop.ReadOnly = true;

        var pnlGridTop = Theme.CardPanel(new Padding(1));
        pnlGridTop.Dock = DockStyle.Fill;
        pnlGridTop.Margin = new Padding(0, 0, 14, 12);
        var lblTop = new System.Windows.Forms.Label { Text = "Bán chạy tháng này", Font = Theme.H2, AutoSize = false, Height = 40, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
        lblTop.Dock = DockStyle.Top;
        _gridTop.Dock = DockStyle.Fill;
        pnlGridTop.Controls.Add(_gridTop);
        pnlGridTop.Controls.Add(lblTop);

        // --- Donut Chart Panel ---
        var pnlDonut = Theme.CardPanel();
        pnlDonut.Dock = DockStyle.Fill;
        pnlDonut.Margin = new Padding(0, 0, 14, 0);
        pnlDonut.Controls.Add(_plotDonut);

        // --- 5 Recent Invoices Panel ---
        _gridRecent.AutoGenerateColumns = false;
        _gridRecent.Columns.AddRange(
            Theme.Col("MaHD", "Mã HĐ", weight: 0.5f),
            Theme.Col("TenKH", "Khách hàng", weight: 1.5f),
            Theme.Col("NgayLap", "Giờ bán", weight: 1.1f, format: "dd/MM/yyyy HH:mm"),
            Theme.Col("ThanhToan", "Tổng tiền", weight: 1.1f, format: "N0", right: true)
        );
        _gridRecent.AllowUserToAddRows = false;
        _gridRecent.ReadOnly = true;

        var pnlRecent = Theme.CardPanel(new Padding(1));
        pnlRecent.Dock = DockStyle.Fill;
        pnlRecent.Margin = new Padding(0);
        var lblRecent = new System.Windows.Forms.Label { Text = "5 Hóa đơn bán gần nhất", Font = Theme.H2, AutoSize = false, Height = 40, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
        lblRecent.Dock = DockStyle.Top;
        _gridRecent.Dock = DockStyle.Fill;
        pnlRecent.Controls.Add(_gridRecent);
        pnlRecent.Controls.Add(lblRecent);

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Margin = new Padding(0) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 48));

        // Row 0
        table.Controls.Add(pnlChart, 0, 0);
        table.Controls.Add(pnlGridTop, 1, 0);
        table.Controls.Add(pnlGrid, 2, 0);

        // Row 1
        table.Controls.Add(pnlDonut, 0, 1);
        table.Controls.Add(pnlRecent, 1, 1);
        table.SetColumnSpan(pnlRecent, 2);

        AddRow(_pnlCards);
        AddRow(table, fill: true);
    }

    private Components.ModernPanel DashboardCard(string title, string value, System.Drawing.Color color)
    {
        var p = new Components.ModernPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 12), BorderRadius = 8, ShowShadow = true };
        var lTitle = new System.Windows.Forms.Label { Text = title, Font = Theme.Base, ForeColor = Theme.Muted, AutoSize = true, UseMnemonic = false, Location = new Point(24, 16) };
        var lValue = new System.Windows.Forms.Label { Text = value, Font = new System.Drawing.Font("Segoe UI Semibold", 19), ForeColor = color, AutoSize = true, UseMnemonic = false, Location = new Point(22, 44) };
        p.Controls.Add(lTitle); p.Controls.Add(lValue);
        var accentStrip = new Panel { Width = 6, Height = 40, Location = new Point(0, 24), BackColor = color };
        p.Controls.Add(accentStrip);
        return p;
    }

    public override void LoadData()
    {
        Msg.Run(() =>
        {
            var tq = _bll.TongQuan();
            while (_pnlCards.Controls.Count > 0) _pnlCards.Controls[0].Dispose();
            _pnlCards.Controls.Clear();
            _pnlCards.Controls.Add(DashboardCard("Doanh thu hôm nay", $"{tq.DoanhThuHomNay:N0} ₫", Theme.Accent), 0, 0);
            _pnlCards.Controls.Add(DashboardCard("Số hóa đơn hôm nay", $"{tq.SoHoaDonHomNay}", Theme.Success), 1, 0);
            _pnlCards.Controls.Add(DashboardCard("Doanh thu tháng này", $"{tq.DoanhThuThangNay:N0} ₫", Theme.Warning), 2, 0);
            _pnlCards.Controls.Add(DashboardCard("Sản phẩm sắp hết", $"{tq.SoSanPhamSapHet}", Theme.Danger), 3, 0);

            _gridSapHet.DataSource = _bll.SanPhamSapHet();
            
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var topData = _bll.TopBanChay(startOfMonth, today, 10);
            _gridTop.DataSource = topData;

            // 5 Hóa đơn bán gần nhất
            _gridRecent.DataSource = _bll.HoaDonGanNhat(5);

            // Bar chart: Doanh thu 7 ngày gần nhất
            var data7Ngay = _bll.DoanhThu7Ngay();
            double[] values = data7Ngay.Select(x => (double)x.DoanhThu / 1000000.0).ToArray();
            
            _plot.Plot.Clear();
            _plot.Plot.FigureBackground.Color = ScottPlot.Colors.White;
            var bar = _plot.Plot.Add.Bars(values);
            foreach (var b in bar.Bars) b.FillColor = ScottPlot.Color.FromHex("#3B82F6");
            
            for (int i = 0; i < values.Length; i++)
            {
                if (data7Ngay[i].DoanhThu > 0)
                {
                    var txt = _plot.Plot.Add.Text(data7Ngay[i].DoanhThu.ToString("N0") + " ₫", i, values[i]);
                    txt.LabelFontSize = 10;
                    txt.LabelBold = true;
                    txt.LabelAlignment = ScottPlot.Alignment.LowerCenter;
                    txt.LabelFontColor = ScottPlot.Color.FromHex("#1E3A8A");
                }
            }
            
            var positions = data7Ngay.Select((x, i) => (double)i).ToArray();
            var labels = data7Ngay.Select(x => x.Nhan).ToArray();
            _plot.Plot.Axes.Bottom.SetTicks(positions, labels);
            _plot.Plot.Title("Doanh thu 7 ngày gần nhất");
            _plot.Plot.YLabel("Doanh thu (Triệu ₫)");
            _plot.Refresh();

            // Donut chart: Cơ cấu doanh thu theo Danh mục
            var dataDm = _bll.DoanhThuTheoDanhMuc();
            _plotDonut.Plot.Clear();
            _plotDonut.Plot.FigureBackground.Color = ScottPlot.Colors.White;

            if (dataDm.Count > 0)
            {
                var palette = new ScottPlot.Color[]
                {
                    ScottPlot.Color.FromHex("#2563EB"),
                    ScottPlot.Color.FromHex("#10B981"),
                    ScottPlot.Color.FromHex("#F59E0B"),
                    ScottPlot.Color.FromHex("#8B5CF6"),
                    ScottPlot.Color.FromHex("#EC4899"),
                    ScottPlot.Color.FromHex("#06B6D4"),
                    ScottPlot.Color.FromHex("#EF4444"),
                    ScottPlot.Color.FromHex("#64748B")
                };

                var slices = new List<ScottPlot.PieSlice>();
                for (int i = 0; i < dataDm.Count; i++)
                {
                    var item = dataDm[i];
                    slices.Add(new ScottPlot.PieSlice
                    {
                        Value = (double)item.DoanhThu,
                        FillColor = palette[i % palette.Length],
                        LegendText = $"{item.TenDM}: {item.TyLe:0.#}% ({item.DoanhThu:N0} ₫)"
                    });
                }

                var pie = _plotDonut.Plot.Add.Pie(slices);
                pie.DonutFraction = 0.55;
                _plotDonut.Plot.ShowLegend(ScottPlot.Alignment.UpperRight);
                _plotDonut.Plot.Title("Cơ cấu doanh thu theo Danh mục");
                _plotDonut.Plot.Axes.Frameless();
                _plotDonut.Plot.HideGrid();
            }
            else
            {
                _plotDonut.Plot.Title("Cơ cấu doanh thu theo Danh mục (Chưa có dữ liệu)");
                _plotDonut.Plot.Axes.Frameless();
                _plotDonut.Plot.HideGrid();
            }
            _plotDonut.Refresh();
        });
    }
}
