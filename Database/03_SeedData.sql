/* 03 - Dữ liệu mẫu (MySQL 8.0+)
   Tài khoản demo: admin / 123456 (Admin), nhanvien / 123456 (Nhân viên), trang / 123456 (Nhân viên) */
USE QLCuaHangLinhKien;
SET NAMES utf8mb4;

/* ---------- Nhân viên & tài khoản ---------- */
INSERT INTO NhanVien (HoTen, GioiTinh, NgaySinh, SDT, Email, DiaChi, ChucVu) VALUES
 ('Lưu Đức Hiệp',    'Nam', '2005-04-26', '0912345678', 'hiep@pcstore.vn',  'Hà Nội',   'Quản lý'),
 ('Lê Mạnh Tuấn',    'Nam', '2005-10-22', '0987654321', 'tuan@pcstore.vn',  'Hà Nội',   'Nhân viên bán hàng'),
 ('Nguyễn Thu Trang','Nữ',  '2003-07-12', '0934567890', 'trang@pcstore.vn', 'Bắc Ninh', 'Nhân viên bán hàng'),
 ('Phạm Văn Long',   'Nam', '2001-01-30', '0976543210', 'long@pcstore.vn',  'Hưng Yên', 'Kỹ thuật viên');

-- Hash BCrypt (cost 11) của mật khẩu "123456"
INSERT INTO TaiKhoan (TenDangNhap, MatKhauHash, MaNV, VaiTro) VALUES
 ('admin',    '$2a$11$lI0vWF.NvOTLvVkNeLhX5ejqJuEyeJz7R.L5DGTK7bvv6mbeWMWEO', 1, 'Admin'),
 ('nhanvien', '$2a$11$lI0vWF.NvOTLvVkNeLhX5ejqJuEyeJz7R.L5DGTK7bvv6mbeWMWEO', 2, 'NhanVien'),
 ('trang',    '$2a$11$lI0vWF.NvOTLvVkNeLhX5ejqJuEyeJz7R.L5DGTK7bvv6mbeWMWEO', 3, 'NhanVien');

/* ---------- Danh mục & hãng ---------- */
INSERT INTO DanhMuc (TenDM) VALUES
 ('CPU'), ('Mainboard'), ('RAM'), ('Card màn hình'), ('Ổ cứng SSD'),
 ('Nguồn (PSU)'), ('Vỏ case'), ('Tản nhiệt'), ('Màn hình'), ('Bàn phím'), ('Chuột'), ('Tai nghe');

INSERT INTO Hang (TenHang, QuocGia) VALUES
 ('Intel', 'Mỹ'), ('AMD', 'Mỹ'), ('ASUS', 'Đài Loan'), ('MSI', 'Đài Loan'), ('Gigabyte', 'Đài Loan'),
 ('Kingston', 'Mỹ'), ('Corsair', 'Mỹ'), ('Samsung', 'Hàn Quốc'), ('Western Digital', 'Mỹ'),
 ('Cooler Master', 'Đài Loan'), ('NZXT', 'Mỹ'), ('LG', 'Hàn Quốc'), ('Dell', 'Mỹ'),
 ('Logitech', 'Thụy Sĩ'), ('Razer', 'Mỹ'), ('DeepCool', 'Trung Quốc');

