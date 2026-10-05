/* =========================================================
   Phần mềm quản lý cửa hàng linh kiện & thiết bị máy tính
   01 - Tạo CSDL và các bảng (MySQL 8.0+)
   Chạy:  mysql -h 127.0.0.1 -P 3307 -u root < 01_CreateDatabase.sql
   ========================================================= */
DROP DATABASE IF EXISTS QLCuaHangLinhKien;
CREATE DATABASE QLCuaHangLinhKien CHARACTER SET utf8mb4 COLLATE utf8mb4_vietnamese_ci;
USE QLCuaHangLinhKien;

/* ---------- Nhân viên & tài khoản ---------- */
CREATE TABLE NhanVien (
    MaNV        INT AUTO_INCREMENT PRIMARY KEY,
    HoTen       VARCHAR(100) NOT NULL,
    GioiTinh    VARCHAR(10)  NULL,
    NgaySinh    DATE         NULL,
    SDT         VARCHAR(15)  NULL,
    Email       VARCHAR(100) NULL,
    DiaChi      VARCHAR(200) NULL,
    ChucVu      VARCHAR(50)  NOT NULL DEFAULT 'Nhân viên bán hàng',
    TrangThai   TINYINT(1)   NOT NULL DEFAULT 1   -- 1: đang làm, 0: nghỉ
);

CREATE TABLE TaiKhoan (
    TenDangNhap VARCHAR(50)  PRIMARY KEY,
    MatKhauHash VARCHAR(100) NOT NULL,
    MaNV        INT          NOT NULL UNIQUE,
    VaiTro      VARCHAR(20)  NOT NULL DEFAULT 'NhanVien',
    TrangThai   TINYINT(1)   NOT NULL DEFAULT 1,  -- 1: hoạt động, 0: khóa
    CONSTRAINT FK_TaiKhoan_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    CONSTRAINT CK_TaiKhoan_VaiTro CHECK (VaiTro IN ('Admin','NhanVien'))
);

/* ---------- Danh mục, hãng, sản phẩm ---------- */
CREATE TABLE DanhMuc (
    MaDM  INT AUTO_INCREMENT PRIMARY KEY,
    TenDM VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Hang (
    MaHang  INT AUTO_INCREMENT PRIMARY KEY,
    TenHang VARCHAR(100) NOT NULL UNIQUE,
    QuocGia VARCHAR(50)  NULL
);

CREATE TABLE SanPham (
    MaSP        INT AUTO_INCREMENT PRIMARY KEY,
    TenSP       VARCHAR(200)  NOT NULL,
    MaDM        INT           NOT NULL,
    MaHang      INT           NOT NULL,
    GiaNhap     DECIMAL(18,0) NOT NULL DEFAULT 0,
    GiaBan      DECIMAL(18,0) NOT NULL DEFAULT 0,
    SoLuongTon  INT           NOT NULL DEFAULT 0,
    ThoiGianBH  INT           NOT NULL DEFAULT 12,   -- tháng
    MoTa        TEXT          NULL,
    HinhAnh     VARCHAR(260)  NULL,
    TrangThai   TINYINT(1)    NOT NULL DEFAULT 1,    -- 1: đang kinh doanh
    CONSTRAINT FK_SanPham_DanhMuc FOREIGN KEY (MaDM)   REFERENCES DanhMuc(MaDM),
    CONSTRAINT FK_SanPham_Hang    FOREIGN KEY (MaHang) REFERENCES Hang(MaHang),
    CONSTRAINT CK_SanPham_GiaNhap CHECK (GiaNhap >= 0),
    CONSTRAINT CK_SanPham_GiaBan  CHECK (GiaBan >= 0),
    CONSTRAINT CK_SanPham_Ton     CHECK (SoLuongTon >= 0),
    CONSTRAINT CK_SanPham_BH      CHECK (ThoiGianBH >= 0),
    INDEX IX_SanPham_Ten (TenSP)
);

/* ---------- Nhà cung cấp & nhập kho ---------- */
CREATE TABLE NhaCungCap (
    MaNCC  INT AUTO_INCREMENT PRIMARY KEY,
    TenNCC VARCHAR(150) NOT NULL,
    SDT    VARCHAR(15)  NULL,
    Email  VARCHAR(100) NULL,
    DiaChi VARCHAR(200) NULL
);

CREATE TABLE PhieuNhap (
    MaPN     INT AUTO_INCREMENT PRIMARY KEY,
    NgayNhap DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    MaNCC    INT           NOT NULL,
    MaNV     INT           NOT NULL,
    TongTien DECIMAL(18,0) NOT NULL DEFAULT 0,
    GhiChu   VARCHAR(300)  NULL,
    CONSTRAINT FK_PN_NCC FOREIGN KEY (MaNCC) REFERENCES NhaCungCap(MaNCC),
    CONSTRAINT FK_PN_NV  FOREIGN KEY (MaNV)  REFERENCES NhanVien(MaNV)
);

CREATE TABLE ChiTietPhieuNhap (
    MaPN    INT           NOT NULL,
    MaSP    INT           NOT NULL,
    SoLuong INT           NOT NULL,
    DonGia  DECIMAL(18,0) NOT NULL,
    PRIMARY KEY (MaPN, MaSP),
    CONSTRAINT FK_CTPN_PN FOREIGN KEY (MaPN) REFERENCES PhieuNhap(MaPN),
    CONSTRAINT FK_CTPN_SP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP),
    CONSTRAINT CK_CTPN_SL CHECK (SoLuong > 0),
    CONSTRAINT CK_CTPN_DG CHECK (DonGia >= 0)
);

