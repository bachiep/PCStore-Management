using ComputerStore.BLL;
using ComputerStore.DTO;
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
    private readonly ToolTip _donutLegendTip = new();
    private readonly TableLayoutPanel _pnlCards = new() { Dock = DockStyle.Top, Height = 106, ColumnCount = 4, RowCount = 1, Margin = new Padding(0) };
    private readonly FlowLayoutPanel _pnlDonutLegend = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Margin = new Padding(0),
        Padding = new Padding(4),
        BackColor = System.Drawing.Color.Transparent
    };
    private static readonly string[] DonutPaletteHex = { "#2563EB", "#10B981", "#F59E0B", "#8B5CF6", "#EC4899" };
    private bool _compactCards;

    public ucDashboard() : base("Dashboard")
    {
        Name = "ucDashboard";
        for (var i = 0; i < 4; i++) _pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        _pnlCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        
        _gridSapHet.AutoGenerateColumns = false;
        Theme.EmptyHint(_gridSapHet, "Không có sản phẩm nào sắp hết hàng.");
        _gridSapHet.Columns.AddRange(
            Theme.Col("TenSP", "Sản phẩm", weight: 2f),
            Theme.Col("TenDM", "Nhóm", weight: 1f),
            Theme.Col("SoLuongTon", "SL", weight: 0.5f, right: true));
        _gridSapHet.RowTemplate.Height = 32;
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
        Theme.EmptyHint(_gridTop, "Chưa có sản phẩm bán trong tháng này.");
        var topProductColumn = Theme.Col("TenSP", "Sản phẩm", weight: 2.2f);
        topProductColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var topCountColumn = Theme.Col("SoLuongBan", "SL bán", weight: 0.8f, right: true);
        topCountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        topCountColumn.Width = 48;
        topCountColumn.MinimumWidth = 44;
        _gridTop.Columns.AddRange(topProductColumn, topCountColumn);
        _gridTop.RowTemplate.Height = 32;
        _gridTop.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _gridTop.AllowUserToAddRows = false;
        _gridTop.ReadOnly = true;
        _gridTop.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && _gridTop.Rows[e.RowIndex].DataBoundItem is TopSanPhamDTO item)
                e.ToolTipText = item.TenSP;
        };

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
        var donutLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0), Padding = new Padding(0) };
        donutLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        donutLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        donutLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        donutLayout.Controls.Add(_plotDonut, 0, 0);
        donutLayout.Controls.Add(_pnlDonutLegend, 1, 0);
        pnlDonut.Controls.Add(donutLayout);
        _pnlDonutLegend.SizeChanged += (_, _) =>
        {
            foreach (Control row in _pnlDonutLegend.Controls)
                row.Width = Math.Max(90, _pnlDonutLegend.ClientSize.Width - 8);
        };

        // --- 5 Recent Invoices Panel ---
        _gridRecent.AutoGenerateColumns = false;
        Theme.EmptyHint(_gridRecent, "Chưa có hóa đơn bán hàng.");
        _gridRecent.Columns.AddRange(
            Theme.Col("MaHD", "Mã HĐ", weight: 0.5f),
            Theme.Col("TenKH", "Khách hàng", weight: 1.5f),
            Theme.Col("NgayLap", "Giờ bán", weight: 1.1f, format: "dd/MM/yy HH:mm"),
            Theme.Col("ThanhToan", "Tổng tiền", weight: 1.1f, format: "N0", right: true)
        );
        _gridRecent.RowTemplate.Height = 32;
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
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
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

        void ApplyResponsiveLayout()
        {
            var compact = _pnlCards.ClientSize.Width < 920;
            if (_compactCards == compact && _pnlCards.ColumnCount == (compact ? 2 : 4)) return;
            _compactCards = compact;
            _pnlCards.SuspendLayout();
            try
            {
                _pnlCards.ColumnCount = compact ? 2 : 4;
                _pnlCards.RowCount = compact ? 2 : 1;
                _pnlCards.Height = compact ? 190 : 106;
                _pnlCards.ColumnStyles.Clear();
                for (var i = 0; i < _pnlCards.ColumnCount; i++)
                    _pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / _pnlCards.ColumnCount));
                _pnlCards.RowStyles.Clear();
                for (var i = 0; i < _pnlCards.RowCount; i++)
                    _pnlCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / _pnlCards.RowCount));
                foreach (Control card in _pnlCards.Controls)
                {
                    if (card.Tag is int index)
                        _pnlCards.SetCellPosition(card, new TableLayoutPanelCellPosition(compact ? index % 2 : index, compact ? index / 2 : 0));
                }
            }
            finally { _pnlCards.ResumeLayout(true); }
        }

        _pnlCards.SizeChanged += (_, _) => ApplyResponsiveLayout();
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        ApplyResponsiveLayout();
    }

    private Components.ModernPanel DashboardCard(string title, string value, System.Drawing.Color color)
    {
        var p = new Components.ModernPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 12), BorderRadius = 8, ShowShadow = true };
        var lTitle = new System.Windows.Forms.Label
        {
            Text = title,
            Font = Theme.Base,
            ForeColor = Theme.Muted,
            AutoSize = false,
            UseMnemonic = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(18, 10),
            Height = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        var lValue = new System.Windows.Forms.Label
        {
            Text = value,
            Font = new System.Drawing.Font("Segoe UI Semibold", 19),
            ForeColor = color,
            AutoSize = false,
            UseMnemonic = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(18, 44),
            Height = 36,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        p.Controls.Add(lTitle); p.Controls.Add(lValue);
        var accentStrip = new Panel { Width = 6, Height = 40, Location = new Point(0, 24), BackColor = color };
        p.Controls.Add(accentStrip);
        p.SizeChanged += (_, _) =>
        {
            lTitle.Width = Math.Max(80, p.ClientSize.Width - 30);
            lValue.Width = Math.Max(80, p.ClientSize.Width - 30);
            var valueFontSize = p.ClientSize.Width < 220 ? 16f : 19f;
            if (lValue.Font.Size != valueFontSize)
            {
                var previousFont = lValue.Font;
                lValue.Font = new System.Drawing.Font("Segoe UI Semibold", valueFontSize);
                previousFont.Dispose();
            }
        };
        p.PerformLayout();
        return p;
    }

    private void AddDashboardCard(string title, string value, System.Drawing.Color color, int index)
    {
        var card = DashboardCard(title, value, color);
        card.Tag = index;
        var column = _compactCards ? index % 2 : index;
        var row = _compactCards ? index / 2 : 0;
        _pnlCards.Controls.Add(card, column, row);
    }

    public override void LoadData()
    {
        Msg.Run(() =>
        {
            var tq = _bll.TongQuan();
            while (_pnlCards.Controls.Count > 0) _pnlCards.Controls[0].Dispose();
            _pnlCards.Controls.Clear();
            AddDashboardCard("Doanh thu hôm nay", $"{tq.DoanhThuHomNay:N0} ₫", Theme.Accent, 0);
            AddDashboardCard("Số hóa đơn hôm nay", $"{tq.SoHoaDonHomNay}", Theme.Success, 1);
            AddDashboardCard("Doanh thu tháng này", $"{tq.DoanhThuThangNay:N0} ₫", Theme.Warning, 2);
            AddDashboardCard("Sản phẩm sắp hết", $"{tq.SoSanPhamSapHet}", Theme.Danger, 3);

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

            // Reserve space above the tallest bar so its value label never
            // touches or gets clipped by the plot frame.
            var maxRevenue = values.Length == 0 ? 0 : values.Max();
            _plot.Plot.Axes.SetLimitsY(0, maxRevenue > 0 ? maxRevenue * 1.2 : 1);
            
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
            _plotDonut.Plot.Legend.IsVisible = false;
            while (_pnlDonutLegend.Controls.Count > 0) _pnlDonutLegend.Controls[0].Dispose();
            _pnlDonutLegend.Controls.Clear();

            if (dataDm.Count > 0)
            {
                var chartData = dataDm.OrderByDescending(x => x.DoanhThu)
                    .Take(4)
                    .Select(x => (x.TenDM, x.DoanhThu))
                    .ToList();
                var otherRevenue = dataDm.Sum(x => x.DoanhThu) - chartData.Sum(x => x.DoanhThu);
                if (otherRevenue > 0) chartData.Add(("Khác", otherRevenue));

                var slices = new List<ScottPlot.PieSlice>();
                for (int i = 0; i < chartData.Count; i++)
                {
                    var item = chartData[i];
                    slices.Add(new ScottPlot.PieSlice
                    {
                        Value = (double)item.DoanhThu,
                        FillColor = ScottPlot.Color.FromHex(DonutPaletteHex[i % DonutPaletteHex.Length]),
                        Label = string.Empty,
                        LegendText = $"{item.TenDM}: {item.DoanhThu:N0} ₫"
                    });
                    AddDonutLegendItem(item.TenDM, item.DoanhThu, DonutPaletteHex[i % DonutPaletteHex.Length]);
                }

                var pie = _plotDonut.Plot.Add.Pie(slices);
                pie.DonutFraction = 0.55;
                pie.SliceLabelDistance = 0;
                _plotDonut.Plot.Title("Doanh thu theo danh mục");
                _plotDonut.Plot.Axes.Frameless();
                _plotDonut.Plot.HideGrid();
            }
            else
            {
                _plotDonut.Plot.Title("Chưa có doanh thu theo danh mục");
                _plotDonut.Plot.Axes.Frameless();
                _plotDonut.Plot.HideGrid();
            }
            _plotDonut.Refresh();
        });
    }

    private void AddDonutLegendItem(string category, decimal revenue, string colorHex)
    {
        var row = new Panel
        {
            Height = 34,
            Width = Math.Max(90, _pnlDonutLegend.ClientSize.Width - 8),
            Margin = new Padding(0, 0, 0, 1),
            BackColor = System.Drawing.Color.Transparent
        };
        var marker = new Panel
        {
            Width = 10,
            Height = 10,
            Location = new Point(2, 8),
            BackColor = ColorTranslator.FromHtml(colorHex)
        };
        var label = new System.Windows.Forms.Label
        {
            Text = category,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Theme.Text,
            AutoSize = false,
            AutoEllipsis = false,
            UseMnemonic = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(17, 0),
            Height = 32,
            Width = Math.Max(56, row.ClientSize.Width - 19),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        void FitCategoryLabel()
        {
            label.Width = Math.Max(56, row.ClientSize.Width - label.Left - 2);
            var measured = TextRenderer.MeasureText(category, label.Font, new Size(1000, label.Height),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            if (measured <= label.Width)
            {
                label.Text = category;
                return;
            }

            var words = category.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                var splitAt = 1;
                var bestWidth = int.MaxValue;
                for (var i = 1; i < words.Length; i++)
                {
                    var firstLine = string.Join(" ", words.Take(i));
                    var secondLine = string.Join(" ", words.Skip(i));
                    var firstWidth = TextRenderer.MeasureText(firstLine, label.Font, new Size(1000, label.Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
                    var secondWidth = TextRenderer.MeasureText(secondLine, label.Font, new Size(1000, label.Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
                    var width = Math.Max(firstWidth, secondWidth);
                    if (width < bestWidth) { bestWidth = width; splitAt = i; }
                }
                label.Text = string.Join(" ", words.Take(splitAt)) + Environment.NewLine + string.Join(" ", words.Skip(splitAt));
            }
            else
            {
                var midpoint = Math.Max(1, category.Length / 2);
                label.Text = category[..midpoint] + Environment.NewLine + category[midpoint..];
            }
        }
        _donutLegendTip.SetToolTip(label, $"{category}: {revenue:N0} ₫");
        row.Controls.Add(marker);
        row.Controls.Add(label);
        row.SizeChanged += (_, _) => FitCategoryLabel();
        FitCategoryLabel();
        _pnlDonutLegend.Controls.Add(row);
    }
}
