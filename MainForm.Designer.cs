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
            _btnHouseCards = new Button();
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
            _top.Controls.Add(_btnHouseCards);
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
            // _btnHouseCards
            //
            _btnHouseCards.AutoSize = true;
            _btnHouseCards.Location = new Point(770, 40);
            _btnHouseCards.Name = "_btnHouseCards";
            _btnHouseCards.Size = new Size(122, 25);
            _btnHouseCards.TabIndex = 16;
            _btnHouseCards.Text = "Ev kartlarını topla";
            _btnHouseCards.UseVisualStyleBackColor = true;
            _btnHouseCards.Click += BtnHouseCards_Click;
            //
            // _split
            //
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 70);
            _split.Name = "_split";
            //
            // _split.Panel1
            //
            _split.Panel1.Controls.Add(_grid);
            //
            // _split.Panel2
            //
            _split.Panel2.Controls.Add(_right);
            _split.Size = new Size(1544, 767);
            _split.SplitterDistance = 864;
            _split.TabIndex = 1;
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
            // MainForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1544, 861);
            Controls.Add(_split);
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
        private Button _btnHouseCards;

        // Sol: tablo
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
