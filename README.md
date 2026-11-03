# 🛒 HỆ THỐNG QUẢN LÝ CỬA HÀNG MÁY TÍNH & LINH KIỆN (PC STORE)

**Mã nguồn công khai:** [github.com/bachiep/PCStore-Management](https://github.com/bachiep/PCStore-Management)

Ứng dụng quản lý bán hàng, tồn kho và bảo hành linh kiện máy tính, xây dựng bằng **.NET 8 Windows Forms**, kiến trúc **GUI – BLL – DAL** và MySQL. DTO dùng để truyền dữ liệu giữa các lớp. Phạm vi và kết quả hiện tại được mô tả trong báo cáo; các phần chưa kiểm chứng trên giao diện thực tế không được xem là đã nghiệm thu.

---

## 🌟 Điểm nổi bật & Tính năng chính

### 1. Bán hàng & Điểm bán lẻ (POS)
* **Giao diện bán hàng:** Danh mục linh kiện dạng tab và danh sách sản phẩm dạng bảng theo mặc định; có thể chuyển sang lưới thẻ ảnh. Mỗi sản phẩm hiển thị đơn giá và số lượng tồn kho.
* **Giỏ hàng & Thanh toán:** Hỗ trợ tăng/giảm nhanh số lượng, xóa linh hoạt, áp dụng chiết khấu/giảm giá linh hoạt theo số tiền (VNĐ) hoặc phần trăm (%).
* **Quản lý Serial xuất bán:** Chọn chính xác mã serial vật lý khi xuất bán từng sản phẩm để phục vụ theo dõi bảo hành.
* **Khách hàng:** Nhập số điện thoại rồi nhấn Enter để tra cứu khách hàng; có thể thêm nhanh khách hàng mới ngay trên màn hình POS.
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
* **Dashboard trực quan:** Thẻ chỉ số tổng quan gồm doanh thu hôm nay, số hóa đơn hôm nay, doanh thu tháng này và số sản phẩm sắp hết hàng.
* **Biểu đồ doanh thu tương tác:** Biểu đồ đường/cột thể hiện biến động doanh thu theo thời gian thực (sử dụng ScottPlot).
* **Xuất báo cáo Excel:** Hỗ trợ trích xuất toàn bộ bảng kê doanh thu, tồn kho và danh mục ra file Excel chuẩn (`ClosedXML`).

### 5. Thiết kế UI/UX Modern Flat Design
* **Không phụ thuộc thư viện UI nặng:** Toàn bộ các component (`ModernButton`, `ModernPanel`, `ProductCard`, `Theme.cs`) được tự lập trình bằng GDI+ hỗ trợ khử răng cưa (Anti-aliasing), hiệu ứng hover mượt mà và bo góc hiện đại.
* **Bố cục co giãn:** Các trang sử dụng bố cục WinForms có thể thay đổi kích thước; việc nghiệm thu ở các độ phân giải và mức DPI cụ thể cần được thực hiện trên máy chạy ứng dụng.
* **Ảnh sản phẩm:** 12 ảnh chụp thật theo danh mục, 7 ảnh đối chiếu đúng SKU và 2 ảnh tham khảo biến thể gần giống (được gắn nhãn cùng ghi rõ khác biệt). Các sản phẩm còn lại dùng ảnh tham khảo theo danh mục; quản trị viên có thể chọn ảnh riêng cho từng sản phẩm. Tác giả và giấy phép được ghi trong `ComputerStore.GUI/Resources/ProductImages/ATTRIBUTION.md`.

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
| **Kiểm thử tự động** | `xUnit` | Bộ 99 ca kiểm thử xUnit cho nghiệp vụ và tích hợp CSDL |

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
├── ComputerStore.Tests/     # Bộ kiểm thử xUnit (99 ca logic và tích hợp)
└── Database/                # Scripts cài mới, dữ liệu mẫu và migration
    ├── 01_CreateDatabase.sql
    ├── 02_StoredProcedures.sql
    ├── 03_SeedData.sql
    ├── 04_Migration_HoaDon.sql
    ├── 05_Migration_LichSuBanSerial.sql
    ├── 06_SeedDemoHistory.sql
    └── 07_Migration_NetRevenue.sql
```

---

## 🚀 Hướng dẫn cài đặt & Khởi chạy

### 1. Yêu cầu môi trường
* Hệ điều hành Windows 10/11.
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
* [MySQL Server 8.0+](https://dev.mysql.com/downloads/installer/) hoặc XAMPP / MariaDB.

Nút Retry có thể khởi động máy chủ demo riêng nếu tìm thấy MySQL Server 8 trong thư mục cài chuẩn hoặc `PATH`. Với XAMPP/MariaDB, hãy khởi động dịch vụ của bộ đó trước rồi cấu hình chuỗi kết nối; script tự khởi động chỉ nhận MySQL Server 8 đã xác minh phiên bản.

### 2. Thiết lập Cơ sở dữ liệu
**Cài mới — chọn một trong hai cách:**

- **Để ứng dụng tự tạo CSDL (hỗ trợ tên database tùy chọn):** cấu hình `COMPUTERSTORE_CONNECTION` với tên database mong muốn, rồi chạy ứng dụng. Nếu database chưa tồn tại, bootstrapper tạo schema đó và nạp lần lượt `01_CreateDatabase.sql`, `02_StoredProcedures.sql`, `03_SeedData.sql`, `06_SeedDemoHistory.sql` vào đúng schema trong chuỗi kết nối. Mặc định, tự khởi tạo chỉ được phép trên MySQL cục bộ/loopback.
- **Chạy SQL thủ công:** chỉ dùng cách này cho database demo mới, trống, có tên chính xác `QLCuaHangLinhKien`. Scripts 01, 02 và 03 chứa `CREATE DATABASE`/`USE QLCuaHangLinhKien`; script 06 không chọn schema. Vì vậy, không đổi tên schema cho cách này và không chạy các lệnh dưới đây trên database chứa dữ liệu cần giữ. Trong Bash hoặc Git Bash, chạy đúng thứ tự:

```bash
mysql -h 127.0.0.1 -P 3307 -u root -p < Database/01_CreateDatabase.sql
mysql -h 127.0.0.1 -P 3307 -u root -p --database QLCuaHangLinhKien < Database/02_StoredProcedures.sql
mysql -h 127.0.0.1 -P 3307 -u root -p --database QLCuaHangLinhKien < Database/03_SeedData.sql
mysql -h 127.0.0.1 -P 3307 -u root -p --database QLCuaHangLinhKien < Database/06_SeedDemoHistory.sql
```

Dữ liệu cơ sở có 60 hóa đơn demo; script 06 bổ sung 72 hóa đơn trải đều 12 tháng gần nhất, có nhiều ngày trong tháng, khách hàng và hình thức thanh toán. Script 06 cập nhật tồn kho/serial trong một transaction và có thể chạy lại mà không tạo hóa đơn trùng.

**Nâng cấp database cũ đang có dữ liệu:** sao lưu database trước, rồi chạy lần lượt migration `04_Migration_HoaDon.sql`, `05_Migration_LichSuBanSerial.sql`, `07_Migration_NetRevenue.sql` trên đúng schema mà ứng dụng đang dùng. Các migration không tự chọn schema; phải chỉ rõ database đích khi kết nối (hoặc chọn đúng schema trong công cụ quản trị), nếu không chúng có thể chạy nhầm database. Ví dụ với MySQL CLI trên Bash, thay `QLCuaHangLinhKien` bằng giá trị `Database` trong chuỗi kết nối của ứng dụng:

```bash
DB_NAME=QLCuaHangLinhKien
mysql -h 127.0.0.1 -P 3307 -u root -p --database "$DB_NAME" < Database/04_Migration_HoaDon.sql
mysql -h 127.0.0.1 -P 3307 -u root -p --database "$DB_NAME" < Database/05_Migration_LichSuBanSerial.sql
mysql -h 127.0.0.1 -P 3307 -u root -p --database "$DB_NAME" < Database/07_Migration_NetRevenue.sql
```

Ba script lần lượt cập nhật cấu trúc chứng từ, backfill lịch sử serial có thể xác định và thay các thủ tục thống kê để phân bổ giảm giá vào doanh thu sản phẩm. Chúng có thể chạy lại. Các trường snapshot lịch sử còn thiếu được để trống và hiển thị là chưa có dữ liệu; không suy đoán từ danh mục hiện tại. Lịch sử bán serial đã bị xóa trước đó không thể khôi phục đáng tin cậy.

Thống kê theo sản phẩm và danh mục phân bổ giảm giá hóa đơn theo tỷ trọng tiền hàng; phần dư làm tròn được phân phối theo phần dư lớn nhất để tổng doanh thu thuần khớp chính xác `HoaDon.ThanhToan`. Lợi nhuận theo tháng là **ước tính theo `SanPham.GiaNhap` hiện tại** trừ giảm giá đã phân bổ; hệ thống chưa lưu giá vốn tại thời điểm bán nên số liệu lịch sử không phải lợi nhuận kế toán chính xác.

> `01_CreateDatabase.sql` chỉ tạo database nếu chưa có, không xóa database hiện tại. Đây vẫn là script cài mới: nếu schema đã tồn tại, các lệnh tạo bảng sẽ dừng ở bảng trùng; hãy dùng các migration đúng phiên bản để nâng cấp, không chạy lại script 01 trên database đang sử dụng.

> Chỉ chạy `06_SeedDemoHistory.sql` trên database demo. Script tạo dữ liệu giả và trừ số serial đang trong kho; không chạy trên database vận hành hoặc dữ liệu thật. Với database demo đã cài từ trước, có thể chạy riêng script 06 trên đúng schema đã chọn; lệnh MySQL CLI ở trên truyền schema bằng `--database`.

**Sao lưu và khôi phục:** file backup có thông tin tên database nguồn và chứa toàn bộ dữ liệu khách hàng, nhân viên, hash mật khẩu cùng nhật ký; file không được mã hóa. Khi khôi phục, ứng dụng hiện nguồn/đích và yêu cầu nhập lại chính xác tên database đích trước khi ghi đè. Chỉ khôi phục sau khi xác nhận đúng database và giữ file backup ở nơi hạn chế truy cập.

### 3. Cấu hình chuỗi kết nối
Mở file `ComputerStore.GUI/appsettings.json` và điều chỉnh thông số tài khoản/cổng kết nối MySQL phù hợp với máy của bạn:

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Port=3307;Database=QLCuaHangLinhKien;User ID=root;Password=;CharSet=utf8mb4;AllowUserVariables=True;ConnectionTimeout=10"
  },
  "Store": {
    "Name": "PC STORE",
    "Address": "",
    "Phone": ""
  }
}
```
*(Bạn cũng có thể cấu hình thông qua biến môi trường hệ thống `COMPUTERSTORE_CONNECTION`)*.
Các trường `Store.Name`, `Store.Address`, `Store.Phone` chỉ dùng để in thông tin cửa hàng trên hóa đơn PDF. Mặc định tên là `PC STORE`, địa chỉ và số điện thoại để trống để tránh đưa thông tin giả lên hóa đơn; bản demo không yêu cầu bạn nhập thông tin thật.
*Cấu hình `root` không mật khẩu chỉ dành cho MySQL cục bộ dùng demo trên `127.0.0.1:3307`; không dùng cấu hình này cho máy chủ dùng chung hoặc môi trường vận hành. Script MySQL mẫu chỉ bind vào loopback. Nếu triển khai nơi khác, hãy tạo tài khoản MySQL riêng có quyền tối thiểu và đặt mật khẩu bằng biến môi trường/cấu hình cục bộ không đưa lên Git.*

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

### 5. Chạy kiểm thử tự động

Các kiểm thử tích hợp có thao tác thêm, xóa dữ liệu và có ca sao lưu/phục hồi. **Không chạy kiểm thử trên database vận hành hoặc chứa dữ liệu thật.** Hãy dùng một database chuyên biệt cho kiểm thử. Đoạn lệnh dưới đây dùng `ComputerStoreTest`; nếu database này đã tồn tại, ứng dụng sẽ không reset hoặc tự nâng cấp schema hiện có, vì vậy hãy bảo đảm đó là database kiểm thử đúng phiên bản.

`DatabaseBootstrapper` chỉ tạo schema và dữ liệu mẫu khi database được cấu hình chưa tồn tại; thao tác này mặc định chỉ được phép trên MySQL cục bộ/loopback. Nếu chủ động khởi tạo schema trên máy chủ từ xa, đặt `COMPUTERSTORE_ALLOW_REMOTE_BOOTSTRAP=true` trong môi trường chạy. Nếu khởi tạo bị lỗi giữa chừng, schema một phần được giữ lại để kiểm tra, không tự động xóa; hãy kiểm tra, sao lưu rồi xử lý thủ công trước khi chạy lại. Schema hiện hành là phiên bản 7, được ghi trong bảng `AppSchemaVersion`; khi nâng cấp, migration 07 cập nhật dấu phiên bản. Nếu schema hiện có thiếu bảng/cột/thủ tục bắt buộc hoặc còn phiên bản cũ, ứng dụng báo lỗi và giữ nguyên dữ liệu; hãy sao lưu, xác định phiên bản schema, rồi chạy đúng các migration. Không chạy script cài mới trên database đang dùng. Lệnh `--test-pages` khởi động ứng dụng ở chế độ kiểm tra trang, không mở màn hình đăng nhập; lệnh này sẽ kích hoạt bootstrapper trước khi chạy. Dùng cùng một chuỗi kết nối cho khởi tạo và test:

```powershell
$testConnection = 'Server=127.0.0.1;Port=3307;Database=ComputerStoreTest;User ID=root;Password=;CharSet=utf8mb4;AllowUserVariables=True;ConnectionTimeout=10'
$env:COMPUTERSTORE_CONNECTION = $testConnection
dotnet run --project ComputerStore.GUI -- --test-pages
if ($LASTEXITCODE -ne 0) { throw 'Không khởi tạo được database hoặc kiểm tra màn hình thất bại.' }

$env:COMPUTERSTORE_TEST_CONNECTION = $testConnection
dotnet test ComputerStore.sln
if ($LASTEXITCODE -ne 0) { throw 'Kiểm thử thất bại.' }

Remove-Item Env:COMPUTERSTORE_CONNECTION -ErrorAction SilentlyContinue
Remove-Item Env:COMPUTERSTORE_TEST_CONNECTION -ErrorAction SilentlyContinue
```

Các test báo lỗi nếu thiếu biến kết nối hoặc không mở được database, tránh báo pass khi chưa thực sự chạy. Lần kiểm tra gần nhất tại môi trường phát triển: **99 đạt, 0 lỗi, 0 bỏ qua** trên database kiểm thử tách biệt. Suite có ca chạy lại migration 04/05/07 trên schema mô phỏng bản cũ và đối chiếu dữ liệu trước/sau.

---

## 🔑 Tài khoản dùng thử (Demo Accounts)

Sau khi chạy xong script dữ liệu mẫu `03_SeedData.sql`, bạn có thể đăng nhập với các tài khoản sau:

> Đây là tài khoản/mật khẩu công khai chỉ để demo trên database cục bộ. Hãy đổi mật khẩu ngay sau khi cài mới; không sử dụng cấu hình hoặc tài khoản demo trên máy chủ dùng chung hay dữ liệu thật.

| Tên đăng nhập | Mật khẩu | Quyền hạn (Vai trò) | Mục đích sử dụng |
| :--- | :--- | :--- | :--- |
| **admin** | `123456` | **Quản trị viên (Admin)** | Toàn quyền quản trị hệ thống, nhân viên, thống kê, nhập/xuất kho |
| **nhanvien** | `123456` | **Nhân viên (NhanVien)** | Phân hệ bán hàng POS, tra cứu bảo hành, quản lý khách hàng |
| **trang** | `123456` | **Nhân viên (NhanVien)** | Tài khoản thu ngân ca phụ |

### Lưu ý về sao lưu dữ liệu

Tệp `.sql` sao lưu chứa toàn bộ dữ liệu nghiệp vụ, thông tin khách hàng/nhân viên, hash mật khẩu và nhật ký. Tệp hiện không được mã hóa; chỉ lưu trong vị trí được bảo vệ và không gửi qua kênh công khai. Chức năng sao lưu/khôi phục chỉ dành cho Admin.

---

## 👥 Tác giả & Bản quyền

* **Đề tài:** Xây dựng hệ thống quản lý cửa hàng máy tính và linh kiện (PC Store).
* **Học phần:** Công nghệ .NET — Trường Đại học Phenikaa.
* **Tác giả:[Điền họ tên sinh viên]**
