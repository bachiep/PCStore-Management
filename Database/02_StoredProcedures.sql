/* 02 - Stored procedures thống kê (MySQL 8.0+) */
USE QLCuaHangLinhKien;

DROP PROCEDURE IF EXISTS sp_DoanhThuTheoNgay;
DROP PROCEDURE IF EXISTS sp_DoanhThuTheoThang;
DROP PROCEDURE IF EXISTS sp_TopSanPhamBanChay;
DROP PROCEDURE IF EXISTS sp_SanPhamSapHet;

DELIMITER $$

CREATE PROCEDURE sp_DoanhThuTheoNgay(IN TuNgay DATE, IN DenNgay DATE)
BEGIN
    SELECT DATE(NgayLap)      AS Ngay,
           COUNT(*)           AS SoHoaDon,
           SUM(ThanhToan)     AS DoanhThu
    FROM HoaDon
    WHERE DaHuy = 0 AND DATE(NgayLap) BETWEEN TuNgay AND DenNgay
    GROUP BY DATE(NgayLap)
    ORDER BY Ngay;
END$$

CREATE PROCEDURE sp_DoanhThuTheoThang(IN Nam INT)
BEGIN
    WITH RECURSIVE Thang AS (SELECT 1 AS T UNION ALL SELECT T + 1 FROM Thang WHERE T < 12)
    SELECT t.T AS Thang,
           COUNT(h.MaHD)                AS SoHoaDon,
           IFNULL(SUM(h.ThanhToan), 0)  AS DoanhThu,
           IFNULL((SELECT SUM(ct.SoLuong * (ct.DonGia - sp.GiaNhap))
                   FROM ChiTietHoaDon ct
                   JOIN HoaDon h2 ON h2.MaHD = ct.MaHD
                   JOIN SanPham sp ON sp.MaSP = ct.MaSP
                   WHERE h2.DaHuy = 0 AND YEAR(h2.NgayLap) = Nam AND MONTH(h2.NgayLap) = t.T), 0) AS LoiNhuanUocTinh
    FROM Thang t
    LEFT JOIN HoaDon h ON h.DaHuy = 0 AND MONTH(h.NgayLap) = t.T AND YEAR(h.NgayLap) = Nam
    GROUP BY t.T
    ORDER BY t.T;
END$$

CREATE PROCEDURE sp_TopSanPhamBanChay(IN TuNgay DATE, IN DenNgay DATE, IN TopN INT)
BEGIN
    SELECT sp.MaSP, sp.TenSP,
           SUM(ct.SoLuong)             AS SoLuongBan,
           SUM(ct.SoLuong * ct.DonGia) AS DoanhThu
    FROM ChiTietHoaDon ct
    JOIN HoaDon h   ON h.MaHD = ct.MaHD
    JOIN SanPham sp ON sp.MaSP = ct.MaSP
    WHERE h.DaHuy = 0 AND DATE(h.NgayLap) BETWEEN TuNgay AND DenNgay
    GROUP BY sp.MaSP, sp.TenSP
    ORDER BY SoLuongBan DESC
    LIMIT TopN;
END$$

CREATE PROCEDURE sp_SanPhamSapHet(IN Nguong INT)
BEGIN
    SELECT sp.MaSP, sp.TenSP, dm.TenDM, h.TenHang, sp.SoLuongTon
    FROM SanPham sp
    JOIN DanhMuc dm ON dm.MaDM = sp.MaDM
    JOIN Hang h     ON h.MaHang = sp.MaHang
    WHERE sp.TrangThai = 1 AND sp.SoLuongTon <= Nguong
    ORDER BY sp.SoLuongTon;
END$$

DELIMITER ;