/* ---------- Sản phẩm (MaDM, MaHang theo thứ tự insert ở trên) ---------- */
INSERT INTO SanPham (TenSP, MaDM, MaHang, GiaNhap, GiaBan, ThoiGianBH, MoTa) VALUES
 ('Intel Core i5-12400F',               1, 1,  2900000,  3390000, 36, '6 nhân 12 luồng, 4.4GHz'),
 ('Intel Core i7-13700K',               1, 1,  8900000,  9990000, 36, '16 nhân 24 luồng, 5.4GHz'),
 ('AMD Ryzen 5 7600',                   1, 2,  4600000,  5290000, 36, '6 nhân 12 luồng, AM5'),
 ('AMD Ryzen 7 7800X3D',                1, 2,  9300000, 10490000, 36, '8 nhân 16 luồng, 3D V-Cache'),
 ('ASUS PRIME B760M-A WIFI D4',         2, 3,  2900000,  3490000, 36, 'mATX, LGA1700, DDR4'),
 ('MSI MAG B650 TOMAHAWK WIFI',         2, 4,  4900000,  5690000, 36, 'ATX, AM5, DDR5'),
 ('Gigabyte Z790 AORUS ELITE AX',       2, 5,  6300000,  7290000, 36, 'ATX, LGA1700, DDR5'),
 ('Kingston Fury Beast 16GB DDR4 3200', 3, 6,   850000,  1090000, 36, '2x8GB'),
 ('Corsair Vengeance 32GB DDR5 6000',   3, 7,  2400000,  2890000, 36, '2x16GB RGB'),
 ('Kingston Fury Beast 32GB DDR5 5600', 3, 6,  2200000,  2650000, 36, '2x16GB'),
 ('ASUS Dual RTX 4060 8GB',             4, 3,  7300000,  8290000, 36, 'GDDR6, 2 fan'),
 ('MSI RTX 4070 SUPER Ventus 2X 12GB',  4, 4, 14800000, 16490000, 36, 'GDDR6X'),
 ('Gigabyte RX 7800 XT Gaming OC 16GB', 4, 5, 12300000, 13790000, 36, 'GDDR6'),
 ('Samsung 990 PRO 1TB NVMe',           5, 8,  2500000,  2990000, 60, 'PCIe 4.0, 7450MB/s'),
 ('Kingston NV2 500GB NVMe',            5, 6,   750000,   950000, 36, 'PCIe 4.0'),
 ('WD Blue SN580 1TB NVMe',             5, 9,  1450000,  1750000, 60, 'PCIe 4.0'),
 ('Corsair RM750e 750W 80+ Gold',       6, 7,  2200000,  2650000, 84, 'Full modular'),
 ('Cooler Master MWE 650 Bronze V2',    6, 10, 1150000,  1390000, 36, '80+ Bronze'),
 ('NZXT H5 Flow',                       7, 11, 1700000,  2090000, 24, 'Mid tower, kính cường lực'),
 ('Cooler Master MasterBox TD500 Mesh', 7, 10, 1750000,  2150000, 24, 'Mid tower, 3 fan ARGB'),
 ('DeepCool AK400',                     8, 16,  550000,   690000, 24, 'Tản khí 4 ống đồng'),
 ('NZXT Kraken 240',                    8, 11, 2600000,  3190000, 72, 'Tản nước AIO 240mm'),
 ('LG UltraGear 27GP850 27" 2K 165Hz',  9, 12, 7200000,  8290000, 24, 'Nano IPS'),
 ('Dell S2421H 24" FHD IPS',            9, 13, 2700000,  3190000, 36, '75Hz, loa kép'),
 ('Samsung Odyssey G5 32" 2K 144Hz',    9, 8,  6300000,  7190000, 24, 'VA cong 1000R'),
 ('Logitech G Pro X TKL',              10, 14, 3300000,  3890000, 24, 'Bàn phím cơ không dây'),
 ('Razer BlackWidow V4',               10, 15, 3500000,  4190000, 24, 'Switch Green'),
 ('Logitech G102 Lightsync',           11, 14,  330000,   450000, 24, '8000 DPI, RGB'),
 ('Logitech G Pro X Superlight 2',     11, 14, 2900000,  3490000, 24, '60g, không dây'),
 ('Razer DeathAdder V3',               11, 15, 1450000,  1790000, 24, 'Có dây, 59g'),
 ('Logitech G435',                     12, 14, 1250000,  1590000, 24, 'Tai nghe không dây'),
 ('Razer BlackShark V2 X',             12, 15,  850000,  1090000, 24, '7.1 surround');

/* ---------- Nhà cung cấp ---------- */
INSERT INTO NhaCungCap (TenNCC, SDT, Email, DiaChi) VALUES
 ('Công ty TNHH Phân phối Viết Sơn',  '02438686868', 'sales@vietson.vn', 'Cầu Giấy, Hà Nội'),
 ('Công ty CP Thế giới số Digiworld', '02839290059', 'contact@dgw.vn',   'Quận 3, TP.HCM'),
 ('Công ty TNHH Tin học Mai Hoàng',   '02436285999', 'info@maihoang.vn', 'Đống Đa, Hà Nội');

