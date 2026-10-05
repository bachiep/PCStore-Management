using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Forms;

public class frmKhachHangNhanh : Form
{
    private readonly TextBox _ten = Theme.Input();
    private readonly TextBox _sdt = Theme.Input();
    private readonly TextBox _email = Theme.Input();
    private readonly TextBox _diaChi = Theme.Input();
    
    public KhachHangDTO? Result { get; private set; }
    
    public frmKhachHangNhanh(string prefillPhone)
    {
        Text = "Thêm khách hàng mới";
        Size = new Size(400, 480);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Page;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        _ten.MaxLength = 100;
        _sdt.MaxLength = 15; _sdt.Text = prefillPhone;
        _email.MaxLength = 100;
        _diaChi.MaxLength = 200; _diaChi.Multiline = true; _diaChi.Height = 60;

        var btnLuu = Theme.PrimaryButton("Lưu", 100, (_, _) => Save());
        var btnHuy = Theme.GhostButton("Hủy", 100, (_, _) => Close());

        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        flow.Controls.AddRange(new Control[] {
            Theme.Field("Họ tên *", _ten, 340),
            Theme.Field("Số điện thoại *", _sdt, 340),
            Theme.Field("Email", _email, 340),
            Theme.Field("Địa chỉ", _diaChi, 340),
            Theme.Row(btnLuu, btnHuy)
        });
        p.Controls.Add(flow);
        Controls.Add(p);
    }

    private void Save()
    {
        Msg.Run(() =>
        {
            var k = new KhachHangDTO { HoTen = _ten.Text, SDT = _sdt.Text, Email = _email.Text, DiaChi = _diaChi.Text };
            k.MaKH = new KhachHangBLL().Save(k);
            Result = k;
            DialogResult = DialogResult.OK;
        });
    }
}
