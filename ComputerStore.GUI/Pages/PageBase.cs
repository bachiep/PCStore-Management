using ComputerStore.BLL;

namespace ComputerStore.GUI;

/// <summary>Lớp cơ sở cho mọi màn hình nội dung (UserControl) hiển thị trong frmMain.</summary>
public abstract class PageBase : UserControl
{
    protected readonly TableLayoutPanel Root;
    public string PageTitle { get; }

    protected PageBase(string title)
    {
        PageTitle = title;
        Dock = DockStyle.Fill;
        BackColor = Theme.Page;
        Font = Theme.Base;
        Padding = new Padding(22, 18, 22, 14);
        DoubleBuffered = true;

        Root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = Color.Transparent };
        Root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(Root);
    }

    protected static bool IsAdmin => Session.IsAdmin;

    /// <summary>Thêm một hàng vào bố cục dọc; fill = true thì hàng chiếm phần không gian còn lại.</summary>
    protected void AddRow(Control c, bool fill = false)
    {
        Root.RowCount++;
        Root.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
        c.Dock = DockStyle.Fill;
        Root.Controls.Add(c, 0, Root.RowCount - 1);
    }

    /// <summary>Tải (hoặc tải lại) dữ liệu hiển thị. frmMain gọi sau khi tạo trang.</summary>
    public virtual void LoadData() { }

    protected static bool Ask(string text) => Msg.Confirm(text);

    /// <summary>Bọc lưới dữ liệu trong khung trắng có viền.</summary>
    protected static Panel GridCard(DataGridView grid)
    {
        var p = Theme.CardPanel(new Padding(1));
        p.Margin = new Padding(0);
        p.Dock = DockStyle.Fill;
        grid.Dock = DockStyle.Fill;
        p.Controls.Add(grid);
        return p;
    }

    protected static Panel EditorCard(string title, params Control[] controls)
    {
        var p = Theme.CardPanel(new Padding(24, 20, 20, 20));
        p.Margin = new Padding(16, 0, 0, 0);
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        flow.Controls.Add(new Label { Text = title, Font = Theme.H2, AutoSize = true, UseMnemonic = false, Margin = new Padding(0, 0, 0, 16), ForeColor = Theme.Text });
        foreach (var c in controls) 
        {
            if (c.Margin.Bottom == 0) c.Margin = new Padding(c.Margin.Left, c.Margin.Top, c.Margin.Right, 12);
            flow.Controls.Add(c);
        }
        void Stretch()
        {
            var w = flow.ClientSize.Width;
            foreach (Control c in flow.Controls)
                if (c.Tag as string == "fill") c.Width = Math.Max(120, w - c.Margin.Horizontal - 4);
        }
        flow.Layout += (_, _) => Stretch();
        flow.HorizontalScroll.Maximum = 0; flow.AutoScroll = false; flow.VerticalScroll.Visible = false; flow.AutoScroll = true;
        p.Controls.Add(flow);
        return p;
    }

    /// <summary>Bố cục 2 cột: trái co giãn, phải cố định.</summary>
    protected static TableLayoutPanel TwoCols(Control left, Control right, int rightWidth = 360)
    {
        var t = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = new Padding(0), Dock = DockStyle.Fill };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rightWidth));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Dock = DockStyle.Fill; right.Dock = DockStyle.Fill;
        t.Controls.Add(left, 0, 0);
        t.Controls.Add(right, 1, 0);
        return t;
    }

    /// <summary>Thanh công cụ nằm ngang (ô tìm kiếm, nút bấm).</summary>
    protected static FlowLayoutPanel Toolbar(params Control[] controls)
    {
        var f = Theme.Row(controls);
        f.Margin = new Padding(0, 0, 0, 10);
        return f;
    }

    /// <summary>Gọi hành động sau khi người dùng ngừng gõ 300 ms (tìm kiếm không giật).</summary>
    protected static void Debounce(TextBox box, Action action)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 300 };
        timer.Tick += (_, _) => { timer.Stop(); action(); };
        box.TextChanged += (_, _) => { timer.Stop(); timer.Start(); };
        box.Disposed += (_, _) => timer.Dispose();
    }

    /// <summary>Nút "Xuất Excel" cho một lưới dữ liệu.</summary>
    protected static Button ExportButton(DataGridView grid, string title)
        => Theme.GhostButton("Xuất Excel", 100, (_, _) =>
        {
            if (grid.Rows.Count == 0) { Msg.Warn("Không có dữ liệu để xuất."); return; }
            using var dlg = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"{title}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            Msg.Run(() =>
            {
                ComputerStore.GUI.Services.ExcelExporter.ExportGrid(grid, title, dlg.FileName);
                Msg.Info("Đã xuất Excel thành công.");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            });
        });

    protected static int ParseInt(string text, string field)
        => int.TryParse(text.Trim().Replace(".", ""), out var v) ? v : throw new BusinessException($"{field} phải là số nguyên hợp lệ.");

    protected static decimal ParseMoney(string text, string field)
        => decimal.TryParse(text.Trim().Replace(".", "").Replace(",", ""), out var v) ? v : throw new BusinessException($"{field} phải là số hợp lệ.");
}
