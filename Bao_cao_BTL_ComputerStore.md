# BÀI TẬP LỚN
**HỌC PHẦN:** CÔNG NGHỆ .NET
**TÊN ĐỀ TÀI:** XÂY DỰNG HỆ THỐNG QUẢN LÝ CỬA HÀNG MÁY TÍNH VÀ LINH KIỆN (PC STORE)

## MỞ ĐẦU
**1. Lý do chọn đề tài**
Trong thời đại công nghệ số hiện nay, nhu cầu sử dụng và nâng cấp máy tính, thiết bị điện tử ngày càng tăng cao. Các cửa hàng kinh doanh thiết bị IT, linh kiện máy tính mọc lên ngày càng nhiều. Tuy nhiên, đặc thù của mặt hàng này là sự đa dạng về chủng loại, thông số kỹ thuật phức tạp, và đặc biệt là yêu cầu khắt khe về việc quản lý bảo hành theo từng số Serial (IMEI) riêng biệt. Việc quản lý thủ công hoặc sử dụng các phần mềm bán hàng chung chung (không hỗ trợ quản lý Serial sâu) gây ra nhiều khó khăn trong việc kiểm soát tồn kho, thất thoát tài sản và chậm trễ trong khâu bảo hành cho khách hàng. Nhận thấy vấn đề đó, nhóm quyết định chọn đề tài "Xây dựng hệ thống quản lý cửa hàng máy tính và linh kiện" nhằm mang lại một giải pháp phần mềm chuyên biệt, tối ưu quy trình bán hàng và quản lý kho theo chuẩn ngành IT.

**2. Mục đích nghiên cứu**
- Xây dựng một ứng dụng Desktop (Windows Forms) hoàn chỉnh áp dụng kiến trúc 3 lớp (3-Tier).
- Cung cấp giải pháp bán hàng (POS) nhanh chóng, giao diện trực quan, hiện đại.
- Giải quyết bài toán quản lý tồn kho và bảo hành thông qua hệ thống mã Serial chi tiết tới từng cá thể sản phẩm.

**3. Đối tượng và phạm vi nghiên cứu**
- **Đối tượng:** Các quy trình nghiệp vụ thực tế tại một cửa hàng bán lẻ máy tính, linh kiện.
- **Phạm vi:** Ứng dụng tập trung vào phân hệ Bán hàng, Nhập kho, Quản lý đối tác (Khách hàng, Nhà cung cấp), Bảo hành, và Thống kê báo cáo. Hệ thống phân quyền cơ bản (Admin/Nhân viên).

