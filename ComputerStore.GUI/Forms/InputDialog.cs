namespace ComputerStore.GUI.Forms;

/// <summary>Hộp thoại nhập một giá trị văn bản (dùng cho đặt lại mật khẩu, nhập serial...).</summary>
public class InputDialog : Form
{
    private readonly TextBox _box = Theme.Input(320);
    public string Value => _box.Text;

    public InputDialog(string title, string caption, bool password = false, string initial = "")
    {
        Text = title;
        ClientSize = new Size(380, 150);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        Font = Theme.Base; BackColor = Color.White;
        Name = "InputDialog";
        _box.UseSystemPasswordChar = password; _box.Text = initial; _box.Name = "txtInput";
        var ok = Theme.PrimaryButton("OK", 90, (_, _) => { DialogResult = DialogResult.OK; Close(); });
        var cancel = Theme.GhostButton("Hủy", 90, (_, _) => Close());
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20, 16, 20, 8) };
        flow.Controls.Add(Theme.Field(caption, _box, 340));
        flow.Controls.Add(Theme.Row(ok, cancel));
        Controls.Add(flow);
        AcceptButton = ok; CancelButton = cancel;
        Shown += (_, _) => _box.Focus();
    }

    public static string? Ask(IWin32Window owner, string title, string caption, bool password = false, string initial = "")
    {
        using var d = new InputDialog(title, caption, password, initial);
        return d.ShowDialog(owner) == DialogResult.OK ? d.Value : null;
    }
}
