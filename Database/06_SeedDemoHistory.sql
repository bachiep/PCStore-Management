/*
  06 - Tạo thêm hóa đơn demo trải đều 12 tháng gần nhất.
  Chỉ dùng cho database demo. Script bổ sung 72 hóa đơn, trừ tồn kho và đổi
  serial sang trạng thái đã bán; có thể chạy lại mà không tạo bản ghi trùng.
  Không chạy trên database vận hành hoặc dữ liệu thật.
*/
DROP PROCEDURE IF EXISTS sp_SeedDemoHistory;
DELIMITER $$
CREATE PROCEDURE sp_SeedDemoHistory()
BEGIN
    DECLARE v_i INT DEFAULT 1;
    DECLARE v_j INT;
    DECLARE v_so_dong INT;
    DECLARE v_sl INT;
    DECLARE v_sp INT;
    DECLARE v_hd INT;
    DECLARE v_kh INT;
    DECLARE v_nv INT;
    DECLARE v_gia DECIMAL(18,0);
    DECLARE v_bh INT;
    DECLARE v_ten VARCHAR(200);
    DECLARE v_tong DECIMAL(18,0);
    DECLARE v_giam DECIMAL(18,0);
    DECLARE v_ngay DATETIME;
    DECLARE v_ngay_goc DATE;
    DECLARE v_thang INT;
    DECLARE v_ngay_trong_thang INT;
    DECLARE v_kh_offset INT;
    DECLARE v_tag VARCHAR(40);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_ngay_goc = STR_TO_DATE(DATE_FORMAT(DATE_SUB(CURDATE(), INTERVAL 11 MONTH), '%Y-%m-01'), '%Y-%m-%d');
    START TRANSACTION;

    WHILE v_i <= 72 DO
        SET v_thang = FLOOR((v_i - 1) / 6);
        SET v_ngay_trong_thang = IF(v_thang = 11, GREATEST(DAY(CURDATE()), 1), 27);
        SET v_ngay = DATE_ADD(
            DATE_ADD(v_ngay_goc, INTERVAL v_thang MONTH),
            INTERVAL MOD((v_i - 1) * 3 + v_thang, v_ngay_trong_thang) DAY
        );
        SET v_ngay = DATE_ADD(v_ngay, INTERVAL (8 + MOD(v_i * 7, 10)) HOUR);
        SET v_ngay = DATE_ADD(v_ngay, INTERVAL MOD(v_i * 13, 60) MINUTE);
        SET v_ngay = LEAST(NOW(), v_ngay);
        SET v_tag = CONCAT('DEMO-HISTORY-V1-', LPAD(v_i, 3, '0'));

        IF NOT EXISTS (SELECT 1 FROM HoaDon WHERE GhiChu = v_tag) THEN
            SET v_kh_offset = MOD(v_i, (SELECT COUNT(*) FROM KhachHang));
            SET v_kh = IF(MOD(v_i, 4) = 0, NULL,
                (SELECT MaKH FROM KhachHang ORDER BY MaKH LIMIT v_kh_offset, 1));
            SET v_nv = IF(MOD(v_i, 2) = 0, 2, 3);

            INSERT INTO HoaDon (NgayLap, MaKH, MaNV, HinhThucTT, GhiChu)
            VALUES (v_ngay, v_kh, v_nv,
                    ELT(1 + MOD(v_i, 3), 'Tiền mặt', 'Chuyển khoản', 'Thẻ'), v_tag);
            SET v_hd = LAST_INSERT_ID();
            SET v_so_dong = 1 + MOD(v_i, 3);
            SET v_j = 1;

            WHILE v_j <= v_so_dong DO
                SET v_sl = 1 + MOD(v_i + v_j, 2);
                SET v_sp = (
                    SELECT p.MaSP
                    FROM SanPham p
                    WHERE p.TrangThai = 1
                      AND p.SoLuongTon >= v_sl
                      AND (SELECT COUNT(*) FROM SanPhamSerial s
                           WHERE s.MaSP = p.MaSP AND s.TrangThai = 'TrongKho') >= v_sl
                      AND NOT EXISTS (SELECT 1 FROM ChiTietHoaDon c WHERE c.MaHD = v_hd AND c.MaSP = p.MaSP)
                    ORDER BY CRC32(CONCAT(v_tag, '-', p.MaSP))
                    LIMIT 1
                );

                IF v_sp IS NULL THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Không đủ serial/tồn kho để tạo dữ liệu demo. Giao dịch đã được hoàn tác.';
                END IF;

                SELECT TenSP, GiaBan, ThoiGianBH
                INTO v_ten, v_gia, v_bh
                FROM SanPham WHERE MaSP = v_sp;

                INSERT INTO ChiTietHoaDon (MaHD, MaSP, SoLuong, DonGia, TenSPKhiBan, ThoiGianBHKhiBan)
                VALUES (v_hd, v_sp, v_sl, v_gia, v_ten, v_bh);

                UPDATE SanPhamSerial
                SET MaHD = v_hd, NgayBan = v_ngay,
                    HanBH = CASE WHEN v_bh > 0 THEN DATE_ADD(DATE(v_ngay), INTERVAL v_bh MONTH) ELSE NULL END,
                    TrangThai = 'DaBan'
                WHERE MaSP = v_sp AND TrangThai = 'TrongKho'
                ORDER BY Serial
                LIMIT v_sl;

                IF ROW_COUNT() <> v_sl THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Không chọn đủ serial cho dữ liệu demo. Giao dịch đã được hoàn tác.';
                END IF;

                INSERT INTO LichSuBanSerial (MaHD, MaSP, Serial, NgayBan, HanBH)
                SELECT MaHD, MaSP, Serial, NgayBan, HanBH
                FROM SanPhamSerial WHERE MaHD = v_hd AND MaSP = v_sp;

                UPDATE SanPham
                SET SoLuongTon = SoLuongTon - v_sl
                WHERE MaSP = v_sp AND SoLuongTon >= v_sl;

                IF ROW_COUNT() <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Tồn kho thay đổi khi tạo dữ liệu demo. Giao dịch đã được hoàn tác.';
                END IF;

                SET v_j = v_j + 1;
            END WHILE;

            SELECT SUM(SoLuong * DonGia) INTO v_tong FROM ChiTietHoaDon WHERE MaHD = v_hd;
            SET v_giam = IF(v_tong >= 10000000 AND MOD(v_i, 2) = 0, 200000, 0);
            UPDATE HoaDon
            SET TongTien = v_tong, GiamGia = v_giam, ThanhToan = v_tong - v_giam
            WHERE MaHD = v_hd;
        END IF;

        SET v_i = v_i + 1;
    END WHILE;

    COMMIT;
END$$
DELIMITER ;

CALL sp_SeedDemoHistory();
DROP PROCEDURE sp_SeedDemoHistory;

SELECT CONCAT('Đã xác nhận dữ liệu demo: ', COUNT(*), ' hóa đơn lịch sử.') AS KetQua
FROM HoaDon WHERE GhiChu LIKE 'DEMO-HISTORY-V1-%';
