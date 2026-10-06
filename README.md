# 🛒 HỆ THỐNG QUẢN LÝ CỬA HÀNG MÁY TÍNH & LINH KIỆN (PC STORE)

Hệ thống quản lý bán hàng, tồn kho và bảo hành linh kiện máy tính chuyên biệt cho các cửa hàng IT/Tech store. Dự án được xây dựng trên nền tảng **.NET 8 Windows Forms** áp dụng kiến trúc **3 lớp (3-Tier Architecture)**, kết hợp bộ giao diện tùy biến hiện đại (Flat UI/UX) và kiểm thử tự động (Unit Testing).

---

## 🌟 Điểm nổi bật & Tính năng chính

### 1. Bán hàng & Điểm bán lẻ (POS)
* **Giao diện trực quan:** Bộ lọc danh mục linh kiện dạng thanh trượt/tab ngang, lưới sản phẩm hiển thị dạng thẻ card trực quan kèm ảnh, đơn giá và số lượng tồn kho thực tế.
* **Giỏ hàng & Thanh toán:** Hỗ trợ tăng/giảm nhanh số lượng, xóa linh hoạt, áp dụng chiết khấu/giảm giá linh hoạt theo số tiền (VNĐ) hoặc phần trăm (%).
* **Quản lý Serial xuất bán:** Chọn chính xác mã serial vật lý khi xuất bán từng sản phẩm để phục vụ theo dõi bảo hành.
* **Khách hàng:** Tự động tìm kiếm khách hàng theo SĐT, thêm nhanh khách hàng mới ngay trên màn hình POS.
* **In hóa đơn:** Xuất hóa đơn bán lẻ chuyên nghiệp định dạng **PDF** (thư viện QuestPDF).

### 2. Quản lý Nhập kho & Tự động sinh Serial
* **Phiếu nhập kho:** Tạo phiếu nhập theo nhà cung cấp, kiểm soát chặt chẽ số lượng và giá vốn.
* **Cơ chế Serial thông minh:** 
  * Tự động nhận diện và trích xuất tiền tố theo tên linh kiện (Ví dụ: `WD Blue...` $\rightarrow$ `WDBLUE`, `RAM Corsair...` $\rightarrow$ `RAMCORSAIR`).
  * Tự động sinh bù các mã serial còn thiếu theo định dạng chuẩn `{TIỀN_TỐ}-{YYMMDD}-{XXXX}`.
  * Kiểm tra và cảnh báo mã serial trùng lặp theo thời gian thực (Live badge validation).
* **Lịch sử nhập kho:** Tra cứu, lọc theo ngày và xem chi tiết danh sách linh kiện kèm serial từng lô nhập.

### 3. Quản lý Hàng hóa & Bảo hành theo Serial
* **Danh mục đa cấp độ:** Quản lý Linh kiện, Danh mục và Hãng sản xuất (kèm hộp thoại thêm/sửa hãng trực quan).
* **Vòng đời Serial:** Kiểm soát trạng thái từng cá thể sản phẩm xuyên suốt vòng đời (`TrongKho` $\rightarrow$ `DaBan` $\rightarrow$ `DangBaoHanh`).
* **Tra cứu bảo hành tức thì:** Tìm kiếm theo số Serial để kiểm tra ngày mua, hạn bảo hành còn lại, khách hàng sở hữu và tiếp nhận/trả bảo hành.

### 4. Báo cáo & Thống kê kinh doanh
* **Dashboard trực quan:** Thẻ chỉ số tổng quan (Doanh thu hôm nay, Số đơn hàng, Khách hàng mới, Cảnh báo sắp hết hàng).
* **Biểu đồ doanh thu tương tác:** Biểu đồ đường/cột thể hiện biến động doanh thu theo thời gian thực (sử dụng ScottPlot).
* **Xuất báo cáo Excel:** Hỗ trợ trích xuất toàn bộ bảng kê doanh thu, tồn kho và danh mục ra file Excel chuẩn (`ClosedXML`).

### 5. Thiết kế UI/UX Modern Flat Design
* **Không phụ thuộc thư viện UI nặng:** Toàn bộ các component (`ModernButton`, `ModernPanel`, `ProductCard`, `Theme.cs`) được tự lập trình bằng GDI+ hỗ trợ khử răng cưa (Anti-aliasing), hiệu ứng hover mượt mà và bo góc hiện đại.
* **Bố cục co giãn:** Hỗ trợ phóng to toàn màn hình và tự căn chỉnh tỷ lệ hiển thị trên nhiều độ phân giải khác nhau.

---

## 🛠️ Công nghệ & Thư viện sử dụng

| Phân hệ | Công nghệ / Thư viện | Vai trò |
| :--- | :--- | :--- |
| **Ngôn ngữ & Runtime** | C# 12 / .NET 8.0 (Windows Forms) | Nền tảng cốt lõi của ứng dụng Desktop |
| **Cơ sở dữ liệu** | MySQL Server 8.0+ | Lưu trữ dữ liệu quan hệ |
| **Data Access** | `MySqlConnector` + `DbHelper.cs` | Kết nối ADO.NET thuần tối ưu hiệu năng |
| **Mã hóa bảo mật** | `BCrypt.Net-Next` | Băm mật khẩu tài khoản người dùng an toàn |
| **Xuất hóa đơn** | `QuestPDF` | Thiết kế và xuất hóa đơn bán lẻ dạng PDF |
| **Biểu đồ thống kê** | `ScottPlot.WinForms` | Vẽ biểu đồ doanh thu tương tác cao |
| **Xuất báo cáo** | `ClosedXML` | Xuất bảng dữ liệu và báo cáo ra file Excel (.xlsx) |
| **Kiểm thử tự động** | `xUnit` | Bộ 70 Unit Tests kiểm thử tầng nghiệp vụ BLL & DAL |

