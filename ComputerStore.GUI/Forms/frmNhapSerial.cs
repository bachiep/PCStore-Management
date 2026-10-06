using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Components;

namespace ComputerStore.GUI.Forms;

public class frmNhapSerial : Form
{
    private readonly TextBox _txtPrefix;
    private readonly TextBox _txtSerials = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10.5f),
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = Color.White
    };
    private readonly Label _lblCounter = new();
    private readonly SanPhamDTO _sp;
    private readonly int _soLuong;
    public List<string> ResultSerials { get; private set; } = new();

    public frmNhapSerial(SanPhamDTO sp, int soLuong, List<string> existingSerials)
    {
        _sp = sp;
        _soLuong = soLuong;
        Text = $"Nhập Serial - {sp.TenSP} (Cần {soLuong})";
        Size = new Size(580, 530);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Page;

        var defaultPrefix = GetDefaultPrefix(sp.TenSP);
        _txtPrefix = new TextBox
        {
            Width = 120,
            Font = new Font("Segoe UI", 9.5f),
            Text = defaultPrefix,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Tiền tố..."
        };

        // --- 1. HEADER (2 DÒNG RÕ RÀNG, KHÔNG BỊ ĐÈ CHỮ) ---
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            Padding = new Padding(16, 12, 16, 8),
            BackColor = Color.White
        };

        // Dòng 1: Hướng dẫn bên trái, Huy hiệu số lượng bên phải
        var row1 = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.Transparent };
        var lblInstruction = new Label
        {
            Text = $"Cần nhập {soLuong} serial (mỗi dòng 1 serial):",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1E293B"),
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblCounter.AutoSize = true;
        _lblCounter.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _lblCounter.Dock = DockStyle.Right;
        _lblCounter.TextAlign = ContentAlignment.MiddleRight;
        _lblCounter.Padding = new Padding(8, 4, 8, 4);

        row1.Controls.Add(_lblCounter);
        row1.Controls.Add(lblInstruction);

        // Dòng 2: Thanh công cụ tự sinh (Tiền tố + Nút Tự sinh + Nút Xóa hết)
        var row2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            WrapContents = false,
            AutoSize = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        var lblPrefix = new Label
        {
            Text = "Tiền tố:",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTranslator.FromHtml("#64748B"),
            AutoSize = true,
            Margin = new Padding(0, 6, 6, 0)
        };

        _txtPrefix.Margin = new Padding(0, 2, 8, 0);

        var btnTuDong = new ModernButton
        {
            Text = "⚡ Tự sinh serial",
            Width = 130,
            Height = 28,
            BorderRadius = 6,
            NormalColor = ColorTranslator.FromHtml("#2563EB"),
            HoverColor = ColorTranslator.FromHtml("#1D4ED8"),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 1, 8, 0)
        };
        var tipTuDong = new ToolTip();
        tipTuDong.SetToolTip(btnTuDong, "Tự động sinh các serial còn thiếu theo định dạng TIỀN_TỐ-YYMMDD-XXXX");

        btnTuDong.Click += (_, _) =>
        {
            var prefix = _txtPrefix.Text.Trim();
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = GetDefaultPrefix(_sp.TenSP);
                _txtPrefix.Text = prefix;
            }

            var currentLines = _txtSerials.Lines
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var required = _soLuong - currentLines.Count;
            if (required <= 0)
            {
                Msg.Warn($"Đã đủ {_soLuong} serial. Không cần sinh thêm.");
                return;
            }

            if (Msg.Run(() =>
            {
                var generated = new PhieuNhapBLL().TaoSerialTuDong(_sp.MaSP, prefix, required);
                var all = new List<string>(currentLines);
                all.AddRange(generated);
                _txtSerials.Text = string.Join(Environment.NewLine, all);
                _txtSerials.SelectionStart = _txtSerials.Text.Length;
                _txtSerials.ScrollToCaret();
            }))
            {
                Msg.Info($"Đã tự sinh thành công {required} serial với tiền tố \"{prefix.ToUpperInvariant()}\".");
            }
        };

        var btnClear = Theme.GhostButton("Xóa hết", 75, (_, _) =>
        {
            if (_txtSerials.Text.Trim().Length > 0 && Msg.Confirm("Bạn có muốn xóa toàn bộ serial đang nhập?"))
            {
                _txtSerials.Clear();
            }
        });
        btnClear.Height = 28;
        btnClear.Font = new Font("Segoe UI", 9f);
        btnClear.Margin = new Padding(0, 1, 0, 0);

        row2.Controls.Add(lblPrefix);
        row2.Controls.Add(_txtPrefix);
        row2.Controls.Add(btnTuDong);
        row2.Controls.Add(btnClear);

        pnlHeader.Controls.Add(row2);
        pnlHeader.Controls.Add(row1);

        // --- 2. VÙNG NHẬP LIỆU CHÍNH (CARD KHUNG VIỀN MỀM) ---
        var pnlFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 10), BackColor = Theme.Page };
        var card = new ModernPanel
        {
            Dock = DockStyle.Fill,
            FillColor = Color.White,
            BorderColor = ColorTranslator.FromHtml("#CBD5E1"),
            BorderSize = 1,
            BorderRadius = 8,
            Padding = new Padding(10)
        };
        card.Controls.Add(_txtSerials);
        pnlFill.Controls.Add(card);

        // --- 3. BOTTOM (NÚT LƯU, HỦY VÀ GỢI Ý) ---
        var pnlBot = new Panel { Height = 56, Dock = DockStyle.Bottom, Padding = new Padding(16, 10, 16, 12), BackColor = Color.White };
        var lblTip = new Label
        {
            Text = "💡 Mẹo: Có thể dán (Paste) trực tiếp từ Excel.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = ColorTranslator.FromHtml("#64748B"),
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var pnlActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        var btnHuy = Theme.GhostButton("Hủy", 80, (_, _) => Close());
        btnHuy.Height = 34;
        btnHuy.Margin = new Padding(0, 0, 8, 0);

        var btnLuu = Theme.PrimaryButton("Lưu serial", 110, (_, _) => Save());
        btnLuu.Height = 34;
        btnLuu.Margin = new Padding(0);

        pnlActions.Controls.Add(btnHuy);
        pnlActions.Controls.Add(btnLuu);

        pnlBot.Controls.Add(lblTip);
        pnlBot.Controls.Add(pnlActions);

        Controls.Add(pnlFill);
        Controls.Add(pnlHeader);
        Controls.Add(pnlBot);

        // Event hooks
        _txtSerials.TextChanged += (_, _) => UpdateCounter();

        if (existingSerials?.Count > 0)
            _txtSerials.Text = string.Join(Environment.NewLine, existingSerials);

        UpdateCounter();
    }

    private static string GetDefaultPrefix(string tenSP)
    {
        if (string.IsNullOrWhiteSpace(tenSP)) return "SP";
        var words = tenSP.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return "SP";
        var first = words[0].ToUpperInvariant();
        var clean = new string(first.Where(char.IsLetterOrDigit).ToArray());
        if (clean.Length >= 2) return clean.Length > 8 ? clean[..8] : clean;
        if (words.Length > 1)
        {
            var combined = clean + new string(words[1].Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (combined.Length >= 2) return combined.Length > 8 ? combined[..8] : combined;
        }
        return clean.Length > 0 ? clean : "SP";
    }

    private void UpdateCounter()
    {
        var lines = _txtSerials.Lines.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        var count = lines.Count;
        var hasDup = lines.Distinct(StringComparer.OrdinalIgnoreCase).Count() != count;

        if (hasDup)
        {
            _lblCounter.Text = $" ⚠️ Trùng lặp! ({count}/{_soLuong}) ";
            _lblCounter.ForeColor = ColorTranslator.FromHtml("#DC2626");
            _lblCounter.BackColor = ColorTranslator.FromHtml("#FEE2E2");
        }
        else if (count == _soLuong)
        {
            _lblCounter.Text = $" ✓ Đã đủ {count}/{_soLuong} serial ";
            _lblCounter.ForeColor = ColorTranslator.FromHtml("#16A34A");
            _lblCounter.BackColor = ColorTranslator.FromHtml("#DCFCE7");
        }
        else if (count < _soLuong)
        {
            _lblCounter.Text = $" Đang có: {count}/{_soLuong} (thiếu {_soLuong - count}) ";
            _lblCounter.ForeColor = ColorTranslator.FromHtml("#D97706");
            _lblCounter.BackColor = ColorTranslator.FromHtml("#FEF3C7");
        }
        else
        {
            _lblCounter.Text = $" ⚠️ Thừa: {count}/{_soLuong} ";
            _lblCounter.ForeColor = ColorTranslator.FromHtml("#DC2626");
            _lblCounter.BackColor = ColorTranslator.FromHtml("#FEE2E2");
        }
    }

    private void Save()
    {
        var lines = _txtSerials.Lines.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        if (lines.Count != _soLuong)
        {
            Msg.Warn($"Cần nhập đúng {_soLuong} serial (hiện có {lines.Count}).");
            return;
        }
        var unique = lines.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (unique.Count != lines.Count)
        {
            Msg.Warn("Có serial bị trùng lặp trong danh sách.");
            return;
        }
        ResultSerials = unique;
        DialogResult = DialogResult.OK;
    }
}