/* ---------- Khách hàng ---------- */
INSERT INTO KhachHang (HoTen, SDT, DiaChi, Email) VALUES
 ('Trần Minh Khoa',  '0901111222', 'Thanh Xuân, Hà Nội',   'khoa@gmail.com'),
 ('Đỗ Thị Hương',    '0902222333', 'Hà Đông, Hà Nội',      'huong@gmail.com'),
 ('Vũ Quốc Bảo',     '0903333444', 'Long Biên, Hà Nội',    NULL),
 ('Hoàng Anh Dũng',  '0904444555', 'Nam Từ Liêm, Hà Nội',  'dung@gmail.com'),
 ('Bùi Thanh Hà',    '0905555666', 'Hoàng Mai, Hà Nội',    NULL),
 ('Ngô Đức Mạnh',    '0906666777', 'Bắc Từ Liêm, Hà Nội',  'manh@gmail.com'),
 ('Lý Thị Mai',      '0907777888', 'Gia Lâm, Hà Nội',      NULL),
 ('Đặng Văn Hùng',   '0908888999', 'Ba Đình, Hà Nội',      'hung@gmail.com'),
 ('Phan Thùy Linh',  '0909999000', 'Tây Hồ, Hà Nội',       NULL),
 ('Cao Minh Tú',     '0910000111', 'Đông Anh, Hà Nội',     'tu@gmail.com');

