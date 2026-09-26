namespace FredPull
{
    partial class MainForm
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
            _top = new FlowLayoutPanel();
            _lblState = new Label();
            _cbState = new ComboBox();
            _lblCounties = new Label();
            _lblKey = new Label();
            _txtKey = new TextBox();
            _btnFetch = new Button();
            _btnCancel = new Button();
            _btnLoad = new Button();
            _btnOpenOut = new Button();
            _btnRedfin = new Button();
            _lblRedfin = new Label();
            _btnAttachRedfin = new Button();
            _lblHouse = new Label();
            _cbHouseMode = new ComboBox();
            _lblBand = new Label();
            _txtBand = new TextBox();
            _chkCdp = new CheckBox();
            _btnHouseCards = new Button();
            _btnHouseDetail = new Button();
            _lblStudio = new Label();
            _txtStudio = new TextBox();
            _btnStudioBrowse = new Button();
            _btnStudioProject = new Button();
            _btnStudioRender = new Button();
            _mainTabs = new TabControl();
            _tabCounties = new TabPage();
            _tabVideo = new TabPage();
            _videoLayout = new TableLayoutPanel();
            _grpAnim = new GroupBox();
            _animLayout = new TableLayoutPanel();
            _animTabs = new TabControl();
            _tabAnimTexts = new TabPage();
            _tabAnimCharts = new TabPage();
            _textsLayout = new TableLayoutPanel();
            _chartsLayout = new TableLayoutPanel();
            _chartsRow = new FlowLayoutPanel();
            _lblCharts = new Label();
            _btnChartsPick = new Button();
            _chartAddRow = new FlowLayoutPanel();
            _lblChartCounty = new Label();
            _cbChartCounty = new ComboBox();
            _lblChartRecipe = new Label();
            _cbChartRecipe = new ComboBox();
            _btnChartAdd = new Button();
            _chartMetricsRow = new FlowLayoutPanel();
            _lblQuiz = new Label();
            _cbQuiz1 = new ComboBox();
            _cbQuiz2 = new ComboBox();
            _cbQuiz3 = new ComboBox();
            _chartCardRow = new FlowLayoutPanel();
            _lblCardIcon = new Label();
            _cbCardIcon = new ComboBox();
            _lblCardValue = new Label();
            _txtCardValue = new TextBox();
            _lblCardCaption = new Label();
            _txtCardCaption = new TextBox();
            _gridCharts = new DataGridView();
            _gSeq = new DataGridViewTextBoxColumn();
            _gSlot = new DataGridViewTextBoxColumn();
            _gCounty = new DataGridViewTextBoxColumn();
            _gChart = new DataGridViewTextBoxColumn();
            _gMetrics = new DataGridViewTextBoxColumn();
            _gText = new DataGridViewTextBoxColumn();
            _gCaption = new DataGridViewTextBoxColumn();
            _chartBottomRow = new FlowLayoutPanel();
            _btnChartDelete = new Button();
            _chkTransparent = new CheckBox();
            _animStudioRow = new FlowLayoutPanel();
            _animTextsRow = new FlowLayoutPanel();
            _lblTexts = new Label();
            _btnTextsPick = new Button();
            _gridTexts = new DataGridView();
            _tOrder = new DataGridViewTextBoxColumn();
            _tCounty = new DataGridViewTextBoxColumn();
            _tCities = new DataGridViewTextBoxColumn();
            _tStat = new DataGridViewTextBoxColumn();
            _animButtons = new FlowLayoutPanel();
            _btnStudioOpenOut = new Button();
            _lblStudioUnsupported = new Label();
            _grpIntro = new GroupBox();
            _introLayout = new TableLayoutPanel();
            _introFields = new TableLayoutPanel();
            _lblNeighbors = new Label();
            _txtNeighbors = new TextBox();
            _lblPinCity = new Label();
            _txtPinCity = new TextBox();
            _introButtons = new FlowLayoutPanel();
            _btnIntroCopy = new Button();
            _btnIntroTemplate = new Button();
            _btnIntroReset = new Button();
            _lblIntroTemplate = new Label();
            _btnIntroTemplateReset = new Button();
            _lblIntroWarn = new Label();
            _txtIntroPreview = new TextBox();
            _split = new SplitContainer();
            _grid = new DataGridView();
            _colName = new DataGridViewTextBoxColumn();
            _colSignal = new DataGridViewTextBoxColumn();
            _colScore = new DataGridViewTextBoxColumn();
            _colActive = new DataGridViewTextBoxColumn();
            _colActiveVs2019 = new DataGridViewTextBoxColumn();
            _colMonthsSupply = new DataGridViewTextBoxColumn();
            _colSoldYoY = new DataGridViewTextBoxColumn();
            _colPriceFromPeak = new DataGridViewTextBoxColumn();
            _colSaleToList = new DataGridViewTextBoxColumn();
            _colCutVsState = new DataGridViewTextBoxColumn();
            _right = new TableLayoutPanel();
            _metricRow = new FlowLayoutPanel();
            _lblMetric = new Label();
            _cbMetric = new ComboBox();
            _plot = new ScottPlot.WinForms.FormsPlot();
            _info = new RichTextBox();
            _bottom = new TableLayoutPanel();
            _progress = new ProgressBar();
            _status = new Label();
            _top.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
            _split.Panel1.SuspendLayout();
            _split.Panel2.SuspendLayout();
            _split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
            _right.SuspendLayout();
            _metricRow.SuspendLayout();
            _bottom.SuspendLayout();
            _mainTabs.SuspendLayout();
            _tabCounties.SuspendLayout();
            _tabVideo.SuspendLayout();
            _videoLayout.SuspendLayout();
            _grpAnim.SuspendLayout();
            _animLayout.SuspendLayout();
            _animTabs.SuspendLayout();
            _tabAnimTexts.SuspendLayout();
            _tabAnimCharts.SuspendLayout();
            _textsLayout.SuspendLayout();
            _chartsLayout.SuspendLayout();
            _chartsRow.SuspendLayout();
            _chartAddRow.SuspendLayout();
            _chartMetricsRow.SuspendLayout();
            _chartCardRow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_gridCharts).BeginInit();
            _chartBottomRow.SuspendLayout();
            _animStudioRow.SuspendLayout();
            _animTextsRow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_gridTexts).BeginInit();
            _animButtons.SuspendLayout();
            _grpIntro.SuspendLayout();
            _introLayout.SuspendLayout();
            _introFields.SuspendLayout();
            _introButtons.SuspendLayout();
            SuspendLayout();
            //
            // _top
            //
            _top.AutoSize = true;
            _top.Controls.Add(_lblState);
            _top.Controls.Add(_cbState);
            _top.Controls.Add(_lblCounties);
            _top.Controls.Add(_lblKey);
            _top.Controls.Add(_txtKey);
            _top.Controls.Add(_btnFetch);
            _top.Controls.Add(_btnCancel);
            _top.Controls.Add(_btnLoad);
            _top.Controls.Add(_btnOpenOut);
            _top.Controls.Add(_btnRedfin);
            _top.Controls.Add(_lblRedfin);
            _top.Controls.Add(_btnAttachRedfin);
            _top.Controls.Add(_lblHouse);
            _top.Controls.Add(_cbHouseMode);
            _top.Controls.Add(_lblBand);
            _top.Controls.Add(_txtBand);
            _top.Controls.Add(_chkCdp);
            _top.Controls.Add(_btnHouseCards);
            _top.Controls.Add(_btnHouseDetail);
            _top.Dock = DockStyle.Top;
            _top.Location = new Point(0, 0);
            _top.Name = "_top";
            _top.Padding = new Padding(8, 6, 8, 2);
            _top.Size = new Size(1544, 70);
            _top.TabIndex = 0;
            _top.SetFlowBreak(_btnOpenOut, true);
            //
            // _lblState
            //
            _lblState.AutoSize = true;
            _lblState.Location = new Point(18, 13);
            _lblState.Margin = new Padding(10, 7, 4, 0);
            _lblState.Name = "_lblState";
            _lblState.Size = new Size(43, 15);
            _lblState.TabIndex = 0;
            _lblState.Text = "Eyalet:";
            //
            // _cbState
            //
            _cbState.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbState.Location = new Point(68, 9);
            _cbState.Name = "_cbState";
            _cbState.Size = new Size(200, 23);
            _cbState.TabIndex = 1;
            //
            // _lblCounties
            //
            _lblCounties.AutoSize = true;
            _lblCounties.ForeColor = SystemColors.GrayText;
            _lblCounties.Location = new Point(277, 13);
            _lblCounties.Margin = new Padding(6, 7, 4, 0);
            _lblCounties.Name = "_lblCounties";
            _lblCounties.Size = new Size(38, 15);
            _lblCounties.TabIndex = 2;
            _lblCounties.Text = "0 ilçe";
            //
            // _lblKey
            //
            _lblKey.AutoSize = true;
            _lblKey.Location = new Point(329, 13);
            _lblKey.Margin = new Padding(10, 7, 4, 0);
            _lblKey.Name = "_lblKey";
            _lblKey.Size = new Size(104, 15);
            _lblKey.TabIndex = 3;
            _lblKey.Text = "FRED API anahtarı:";
            //
            // _txtKey
            //
            _txtKey.Location = new Point(440, 9);
            _txtKey.Name = "_txtKey";
            _txtKey.Size = new Size(260, 23);
            _txtKey.TabIndex = 4;
            _txtKey.UseSystemPasswordChar = true;
            //
            // _btnFetch
            //
            _btnFetch.AutoSize = true;
            _btnFetch.Enabled = false;
            _btnFetch.Location = new Point(706, 9);
            _btnFetch.Name = "_btnFetch";
            _btnFetch.Size = new Size(82, 25);
            _btnFetch.TabIndex = 5;
            _btnFetch.Text = "Verileri çek";
            _btnFetch.UseVisualStyleBackColor = true;
            _btnFetch.Click += BtnFetch_Click;
            //
            // _btnCancel
            //
            _btnCancel.AutoSize = true;
            _btnCancel.Enabled = false;
            _btnCancel.Location = new Point(794, 9);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(75, 25);
            _btnCancel.TabIndex = 6;
            _btnCancel.Text = "İptal";
            _btnCancel.UseVisualStyleBackColor = true;
            _btnCancel.Click += BtnCancel_Click;
            //
            // _btnLoad
            //
            _btnLoad.AutoSize = true;
            _btnLoad.Location = new Point(875, 9);
            _btnLoad.Name = "_btnLoad";
            _btnLoad.Size = new Size(111, 25);
            _btnLoad.TabIndex = 7;
            _btnLoad.Text = "Son sonucu yükle";
            _btnLoad.UseVisualStyleBackColor = true;
            _btnLoad.Click += BtnLoad_Click;
            //
            // _btnOpenOut
            //
            _btnOpenOut.AutoSize = true;
            _btnOpenOut.Location = new Point(992, 9);
            _btnOpenOut.Name = "_btnOpenOut";
            _btnOpenOut.Size = new Size(80, 25);
            _btnOpenOut.TabIndex = 8;
            _btnOpenOut.Text = "out klasörü";
            _btnOpenOut.UseVisualStyleBackColor = true;
            _btnOpenOut.Click += BtnOpenOut_Click;
            //
            // _btnRedfin
            //
            _btnRedfin.AutoSize = true;
            _btnRedfin.Location = new Point(11, 40);
            _btnRedfin.Name = "_btnRedfin";
            _btnRedfin.Size = new Size(131, 25);
            _btnRedfin.TabIndex = 9;
            _btnRedfin.Text = "Redfin verisini indir";
            _btnRedfin.UseVisualStyleBackColor = true;
            _btnRedfin.Click += BtnRedfin_Click;
            //
            // _lblRedfin
            //
            _lblRedfin.AutoSize = true;
            _lblRedfin.ForeColor = SystemColors.GrayText;
            _lblRedfin.Location = new Point(149, 47);
            _lblRedfin.Margin = new Padding(4, 7, 4, 0);
            _lblRedfin.Name = "_lblRedfin";
            _lblRedfin.Size = new Size(48, 15);
            _lblRedfin.TabIndex = 10;
            _lblRedfin.Text = "Redfin: ";
            //
            // _btnAttachRedfin
            //
            _btnAttachRedfin.AutoSize = true;
            _btnAttachRedfin.Location = new Point(204, 40);
            _btnAttachRedfin.Name = "_btnAttachRedfin";
            _btnAttachRedfin.Size = new Size(164, 25);
            _btnAttachRedfin.TabIndex = 11;
            _btnAttachRedfin.Text = "Redfin'i mevcut sonuca ekle";
            _btnAttachRedfin.UseVisualStyleBackColor = true;
            _btnAttachRedfin.Click += BtnAttachRedfin_Click;
            //
            // _lblHouse
            //
            _lblHouse.AutoSize = true;
            _lblHouse.Location = new Point(391, 47);
            _lblHouse.Margin = new Padding(20, 7, 4, 0);
            _lblHouse.Name = "_lblHouse";
            _lblHouse.Size = new Size(51, 15);
            _lblHouse.TabIndex = 12;
            _lblHouse.Text = "Ev kartı:";
            //
            // _cbHouseMode
            //
            _cbHouseMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbHouseMode.Items.AddRange(new object[] { "Müstakil ev", "Daire" });
            _cbHouseMode.Location = new Point(449, 43);
            _cbHouseMode.Name = "_cbHouseMode";
            _cbHouseMode.Size = new Size(110, 23);
            _cbHouseMode.TabIndex = 13;
            //
            // _lblBand
            //
            _lblBand.AutoSize = true;
            _lblBand.Location = new Point(572, 47);
            _lblBand.Margin = new Padding(10, 7, 4, 0);
            _lblBand.Name = "_lblBand";
            _lblBand.Size = new Size(75, 15);
            _lblBand.TabIndex = 14;
            _lblBand.Text = "Bant (bin $):";
            //
            // _txtBand
            //
            _txtBand.Location = new Point(654, 43);
            _txtBand.Name = "_txtBand";
            _txtBand.PlaceholderText = "oto (ör. 200-450)";
            _txtBand.Size = new Size(110, 23);
            _txtBand.TabIndex = 15;
            //
            // _chkCdp
            //
            _chkCdp.AutoSize = true;
            _chkCdp.Location = new Point(780, 45);
            _chkCdp.Margin = new Padding(10, 5, 3, 3);
            _chkCdp.Name = "_chkCdp";
            _chkCdp.Size = new Size(177, 19);
            _chkCdp.TabIndex = 16;
            _chkCdp.Text = "Açık Chrome'a bağlan (9222)";
            _chkCdp.UseVisualStyleBackColor = true;
            //
            // _btnHouseCards
            //
            _btnHouseCards.AutoSize = true;
            _btnHouseCards.Location = new Point(963, 40);
            _btnHouseCards.Name = "_btnHouseCards";
            _btnHouseCards.Size = new Size(122, 25);
            _btnHouseCards.TabIndex = 17;
            _btnHouseCards.Text = "Ev kartlarını topla";
            _btnHouseCards.UseVisualStyleBackColor = true;
            _btnHouseCards.Click += BtnHouseCards_Click;
            //
            // _btnHouseDetail
            //
            _btnHouseDetail.AutoSize = true;
            _btnHouseDetail.Location = new Point(1091, 40);
            _btnHouseDetail.Name = "_btnHouseDetail";
            _btnHouseDetail.Size = new Size(90, 25);
            _btnHouseDetail.TabIndex = 18;
            _btnHouseDetail.Text = "Ev detayları";
            _btnHouseDetail.UseVisualStyleBackColor = true;
            _btnHouseDetail.Click += BtnHouseDetail_Click;
            //
            // _lblStudio
            //
            _lblStudio.AutoSize = true;
            _lblStudio.Location = new Point(3, 7);
            _lblStudio.Margin = new Padding(3, 7, 4, 0);
            _lblStudio.Name = "_lblStudio";
            _lblStudio.Size = new Size(138, 15);
            _lblStudio.TabIndex = 0;
            _lblStudio.Text = "Harita Stüdyosu klasörü:";
            //
            // _txtStudio
            //
            _txtStudio.Location = new Point(148, 3);
            _txtStudio.Name = "_txtStudio";
            _txtStudio.PlaceholderText = "klasör seçilmedi";
            _txtStudio.ReadOnly = true;
            _txtStudio.Size = new Size(380, 23);
            _txtStudio.TabIndex = 1;
            //
            // _btnStudioBrowse
            //
            _btnStudioBrowse.AutoSize = true;
            _btnStudioBrowse.Location = new Point(534, 2);
            _btnStudioBrowse.Name = "_btnStudioBrowse";
            _btnStudioBrowse.Size = new Size(50, 25);
            _btnStudioBrowse.TabIndex = 2;
            _btnStudioBrowse.Text = "Seç…";
            _btnStudioBrowse.UseVisualStyleBackColor = true;
            _btnStudioBrowse.Click += BtnStudioBrowse_Click;
            //
            // _btnStudioProject
            //
            _btnStudioProject.AutoSize = true;
            _btnStudioProject.Location = new Point(3, 3);
            _btnStudioProject.Name = "_btnStudioProject";
            _btnStudioProject.Size = new Size(140, 25);
            _btnStudioProject.TabIndex = 0;
            _btnStudioProject.Text = "Stüdyo projesi oluştur";
            _btnStudioProject.UseVisualStyleBackColor = true;
            _btnStudioProject.Click += BtnStudioProject_Click;
            //
            // _btnStudioRender
            //
            _btnStudioRender.AutoSize = true;
            _btnStudioRender.Location = new Point(149, 3);
            _btnStudioRender.Name = "_btnStudioRender";
            _btnStudioRender.Size = new Size(130, 25);
            _btnStudioRender.TabIndex = 1;
            _btnStudioRender.Text = "Stüdyoda render al";
            _btnStudioRender.UseVisualStyleBackColor = true;
            _btnStudioRender.Click += BtnStudioRender_Click;
            //
            // _split
            //
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 0);
            _split.Name = "_split";
            //
            // _split.Panel1
            //
            _split.Panel1.Controls.Add(_grid);
            //
            // _split.Panel2
            //
            _split.Panel2.Controls.Add(_right);
            _split.Size = new Size(1536, 739);
            _split.SplitterDistance = 860;
            _split.TabIndex = 0;
            //
            // _grid
            //
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            _grid.BackgroundColor = SystemColors.Window;
            _grid.BorderStyle = BorderStyle.None;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _grid.Columns.AddRange(new DataGridViewColumn[] { _colName, _colSignal, _colScore, _colActive, _colActiveVs2019, _colMonthsSupply, _colSoldYoY, _colPriceFromPeak, _colSaleToList, _colCutVsState });
            _grid.Dock = DockStyle.Fill;
            _grid.Location = new Point(0, 0);
            _grid.Name = "_grid";
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.Size = new Size(864, 767);
            _grid.TabIndex = 0;
            _grid.CurrentCellChanged += Grid_CurrentCellChanged;
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            //
            // _colName
            //
            _colName.HeaderText = "İlçe";
            _colName.Name = "_colName";
            _colName.ReadOnly = true;
            _colName.ToolTipText = "Sayım Bürosu ilçe adı.";
            //
            // _colSignal
            //
            _colSignal.HeaderText = "Sinyal";
            _colSignal.Name = "_colSignal";
            _colSignal.ReadOnly = true;
            _colSignal.ToolTipText = "Programın tek kelimelik yorumu. Sağ panelde gerekçesi yazar.";
            //
            // _colScore
            //
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle1.Format = "0";
            _colScore.DefaultCellStyle = dataGridViewCellStyle1;
            _colScore.HeaderText = "Skor";
            _colScore.Name = "_colScore";
            _colScore.ReadOnly = true;
            _colScore.ToolTipText = "0-100. Mutlak eşiklerden toplanan puan; yüksek = daha kırık piyasa. Döküm sağ panelde.";
            //
            // _colActive
            //
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.Format = "N0";
            _colActive.DefaultCellStyle = dataGridViewCellStyle2;
            _colActive.HeaderText = "Satılık ev";
            _colActive.Name = "_colActive";
            _colActive.ReadOnly = true;
            _colActive.ToolTipText = "O ay ilanda olan ev sayısı.";
            //
            // _colActiveVs2019
            //
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Format = "+0;-0;0";
            _colActiveVs2019.DefaultCellStyle = dataGridViewCellStyle3;
            _colActiveVs2019.HeaderText = "Stok 2019'a göre %";
            _colActiveVs2019.Name = "_colActiveVs2019";
            _colActiveVs2019.ReadOnly = true;
            _colActiveVs2019.ToolTipText = "Satılık ev sayısının pandemi öncesi 2019'un aynı ayına göre değişimi. Artı = normalden fazla stok.";
            //
            // _colMonthsSupply
            //
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle4.Format = "0.0";
            _colMonthsSupply.DefaultCellStyle = dataGridViewCellStyle4;
            _colMonthsSupply.HeaderText = "Aylık stok";
            _colMonthsSupply.Name = "_colMonthsSupply";
            _colMonthsSupply.ReadOnly = true;
            _colMonthsSupply.ToolTipText = "Satılık ev / aylık satış (Redfin). 6+ alıcı piyasası, 3 altı satıcı piyasası.";
            //
            // _colSoldYoY
            //
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle5.Format = "+0;-0;0";
            _colSoldYoY.DefaultCellStyle = dataGridViewCellStyle5;
            _colSoldYoY.HeaderText = "Satış Y/Y %";
            _colSoldYoY.Name = "_colSoldYoY";
            _colSoldYoY.ReadOnly = true;
            _colSoldYoY.ToolTipText = "Satılan ev sayısının geçen yıla göre değişimi (Redfin). Redfin yoksa sözleşmeye bağlanan ev kullanılır.";
            //
            // _colPriceFromPeak
            //
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle6.Format = "+0;-0;0";
            _colPriceFromPeak.DefaultCellStyle = dataGridViewCellStyle6;
            _colPriceFromPeak.HeaderText = "Fiyat zirveden %";
            _colPriceFromPeak.Name = "_colPriceFromPeak";
            _colPriceFromPeak.ReadOnly = true;
            _colPriceFromPeak.ToolTipText = "Gerçekleşen satış fiyatının kendi zirvesine göre düşüşü (Redfin). Redfin yoksa istenen fiyat.";
            //
            // _colSaleToList
            //
            dataGridViewCellStyle7.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle7.Format = "0.0";
            _colSaleToList.DefaultCellStyle = dataGridViewCellStyle7;
            _colSaleToList.HeaderText = "Satış/Liste %";
            _colSaleToList.Name = "_colSaleToList";
            _colSaleToList.ReadOnly = true;
            _colSaleToList.ToolTipText = "Ödenen fiyat, istenen fiyatın yüzde kaçı (Redfin).";
            //
            // _colCutVsState
            //
            dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle8.Format = "+0.0;-0.0;0.0";
            _colCutVsState.DefaultCellStyle = dataGridViewCellStyle8;
            _colCutVsState.HeaderText = "İndirim payı vs eyalet";
            _colCutVsState.Name = "_colCutVsState";
            _colCutVsState.ReadOnly = true;
            _colCutVsState.ToolTipText = "Fiyat kıran satıcı payının eyalet ortalamasına göre puan farkı.";
            //
            // _right
            //
            _right.ColumnCount = 1;
            _right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _right.Controls.Add(_metricRow, 0, 0);
            _right.Controls.Add(_plot, 0, 1);
            _right.Controls.Add(_info, 0, 2);
            _right.Dock = DockStyle.Fill;
            _right.Location = new Point(0, 0);
            _right.Name = "_right";
            _right.RowCount = 3;
            _right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            _right.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            _right.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            _right.Size = new Size(676, 767);
            _right.TabIndex = 0;
            //
            // _metricRow
            //
            _metricRow.Controls.Add(_lblMetric);
            _metricRow.Controls.Add(_cbMetric);
            _metricRow.Dock = DockStyle.Fill;
            _metricRow.Location = new Point(3, 3);
            _metricRow.Name = "_metricRow";
            _metricRow.Padding = new Padding(4, 4, 4, 0);
            _metricRow.Size = new Size(670, 28);
            _metricRow.TabIndex = 0;
            _metricRow.WrapContents = false;
            //
            // _lblMetric
            //
            _lblMetric.AutoSize = true;
            _lblMetric.Location = new Point(14, 11);
            _lblMetric.Margin = new Padding(10, 7, 4, 0);
            _lblMetric.Name = "_lblMetric";
            _lblMetric.Size = new Size(46, 15);
            _lblMetric.TabIndex = 0;
            _lblMetric.Text = "Grafik:";
            //
            // _cbMetric
            //
            _cbMetric.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbMetric.Location = new Point(67, 7);
            _cbMetric.Name = "_cbMetric";
            _cbMetric.Size = new Size(240, 23);
            _cbMetric.TabIndex = 1;
            _cbMetric.SelectedIndexChanged += CbMetric_SelectedIndexChanged;
            //
            // _plot
            //
            _plot.Dock = DockStyle.Fill;
            _plot.Location = new Point(3, 37);
            _plot.Name = "_plot";
            _plot.Size = new Size(670, 325);
            _plot.TabIndex = 1;
            //
            // _info
            //
            _info.BackColor = SystemColors.Control;
            _info.BorderStyle = BorderStyle.None;
            _info.Dock = DockStyle.Fill;
            _info.Font = new Font("Consolas", 9.5F);
            _info.Location = new Point(3, 368);
            _info.Name = "_info";
            _info.ReadOnly = true;
            _info.Size = new Size(670, 396);
            _info.TabIndex = 2;
            _info.Text = "";
            //
            // _bottom
            //
            _bottom.ColumnCount = 2;
            _bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            _bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _bottom.Controls.Add(_progress, 0, 0);
            _bottom.Controls.Add(_status, 1, 0);
            _bottom.Dock = DockStyle.Bottom;
            _bottom.Location = new Point(0, 837);
            _bottom.Name = "_bottom";
            _bottom.RowCount = 1;
            _bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _bottom.Size = new Size(1544, 24);
            _bottom.TabIndex = 2;
            //
            // _progress
            //
            _progress.Dock = DockStyle.Fill;
            _progress.Location = new Point(3, 3);
            _progress.Name = "_progress";
            _progress.Size = new Size(274, 18);
            _progress.TabIndex = 0;
            //
            // _status
            //
            _status.AutoEllipsis = true;
            _status.Dock = DockStyle.Fill;
            _status.Location = new Point(283, 0);
            _status.Name = "_status";
            _status.Size = new Size(1258, 24);
            _status.TabIndex = 1;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            //
            // _mainTabs
            //
            _mainTabs.Controls.Add(_tabCounties);
            _mainTabs.Controls.Add(_tabVideo);
            _mainTabs.Dock = DockStyle.Fill;
            _mainTabs.Location = new Point(0, 70);
            _mainTabs.Name = "_mainTabs";
            _mainTabs.SelectedIndex = 0;
            _mainTabs.Size = new Size(1544, 767);
            _mainTabs.TabIndex = 1;
            _mainTabs.SelectedIndexChanged += MainTabs_SelectedIndexChanged;
            //
            // _tabCounties
            //
            _tabCounties.Controls.Add(_split);
            _tabCounties.Location = new Point(4, 24);
            _tabCounties.Name = "_tabCounties";
            _tabCounties.Size = new Size(1536, 739);
            _tabCounties.TabIndex = 0;
            _tabCounties.Text = "İlçeler";
            _tabCounties.UseVisualStyleBackColor = true;
            //
            // _tabVideo
            //
            _tabVideo.Controls.Add(_videoLayout);
            _tabVideo.Location = new Point(4, 24);
            _tabVideo.Name = "_tabVideo";
            _tabVideo.Padding = new Padding(6);
            _tabVideo.Size = new Size(1536, 739);
            _tabVideo.TabIndex = 1;
            _tabVideo.Text = "Video üretimi";
            _tabVideo.UseVisualStyleBackColor = true;
            //
            // _videoLayout
            //
            _videoLayout.ColumnCount = 2;
            _videoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            _videoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            _videoLayout.Controls.Add(_grpAnim, 0, 0);
            _videoLayout.Controls.Add(_grpIntro, 1, 0);
            _videoLayout.Dock = DockStyle.Fill;
            _videoLayout.Location = new Point(6, 6);
            _videoLayout.Name = "_videoLayout";
            _videoLayout.RowCount = 1;
            _videoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _videoLayout.Size = new Size(1524, 727);
            _videoLayout.TabIndex = 0;
            //
            // _grpAnim
            //
            _grpAnim.Controls.Add(_animLayout);
            _grpAnim.Dock = DockStyle.Fill;
            _grpAnim.Location = new Point(3, 3);
            _grpAnim.Name = "_grpAnim";
            _grpAnim.Padding = new Padding(8);
            _grpAnim.Size = new Size(679, 721);
            _grpAnim.TabIndex = 0;
            _grpAnim.TabStop = false;
            _grpAnim.Text = "Animasyon — Harita Stüdyosu";
            //
            // _animLayout
            //
            _animLayout.ColumnCount = 1;
            _animLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _animLayout.Controls.Add(_animStudioRow, 0, 0);
            _animLayout.Controls.Add(_animTabs, 0, 1);
            _animLayout.Controls.Add(_animButtons, 0, 2);
            _animLayout.Dock = DockStyle.Fill;
            _animLayout.Location = new Point(8, 24);
            _animLayout.Name = "_animLayout";
            _animLayout.RowCount = 3;
            _animLayout.RowStyles.Add(new RowStyle());
            _animLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _animLayout.RowStyles.Add(new RowStyle());
            _animLayout.Size = new Size(663, 689);
            _animLayout.TabIndex = 0;
            //
            // _animTabs
            //
            _animTabs.Controls.Add(_tabAnimTexts);
            _animTabs.Controls.Add(_tabAnimCharts);
            _animTabs.Dock = DockStyle.Fill;
            _animTabs.Location = new Point(3, 40);
            _animTabs.Name = "_animTabs";
            _animTabs.SelectedIndex = 0;
            _animTabs.Size = new Size(657, 609);
            _animTabs.TabIndex = 1;
            //
            // _tabAnimTexts
            //
            _tabAnimTexts.Controls.Add(_textsLayout);
            _tabAnimTexts.Location = new Point(4, 24);
            _tabAnimTexts.Name = "_tabAnimTexts";
            _tabAnimTexts.Padding = new Padding(3);
            _tabAnimTexts.Size = new Size(649, 581);
            _tabAnimTexts.TabIndex = 0;
            _tabAnimTexts.Text = "Metinler";
            _tabAnimTexts.UseVisualStyleBackColor = true;
            //
            // _textsLayout
            //
            _textsLayout.ColumnCount = 1;
            _textsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _textsLayout.Controls.Add(_animTextsRow, 0, 0);
            _textsLayout.Controls.Add(_gridTexts, 0, 1);
            _textsLayout.Dock = DockStyle.Fill;
            _textsLayout.Location = new Point(3, 3);
            _textsLayout.Name = "_textsLayout";
            _textsLayout.RowCount = 2;
            _textsLayout.RowStyles.Add(new RowStyle());
            _textsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _textsLayout.Size = new Size(643, 575);
            _textsLayout.TabIndex = 0;
            //
            // _tabAnimCharts
            //
            _tabAnimCharts.Controls.Add(_chartsLayout);
            _tabAnimCharts.Location = new Point(4, 24);
            _tabAnimCharts.Name = "_tabAnimCharts";
            _tabAnimCharts.Padding = new Padding(3);
            _tabAnimCharts.Size = new Size(649, 581);
            _tabAnimCharts.TabIndex = 1;
            _tabAnimCharts.Text = "Grafikler";
            _tabAnimCharts.UseVisualStyleBackColor = true;
            //
            // _chartsLayout
            //
            _chartsLayout.ColumnCount = 1;
            _chartsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _chartsLayout.Controls.Add(_chartsRow, 0, 0);
            _chartsLayout.Controls.Add(_chartAddRow, 0, 1);
            _chartsLayout.Controls.Add(_chartMetricsRow, 0, 2);
            _chartsLayout.Controls.Add(_chartCardRow, 0, 3);
            _chartsLayout.Controls.Add(_gridCharts, 0, 4);
            _chartsLayout.Controls.Add(_chartBottomRow, 0, 5);
            _chartsLayout.Dock = DockStyle.Fill;
            _chartsLayout.Location = new Point(3, 3);
            _chartsLayout.Name = "_chartsLayout";
            _chartsLayout.RowCount = 6;
            _chartsLayout.RowStyles.Add(new RowStyle());
            _chartsLayout.RowStyles.Add(new RowStyle());
            _chartsLayout.RowStyles.Add(new RowStyle());
            _chartsLayout.RowStyles.Add(new RowStyle());
            _chartsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _chartsLayout.RowStyles.Add(new RowStyle());
            _chartsLayout.Size = new Size(643, 575);
            _chartsLayout.TabIndex = 0;
            //
            // _chartsRow
            //
            _chartsRow.AutoSize = true;
            _chartsRow.Controls.Add(_lblCharts);
            _chartsRow.Controls.Add(_btnChartsPick);
            _chartsRow.Dock = DockStyle.Fill;
            _chartsRow.Location = new Point(3, 3);
            _chartsRow.Name = "_chartsRow";
            _chartsRow.Size = new Size(637, 31);
            _chartsRow.TabIndex = 0;
            _chartsRow.WrapContents = false;
            //
            // _lblCharts
            //
            _lblCharts.AutoSize = true;
            _lblCharts.Location = new Point(3, 8);
            _lblCharts.Margin = new Padding(3, 8, 10, 0);
            _lblCharts.Name = "_lblCharts";
            _lblCharts.Size = new Size(106, 15);
            _lblCharts.TabIndex = 0;
            _lblCharts.Text = "Grafik listesi yok";
            //
            // _btnChartsPick
            //
            _btnChartsPick.AutoSize = true;
            _btnChartsPick.Location = new Point(122, 3);
            _btnChartsPick.Name = "_btnChartsPick";
            _btnChartsPick.Size = new Size(130, 25);
            _btnChartsPick.TabIndex = 1;
            _btnChartsPick.Text = "Grafik dosyası seç…";
            _btnChartsPick.UseVisualStyleBackColor = true;
            _btnChartsPick.Click += BtnChartsPick_Click;
            //
            // _chartAddRow
            //
            _chartAddRow.AutoSize = true;
            _chartAddRow.Controls.Add(_lblChartCounty);
            _chartAddRow.Controls.Add(_cbChartCounty);
            _chartAddRow.Controls.Add(_lblChartRecipe);
            _chartAddRow.Controls.Add(_cbChartRecipe);
            _chartAddRow.Controls.Add(_btnChartAdd);
            _chartAddRow.Dock = DockStyle.Fill;
            _chartAddRow.Location = new Point(3, 40);
            _chartAddRow.Name = "_chartAddRow";
            _chartAddRow.Size = new Size(637, 29);
            _chartAddRow.TabIndex = 1;
            _chartAddRow.WrapContents = false;
            //
            // _lblChartCounty
            //
            _lblChartCounty.AutoSize = true;
            _lblChartCounty.Location = new Point(3, 7);
            _lblChartCounty.Margin = new Padding(3, 7, 3, 0);
            _lblChartCounty.Name = "_lblChartCounty";
            _lblChartCounty.Size = new Size(47, 15);
            _lblChartCounty.TabIndex = 0;
            _lblChartCounty.Text = "County:";
            //
            // _cbChartCounty
            //
            _cbChartCounty.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbChartCounty.Location = new Point(56, 3);
            _cbChartCounty.MaxDropDownItems = 20;
            _cbChartCounty.Name = "_cbChartCounty";
            _cbChartCounty.Size = new Size(160, 23);
            _cbChartCounty.TabIndex = 1;
            //
            // _lblChartRecipe
            //
            _lblChartRecipe.AutoSize = true;
            _lblChartRecipe.Location = new Point(229, 7);
            _lblChartRecipe.Margin = new Padding(10, 7, 3, 0);
            _lblChartRecipe.Name = "_lblChartRecipe";
            _lblChartRecipe.Size = new Size(43, 15);
            _lblChartRecipe.TabIndex = 2;
            _lblChartRecipe.Text = "Grafik:";
            //
            // _cbChartRecipe
            //
            _cbChartRecipe.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbChartRecipe.DropDownWidth = 300;
            _cbChartRecipe.Location = new Point(278, 3);
            _cbChartRecipe.MaxDropDownItems = 12;
            _cbChartRecipe.Name = "_cbChartRecipe";
            _cbChartRecipe.Size = new Size(250, 23);
            _cbChartRecipe.TabIndex = 3;
            _cbChartRecipe.SelectedIndexChanged += CbChartRecipe_SelectedIndexChanged;
            //
            // _btnChartAdd
            //
            _btnChartAdd.AutoSize = true;
            _btnChartAdd.Location = new Point(534, 2);
            _btnChartAdd.Margin = new Padding(3, 2, 3, 2);
            _btnChartAdd.Name = "_btnChartAdd";
            _btnChartAdd.Size = new Size(75, 25);
            _btnChartAdd.TabIndex = 4;
            _btnChartAdd.Text = "Ekle";
            _btnChartAdd.UseVisualStyleBackColor = true;
            _btnChartAdd.Click += BtnChartAdd_Click;
            //
            // _chartMetricsRow
            //
            _chartMetricsRow.AutoSize = true;
            _chartMetricsRow.Controls.Add(_lblQuiz);
            _chartMetricsRow.Controls.Add(_cbQuiz1);
            _chartMetricsRow.Controls.Add(_cbQuiz2);
            _chartMetricsRow.Controls.Add(_cbQuiz3);
            _chartMetricsRow.Dock = DockStyle.Fill;
            _chartMetricsRow.Location = new Point(3, 75);
            _chartMetricsRow.Name = "_chartMetricsRow";
            _chartMetricsRow.Size = new Size(637, 29);
            _chartMetricsRow.TabIndex = 2;
            _chartMetricsRow.Visible = false;
            _chartMetricsRow.WrapContents = false;
            //
            // _lblQuiz
            //
            _lblQuiz.AutoSize = true;
            _lblQuiz.Location = new Point(3, 7);
            _lblQuiz.Margin = new Padding(3, 7, 3, 0);
            _lblQuiz.Name = "_lblQuiz";
            _lblQuiz.Size = new Size(50, 15);
            _lblQuiz.TabIndex = 0;
            _lblQuiz.Text = "Ölçüler:";
            //
            // _cbQuiz1
            //
            _cbQuiz1.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbQuiz1.DropDownWidth = 200;
            _cbQuiz1.Location = new Point(59, 3);
            _cbQuiz1.Name = "_cbQuiz1";
            _cbQuiz1.Size = new Size(170, 23);
            _cbQuiz1.TabIndex = 1;
            //
            // _cbQuiz2
            //
            _cbQuiz2.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbQuiz2.DropDownWidth = 200;
            _cbQuiz2.Location = new Point(235, 3);
            _cbQuiz2.Name = "_cbQuiz2";
            _cbQuiz2.Size = new Size(170, 23);
            _cbQuiz2.TabIndex = 2;
            //
            // _cbQuiz3
            //
            _cbQuiz3.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbQuiz3.DropDownWidth = 200;
            _cbQuiz3.Location = new Point(411, 3);
            _cbQuiz3.Name = "_cbQuiz3";
            _cbQuiz3.Size = new Size(170, 23);
            _cbQuiz3.TabIndex = 3;
            //
            // _chartCardRow
            //
            _chartCardRow.AutoSize = true;
            _chartCardRow.Controls.Add(_lblCardIcon);
            _chartCardRow.Controls.Add(_cbCardIcon);
            _chartCardRow.Controls.Add(_lblCardValue);
            _chartCardRow.Controls.Add(_txtCardValue);
            _chartCardRow.Controls.Add(_lblCardCaption);
            _chartCardRow.Controls.Add(_txtCardCaption);
            _chartCardRow.Dock = DockStyle.Fill;
            _chartCardRow.Location = new Point(3, 110);
            _chartCardRow.Name = "_chartCardRow";
            _chartCardRow.Size = new Size(637, 29);
            _chartCardRow.TabIndex = 3;
            _chartCardRow.Visible = false;
            _chartCardRow.WrapContents = false;
            //
            // _lblCardIcon
            //
            _lblCardIcon.AutoSize = true;
            _lblCardIcon.Location = new Point(3, 7);
            _lblCardIcon.Margin = new Padding(3, 7, 3, 0);
            _lblCardIcon.Name = "_lblCardIcon";
            _lblCardIcon.Size = new Size(42, 15);
            _lblCardIcon.TabIndex = 0;
            _lblCardIcon.Text = "Simge:";
            //
            // _cbCardIcon
            //
            _cbCardIcon.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbCardIcon.Location = new Point(51, 3);
            _cbCardIcon.Name = "_cbCardIcon";
            _cbCardIcon.Size = new Size(90, 23);
            _cbCardIcon.TabIndex = 1;
            //
            // _lblCardValue
            //
            _lblCardValue.AutoSize = true;
            _lblCardValue.Location = new Point(154, 7);
            _lblCardValue.Margin = new Padding(10, 7, 3, 0);
            _lblCardValue.Name = "_lblCardValue";
            _lblCardValue.Size = new Size(39, 15);
            _lblCardValue.TabIndex = 2;
            _lblCardValue.Text = "Değer:";
            //
            // _txtCardValue
            //
            _txtCardValue.Location = new Point(199, 3);
            _txtCardValue.Name = "_txtCardValue";
            _txtCardValue.PlaceholderText = "$580K -> ?";
            _txtCardValue.Size = new Size(110, 23);
            _txtCardValue.TabIndex = 3;
            //
            // _lblCardCaption
            //
            _lblCardCaption.AutoSize = true;
            _lblCardCaption.Location = new Point(322, 7);
            _lblCardCaption.Margin = new Padding(10, 7, 3, 0);
            _lblCardCaption.Name = "_lblCardCaption";
            _lblCardCaption.Size = new Size(58, 15);
            _lblCardCaption.TabIndex = 4;
            _lblCardCaption.Text = "Açıklama:";
            //
            // _txtCardCaption
            //
            _txtCardCaption.Location = new Point(386, 3);
            _txtCardCaption.Name = "_txtCardCaption";
            _txtCardCaption.PlaceholderText = "ONE HOUSE IN / FORT MYERS";
            _txtCardCaption.Size = new Size(230, 23);
            _txtCardCaption.TabIndex = 5;
            //
            // _gridCharts
            //
            _gridCharts.AllowUserToAddRows = false;
            _gridCharts.AllowUserToDeleteRows = false;
            _gridCharts.AllowUserToResizeRows = false;
            _gridCharts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridCharts.BackgroundColor = SystemColors.Window;
            _gridCharts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridCharts.Columns.AddRange(new DataGridViewColumn[] { _gSeq, _gSlot, _gCounty, _gChart, _gMetrics, _gText, _gCaption });
            _gridCharts.Dock = DockStyle.Fill;
            _gridCharts.Location = new Point(3, 145);
            _gridCharts.MultiSelect = false;
            _gridCharts.Name = "_gridCharts";
            _gridCharts.ReadOnly = true;
            _gridCharts.RowHeadersVisible = false;
            _gridCharts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridCharts.Size = new Size(637, 390);
            _gridCharts.TabIndex = 4;
            //
            // _gSeq
            //
            _gSeq.FillWeight = 8F;
            _gSeq.HeaderText = "Sıra";
            _gSeq.Name = "_gSeq";
            _gSeq.ReadOnly = true;
            //
            // _gSlot
            //
            _gSlot.FillWeight = 12F;
            _gSlot.HeaderText = "Yer";
            _gSlot.Name = "_gSlot";
            _gSlot.ReadOnly = true;
            //
            // _gCounty
            //
            _gCounty.FillWeight = 18F;
            _gCounty.HeaderText = "County";
            _gCounty.Name = "_gCounty";
            _gCounty.ReadOnly = true;
            //
            // _gChart
            //
            _gChart.FillWeight = 22F;
            _gChart.HeaderText = "Grafik";
            _gChart.Name = "_gChart";
            _gChart.ReadOnly = true;
            //
            // _gMetrics
            //
            _gMetrics.FillWeight = 22F;
            _gMetrics.HeaderText = "Ölçü / simge";
            _gMetrics.Name = "_gMetrics";
            _gMetrics.ReadOnly = true;
            //
            // _gText
            //
            _gText.FillWeight = 14F;
            _gText.HeaderText = "Yazı";
            _gText.Name = "_gText";
            _gText.ReadOnly = true;
            //
            // _gCaption
            //
            _gCaption.FillWeight = 20F;
            _gCaption.HeaderText = "Açıklama";
            _gCaption.Name = "_gCaption";
            _gCaption.ReadOnly = true;
            //
            // _chartBottomRow
            //
            _chartBottomRow.AutoSize = true;
            _chartBottomRow.Controls.Add(_btnChartDelete);
            _chartBottomRow.Dock = DockStyle.Fill;
            _chartBottomRow.Location = new Point(3, 541);
            _chartBottomRow.Name = "_chartBottomRow";
            _chartBottomRow.Size = new Size(637, 31);
            _chartBottomRow.TabIndex = 5;
            //
            // _btnChartDelete
            //
            _btnChartDelete.AutoSize = true;
            _btnChartDelete.Location = new Point(3, 3);
            _btnChartDelete.Name = "_btnChartDelete";
            _btnChartDelete.Size = new Size(110, 25);
            _btnChartDelete.TabIndex = 0;
            _btnChartDelete.Text = "Seçili satırı sil";
            _btnChartDelete.UseVisualStyleBackColor = true;
            _btnChartDelete.Click += BtnChartDelete_Click;
            //
            // _animStudioRow
            //
            _animStudioRow.AutoSize = true;
            _animStudioRow.Controls.Add(_lblStudio);
            _animStudioRow.Controls.Add(_txtStudio);
            _animStudioRow.Controls.Add(_btnStudioBrowse);
            _animStudioRow.Dock = DockStyle.Fill;
            _animStudioRow.Location = new Point(3, 3);
            _animStudioRow.Name = "_animStudioRow";
            _animStudioRow.Size = new Size(657, 31);
            _animStudioRow.TabIndex = 0;
            _animStudioRow.WrapContents = false;
            //
            // _animTextsRow
            //
            _animTextsRow.AutoSize = true;
            _animTextsRow.Controls.Add(_lblTexts);
            _animTextsRow.Controls.Add(_btnTextsPick);
            _animTextsRow.Dock = DockStyle.Fill;
            _animTextsRow.Location = new Point(3, 3);
            _animTextsRow.Name = "_animTextsRow";
            _animTextsRow.Size = new Size(637, 31);
            _animTextsRow.TabIndex = 1;
            _animTextsRow.WrapContents = false;
            //
            // _lblTexts
            //
            _lblTexts.AutoSize = true;
            _lblTexts.Location = new Point(3, 8);
            _lblTexts.Margin = new Padding(3, 8, 10, 0);
            _lblTexts.Name = "_lblTexts";
            _lblTexts.Size = new Size(104, 15);
            _lblTexts.TabIndex = 0;
            _lblTexts.Text = "Metin dosyası yok";
            //
            // _btnTextsPick
            //
            _btnTextsPick.AutoSize = true;
            _btnTextsPick.Location = new Point(120, 3);
            _btnTextsPick.Name = "_btnTextsPick";
            _btnTextsPick.Size = new Size(125, 25);
            _btnTextsPick.TabIndex = 1;
            _btnTextsPick.Text = "Metin dosyası seç…";
            _btnTextsPick.UseVisualStyleBackColor = true;
            _btnTextsPick.Click += BtnTextsPick_Click;
            //
            // _gridTexts
            //
            _gridTexts.AllowUserToAddRows = false;
            _gridTexts.AllowUserToDeleteRows = false;
            _gridTexts.AllowUserToResizeRows = false;
            _gridTexts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridTexts.BackgroundColor = SystemColors.Window;
            _gridTexts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridTexts.Columns.AddRange(new DataGridViewColumn[] { _tOrder, _tCounty, _tCities, _tStat });
            _gridTexts.Dock = DockStyle.Fill;
            _gridTexts.Location = new Point(3, 40);
            _gridTexts.MultiSelect = false;
            _gridTexts.Name = "_gridTexts";
            _gridTexts.ReadOnly = true;
            _gridTexts.RowHeadersVisible = false;
            _gridTexts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridTexts.Size = new Size(637, 532);
            _gridTexts.TabIndex = 2;
            //
            // _tOrder
            //
            _tOrder.FillWeight = 12F;
            _tOrder.HeaderText = "Sıra";
            _tOrder.Name = "_tOrder";
            _tOrder.ReadOnly = true;
            //
            // _tCounty
            //
            _tCounty.FillWeight = 28F;
            _tCounty.HeaderText = "County";
            _tCounty.Name = "_tCounty";
            _tCounty.ReadOnly = true;
            //
            // _tCities
            //
            _tCities.FillWeight = 50F;
            _tCities.HeaderText = "Şehirler";
            _tCities.Name = "_tCities";
            _tCities.ReadOnly = true;
            //
            // _tStat
            //
            _tStat.FillWeight = 70F;
            _tStat.HeaderText = "İstatistik";
            _tStat.Name = "_tStat";
            _tStat.ReadOnly = true;
            //
            // _animButtons
            //
            _animButtons.AutoSize = true;
            _animButtons.Controls.Add(_btnStudioProject);
            _animButtons.Controls.Add(_btnStudioRender);
            _animButtons.Controls.Add(_btnStudioOpenOut);
            _animButtons.Controls.Add(_chkTransparent);
            _animButtons.Controls.Add(_lblStudioUnsupported);
            _animButtons.Dock = DockStyle.Fill;
            _animButtons.Location = new Point(3, 655);
            _animButtons.Name = "_animButtons";
            _animButtons.Size = new Size(657, 31);
            _animButtons.TabIndex = 3;
            //
            // _btnStudioOpenOut
            //
            _btnStudioOpenOut.AutoSize = true;
            _btnStudioOpenOut.Enabled = false;
            _btnStudioOpenOut.Location = new Point(285, 3);
            _btnStudioOpenOut.Name = "_btnStudioOpenOut";
            _btnStudioOpenOut.Size = new Size(120, 25);
            _btnStudioOpenOut.TabIndex = 2;
            _btnStudioOpenOut.Text = "Çıktı klasörünü aç";
            _btnStudioOpenOut.UseVisualStyleBackColor = true;
            _btnStudioOpenOut.Click += BtnStudioOpenOut_Click;
            //
            // _chkTransparent
            //
            _chkTransparent.AutoSize = true;
            _chkTransparent.Location = new Point(419, 7);
            _chkTransparent.Margin = new Padding(12, 7, 3, 3);
            _chkTransparent.Name = "_chkTransparent";
            _chkTransparent.Size = new Size(160, 19);
            _chkTransparent.TabIndex = 3;
            _chkTransparent.Text = "Şeffaf arka plan (MOV)";
            _chkTransparent.UseVisualStyleBackColor = true;
            _chkTransparent.CheckedChanged += ChkTransparent_CheckedChanged;
            //
            // _lblStudioUnsupported
            //
            _lblStudioUnsupported.AutoSize = true;
            _lblStudioUnsupported.ForeColor = Color.FromArgb(190, 30, 30);
            _lblStudioUnsupported.Location = new Point(416, 8);
            _lblStudioUnsupported.Margin = new Padding(8, 8, 3, 0);
            _lblStudioUnsupported.Name = "_lblStudioUnsupported";
            _lblStudioUnsupported.Size = new Size(300, 15);
            _lblStudioUnsupported.TabIndex = 4;
            _lblStudioUnsupported.Text = "Harita Stüdyosu yalnızca ana karadaki 48 eyaleti destekler";
            _lblStudioUnsupported.Visible = false;
            //
            // _grpIntro
            //
            _grpIntro.Controls.Add(_introLayout);
            _grpIntro.Dock = DockStyle.Fill;
            _grpIntro.Location = new Point(688, 3);
            _grpIntro.Name = "_grpIntro";
            _grpIntro.Padding = new Padding(8);
            _grpIntro.Size = new Size(833, 721);
            _grpIntro.TabIndex = 1;
            _grpIntro.TabStop = false;
            _grpIntro.Text = "Intro prompt'u — Flow";
            //
            // _introLayout
            //
            _introLayout.ColumnCount = 1;
            _introLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _introLayout.Controls.Add(_introFields, 0, 0);
            _introLayout.Controls.Add(_introButtons, 0, 1);
            _introLayout.Controls.Add(_lblIntroWarn, 0, 2);
            _introLayout.Controls.Add(_txtIntroPreview, 0, 3);
            _introLayout.Dock = DockStyle.Fill;
            _introLayout.Location = new Point(8, 24);
            _introLayout.Name = "_introLayout";
            _introLayout.RowCount = 4;
            _introLayout.RowStyles.Add(new RowStyle());
            _introLayout.RowStyles.Add(new RowStyle());
            _introLayout.RowStyles.Add(new RowStyle());
            _introLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _introLayout.Size = new Size(817, 689);
            _introLayout.TabIndex = 0;
            //
            // _introFields
            //
            _introFields.AutoSize = true;
            _introFields.ColumnCount = 2;
            _introFields.ColumnStyles.Add(new ColumnStyle());
            _introFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _introFields.Controls.Add(_lblNeighbors, 0, 0);
            _introFields.Controls.Add(_txtNeighbors, 1, 0);
            _introFields.Controls.Add(_lblPinCity, 0, 1);
            _introFields.Controls.Add(_txtPinCity, 1, 1);
            _introFields.Dock = DockStyle.Fill;
            _introFields.Location = new Point(3, 3);
            _introFields.Name = "_introFields";
            _introFields.RowCount = 2;
            _introFields.RowStyles.Add(new RowStyle());
            _introFields.RowStyles.Add(new RowStyle());
            _introFields.Size = new Size(811, 58);
            _introFields.TabIndex = 0;
            //
            // _lblNeighbors
            //
            _lblNeighbors.AutoSize = true;
            _lblNeighbors.Location = new Point(3, 7);
            _lblNeighbors.Margin = new Padding(3, 7, 6, 0);
            _lblNeighbors.Name = "_lblNeighbors";
            _lblNeighbors.Size = new Size(59, 15);
            _lblNeighbors.TabIndex = 0;
            _lblNeighbors.Text = "Komşular:";
            //
            // _txtNeighbors
            //
            _txtNeighbors.Dock = DockStyle.Fill;
            _txtNeighbors.Location = new Point(71, 3);
            _txtNeighbors.Name = "_txtNeighbors";
            _txtNeighbors.Size = new Size(737, 23);
            _txtNeighbors.TabIndex = 1;
            _txtNeighbors.TextChanged += IntroField_TextChanged;
            //
            // _lblPinCity
            //
            _lblPinCity.AutoSize = true;
            _lblPinCity.Location = new Point(3, 36);
            _lblPinCity.Margin = new Padding(3, 7, 6, 0);
            _lblPinCity.Name = "_lblPinCity";
            _lblPinCity.Size = new Size(58, 15);
            _lblPinCity.TabIndex = 2;
            _lblPinCity.Text = "Pin şehri:";
            //
            // _txtPinCity
            //
            _txtPinCity.Dock = DockStyle.Fill;
            _txtPinCity.Location = new Point(71, 32);
            _txtPinCity.Name = "_txtPinCity";
            _txtPinCity.Size = new Size(737, 23);
            _txtPinCity.TabIndex = 3;
            _txtPinCity.TextChanged += IntroField_TextChanged;
            //
            // _introButtons
            //
            _introButtons.AutoSize = true;
            _introButtons.Controls.Add(_btnIntroCopy);
            _introButtons.Controls.Add(_btnIntroReset);
            _introButtons.Controls.Add(_lblIntroTemplate);
            _introButtons.Controls.Add(_btnIntroTemplate);
            _introButtons.Controls.Add(_btnIntroTemplateReset);
            _introButtons.Dock = DockStyle.Fill;
            _introButtons.Location = new Point(3, 67);
            _introButtons.Name = "_introButtons";
            _introButtons.Size = new Size(811, 31);
            _introButtons.TabIndex = 1;
            //
            // _btnIntroCopy
            //
            _btnIntroCopy.AutoSize = true;
            _btnIntroCopy.Location = new Point(3, 3);
            _btnIntroCopy.Name = "_btnIntroCopy";
            _btnIntroCopy.Size = new Size(75, 25);
            _btnIntroCopy.TabIndex = 0;
            _btnIntroCopy.Text = "Kopyala";
            _btnIntroCopy.UseVisualStyleBackColor = true;
            _btnIntroCopy.Click += BtnIntroCopy_Click;
            //
            // _btnIntroTemplate
            //
            _btnIntroTemplate.AutoSize = true;
            _btnIntroTemplate.Location = new Point(355, 3);
            _btnIntroTemplate.Name = "_btnIntroTemplate";
            _btnIntroTemplate.Size = new Size(85, 25);
            _btnIntroTemplate.TabIndex = 3;
            _btnIntroTemplate.Text = "Şablonu aç";
            _btnIntroTemplate.UseVisualStyleBackColor = true;
            _btnIntroTemplate.Click += BtnIntroTemplate_Click;
            //
            // _btnIntroReset
            //
            _btnIntroReset.AutoSize = true;
            _btnIntroReset.Enabled = false;
            _btnIntroReset.Location = new Point(84, 3);
            _btnIntroReset.Name = "_btnIntroReset";
            _btnIntroReset.Size = new Size(110, 25);
            _btnIntroReset.TabIndex = 1;
            _btnIntroReset.Text = "Varsayılana dön";
            _btnIntroReset.UseVisualStyleBackColor = true;
            _btnIntroReset.Click += BtnIntroReset_Click;
            //
            // _lblIntroTemplate
            //
            _lblIntroTemplate.AutoSize = true;
            _lblIntroTemplate.ForeColor = SystemColors.GrayText;
            _lblIntroTemplate.Location = new Point(220, 8);
            _lblIntroTemplate.Margin = new Padding(23, 8, 3, 0);
            _lblIntroTemplate.Name = "_lblIntroTemplate";
            _lblIntroTemplate.Size = new Size(110, 15);
            _lblIntroTemplate.TabIndex = 2;
            _lblIntroTemplate.Text = "Şablon: varsayılan";
            //
            // _btnIntroTemplateReset
            //
            _btnIntroTemplateReset.AutoSize = true;
            _btnIntroTemplateReset.Enabled = false;
            _btnIntroTemplateReset.Location = new Point(446, 3);
            _btnIntroTemplateReset.Name = "_btnIntroTemplateReset";
            _btnIntroTemplateReset.Size = new Size(170, 25);
            _btnIntroTemplateReset.TabIndex = 4;
            _btnIntroTemplateReset.Text = "Şablonu varsayılana döndür";
            _btnIntroTemplateReset.UseVisualStyleBackColor = true;
            _btnIntroTemplateReset.Click += BtnIntroTemplateReset_Click;
            //
            // _lblIntroWarn
            //
            _lblIntroWarn.AutoSize = true;
            _lblIntroWarn.ForeColor = Color.FromArgb(190, 30, 30);
            _lblIntroWarn.Location = new Point(3, 104);
            _lblIntroWarn.Margin = new Padding(3, 3, 3, 6);
            _lblIntroWarn.Name = "_lblIntroWarn";
            _lblIntroWarn.Size = new Size(0, 15);
            _lblIntroWarn.TabIndex = 2;
            _lblIntroWarn.Visible = false;
            //
            // _txtIntroPreview
            //
            _txtIntroPreview.BackColor = SystemColors.Window;
            _txtIntroPreview.Dock = DockStyle.Fill;
            _txtIntroPreview.Font = new Font("Segoe UI", 10F);
            _txtIntroPreview.Location = new Point(3, 128);
            _txtIntroPreview.Multiline = true;
            _txtIntroPreview.Name = "_txtIntroPreview";
            _txtIntroPreview.ReadOnly = true;
            _txtIntroPreview.ScrollBars = ScrollBars.Vertical;
            _txtIntroPreview.Size = new Size(811, 558);
            _txtIntroPreview.TabIndex = 3;
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1544, 861);
            Controls.Add(_mainTabs);
            Controls.Add(_top);
            Controls.Add(_bottom);
            Font = new Font("Segoe UI", 9F);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FredPull — İlçe konut piyasası (Realtor.com/FRED + Redfin)";
            Load += MainForm_Load;
            _top.ResumeLayout(false);
            _top.PerformLayout();
            _split.Panel1.ResumeLayout(false);
            _split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_split).EndInit();
            _split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
            _right.ResumeLayout(false);
            _metricRow.ResumeLayout(false);
            _metricRow.PerformLayout();
            _bottom.ResumeLayout(false);
            _animStudioRow.ResumeLayout(false);
            _animStudioRow.PerformLayout();
            _animTextsRow.ResumeLayout(false);
            _animTextsRow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_gridTexts).EndInit();
            _textsLayout.ResumeLayout(false);
            _textsLayout.PerformLayout();
            _tabAnimTexts.ResumeLayout(false);
            _chartsRow.ResumeLayout(false);
            _chartsRow.PerformLayout();
            _chartAddRow.ResumeLayout(false);
            _chartAddRow.PerformLayout();
            _chartMetricsRow.ResumeLayout(false);
            _chartMetricsRow.PerformLayout();
            _chartCardRow.ResumeLayout(false);
            _chartCardRow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_gridCharts).EndInit();
            _chartBottomRow.ResumeLayout(false);
            _chartBottomRow.PerformLayout();
            _chartsLayout.ResumeLayout(false);
            _chartsLayout.PerformLayout();
            _tabAnimCharts.ResumeLayout(false);
            _animTabs.ResumeLayout(false);
            _animButtons.ResumeLayout(false);
            _animButtons.PerformLayout();
            _animLayout.ResumeLayout(false);
            _animLayout.PerformLayout();
            _grpAnim.ResumeLayout(false);
            _introFields.ResumeLayout(false);
            _introFields.PerformLayout();
            _introButtons.ResumeLayout(false);
            _introButtons.PerformLayout();
            _introLayout.ResumeLayout(false);
            _introLayout.PerformLayout();
            _grpIntro.ResumeLayout(false);
            _videoLayout.ResumeLayout(false);
            _tabVideo.ResumeLayout(false);
            _tabCounties.ResumeLayout(false);
            _mainTabs.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // Üst çubuk
        private FlowLayoutPanel _top;
        private Label _lblState;
        private ComboBox _cbState;
        private Label _lblCounties;
        private Label _lblKey;
        private TextBox _txtKey;
        private Button _btnFetch;
        private Button _btnCancel;
        private Button _btnLoad;
        private Button _btnOpenOut;
        private Button _btnRedfin;
        private Label _lblRedfin;
        private Button _btnAttachRedfin;
        private Label _lblHouse;
        private ComboBox _cbHouseMode;
        private Label _lblBand;
        private TextBox _txtBand;
        private CheckBox _chkCdp;
        private Button _btnHouseCards;
        private Button _btnHouseDetail;

        // Sekmeler
        private TabControl _mainTabs;
        private TabPage _tabCounties;
        private TabPage _tabVideo;

        // Video üretimi: animasyon (Harita Stüdyosu)
        private TableLayoutPanel _videoLayout;
        private GroupBox _grpAnim;
        private TableLayoutPanel _animLayout;
        private FlowLayoutPanel _animStudioRow;
        private Label _lblStudio;
        private TextBox _txtStudio;
        private Button _btnStudioBrowse;
        private FlowLayoutPanel _animTextsRow;
        private Label _lblTexts;
        private Button _btnTextsPick;
        private DataGridView _gridTexts;
        private DataGridViewTextBoxColumn _tOrder;
        private DataGridViewTextBoxColumn _tCounty;
        private DataGridViewTextBoxColumn _tCities;
        private DataGridViewTextBoxColumn _tStat;
        private FlowLayoutPanel _animButtons;
        private Button _btnStudioProject;
        private Button _btnStudioRender;
        private Button _btnStudioOpenOut;
        private Label _lblStudioUnsupported;
        private CheckBox _chkTransparent;
        private TabControl _animTabs;
        private TabPage _tabAnimTexts;
        private TableLayoutPanel _textsLayout;

        // Video üretimi: grafik listesi (out\grafikler_XX.csv)
        private TabPage _tabAnimCharts;
        private TableLayoutPanel _chartsLayout;
        private FlowLayoutPanel _chartsRow;
        private Label _lblCharts;
        private Button _btnChartsPick;
        private FlowLayoutPanel _chartAddRow;
        private Label _lblChartCounty;
        private ComboBox _cbChartCounty;
        private Label _lblChartRecipe;
        private ComboBox _cbChartRecipe;
        private Button _btnChartAdd;
        private FlowLayoutPanel _chartMetricsRow;
        private Label _lblQuiz;
        private ComboBox _cbQuiz1;
        private ComboBox _cbQuiz2;
        private ComboBox _cbQuiz3;
        private FlowLayoutPanel _chartCardRow;
        private Label _lblCardIcon;
        private ComboBox _cbCardIcon;
        private Label _lblCardValue;
        private TextBox _txtCardValue;
        private Label _lblCardCaption;
        private TextBox _txtCardCaption;
        private DataGridView _gridCharts;
        private DataGridViewTextBoxColumn _gSeq;
        private DataGridViewTextBoxColumn _gSlot;
        private DataGridViewTextBoxColumn _gCounty;
        private DataGridViewTextBoxColumn _gChart;
        private DataGridViewTextBoxColumn _gMetrics;
        private DataGridViewTextBoxColumn _gText;
        private DataGridViewTextBoxColumn _gCaption;
        private FlowLayoutPanel _chartBottomRow;
        private Button _btnChartDelete;

        // Video üretimi: intro prompt'u (Flow)
        private GroupBox _grpIntro;
        private TableLayoutPanel _introLayout;
        private TableLayoutPanel _introFields;
        private Label _lblNeighbors;
        private TextBox _txtNeighbors;
        private Label _lblPinCity;
        private TextBox _txtPinCity;
        private FlowLayoutPanel _introButtons;
        private Button _btnIntroCopy;
        private Button _btnIntroTemplate;
        private Button _btnIntroReset;
        private Label _lblIntroTemplate;
        private Button _btnIntroTemplateReset;
        private Label _lblIntroWarn;
        private TextBox _txtIntroPreview;

        // İlçeler sekmesi: tablo
        private SplitContainer _split;
        private DataGridView _grid;
        private DataGridViewTextBoxColumn _colName;
        private DataGridViewTextBoxColumn _colSignal;
        private DataGridViewTextBoxColumn _colScore;
        private DataGridViewTextBoxColumn _colActive;
        private DataGridViewTextBoxColumn _colActiveVs2019;
        private DataGridViewTextBoxColumn _colMonthsSupply;
        private DataGridViewTextBoxColumn _colSoldYoY;
        private DataGridViewTextBoxColumn _colPriceFromPeak;
        private DataGridViewTextBoxColumn _colSaleToList;
        private DataGridViewTextBoxColumn _colCutVsState;

        // Sağ: grafik + açıklama
        private TableLayoutPanel _right;
        private FlowLayoutPanel _metricRow;
        private Label _lblMetric;
        private ComboBox _cbMetric;
        private ScottPlot.WinForms.FormsPlot _plot;
        private RichTextBox _info;

        // Alt: durum
        private TableLayoutPanel _bottom;
        private ProgressBar _progress;
        private Label _status;
    }
}
