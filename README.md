# XÂY DỰNG HỆ THỐNG QUẢN LÝ CỬA HÀNG MÁY TÍNH VÀ LINH KIỆN (PC STORE)

Đây là mã nguồn Bài tập lớn học phần **CÔNG NGHỆ .NET**, áp dụng kiến trúc 3 lớp (3-Tier Architecture) kết hợp thiết kế UI/UX hiện đại trên nền tảng **.NET 8 Windows Forms**.

## 📌 Tính năng nổi bật
- **Bán hàng (POS):** Giao diện bán lẻ siêu tốc, tự động nhận diện khách hàng qua SĐT. Hỗ trợ quét chọn Serial cho từng sản phẩm.
- **Quản lý Hàng hóa & Tồn kho:** Quản lý sản phẩm đa cấp độ (Danh mục, Hãng). Kiểm soát tồn kho nghiêm ngặt theo **Mã Serial (IMEI)** độc nhất.
- **Bảo hành thông minh:** Truy xuất lịch sử mua hàng, thời gian bảo hành và tình trạng bảo hành chỉ với 1 lượt tra cứu Serial.
- **Dashboard & Báo cáo:** Hiển thị biểu đồ doanh thu trực quan, cảnh báo hàng sắp hết. Xuất hóa đơn bán lẻ PDF chuyên nghiệp, xuất báo cáo ra Excel.
- **UI/UX Custom:** Không sử dụng các Component nặng nề của bên thứ 3 (như DevExpress). Tự vẽ lại toàn bộ Control bằng GDI+ mang lại giao diện Flat Design chuẩn 2024, đáp ứng Responsive.

## 🛠️ Công nghệ sử dụng
- **Ngôn ngữ:** C# 12
- **Nền tảng:** .NET 8.0 (Windows Forms)
- **Database:** MySQL
- **ORM / Data Access:** Truy vấn thuần `MySqlConnector` + cơ chế mapping thủ công siêu nhẹ (`DbHelper.cs`).
- **Thư viện bên thứ ba:**
  - `QuestPDF`: Xuất Hóa đơn điện tử PDF.
  - `ScottPlot.WinForms`: Vẽ biểu đồ doanh thu tương tác.
  - `ClosedXML`: Xuất báo cáo, bảng kê ra Excel.

## 📂 Cấu trúc thư mục (Kiến trúc 3-Tier)
```
ComputerStore.sln
│
├── ComputerStore.DTO      # Lớp đối tượng trung chuyển (Data Transfer Object)
├── ComputerStore.DAL      # Lớp truy xuất dữ liệu (Data Access Layer)
├── ComputerStore.BLL      # Lớp nghiệp vụ cốt lõi (Business Logic Layer)
├── ComputerStore.GUI      # Lớp giao diện người dùng (Presentation Layer)
└── ComputerStore.Tests    # Unit Tests (xUnit) cho toàn bộ Logic nghiệp vụ
```

## 🚀 Hướng dẫn cài đặt
1. **Thiết lập Database:**
   - Import file CSDL (nếu có) vào MySQL Server.
   - Hoặc chỉnh sửa chuỗi kết nối (Connection String) trong file `ComputerStore.DAL/DbHelper.cs`.
2. **Restore Packages:**
   ```bash
   dotnet restore
   ```
3. **Chạy ứng dụng:**
   ```bash
   dotnet run --project ComputerStore.GUI
   ```
4. **Chạy Unit Test:**
   ```bash
   dotnet test
   ```

## 👥 Tác giả
Sinh viên thực hiện bài tập lớn Đại học Phenikaa.