**4. Ý nghĩa khoa học và thực tiễn**
- **Thực tiễn:** Ứng dụng có thể triển khai ngay cho các cửa hàng vừa và nhỏ, giúp họ giảm thiểu sai sót sổ sách, tăng tốc độ phục vụ.
- **Khoa học:** Nắm vững và áp dụng các công nghệ nền tảng của .NET (C#, WinForms, ADO.NET), các thư viện hỗ trợ xuất báo cáo (QuestPDF, ClosedXML, ScottPlot). Nắm vững kỹ năng thiết kế UI/UX hiện đại trên WinForms mà không bị phụ thuộc vào framework nặng nề.

---

## CHƯƠNG 1: XÁC ĐỊNH YÊU CẦU

**1.1. Giới thiệu bối cảnh, đối tượng khách hàng**
- Khách hàng mục tiêu là các chủ cửa hàng, quản lý và nhân viên bán hàng tại các cửa hàng thiết bị IT, máy tính, linh kiện. 
- Quy mô: Cửa hàng vừa và nhỏ.
- Khó khăn hiện tại: Số lượng mã hàng (SKU) rất lớn, cùng một loại CPU hoặc VGA nhưng có hàng trăm mã Serial khác nhau, xuất nhập kho rất dễ nhầm lẫn. Chế độ bảo hành lên tới 36 tháng, khó tra cứu nếu mất hóa đơn giấy.

**1.2. Xác định vấn đề cần giải quyết**
Hệ thống cần giải quyết bài toán:
- Rút ngắn thời gian lập hóa đơn bán hàng, hỗ trợ bán cho khách vãng lai hoặc khách quen.
- Quản lý chặt chẽ số lượng tồn kho: Khớp giữa số lượng trên sổ sách và số lượng Serial vật lý trong kho.
- Tra cứu hạn bảo hành của sản phẩm chỉ bằng 1 thao tác quét mã Serial.
- Đánh giá hiệu quả kinh doanh thông qua các biểu đồ thống kê trực quan.

**1.3. Yêu cầu chung**
- Kiến trúc: 3 lớp (GUI, BLL, DAL).
- Đối tượng sử dụng: Quản trị viên (Toàn quyền), Nhân viên bán hàng (Quyền hạn chế: Lập hóa đơn, tra cứu bảo hành).
- Giao diện (UI/UX): Cần được thiết kế hiện đại (Flat Design), thao tác thân thiện với thói quen của con người, giảm thiểu các form nhập liệu thừa, nút bấm đặt ở vị trí thuận tiện.

**1.4. Yêu cầu cụ thể**
- **Quản lý danh mục & hàng hóa:** Thêm, sửa, xóa các loại linh kiện, nhà sản xuất. Gắn Icon/Hình ảnh nhận diện.
- **Bán hàng (POS):** Thêm vào giỏ hàng có kiểm tra tồn kho, nhập mã Serial tự động, áp dụng giảm giá, in hóa đơn PDF.
- **Nhập kho:** Chọn Nhà cung cấp, tạo phiếu nhập, hệ thống tự động sinh số lượng mã Serial tương ứng vào kho.
- **Khách hàng:** Quản lý thông tin liên hệ, xem lịch sử mua hàng, tổng chi tiêu. Thêm nhanh ngay tại màn hình POS.
- **Bảo hành:** Tra cứu Serial cho biết sản phẩm bán ngày nào, còn bảo hành bao lâu, ghi nhận lỗi và trả bảo hành.
- **Thống kê:** Biểu đồ doanh thu 7 ngày, top sản phẩm bán chạy, cảnh báo hàng sắp hết.

---

## CHƯƠNG 2: PHÂN TÍCH THIẾT KẾ HỆ THỐNG

**2.1. Phân tích yêu cầu khách hàng**
Từ các yêu cầu trên, giải pháp kỹ thuật được đưa ra là:
- Dùng hệ quản trị CSDL **MySQL** để lưu trữ (tối ưu, nhẹ nhàng).
- Ứng dụng Desktop dùng **.NET 8 Windows Forms**.
- Thiết kế UI: Tự xây dựng Class `Theme.cs` để chuẩn hóa các Control (Button, TextBox, Grid) theo phong cách hiện đại mà không cần dùng thư viện ngoài (Devexpress, Guna) nhằm tăng tốc độ ứng dụng.

**2.2. Thiết kế chức năng (Phân rã chức năng)**
- **Nhóm chức năng Hệ thống:** Đăng nhập, Đổi mật khẩu, Quản lý tài khoản, Sao lưu/Phục hồi dữ liệu.
- **Nhóm chức năng Giao dịch:** Lập hóa đơn bán hàng, Lập phiếu nhập kho, Lập phiếu tiếp nhận bảo hành.
- **Nhóm chức năng Danh mục:** Quản lý Sản phẩm, Hãng, Danh mục, Khách hàng, Nhà cung cấp.
- **Nhóm chức năng Thống kê:** Xem Dashboard doanh thu, xuất file Excel báo cáo.

**2.3. Thiết kế về cơ sở dữ liệu**
Hệ thống bao gồm các bảng chính:
- `TaiKhoan`, `NhanVien`: Quản lý truy cập.
- `DanhMuc`, `Hang`, `SanPham`: Thông tin mặt hàng.
- `NhaCungCap`, `KhachHang`: Đối tác.
- `PhieuNhap`, `ChiTietPhieuNhap`: Quản lý nhập.
- `HoaDon`, `ChiTietHoaDon`: Quản lý bán.
- `SanPhamSerial`: (Bảng quan trọng nhất) Quản lý vòng đời từng thiết bị từ lúc vào kho (`TrongKho`) -> Bán (`DaBan`) -> Bảo hành (`DangBaoHanh`). Bảng này liên kết với MaPN (lô nhập) và MaHD (lô bán).

**2.4. Thiết kế giao diện**
- **Triết lý thiết kế:** Flat Design, nhiều khoảng trắng (spacing/padding), bố cục phân chia theo tỷ lệ phần trăm (TableLayoutPanel) để form không bị vỡ khi resize.
- **Màn hình Dashboard:** Được chia tỉ lệ (50% biểu đồ - 25% Top bán chạy - 25% Sắp hết hàng).
- **Màn hình POS:** Layout 2 cột. Trái: Giỏ hàng. Phải: Form chọn sản phẩm và thanh toán. 
- Cấu trúc: Form chính (`frmMain`) dùng Menu dọc bên trái (Sidebar). Phần nội dung bên phải dùng UserControl (`uc...`) để chuyển trang linh hoạt không cần mở nhiều cửa sổ.

**2.5. Các vấn đề khác**
- **Bảo mật:** Mật khẩu lưu trong CSDL được băm (Hashing) an toàn.
- **Tính vẹn toàn dữ liệu (Transaction):** Các thao tác Nhập kho, Bán hàng được bọc trong Transaction. Nếu đang ghi Chi tiết hóa đơn mà lỗi, toàn bộ dữ liệu sẽ Rollback, kho không bị trừ sai.

---

## CHƯƠNG 3: TRIỂN KHAI

**3.1. Lựa chọn giải pháp công nghệ**
- **Frontend (Giao diện):** Windows Forms trên nền tảng **.NET 8.0**. Sử dụng code thuần (C#) để custom vẽ (OwnerDraw) các Component mang lại giao diện hiện đại.
- **Backend (Nghiệp vụ):** C#, Kiến trúc 3-Tier (DTO - BLL - DAL).
- **Database:** MySQL. Kết nối bằng thư viện `MySqlConnector` (Nhanh, nhẹ, chuẩn ADO.NET).
- **Thư viện bên thứ ba:**
  - `QuestPDF`: Xuất hóa đơn bán hàng ra file PDF chuẩn, đẹp.
  - `ScottPlot`: Vẽ biểu đồ thống kê trực quan trên Dashboard.
  - `ClosedXML`: Xuất báo cáo dữ liệu ra file Excel.

**3.2. Thử nghiệm (Test), đánh giá hiệu quả vận hành**
- **Unit Test (Kiểm thử chức năng logic):** Đã xây dựng bộ `ComputerStore.Tests` sử dụng xUnit để kiểm thử tầng BLL. Toàn bộ 68 test case về luồng tính tiền, giảm giá, trừ tồn kho, kiểm tra thời hạn bảo hành đều đạt **PASS 100%**.
- **Xử lý ngoại lệ (Validation):** Ngăn chặn thành công các luồng sai logic (Ví dụ: Chặn gõ chữ vào ô Số điện thoại, chặn bán số lượng vượt mức tồn kho).
- **Đánh giá hiệu năng:** Khởi động ứng dụng cực nhanh do không phụ thuộc UI framework nặng. Truy vấn CSDL ổn định.

---

## KẾT LUẬN

**1. Tóm tắt kết quả thực hiện được**
Nhóm đã hoàn thành xuất sắc việc phân tích, thiết kế và lập trình phần mềm Quản lý cửa hàng Máy tính (PC Store). Phần mềm đáp ứng đầy đủ nghiệp vụ lõi, giao diện thân thiện, tốc độ xử lý nhanh, bảo mật dữ liệu tốt thông qua Transaction.

**2. So sánh với yêu cầu đặt ra lúc đầu**
- Hoàn thiện 100% các tính năng nghiệp vụ đã đề ra ở Chương 1.
- Giải quyết triệt để bài toán khó nhất là: Quản lý hàng hóa phức tạp dựa trên Serial, có khả năng sinh Serial tự động khi nhập kho và tra vết bảo hành ngược.
- Áp dụng thành công kiến trúc 3 lớp, clean code.

**3. Hướng phát triển, mở rộng sản phẩm**
- Tích hợp gửi Hóa đơn điện tử qua Email cho khách hàng.
- Xây dựng thêm App Mobile hoặc Web dành cho Khách hàng tự tra cứu tình trạng bảo hành.
- Tích hợp thêm máy quét mã vạch (Barcode Scanner) qua cổng COM/USB để bắn mã Serial trực tiếp vào hệ thống thay vì gõ tay hoặc chọn danh sách.

---
## TÀI LIỆU THAM KHẢO

[1] Microsoft, "Windows Forms Documentation", Trang tài liệu chính thức của Microsoft về WinForms .NET 8.
[2] MySQL, "MySQL 8.0 Reference Manual", MySQL.
[3] Các tài liệu mở về Kiến trúc 3 lớp (3-Tier Architecture) và ADO.NET.
[4] Thư viện QuestPDF (questpdf.com), ScottPlot (scottplot.net), ClosedXML (closedxml.readthedocs.io).