/* ---------- Khách hàng & bán hàng ---------- */
CREATE TABLE KhachHang (
    MaKH   INT AUTO_INCREMENT PRIMARY KEY,
    HoTen  VARCHAR(100) NOT NULL,
    SDT    VARCHAR(15)  NOT NULL UNIQUE,
    DiaChi VARCHAR(200) NULL,
    Email  VARCHAR(100) NULL
);

CREATE TABLE HoaDon (
    MaHD      INT AUTO_INCREMENT PRIMARY KEY,
    NgayLap   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    MaKH      INT           NULL,                  -- NULL: khách lẻ
    MaNV      INT           NOT NULL,
    TongTien  DECIMAL(18,0) NOT NULL DEFAULT 0,
    GiamGia   DECIMAL(18,0) NOT NULL DEFAULT 0,
    ThanhToan DECIMAL(18,0) NOT NULL DEFAULT 0,
    GhiChu    VARCHAR(300)  NULL,
    HinhThucTT VARCHAR(20)  NOT NULL DEFAULT 'Tiền mặt',
    DaHuy     TINYINT(1)    NOT NULL DEFAULT 0,
    LyDoHuy   VARCHAR(300)  NULL,
    NgayHuy   DATETIME      NULL,
    CONSTRAINT FK_HD_KH FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    CONSTRAINT FK_HD_NV FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    CONSTRAINT CK_HD_GiamGia CHECK (GiamGia >= 0),
    INDEX IX_HoaDon_NgayLap (NgayLap)
);

CREATE TABLE ChiTietHoaDon (
    MaHD    INT           NOT NULL,
    MaSP    INT           NOT NULL,
    SoLuong INT           NOT NULL,
    DonGia  DECIMAL(18,0) NOT NULL,
    PRIMARY KEY (MaHD, MaSP),
    CONSTRAINT FK_CTHD_HD FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD),
    CONSTRAINT FK_CTHD_SP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP),
    CONSTRAINT CK_CTHD_SL CHECK (SoLuong > 0),
    CONSTRAINT CK_CTHD_DG CHECK (DonGia >= 0)
);

/* ---------- Serial & bảo hành ---------- */
CREATE TABLE SanPhamSerial (
    Serial    VARCHAR(50) PRIMARY KEY,
    MaSP      INT         NOT NULL,
    MaPN      INT         NULL,
    MaHD      INT         NULL,
    NgayBan   DATETIME    NULL,
    HanBH     DATE        NULL,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'TrongKho',
    CONSTRAINT FK_Serial_SP FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP),
    CONSTRAINT FK_Serial_PN FOREIGN KEY (MaPN) REFERENCES PhieuNhap(MaPN),
    CONSTRAINT FK_Serial_HD FOREIGN KEY (MaHD) REFERENCES HoaDon(MaHD),
    CONSTRAINT CK_Serial_TT CHECK (TrangThai IN ('TrongKho','DaBan','DangBaoHanh','Loi')),
    INDEX IX_Serial_MaSP (MaSP, TrangThai)
);

CREATE TABLE PhieuBaoHanh (
    MaPBH     INT AUTO_INCREMENT PRIMARY KEY,
    Serial    VARCHAR(50)  NOT NULL,
    MaKH      INT          NULL,
    MaNV      INT          NOT NULL,
    NgayNhan  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    MoTaLoi   VARCHAR(500) NOT NULL,
    NgayTra   DATETIME     NULL,
    KetQua    VARCHAR(500) NULL,
    TrangThai VARCHAR(30)  NOT NULL DEFAULT 'Đang xử lý',
    CONSTRAINT FK_PBH_Serial FOREIGN KEY (Serial) REFERENCES SanPhamSerial(Serial),
    CONSTRAINT FK_PBH_KH     FOREIGN KEY (MaKH)   REFERENCES KhachHang(MaKH),
    CONSTRAINT FK_PBH_NV     FOREIGN KEY (MaNV)   REFERENCES NhanVien(MaNV),
    CONSTRAINT CK_PBH_TT CHECK (TrangThai IN ('Đang xử lý', 'Đã trả khách', 'Từ chối'))
);
