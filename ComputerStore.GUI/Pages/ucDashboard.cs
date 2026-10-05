using ComputerStore.BLL;
using ScottPlot.WinForms;
using ScottPlot;

namespace ComputerStore.GUI.Pages;

public class ucDashboard : PageBase
{
    private readonly ThongKeBLL _bll = new();
    private readonly FormsPlot _plot = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _gridSapHet = Theme.Grid();
    private readonly DataGridView _gridTop = Theme.Grid();
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
        pnlChart.Controls.Add(_plot);
        
        var pnlGrid = Theme.CardPanel(new Padding(1));
        pnlGrid.Dock = DockStyle.Fill;
        pnlGrid.Margin = new Padding(14, 0, 0, 12);
        var lblSapHet = new System.Windows.Forms.Label { Text = "Sản phẩm sắp hết hàng", Font = Theme.H2, AutoSize = false, Height = 44, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
        lblSapHet.Dock = DockStyle.Top;
        _gridSapHet.Dock = DockStyle.Fill;
        pnlGrid.Controls.Add(_gridSapHet);
        pnlGrid.Controls.Add(lblSapHet);

        _gridTop.AutoGenerateColumns = false;
        _gridTop.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm", weight: 2.5f),
            Theme.Col("SoLuongBan", "Đã bán", weight: 1f, right: true));
        _gridTop.AllowUserToAddRows = false; _gridTop.ReadOnly = true;

        var pnlGridTop = Theme.CardPanel(new Padding(1));
        pnlGridTop.Dock = DockStyle.Fill;
        pnlGridTop.Margin = new Padding(14, 0, 0, 12);
        var lblTop = new System.Windows.Forms.Label { Text = "Bán chạy tháng này", Font = Theme.H2, AutoSize = false, Height = 44, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
        lblTop.Dock = DockStyle.Top;
        _gridTop.Dock = DockStyle.Fill;
        pnlGridTop.Controls.Add(_gridTop);
        pnlGridTop.Controls.Add(lblTop);

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        table.Controls.Add(pnlChart, 0, 0);
        table.Controls.Add(pnlGridTop, 1, 0);
        table.Controls.Add(pnlGrid, 2, 0);

        AddRow(_pnlCards);
        AddRow(table, fill: true);
    }

    private Panel DashboardCard(string title, string value, System.Drawing.Color color)
    {
        var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 12), BackColor = System.Drawing.Color.White };
        var lTitle = new System.Windows.Forms.Label { Text = title, Font = Theme.Base, ForeColor = Theme.Muted, AutoSize = true, UseMnemonic = false, Location = new Point(20, 16) };
        var lValue = new System.Windows.Forms.Label { Text = value, Font = new System.Drawing.Font("Segoe UI Semibold", 19), ForeColor = color, AutoSize = true, UseMnemonic = false, Location = new Point(20, 44) };
        p.Controls.Add(lTitle); p.Controls.Add(lValue);
        p.Controls.Add(new Panel { Width = 5, Dock = DockStyle.Left, BackColor = color });
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
                    txt.LabelFontSize = 11;
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
        });
    }
}
