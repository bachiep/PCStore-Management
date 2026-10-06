using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Forms;

public class frmThemHang : Form
{
    private readonly HangBLL _bll = new();
    private readonly TextBox _ten = Theme.Input();
    private readonly TextBox _quocGia = Theme.Input();
    private readonly int _maHang;

    public frmThemHang(HangDTO? hang = null)
    {
        _maHang = hang?.MaHang ?? 0;
        Text = _maHang == 0 ? "Thêm hãng sản xuất" : $"Sửa hãng #{_maHang}";
        Size = new Size(380, 260);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Page;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = Theme.Base;

        _ten.MaxLength = 100;
        _quocGia.MaxLength = 50;

        if (hang != null)
        {
            _ten.Text = hang.TenHang;
            _quocGia.Text = hang.QuocGia ?? "";
        }

        var btnLuu = Theme.PrimaryButton("Lưu", 90, (_, _) => Save());
        var btnHuy = Theme.GhostButton("Hủy", 90, (_, _) => Close());

        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        flow.Controls.Add(Theme.Field("Tên hãng *", _ten, 320));
        flow.Controls.Add(Theme.Field("Quốc gia", _quocGia, 320));
        flow.Controls.Add(Theme.Row(btnLuu, btnHuy));

        p.Controls.Add(flow);
        Controls.Add(p);
        AcceptButton = btnLuu;
        CancelButton = btnHuy;
    }

    private void Save()
    {
        Msg.Run(() =>
        {
            var h = new HangDTO { MaHang = _maHang, TenHang = _ten.Text, QuocGia = _quocGia.Text };
            _bll.Save(h);
            DialogResult = DialogResult.OK;
        });
    }
}
