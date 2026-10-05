using ComputerStore.BLL;

namespace ComputerStore.GUI.Forms;

public class frmDangNhap : Form
{
    private readonly TextBox _user = new();
    private readonly TextBox _pass = new();
    private readonly Button _btnLogin;
    private readonly Label _error = new();
    private readonly CheckBox _show = new();
    private readonly TaiKhoanBLL _bll = new();

    public frmDangNhap()
    {
        Text = "Đăng nhập - PC Store";
        ClientSize = new Size(820, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = Theme.Base;
        BackColor = Color.White;
        Name = "frmDangNhap";

        // ---- Bên trái: thương hiệu ----
        var left = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = Theme.Sidebar };
        try 
        { 
            left.BackgroundImage = Image.FromFile(@"Resources\bg_login.jpg"); 
            left.BackgroundImageLayout = ImageLayout.Stretch; 
        } 
        catch { }

        // Phủ lớp nền đen bán trong suốt lên trên ảnh
        var overlay = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(140, 0, 0, 0) };
        left.Controls.Add(overlay);

        var brand = new FlowLayoutPanel { Location = new Point(40, 140), AutoSize = true, BackColor = Color.Transparent };
        var l1 = new Label { Text = "PC", Font = new Font("Segoe UI Black", 32f), ForeColor = Theme.Danger, AutoSize = true, Margin = new Padding(0), UseMnemonic = false };
        var l2 = new Label { Text = "STORE", Font = new Font("Segoe UI", 32f, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Margin = new Padding(6, 0, 0, 0), UseMnemonic = false };
        brand.Controls.Add(l1); brand.Controls.Add(l2);
        overlay.Controls.Add(brand);
        overlay.Controls.Add(new Label
        {
            Text = "Hệ thống quản lý\r\ncửa hàng linh kiện\r\nvà thiết bị máy tính.", Font = new Font("Segoe UI", 13f), ForeColor = ColorTranslator.FromHtml("#E0E7FF"),
            AutoSize = true, Location = new Point(50, 215), BackColor = Color.Transparent
        });
        overlay.Controls.Add(new Label { Text = "Đồ án .NET - Đại học Phenikaa", Font = Theme.Small, ForeColor = ColorTranslator.FromHtml("#93C5FD"), AutoSize = true, Location = new Point(50, 425), BackColor = Color.Transparent });

        // ---- Bên phải: form ----
        var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(60, 70, 60, 40) };
        var title = new Label { Text = "Đăng nhập", Font = new Font("Segoe UI", 26f, FontStyle.Bold), AutoSize = true, Location = new Point(55, 50), ForeColor = Theme.Text };
        var sub = new Label { Text = "Chào mừng trở lại! Vui lòng đăng nhập.", Font = new Font("Segoe UI", 11.5f), ForeColor = Theme.Muted, AutoSize = true, Location = new Point(60, 105) };

        Panel MakeModernInput(TextBox t, int x, int y, int width)
        {
            var p = new Panel { Location = new Point(x, y), Width = width, Height = 46, BackColor = Color.White, Cursor = Cursors.IBeam };
            p.Paint += (s, e) => { using var pen = new Pen(t.Focused ? Theme.Accent : Theme.Border, t.Focused ? 2f : 1f); e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1); };
            t.BorderStyle = BorderStyle.None;
            t.Location = new Point(12, 12);
            t.Width = width - 24;
            t.GotFocus += (s, e) => p.Invalidate();
            t.LostFocus += (s, e) => p.Invalidate();
            p.Click += (s, e) => t.Focus();
            p.Controls.Add(t);
            return p;
        }

        var lblU = Theme.Caption("TÊN ĐĂNG NHẬP", false); lblU.Location = new Point(60, 155); lblU.Font = new Font("Segoe UI", 9f, FontStyle.Bold); lblU.ForeColor = Theme.Muted;
        _user.Name = "txtUser"; _user.Font = new Font("Segoe UI", 12f); _user.MaxLength = 50;
        var pnlUser = MakeModernInput(_user, 60, 180, 360);

        var lblP = Theme.Caption("MẬT KHẨU", false); lblP.Location = new Point(60, 240); lblP.Font = new Font("Segoe UI", 9f, FontStyle.Bold); lblP.ForeColor = Theme.Muted;
        _pass.Name = "txtPass"; _pass.Font = new Font("Segoe UI", 12f); _pass.UseSystemPasswordChar = true; _pass.MaxLength = 100;
        var pnlPass = MakeModernInput(_pass, 60, 265, 360);

        _show.Text = "Hiển thị mật khẩu"; _show.AutoSize = true; _show.Font = Theme.Base; _show.ForeColor = Theme.Text;
        _show.Location = new Point(60, 320);
        _show.CheckedChanged += (_, _) => _pass.UseSystemPasswordChar = !_show.Checked;

        _error.AutoSize = false; _error.Size = new Size(360, 20); _error.Location = new Point(60, 350);
        _error.ForeColor = Theme.Danger; _error.Font = Theme.Base; _error.TextAlign = ContentAlignment.MiddleLeft;

        _btnLogin = Theme.PrimaryButton("ĐĂNG NHẬP", 360, (_, _) => DoLogin());
        _btnLogin.Name = "btnLogin"; _btnLogin.Height = 46; _btnLogin.Location = new Point(60, 375); _btnLogin.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        AcceptButton = _btnLogin;

        right.Controls.AddRange(new Control[] { title, sub, lblU, pnlUser, lblP, pnlPass, _show, _error, _btnLogin });
        Controls.Add(right);
        Controls.Add(left);

        Shown += (_, _) => _user.Focus();
    }

    private void DoLogin()
    {
        _error.Text = "";
        try
        {
            _bll.DangNhap(_user.Text, _pass.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (BusinessException ex)
        {
            _error.Text = ex.Message;
            _pass.SelectAll();
            _pass.Focus();
        }
        catch (Exception ex)
        {
            _error.Text = "Không thể đăng nhập: " + ex.Message;
        }
    }
}
