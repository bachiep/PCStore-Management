using ComputerStore.BLL;

namespace ComputerStore.GUI.Forms;

public class frmDoiMatKhau : Form
{
    private readonly TextBox _old = Theme.Input(300), _new = Theme.Input(300), _confirm = Theme.Input(300);

    public frmDoiMatKhau()
    {
        Text = "Đổi mật khẩu";
        ClientSize = new Size(360, 330);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        Font = Theme.Base; BackColor = Color.White;
        Name = "frmDoiMatKhau";

        foreach (var t in new[] { _old, _new, _confirm }) { t.UseSystemPasswordChar = true; t.MaxLength = 100; }
        _old.Name = "txtOld"; _new.Name = "txtNew"; _confirm.Name = "txtConfirm";

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(24, 18, 24, 10) };
        flow.Controls.Add(Theme.Field("Mật khẩu hiện tại", _old, 300));
        flow.Controls.Add(Theme.Field("Mật khẩu mới (tối thiểu 6 ký tự)", _new, 300));
        flow.Controls.Add(Theme.Field("Nhập lại mật khẩu mới", _confirm, 300));
        var btnOk = Theme.PrimaryButton("Đổi mật khẩu", 140, (_, _) => Save());
        var btnCancel = Theme.GhostButton("Hủy", 90, (_, _) => Close());
        flow.Controls.Add(Theme.Row(btnOk, btnCancel));
        Controls.Add(flow);
        AcceptButton = btnOk;
    }

    private void Save()
    {
        if (Msg.Run(() => new TaiKhoanBLL().DoiMatKhau(_old.Text, _new.Text, _confirm.Text)))
        {
            Msg.Info("Đổi mật khẩu thành công.");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
