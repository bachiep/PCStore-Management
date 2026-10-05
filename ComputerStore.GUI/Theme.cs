using System.Globalization;
using System.Reflection;
using ComputerStore.BLL;

namespace ComputerStore.GUI;

/// <summary>Bảng màu, phông chữ và hàm dựng control dùng chung cho toàn bộ giao diện.</summary>
public static class Theme
{
    public static readonly CultureInfo Vi = new("vi-VN");

    public static readonly Color Sidebar = ColorTranslator.FromHtml("#111827");
    public static readonly Color SidebarHover = ColorTranslator.FromHtml("#1F2937");
    public static readonly Color Accent = ColorTranslator.FromHtml("#2563EB");
    public static readonly Color AccentDark = ColorTranslator.FromHtml("#1D4ED8");
    public static readonly Color Page = ColorTranslator.FromHtml("#F3F4F6");
    public static readonly Color Card = Color.White;
    public static readonly Color Text = ColorTranslator.FromHtml("#111827");
    public static readonly Color Muted = ColorTranslator.FromHtml("#6B7280");
    public static readonly Color Border = ColorTranslator.FromHtml("#E5E7EB");
    public static readonly Color Danger = ColorTranslator.FromHtml("#DC2626");
    public static readonly Color Success = ColorTranslator.FromHtml("#16A34A");
    public static readonly Color Warning = ColorTranslator.FromHtml("#D97706");

    public static readonly Font Base = new("Segoe UI", 10f);
    public static readonly Font Bold = new("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font H1 = new("Segoe UI Semibold", 17f);
    public static readonly Font H2 = new("Segoe UI Semibold", 12f);
    public static readonly Font Big = new("Segoe UI Semibold", 20f);

    /// <summary>Định dạng tiền: 1.025.845 ₫</summary>
    public static string Money(decimal v) => v.ToString("N0", Vi) + " ₫";
    public static string Num(decimal v) => v.ToString("N0", Vi);
    public static string DateTimeText(DateTime d) => d.ToString("dd/MM/yyyy HH:mm", Vi);
    public static string DateText(DateTime d) => d.ToString("dd/MM/yyyy", Vi);

    // ---------------- Control factories ----------------

    public static Button PrimaryButton(string text, int width = 110, EventHandler? onClick = null)
        => MakeButton(text, Accent, Color.White, width, onClick);

    public static Button DangerButton(string text, int width = 110, EventHandler? onClick = null)
        => MakeButton(text, Danger, Color.White, width, onClick);

    public static Button GhostButton(string text, int width = 110, EventHandler? onClick = null)
        => MakeButton(text, Color.White, Text, width, onClick, true);

    private static Button MakeButton(string text, Color back, Color fore, int width, EventHandler? onClick, bool border = false)
    {
        var b = new Button
        {
            Text = text, Width = width, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore,
            Font = Bold, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0), UseVisualStyleBackColor = false,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly, MinimumSize = new Size(width, 34), Padding = new Padding(8, 0, 8, 0)
        };
        b.FlatAppearance.BorderSize = border ? 1 : 0;
        b.FlatAppearance.BorderColor = Border;
        var hover = border ? ColorTranslator.FromHtml("#F9FAFB") : ControlPaint.Dark(back, 0.08f);
        b.FlatAppearance.MouseOverBackColor = hover;
        b.FlatAppearance.MouseOverBackColor = hover;
        if (onClick != null) b.Click += onClick;
        return b;
    }

    public static Button LinkButton(string text, EventHandler? onClick = null)
    {
        var b = new Button
        {
            Text = text, FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = AccentDark,
            Font = Small, Cursor = Cursors.Hand, Margin = new Padding(8, 0, 0, 0), UseVisualStyleBackColor = false,
            AutoSize = true, Padding = new Padding(0), Height = 25
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#E0E7FF");
        b.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#C7D2FE");
        if (onClick != null) b.Click += onClick;
        return b;
    }

    public static TextBox Input(int width = 200, string placeholder = "")
    {
        var t = new TextBox { Width = width, Font = Base, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 8, 0) };
        if (!string.IsNullOrEmpty(placeholder)) t.PlaceholderText = placeholder;
        return t;
    }

    public static ComboBox Combo(int width = 200)
        => new() { Width = width, Font = Base, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Standard, BackColor = Color.White, Margin = new Padding(0, 0, 8, 0) };

    public static Label Caption(string text, bool muted = true)
        => new() { Text = text, AutoSize = true, Font = muted ? Small : Base, ForeColor = muted ? Muted : Text, Margin = new Padding(0, 4, 8, 2) };

    public static Label Title(string text)
        => new() { Text = text, AutoSize = true, UseMnemonic = false, Font = H1, ForeColor = Text, Margin = new Padding(0, 0, 0, 6) };

