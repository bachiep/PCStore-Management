using ComputerStore.BLL;
using ComputerStore.GUI.Pages;

namespace ComputerStore.GUI.Forms;

public class frmMain : Form
{
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Theme.Page };
    private readonly Label _headerTitle = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private readonly Dictionary<string, PageBase> _pages = new();
    private string? _current;

    /// <summary>true nếu người dùng bấm Đăng xuất (Program sẽ hiện lại form đăng nhập).</summary>
    public bool LogoutRequested { get; private set; }

    public frmMain()
    {
        Text = "PC Store - Quản lý cửa hàng linh kiện & thiết bị máy tính";
        Name = "frmMain";
        Font = Theme.Base;
        BackColor = Theme.Page;
        MinimumSize = new Size(1100, 680);
        Size = new Size(1360, 820);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;

        Controls.Add(_content);
        Controls.Add(BuildHeader());
        Controls.Add(BuildSidebar());

        Shown += (_, _) => Navigate(PageRegistry.DefaultKey);
    }

    private Control BuildSidebar()
    {
        var side = new Panel { Dock = DockStyle.Left, Width = 236, BackColor = Theme.Sidebar };
        var scroll = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Sidebar, Padding = new Padding(0, 8, 0, 0) };
        scroll.HorizontalScroll.Enabled = false;

        var brand = new FlowLayoutPanel { Height = 62, Width = 236, Margin = new Padding(0), Padding = new Padding(22, 16, 0, 0), WrapContents = false };
        var l1 = new Label { Text = "PC", Font = new Font("Segoe UI Black", 18f), ForeColor = Theme.Danger, AutoSize = true, Margin = new Padding(0), UseMnemonic = false };
        var l2 = new Label { Text = "STORE", Font = new Font("Segoe UI Semibold", 18f), ForeColor = Color.White, AutoSize = true, Margin = new Padding(4, 0, 0, 0), UseMnemonic = false };
        brand.Controls.Add(l1); brand.Controls.Add(l2);
        scroll.Controls.Add(brand);

        string? lastGroup = null;
        foreach (var item in PageRegistry.Items.Where(i => !i.AdminOnly || Session.IsAdmin))
        {
            if (item.Group != lastGroup)
            {
                lastGroup = item.Group;
                scroll.Controls.Add(new Label
                {
                    Text = item.Group.ToUpperInvariant(), Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#6B7280"),
                    Width = 236, Height = 32, Margin = new Padding(0), TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(22, 0, 0, 4)
                });
            }
            var btn = new Button
            {
                Text = item.Text, Name = "nav_" + item.Key, Width = 236, Height = 42, FlatStyle = FlatStyle.Flat, ForeColor = ColorTranslator.FromHtml("#D1D5DB"),
                BackColor = Theme.Sidebar, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(22, 0, 0, 0), Margin = new Padding(0),
                Font = Theme.Base, Cursor = Cursors.Hand, UseVisualStyleBackColor = false, UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            var key = item.Key;
            btn.Click += (_, _) => Navigate(key);
            _navButtons[item.Key] = btn;
            scroll.Controls.Add(btn);
        }
        var syncingWidths = false;
        void FitSidebarItems()
        {
            if (syncingWidths || scroll.IsDisposed) return;
            syncingWidths = true;
            try
            {
                var width = Math.Max(0, scroll.ClientSize.Width
                    - (scroll.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0)
                    - scroll.Padding.Horizontal);
                foreach (Control item in scroll.Controls)
                    if (item.Width != width) item.Width = width;
            }
            finally { syncingWidths = false; }
        }
        scroll.ClientSizeChanged += (_, _) => FitSidebarItems();
        scroll.Layout += (_, _) => FitSidebarItems();
        FitSidebarItems();
        side.Controls.Add(scroll);
        return side;
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White };
        header.Paint += (_, e) => { using var p = new Pen(Theme.Border); e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1); };

        _headerTitle.Font = Theme.H2; _headerTitle.AutoSize = true; _headerTitle.UseMnemonic = false; _headerTitle.Location = new Point(22, 16); _headerTitle.ForeColor = Theme.Text;

        var user = Session.CurrentUser!;
        var role = user.VaiTro == "Admin" ? "Quản trị viên" : "Nhân viên";
        var info = new Label
        {
            Text = $"{user.HoTen}  ·  {role}", Font = Theme.Base, ForeColor = Theme.Muted, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Name = "lblUser"
        };
        var btnPwd = Theme.GhostButton("Đổi mật khẩu", 140, (_, _) => { using var f = new frmDoiMatKhau(); f.ShowDialog(this); });
        var btnOut = Theme.GhostButton("Đăng xuất", 120, (_, _) =>
        {
            if (!Msg.Confirm("Bạn có chắc muốn đăng xuất?")) return;
            if (HasUnsavedChanges() && !Msg.Confirm("Bạn còn giỏ hàng hoặc phiếu đang nhập. Đăng xuất sẽ xóa các nội dung này. Bạn có muốn tiếp tục?")) return;
            LogoutRequested = true;
            Session.End();
            Close();
        });
        btnPwd.Anchor = btnOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnPwd.Name = "btnDoiMatKhau"; btnOut.Name = "btnDangXuat";
        btnPwd.Margin = btnOut.Margin = new Padding(0);

        header.Controls.AddRange(new Control[] { _headerTitle, info, btnPwd, btnOut });
        header.Resize += (_, _) =>
        {
            btnOut.Location = new Point(header.Width - btnOut.Width - 18, 12);
            btnPwd.Location = new Point(btnOut.Left - btnPwd.Width - 8, 12);
            info.Location = new Point(btnPwd.Left - info.Width - 18, 18);
        };
        FormClosing += (_, e) =>
        {
            if (!LogoutRequested && HasUnsavedChanges() && !Msg.Confirm("Bạn còn giỏ hàng hoặc phiếu đang nhập. Đóng ứng dụng sẽ xóa các nội dung này. Bạn có muốn tiếp tục?"))
                e.Cancel = true;
        };
        FormClosed += (_, _) =>
        {
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
        };
        return header;
    }

    /// <summary>Hiển thị trang theo khóa menu, giữ nháp trong phiên đăng nhập.</summary>
    public void Navigate(string key)
    {
        var item = PageRegistry.Items.FirstOrDefault(i => i.Key == key);
        if (item == null || (item.AdminOnly && !Session.IsAdmin)) return;
        if (_current == key) return;

        if (_current != null && _pages.TryGetValue(_current, out var currentPage)
            && currentPage.ConfirmDiscardOnNavigation && currentPage.HasUnsavedChanges)
        {
            if (!Msg.Confirm("Bạn có thay đổi chưa lưu ở trang hiện tại. Bỏ thay đổi và chuyển trang?")) return;
            currentPage.AcknowledgeDiscardedChanges();
            Msg.Run(currentPage.LoadData);
        }

        if (!_pages.TryGetValue(key, out var page))
        {
            var created = Msg.Run(() => item.Create(this));
            if (created == null) return;
            page = created;
            _pages[key] = page;
        }

        _content.SuspendLayout();
        _content.Controls.Clear();
        _current = key;
        _headerTitle.Text = item.Text;
        _content.Controls.Add(page);
        _content.ResumeLayout();
        foreach (var (k, b) in _navButtons)
        {
            var active = k == key;
            b.BackColor = active ? Theme.Accent : Theme.Sidebar;
            b.ForeColor = active ? Color.White : ColorTranslator.FromHtml("#D1D5DB");
            b.Font = active ? Theme.Bold : Theme.Base;
        }
        Msg.Run(page.LoadData);
    }

    private bool HasUnsavedChanges() => _pages.Values.Any(page => page.HasUnsavedChanges);

    public string? CurrentKey => _current;
    public Control ContentHost => _content;
}
