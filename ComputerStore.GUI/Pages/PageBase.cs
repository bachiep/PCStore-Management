using ComputerStore.BLL;
using System.Runtime.CompilerServices;

namespace ComputerStore.GUI;

/// <summary>Lớp cơ sở cho mọi màn hình nội dung (UserControl) hiển thị trong frmMain.</summary>
public abstract class PageBase : UserControl
{
    private sealed class DebounceState
    {
        public required System.Windows.Forms.Timer Timer { get; init; }
        public bool SuppressTextChanged { get; set; }
    }

    private static readonly ConditionalWeakTable<TextBox, DebounceState> DebounceStates = new();
    protected readonly TableLayoutPanel Root;
    public string PageTitle { get; }

    public virtual bool HasUnsavedChanges => false;
    public virtual bool ConfirmDiscardOnNavigation => false;

    private bool _trackingSuppressed;
    private bool _hasTrackedChanges;
    protected bool HasTrackedChanges => _hasTrackedChanges;

    /// <summary>Tracks edits in form controls so navigation can warn before discarding a draft.</summary>
    protected void TrackChanges(params Control[] controls)
    {
        void Changed(object? sender, EventArgs e)
        {
            if (!_trackingSuppressed) _hasTrackedChanges = true;
        }

        foreach (var control in controls)
        {
            if (control is TextBoxBase text) text.TextChanged += Changed;
            if (control is ComboBox combo) combo.SelectedIndexChanged += Changed;
            if (control is CheckBox checkBox) checkBox.CheckedChanged += Changed;
            if (control is DateTimePicker dateTimePicker) dateTimePicker.ValueChanged += Changed;
        }
    }

    /// <summary>Sets editor values from loaded data without marking them as user edits.</summary>
    protected void SetTrackedValues(Action setValues)
    {
        _trackingSuppressed = true;
        try { setValues(); }
        finally
        {
            _hasTrackedChanges = false;
            _trackingSuppressed = false;
        }
    }

    protected void MarkTrackedChangesClean() => _hasTrackedChanges = false;
    protected void MarkTrackedChangesDirty() => _hasTrackedChanges = true;
    public virtual void AcknowledgeDiscardedChanges() => _hasTrackedChanges = false;

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
        var p = Theme.CardPanel(new Padding(24, 20, 24, 20));
        p.Margin = new Padding(16, 0, 0, 0);

        Control? lastControl = controls.Length > 0 ? controls[^1] : null;
        var bottomPanel = lastControl as FlowLayoutPanel;
        if (bottomPanel != null)
        {
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Padding = new Padding(0, 10, 0, 0);
        }

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        flow.Controls.Add(new Label { Text = title, Font = Theme.H2, AutoSize = true, UseMnemonic = false, Margin = new Padding(0, 0, 0, 16), ForeColor = Theme.Text });
        foreach (var c in controls) 
        {
            if (c == lastControl && lastControl is FlowLayoutPanel) continue;
            if (c.Margin.Bottom == 0) c.Margin = new Padding(c.Margin.Left, c.Margin.Top, c.Margin.Right, 12);
            flow.Controls.Add(c);
        }
        void Stretch()
        {
            var w = Math.Max(120, flow.ClientSize.Width - flow.Padding.Horizontal);
            foreach (Control c in flow.Controls)
            {
                // Field panels and paired field tables must follow the editor width.
                // Keeping their design-time widths caused a horizontal scrollbar in
                // the product editor even when the form had enough vertical space.
                if (c is Panel || string.Equals(c.Tag as string, "fill", StringComparison.Ordinal))
                    c.Width = Math.Max(120, w - c.Margin.Horizontal);
            }
        }
        flow.Layout += (_, _) => Stretch();
        // Add the fill region first and footer second. WinForms docks overlapping
        // controls according to z-order; the previous order let the fill panel
        // cover action rows such as Save, Delete, and Warranty update.
        p.Controls.Add(flow);
        if (bottomPanel != null) p.Controls.Add(bottomPanel);
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
        var state = new DebounceState { Timer = new System.Windows.Forms.Timer { Interval = 300 } };
        state.Timer.Tick += (_, _) => { state.Timer.Stop(); if (!box.IsDisposed) action(); };
        box.TextChanged += (_, _) =>
        {
            if (state.SuppressTextChanged) return;
            state.Timer.Stop();
            state.Timer.Start();
        };
        box.Disposed += (_, _) => state.Timer.Dispose();
        DebounceStates.Add(box, state);
    }

    /// <summary>Sets a search value without allowing a pending/programmatic change to trigger a second search.</summary>
    protected static void SetSearchWithoutDebounce(TextBox box, string value)
    {
        if (!DebounceStates.TryGetValue(box, out var state))
        {
            box.Text = value;
            return;
        }

        state.Timer.Stop();
        state.SuppressTextChanged = true;
        try { box.Text = value; }
        finally { state.SuppressTextChanged = false; }
    }

    /// <summary>Runs a pending debounced search now so keyboard actions use the current query results.</summary>
    protected static void FlushDebounce(TextBox box, Action action)
    {
        if (!DebounceStates.TryGetValue(box, out var state) || !state.Timer.Enabled) return;
        state.Timer.Stop();
        if (!box.IsDisposed) action();
    }

    /// <summary>Nút "Xuất Excel" cho một lưới dữ liệu.</summary>
    protected static Components.ModernButton ExportButton(DataGridView grid, string title, int width = 100)
        => Theme.GhostButton("Xuất Excel", width, (_, _) =>
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
