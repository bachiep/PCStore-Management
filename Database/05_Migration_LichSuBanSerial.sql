/*
  05 - Nâng cấp schema hiện có để lưu snapshot bán hàng và lịch sử serial.
  Chạy sau migration 04 trên database đích đã chọn. Script có thể chạy lại.
  Không xóa bảng hoặc dữ liệu. Snapshot của hóa đơn cũ không được suy đoán từ
  danh mục hiện tại; các trường này để NULL nếu schema trước không lưu chúng.
  Chọn database đích trước khi chạy script; script không tự chọn schema.
*/

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_MigrateLichSuBanSerial$$
CREATE PROCEDURE sp_MigrateLichSuBanSerial()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ChiTietHoaDon' AND COLUMN_NAME = 'TenSPKhiBan'
    ) THEN
        ALTER TABLE ChiTietHoaDon ADD COLUMN TenSPKhiBan VARCHAR(200) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ChiTietHoaDon' AND COLUMN_NAME = 'ThoiGianBHKhiBan'
    ) THEN
        ALTER TABLE ChiTietHoaDon ADD COLUMN ThoiGianBHKhiBan INT NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PhieuBaoHanh' AND COLUMN_NAME = 'MaHDLucTiepNhan'
    ) THEN
        ALTER TABLE PhieuBaoHanh ADD COLUMN MaHDLucTiepNhan INT NULL;
    END IF;

    CREATE TABLE IF NOT EXISTS LichSuBanSerial (
        MaHD    INT         NOT NULL,
        MaSP    INT         NOT NULL,
        Serial  VARCHAR(50) NOT NULL,
        NgayBan DATETIME    NOT NULL,
        HanBH   DATE        NULL,
        PRIMARY KEY (MaHD, Serial),
        INDEX IX_LichSuBanSerial_Serial (Serial),
        CONSTRAINT FK_LSBS_HD FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD),
        CONSTRAINT FK_LSBS_SP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP),
        CONSTRAINT FK_LSBS_Serial FOREIGN KEY (Serial) REFERENCES SanPhamSerial(Serial)
    ) ENGINE=InnoDB;

    /* Bổ sung ràng buộc nếu bảng đã tồn tại từ lần nâng cấp bị ngắt giữa chừng. */
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'LichSuBanSerial'
          AND CONSTRAINT_NAME = 'FK_LSBS_HD' AND CONSTRAINT_TYPE = 'FOREIGN KEY'
    ) THEN
        ALTER TABLE LichSuBanSerial
            ADD CONSTRAINT FK_LSBS_HD FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'LichSuBanSerial'
          AND CONSTRAINT_NAME = 'FK_LSBS_SP' AND CONSTRAINT_TYPE = 'FOREIGN KEY'
    ) THEN
        ALTER TABLE LichSuBanSerial
            ADD CONSTRAINT FK_LSBS_SP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'LichSuBanSerial'
          AND CONSTRAINT_NAME = 'FK_LSBS_Serial' AND CONSTRAINT_TYPE = 'FOREIGN KEY'
    ) THEN
        ALTER TABLE LichSuBanSerial
            ADD CONSTRAINT FK_LSBS_Serial FOREIGN KEY (Serial) REFERENCES SanPhamSerial(Serial);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'PhieuBaoHanh'
          AND CONSTRAINT_NAME = 'FK_PBH_HD' AND CONSTRAINT_TYPE = 'FOREIGN KEY'
    ) THEN
        ALTER TABLE PhieuBaoHanh
            ADD CONSTRAINT FK_PBH_HD FOREIGN KEY (MaHDLucTiepNhan) REFERENCES HoaDon(MaHD);
    END IF;

    /* Hóa đơn cũ không lưu snapshot. Giữ NULL thay vì gán dữ liệu hiện tại. */

    /* Chỉ trạng thái bán hiện tại còn MaHD trên SanPhamSerial. Những liên kết
       lịch sử đã bị xóa bởi phiên bản cũ không thể được suy đoán an toàn. */
    INSERT IGNORE INTO LichSuBanSerial (MaHD, MaSP, Serial, NgayBan, HanBH)
    SELECT ss.MaHD, ss.MaSP, ss.Serial,
           COALESCE(ss.NgayBan, hd.NgayLap), ss.HanBH
    FROM SanPhamSerial ss
    JOIN HoaDon hd ON hd.MaHD = ss.MaHD
    WHERE ss.MaHD IS NOT NULL;

    /* Giữ NULL với phiếu bảo hành cũ: serial có thể đã được hủy bán và bán lại,
       nên MaHD hiện tại không chứng minh được hóa đơn lúc tiếp nhận bảo hành. */
END$$

CALL sp_MigrateLichSuBanSerial()$$
DROP PROCEDURE sp_MigrateLichSuBanSerial$$
DELIMITER ;