/* ---------- Sinh phiếu nhập đầu kỳ, serial, hóa đơn ngẫu nhiên 12 tháng, bảo hành ---------- */
DROP PROCEDURE IF EXISTS sp_SeedDemo;
DELIMITER $$
CREATE PROCEDURE sp_SeedDemo()
BEGIN
    DECLARE v_ngay_dau DATETIME DEFAULT DATE_SUB(NOW(), INTERVAL 13 MONTH);
    DECLARE v_ncc INT DEFAULT 1;
    DECLARE v_pn INT;
    DECLARE v_i INT DEFAULT 1;
    DECLARE v_j INT;
    DECLARE v_hd INT;
    DECLARE v_ngay DATETIME;
    DECLARE v_kh INT;
    DECLARE v_nv INT;
    DECLARE v_sodong INT;
    DECLARE v_sl INT;
    DECLARE v_sp INT;
    DECLARE v_bh INT;
    DECLARE v_serial VARCHAR(50);

    -- bảng số 1..30 để sinh serial
    CREATE TEMPORARY TABLE tmp_n (n INT PRIMARY KEY);
    INSERT INTO tmp_n (n) WITH RECURSIVE s AS (SELECT 1 AS n UNION ALL SELECT n + 1 FROM s WHERE n < 30) SELECT n FROM s;

    WHILE v_ncc <= 3 DO
        INSERT INTO PhieuNhap (NgayNhap, MaNCC, MaNV, GhiChu) VALUES (v_ngay_dau, v_ncc, 1, 'Nhập hàng đầu kỳ');
        SET v_pn = LAST_INSERT_ID();

        INSERT INTO ChiTietPhieuNhap (MaPN, MaSP, SoLuong, DonGia)
        SELECT v_pn, MaSP, 30, GiaNhap FROM SanPham WHERE MaSP % 3 = v_ncc - 1;

        INSERT INTO SanPhamSerial (Serial, MaSP, MaPN, TrangThai)
        SELECT CONCAT('SN', LPAD(ct.MaSP, 3, '0'), '-', LPAD(t.n, 4, '0')), ct.MaSP, v_pn, 'TrongKho'
        FROM ChiTietPhieuNhap ct CROSS JOIN tmp_n t
        WHERE ct.MaPN = v_pn;

        UPDATE PhieuNhap SET TongTien = (SELECT SUM(SoLuong * DonGia) FROM ChiTietPhieuNhap WHERE MaPN = v_pn)
        WHERE MaPN = v_pn;
        SET v_ncc = v_ncc + 1;
    END WHILE;

    WHILE v_i <= 200 DO
        SET v_ngay = DATE_ADD(DATE_ADD(DATE(DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 365) DAY)), INTERVAL 8 HOUR),
                              INTERVAL FLOOR(RAND() * 720) MINUTE);
        SET v_kh = IF(FLOOR(RAND() * 4) = 0, NULL, (SELECT MaKH FROM KhachHang ORDER BY RAND() LIMIT 1));
        SET v_nv = (SELECT MaNV FROM NhanVien WHERE MaNV <= 3 ORDER BY RAND() LIMIT 1);

        INSERT INTO HoaDon (NgayLap, MaKH, MaNV) VALUES (v_ngay, v_kh, v_nv);
        SET v_hd = LAST_INSERT_ID();

        SET v_sodong = 1 + FLOOR(RAND() * 3);
        SET v_j = 0;
        WHILE v_j < v_sodong DO
            SET v_sl = 1 + FLOOR(RAND() * 2);
            SET v_sp = (SELECT p.MaSP FROM SanPham p
                        WHERE NOT EXISTS (SELECT 1 FROM ChiTietHoaDon c WHERE c.MaHD = v_hd AND c.MaSP = p.MaSP)
                          AND (SELECT COUNT(*) FROM SanPhamSerial s WHERE s.MaSP = p.MaSP AND s.TrangThai = 'TrongKho') >= v_sl
                        ORDER BY RAND() LIMIT 1);
            IF v_sp IS NOT NULL THEN
                SET v_bh = (SELECT ThoiGianBH FROM SanPham WHERE MaSP = v_sp);
                INSERT INTO ChiTietHoaDon (MaHD, MaSP, SoLuong, DonGia)
                SELECT v_hd, MaSP, v_sl, GiaBan FROM SanPham WHERE MaSP = v_sp;

                UPDATE SanPhamSerial
                SET MaHD = v_hd, NgayBan = v_ngay, HanBH = DATE_ADD(DATE(v_ngay), INTERVAL v_bh MONTH), TrangThai = 'DaBan'
                WHERE MaSP = v_sp AND TrangThai = 'TrongKho'
                ORDER BY Serial
                LIMIT v_sl;
            END IF;
            SET v_j = v_j + 1;
        END WHILE;

        IF NOT EXISTS (SELECT 1 FROM ChiTietHoaDon WHERE MaHD = v_hd) THEN
            DELETE FROM HoaDon WHERE MaHD = v_hd;
        ELSE
            UPDATE HoaDon h
            JOIN (SELECT SUM(SoLuong * DonGia) AS Tong FROM ChiTietHoaDon WHERE MaHD = v_hd) t
            SET h.TongTien  = t.Tong,
                h.GiamGia   = IF(t.Tong > 10000000, 200000, 0),
                h.ThanhToan = t.Tong - IF(t.Tong > 10000000, 200000, 0)
            WHERE h.MaHD = v_hd;
        END IF;
        SET v_i = v_i + 1;
    END WHILE;

    -- Đồng bộ tồn kho theo serial
    UPDATE SanPham p
    SET p.SoLuongTon = (SELECT COUNT(*) FROM SanPhamSerial s WHERE s.MaSP = p.MaSP AND s.TrangThai = 'TrongKho');

    -- Vài phiếu bảo hành đã hoàn tất
    INSERT INTO PhieuBaoHanh (Serial, MaKH, MaNV, NgayNhan, MoTaLoi, NgayTra, KetQua, TrangThai)
    SELECT s.Serial, h.MaKH, 4, DATE_ADD(s.NgayBan, INTERVAL 20 DAY), 'Máy không lên hình / lỗi phần cứng',
           DATE_ADD(s.NgayBan, INTERVAL 27 DAY), 'Đã đổi sản phẩm mới cùng loại', 'Đã trả khách'
    FROM SanPhamSerial s JOIN HoaDon h ON h.MaHD = s.MaHD
    WHERE s.TrangThai = 'DaBan' AND h.MaKH IS NOT NULL AND s.NgayBan < DATE_SUB(NOW(), INTERVAL 2 MONTH)
    ORDER BY s.NgayBan
    LIMIT 3;

    -- Một phiếu đang xử lý
    SET v_serial = (SELECT s.Serial FROM SanPhamSerial s JOIN HoaDon h ON h.MaHD = s.MaHD
                    WHERE s.TrangThai = 'DaBan' AND h.MaKH IS NOT NULL AND s.HanBH > CURDATE()
                      AND s.Serial NOT IN (SELECT Serial FROM PhieuBaoHanh)
                    ORDER BY s.NgayBan DESC LIMIT 1);
    INSERT INTO PhieuBaoHanh (Serial, MaKH, MaNV, MoTaLoi)
    SELECT v_serial, h.MaKH, 4, 'Sản phẩm phát tiếng kêu lạ khi hoạt động'
    FROM SanPhamSerial s JOIN HoaDon h ON h.MaHD = s.MaHD WHERE s.Serial = v_serial;
    UPDATE SanPhamSerial SET TrangThai = 'DangBaoHanh' WHERE Serial = v_serial;

    DROP TEMPORARY TABLE tmp_n;
END$$
DELIMITER ;

CALL sp_SeedDemo();
DROP PROCEDURE sp_SeedDemo;

SELECT CONCAT('Seed xong: ', (SELECT COUNT(*) FROM HoaDon), ' hóa đơn, ',
              (SELECT COUNT(*) FROM SanPhamSerial), ' serial.') AS KetQua;
