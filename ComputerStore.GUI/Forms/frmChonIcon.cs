using ComputerStore.GUI;
using System.Drawing.Text;

namespace ComputerStore.GUI.Forms;

public class frmChonIcon : Form
{
    public string? SelectedIcon { get; private set; }
    
    public frmChonIcon()
    {
        Text = "Chọn biểu tượng (Icon) sản phẩm";
        Size = new Size(620, 480);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Page;
        Font = Theme.Base;
        
        var lbl = new Label { Text = "Chọn một biểu tượng phù hợp nếu chưa có ảnh thực tế:", Font = Theme.H2, Dock = DockStyle.Top, Padding = new Padding(12), AutoSize = true, ForeColor = Theme.Text };
        
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10), BackColor = Theme.Page };
        
        var icons = new Dictionary<string, string> {
            {"CPU", "🖲️"},
            {"Mainboard", "🎛️"},
            {"RAM", "📏"},
            {"VGA", "🎮"},
            {"Ổ cứng", "💾"},
            {"Nguồn", "🔋"},
            {"Vỏ case", "🗄️"},
            {"Tản nhiệt", "❄️"},
            {"Màn hình", "🖥️"},
            {"Chuột", "🖱️"},
            {"Bàn phím", "⌨️"},
            {"Tai nghe", "🎧"},
            {"Loa", "🔊"},
            {"Laptop", "💻"},
            {"Linh kiện", "⚙️"}
        };
        
        foreach (var kv in icons)
        {
            var btn = new Button { Width = 130, Height = 130, Margin = new Padding(8), Cursor = Cursors.Hand, BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#F8FAFC");
            
            btn.Paint += (s, e) => {
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                TextRenderer.DrawText(e.Graphics, kv.Value, new Font("Segoe UI Emoji", 42), new Rectangle(0, 0, 130, 90), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(e.Graphics, kv.Key, Theme.Base, new Rectangle(0, 90, 130, 40), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
            };
            
            btn.Click += (_, _) => {
                var fileName = $"icon_{kv.Key.Replace(" ", "").Replace("/", "")}.png";
                var dir = Path.Combine(AppContext.BaseDirectory, "Images");
                var path = Path.Combine(dir, fileName);
                
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(dir);
                    using var bmp = new Bitmap(400, 400);
                    using var g = Graphics.FromImage(bmp);
                    g.Clear(Color.Transparent); 
                    TextRenderer.DrawText(g, kv.Value, new Font("Segoe UI Emoji", 140), new Rectangle(0, 0, 400, 400), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
                SelectedIcon = fileName;
                DialogResult = DialogResult.OK;
            };
            flow.Controls.Add(btn);
        }
        
        Controls.Add(flow);
        Controls.Add(lbl);
    }
}
