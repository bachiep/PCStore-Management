using ComputerStore.BLL;
using ComputerStore.DTO;

namespace ComputerStore.GUI.Forms;

public class frmChonSerial : Form
{
    private readonly CheckedListBox _list = new() { Dock = DockStyle.Fill, Font = Theme.Base, CheckOnClick = true };
    private readonly int _soLuong;
    public List<string> ResultSerials { get; private set; } = new();

    public frmChonSerial(SanPhamDTO sp, int soLuong)
    {
        _soLuong = soLuong;
        Text = $"Chọn Serial - {sp.TenSP} (Cần {soLuong})";
        Size = new Size(400, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Theme.Page;

        var lbl = new Label { Text = $"Hãy đánh dấu chọn đúng {soLuong} serial:", AutoSize = true, Font = Theme.Base, Margin = new Padding(10) };
        var pnlTop = new FlowLayoutPanel { Height = 40, Dock = DockStyle.Top };
        pnlTop.Controls.Add(lbl);

        var btnLuu = Theme.PrimaryButton("Lưu", 80, (_, _) => Save());
        var pnlBot = new Panel { Height = 50, Dock = DockStyle.Bottom };
        pnlBot.Controls.Add(btnLuu); btnLuu.Location = new Point(290, 10);

        var pnlFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 10) };
        var card = Theme.CardPanel(new Padding(5));
        card.Dock = DockStyle.Fill;
        _list.BorderStyle = BorderStyle.None;
        card.Controls.Add(_list);
        pnlFill.Controls.Add(card);

        Controls.Add(pnlFill);
        Controls.Add(pnlTop);
        Controls.Add(pnlBot);

        Msg.Run(() =>
        {
            var kho = new HoaDonBLL().GetSerialsInStock(sp.MaSP);
            if (kho.Count < _soLuong) 
            {
                Msg.Warn($"Cảnh báo: Dữ liệu Serial thực tế trong kho ({kho.Count}) không đủ số lượng bạn cần bán ({_soLuong}). Vui lòng kiểm tra lại hệ thống.");
            }
            foreach (var s in kho) _list.Items.Add(s);
        });

        _list.ItemCheck += (s, e) =>
        {
            if (e.NewValue == CheckState.Checked && _list.CheckedItems.Count >= _soLuong && !_list.GetItemChecked(e.Index))
            {
                e.NewValue = CheckState.Unchecked;
            }
        };
    }

    private void Save()
    {
        if (_list.CheckedItems.Count != _soLuong)
        {
            Msg.Warn($"Vui lòng chọn đúng {_soLuong} serial.");
            return;
        }
        foreach (var item in _list.CheckedItems)
            ResultSerials.Add(item.ToString()!);
        DialogResult = DialogResult.OK;
    }
}
