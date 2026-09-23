namespace FredPull
{
    partial class HouseDetailForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle9 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle10 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle11 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle12 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle13 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle14 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle15 = new DataGridViewCellStyle();
            _header = new TableLayoutPanel();
            _lblCounty = new Label();
            _txtCard = new RichTextBox();
            _buttons = new FlowLayoutPanel();
            _btnOpenRedfin = new Button();
            _btnOpenMap = new Button();
            _btnChoose = new Button();
            _btnCopyCard = new Button();
            _btnCopyAddress = new Button();
            _btnCopyCoords = new Button();
            _btnPhotos = new Button();
            _btnShowFolder = new Button();
            _btnKml = new Button();
            _btnAnim = new Button();
            _split = new SplitContainer();
            _gridCands = new DataGridView();
            _cSelected = new DataGridViewTextBoxColumn();
            _cStreet = new DataGridViewTextBoxColumn();
            _cCity = new DataGridViewTextBoxColumn();
            _cPrice = new DataGridViewTextBoxColumn();
            _cOriginal = new DataGridViewTextBoxColumn();
            _cCurrent = new DataGridViewTextBoxColumn();
            _cCuts = new DataGridViewTextBoxColumn();
            _cTotalCut = new DataGridViewTextBoxColumn();
            _cDays = new DataGridViewTextBoxColumn();
            _cYear = new DataGridViewTextBoxColumn();
            _cM2 = new DataGridViewTextBoxColumn();
            _cBeds = new DataGridViewTextBoxColumn();
            _cSaleDate = new DataGridViewTextBoxColumn();
            _cSalePrice = new DataGridViewTextBoxColumn();
            _cWithdrawn = new DataGridViewTextBoxColumn();
            _cLat = new DataGridViewTextBoxColumn();
            _cLng = new DataGridViewTextBoxColumn();
            _splitRight = new SplitContainer();
            _tabs = new TabControl();
            _tabHistory = new TabPage();
            _gridHistory = new DataGridView();
            _hDate = new DataGridViewTextBoxColumn();
            _hEvent = new DataGridViewTextBoxColumn();
            _hPrice = new DataGridViewTextBoxColumn();
            _hDiff = new DataGridViewTextBoxColumn();
            _tabChart = new TabPage();
            _plot = new ScottPlot.WinForms.FormsPlot();
            _photoLayout = new TableLayoutPanel();
            _lblPhotos = new Label();
            _thumbs = new FlowLayoutPanel();
            _preview = new PictureBox();
            _status = new Label();
            _header.SuspendLayout();
            _buttons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
            _split.Panel1.SuspendLayout();
            _split.Panel2.SuspendLayout();
            _split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_gridCands).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_splitRight).BeginInit();
            _splitRight.Panel1.SuspendLayout();
            _splitRight.Panel2.SuspendLayout();
            _splitRight.SuspendLayout();
            _tabs.SuspendLayout();
            _tabHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_gridHistory).BeginInit();
            _tabChart.SuspendLayout();
            _photoLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
            SuspendLayout();
            //
            // _header
            //
            _header.ColumnCount = 1;
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _header.Controls.Add(_lblCounty, 0, 0);
            _header.Controls.Add(_txtCard, 0, 1);
            _header.Dock = DockStyle.Top;
            _header.Location = new Point(0, 0);
            _header.Name = "_header";
            _header.Padding = new Padding(6, 4, 6, 0);
            _header.RowCount = 2;
            _header.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            _header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _header.Size = new Size(1384, 92);
            _header.TabIndex = 0;
            //
            // _lblCounty
            //
            _lblCounty.AutoSize = true;
            _lblCounty.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblCounty.Location = new Point(9, 4);
            _lblCounty.Name = "_lblCounty";
            _lblCounty.Size = new Size(58, 20);
            _lblCounty.TabIndex = 0;
            _lblCounty.Text = "İlçe";
            //
            // _txtCard
            //
            _txtCard.BackColor = SystemColors.Window;
            _txtCard.Dock = DockStyle.Fill;
            _txtCard.Font = new Font("Segoe UI", 10.5F);
            _txtCard.Location = new Point(9, 33);
            _txtCard.Name = "_txtCard";
            _txtCard.ReadOnly = true;
            _txtCard.Size = new Size(1366, 56);
            _txtCard.TabIndex = 1;
            _txtCard.Text = "";
            //
            // _buttons
            //
            _buttons.AutoSize = true;
            _buttons.Controls.Add(_btnOpenRedfin);
            _buttons.Controls.Add(_btnOpenMap);
            _buttons.Controls.Add(_btnChoose);
            _buttons.Controls.Add(_btnCopyCard);
            _buttons.Controls.Add(_btnCopyAddress);
            _buttons.Controls.Add(_btnCopyCoords);
            _buttons.Controls.Add(_btnPhotos);
            _buttons.Controls.Add(_btnShowFolder);
            _buttons.Controls.Add(_btnKml);
            _buttons.Controls.Add(_btnAnim);
            _buttons.Dock = DockStyle.Top;
            _buttons.Location = new Point(0, 92);
            _buttons.Name = "_buttons";
            _buttons.Padding = new Padding(6, 2, 6, 2);
            _buttons.Size = new Size(1384, 35);
            _buttons.TabIndex = 1;
            //
            // _btnOpenRedfin
            //
            _btnOpenRedfin.AutoSize = true;
            _btnOpenRedfin.Location = new Point(9, 5);
            _btnOpenRedfin.Name = "_btnOpenRedfin";
            _btnOpenRedfin.Size = new Size(95, 25);
            _btnOpenRedfin.TabIndex = 0;
            _btnOpenRedfin.Text = "Redfin'de aç";
            _btnOpenRedfin.UseVisualStyleBackColor = true;
            _btnOpenRedfin.Click += BtnOpenRedfin_Click;
            //
            // _btnOpenMap
            //
            _btnOpenMap.AutoSize = true;
            _btnOpenMap.Location = new Point(110, 5);
            _btnOpenMap.Name = "_btnOpenMap";
            _btnOpenMap.Size = new Size(95, 25);
            _btnOpenMap.TabIndex = 1;
            _btnOpenMap.Text = "Haritada aç";
            _btnOpenMap.UseVisualStyleBackColor = true;
            _btnOpenMap.Click += BtnOpenMap_Click;
            //
            // _btnChoose
            //
            _btnChoose.AutoSize = true;
            _btnChoose.Location = new Point(211, 5);
            _btnChoose.Name = "_btnChoose";
            _btnChoose.Size = new Size(85, 25);
            _btnChoose.TabIndex = 2;
            _btnChoose.Text = "Bu evi seç";
            _btnChoose.UseVisualStyleBackColor = true;
            _btnChoose.Click += BtnChoose_Click;
            //
            // _btnCopyCard
            //
            _btnCopyCard.AutoSize = true;
            _btnCopyCard.Location = new Point(302, 5);
            _btnCopyCard.Name = "_btnCopyCard";
            _btnCopyCard.Size = new Size(95, 25);
            _btnCopyCard.TabIndex = 3;
            _btnCopyCard.Text = "Kartı kopyala";
            _btnCopyCard.UseVisualStyleBackColor = true;
            _btnCopyCard.Click += BtnCopyCard_Click;
            //
            // _btnCopyAddress
            //
            _btnCopyAddress.AutoSize = true;
            _btnCopyAddress.Location = new Point(403, 5);
            _btnCopyAddress.Name = "_btnCopyAddress";
            _btnCopyAddress.Size = new Size(100, 25);
            _btnCopyAddress.TabIndex = 4;
            _btnCopyAddress.Text = "Adresi kopyala";
            _btnCopyAddress.UseVisualStyleBackColor = true;
            _btnCopyAddress.Click += BtnCopyAddress_Click;
            //
            // _btnCopyCoords
            //
            _btnCopyCoords.AutoSize = true;
            _btnCopyCoords.Location = new Point(509, 5);
            _btnCopyCoords.Name = "_btnCopyCoords";
            _btnCopyCoords.Size = new Size(120, 25);
            _btnCopyCoords.TabIndex = 5;
            _btnCopyCoords.Text = "Koordinatı kopyala";
            _btnCopyCoords.UseVisualStyleBackColor = true;
            _btnCopyCoords.Click += BtnCopyCoords_Click;
            //
            // _btnPhotos
            //
            _btnPhotos.AutoSize = true;
            _btnPhotos.Location = new Point(655, 5);
            _btnPhotos.Margin = new Padding(20, 3, 3, 3);
            _btnPhotos.Name = "_btnPhotos";
            _btnPhotos.Size = new Size(180, 25);
            _btnPhotos.TabIndex = 6;
            _btnPhotos.Text = "Fotoğrafları indir (referans)";
            _btnPhotos.UseVisualStyleBackColor = true;
            _btnPhotos.Click += BtnPhotos_Click;
            //
            // _btnShowFolder
            //
            _btnShowFolder.AutoSize = true;
            _btnShowFolder.Location = new Point(841, 5);
            _btnShowFolder.Name = "_btnShowFolder";
            _btnShowFolder.Size = new Size(110, 25);
            _btnShowFolder.TabIndex = 7;
            _btnShowFolder.Text = "Klasörde göster";
            _btnShowFolder.UseVisualStyleBackColor = true;
            _btnShowFolder.Click += BtnShowFolder_Click;
            //
            // _btnKml
            //
            _btnKml.AutoSize = true;
            _btnKml.Location = new Point(977, 5);
            _btnKml.Margin = new Padding(20, 3, 3, 3);
            _btnKml.Name = "_btnKml";
            _btnKml.Size = new Size(110, 25);
            _btnKml.TabIndex = 8;
            _btnKml.Text = "KML dışa aktar";
            _btnKml.UseVisualStyleBackColor = true;
            _btnKml.Click += BtnKml_Click;
            //
            // _btnAnim
            //
            _btnAnim.AutoSize = true;
            _btnAnim.Location = new Point(1093, 5);
            _btnAnim.Name = "_btnAnim";
            _btnAnim.Size = new Size(110, 25);
            _btnAnim.TabIndex = 9;
            _btnAnim.Text = "Animasyon CSV";
            _btnAnim.UseVisualStyleBackColor = true;
            _btnAnim.Click += BtnAnim_Click;
            //
            // _split
            //
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 127);
            _split.Name = "_split";
            //
            // _split.Panel1
            //
            _split.Panel1.Controls.Add(_gridCands);
            //
            // _split.Panel2
            //
            _split.Panel2.Controls.Add(_splitRight);
            _split.Size = new Size(1384, 710);
            _split.SplitterDistance = 780;
            _split.TabIndex = 2;
            //
            // _gridCands
            //
            _gridCands.AllowUserToAddRows = false;
            _gridCands.AllowUserToDeleteRows = false;
            _gridCands.AllowUserToResizeRows = false;
            _gridCands.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            _gridCands.BackgroundColor = SystemColors.Window;
            _gridCands.BorderStyle = BorderStyle.None;
            _gridCands.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridCands.Columns.AddRange(new DataGridViewColumn[] { _cSelected, _cStreet, _cCity, _cPrice, _cOriginal, _cCurrent, _cCuts, _cTotalCut, _cDays, _cYear, _cM2, _cBeds, _cSaleDate, _cSalePrice, _cWithdrawn, _cLat, _cLng });
            _gridCands.Dock = DockStyle.Fill;
            _gridCands.Location = new Point(0, 0);
            _gridCands.MultiSelect = false;
            _gridCands.Name = "_gridCands";
            _gridCands.ReadOnly = true;
            _gridCands.RowHeadersVisible = false;
            _gridCands.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridCands.Size = new Size(780, 710);
            _gridCands.TabIndex = 0;
            _gridCands.CurrentCellChanged += GridCands_CurrentCellChanged;
            //
            // _cSelected
            //
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _cSelected.DefaultCellStyle = dataGridViewCellStyle1;
            _cSelected.HeaderText = "Seçildi";
            _cSelected.Name = "_cSelected";
            _cSelected.ReadOnly = true;
            _cSelected.ToolTipText = "Kart metnine giren ev. Değiştirmek için satırı seç, \"Bu evi seç\".";
            //
            // _cStreet
            //
            _cStreet.HeaderText = "Sokak";
            _cStreet.Name = "_cStreet";
            _cStreet.ReadOnly = true;
            //
            // _cCity
            //
            _cCity.HeaderText = "Şehir";
            _cCity.Name = "_cCity";
            _cCity.ReadOnly = true;
            //
            // _cPrice
            //
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.Format = "N0";
            _cPrice.DefaultCellStyle = dataGridViewCellStyle2;
            _cPrice.HeaderText = "Fiyat";
            _cPrice.Name = "_cPrice";
            _cPrice.ReadOnly = true;
            _cPrice.ToolTipText = "Liste sayfasındaki güncel fiyat ($).";
            //
            // _cOriginal
            //
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Format = "N0";
            _cOriginal.DefaultCellStyle = dataGridViewCellStyle3;
            _cOriginal.HeaderText = "İlk fiyat";
            _cOriginal.Name = "_cOriginal";
            _cOriginal.ReadOnly = true;
            _cOriginal.ToolTipText = "Mevcut ilanın ilk istenen fiyatı ($).";
            //
            // _cCurrent
            //
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle4.Format = "N0";
            _cCurrent.DefaultCellStyle = dataGridViewCellStyle4;
            _cCurrent.HeaderText = "Şimdiki";
            _cCurrent.Name = "_cCurrent";
            _cCurrent.ReadOnly = true;
            _cCurrent.ToolTipText = "Fiyat geçmişine göre şimdiki fiyat ($).";
            //
            // _cCuts
            //
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleRight;
            _cCuts.DefaultCellStyle = dataGridViewCellStyle5;
            _cCuts.HeaderText = "İndirim sayısı";
            _cCuts.Name = "_cCuts";
            _cCuts.ReadOnly = true;
            _cCuts.ToolTipText = "Bir öncekinden en az 1.000 $ düşük fiyat değişiklikleri.";
            //
            // _cTotalCut
            //
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle6.Format = "N0";
            _cTotalCut.DefaultCellStyle = dataGridViewCellStyle6;
            _cTotalCut.HeaderText = "Toplam indirim";
            _cTotalCut.Name = "_cTotalCut";
            _cTotalCut.ReadOnly = true;
            //
            // _cDays
            //
            dataGridViewCellStyle7.Alignment = DataGridViewContentAlignment.MiddleRight;
            _cDays.DefaultCellStyle = dataGridViewCellStyle7;
            _cDays.HeaderText = "Gün";
            _cDays.Name = "_cDays";
            _cDays.ReadOnly = true;
            _cDays.ToolTipText = "İlk ilan tarihinden bugüne gün.";
            //
            // _cYear
            //
            dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleRight;
            _cYear.DefaultCellStyle = dataGridViewCellStyle8;
            _cYear.HeaderText = "Yapım";
            _cYear.Name = "_cYear";
            _cYear.ReadOnly = true;
            //
            // _cM2
            //
            dataGridViewCellStyle9.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle9.Format = "N0";
            _cM2.DefaultCellStyle = dataGridViewCellStyle9;
            _cM2.HeaderText = "m²";
            _cM2.Name = "_cM2";
            _cM2.ReadOnly = true;
            //
            // _cBeds
            //
            dataGridViewCellStyle10.Alignment = DataGridViewContentAlignment.MiddleRight;
            _cBeds.DefaultCellStyle = dataGridViewCellStyle10;
            _cBeds.HeaderText = "Oda";
            _cBeds.Name = "_cBeds";
            _cBeds.ReadOnly = true;
            //
            // _cSaleDate
            //
            _cSaleDate.HeaderText = "Son satış tarihi";
            _cSaleDate.Name = "_cSaleDate";
            _cSaleDate.ReadOnly = true;
            //
            // _cSalePrice
            //
            dataGridViewCellStyle11.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle11.Format = "N0";
            _cSalePrice.DefaultCellStyle = dataGridViewCellStyle11;
            _cSalePrice.HeaderText = "Son satış fiyatı";
            _cSalePrice.Name = "_cSalePrice";
            _cSalePrice.ReadOnly = true;
            //
            // _cWithdrawn
            //
            _cWithdrawn.HeaderText = "Çekilmiş mi";
            _cWithdrawn.Name = "_cWithdrawn";
            _cWithdrawn.ReadOnly = true;
            _cWithdrawn.ToolTipText = "Son satıştan bu yana ilandan kaldırılıp yeniden çıkmış mı.";
            //
            // _cLat
            //
            dataGridViewCellStyle12.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle12.Format = "0.00000";
            _cLat.DefaultCellStyle = dataGridViewCellStyle12;
            _cLat.HeaderText = "Enlem";
            _cLat.Name = "_cLat";
            _cLat.ReadOnly = true;
            //
            // _cLng
            //
            dataGridViewCellStyle13.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle13.Format = "0.00000";
            _cLng.DefaultCellStyle = dataGridViewCellStyle13;
            _cLng.HeaderText = "Boylam";
            _cLng.Name = "_cLng";
            _cLng.ReadOnly = true;
            //
            // _splitRight
            //
            _splitRight.Dock = DockStyle.Fill;
            _splitRight.Location = new Point(0, 0);
            _splitRight.Name = "_splitRight";
            _splitRight.Orientation = Orientation.Horizontal;
            //
            // _splitRight.Panel1
            //
            _splitRight.Panel1.Controls.Add(_tabs);
            //
            // _splitRight.Panel2
            //
            _splitRight.Panel2.Controls.Add(_photoLayout);
            _splitRight.Size = new Size(600, 710);
            _splitRight.SplitterDistance = 300;
            _splitRight.TabIndex = 0;
            //
            // _tabs
            //
            _tabs.Controls.Add(_tabHistory);
            _tabs.Controls.Add(_tabChart);
            _tabs.Dock = DockStyle.Fill;
            _tabs.Location = new Point(0, 0);
            _tabs.Name = "_tabs";
            _tabs.SelectedIndex = 0;
            _tabs.Size = new Size(600, 300);
            _tabs.TabIndex = 0;
            //
            // _tabHistory
            //
            _tabHistory.Controls.Add(_gridHistory);
            _tabHistory.Location = new Point(4, 24);
            _tabHistory.Name = "_tabHistory";
            _tabHistory.Size = new Size(592, 272);
            _tabHistory.TabIndex = 0;
            _tabHistory.Text = "Fiyat geçmişi";
            _tabHistory.UseVisualStyleBackColor = true;
            //
            // _gridHistory
            //
            _gridHistory.AllowUserToAddRows = false;
            _gridHistory.AllowUserToDeleteRows = false;
            _gridHistory.AllowUserToResizeRows = false;
            _gridHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            _gridHistory.BackgroundColor = SystemColors.Window;
            _gridHistory.BorderStyle = BorderStyle.None;
            _gridHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridHistory.Columns.AddRange(new DataGridViewColumn[] { _hDate, _hEvent, _hPrice, _hDiff });
            _gridHistory.Dock = DockStyle.Fill;
            _gridHistory.Location = new Point(0, 0);
            _gridHistory.Name = "_gridHistory";
            _gridHistory.ReadOnly = true;
            _gridHistory.RowHeadersVisible = false;
            _gridHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridHistory.Size = new Size(592, 272);
            _gridHistory.TabIndex = 0;
            //
            // _hDate
            //
            _hDate.HeaderText = "Tarih";
            _hDate.Name = "_hDate";
            _hDate.ReadOnly = true;
            //
            // _hEvent
            //
            _hEvent.HeaderText = "Olay";
            _hEvent.Name = "_hEvent";
            _hEvent.ReadOnly = true;
            _hEvent.ToolTipText = "Kalın: mevcut ilan. Gri: kira kaydı (hesaba katılmaz).";
            //
            // _hPrice
            //
            dataGridViewCellStyle14.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle14.Format = "N0";
            _hPrice.DefaultCellStyle = dataGridViewCellStyle14;
            _hPrice.HeaderText = "Fiyat";
            _hPrice.Name = "_hPrice";
            _hPrice.ReadOnly = true;
            //
            // _hDiff
            //
            dataGridViewCellStyle15.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle15.Format = "+#,##0;-#,##0;0";
            _hDiff.DefaultCellStyle = dataGridViewCellStyle15;
            _hDiff.HeaderText = "Fark";
            _hDiff.Name = "_hDiff";
            _hDiff.ReadOnly = true;
            _hDiff.ToolTipText = "Bir önceki istenen fiyata göre fark ($).";
            //
            // _tabChart
            //
            _tabChart.Controls.Add(_plot);
            _tabChart.Location = new Point(4, 24);
            _tabChart.Name = "_tabChart";
            _tabChart.Size = new Size(592, 272);
            _tabChart.TabIndex = 1;
            _tabChart.Text = "Grafik";
            _tabChart.UseVisualStyleBackColor = true;
            //
            // _plot
            //
            _plot.Dock = DockStyle.Fill;
            _plot.Location = new Point(0, 0);
            _plot.Name = "_plot";
            _plot.Size = new Size(592, 272);
            _plot.TabIndex = 0;
            //
            // _photoLayout
            //
            _photoLayout.ColumnCount = 1;
            _photoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _photoLayout.Controls.Add(_lblPhotos, 0, 0);
            _photoLayout.Controls.Add(_thumbs, 0, 1);
            _photoLayout.Controls.Add(_preview, 0, 2);
            _photoLayout.Dock = DockStyle.Fill;
            _photoLayout.Location = new Point(0, 0);
            _photoLayout.Name = "_photoLayout";
            _photoLayout.RowCount = 3;
            _photoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            _photoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 158F));
            _photoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _photoLayout.Size = new Size(600, 406);
            _photoLayout.TabIndex = 0;
            //
            // _lblPhotos
            //
            _lblPhotos.AutoSize = true;
            _lblPhotos.ForeColor = SystemColors.GrayText;
            _lblPhotos.Location = new Point(3, 4);
            _lblPhotos.Margin = new Padding(3, 4, 3, 0);
            _lblPhotos.Name = "_lblPhotos";
            _lblPhotos.Size = new Size(120, 15);
            _lblPhotos.TabIndex = 0;
            _lblPhotos.Text = "Referans fotoğraflar";
            //
            // _thumbs
            //
            _thumbs.AutoScroll = true;
            _thumbs.Dock = DockStyle.Fill;
            _thumbs.Location = new Point(3, 27);
            _thumbs.Name = "_thumbs";
            _thumbs.Size = new Size(594, 142);
            _thumbs.TabIndex = 1;
            _thumbs.WrapContents = false;
            //
            // _preview
            //
            _preview.BackColor = SystemColors.ControlDark;
            _preview.Dock = DockStyle.Fill;
            _preview.Location = new Point(3, 175);
            _preview.Name = "_preview";
            _preview.Size = new Size(594, 228);
            _preview.SizeMode = PictureBoxSizeMode.Zoom;
            _preview.TabIndex = 2;
            _preview.TabStop = false;
            //
            // _status
            //
            _status.AutoEllipsis = true;
            _status.Dock = DockStyle.Bottom;
            _status.Location = new Point(0, 837);
            _status.Name = "_status";
            _status.Padding = new Padding(6, 0, 0, 0);
            _status.Size = new Size(1384, 24);
            _status.TabIndex = 3;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            //
            // HouseDetailForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1384, 861);
            Controls.Add(_split);
            Controls.Add(_buttons);
            Controls.Add(_header);
            Controls.Add(_status);
            Font = new Font("Segoe UI", 9F);
            Name = "HouseDetailForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Ev detayları";
            FormClosed += HouseDetailForm_FormClosed;
            _header.ResumeLayout(false);
            _header.PerformLayout();
            _buttons.ResumeLayout(false);
            _buttons.PerformLayout();
            _split.Panel1.ResumeLayout(false);
            _split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_split).EndInit();
            _split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_gridCands).EndInit();
            _splitRight.Panel1.ResumeLayout(false);
            _splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitRight).EndInit();
            _splitRight.ResumeLayout(false);
            _tabs.ResumeLayout(false);
            _tabHistory.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_gridHistory).EndInit();
            _tabChart.ResumeLayout(false);
            _photoLayout.ResumeLayout(false);
            _photoLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // Üst: ilçe ve kart metni, düğmeler
        private TableLayoutPanel _header;
        private Label _lblCounty;
        private RichTextBox _txtCard;
        private FlowLayoutPanel _buttons;
        private Button _btnOpenRedfin;
        private Button _btnOpenMap;
        private Button _btnChoose;
        private Button _btnCopyCard;
        private Button _btnCopyAddress;
        private Button _btnCopyCoords;
        private Button _btnPhotos;
        private Button _btnShowFolder;
        private Button _btnKml;
        private Button _btnAnim;

        // Sol: adaylar
        private SplitContainer _split;
        private DataGridView _gridCands;
        private DataGridViewTextBoxColumn _cSelected;
        private DataGridViewTextBoxColumn _cStreet;
        private DataGridViewTextBoxColumn _cCity;
        private DataGridViewTextBoxColumn _cPrice;
        private DataGridViewTextBoxColumn _cOriginal;
        private DataGridViewTextBoxColumn _cCurrent;
        private DataGridViewTextBoxColumn _cCuts;
        private DataGridViewTextBoxColumn _cTotalCut;
        private DataGridViewTextBoxColumn _cDays;
        private DataGridViewTextBoxColumn _cYear;
        private DataGridViewTextBoxColumn _cM2;
        private DataGridViewTextBoxColumn _cBeds;
        private DataGridViewTextBoxColumn _cSaleDate;
        private DataGridViewTextBoxColumn _cSalePrice;
        private DataGridViewTextBoxColumn _cWithdrawn;
        private DataGridViewTextBoxColumn _cLat;
        private DataGridViewTextBoxColumn _cLng;

        // Sağ üst: fiyat geçmişi ve grafik
        private SplitContainer _splitRight;
        private TabControl _tabs;
        private TabPage _tabHistory;
        private DataGridView _gridHistory;
        private DataGridViewTextBoxColumn _hDate;
        private DataGridViewTextBoxColumn _hEvent;
        private DataGridViewTextBoxColumn _hPrice;
        private DataGridViewTextBoxColumn _hDiff;
        private TabPage _tabChart;
        private ScottPlot.WinForms.FormsPlot _plot;

        // Sağ alt: fotoğraflar
        private TableLayoutPanel _photoLayout;
        private Label _lblPhotos;
        private FlowLayoutPanel _thumbs;
        private PictureBox _preview;

        // Alt: durum
        private Label _status;
    }
}
