using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ComputerStore.BLL;
using ComputerStore.DTO;
using ComputerStore.GUI.Components;
using ComputerStore.GUI.Forms;

namespace ComputerStore.GUI.Pages;

public class ucBanHang : PageBase
{
    private readonly HoaDonBLL _bll = new();
    private readonly SanPhamBLL _spBll = new();
    private readonly KhachHangBLL _khBll = new();
    private readonly DanhMucBLL _dmBll = new();

    // --- Left Panel Controls (Search & Products) ---
    private readonly TextBox _txtSearchSP = Theme.Input(340, "🔍 Tìm tên, mã SP hoặc quét Barcode (F2)...");
    private readonly FlowLayoutPanel _flowCategories = new();
    private readonly Panel _productContainer = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _gridProducts = Theme.Grid();
    private readonly FlowLayoutPanel _flowProducts = new();
    private readonly ModernButton _btnToggleView;
    private bool _isCardView = false;
    private List<SanPhamDTO> _currentFilteredProducts = new();

    // --- Right Panel Controls (Cart & Checkout) ---
    private readonly DataGridView _grid = Theme.Grid();
    private readonly BindingList<ChiTietHoaDonDTO> _items = new();
    private readonly Label _lblCartHeader = new() { AutoSize = true, Font = Theme.Bold, ForeColor = Theme.Text, Text = "GIỎ HÀNG (0 món)" };

    private readonly TextBox _txtSdtKH = Theme.Input(240, "SĐT khách (F4) - Enter...");
    private readonly Label _lblKhach = new() { AutoSize = true, Font = Theme.Small, ForeColor = Theme.Muted, UseMnemonic = false, Text = "👤 Khách lẻ (không lưu thông tin)" };
    private KhachHangDTO? _khachHang;

