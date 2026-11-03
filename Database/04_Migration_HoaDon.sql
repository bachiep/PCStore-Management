/*
  04 - Nâng cấp database cũ: thông tin thanh toán và hủy hóa đơn.
  Chỉ chạy trên database tạo từ schema cũ chưa có các cột này; có thể chạy lại.
  Database cài mới đã có sẵn các cột trong 01_CreateDatabase.sql.
  Chọn database đích trước khi chạy script; script không tự chọn schema.
*/

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_MigrateHoaDon$$
CREATE PROCEDURE sp_MigrateHoaDon()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HoaDon' AND COLUMN_NAME = 'HinhThucTT'
    ) THEN
        ALTER TABLE HoaDon ADD COLUMN HinhThucTT VARCHAR(20) NOT NULL DEFAULT 'Tiền mặt' AFTER GhiChu;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HoaDon' AND COLUMN_NAME = 'DaHuy'
    ) THEN
        ALTER TABLE HoaDon ADD COLUMN DaHuy TINYINT(1) NOT NULL DEFAULT 0;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HoaDon' AND COLUMN_NAME = 'LyDoHuy'
    ) THEN
        ALTER TABLE HoaDon ADD COLUMN LyDoHuy VARCHAR(300) NULL;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HoaDon' AND COLUMN_NAME = 'NgayHuy'
    ) THEN
        ALTER TABLE HoaDon ADD COLUMN NgayHuy DATETIME NULL;
    END IF;
END$$

CALL sp_MigrateHoaDon()$$
DROP PROCEDURE sp_MigrateHoaDon$$
DELIMITER ;
