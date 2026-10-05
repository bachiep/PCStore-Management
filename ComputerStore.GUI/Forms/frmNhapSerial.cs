using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Forms;

public class frmNhapSerial : Form
{
    private readonly TextBox _txtPrefix = Theme.Input(120, "Tiền tố (vd: DELL)");
    private readonly TextBox _txtSerials = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Font = Theme.Base, Dock = DockStyle.Fill };
    private readonly SanPhamDTO _sp;
    private readonly int _soLuong;
    public List<string> ResultSerials { get; private set; } = new();

    public frmNhapSerial(SanPhamDTO sp, int soLuong, List<string> existingSerials)
    {
        _sp = sp;
        _soLuong = soLuong;
        Text = $"Nhập Serial - {sp.TenSP} (Cần {soLuong})";
        Size = new Size(500, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        var lbl = new Label { Text = $"Nhập chính xác {soLuong} serial, mỗi serial một dòng:", AutoSize = true, Font = Theme.Base };
        
        var btnTuDong = Theme.GhostButton("Tự sinh", 80, (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtPrefix.Text)) { Msg.Warn("Hãy nhập tiền tố."); return; }
            var required = _soLuong - _txtSerials.Lines.Count(l => !string.IsNullOrWhiteSpace(l));
            if (required <= 0) { Msg.Warn("Đã đủ serial."); return; }
            if (Msg.Run(() =>
            {
                var generated = new PhieuNhapBLL().TaoSerialTuDong(_sp.MaSP, _txtPrefix.Text, required);
                var current = _txtSerials.Text.Trim();
                _txtSerials.Text = string.IsNullOrEmpty(current) ? string.Join(Environment.NewLine, generated) : current + Environment.NewLine + string.Join(Environment.NewLine, generated);
            })) Msg.Info($"Đã sinh thêm {required} serial.");
        });

        var pnlTop = new Panel { Height = 40, Dock = DockStyle.Top };
        pnlTop.Controls.Add(lbl); lbl.Location = new Point(10, 10);
        pnlTop.Controls.Add(_txtPrefix); _txtPrefix.Location = new Point(250, 5);
        pnlTop.Controls.Add(btnTuDong); btnTuDong.Location = new Point(380, 5);

        var btnLuu = Theme.PrimaryButton("Lưu", 80, (_, _) => Save());
        var pnlBot = new Panel { Height = 50, Dock = DockStyle.Bottom };
        pnlBot.Controls.Add(btnLuu); btnLuu.Location = new Point(390, 10);

        var pnlFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        pnlFill.Controls.Add(_txtSerials);

        Controls.Add(pnlFill);
        Controls.Add(pnlTop);
        Controls.Add(pnlBot);

        if (existingSerials?.Count > 0)
            _txtSerials.Text = string.Join(Environment.NewLine, existingSerials);
    }

    private void Save()
    {
        var lines = _txtSerials.Lines.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        if (lines.Count != _soLuong)
        {
            Msg.Warn($"Cần nhập đúng {_soLuong} serial (đang có {lines.Count}).");
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