    private readonly TextBox _txtGiamGia = new() { Width = 80, Font = new Font("Segoe UI", 10f, FontStyle.Regular), TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.FixedSingle, Text = "0" };
    private readonly ComboBox _cboLoaiGiamGia = new() { Width = 60, Font = Theme.Small, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
    private readonly Label _lblGiamGiaPreview = new() { AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = ColorTranslator.FromHtml("#DC2626"), Text = "" };
    private readonly ComboBox _cboHTTT = new() { Width = 150, Font = Theme.Base, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
    private readonly Label _lblTongTien = new() { Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), AutoSize = true, ForeColor = Theme.Text, Text = "0 ₫" };
    private readonly ModernPanel _pnlTotalHero = new()
    {
        Height = 44,
        Dock = DockStyle.Top,
        FillColor = ColorTranslator.FromHtml("#F1F5F9"),
        BorderColor = ColorTranslator.FromHtml("#E2E8F0"),
        BorderSize = 1,
        BorderRadius = 8,
        Padding = new Padding(12, 0, 12, 0),
        Margin = new Padding(0, 4, 0, 8)
    };
    private readonly Label _lblTotalTitle = new()
    {
        Text = "KHÁCH CẦN TRẢ:",
        Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
        ForeColor = ColorTranslator.FromHtml("#64748B"),
        AutoSize = true,
        Dock = DockStyle.Left,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _lblThanhToan = new()
    {
        Font = new Font("Segoe UI", 19f, FontStyle.Bold),
        AutoSize = true,
        ForeColor = ColorTranslator.FromHtml("#94A3B8"),
        Text = "0 ₫",
        Dock = DockStyle.Right,
        TextAlign = ContentAlignment.MiddleRight
    };
    private readonly TextBox _txtGhiChu = Theme.Input(200, "Ghi chú đơn hàng (Giao lúc 5h chiều...)");
    private readonly ModernButton _btnHoldOrder = new()
    {
        Text = "⏱ Treo HĐ (F10)",
        Width = 135,
        Height = 44,
        BorderRadius = 8,
        NormalColor = Color.White,
        HoverColor = ColorTranslator.FromHtml("#F1F5F9"),
        ForeColor = ColorTranslator.FromHtml("#334155"),
        BorderColor = ColorTranslator.FromHtml("#CBD5E1"),
        BorderSize = 1,
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        Cursor = Cursors.Hand
    };

    private record HeldBill(
        KhachHangDTO? Khach,
        List<ChiTietHoaDonDTO> Items,
        string GiamGiaText,
        int LoaiGiamGia,
        string HTTT,
        string GhiChu,
        DateTime CreatedAt);
    private readonly List<HeldBill> _heldBills = new();

    public ucBanHang() : base("Bán hàng (POS)")
    {
        Name = "ucBanHang";

        // ==========================================
        // 1. LEFT PANEL (60%): Tìm kiếm & Danh sách linh kiện
        // ==========================================
        _txtSearchSP.Font = new Font("Segoe UI", 10f);
        _txtSearchSP.Margin = new Padding(0, 0, 8, 0);

        _btnToggleView = Theme.GhostButton("Dạng lưới ảnh", 116, (_, _) => ToggleView());
        _btnToggleView.Margin = new Padding(0, 0, 8, 0);
        var tipToggle = new ToolTip();
        tipToggle.SetToolTip(_btnToggleView, "Chuyển đổi giao diện: Lưới hình ảnh / Bảng danh sách");

        var infoHotkeys = Theme.InfoBadge(
            "• F2: Nhảy con trỏ vào ô Tìm kiếm linh kiện / Quét mã vạch\n" +
            "• Enter: Thêm linh kiện đang chọn vào giỏ hàng\n" +
            "• F4: Nhảy con trỏ vào ô nhập SĐT khách hàng\n" +
            "• F9: Bấm Thanh toán & In hóa đơn PDF\n" +
            "• Esc: Xóa sạch giỏ hàng\n" +
            "• Bấm nút [✕] ở cuối dòng để xóa món khỏi giỏ\n" +
            "• Chỉnh sửa cột SL trong giỏ hàng: Gõ trực tiếp số lượng mới",
            "Phím tắt & Hướng dẫn Bán hàng (POS)");

        var topSearchRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false, AutoSize = false };
        topSearchRow.Controls.Add(_txtSearchSP);
        topSearchRow.Controls.Add(_btnToggleView);
        topSearchRow.Controls.Add(infoHotkeys);

        // Thanh danh mục cuộn mượt bằng TableLayoutPanel (cột trái 28px, clipper 100%, cột phải 28px)
        var pnlCategoryBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        pnlCategoryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28f));
        pnlCategoryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlCategoryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28f));
        pnlCategoryBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var btnCatLeft = new ModernButton
        {
            Text = "◀",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 3, 2, 3),
            BorderRadius = 6,
            NormalColor = Color.White,
            HoverColor = ColorTranslator.FromHtml("#F1F5F9"),
            ForeColor = ColorTranslator.FromHtml("#64748B"),
            BorderColor = ColorTranslator.FromHtml("#CBD5E1"),
            BorderSize = 1,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        var tipCatLeft = new ToolTip();
        tipCatLeft.SetToolTip(btnCatLeft, "Cuộn danh mục sang trái");

        var btnCatRight = new ModernButton
        {
            Text = "▶",
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 3, 0, 3),
            BorderRadius = 6,
            NormalColor = Color.White,
            HoverColor = ColorTranslator.FromHtml("#F1F5F9"),
            ForeColor = ColorTranslator.FromHtml("#64748B"),
            BorderColor = ColorTranslator.FromHtml("#CBD5E1"),
            BorderSize = 1,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        var tipCatRight = new ToolTip();
        tipCatRight.SetToolTip(btnCatRight, "Cuộn danh mục sang phải");

        var pnlCatClipper = new Panel { Dock = DockStyle.Fill, Height = 36, Padding = new Padding(2, 0, 2, 0), Margin = new Padding(0) };

        _flowCategories.Dock = DockStyle.Top;
        _flowCategories.Height = 58;
        _flowCategories.WrapContents = false;
        _flowCategories.AutoScroll = true;
        _flowCategories.Padding = new Padding(0, 3, 0, 0);
        typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty, null, _flowCategories, new object[] { true });

        pnlCatClipper.Controls.Add(_flowCategories);

        btnCatLeft.Click += (_, _) =>
        {
            var curX = -_flowCategories.AutoScrollPosition.X;
            _flowCategories.AutoScrollPosition = new Point(Math.Max(0, curX - 180), 0);
        };
        btnCatRight.Click += (_, _) =>
        {
            var curX = -_flowCategories.AutoScrollPosition.X;
            _flowCategories.AutoScrollPosition = new Point(curX + 180, 0);
        };

        _flowCategories.MouseWheel += (_, e) => ScrollCategories(e.Delta);
        pnlCatClipper.MouseWheel += (_, e) => ScrollCategories(e.Delta);

        pnlCategoryBar.Controls.Add(btnCatLeft, 0, 0);
        pnlCategoryBar.Controls.Add(pnlCatClipper, 1, 0);
        pnlCategoryBar.Controls.Add(btnCatRight, 2, 0);

        var leftHeader = new Panel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(0, 0, 0, 4) };
        leftHeader.Controls.Add(pnlCategoryBar);
        leftHeader.Controls.Add(topSearchRow);

        // Product Grid (Table View - Default)
        _gridProducts.AutoGenerateColumns = false;
        _gridProducts.Columns.AddRange(
            Theme.Col("MaSP", "Mã", weight: 0.5f),
            Theme.Col("TenSP", "Tên linh kiện đầy đủ", weight: 3f),
            Theme.Col("TenDM", "Danh mục", weight: 1f),
            Theme.Col("SoLuongTon", "Tồn kho", weight: 0.7f, right: true),
            Theme.Col("GiaBan", "Đơn giá", weight: 1.1f, format: "N0", right: true)
        );
        _gridProducts.CellFormatting += (_, e) =>
        {
            if (_gridProducts.Rows[e.RowIndex].DataBoundItem is SanPhamDTO sp)
            {
                if (sp.SoLuongTon <= 0 || !sp.TrangThai)
                {
                    e.CellStyle!.ForeColor = Theme.Danger;
                    if (_gridProducts.Columns[e.ColumnIndex].Name == "SoLuongTon")
                    {
                        e.Value = "Hết hàng";
                        e.FormattingApplied = true;
                    }
                }
            }
        };
        _gridProducts.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _gridProducts.Rows[e.RowIndex].DataBoundItem is SanPhamDTO sp)
                OnProductClicked(this, sp);
        };
        _gridProducts.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_gridProducts.CurrentRow?.DataBoundItem is SanPhamDTO sp)
                    OnProductClicked(this, sp);
            }
        };

        // Product Flow (Card View)
        _flowProducts.Dock = DockStyle.Fill;
        _flowProducts.AutoScroll = true;
        _flowProducts.BackColor = Theme.Page;
        _flowProducts.Padding = new Padding(4, 4, 4, 30);
        typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty, null, _flowProducts, new object[] { true });

        _productContainer.Padding = new Padding(0, 0, 0, 6);
        _productContainer.Controls.Add(GridCard(_gridProducts));

        var leftPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        leftPanel.Controls.Add(_productContainer);
        leftPanel.Controls.Add(leftHeader);

        // ==========================================
        // 2. RIGHT PANEL (40%): Giỏ hàng & Thanh toán
        // ==========================================
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(8),
            BackColor = Color.White
        };
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f)); // Row 0: Khách hàng (F4)
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Row 1: Giỏ hàng
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 276f)); // Row 2: Thanh toán

        // Top: Khách hàng (Thẻ Khách Hàng chuyên nghiệp, có viền bo tròn)
        var pnlCustomer = new ModernPanel
        {
            Dock = DockStyle.Fill,
            FillColor = ColorTranslator.FromHtml("#F8FAFC"),
            BorderColor = ColorTranslator.FromHtml("#E2E8F0"),
            BorderSize = 1,
            BorderRadius = 8,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 8)
        };

        var tblCustLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        tblCustLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tblCustLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        tblCustLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        tblCustLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));

        var pnlCustTop = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.Transparent };
        var lblCustTitle = new Label
        {
            Text = "👤 KHÁCH HÀNG (F4)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#1E293B"),
            AutoSize = true,
            Location = new Point(0, 5),
            BackColor = Color.Transparent
        };
        var tipCust = new ToolTip();
        tipCust.SetToolTip(lblCustTitle, "Nhập SĐT khách hàng rồi ấn Enter. F4 để nhảy con trỏ.");

        var btnAddKH = new ModernButton
        {
            Text = "+ Thêm mới",
            Width = 96,
            Height = 26,
            BorderRadius = 6,
            NormalColor = ColorTranslator.FromHtml("#2563EB"),
            HoverColor = ColorTranslator.FromHtml("#1D4ED8"),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right
        };
        btnAddKH.Click += (_, _) => ThemKHNhanh();

        pnlCustTop.Controls.Add(lblCustTitle);
        pnlCustTop.Controls.Add(btnAddKH);

        _txtSdtKH.Font = new Font("Segoe UI", 10f);
        _txtSdtKH.PlaceholderText = "🔍 Nhập SĐT tìm khách (Enter)...";
        _txtSdtKH.Dock = DockStyle.Fill;
        _txtSdtKH.Margin = new Padding(0, 3, 0, 5);

        var pnlCustBadge = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 0), Margin = new Padding(0), AutoSize = false, BackColor = Color.Transparent };
        _lblKhach.AutoSize = true;
        _lblKhach.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        _lblKhach.Padding = new Padding(10, 4, 10, 4);
        _lblKhach.Margin = new Padding(0);
        ResetKhachBadge();
        _lblKhach.Click += (_, _) =>
        {
            if (_khachHang != null)
            {
                _txtSdtKH.Clear();
                ResetKhachBadge();
            }
        };
        pnlCustBadge.Controls.Add(_lblKhach);

        tblCustLayout.Controls.Add(pnlCustTop, 0, 0);
        tblCustLayout.Controls.Add(_txtSdtKH, 0, 1);
        tblCustLayout.Controls.Add(pnlCustBadge, 0, 2);

        pnlCustomer.Controls.Add(tblCustLayout);

        _txtSdtKH.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; TimKH(); } };
        _txtSdtKH.TextChanged += (_, _) =>
        {
            if (_khachHang != null && _khachHang.SDT != _txtSdtKH.Text.Trim())
            {
                ResetKhachBadge();
            }
        };

        // Middle: Cart Grid & Header
        var pnlCartHeader = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(4, 2, 8, 2) };
        _lblCartHeader.Dock = DockStyle.Left;
        _lblCartHeader.TextAlign = ContentAlignment.MiddleLeft;
        _lblCartHeader.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        _lblCartHeader.ForeColor = ColorTranslator.FromHtml("#1E293B");

        var btnClearCart = new Button
        {
            Text = "🗑 Xóa tất cả",
            Dock = DockStyle.Right,
            Height = 26,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = ColorTranslator.FromHtml("#DC2626"),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnClearCart.FlatAppearance.BorderSize = 0;
        btnClearCart.Click += (_, _) =>
        {
            if (_items.Count > 0 && Msg.Confirm("Bạn có chắc muốn xóa sạch giỏ hàng này?"))
                _items.Clear();
        };

        var tipClear = new ToolTip();
        tipClear.SetToolTip(btnClearCart, "Xóa toàn bộ sản phẩm trong giỏ hàng (Esc)");

        pnlCartHeader.Controls.Add(_lblCartHeader);
        pnlCartHeader.Controls.Add(btnClearCart);

        _grid.AutoGenerateColumns = false;
        _grid.RowTemplate.Height = 34;
        _grid.ColumnHeadersHeight = 34;
        _grid.GridColor = ColorTranslator.FromHtml("#E2E8F0");
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
        _grid.DefaultCellStyle.Padding = new Padding(3, 0, 3, 0);
        _grid.Columns.Clear();

        var colTen = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "TenSP",
            HeaderText = "Sản phẩm",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 100f
        };

        var colDonGia = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "DonGia",
            HeaderText = "Đơn giá",
            Width = 92,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        colDonGia.DefaultCellStyle.Format = "N0";
        colDonGia.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        colDonGia.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

        var colSL = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "SoLuong",
            HeaderText = "SL",
            Width = 46,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        colSL.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colSL.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

        var colThanhTien = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ThanhTien",
            HeaderText = "Thành tiền",
            Width = 98,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        colThanhTien.DefaultCellStyle.Format = "N0";
        colThanhTien.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        colThanhTien.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

        var colDelete = new DataGridViewButtonColumn
        {
            Name = "ColXoa",
            HeaderText = "",
            Text = "✕",
            UseColumnTextForButtonValue = true,
            Width = 26,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            FlatStyle = FlatStyle.Flat
        };
        colDelete.DefaultCellStyle.ForeColor = Theme.Danger;
        colDelete.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        colDelete.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        _grid.Columns.AddRange(colTen, colDonGia, colSL, colThanhTien, colDelete);

        _grid.DataSource = _items;
        Theme.EmptyHint(_grid, "Giỏ hàng trống. Chọn sản phẩm bên trái.");
        _grid.ReadOnly = false;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        foreach (DataGridViewColumn col in _grid.Columns) if (col.DataPropertyName != "SoLuong") col.ReadOnly = true;
        _grid.CellEndEdit += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].DataPropertyName == "SoLuong")
            {
                var row = _grid.Rows[e.RowIndex];
                if (row.DataBoundItem is ChiTietHoaDonDTO item)
                {
                    if (item.SoLuong <= 0) { _items.Remove(item); }
                    else { _items.ResetItem(_items.IndexOf(item)); }
                    UpdateTien();
                }
            }
        };
        _grid.DataError += (_, e) => { e.ThrowException = false; Msg.Warn("Vui lòng nhập số lượng nguyên hợp lệ!"); };
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) RemoveItem(); };
        _grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "ColXoa")
            {
                if (_grid.Rows[e.RowIndex].DataBoundItem is ChiTietHoaDonDTO item)
                {
                    _items.Remove(item);
                    UpdateTien();
                }
            }
        };

        var pnlCartGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0), Margin = new Padding(0, 0, 0, 8) };
        pnlCartGrid.Controls.Add(GridCard(_grid));
        pnlCartGrid.Controls.Add(pnlCartHeader);

        // Bottom: Fixed Payment Panel (Dock = Fill inside Row 2 of rightPanel TableLayoutPanel)
        var pnlCheckout = new ModernPanel
        {
            Dock = DockStyle.Fill,
            FillColor = ColorTranslator.FromHtml("#F8FAFC"),
            BorderColor = ColorTranslator.FromHtml("#E2E8F0"),
            BorderSize = 1,
            BorderRadius = 10,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0)
        };

        _cboLoaiGiamGia.Items.AddRange(new object[] { "VNĐ", "%" });
        _cboLoaiGiamGia.SelectedIndex = 0;
        _cboLoaiGiamGia.SelectedIndexChanged += (_, _) =>
        {
            _txtGiamGia.Text = "0";
            UpdateTien();
        };

        _txtGiamGia.TextChanged += (_, _) =>
        {
            if (_cboLoaiGiamGia.SelectedIndex == 0)
            {
                if (decimal.TryParse(_txtGiamGia.Text.Replace(".", "").Replace(",", ""), out var v))
                {
                    _txtGiamGia.Text = v.ToString("N0");
                    _txtGiamGia.SelectionStart = _txtGiamGia.Text.Length;
                }
            }
            UpdateTien();
        };

        _cboHTTT.DataSource = HinhThucThanhToan.TatCa.ToList();
        _cboHTTT.Dock = DockStyle.Fill;
        _cboHTTT.Font = new Font("Segoe UI", 9.5f);
        _cboHTTT.Margin = new Padding(0, 3, 0, 3);

        _txtGhiChu.Dock = DockStyle.Fill;
        _txtGhiChu.Font = new Font("Segoe UI", 9.5f);
        _txtGhiChu.Multiline = false;
        _txtGhiChu.Margin = new Padding(0, 3, 0, 3);
        _txtGhiChu.BorderStyle = BorderStyle.FixedSingle;

        var pnlGiamGiaWrap = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoSize = false,
            Margin = new Padding(0),
            Padding = new Padding(0, 3, 0, 3)
        };
        _txtGiamGia.Font = new Font("Segoe UI", 9.5f);
        _txtGiamGia.Height = 26;
        _txtGiamGia.Margin = new Padding(0, 0, 6, 0);
        _cboLoaiGiamGia.Font = new Font("Segoe UI", 9.5f);
        _cboLoaiGiamGia.Height = 26;
        _cboLoaiGiamGia.Margin = new Padding(0, 0, 8, 0);
        _lblGiamGiaPreview.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
        _lblGiamGiaPreview.Margin = new Padding(0, 4, 0, 0);
        pnlGiamGiaWrap.Controls.Add(_txtGiamGia);
        pnlGiamGiaWrap.Controls.Add(_cboLoaiGiamGia);
        pnlGiamGiaWrap.Controls.Add(_lblGiamGiaPreview);

        var tblPayInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        tblPayInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125f));
        tblPayInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tblPayInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        tblPayInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        tblPayInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        tblPayInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));

        var l1 = new Label { Text = "Tổng tiền hàng:", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorTranslator.FromHtml("#64748B"), AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };
        var l2 = new Label { Text = "Giảm giá:", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorTranslator.FromHtml("#64748B"), AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };
        var l3 = new Label { Text = "Hình thức TT:", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorTranslator.FromHtml("#64748B"), AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };
        var l4 = new Label { Text = "Ghi chú:", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorTranslator.FromHtml("#64748B"), AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };

        _lblTongTien.Dock = DockStyle.Fill;
        _lblTongTien.TextAlign = ContentAlignment.MiddleRight;
        _lblTongTien.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _lblTongTien.ForeColor = ColorTranslator.FromHtml("#1E293B");
        _lblTongTien.BackColor = Color.Transparent;

        tblPayInfo.Controls.Add(l1, 0, 0); tblPayInfo.Controls.Add(_lblTongTien, 1, 0);
        tblPayInfo.Controls.Add(l2, 0, 1); tblPayInfo.Controls.Add(pnlGiamGiaWrap, 1, 1);
        tblPayInfo.Controls.Add(l3, 0, 2); tblPayInfo.Controls.Add(_cboHTTT, 1, 2);
        tblPayInfo.Controls.Add(l4, 0, 3); tblPayInfo.Controls.Add(_txtGhiChu, 1, 3);

        // Khung card viền bo tròn bọc riêng cụm trường nhập liệu
        var pnlPayFieldsCard = new ModernPanel
        {
            Dock = DockStyle.Fill,
            FillColor = Color.White,
            BorderColor = ColorTranslator.FromHtml("#E2E8F0"),
            BorderSize = 1,
            BorderRadius = 8,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 0, 0, 6)
        };
        pnlPayFieldsCard.Controls.Add(tblPayInfo);

        // Hero Card: [ KHÁCH CẦN TRẢ:                   38.360.000 ₫ ]
        _pnlTotalHero.Dock = DockStyle.Fill;
        _pnlTotalHero.Margin = new Padding(0, 0, 0, 6);
        _lblTotalTitle.AutoSize = false;
        _lblTotalTitle.Width = 145;
        _lblTotalTitle.Dock = DockStyle.Left;
        _lblTotalTitle.TextAlign = ContentAlignment.MiddleLeft;
        _lblTotalTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _lblTotalTitle.BackColor = Color.Transparent;

        _lblThanhToan.AutoSize = false;
        _lblThanhToan.Dock = DockStyle.Fill;
        _lblThanhToan.TextAlign = ContentAlignment.MiddleRight;
        _lblThanhToan.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
        _lblThanhToan.BackColor = Color.Transparent;

        _pnlTotalHero.Controls.Clear();
        _pnlTotalHero.Controls.Add(_lblThanhToan);
        _pnlTotalHero.Controls.Add(_lblTotalTitle);

        // Action Buttons Row: [⏱ Treo đơn (F10)] [THANH TOÁN (F9) 💳]
        var pnlActions = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.Transparent };
        _btnHoldOrder.Dock = DockStyle.Left;
        _btnHoldOrder.Width = 135;
        _btnHoldOrder.Height = 44;
        _btnHoldOrder.BorderRadius = 8;
        _btnHoldOrder.BorderSize = 1;
        _btnHoldOrder.BorderColor = ColorTranslator.FromHtml("#CBD5E1");
        _btnHoldOrder.NormalColor = Color.White;
        _btnHoldOrder.HoverColor = ColorTranslator.FromHtml("#F1F5F9");
        _btnHoldOrder.ForeColor = ColorTranslator.FromHtml("#334155");
        _btnHoldOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _btnHoldOrder.Click += (_, _) => { if (_heldBills.Count > 0 && _items.Count == 0) ShowHeldBillsMenu(); else TreoDon(); };

        var btnPay = new ModernButton
        {
            Text = "THANH TOÁN (F9) 💳",
            Dock = DockStyle.Fill,
            Height = 44,
            BorderRadius = 8,
            NormalColor = ColorTranslator.FromHtml("#16A34A"),
            HoverColor = ColorTranslator.FromHtml("#15803D"),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPay.Click += (_, _) => ThanhToan();

        var pnlPayWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0), BackColor = Color.Transparent };
        pnlPayWrapper.Controls.Add(btnPay);

        pnlActions.Controls.Add(pnlPayWrapper);
        pnlActions.Controls.Add(_btnHoldOrder);

        // Assemble Layout inside pnlCheckout using an explicit TableLayoutPanel to ensure strict row ordering
        var tblCheckoutLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        tblCheckoutLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tblCheckoutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f)); // pnlPayFieldsCard
        tblCheckoutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // _pnlTotalHero
        tblCheckoutLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));  // pnlActions

        tblCheckoutLayout.Controls.Add(pnlPayFieldsCard, 0, 0);
        tblCheckoutLayout.Controls.Add(_pnlTotalHero, 0, 1);
        tblCheckoutLayout.Controls.Add(pnlActions, 0, 2);

        pnlCheckout.Controls.Add(tblCheckoutLayout);

        // Assemble Right Panel TableLayoutPanel (0: Customer, 1: Cart, 2: Checkout)
        rightPanel.Controls.Add(pnlCustomer, 0, 0);
        rightPanel.Controls.Add(pnlCartGrid, 0, 1);
        rightPanel.Controls.Add(pnlCheckout, 0, 2);

        // ==========================================
        // 3. MAIN LAYOUT: Trái co giãn (100%) : Phải cố định chuẩn POS (450px)
        // ==========================================
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainLayout.Controls.Add(leftPanel, 0, 0);
        mainLayout.Controls.Add(rightPanel, 1, 0);

        AddRow(mainLayout, fill: true);

        // Event hooks
        Debounce(_txtSearchSP, ReloadProducts);
        _txtSearchSP.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_currentFilteredProducts.Count == 1)
                {
                    OnProductClicked(this, _currentFilteredProducts[0]);
                    _txtSearchSP.SelectAll();
                }
                else if (_currentFilteredProducts.Count > 1 && !_isCardView && _gridProducts.Rows.Count > 0)
                {
                    _gridProducts.Focus();
                    _gridProducts.Rows[0].Selected = true;
                }
            }
            else if (e.KeyCode == Keys.Down && !_isCardView && _gridProducts.Rows.Count > 0)
            {
                e.SuppressKeyPress = true;
                _gridProducts.Focus();
            }
        };

        _items.ListChanged += (_, _) => UpdateTien();
        UpdateTien();
    }

    private void ToggleView()
    {
        _isCardView = !_isCardView;
        _btnToggleView.Text = _isCardView ? "Dạng danh sách" : "Dạng lưới ảnh";
        _btnToggleView.Width = _isCardView ? 124 : 116;
        _productContainer.Controls.Clear();
        if (_isCardView)
        {
            _flowProducts.Dock = DockStyle.Fill;
            _productContainer.Controls.Add(_flowProducts);
        }
        else
        {
            _gridProducts.Dock = DockStyle.Fill;
            _productContainer.Controls.Add(GridCard(_gridProducts));
        }
        ReloadProducts();
    }

    private void ScrollCategories(int delta)
    {
        var curX = -_flowCategories.AutoScrollPosition.X;
        var step = delta > 0 ? -120 : 120;
        _flowCategories.AutoScrollPosition = new Point(Math.Max(0, curX + step), 0);
    }

    public override void LoadData()
    {
        Msg.Run(() =>
        {
            var dms = _dmBll.GetAll();
            _flowCategories.Controls.Clear();
            var btnAll = Theme.GhostButton("Tất cả", 50, (_, _) => { _flowCategories.Tag = 0; ReloadProducts(); });
            btnAll.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnAll.Padding = new Padding(6, 0, 6, 0);
            btnAll.Margin = new Padding(2, 0, 2, 0);
            btnAll.Height = 28;
            btnAll.AutoSize = true;
            btnAll.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnAll.MouseWheel += (_, e) => ScrollCategories(e.Delta);
            _flowCategories.Controls.Add(btnAll);

            var tipDm = new ToolTip();
            foreach (var dm in dms)
            {
                var label = dm.TenDM switch
                {
                    "Card màn hình" => "VGA",
                    "Ổ cứng SSD" => "SSD",
                    "Nguồn (PSU)" => "Nguồn",
                    "Vỏ case" => "Case",
                    "Bàn phím" => "Phím",
                    _ => dm.TenDM
                };
                var btn = Theme.GhostButton(label, 50, (_, _) => { _flowCategories.Tag = dm.MaDM; ReloadProducts(); });
                btn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                btn.Padding = new Padding(6, 0, 6, 0);
                btn.Margin = new Padding(2, 0, 2, 0);
                btn.Height = 28;
                btn.AutoSize = true;
                btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                btn.MouseWheel += (_, e) => ScrollCategories(e.Delta);
                tipDm.SetToolTip(btn, dm.TenDM);
                _flowCategories.Controls.Add(btn);
            }
            _flowCategories.Tag = 0;
            ReloadProducts();
        });
    }

    private void ReloadProducts()
    {
        var keyword = _txtSearchSP.Text;
        int maDm = _flowCategories.Tag is int id ? id : 0;
        
        Msg.Run(() =>
        {
            var sps = _spBll.Search(keyword, maDm, 0, true);
            _currentFilteredProducts = sps;
            _gridProducts.DataSource = sps;

            if (_isCardView)
            {
                _flowProducts.SuspendLayout();
                while (_flowProducts.Controls.Count > 0) _flowProducts.Controls[0].Dispose();
                _flowProducts.Controls.Clear();
                foreach (var sp in sps)
                {
                    var card = new ProductCard(sp);
                    card.ProductClicked += OnProductClicked;
                    _flowProducts.Controls.Add(card);
                }
                _flowProducts.ResumeLayout();
            }
        });
    }

    private void OnProductClicked(object? sender, SanPhamDTO sp)
    {
        if (sp.SoLuongTon <= 0) { Msg.Warn($"Sản phẩm \"{sp.TenSP}\" đã hết hàng trong kho!"); return; }
        
        var ex = _items.FirstOrDefault(x => x.MaSP == sp.MaSP);
        if (ex != null)
        {
            if (ex.SoLuong >= sp.SoLuongTon) { Msg.Warn($"Vượt quá số lượng tồn kho ({sp.SoLuongTon})!"); return; }
            ex.SoLuong++;
            _items.ResetItem(_items.IndexOf(ex));
        }
        else
        {
            _items.Add(new ChiTietHoaDonDTO { MaSP = sp.MaSP, TenSP = sp.TenSP, DonGia = sp.GiaBan, SoLuong = 1 });
        }
    }

    private void RemoveItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is ChiTietHoaDonDTO c) _items.Remove(c);
    }

    /// <summary>Thêm sản phẩm mẫu và chọn khách hàng mẫu để chụp ảnh demo POS trực quan.</summary>
    public void AddSampleItemsForDemo()
    {
        if (_items.Count == 0)
        {
            var sp1 = _currentFilteredProducts.FirstOrDefault(x => x.SoLuongTon > 0)
                   ?? new SanPhamDTO { MaSP = 1, TenSP = "Intel Core i5-12400F", GiaBan = 3390000, SoLuongTon = 10 };
            var sp2 = _currentFilteredProducts.FirstOrDefault(x => x.MaSP != sp1.MaSP && x.SoLuongTon > 0)
                   ?? new SanPhamDTO { MaSP = 2, TenSP = "Intel Core i7-13700K", GiaBan = 9990000, SoLuongTon = 10 };

            _items.Add(new ChiTietHoaDonDTO { MaSP = sp1.MaSP, TenSP = sp1.TenSP, DonGia = sp1.GiaBan, SoLuong = 1 });
            _items.Add(new ChiTietHoaDonDTO { MaSP = sp2.MaSP, TenSP = sp2.TenSP, DonGia = sp2.GiaBan, SoLuong = 1 });
            _khachHang = new KhachHangDTO { MaKH = 1, HoTen = "Nguyễn Văn An", SDT = "0987654321" };
            _txtSdtKH.Text = "0987654321";
            ShowKhach();
            UpdateTien();
        }
    }

    private void ResetKhachBadge()
    {
        _khachHang = null;
        _lblKhach.Text = "  👤 Khách bán lẻ (Chưa tích điểm)  ";
        _lblKhach.BackColor = ColorTranslator.FromHtml("#F1F5F9");
        _lblKhach.ForeColor = ColorTranslator.FromHtml("#64748B");
        _lblKhach.Cursor = Cursors.Default;
    }

    private void ThemKHNhanh()
    {
        var sdt = _txtSdtKH.Text.Trim();
        using var f = new frmKhachHangNhanh(sdt);
        if (f.ShowDialog() == DialogResult.OK && f.Result != null)
        {
            _khachHang = f.Result;
            _txtSdtKH.Text = f.Result.SDT;
            ShowKhach();
        }
    }

    private void TimKH()
    {
        var sdt = _txtSdtKH.Text.Trim();
        if (string.IsNullOrWhiteSpace(sdt))
        {
            ResetKhachBadge();
            return;
        }
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^[0-9\-\+\s]+$"))
        {
            Msg.Warn("Vui lòng nhập số điện thoại hợp lệ.");
            _txtSdtKH.SelectAll();
            _txtSdtKH.Focus();
            return;
        }

        Msg.Run(() =>
        {
            var kh = _khBll.GetByPhone(sdt);
            if (kh != null)
            {
                _khachHang = kh;
                ShowKhach();
            }
            else
            {
                if (Ask($"Không tìm thấy khách có SĐT {sdt}. Bạn có muốn thêm mới?"))
                {
                    ThemKHNhanh();
                }
            }
        });
    }

    private void ShowKhach()
    {
        _lblKhach.Text = $"  ✓ {_khachHang!.HoTen} - {_khachHang.SDT}  ✕  ";
        _lblKhach.BackColor = ColorTranslator.FromHtml("#EFF6FF");
        _lblKhach.ForeColor = ColorTranslator.FromHtml("#1D4ED8");
        _lblKhach.Cursor = Cursors.Hand;
    }

    private void UpdateTien()
    {
        var tong = _items.Sum(x => x.ThanhTien);
        _lblTongTien.Text = $"{tong:N0} ₫";

        decimal giam = 0;
        var isPercent = _cboLoaiGiamGia.SelectedIndex == 1;
        if (isPercent)
        {
            if (decimal.TryParse(_txtGiamGia.Text.Trim(), out var pct))
            {
                pct = Math.Clamp(pct, 0, 100);
                giam = Math.Round(tong * pct / 100m);
                _lblGiamGiaPreview.Text = $"- {giam:N0} ₫";
            }
            else
            {
                _lblGiamGiaPreview.Text = "- 0 ₫";
            }
        }
        else
        {
            if (decimal.TryParse(_txtGiamGia.Text.Replace(".", "").Replace(",", ""), out var v))
            {
                giam = Math.Min(tong, Math.Max(0, v));
                _lblGiamGiaPreview.Text = "";
            }
        }

        var phaiThu = Math.Max(0, tong - giam);
        _lblThanhToan.Text = $"{phaiThu:N0} ₫";
        if (phaiThu > 0)
        {
            _pnlTotalHero.FillColor = ColorTranslator.FromHtml("#F0FDF4");
            _pnlTotalHero.BorderColor = ColorTranslator.FromHtml("#A7F3D0");
            _lblTotalTitle.ForeColor = ColorTranslator.FromHtml("#065F46");
            _lblThanhToan.ForeColor = ColorTranslator.FromHtml("#047857");
        }
        else
        {
            _pnlTotalHero.FillColor = ColorTranslator.FromHtml("#F8FAFC");
            _pnlTotalHero.BorderColor = ColorTranslator.FromHtml("#E2E8F0");
            _lblTotalTitle.ForeColor = ColorTranslator.FromHtml("#64748B");
            _lblThanhToan.ForeColor = ColorTranslator.FromHtml("#94A3B8");
        }
        _pnlTotalHero.Invalidate();

        _lblCartHeader.Text = $"🛒 GIỎ HÀNG ({_items.Sum(x => x.SoLuong)} món)";
    }

    private void TreoDon()
    {
        if (_items.Count == 0 && _heldBills.Count == 0)
        {
            Msg.Warn("Giỏ hàng đang trống, không có đơn hàng để treo!");
            return;
        }

        if (_heldBills.Count > 0 && _items.Count == 0)
        {
            ShowHeldBillsMenu();
            return;
        }

        if (_items.Count > 0)
        {
            if (_heldBills.Count >= 5)
            {
                Msg.Warn("Đã đạt giới hạn tối đa 5 đơn hàng treo. Vui lòng thanh toán hoặc xử lý bớt đơn cũ.");
                return;
            }

            var bill = new HeldBill(
                _khachHang,
                _items.ToList(),
                _txtGiamGia.Text,
                _cboLoaiGiamGia.SelectedIndex,
                _cboHTTT.SelectedItem?.ToString() ?? HinhThucThanhToan.TienMat,
                _txtGhiChu.Text,
                DateTime.Now
            );
            _heldBills.Add(bill);

            _items.Clear();
            _txtSdtKH.Clear();
            ResetKhachBadge();
            _txtGiamGia.Text = "0";
            _cboLoaiGiamGia.SelectedIndex = 0;
            _txtGhiChu.Clear();
            UpdateTien();
            UpdateHoldButtonText();

            Msg.Info($"Đã treo đơn hàng #{_heldBills.Count} ({bill.CreatedAt:HH:mm}). Bạn có thể tiếp tục phục vụ khách khác!");
        }
    }

    private void UpdateHoldButtonText()
    {
        _btnHoldOrder.Text = _heldBills.Count > 0 ? $"⏱ Đơn treo ({_heldBills.Count})" : "⏱ Treo HĐ (F10)";
    }

    private void ShowHeldBillsMenu()
    {
        if (_heldBills.Count == 0)
        {
            Msg.Info("Hiện không có đơn hàng nào đang treo.");
            return;
        }

        var menu = new ContextMenuStrip();
        if (_items.Count > 0)
        {
            var mnuAdd = new ToolStripMenuItem("➕ Treo tiếp đơn hiện tại (F10)", null, (_, _) => TreoDon());
            menu.Items.Add(mnuAdd);
            menu.Items.Add(new ToolStripSeparator());
        }

        for (int i = 0; i < _heldBills.Count; i++)
        {
            var idx = i;
            var bill = _heldBills[i];
            var khText = bill.Khach != null ? bill.Khach.HoTen : "Khách vãng lai";
            var tongTien = bill.Items.Sum(x => x.ThanhTien);
            var itemText = $"Mở đơn #{idx + 1}: {khText} ({bill.Items.Sum(x => x.SoLuong)} món - {tongTien:N0}₫) lúc {bill.CreatedAt:HH:mm}";

            var mnuItem = new ToolStripMenuItem(itemText, null, (_, _) => RestoreHeldBill(idx));
            menu.Items.Add(mnuItem);
        }

        menu.Show(_btnHoldOrder, new Point(0, -menu.PreferredSize.Height));
    }

    private void RestoreHeldBill(int index)
    {
        if (index < 0 || index >= _heldBills.Count) return;

        if (_items.Count > 0)
        {
            if (!Msg.Confirm("Giỏ hàng hiện tại đang có sản phẩm. Bạn có muốn phục hồi đơn treo này (đơn hiện tại chưa lưu sẽ bị xóa)?"))
                return;
        }

        var bill = _heldBills[index];
        _heldBills.RemoveAt(index);
        UpdateHoldButtonText();

        _items.Clear();
        foreach (var it in bill.Items) _items.Add(it);

        _khachHang = bill.Khach;
        if (_khachHang != null)
        {
            _txtSdtKH.Text = _khachHang.SDT;
            ShowKhach();
        }
        else
        {
            _txtSdtKH.Clear();
            ResetKhachBadge();
        }

        _cboLoaiGiamGia.SelectedIndex = bill.LoaiGiamGia;
        _txtGiamGia.Text = bill.GiamGiaText;
        _cboHTTT.SelectedItem = bill.HTTT;
        _txtGhiChu.Text = bill.GhiChu;

        UpdateTien();
        Msg.Info($"Đã phục hồi đơn #{index + 1} vào giỏ hàng!");
    }

    private void ThanhToan()
    {
        if (_items.Count == 0) { Msg.Warn("Giỏ hàng đang trống. Hãy chọn sản phẩm trước khi thanh toán."); return; }
        var tong = _items.Sum(x => x.ThanhTien);
        decimal giam = 0;
        if (_cboLoaiGiamGia.SelectedIndex == 1)
        {
            if (decimal.TryParse(_txtGiamGia.Text.Trim(), out var pct))
                giam = Math.Round(tong * Math.Clamp(pct, 0, 100) / 100m);
        }
        else
        {
            if (decimal.TryParse(_txtGiamGia.Text.Replace(".", "").Replace(",", ""), out var v))
                giam = Math.Min(tong, Math.Max(0, v));
        }
        var phaiThu = Math.Max(0, tong - giam);

        // Chọn serial cho các sản phẩm trong giỏ
        var hd = new HoaDonDTO
        {
            MaKH = _khachHang?.MaKH,
            TenKH = _khachHang?.HoTen,
            SDTKH = _khachHang?.SDT,
            MaNV = Session.CurrentUser!.MaNV,
            TenNV = Session.CurrentUser?.HoTen ?? "",
            TongTien = tong,
            GiamGia = giam,
            HinhThucTT = _cboHTTT.SelectedItem?.ToString() ?? HinhThucThanhToan.TienMat,
            GhiChu = _txtGhiChu.Text
        };

        foreach (var ct in _items)
        {
            var sp = new SanPhamDTO { MaSP = ct.MaSP, TenSP = ct.TenSP ?? "" };
            using var f = new frmChonSerial(sp, ct.SoLuong);
            if (f.ShowDialog() != DialogResult.OK) return;
            ct.Serials = f.ResultSerials;
        }

        var idHD = 0;
        if (Msg.Run(() =>
        {
            idHD = _bll.LapHoaDon(hd, _items.ToList());
            hd.MaHD = idHD;
            hd.NgayLap = DateTime.Now;
        }))
        {
            Msg.Info($"Thanh toán thành công hóa đơn #{idHD}!");
            if (Ask("In hóa đơn ra file PDF?"))
            {
                using var dlg = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"HoaDon_{idHD}.pdf" };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ComputerStore.GUI.Services.PdfExporter.ExportHoaDon(hd, _items.ToList(), dlg.FileName);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
            }
            _items.Clear();
            _txtSdtKH.Clear();
            _txtGiamGia.Text = "0";
            _cboLoaiGiamGia.SelectedIndex = 0;
            _txtGhiChu.Clear();
            ResetKhachBadge();
            UpdateTien();
            ReloadProducts(); // Reload kho
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F2)
        {
            _txtSearchSP.Focus();
            _txtSearchSP.SelectAll();
            return true;
        }
        if (keyData == Keys.F4)
        {
            _txtSdtKH.Focus();
            _txtSdtKH.SelectAll();
            return true;
        }
        if (keyData == Keys.F9)
        {
            ThanhToan();
            return true;
        }
        if (keyData == Keys.F10)
        {
            if (_heldBills.Count > 0 && _items.Count == 0)
                ShowHeldBillsMenu();
            else
                TreoDon();
            return true;
        }
        if (keyData == Keys.Escape)
        {
            if (_items.Count > 0 && Msg.Confirm("Bạn có muốn xóa sạch giỏ hàng?"))
            {
                _items.Clear();
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}

