using ClosedXML.Excel;
using ComputerStore.BLL;
using System.Diagnostics;

namespace ComputerStore.GUI.Services;

public static class ExcelExporter
{
    /// <summary>Xuất các cột đang hiển thị của một lưới dữ liệu ra file Excel.</summary>
    public static void ExportGrid(DataGridView grid, string title, string outputPath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Dữ liệu");
        ws.Cell(1, 1).Value = title.ToUpper();
        ws.Cell(1, 1).Style.Font.SetBold().Font.FontSize = 14;
        var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
        for (int c = 0; c < cols.Count; c++)
        {
            ws.Cell(3, c + 1).Value = cols[c].HeaderText;
        }
        ws.Range(3, 1, 3, Math.Max(1, cols.Count)).Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGray;
        int row = 4;
        foreach (DataGridViewRow r in grid.Rows)
        {
            for (int c = 0; c < cols.Count; c++)
            {
                var v = r.Cells[cols[c].Index].Value;
                var cell = ws.Cell(row, c + 1);
                switch (v)
                {
                    case null: break;
                    case int i: cell.Value = i; break;
                    case long l: cell.Value = l; break;
                    case decimal d: cell.Value = d; cell.Style.NumberFormat.Format = "#,##0"; break;
                    case double db: cell.Value = db; break;
                    case DateTime dt: cell.Value = dt; cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm"; break;
                    default: cell.Value = v.ToString(); break;
                }
            }
            row++;
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(outputPath);
    }

    public static void ExportThongKe(int nam, string outputPath)
    {
        var bll = new ThongKeBLL();
        var dt = bll.DoanhThuTheoThang(nam);
        var top = bll.TopBanChay(new DateTime(nam, 1, 1), new DateTime(nam, 12, 31));
        var sapHet = bll.SanPhamSapHet();

        using var wb = new XLWorkbook();
        
        // Sheet 1: Doanh thu
        var ws1 = wb.Worksheets.Add($"Doanh Thu {nam}");
        ws1.Cell(1, 1).Value = $"BÁO CÁO DOANH THU NĂM {nam}";
        ws1.Range("A1:D1").Merge().Style.Font.SetBold().Font.FontSize = 16;
        
        ws1.Cell(3, 1).Value = "Tháng";
        ws1.Cell(3, 2).Value = "Số HĐ";
        ws1.Cell(3, 3).Value = "Doanh thu (VND)";
        ws1.Cell(3, 4).Value = "Lợi nhuận ước tính (VND)";
        ws1.Range("A3:D3").Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGray;

        int row = 4;
        foreach (var item in dt)
        {
            ws1.Cell(row, 1).Value = item.Nhan;
            ws1.Cell(row, 2).Value = item.SoHoaDon;
            ws1.Cell(row, 3).Value = item.DoanhThu;
            ws1.Cell(row, 4).Value = item.LoiNhuan;
            row++;
        }
        if (row > 4) 
            ws1.Range(4, 3, row - 1, 4).Style.NumberFormat.Format = "#,##0";
        ws1.Columns().AdjustToContents();

        // Sheet 2: Top Sản phẩm
        var ws2 = wb.Worksheets.Add("Top Sản Phẩm");
        ws2.Cell(1, 1).Value = "TOP SẢN PHẨM BÁN CHẠY";
        ws2.Range("A1:C1").Merge().Style.Font.SetBold();
        
        ws2.Cell(3, 1).Value = "Sản phẩm";
        ws2.Cell(3, 2).Value = "Đã bán";
        ws2.Cell(3, 3).Value = "Doanh thu (VND)";
        ws2.Range("A3:C3").Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGray;

        row = 4;
        foreach (var item in top)
        {
            ws2.Cell(row, 1).Value = item.TenSP;
            ws2.Cell(row, 2).Value = item.SoLuongBan;
            ws2.Cell(row, 3).Value = item.DoanhThu;
            row++;
        }
        if (row > 4)
            ws2.Range(4, 3, row - 1, 3).Style.NumberFormat.Format = "#,##0";
        ws2.Columns().AdjustToContents();

        // Sheet 3: Tồn kho
        var ws3 = wb.Worksheets.Add("Tồn Kho Sắp Hết");
        ws3.Cell(1, 1).InsertTable(sapHet, "TonKho", true);
        ws3.Columns().AdjustToContents();

        wb.SaveAs(outputPath);
    }
}