    public static Panel CardPanel(Padding? padding = null)
    {
        var p = new Panel { BackColor = Card, Padding = padding ?? new Padding(14), Margin = new Padding(0, 0, 0, 12) };
        p.Paint += (_, e) => { using var pen = new Pen(Border); e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };
        return p;
    }

    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var f = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = new Padding(0), BackColor = Color.Transparent };
        f.Controls.AddRange(controls);
        return f;
    }

    /// <summary>Nhãn + ô nhập xếp dọc, dùng cho form chi tiết. Tag = "fill" để EditorCard tự giãn theo chiều rộng khung.</summary>
    public static Control Field(string caption, Control input, int width = 220)
    {
        var f = new Panel { Width = width, Margin = new Padding(0, 0, 12, 14), BackColor = Color.Transparent, Tag = "fill" };
        var cap = new Label { Text = caption, AutoSize = false, Height = 22, Dock = DockStyle.Top, Font = Small, ForeColor = Muted, UseMnemonic = false, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 4) };
        input.Margin = new Padding(0);
        input.Dock = DockStyle.Top;
        f.Controls.Add(input);
        f.Controls.Add(cap);
        f.Height = cap.Height + input.Height + 4;
        return f;
    }

    /// <summary>Hai Field đứng cạnh nhau, chia đều chiều rộng khung chứa.</summary>
    public static Control Pair(Control a, Control b)
    {
        var t = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 12, 0), Tag = "fill", Width = 300, BackColor = Color.Transparent };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var h = Math.Max(a.Height, b.Height);
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        t.Height = h;
        a.Dock = b.Dock = DockStyle.Fill;
        a.Margin = new Padding(0, 0, 6, 0); b.Margin = new Padding(6, 0, 0, 0);
        a.Tag = b.Tag = null;
        t.Controls.Add(a, 0, 0); t.Controls.Add(b, 1, 0);
        return t;
    }

    /// <summary>Hiện dòng chữ mờ ở giữa lưới khi chưa có dòng dữ liệu nào.</summary>
    public static void EmptyHint(DataGridView g, string text)
    {
        g.Paint += (_, e) =>
        {
            if (g.Rows.Count > 0) return;
            var r = new Rectangle(0, g.ColumnHeadersHeight, g.Width, Math.Max(0, g.Height - g.ColumnHeadersHeight));
            TextRenderer.DrawText(e.Graphics, text, Base, r, Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
    }

    public static DataGridView Grid()
    {
        var g = new DataGridView();
        StyleGrid(g);
        return g;
    }

    public static void StyleGrid(DataGridView g)
    {
        g.Dock = DockStyle.Fill;
        g.BackgroundColor = Color.White;
        g.BorderStyle = BorderStyle.None;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.ReadOnly = true;
        g.MultiSelect = false;
        g.RowHeadersVisible = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersHeight = 40; // Tiêu đề cao hơn
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F1F5F9"); // Màu nền tiêu đề đậm hơn 1 chút
        g.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#F1F5F9");
        g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
        g.ColumnHeadersDefaultCellStyle.Font = Bold;
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        g.DefaultCellStyle.Font = Base;
        g.DefaultCellStyle.ForeColor = Text;
        g.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DBEAFE");
        g.DefaultCellStyle.SelectionForeColor = Text;
        g.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0); // Tăng padding
        
        g.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F8FAFC"); // Màu nền xen kẽ để dễ đọc

        g.RowTemplate.Height = 36; // Tăng chiều cao dòng cho thoáng
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.GridColor = Border;
        typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, g, new object[] { true });
    }

    public static DataGridViewTextBoxColumn Col(string name, string header, string? dataProp = null, float weight = 1f,
        string? format = null, bool right = false)
    {
        var c = new DataGridViewTextBoxColumn
        {
            Name = name, HeaderText = header, DataPropertyName = dataProp ?? name, FillWeight = weight * 100f, SortMode = DataGridViewColumnSortMode.Automatic
        };
        c.MinimumWidth = weight < 0.6f ? 42 : 60;
        if (format != null) c.DefaultCellStyle.Format = format;
        c.DefaultCellStyle.FormatProvider = Vi;
        // Xóa căn phải, để mọi thứ sát bên trái như yêu cầu
        return c;
    }
}

/// <summary>Hộp thoại thông báo thống nhất.</summary>
public static class Msg
{
    public static void Info(string text) => MessageBox.Show(text, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    public static void Warn(string text) => MessageBox.Show(text, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public static void Error(string text) => MessageBox.Show(text, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    public static bool Confirm(string text) => MessageBox.Show(text, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>Chạy một hành động; lỗi nghiệp vụ hiện cảnh báo, lỗi hệ thống hiện thông báo lỗi. Trả về true nếu thành công.</summary>
    public static bool Run(Action action)
    {
        try { action(); return true; }
        catch (BusinessException ex) { Warn(ex.Message); }
        catch (MySqlConnector.MySqlException ex) { Error("Lỗi cơ sở dữ liệu: " + ex.Message); }
        catch (Exception ex) { Error("Có lỗi xảy ra: " + ex.Message); }
        return false;
    }

    public static T? Run<T>(Func<T> func)
    {
        T? result = default;
        Run(() => { result = func(); });
        return result;
    }
}
