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
    WITH RECURSIVE
    Thang AS (SELECT 1 AS T UNION ALL SELECT T + 1 FROM Thang WHERE T < 12),
    DongHoaDon AS (
        SELECT ct.MaHD, ct.MaSP, ct.SoLuong,
               CAST(ct.SoLuong * ct.DonGia AS DECIMAL(18,0)) AS TienHang,
               hd.GiamGia,
               CAST(ct.SoLuong * sp.GiaNhap AS DECIMAL(18,0)) AS GiaVonUocTinh,
               SUM(ct.SoLuong * ct.DonGia) OVER (PARTITION BY ct.MaHD) AS TongTienHang
        FROM ChiTietHoaDon ct
        JOIN HoaDon hd ON hd.MaHD = ct.MaHD
        JOIN SanPham sp ON sp.MaSP = ct.MaSP
        WHERE hd.DaHuy = 0 AND YEAR(hd.NgayLap) = Nam
    ),
    TyLeGiam AS (
        SELECT d.*,
               CASE WHEN d.TongTienHang > 0 THEN FLOOR(d.GiamGia * d.TienHang / d.TongTienHang) ELSE 0 END AS GiamGiaCoBan,
               CASE WHEN d.TongTienHang > 0 THEN MOD(d.GiamGia * d.TienHang, d.TongTienHang) ELSE 0 END AS PhanDu
        FROM DongHoaDon d
    ),
    XepHangPhanDu AS (
        SELECT d.*,
               ROW_NUMBER() OVER (PARTITION BY d.MaHD ORDER BY d.PhanDu DESC, d.MaSP) AS HangPhanDu,
               SUM(d.GiamGiaCoBan) OVER (PARTITION BY d.MaHD) AS TongGiamGiaCoBan
        FROM TyLeGiam d
    ),
    DongDaPhanBo AS (
        SELECT d.*,
               d.GiamGiaCoBan + CASE WHEN d.HangPhanDu <= d.GiamGia - d.TongGiamGiaCoBan THEN 1 ELSE 0 END AS GiamGiaPhanBo
        FROM XepHangPhanDu d
    )
    SELECT t.T AS Thang,
           COUNT(h.MaHD)                AS SoHoaDon,
           IFNULL(SUM(h.ThanhToan), 0)  AS DoanhThu,
           IFNULL((SELECT SUM(d.TienHang - d.GiamGiaPhanBo - d.GiaVonUocTinh)
                   FROM DongDaPhanBo d
                   JOIN HoaDon h2 ON h2.MaHD = d.MaHD
                   WHERE MONTH(h2.NgayLap) = t.T), 0) AS LoiNhuanUocTinh
    FROM Thang t
    LEFT JOIN HoaDon h ON h.DaHuy = 0 AND MONTH(h.NgayLap) = t.T AND YEAR(h.NgayLap) = Nam
    GROUP BY t.T
    ORDER BY t.T;
END$$

CREATE PROCEDURE sp_TopSanPhamBanChay(IN TuNgay DATE, IN DenNgay DATE, IN TopN INT)
BEGIN
    WITH DongHoaDon AS (
        SELECT ct.MaHD, ct.MaSP, ct.SoLuong,
               CAST(ct.SoLuong * ct.DonGia AS DECIMAL(18,0)) AS TienHang,
               hd.GiamGia,
               SUM(ct.SoLuong * ct.DonGia) OVER (PARTITION BY ct.MaHD) AS TongTienHang
        FROM ChiTietHoaDon ct
        JOIN HoaDon hd ON hd.MaHD = ct.MaHD
        WHERE hd.DaHuy = 0 AND DATE(hd.NgayLap) BETWEEN TuNgay AND DenNgay
    ),
    TyLeGiam AS (
        SELECT d.*,
               CASE WHEN d.TongTienHang > 0 THEN FLOOR(d.GiamGia * d.TienHang / d.TongTienHang) ELSE 0 END AS GiamGiaCoBan,
               CASE WHEN d.TongTienHang > 0 THEN MOD(d.GiamGia * d.TienHang, d.TongTienHang) ELSE 0 END AS PhanDu
        FROM DongHoaDon d
    ),
    XepHangPhanDu AS (
        SELECT d.*,
               ROW_NUMBER() OVER (PARTITION BY d.MaHD ORDER BY d.PhanDu DESC, d.MaSP) AS HangPhanDu,
               SUM(d.GiamGiaCoBan) OVER (PARTITION BY d.MaHD) AS TongGiamGiaCoBan
        FROM TyLeGiam d
    ),
    DongDaPhanBo AS (
        SELECT d.*,
               d.GiamGiaCoBan + CASE WHEN d.HangPhanDu <= d.GiamGia - d.TongGiamGiaCoBan THEN 1 ELSE 0 END AS GiamGiaPhanBo
        FROM XepHangPhanDu d
    )
    SELECT sp.MaSP, sp.TenSP,
           SUM(d.SoLuong) AS SoLuongBan,
           SUM(d.TienHang - d.GiamGiaPhanBo) AS DoanhThu
    FROM DongDaPhanBo d
    JOIN SanPham sp ON sp.MaSP = d.MaSP
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