---

## 📂 Cấu trúc dự án (3-Tier Architecture)

```
ComputerStore.sln
│
├── ComputerStore.DTO/       # Data Transfer Object: Các lớp thực thể dữ liệu (SanPham, HoaDon, Serial...)
├── ComputerStore.DAL/       # Data Access Layer: Truy vấn dữ liệu MySQL, ánh xạ DataTable sang DTO
├── ComputerStore.BLL/       # Business Logic Layer: Kiểm tra ràng buộc nghiệp vụ, tính toán, Transaction
├── ComputerStore.GUI/       # Presentation Layer: Giao diện người dùng WinForms
│   ├── Components/          # Bộ Custom Controls (ModernButton, ModernPanel, ProductCard...)
│   ├── Forms/               # Các Form chức năng và Dialogs
│   ├── Pages/               # Các UserControl màn hình chính (ucBanHang, ucNhapKho, ucDashboard...)
│   └── Services/            # Dịch vụ xuất file, in ấn và tiện ích hỗ trợ
├── ComputerStore.Tests/     # Bộ kiểm thử Unit Test (70 test cases bao phủ logic nghiệp vụ)
└── Database/                # Scripts khởi tạo cấu trúc CSDL và dữ liệu mẫu
    ├── 01_CreateDatabase.sql
    ├── 02_StoredProcedures.sql
    ├── 03_SeedData.sql
    └── 04_Migration_HoaDon.sql
```

---

## 🚀 Hướng dẫn cài đặt & Khởi chạy

### 1. Yêu cầu môi trường
* Hệ điều hành Windows 10/11.
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
* [MySQL Server 8.0+](https://dev.mysql.com/downloads/installer/) hoặc XAMPP / MariaDB.

### 2. Thiết lập Cơ sở dữ liệu
Mở công cụ quản lý MySQL (MySQL Workbench, phpMyAdmin, DBeaver, HeidiSQL...) và chạy lần lượt các script trong thư mục `Database/`:
1. `01_CreateDatabase.sql` — Khởi tạo database `QLCuaHangLinhKien` và hệ thống bảng.
2. `02_StoredProcedures.sql` — Khởi tạo các stored procedures và triggers hỗ trợ.
3. `03_SeedData.sql` — Nạp dữ liệu mẫu ban đầu (nhân viên, danh mục, linh kiện, serial mẫu).
4. `04_Migration_HoaDon.sql` — Cập nhật cấu trúc bổ sung cho hóa đơn.

### 3. Cấu hình chuỗi kết nối
Mở file `ComputerStore.GUI/appsettings.json` và điều chỉnh thông số tài khoản/cổng kết nối MySQL phù hợp với máy của bạn:

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Port=3306;Database=QLCuaHangLinhKien;User ID=root;Password=your_password;CharSet=utf8mb4;AllowUserVariables=True;ConnectionTimeout=10"
  }
}
```
*(Bạn cũng có thể cấu hình thông qua biến môi trường hệ thống `COMPUTERSTORE_CONNECTION`)*.

### 4. Build và Chạy ứng dụng

Khôi phục các gói phụ thuộc và biên dịch:
```bash
dotnet restore
dotnet build
```

Chạy chương trình:
```bash
dotnet run --project ComputerStore.GUI
```

### 5. Chạy kiểm thử tự động (Unit Tests)
```bash
dotnet test
```
*(Toàn bộ 70/70 Unit Tests được thiết kế kiểm tra tính đúng đắn của logic tính toán tiền, giảm giá, trừ tồn kho và luồng xử lý Serial)*.

---

## 🔑 Tài khoản dùng thử (Demo Accounts)

Sau khi chạy xong script dữ liệu mẫu `03_SeedData.sql`, bạn có thể đăng nhập với các tài khoản sau:

| Tên đăng nhập | Mật khẩu | Quyền hạn (Vai trò) | Mục đích sử dụng |
| :--- | :--- | :--- | :--- |
| **admin** | `123456` | **Quản trị viên (Admin)** | Toàn quyền quản trị hệ thống, nhân viên, thống kê, nhập/xuất kho |
| **nhanvien** | `123456` | **Nhân viên (NhanVien)** | Phân hệ bán hàng POS, tra cứu bảo hành, quản lý khách hàng |
| **trang** | `123456` | **Nhân viên (NhanVien)** | Tài khoản thu ngân ca phụ |

---

## 👥 Tác giả & Bản quyền

* **Đề tài:** Xây dựng hệ thống quản lý cửa hàng máy tính và linh kiện (PC Store).
* **Học phần:** Công nghệ .NET — Trường Đại học Phenikaa.
* **Tác giả:** Lưu Đức Hiệp.
