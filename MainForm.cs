using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ScottPlot.WinForms;

namespace FredPull;

public class MainForm : Form
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    readonly string _baseDir = AppContext.BaseDirectory;
    readonly string _outDir;
    readonly string _keyFile;
    readonly string _catalogFile;
    readonly string _redfinDir;

    // Üst çubuk
    readonly ComboBox _cbState = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    readonly Label _lblCounties = new() { AutoSize = true, Margin = new Padding(6, 7, 4, 0), ForeColor = SystemColors.GrayText };
    readonly TextBox _txtKey = new() { Width = 260, UseSystemPasswordChar = true };
    readonly Button _btnFetch = new() { Text = "Verileri çek", AutoSize = true, Enabled = false };
    readonly Button _btnCancel = new() { Text = "İptal", AutoSize = true, Enabled = false };
    readonly Button _btnRedfin = new() { Text = "Redfin verisini indir", AutoSize = true };
    readonly Label _lblRedfin = new() { AutoSize = true, Margin = new Padding(4, 7, 4, 0), ForeColor = SystemColors.GrayText };
    readonly Button _btnLoad = new() { Text = "Son sonucu yükle", AutoSize = true };
    readonly Button _btnOpenOut = new() { Text = "out klasörü", AutoSize = true };

    // Sol: tablo
    readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.None,
    };

    // Sağ: grafik + açıklama
    readonly ComboBox _cbMetric = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    readonly FormsPlot _plot = new() { Dock = DockStyle.Fill };
    readonly RichTextBox _info = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
        Font = new Font("Consolas", 9.5f), BackColor = SystemColors.Control,
    };
    readonly SplitContainer _split = new() { Dock = DockStyle.Fill };

    // Alt: durum
    readonly ProgressBar _progress = new() { Dock = DockStyle.Fill };
    readonly Label _status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };

    Dictionary<string, List<County>>? _catalog;
    Snapshot _snap = new();
    CancellationTokenSource? _cts;

    public MainForm()
    {
        _outDir = Path.Combine(_baseDir, "out");
        _keyFile = Path.Combine(_baseDir, "fred_api_key.txt");
        _catalogFile = Path.Combine(_baseDir, "counties_all.txt");
        _redfinDir = Path.Combine(_baseDir, "redfin");

        Text = "FredPull — İlçe konut piyasası (Realtor.com/FRED + Redfin)";
        Width = 1560; Height = 900; StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        BuildLayout();
        BuildGridColumns();

        foreach (var m in Analyzer.Metrics) _cbMetric.Items.Add(m.Name);
        _cbMetric.SelectedIndex = 0;

        foreach (var s in CountyCatalog.States.OrderBy(s => s.Name))
            _cbState.Items.Add(new StateItem(s.Abbr, s.Name));
        SelectState("FL");

        if (File.Exists(_keyFile)) _txtKey.Text = File.ReadAllText(_keyFile).Trim();
        UpdateRedfinLabel();

        _btnFetch.Click += BtnFetch_Click;
        _btnRedfin.Click += BtnRedfin_Click;
        _btnCancel.Click += (_, _) => _cts?.Cancel();
        _btnLoad.Click += (_, _) => LoadCache(showMessage: true);
        _btnOpenOut.Click += (_, _) => { Directory.CreateDirectory(_outDir); Process.Start("explorer.exe", _outDir); };
        _cbMetric.SelectedIndexChanged += (_, _) => UpdatePlot();
        _grid.SelectionChanged += (_, _) => UpdatePlot();
        _cbState.SelectedIndexChanged += (_, _) => { UpdateCountyCount(); LoadCache(showMessage: false); };

        _info.Text = Analyzer.Glossary;
        Load += async (_, _) =>
        {
            _split.SplitterDistance = (int)(_split.Width * 0.56);
            await LoadCatalogAsync();
            LoadCache(showMessage: false);
        };
    }

    sealed record StateItem(string Abbr, string Name)
    {
        public override string ToString() => $"{Name} ({Abbr})";
    }

    string SelectedAbbr => (_cbState.SelectedItem as StateItem)?.Abbr ?? "FL";

    void SelectState(string abbr)
    {
        for (int i = 0; i < _cbState.Items.Count; i++)
            if (_cbState.Items[i] is StateItem s && s.Abbr == abbr) { _cbState.SelectedIndex = i; return; }
    }

    // ---------- yerleşim ----------

    void BuildLayout()
    {
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8, 6, 8, 2), WrapContents = false };
        top.Controls.Add(Lbl("Eyalet:"));
        top.Controls.Add(_cbState);
        top.Controls.Add(_lblCounties);
        top.Controls.Add(Lbl("FRED API anahtarı:"));
        top.Controls.Add(_txtKey);
        top.Controls.Add(_btnFetch);
        top.Controls.Add(_btnCancel);
        top.Controls.Add(_btnRedfin);
        top.Controls.Add(_lblRedfin);
        top.Controls.Add(_btnLoad);
        top.Controls.Add(_btnOpenOut);

        _split.Panel1.Controls.Add(_grid);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        var metricRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 0), WrapContents = false };
        metricRow.Controls.Add(Lbl("Grafik:"));
        metricRow.Controls.Add(_cbMetric);
        right.Controls.Add(metricRow, 0, 0);
        right.Controls.Add(_plot, 0, 1);
        right.Controls.Add(_info, 0, 2);
        _split.Panel2.Controls.Add(right);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 24, ColumnCount = 2 };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.Controls.Add(_progress, 0, 0);
        bottom.Controls.Add(_status, 1, 0);

        Controls.Add(_split);
        Controls.Add(top);
        Controls.Add(bottom);
    }

    static Label Lbl(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(10, 7, 4, 0) };

    void BuildGridColumns()
    {
        _grid.Columns.Clear();
        AddCol("Name", "İlçe", null, "Sayım Bürosu ilçe adı.");
        AddCol("Signal", "Sinyal", null, "Programın tek kelimelik yorumu. Sağ panelde gerekçesi yazar.");
        AddCol("Score", "Skor", "0", "0-100. Mutlak eşiklerden toplanan puan; yüksek = daha kırık piyasa. Döküm sağ panelde.");
        AddCol("Active", "Satılık ev", "N0", "O ay ilanda olan ev sayısı.");
        AddCol("ActiveVs2019", "Stok 2019'a göre %", "+0;-0;0", "Satılık ev sayısının pandemi öncesi 2019'un aynı ayına göre değişimi. Artı = normalden fazla stok.");
        AddCol("MonthsSupply", "Aylık stok", "0.0", "Satılık ev / aylık satış (Redfin). 6+ alıcı piyasası, 3 altı satıcı piyasası.");
        AddCol("SoldYoY", "Satış Y/Y %", "+0;-0;0", "Satılan ev sayısının geçen yıla göre değişimi (Redfin). Redfin yoksa sözleşmeye bağlanan ev kullanılır.");
        AddCol("PriceFromPeak", "Fiyat zirveden %", "+0;-0;0", "Gerçekleşen satış fiyatının kendi zirvesine göre düşüşü (Redfin). Redfin yoksa istenen fiyat.");
        AddCol("SaleToList", "Satış/Liste %", "0.0", "Ödenen fiyat, istenen fiyatın yüzde kaçı (Redfin).");
        AddCol("CutVsState", "İndirim payı vs eyalet", "+0.0;-0.0;0.0", "Fiyat kıran satıcı payının eyalet ortalamasına göre puan farkı.");
    }

    void AddCol(string name, string header, string? fmt, string tip)
    {
        var c = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, ToolTipText = tip, SortMode = DataGridViewColumnSortMode.Automatic };
        if (fmt != null)
        {
            c.DefaultCellStyle.Format = fmt;
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        _grid.Columns.Add(c);
    }

    // ---------- katalog ----------

    async Task LoadCatalogAsync()
    {
        _btnFetch.Enabled = false;
        var status = new Progress<string>(s => _status.Text = s);
        try { _catalog = await CountyCatalog.LoadAsync(_catalogFile, status, CancellationToken.None); }
        catch (Exception ex) { _catalog = null; _status.Text = "Katalog hatası: " + ex.Message; }

        if (_catalog == null)
            _status.Text = "İlçe kataloğu indirilemedi; yalnızca Florida kullanılabilir. Elle çözüm: national_county2020.txt dosyasını exe yanına counties_all.txt adıyla koy.";
        else
            _status.Text = $"İlçe kataloğu hazır: {_catalog.Count} eyalet, {_catalog.Values.Sum(l => l.Count):N0} ilçe.";
        UpdateCountyCount();
        _btnFetch.Enabled = true;
    }

    List<County> CurrentCounties()
    {
        var abbr = SelectedAbbr;
        if (_catalog != null && _catalog.TryGetValue(abbr, out var list)) return list;
        return abbr == "FL" ? CountyCatalog.FloridaFallback() : new List<County>();
    }

    void UpdateCountyCount()
    {
        int n = CurrentCounties().Count;
        _lblCounties.Text = n > 0 ? $"{n} ilçe" : "ilçe listesi yok";
    }

    // ---------- Redfin ----------

    string RedfinPath(string url) => Path.Combine(_redfinDir, Path.GetFileName(url));
    bool RedfinReady => File.Exists(RedfinPath(RedfinLoader.CountyUrl));

    void UpdateRedfinLabel()
    {
        _lblRedfin.Text = RedfinReady
            ? $"Redfin: {File.GetLastWriteTime(RedfinPath(RedfinLoader.CountyUrl)):dd.MM.yyyy}"
            : "Redfin: yok (satış verisi için indir)";
    }

    async void BtnRedfin_Click(object? sender, EventArgs e)
    {
        var msg = "Redfin'in üç dosyası indirilecek (ilçe dosyası büyük, birkaç yüz MB; süre bağlantıya göre 2-10 dk).\n" +
                  "Dosyalar exe yanındaki redfin klasörüne kaydedilir; ayda bir yenilemek yeterli.\nBaşlasın mı?";
        if (MessageBox.Show(msg, "Redfin verisi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        Directory.CreateDirectory(_redfinDir);
        _cts = new CancellationTokenSource();
        SetBusy(true);
        var progress = new Progress<string>(s => _status.Text = s);
        try
        {
            foreach (var url in new[] { RedfinLoader.UsUrl, RedfinLoader.StateUrl, RedfinLoader.CountyUrl })
                await RedfinLoader.DownloadAsync(url, RedfinPath(url), progress, _cts.Token);
            _status.Text = "Redfin dosyaları indirildi. Şimdi \"Verileri çek\" ile satış verisi de tabloya girer.";
        }
        catch (OperationCanceledException) { _status.Text = "İndirme iptal edildi."; }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Redfin indirme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Hata: " + ex.Message;
        }
        finally { SetBusy(false); UpdateRedfinLabel(); }
    }

    // ---------- veri çekme ----------

    async void BtnFetch_Click(object? sender, EventArgs e)
    {
        var key = _txtKey.Text.Trim();
        if (key.Length == 0)
        {
            MessageBox.Show("FRED API anahtarı gerekli. Ücretsiz: https://fred.stlouisfed.org/docs/api/api_key.html", "Anahtar yok");
            return;
        }
        File.WriteAllText(_keyFile, key);

        var counties = CurrentCounties();
        if (counties.Count == 0) { MessageBox.Show("Bu eyalet için ilçe listesi yok.", "Liste yok"); return; }
        var state = SelectedAbbr;

        int requests = (counties.Count + 2) * Analyzer.FredKeys.Length;
        var eta = TimeSpan.FromSeconds(requests / 1.8);
        var redfinNote = RedfinReady ? "Redfin satış verisi de işlenecek." : "Redfin dosyası yok: satış tarafı boş kalır, sözleşme sayısı kullanılır.";
        if (MessageBox.Show($"{counties.Count} ilçe + eyalet + ABD, {Analyzer.FredKeys.Length} seri = {requests} FRED isteği, yaklaşık {eta.Minutes} dk {eta.Seconds} sn.\n{redfinNote}\nBaşlasın mı?",
                "Veri çekme", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _cts = new CancellationTokenSource();
        SetBusy(true);
        Directory.CreateDirectory(_outDir);
        var logPath = Path.Combine(_outDir, "log.txt");
        using var log = new StreamWriter(logPath, false, Encoding.UTF8) { AutoFlush = true };
        var logLock = new object();
        var progress = new Progress<(int done, int total, string msg)>(p =>
        {
            _progress.Maximum = Math.Max(1, p.total);
            _progress.Value = Math.Min(p.done, _progress.Maximum);
            _status.Text = $"{p.done}/{p.total}  {p.msg}";
        });
        var text = new Progress<string>(s => _status.Text = s);

        try
        {
            using var client = new FredClient(key);
            client.Log = line => { lock (logLock) log.WriteLine($"{DateTime.Now:HH:mm:ss} {line}"); };
            var ct = _cts.Token;

            _status.Text = "Bağlantı testi: ACTLISCOUUS";
            var test = await client.GetObservationsAsync("ACTLISCOUUS", Analyzer.HistoryStart, ct);
            if (test == null || test.Count == 0)
                throw new InvalidOperationException("Bağlantı testi başarısız: FRED ulusal seri boş döndü. Ayrıntı out\\log.txt");

            var snap = new Snapshot { State = state, CreatedAt = DateTime.Now };
            int total = requests, done = 0;

            // 1) Eyalet ve ABD kıyas serileri
            snap.StateData.Fred = await FetchFredSet(client, state, ct, progress, () => done, v => done = v, total, $"{state} eyaleti");
            snap.UsData.Fred = await FetchFredSet(client, "US", ct, progress, () => done, v => done = v, total, "ABD");

            // 2) İlçeler
            int skipped = 0;
            foreach (var c in counties)
            {
                var set = await FetchFredSet(client, c.Fips, ct, progress, () => done, v => done = v, total, $"{c.Name}  (tamam {snap.Counties.Count}, atlanan {skipped})");
                if (!set.ContainsKey("ACTLISCOU")) { skipped++; continue; }
                snap.Counties.Add(new CountyResult { County = c, Data = new SeriesSet { Fred = set } });
            }
            if (snap.Counties.Count == 0)
                throw new InvalidOperationException($"Hiç ilçe verisi gelmedi: {skipped} ilçenin tamamı için FRED \"seri yok\" döndürdü. Ayrıntı: out\\log.txt");

            // 3) Redfin (satış tarafı)
            if (RedfinReady)
            {
                _status.Text = "Redfin dosyaları okunuyor (ilçe dosyası büyük, 1-2 dk)...";
                await Task.Run(() => AttachRedfin(snap, text, ct), ct);
                snap.HasRedfin = true;
            }

            // 4) Özet, skor, sinyal
            foreach (var r in snap.Counties) Analyzer.Summarize(r, snap.StateData, snap.UsData);

            _snap = snap;
            FillGrid();
            SaveOutputs();
            _status.Text = $"Bitti: {snap.Counties.Count} ilçe, {skipped} atlandı. {(snap.HasRedfin ? "Redfin dahil." : "Redfin yok.")} Çıktılar: {_outDir}";
        }
        catch (OperationCanceledException) { _status.Text = "İptal edildi."; }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + $"\n\nLog: {logPath}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Hata: " + ex.Message;
        }
        finally { SetBusy(false); }
    }

    async Task<Dictionary<string, List<Obs>>> FetchFredSet(FredClient client, string suffix, CancellationToken ct,
        IProgress<(int done, int total, string msg)> progress, Func<int> getDone, Action<int> setDone, int total, string label)
    {
        var dict = new Dictionary<string, List<Obs>>();
        foreach (var key in Analyzer.FredKeys)
        {
            ct.ThrowIfCancellationRequested();
            progress.Report((getDone(), total, $"{label} — {key}{suffix}"));
            var obs = await client.GetObservationsAsync($"{key}{suffix}", Analyzer.HistoryStart, ct);
            if (obs != null) dict[key] = obs;
            setDone(getDone() + 1);
        }
        return dict;
    }

    void AttachRedfin(Snapshot snap, IProgress<string> progress, CancellationToken ct)
    {
        var us = RedfinLoader.Parse(RedfinPath(RedfinLoader.UsUrl), null, progress, ct);
        if (us.Count > 0) snap.UsData.Redfin = us.Values.First();

        var st = RedfinLoader.Parse(RedfinPath(RedfinLoader.StateUrl), snap.State, progress, ct);
        if (st.Count > 0)
            snap.StateData.Redfin = st.TryGetValue(StateName(snap.State), out var stSeries) ? stSeries : st.Values.First();

        var counties = RedfinLoader.Parse(RedfinPath(RedfinLoader.CountyUrl), snap.State, progress, ct);
        var byNorm = new Dictionary<string, Dictionary<string, List<Obs>>>();
        foreach (var (region, series) in counties)
            byNorm[RedfinLoader.NormalizeName(region)] = series;

        int matched = 0;
        foreach (var r in snap.Counties)
            if (byNorm.TryGetValue(RedfinLoader.NormalizeName(r.County.Name), out var series)) { r.Data.Redfin = series; matched++; }
        progress.Report($"Redfin eşleşen ilçe: {matched}/{snap.Counties.Count}");
    }

    void SetBusy(bool busy)
    {
        _btnFetch.Enabled = !busy; _btnLoad.Enabled = !busy; _cbState.Enabled = !busy; _btnRedfin.Enabled = !busy;
        _btnCancel.Enabled = busy;
        if (!busy) _progress.Value = 0;
    }

    // ---------- tablo ----------

    void FillGrid()
    {
        _grid.SuspendLayout();
        _grid.Rows.Clear();
        foreach (var r in _snap.Counties.OrderByDescending(r => r.Score).ThenBy(r => r.County.Name))
        {
            int i = _grid.Rows.Add(r.County.Name, r.Signal, r.Score, V(r.Active), V(r.ActiveVs2019), V(r.MonthsSupply),
                V(r.SoldYoY ?? r.PendingYoY), V(r.SalePriceFromPeak ?? r.ListPriceFromPeak), V(r.SaleToList), V(r.CutShareVsState));
            var row = _grid.Rows[i];
            row.Tag = r;
            if (r.Score >= 60) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 226, 226);
            else if (r.Score >= 40) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 241, 214);
            row.Cells["Signal"].Style.ForeColor = SignalColor(r.Signal);
            row.Cells["Signal"].Style.Font = new Font(_grid.Font, FontStyle.Bold);
        }
        _grid.ResumeLayout();
        if (_grid.Rows.Count > 0) { _grid.ClearSelection(); _grid.Rows[0].Selected = true; }
        UpdatePlot();
    }

    static Color SignalColor(string s) => s switch
    {
        "Alıcı çekildi" => Color.FromArgb(180, 20, 20),
        "Satıcı çekiliyor" => Color.FromArgb(160, 60, 0),
        "Fiyat kırılıyor" => Color.FromArgb(150, 90, 0),
        "Sıcak" => Color.FromArgb(0, 110, 60),
        "Zayıflıyor" => Color.FromArgb(120, 100, 0),
        _ => Color.FromArgb(90, 90, 90),
    };

    static object V(double? d) => d.HasValue ? (object)d.Value : DBNull.Value;

    // ---------- grafik + panel ----------

    CountyResult? Selected => _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].Tag as CountyResult : null;

    void UpdatePlot()
    {
        var r = Selected;
        if (r == null) return;
        var m = Analyzer.Metrics[Math.Max(0, _cbMetric.SelectedIndex)];

        _plot.Plot.Clear();
        var county = Analyzer.MetricSeries(r.Data, m);
        var pts = county?.Where(o => o.Value.HasValue).ToList() ?? new List<Obs>();
        if (pts.Count > 0)
        {
            AddLine(pts, r.County.Name, 2.5f);
            if (m.CompareState) AddLine(Analyzer.MetricSeries(_snap.StateData, m), StateName(_snap.State), 1.5f);
            if (m.CompareUs) AddLine(Analyzer.MetricSeries(_snap.UsData, m), "ABD", 1.5f);

            var lastDate = pts[^1].Date;
            if (m.Line2019 && lastDate.Year > 2019)
            {
                var v2019 = Analyzer.At(pts, new DateOnly(2019, lastDate.Month, 1));
                if (v2019.HasValue)
                {
                    var hl = _plot.Plot.Add.HorizontalLine(v2019.Value);
                    hl.LegendText = $"2019 aynı ay ({v2019.Value.ToString("N0", Inv)})";
                    hl.LinePattern = ScottPlot.LinePattern.Dashed;
                }
            }
            if (m.ShowPeak)
            {
                var (pct, peakDate) = Analyzer.FromPeak(pts, lastDate);
                if (peakDate.HasValue)
                {
                    var peakVal = Analyzer.At(pts, peakDate.Value);
                    if (peakVal.HasValue)
                    {
                        var hl = _plot.Plot.Add.HorizontalLine(peakVal.Value);
                        hl.LegendText = $"Zirve {peakDate.Value:yyyy-MM} ({Analyzer.Signed(pct)}%)";
                        hl.LinePattern = ScottPlot.LinePattern.Dashed;
                    }
                }
            }
            _plot.Plot.Axes.DateTimeTicksBottom();
            _plot.Plot.Title($"{r.County.Name}, {r.County.State} — {m.Name}");
            _plot.Plot.YLabel(m.Unit);
            _plot.Plot.ShowLegend(ScottPlot.Alignment.UpperLeft);
        }
        else
        {
            _plot.Plot.Title($"{r.County.Name} — {m.Name}: veri yok" + (m.Source == "redfin" && !_snap.HasRedfin ? " (Redfin indirilmedi)" : ""));
        }
        _plot.Refresh();
        UpdateInfo(r, m);
    }

    void AddLine(List<Obs>? series, string label, float width)
    {
        var pts = series?.Where(o => o.Value.HasValue).ToList();
        if (pts == null || pts.Count == 0) return;
        double[] xs = pts.Select(p => p.Date.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
        double[] ys = pts.Select(p => p.Value!.Value).ToArray();
        var sc = _plot.Plot.Add.Scatter(xs, ys);
        sc.LegendText = label;
        sc.LineWidth = width;
        sc.MarkerSize = 0;
    }

    static string StateName(string abbr) =>
        CountyCatalog.States.FirstOrDefault(s => s.Abbr == abbr).Name ?? abbr;

    void UpdateInfo(CountyResult r, ChartMetric m)
    {
        var st = _snap.StateData; var us = _snap.UsData;
        var mo = ParseMonth(r.Month);
        var ro = ParseMonth(r.RedfinMonth);
        var stName = StateName(_snap.State);

        var sb = new StringBuilder();
        sb.AppendLine($"{r.County.Name.ToUpperInvariant()}, {r.County.State}   Veri: {Analyzer.MonthName(r.Month)}{(ro.HasValue ? $" (Redfin {Analyzer.MonthName(r.RedfinMonth)})" : "")}");
        sb.AppendLine($"Sinyal: {r.Signal}   Skor: {r.Score}/100");
        sb.AppendLine();
        sb.AppendLine("OKUMA");
        sb.AppendLine(Wrap(r.Reading, 110));
        sb.AppendLine();

        sb.AppendLine($"{"RAKAMLAR",-34}{"İlçe",12}{stName,14}{"ABD",12}");
        if (mo.HasValue)
        {
            var d = mo.Value;
            Row(sb, "Satılık ev", N0(r.Active), N0(Analyzer.At(st.F("ACTLISCOU"), d)), N0(Analyzer.At(us.F("ACTLISCOU"), d)));
            Row(sb, "  geçen yıla göre %", Sg(r.ActiveYoY), Sg(Analyzer.Yoy(st.F("ACTLISCOU"), d)), Sg(Analyzer.Yoy(us.F("ACTLISCOU"), d)));
            Row(sb, "  2019'a göre %", Sg(r.ActiveVs2019), Sg(Analyzer.Vs2019(st.F("ACTLISCOU"), d)), Sg(Analyzer.Vs2019(us.F("ACTLISCOU"), d)));
            Row(sb, "Yeni ilan, geçen yıla göre %", Sg(r.NewYoY), Sg(Analyzer.Yoy(st.F("NEWLISCOU"), d)), Sg(Analyzer.Yoy(us.F("NEWLISCOU"), d)));
            Row(sb, "Sözleşmeye bağlanan, Y/Y %", Sg(r.PendingYoY), Sg(Analyzer.Yoy(st.F("PENLISCOU"), d)), Sg(Analyzer.Yoy(us.F("PENLISCOU"), d)));
            Row(sb, "İlan süresi (gün)", N0(r.Dom), N0(Analyzer.At(st.F("MEDDAYONMAR"), d)), N0(Analyzer.At(us.F("MEDDAYONMAR"), d)));
            Row(sb, "  geçen yıla göre (gün)", Sg(r.DomYoY), Sg(Analyzer.Diff(st.F("MEDDAYONMAR"), d)), Sg(Analyzer.Diff(us.F("MEDDAYONMAR"), d)));
            Row(sb, "Fiyat kıran satıcı payı %", F1(r.CutShare), F1(Analyzer.At(Analyzer.ShareSeries(st), d)), F1(Analyzer.At(Analyzer.ShareSeries(us), d)));
            Row(sb, "İstenen fiyat (medyan $)", N0(r.ListPrice), N0(Analyzer.At(st.F("MEDLISPRI"), d)), N0(Analyzer.At(us.F("MEDLISPRI"), d)));
            Row(sb, "  geçen yıla göre %", Sg(r.ListPriceYoY), Sg(Analyzer.Yoy(st.F("MEDLISPRI"), d)), Sg(Analyzer.Yoy(us.F("MEDLISPRI"), d)));
            Row(sb, "  zirveden %", Sg(r.ListPriceFromPeak), Sg(Analyzer.FromPeak(st.F("MEDLISPRI"), d).Pct), Sg(Analyzer.FromPeak(us.F("MEDLISPRI"), d).Pct));
        }
        if (ro.HasValue)
        {
            var d = ro.Value;
            sb.AppendLine("— Redfin (satış tarafı) —");
            Row(sb, "Satılan ev", N0(r.Sold), N0(Analyzer.At(st.R("homes_sold"), d)), N0(Analyzer.At(us.R("homes_sold"), d)));
            Row(sb, "  geçen yıla göre %", Sg(r.SoldYoY), Sg(Analyzer.Yoy(st.R("homes_sold"), d)), Sg(Analyzer.Yoy(us.R("homes_sold"), d)));
            Row(sb, "Satış fiyatı (medyan $)", N0(r.SalePrice), N0(Analyzer.At(st.R("median_sale_price"), d)), N0(Analyzer.At(us.R("median_sale_price"), d)));
            Row(sb, "  geçen yıla göre %", Sg(r.SalePriceYoY), Sg(Analyzer.Yoy(st.R("median_sale_price"), d)), Sg(Analyzer.Yoy(us.R("median_sale_price"), d)));
            Row(sb, "  zirveden %", Sg(r.SalePriceFromPeak), Sg(Analyzer.FromPeak(st.R("median_sale_price"), d).Pct), Sg(Analyzer.FromPeak(us.R("median_sale_price"), d).Pct));
            Row(sb, "Satış / liste fiyatı %", F1(r.SaleToList), F1(Mul100(Analyzer.At(st.R("avg_sale_to_list"), d))), F1(Mul100(Analyzer.At(us.R("avg_sale_to_list"), d))));
            Row(sb, "Aylık stok", F1(r.MonthsSupply), F1(r.MonthsSupplyState), F1(Analyzer.At(us.R("months_of_supply"), d)));
        }
        else
        {
            sb.AppendLine("— Redfin satış verisi yok: \"Redfin verisini indir\" düğmesi, sonra \"Verileri çek\" —");
        }
        sb.AppendLine();
        sb.AppendLine("SKOR DÖKÜMÜ");
        sb.AppendLine(r.ScoreBreakdown);
        sb.AppendLine();
        sb.AppendLine(Analyzer.Glossary);
        _info.Text = sb.ToString();
    }

    static void Row(StringBuilder sb, string label, string a, string b, string c) =>
        sb.AppendLine($"{label,-34}{a,12}{b,14}{c,12}");

    static string N0(double? v) => v.HasValue ? v.Value.ToString("N0", Inv) : "—";
    static string F1(double? v) => v.HasValue ? v.Value.ToString("0.0", Inv) : "—";
    static string Sg(double? v) => v.HasValue ? v.Value.ToString("+0;-0;0", Inv) : "—";
    static double? Mul100(double? v) => v.HasValue ? v * 100 : null;

    static DateOnly? ParseMonth(string yyyyMM) =>
        DateOnly.TryParseExact(yyyyMM + "-01", "yyyy-MM-dd", Inv, DateTimeStyles.None, out var d) ? d : null;

    static string Wrap(string text, int width)
    {
        var sb = new StringBuilder(); int col = 0;
        foreach (var word in text.Split(' '))
        {
            if (col + word.Length + 1 > width) { sb.Append('\n'); col = 0; }
            if (col > 0) { sb.Append(' '); col++; }
            sb.Append(word); col += word.Length;
        }
        return sb.ToString();
    }

    // ---------- kayıt / yükleme ----------

    string CachePath(string state) => Path.Combine(_outDir, $"cache_{state}.json");

    void SaveOutputs()
    {
        var state = _snap.State;
        Directory.CreateDirectory(Path.Combine(_outDir, "series"));

        using (var w = new StreamWriter(Path.Combine(_outDir, $"ranking_{state}.csv"), false, Encoding.UTF8))
        {
            w.WriteLine("rank,score,signal,fips,county,month,redfin_month,active,active_yoy_pct,active_vs2019_pct,new_yoy_pct,pending_yoy_pct,dom,dom_yoy_days,cut_share_pct,cut_vs_state_pp,cut_vs_us_pp,list_price,list_price_yoy_pct,list_from_peak_pct,sold,sold_yoy_pct,sale_price,sale_price_yoy_pct,sale_from_peak_pct,sale_to_list_pct,months_supply,reading");
            int rank = 1;
            foreach (var r in _snap.Counties.OrderByDescending(r => r.Score).ThenBy(r => r.County.Name))
                w.WriteLine(string.Join(",", rank++, r.Score, Q(r.Signal), r.County.Fips, Q(r.County.Name), r.Month, r.RedfinMonth,
                    N(r.Active), N(r.ActiveYoY), N(r.ActiveVs2019), N(r.NewYoY), N(r.PendingYoY), N(r.Dom), N(r.DomYoY),
                    N(r.CutShare), N(r.CutShareVsState), N(r.CutShareVsUs), N(r.ListPrice), N(r.ListPriceYoY), N(r.ListPriceFromPeak),
                    N(r.Sold), N(r.SoldYoY), N(r.SalePrice), N(r.SalePriceYoY), N(r.SalePriceFromPeak), N(r.SaleToList), N(r.MonthsSupply), Q(r.Reading)));
        }

        foreach (var r in _snap.Counties)
        {
            var keys = Analyzer.FredKeys.Concat(Analyzer.RedfinKeys).ToArray();
            var all = r.Data.Fred.Values.Concat(r.Data.Redfin.Values).SelectMany(s => s).Select(o => o.Date).Distinct().OrderBy(d => d).ToList();
            var safe = new string(r.County.Name.Where(ch => char.IsLetterOrDigit(ch)).ToArray());
            using var w = new StreamWriter(Path.Combine(_outDir, "series", $"{r.County.Fips}_{safe}.csv"), false, Encoding.UTF8);
            w.WriteLine("date," + string.Join(",", keys));
            foreach (var d in all)
                w.WriteLine(d.ToString("yyyy-MM-dd", Inv) + "," + string.Join(",", keys.Select(k =>
                    N(Analyzer.At(r.Data.F(k) ?? r.Data.R(k), d)))));
        }

        File.WriteAllText(CachePath(state), JsonSerializer.Serialize(_snap, JsonOpts));
    }

    void LoadCache(bool showMessage)
    {
        var path = CachePath(SelectedAbbr);
        if (!File.Exists(path))
        {
            _snap = new Snapshot { State = SelectedAbbr };
            _grid.Rows.Clear();
            _plot.Plot.Clear(); _plot.Refresh();
            _info.Text = Analyzer.Glossary;
            if (showMessage) MessageBox.Show("Bu eyalet için kayıtlı sonuç yok.\nÖnce \"Verileri çek\".", "Sonuç yok");
            return;
        }
        try
        {
            _snap = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path), JsonOpts) ?? new Snapshot { State = SelectedAbbr };
            foreach (var r in _snap.Counties) Analyzer.Summarize(r, _snap.StateData, _snap.UsData);
            FillGrid();
            _status.Text = $"Önbellekten yüklendi: {_snap.Counties.Count} ilçe — {_snap.CreatedAt:dd.MM.yyyy HH:mm}{(_snap.HasRedfin ? ", Redfin dahil" : ", Redfin yok")}";
        }
        catch (Exception ex)
        {
            if (showMessage) MessageBox.Show(ex.Message, "Önbellek okunamadı");
        }
    }

    static string N(double? v) => v.HasValue ? v.Value.ToString("0.##", Inv) : "";
    static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}
