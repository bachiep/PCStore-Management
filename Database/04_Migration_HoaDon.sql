/* 04 - Nang cap CSDL cu: hinh thuc thanh toan + huy hoa don (chay 1 lan tren DB tao truoc khi co cac cot nay) */
USE QLCuaHangLinhKien;

ALTER TABLE HoaDon
    ADD COLUMN HinhThucTT VARCHAR(20) NOT NULL DEFAULT 'Tiền mặt' AFTER GhiChu,
    ADD COLUMN DaHuy      TINYINT(1)  NOT NULL DEFAULT 0,
    ADD COLUMN LyDoHuy    VARCHAR(300) NULL,
    ADD COLUMN NgayHuy    DATETIME     NULL;
