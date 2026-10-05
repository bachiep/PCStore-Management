using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ComputerStore.DTO;
using ComputerStore.BLL;

namespace ComputerStore.GUI.Services;

public static class PdfExporter
{
    static PdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    public static void ExportHoaDon(HoaDonDTO hd, List<ChiTietHoaDonDTO> items, string outputPath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(c => ComposeHeader(c, hd));
                page.Content().Element(c => ComposeContent(c, items, hd));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(outputPath);
    }

    private static void ComposeHeader(IContainer container, HoaDonDTO hd)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("PC STORE").FontSize(22).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("ĐC: 123 Đường ABC, Hà Nội").FontColor(Colors.Grey.Darken2);
                column.Item().Text("SĐT: 0123 456 789").FontColor(Colors.Grey.Darken2);
            });
            row.ConstantItem(140).AlignRight().Column(c =>
            {
                c.Item().Text($"HÓA ĐƠN BÁN HÀNG").FontSize(14).SemiBold();
                c.Item().Text($"Mã hóa đơn: #{hd.MaHD}");
                c.Item().Text($"Ngày: {hd.NgayLap:dd/MM/yyyy HH:mm}");
            });
        });
    }

    private static void ComposeContent(IContainer container, List<ChiTietHoaDonDTO> items, HoaDonDTO hd)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(10);
            
            // Thông tin khách hàng & nhân viên
            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("KHÁCH HÀNG").SemiBold().FontColor(Colors.Blue.Darken2);
                    c.Item().Text(hd.TenKH ?? "Khách lẻ");
                    c.Item().Text($"SĐT: {hd.SDTKH ?? "N/A"}");
                });
                r.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text("NHÂN VIÊN BÁN").SemiBold().FontColor(Colors.Blue.Darken2);
                    c.Item().Text(hd.TenNV);
                });
            });
            
            // Bảng sản phẩm
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(20); // STT
                    columns.RelativeColumn(3); // Tên SP
                    columns.RelativeColumn(1); // SL
                    columns.RelativeColumn(2); // Đơn giá
                    columns.RelativeColumn(2); // Thành tiền
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("#").SemiBold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Sản phẩm").SemiBold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("SL").SemiBold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("Đơn giá").SemiBold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("Thành tiền").SemiBold();
                });

                int stt = 1;
                foreach (var item in items)
                {
                    table.Cell().PaddingTop(5).Text(stt++.ToString()).FontColor(Colors.Grey.Darken2);
                    table.Cell().PaddingTop(5).Text(item.TenSP);
                    table.Cell().PaddingTop(5).AlignRight().Text(item.SoLuong.ToString());
                    table.Cell().PaddingTop(5).AlignRight().Text($"{item.DonGia:N0}");
                    table.Cell().PaddingTop(5).AlignRight().Text($"{item.ThanhTien:N0}");
                    
                    if (item.Serials.Any())
                    {
                        table.Cell(); // Empty STT
                        table.Cell().ColumnSpan(4).PaddingBottom(5).Text($"S/N: {string.Join(", ", item.Serials)}")
                              .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    }
                    else
                    {
                        table.Cell().ColumnSpan(5).PaddingBottom(5);
                    }
                    table.Cell().ColumnSpan(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
                }
            });

            // Tổng kết
            column.Item().PaddingTop(10).Row(r =>
            {
                r.RelativeItem().AlignBottom().Text("Cảm ơn quý khách đã mua sắm tại PC Store!").FontColor(Colors.Grey.Medium).Italic();
                r.ConstantItem(180).Column(c =>
                {
                    c.Item().Row(r2 => { r2.RelativeItem().Text("Tổng tiền:"); r2.RelativeItem().AlignRight().Text($"{hd.TongTien:N0} ₫"); });
                    c.Item().Row(r2 => { r2.RelativeItem().Text("Giảm giá:"); r2.RelativeItem().AlignRight().Text($"-{hd.GiamGia:N0} ₫"); });
                    c.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(r2 => 
                    { 
                        r2.RelativeItem().Text("Thanh toán:").SemiBold().FontSize(12); 
                        r2.RelativeItem().AlignRight().Text($"{hd.ThanhToan:N0} ₫").SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2); 
                    });
                });
            });
        });
    }

    public static void ExportThongKe(int nam, string outputPath)
    {
        var bll = new ThongKeBLL();
        var dt = bll.DoanhThuTheoThang(nam);
        var top = bll.TopBanChay(new DateTime(nam, 1, 1), new DateTime(nam, 12, 31));

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().AlignCenter().Text($"BÁO CÁO THỐNG KÊ NĂM {nam}").FontSize(16).SemiBold();
                page.Content().PaddingTop(1, Unit.Centimetre).Column(c =>
                {
                    c.Item().PaddingBottom(5).Text("1. Doanh thu theo tháng").SemiBold().FontSize(14);
                    c.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                        t.Header(h => { h.Cell().Text("Tháng"); h.Cell().Text("Số HĐ"); h.Cell().Text("Doanh thu"); h.Cell().Text("Lợi nhuận"); });
                        foreach (var item in dt)
                        {
                            t.Cell().Text(item.Nhan);
                            t.Cell().Text(item.SoHoaDon.ToString());
                            t.Cell().Text($"{item.DoanhThu:N0}");
                            t.Cell().Text($"{item.LoiNhuan:N0}");
                        }
                    });

                    c.Item().PaddingTop(20).PaddingBottom(5).Text("2. Top 10 sản phẩm bán chạy").SemiBold().FontSize(14);
                    c.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cols => { cols.RelativeColumn(3); cols.RelativeColumn(1); cols.RelativeColumn(2); });
                        t.Header(h => { h.Cell().Text("Sản phẩm"); h.Cell().Text("Đã bán"); h.Cell().Text("Doanh thu"); });
                        foreach (var item in top)
                        {
                            t.Cell().Text(item.TenSP);
                            t.Cell().Text(item.SoLuongBan.ToString());
                            t.Cell().Text($"{item.DoanhThu:N0}");
                        }
                    });
                });
            });
        }).GeneratePdf(outputPath);
    }
}
