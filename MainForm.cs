using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FredPull;

/// Arayüz MainForm.Designer.cs'te (Visual Studio tasarımcısı); burada davranış.
public partial class MainForm : Form
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    readonly string _baseDir = AppContext.BaseDirectory;
    readonly string _outDir;
    readonly string _keyFile;
    readonly string _catalogFile;
    readonly string _redfinDir;

    Dictionary<string, List<County>>? _catalog;
    Snapshot _snap = new();
    Dictionary<string, HouseCard> _cards = new();   // fips -> ev kartı (out\listings_XX.json)
    CancellationTokenSource? _cts;

    public MainForm()
    {
        InitializeComponent();

        _outDir = Path.Combine(_baseDir, "out");
        _keyFile = Path.Combine(_baseDir, "fred_api_key.txt");
        _catalogFile = Path.Combine(_baseDir, "counties_all.txt");
        _redfinDir = Path.Combine(_baseDir, "redfin");

        foreach (var m in Analyzer.Metrics) _cbMetric.Items.Add(m.Name);
        _cbMetric.SelectedIndex = 0;
        _cbHouseMode.SelectedIndex = 0;

        foreach (var s in CountyCatalog.States.OrderBy(s => s.Name))
            _cbState.Items.Add(new StateItem(s.Abbr, s.Name));
        SelectState("FL");
        // Eyalet seçildikten sonra bağlanır; yoksa açılışta önbellek iki kez yüklenir.
        _cbState.SelectedIndexChanged += CbState_SelectedIndexChanged;

        if (File.Exists(_keyFile)) _txtKey.Text = File.ReadAllText(_keyFile).Trim();
        UpdateRedfinLabel();
        LoadSettings();
        SetupVideoTab();
        SetupChartControls();

        _info.Text = Analyzer.Glossary;
    }

    async void MainForm_Load(object? sender, EventArgs e)
    {
        _split.SplitterDistance = (int)(_split.Width * 0.56);
        await LoadCatalogAsync();
        LoadCache(showMessage: false);
        RefreshVideoTab(reloadFiles: true);
    }

    void BtnCancel_Click(object? sender, EventArgs e) => _cts?.Cancel();
    void BtnLoad_Click(object? sender, EventArgs e) => LoadCache(showMessage: true);
    void CbMetric_SelectedIndexChanged(object? sender, EventArgs e) => UpdatePlot();
    void Grid_CurrentCellChanged(object? sender, EventArgs e) => UpdatePlot();
    void CbState_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateCountyCount();
        LoadCache(showMessage: false);
        RefreshVideoTab(reloadFiles: false);
    }

    void BtnOpenOut_Click(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(_outDir);
        Process.Start("explorer.exe", _outDir);
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
        {
            // Eşleşmeyen ilçede eski Redfin serisi kalmasın (mevcut sonuca yeniden eklerken)
            r.Data.Redfin = byNorm.TryGetValue(RedfinLoader.NormalizeName(r.County.Name), out var series) ? series : new();
            if (r.Data.Redfin.Count > 0) matched++;
        }
        progress.Report($"Redfin eşleşen ilçe: {matched}/{snap.Counties.Count}");
    }

    /// FRED'i yeniden çekmeden mevcut sonuca Redfin satış verisini ekler.
    async void BtnAttachRedfin_Click(object? sender, EventArgs e)
    {
        if (_snap.Counties.Count == 0) { MessageBox.Show("Mevcut sonuç yok. Önce \"Verileri çek\" ya da \"Son sonucu yükle\".", "Sonuç yok"); return; }
        if (!RedfinReady) { MessageBox.Show("Redfin dosyası yok. Önce \"Redfin verisini indir\".", "Redfin yok"); return; }

        _cts = new CancellationTokenSource();
        SetBusy(true);
        var text = new Progress<string>(s => _status.Text = s);
        try
        {
            var snap = _snap;
            var ct = _cts.Token;
            _status.Text = "Redfin dosyaları okunuyor (ilçe dosyası büyük, 1-2 dk)...";
            await Task.Run(() => AttachRedfin(snap, text, ct), ct);
            snap.HasRedfin = true;
            foreach (var r in snap.Counties) Analyzer.Summarize(r, snap.StateData, snap.UsData);
            FillGrid();
            SaveOutputs();
            _status.Text = $"Redfin mevcut sonuca eklendi: {snap.Counties.Count(r => r.RedfinMonth.Length > 0)}/{snap.Counties.Count} ilçe eşleşti. Çıktılar: {_outDir}";
        }
        catch (OperationCanceledException) { _status.Text = "İptal edildi."; }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Redfin ekleme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Hata: " + ex.Message;
        }
        finally { SetBusy(false); }
    }

    void SetBusy(bool busy)
    {
        Busy = busy;
        _btnFetch.Enabled = !busy; _btnLoad.Enabled = !busy; _cbState.Enabled = !busy; _btnRedfin.Enabled = !busy;
        _btnAttachRedfin.Enabled = !busy; _btnHouseCards.Enabled = !busy; _cbHouseMode.Enabled = !busy; _txtBand.Enabled = !busy; _chkCdp.Enabled = !busy;
        _btnStudioBrowse.Enabled = !busy; _btnTextsPick.Enabled = !busy;
        _btnChartsPick.Enabled = !busy; _btnChartAdd.Enabled = !busy; _btnChartDelete.Enabled = !busy;
        UpdateStudioSupport();
        _btnCancel.Enabled = busy;
        if (!busy) _progress.Value = 0;
    }

    // ---------- ev kartları (Redfin ilanları) ----------

    async void BtnHouseCards_Click(object? sender, EventArgs e)
    {
        if (_snap.Counties.Count == 0) { MessageBox.Show("Mevcut sonuç yok. Önce \"Verileri çek\" ya da \"Son sonucu yükle\".", "Sonuç yok"); return; }
        if (!TryParseBand(_txtBand.Text, out var band))
        {
            MessageBox.Show("Bant biçimi: 200-450 (bin $). Boş bırakılırsa ilçenin medyan liste fiyatından hesaplanır.", "Fiyat bandı");
            return;
        }

        var targets = _grid.SelectedRows.Cast<DataGridViewRow>().OrderBy(row => row.Index).Select(row => row.Tag).OfType<CountyResult>().ToList();
        if (targets.Count == 0)
        {
            if (MessageBox.Show($"Seçili ilçe yok. {_snap.Counties.Count} ilçenin tümü için ev kartı toplansın mı?", "Ev kartları",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            targets = _grid.Rows.Cast<DataGridViewRow>().Select(row => row.Tag).OfType<CountyResult>().ToList();
        }

        bool condo = _cbHouseMode.SelectedIndex == 1;
        var modeName = condo ? "Daire" : "Müstakil ev";
        var bandText = band is { } b ? $"bant {b.Min / 1000}k-{b.Max / 1000}k" : "bant ilçe medyan liste fiyatının %60-115'i";
        bool cdp = _chkCdp.Checked;
        var browser = cdp
            ? "Açık Chrome'a (9222) bağlanılır ve yeni bir sekmede çalışılır; Redfin'i o Chrome'da bir kez elle açmış ol."
            : RedfinListingPicker.ChromePath() != null
                ? "Redfin kurulu Google Chrome'da, ayrı bir profille açılır; bitene kadar pencereyi kapatma."
                : "Google Chrome bulunamadı; Playwright Chromium kullanılır" +
                  (RedfinListingPicker.ChromiumInstalled() ? "." : " (ilk kullanımda indirilir, 1-2 dk).");
        if (MessageBox.Show($"{targets.Count} ilçe için ev kartı toplanacak ({modeName}, {bandText}).\n" +
                            $"{browser} İlçe başına ~1-2 dk.\nBaşlasın mı?",
                            "Ev kartları", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        SetBusy(true);
        Directory.CreateDirectory(_outDir);
        var state = _snap.State;
        var logPath = Path.Combine(_outDir, "log_listings.txt");
        using var log = new StreamWriter(logPath, true, Encoding.UTF8) { AutoFlush = true };
        var logLock = new object();
        void Log(string line) { lock (logLock) log.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}"); }
        _listingLog = Log;     // toplama sürerken ev detay formları da bu yazıcıyı kullanır (dosya açık)
        Log($"--- {state}, {targets.Count} ilçe, {modeName}, {bandText}{(cdp ? ", açık Chrome (9222)" : "")}");

        int done = 0, found = 0;
        _progress.Maximum = targets.Count;
        var status = new Progress<string>(s => _status.Text = $"{Math.Min(done + 1, targets.Count)}/{targets.Count}  {s}");
        RedfinListingPicker? picker = null;
        try
        {
            _status.Text = cdp ? $"Açık Chrome'a bağlanılıyor ({RedfinListingPicker.CdpEndpoint})..." : "Tarayıcı açılıyor...";
            picker = await Task.Run(() => RedfinListingPicker.StartAsync(_baseDir, _outDir, cdp, Log, status), ct);
            foreach (var r in targets)
            {
                ct.ThrowIfCancellationRequested();
                HouseCard card;
                try
                {
                    card = await Task.Run(() => picker.CollectAsync(r.County, r.ListPrice, condo, band, status, ct), ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (picker.Closed) throw new InvalidOperationException("Tarayıcı penceresi kapatıldı; toplama durdu.");
                    Log($"{r.County.Name}: atlandı — {ex.Message}");
                    card = new HouseCard { Fips = r.County.Fips, County = r.County.Name, Mode = modeName, CreatedAt = DateTime.Now, Note = "hata: " + ex.Message };
                }

                // Hata aldıysa önceki başarılı kartın üstüne yazma
                if (card.Chosen != null || !(_cards.TryGetValue(card.Fips, out var old) && old.Chosen != null))
                    _cards[card.Fips] = card;
                if (card.Chosen != null) found++;
                Log($"{r.County.Name}: {(card.Chosen != null ? card.CardText : card.Note)}");
                RedfinListingPicker.SaveListings(_outDir, state, _cards.Values);

                done++;
                _progress.Value = done;
                if (Selected == r) UpdatePlot();
            }
            _status.Text = $"Ev kartları bitti: {found}/{targets.Count} ilçede ev seçildi. out\\listings_{state}.csv";
        }
        catch (OperationCanceledException) { _status.Text = $"İptal edildi ({done}/{targets.Count} ilçe kaydedildi)."; }
        catch (RedfinListingPicker.ChromeNotReachableException ex)
        {
            _status.Text = "Açık Chrome bulunamadı (9222).";
            if (RedfinListingPicker.ChromePath() == null)
                MessageBox.Show(ex.Message, "Açık Chrome'a bağlan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else if (MessageBox.Show(ex.Message + "\n\nChrome'u bu ayarla şimdi başlatayım mı? Redfin açılınca sayfanın yüklendiğini gör, sonra \"Ev kartlarını topla\"ya yeniden bas.",
                         "Açık Chrome'a bağlan", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                RedfinListingPicker.StartDebugChrome();
                _status.Text = "Chrome 9222 portuyla açıldı. Redfin yüklenince \"Ev kartlarını topla\"ya yeniden bas.";
            }
        }
        catch (Exception ex)
        {
            Log("Durdu: " + ex.Message);
            MessageBox.Show(ex.Message + $"\n\nLog: {logPath}", "Ev kartları", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Hata: " + ex.Message;
        }
        finally
        {
            if (picker != null) await Task.Run(() => picker.DisposeAsync().AsTask());
            if (done > 0) SaveRanking();
            _listingLog = null;
            SetBusy(false);
        }
    }

    // ---------- Harita Stüdyosu ----------

    sealed class AppSettings
    {
        public string? StudioDir { get; set; }
        public string? LastStudioOut { get; set; }      // son render'ın çıktı klasörü ("Çıktı klasörünü aç")
        public bool Transparent { get; set; }           // stüdyo projesi şeffaf arka planla (MOV) çıksın
    }

    AppSettings _settings = new();
    string SettingsPath => Path.Combine(_baseDir, "fredpull_settings.json");

    /// Ayar dosyası exe yanında. Stüdyo klasörü yoksa ya da geçersizse exe'den yukarı doğru "harita-studyosu" aranır.
    void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
                _settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException) { _settings = new(); }
        if (StudioExport.CheckStudio(_settings.StudioDir) != null && StudioExport.FindStudio(_baseDir) is { } found)
        {
            _settings.StudioDir = found;
            SaveSettings();
        }
        _txtStudio.Text = _settings.StudioDir ?? "";
        _chkTransparent.Checked = _settings.Transparent;
    }

    void ChkTransparent_CheckedChanged(object? sender, EventArgs e)
    {
        if (_settings.Transparent == _chkTransparent.Checked) return;
        _settings.Transparent = _chkTransparent.Checked;
        SaveSettings();
    }

    void SaveSettings()
    {
        try { File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true })); }
        catch (IOException ex) { _status.Text = "Ayar dosyası yazılamadı: " + ex.Message; }
    }

    void BtnStudioBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Harita Stüdyosu klasörünü seç (içinde engine ve .venv olan)",
            UseDescriptionForTitle = true,
            SelectedPath = _settings.StudioDir ?? "",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (StudioExport.CheckStudio(dlg.SelectedPath) is { } err)
        {
            MessageBox.Show(err, "Harita Stüdyosu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _settings.StudioDir = dlg.SelectedPath;
        SaveSettings();
        _txtStudio.Text = dlg.SelectedPath;
    }

    string? StudioDirOrWarn()
    {
        if (StudioExport.CheckStudio(_settings.StudioDir) is not { } err) return _settings.StudioDir;
        MessageBox.Show(err + "\n\nKlasörü \"Seç…\" ile göster.", "Harita Stüdyosu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return null;
    }

    void LogStudio(string line)
    {
        try
        {
            Directory.CreateDirectory(_outDir);
            File.AppendAllText(Path.Combine(_outDir, "log_studio.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}\r\n", Encoding.UTF8);
        }
        catch (IOException) { }
    }

    /// Proje JSON'unu stüdyonun projects klasörüne yazar ve stüdyoya doğrulatır. Kullanılamazsa null (mesaj gösterilmiştir).
    async Task<StudioExport.Result?> ExportStudioProjectAsync(string studioDir, CancellationToken ct)
    {
        if (_snap.Counties.Count == 0)
        {
            MessageBox.Show("Mevcut sonuç yok. Önce \"Verileri çek\" ya da \"Son sonucu yükle\".", "Stüdyo projesi");
            return null;
        }
        var state = _snap.State;
        var warnings = new List<string>();
        var textPath = Path.Combine(_outDir, $"metinler_{state}.csv");
        List<StudioExport.TextRow> texts;
        if (File.Exists(textPath)) texts = StudioExport.ReadTexts(textPath, warnings);
        else
        {
            // Metin dosyası yok: tabloda seçili satırlar ekrandaki sırayla; alt satır seçilen evin şehri, istatistik boş
            var sel = _grid.SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index).Select(r => r.Tag).OfType<CountyResult>().ToList();
            if (sel.Count == 0)
            {
                MessageBox.Show($"out\\metinler_{state}.csv yok ve tabloda seçili ilçe yok. Videodaki ilçeleri seç ya da metin dosyasını koy.", "Stüdyo projesi");
                return null;
            }
            texts = sel.Select((r, i) => new StudioExport.TextRow(i + 1, r.County.Fips, r.County.Name,
                _cards.TryGetValue(r.County.Fips, out var c) && c.Chosen != null ? StudioExport.CityName(c.Chosen.City) : "", "")).ToList();
            warnings.Insert(0, $"out\\metinler_{state}.csv yok: tablodaki {sel.Count} seçili ilçe ekrandaki sırayla alındı, istatistik satırı boş.");
        }
        if (texts.Count == 0)
        {
            MessageBox.Show($"{Path.GetFileName(textPath)} içinde ilçe yok.", "Stüdyo projesi");
            return null;
        }

        // Grafik listesi (varsa): rakamlar Snapshot'tan hesaplanır, değer dökümü out\grafik_degerleri_{ST}.csv'ye yazılır
        var charts = new List<ChartList.Row>();
        var chartsPath = Path.Combine(_outDir, $"grafikler_{state}.csv");
        if (File.Exists(chartsPath)) charts = ChartList.Read(chartsPath, _snap.Counties.First().County.Fips[..2], warnings);
        var cx = new ChartList.Context(_snap, texts, StudioExport.LoadColors(studioDir), warnings);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var name = StudioExport.UniqueName(studioDir, $"{state}_{today.ToString("yyyyMMdd", Inv)}");
        var (project, scenes, seconds) = StudioExport.Build(_snap, _cards, texts, name, today, warnings, charts, cx, _settings.Transparent);
        if (charts.Count > 0)
        {
            var dumpPath = Path.Combine(_outDir, $"grafik_degerleri_{state}.csv");
            ChartList.WriteDump(dumpPath, cx.Dumps);
            LogStudio($"grafik listesi: {charts.Count} satır, {cx.Dumps.Count} değer → {dumpPath}");
        }
        var path = StudioExport.Write(studioDir, project);
        _status.Text = "Stüdyo doğrulaması çalışıyor...";
        var errors = await StudioExport.ValidateAsync(studioDir, path, ct);
        LogStudio($"proje {path}: {scenes} sahne, doğrulama {(errors.Count == 0 ? "temiz" : errors.Count + " hata")}, {warnings.Count} uyarı");
        foreach (var w in warnings) LogStudio("  uyarı: " + w);
        foreach (var er in errors) LogStudio("  hata: " + er);
        _status.Text = $"Stüdyo projesi: {path} — {scenes} sahne, {(errors.Count == 0 ? "doğrulama temiz" : errors.Count + " doğrulama hatası")}";
        return new StudioExport.Result(name, path, scenes, warnings, errors, seconds);
    }

    static string StudioSummary(StudioExport.Result r)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Proje: {r.Path}");
        sb.AppendLine($"{r.Scenes} sahne, video yaklaşık {TimeSpan.FromSeconds(r.VideoSeconds):m\\:ss}.");
        sb.AppendLine(r.Errors.Count == 0 ? "Stüdyo doğrulaması: hata yok." : $"Stüdyo doğrulaması: {r.Errors.Count} hata");
        foreach (var e in r.Errors.Take(15)) sb.AppendLine("  - " + e);
        if (r.Warnings.Count > 0)
        {
            sb.AppendLine("Uyarılar:");
            foreach (var w in r.Warnings.Take(15)) sb.AppendLine("  - " + w);
        }
        return sb.ToString();
    }

    async void BtnStudioProject_Click(object? sender, EventArgs e)
    {
        if (StudioDirOrWarn() is not { } dir) return;
        var ct = BeginWork();
        try
        {
            if (await ExportStudioProjectAsync(dir, ct) is not { } res) return;
            MessageBox.Show(StudioSummary(res) + $"\nStüdyo arayüzünde \"Proje aç…\" listesinde {res.Name} olarak görünür.",
                "Stüdyo projesi", MessageBoxButtons.OK, res.Errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (OperationCanceledException) { _status.Text = "İptal edildi."; }
        catch (Exception ex)
        {
            LogStudio("proje hatası: " + ex.Message);
            MessageBox.Show(ex.Message, "Stüdyo projesi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Hata: " + ex.Message;
        }
        finally { EndWork(); }
    }

    async void BtnStudioRender_Click(object? sender, EventArgs e)
    {
        if (StudioDirOrWarn() is not { } dir) return;
        var ct = BeginWork();
        var sw = new Stopwatch();
        try
        {
            if (await ExportStudioProjectAsync(dir, ct) is not { } res) return;
            if (res.Errors.Count > 0)
            {
                MessageBox.Show(StudioSummary(res) + "\nHatalar düzelmeden render başlamaz.", "Stüdyoda render al", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Geliştirme bilgisayarında render videonun ~5 katı sürüyor (ENTEGRASYON.md §6)
            var estimate = TimeSpan.FromSeconds(res.VideoSeconds * 5);
            if (MessageBox.Show(StudioSummary(res) + $"\nRender tahminen {Math.Ceiling(estimate.TotalMinutes)} dk sürer (işlemciye bağlı). İptal ile durdurulabilir.\nBaşlasın mı?",
                    "Stüdyoda render al", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _progress.Maximum = 1000;
            _progress.Value = 0;
            _status.Text = "Render başlıyor...";
            LogStudio($"render başladı: {res.Path}");
            sw.Start();
            var outputs = await StudioExport.RenderAsync(dir, res.Path, OnStudioEvent, ct);
            sw.Stop();

            var outDir = outputs.Select(Path.GetDirectoryName).FirstOrDefault(d => !string.IsNullOrEmpty(d));
            LogStudio($"render bitti: {sw.Elapsed:h\\:mm\\:ss}, {outputs.Count} dosya — {outDir}");
            _status.Text = $"Render bitti ({sw.Elapsed:h\\:mm\\:ss}): {outputs.Count} dosya — {outDir}";
            if (outDir != null)
            {
                _settings.LastStudioOut = outDir;
                SaveSettings();
                UpdateStudioOpenOut();
                Process.Start("explorer.exe", outDir);
            }
        }
        catch (OperationCanceledException)
        {
            LogStudio($"render iptal edildi ({sw.Elapsed:h\\:mm\\:ss})");
            _status.Text = "Render iptal edildi; stüdyo süreci sonlandırıldı.";
        }
        catch (Exception ex)
        {
            LogStudio("render hatası: " + ex.Message);
            MessageBox.Show(ex.Message, "Stüdyo render hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.Text = "Render hatası: " + ex.Message.Split('\n')[0];
        }
        finally { EndWork(); }
    }

    /// --progress-json olayları: ilerleme = (scene + frame/total) / scenes.
    void OnStudioEvent(JsonElement ev)
    {
        switch (ev.TryGetProperty("event", out var k) ? k.GetString() : null)
        {
            case "progress":
                int scene = ev.GetProperty("scene").GetInt32(), scenes = ev.GetProperty("scenes").GetInt32();
                int frame = ev.GetProperty("frame").GetInt32(), total = Math.Max(1, ev.GetProperty("total").GetInt32());
                double overall = (scene + (double)frame / total) / Math.Max(1, scenes);
                _progress.Value = Math.Clamp((int)(overall * 1000), 0, 1000);
                _status.Text = $"Render: sahne {scene + 1}/{scenes}, kare {frame}/{total} — %{overall * 100:0}";
                break;
            case "compose":
                _status.Text = "Render: sahneler birleştiriliyor...";
                break;
            case "log":
                _status.Text = "Stüdyo: " + ev.GetProperty("message").GetString();
                break;
            case "error":
                _status.Text = "Stüdyo hatası: " + ev.GetProperty("message").GetString();
                break;
        }
    }

    // ---------- Video üretimi sekmesi ----------

    Dictionary<string, IntroPrompt.StateIntro>? _introStates;
    Dictionary<string, IntroPrompt.Override> _introOverrides = new();
    string? _introTemplate;
    string? _introFileError;
    bool _fillingIntro;                 // kutular programdan doldurulurken değişiklik kaydedilmesin
    FileSystemWatcher? _promptWatcher;  // PromptData\ (varsayılan şablon, eyalet verisi)
    FileSystemWatcher? _userTemplateWatcher;   // exe yanındaki kullanıcı şablonu
    readonly System.Windows.Forms.Timer _promptReload = new() { Interval = 300 };

    /// Harita Stüdyosu yalnızca ana karadaki 48 eyaletin haritasını çizer.
    static readonly HashSet<string> StudioUnsupported = new() { "AK", "HI", "DC" };
    bool StudioSupported => !StudioUnsupported.Contains(SelectedAbbr);

    string OverridesPath => Path.Combine(_baseDir, IntroPrompt.OverridesFile);
    string DefaultTemplatePath => Path.Combine(IntroPrompt.Dir(_baseDir), IntroPrompt.TemplateFile);
    string UserTemplatePath => Path.Combine(_baseDir, IntroPrompt.UserTemplateFile);

    /// Kullanıcı düzeltmelerini okur; şablonlar ve eyalet verisi değişince (dışarıdan düzenleme) önizlemeyi yeniler.
    void SetupVideoTab()
    {
        _introOverrides = IntroPrompt.LoadOverrides(OverridesPath);
        // Düzenleyiciler kaydederken birkaç olay üretir ve dosya bir an kilitli olabilir: 300 ms sonra bir kez oku
        _promptReload.Tick += (_, _) => { _promptReload.Stop(); RefreshIntro(reloadFiles: true); };
        var dir = IntroPrompt.Dir(_baseDir);
        if (Directory.Exists(dir)) _promptWatcher = WatchPromptFiles(dir, "*");
        _userTemplateWatcher = WatchPromptFiles(_baseDir, IntroPrompt.UserTemplateFile);
    }

    FileSystemWatcher WatchPromptFiles(string dir, string filter)
    {
        var w = new FileSystemWatcher(dir, filter)
        {
            SynchronizingObject = this,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        w.Changed += PromptFileChanged;
        w.Created += PromptFileChanged;
        w.Deleted += PromptFileChanged;
        w.Renamed += PromptFileChanged;
        return w;
    }

    static bool IsPromptFile(string? name) =>
        name is IntroPrompt.TemplateFile or IntroPrompt.StatesFile or IntroPrompt.UserTemplateFile;

    void PromptFileChanged(object? sender, FileSystemEventArgs e)
    {
        // Bazı düzenleyiciler geçici dosyaya yazıp yeniden adlandırır: eski ya da yeni ad bizimse yeniden oku
        if (IsPromptFile(e.Name) || (e is RenamedEventArgs r && IsPromptFile(r.OldName))) { _promptReload.Stop(); _promptReload.Start(); }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _promptWatcher?.Dispose();
        _userTemplateWatcher?.Dispose();
        _promptReload.Dispose();
        base.OnFormClosed(e);
    }

    /// Pano başka bir programda açıkken SetText ExternalException atıp programı düşürüyordu: 10 deneme × 100 ms, olmazsa durum satırına yazılır.
    internal static bool TryCopy(string text, Label status)
    {
        try
        {
            Clipboard.SetDataObject(text, true, 10, 100);
            return true;
        }
        catch (ExternalException)
        {
            status.Text = "Pano başka bir program tarafından kullanılıyor, tekrar dene";
            return false;
        }
    }

    /// Alaska, Hawaii ve DC'de stüdyo düğmeleri pasif, yanında açıklama.
    void UpdateStudioSupport()
    {
        _btnStudioProject.Enabled = _btnStudioRender.Enabled = !Busy && StudioSupported;
        _lblStudioUnsupported.Visible = !StudioSupported;
    }

    void MainTabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_mainTabs.SelectedTab == _tabVideo) RefreshVideoTab(reloadFiles: true);
    }

    /// Eyalet değişince, açılışta ve sekme her etkinleştiğinde: metin dosyası, intro kutuları ve önizleme.
    void RefreshVideoTab(bool reloadFiles)
    {
        RefreshTexts();
        RefreshCharts();
        RefreshIntro(reloadFiles);
        UpdateStudioOpenOut();
        UpdateStudioSupport();
    }

    string TextsPath => Path.Combine(_outDir, $"metinler_{SelectedAbbr}.csv");

    void RefreshTexts()
    {
        _gridTexts.Rows.Clear();
        _lblTexts.ForeColor = SystemColors.ControlText;
        if (!File.Exists(TextsPath))
        {
            _lblTexts.Text = "Metin dosyası yok — seçili satırlar ekran sırasıyla kullanılacak";
            _lblTexts.ForeColor = SystemColors.GrayText;
            return;
        }
        try
        {
            var warnings = new List<string>();
            var rows = StudioExport.ReadTexts(TextsPath, warnings);
            foreach (var t in rows)
            {
                var county = t.County.Length > 0 ? t.County
                    : CurrentCounties().FirstOrDefault(c => c.Fips == t.Fips)?.Name ?? t.Fips;
                _gridTexts.Rows.Add(t.Order, county, t.FocusSub, t.FocusStat);
            }
            _gridTexts.ClearSelection();       // salt okunur liste; ilk satır "seçili" görünmesin
            _lblTexts.Text = $"Metin dosyası: {rows.Count} county" + (warnings.Count > 0 ? $" ({warnings.Count} uyarı: {warnings[0]})" : "");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            _lblTexts.Text = "Metin dosyası okunamadı: " + ex.Message;
            _lblTexts.ForeColor = Color.FromArgb(190, 30, 30);
        }
    }

    /// Seçilen CSV'yi out\metinler_{EYALET}.csv adıyla kopyalar (önce okunabildiğini, varsa üzerine yazmayı sorar).
    void BtnTextsPick_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Metin dosyası seç (order, fips, county, focus_sub, focus_stat)",
            Filter = "CSV dosyası (*.csv)|*.csv|Tüm dosyalar (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var target = TextsPath;
        try
        {
            var rows = StudioExport.ReadTexts(dlg.FileName, new List<string>());
            if (rows.Count == 0)
            {
                MessageBox.Show("Dosyada county satırı yok.", "Metin dosyası");
                return;
            }
            if (!string.Equals(Path.GetFullPath(dlg.FileName), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(target) && MessageBox.Show($"{Path.GetFileName(target)} zaten var. Üzerine yazılsın mı?", "Metin dosyası",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                Directory.CreateDirectory(_outDir);
                File.Copy(dlg.FileName, target, overwrite: true);
            }
            RefreshTexts();
            RefreshCharts();   // grafik ekleme listesindeki county'ler metin dosyasından gelir
            _status.Text = $"Metin dosyası: {rows.Count} county — {target}";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "Metin dosyası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ---- grafik listesi (out\grafikler_{ST}.csv) ----

    string ChartsPath => Path.Combine(_outDir, $"grafikler_{SelectedAbbr}.csv");
    string SelectedStateFips => CurrentCounties().FirstOrDefault()?.Fips[..2] ?? "";

    /// Grafik ekleme alanındaki county seçeneği ("Eyalet geneli" için Fips boş).
    sealed record ChartCounty(string Fips, string Name)
    {
        public override string ToString() => Name;
    }

    sealed record ChartRecipeItem(ChartList.Recipe Recipe)
    {
        public override string ToString() => Recipe.Title;
    }

    sealed record QuizMetricItem(string Id, string Title)
    {
        public override string ToString() => Title;
    }

    /// Açılır listelerin sabit içerikleri (constructor'da; tasarımcıda yalnız kontroller).
    void SetupChartControls()
    {
        foreach (var r in ChartList.Recipes) _cbChartRecipe.Items.Add(new ChartRecipeItem(r));
        foreach (var cb in new[] { _cbQuiz1, _cbQuiz2, _cbQuiz3 })
        {
            cb.Items.Add(new QuizMetricItem("", "— yok —"));
            foreach (var (id, title) in ChartList.QuizMetrics) cb.Items.Add(new QuizMetricItem(id, title));
            cb.SelectedIndex = 0;
        }
        foreach (var icon in ChartList.Icons) _cbCardIcon.Items.Add(icon);
        _cbCardIcon.SelectedIndex = 0;
        _cbChartRecipe.SelectedIndex = 0;
    }

    void RefreshCharts()
    {
        // county listesi: metin dosyasındaki video county'leri (yoksa eyaletin hepsi) + eyalet geneli
        var keep = (_cbChartCounty.SelectedItem as ChartCounty)?.Fips;
        _cbChartCounty.Items.Clear();
        _cbChartCounty.Items.Add(new ChartCounty("", "Eyalet geneli"));
        var names = CurrentCounties().ToDictionary(c => c.Fips, c => c.Name);
        List<(string Fips, string Name)> list;
        try
        {
            list = File.Exists(TextsPath)
                ? StudioExport.ReadTexts(TextsPath, new List<string>()).Select(t => (t.Fips, names.GetValueOrDefault(t.Fips, t.County))).ToList()
                : new();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { list = new(); }
        if (list.Count == 0) list = names.OrderBy(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToList();
        foreach (var (fips, name) in list) _cbChartCounty.Items.Add(new ChartCounty(fips, name));
        _cbChartCounty.SelectedIndex = Math.Max(0, _cbChartCounty.Items.Cast<ChartCounty>().ToList().FindIndex(c => c.Fips == keep));
        if (keep == null && _cbChartCounty.Items.Count > 1) _cbChartCounty.SelectedIndex = 1;

        _gridCharts.Rows.Clear();
        _lblCharts.ForeColor = SystemColors.ControlText;
        if (!File.Exists(ChartsPath))
        {
            _lblCharts.Text = "Grafik listesi yok — proje yalnız harita ve fiyat merdiveniyle kurulur";
            _lblCharts.ForeColor = SystemColors.GrayText;
            return;
        }
        try
        {
            var warnings = new List<string>();
            var rows = ChartList.Read(ChartsPath, SelectedStateFips, warnings);
            foreach (var r in rows)
            {
                var i = _gridCharts.Rows.Add(r.Seq, r.Slot, r.Fips.Length > 0 ? names.GetValueOrDefault(r.Fips, r.Fips) : "Eyalet geneli",
                    ChartList.Find(r.Chart)?.Title ?? r.Chart, r.Metrics.Replace(";", ", "), r.Text, r.Caption);
                _gridCharts.Rows[i].Tag = r;
            }
            _gridCharts.ClearSelection();
            _lblCharts.Text = $"Grafik listesi: {rows.Count} satır" + (warnings.Count > 0 ? $", {warnings.Count} uyarı: {warnings[0]}" : "");
            if (warnings.Count > 0) _lblCharts.ForeColor = Color.FromArgb(190, 30, 30);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            _lblCharts.Text = "Grafik listesi okunamadı: " + ex.Message;
            _lblCharts.ForeColor = Color.FromArgb(190, 30, 30);
        }
    }

    void CbChartRecipe_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var id = (_cbChartRecipe.SelectedItem as ChartRecipeItem)?.Recipe.Id;
        _chartMetricsRow.Visible = id == "county_quiz";
        _chartCardRow.Visible = id == "question_card";
    }

    /// Seçili county ve tariften bir satır kurar ve grafikler_{ST}.csv'nin sonuna ekler (dosya yoksa başlıkla oluşturur).
    void BtnChartAdd_Click(object? sender, EventArgs e)
    {
        if (_cbChartRecipe.SelectedItem is not ChartRecipeItem { Recipe: var recipe } || _cbChartCounty.SelectedItem is not ChartCounty county) return;
        var chart = recipe.Id;
        if (!ChartList.StateWide(chart) && county.Fips.Length == 0)
        {
            MessageBox.Show($"\"{recipe.Title}\" bir county ister. County listesinden seç.", "Grafik ekle");
            return;
        }
        string metrics = "", text = "", caption = "";
        if (chart == "county_quiz")
        {
            var ids = new[] { _cbQuiz1, _cbQuiz2, _cbQuiz3 }.Select(cb => (cb.SelectedItem as QuizMetricItem)?.Id ?? "").Where(s => s.Length > 0).Distinct().ToList();
            if (ids.Count == 0) { MessageBox.Show("County soru kartı için en az bir ölçü seç.", "Grafik ekle"); return; }
            metrics = string.Join(";", ids);
        }
        else if (chart == "question_card")
        {
            metrics = _cbCardIcon.SelectedItem as string ?? "none";
            text = _txtCardValue.Text.Trim();
            caption = _txtCardCaption.Text.Trim();
            if (text.Length == 0) { MessageBox.Show("Kartın değerini yaz (ör. $580K -> ?).", "Grafik ekle"); return; }
            if (metrics == "county" && county.Fips.Length == 0) { MessageBox.Show("County simgesi için bir county seç.", "Grafik ekle"); return; }
            if ((text + caption).Contains('%')) { MessageBox.Show("Ekranda yüzde işareti kullanılmaz; '44 OF 100', '3 IN 10' gibi yaz.", "Grafik ekle"); return; }
        }
        try
        {
            var existing = File.Exists(ChartsPath) ? ChartList.Read(ChartsPath, SelectedStateFips, new List<string>()) : new();
            var fips = chart == "months_supply_rank" ? "" : county.Fips;
            var row = new ChartList.Row(ChartList.NextSeq(existing, chart), ChartList.DefaultSlot(chart), fips, chart, metrics, text, caption);
            Directory.CreateDirectory(_outDir);
            ChartList.Append(ChartsPath, row);
            RefreshCharts();
            _status.Text = $"Grafik eklendi: {recipe.Title}{(fips.Length > 0 ? " — " + county.Name : "")} (seq {row.Seq}) → {ChartsPath}";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "Grafik ekle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void BtnChartDelete_Click(object? sender, EventArgs e)
    {
        if (_gridCharts.CurrentRow?.Tag is not ChartList.Row row || !_gridCharts.CurrentRow.Selected)
        {
            MessageBox.Show("Önce listeden bir satır seç.", "Grafik listesi");
            return;
        }
        var title = ChartList.Find(row.Chart)?.Title ?? row.Chart;
        if (MessageBox.Show($"Sıra {row.Seq}: {title} silinsin mi?", "Grafik listesi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            ChartList.DeleteLine(ChartsPath, row.Line);
            RefreshCharts();
            _status.Text = $"Grafik satırı silindi: {title}";
        }
        catch (IOException ex) { MessageBox.Show(ex.Message, "Grafik listesi", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    /// Başka yerdeki grafik listesini out\grafikler_{ST}.csv adıyla kopyalar (önce okunabildiğini, varsa üzerine yazmayı sorar).
    void BtnChartsPick_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Grafik listesi seç (seq, slot, fips, chart, metrics, text, caption)",
            Filter = "CSV dosyası (*.csv)|*.csv|Tüm dosyalar (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var target = ChartsPath;
        try
        {
            var warnings = new List<string>();
            var rows = ChartList.Read(dlg.FileName, SelectedStateFips, warnings);
            if (rows.Count == 0)
            {
                MessageBox.Show("Dosyada kullanılabilir grafik satırı yok." + (warnings.Count > 0 ? "\n\n" + string.Join("\n", warnings.Take(10)) : ""), "Grafik listesi");
                return;
            }
            if (!string.Equals(Path.GetFullPath(dlg.FileName), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(target) && MessageBox.Show($"{Path.GetFileName(target)} zaten var. Üzerine yazılsın mı?", "Grafik listesi",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                Directory.CreateDirectory(_outDir);
                File.Copy(dlg.FileName, target, overwrite: true);
            }
            RefreshCharts();
            _status.Text = $"Grafik listesi: {rows.Count} satır{(warnings.Count > 0 ? $", {warnings.Count} uyarı" : "")} — {target}";
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            MessageBox.Show(ex.Message, "Grafik listesi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void UpdateStudioOpenOut() =>
        _btnStudioOpenOut.Enabled = _settings.LastStudioOut is { } d && Directory.Exists(d);

    void BtnStudioOpenOut_Click(object? sender, EventArgs e)
    {
        if (_settings.LastStudioOut is { } d && Directory.Exists(d)) Process.Start("explorer.exe", d);
        else UpdateStudioOpenOut();
    }

    void LoadIntroFiles()
    {
        var dir = IntroPrompt.Dir(_baseDir);
        var errors = new List<string>();
        try { _introStates = IntroPrompt.LoadStates(Path.Combine(dir, IntroPrompt.StatesFile)); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _introStates = null;
            errors.Add($"{IntroPrompt.StatesFile} okunamadı: {ex.Message}");
        }
        // Kullanıcı kopyası varsa o, yoksa PromptData'daki varsayılan
        bool user = File.Exists(UserTemplatePath);
        var templatePath = user ? UserTemplatePath : DefaultTemplatePath;
        try { _introTemplate = IntroPrompt.LoadTemplate(templatePath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _introTemplate = null;
            errors.Add($"{Path.GetFileName(templatePath)} okunamadı: {ex.Message}");
        }
        _lblIntroTemplate.Text = user ? $"Şablon: kendi kopyan ({IntroPrompt.UserTemplateFile})" : "Şablon: varsayılan";
        _lblIntroTemplate.ForeColor = user ? SystemColors.ControlText : SystemColors.GrayText;
        _btnIntroTemplateReset.Enabled = user;
        _introFileError = errors.Count == 0 ? null : string.Join("\n", errors) + $"\nBeklenen yer: {Path.GetDirectoryName(templatePath)}";
    }

    /// Kutular: önce kullanıcının bu eyalet için kaydettiği değer, yoksa states_intro.json.
    void RefreshIntro(bool reloadFiles)
    {
        if (reloadFiles || (_introStates == null && _introTemplate == null && _introFileError == null)) LoadIntroFiles();
        var abbr = SelectedAbbr;
        var def = _introStates?.GetValueOrDefault(abbr);
        _introOverrides.TryGetValue(abbr, out var ov);
        _fillingIntro = true;
        _txtNeighbors.Text = ov?.Neighbors ?? def?.Neighbors ?? "";
        _txtPinCity.Text = ov?.PinCity ?? def?.PinCity ?? "";
        _fillingIntro = false;
        _btnIntroReset.Enabled = ov != null;
        UpdateIntroPreview();
    }

    string IntroStateName(string abbr) => _introStates?.GetValueOrDefault(abbr)?.Name ?? StateName(abbr);

    void IntroField_TextChanged(object? sender, EventArgs e)
    {
        if (_fillingIntro) return;
        var abbr = SelectedAbbr;
        var def = _introStates?.GetValueOrDefault(abbr);
        if (def != null && _txtNeighbors.Text == def.Neighbors && _txtPinCity.Text == def.PinCity) _introOverrides.Remove(abbr);
        else _introOverrides[abbr] = new IntroPrompt.Override { Neighbors = _txtNeighbors.Text, PinCity = _txtPinCity.Text };
        try { IntroPrompt.SaveOverrides(OverridesPath, _introOverrides); }
        catch (IOException ex) { _status.Text = $"{IntroPrompt.OverridesFile} yazılamadı: {ex.Message}"; }
        _btnIntroReset.Enabled = _introOverrides.ContainsKey(abbr);
        UpdateIntroPreview();
    }

    void BtnIntroReset_Click(object? sender, EventArgs e)
    {
        if (_introOverrides.Remove(SelectedAbbr))
        {
            try { IntroPrompt.SaveOverrides(OverridesPath, _introOverrides); }
            catch (IOException ex) { _status.Text = $"{IntroPrompt.OverridesFile} yazılamadı: {ex.Message}"; }
        }
        RefreshIntro(reloadFiles: false);
        _status.Text = $"Intro değerleri varsayılana döndü ({IntroStateName(SelectedAbbr)})";
    }

    void UpdateIntroPreview()
    {
        var abbr = SelectedAbbr;
        var warnings = new List<string>();
        if (_introFileError != null) warnings.Add(_introFileError);
        else if (_introStates != null && !_introStates.ContainsKey(abbr))
            warnings.Add($"{StateName(abbr)} ({abbr}) {IntroPrompt.StatesFile} içinde yok; Komşular ve Pin şehri kutularını elle doldur.");

        if (_introTemplate == null) _txtIntroPreview.Text = "";
        else
        {
            var neighbors = _txtNeighbors.Text.Trim();
            var pin = _txtPinCity.Text.Trim();
            var text = IntroPrompt.Fill(_introTemplate, IntroStateName(abbr), neighbors, pin);
            _txtIntroPreview.Text = text;
            if (neighbors.Length == 0) warnings.Add("Komşular boş.");
            if (pin.Length == 0) warnings.Add("Pin şehri boş.");
            if (IntroPrompt.Leftovers(text) is { Count: > 0 } left) warnings.Add("Doldurulmamış yer tutucu: " + string.Join(", ", left));
        }
        _lblIntroWarn.Text = string.Join("\n", warnings);
        _lblIntroWarn.Visible = warnings.Count > 0;
        _btnIntroCopy.Enabled = _txtIntroPreview.TextLength > 0;
    }

    void BtnIntroCopy_Click(object? sender, EventArgs e)
    {
        if (_txtIntroPreview.TextLength == 0) return;
        if (TryCopy(_txtIntroPreview.Text, _status))
            _status.Text = $"Intro prompt'u kopyalandı ({IntroStateName(SelectedAbbr)})";
    }

    /// Kullanıcı kopyasını açar; yoksa varsayılan şablondan oluşturur. PromptData'daki kopya her derlemede
    /// depodan yenilendiği için orada yapılan düzenleme kaybolurdu.
    void BtnIntroTemplate_Click(object? sender, EventArgs e)
    {
        var path = UserTemplatePath;
        try
        {
            if (!File.Exists(path))
            {
                if (!File.Exists(DefaultTemplatePath))
                {
                    MessageBox.Show($"Varsayılan şablon bulunamadı:\n{DefaultTemplatePath}", "Intro prompt'u", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                File.Copy(DefaultTemplatePath, path);
                RefreshIntro(reloadFiles: true);
                _status.Text = $"Şablonun kendi kopyası oluşturuldu: {path}";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Şablon kopyası oluşturulamadı:\n{ex.Message}", "Intro prompt'u", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (System.ComponentModel.Win32Exception) { Process.Start("notepad.exe", $"\"{path}\""); }   // .txt ilişkisi yoksa
    }

    void BtnIntroTemplateReset_Click(object? sender, EventArgs e)
    {
        var path = UserTemplatePath;
        if (!File.Exists(path)) { RefreshIntro(reloadFiles: true); return; }
        if (MessageBox.Show($"Şablonda yaptığın değişiklikler silinecek ({IntroPrompt.UserTemplateFile}); önizleme varsayılan şablona döner. Devam edilsin mi?",
                "Şablonu varsayılana döndür", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"{IntroPrompt.UserTemplateFile} silinemedi (düzenleyicide açık olabilir):\n{ex.Message}", "Intro prompt'u", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        RefreshIntro(reloadFiles: true);
        _status.Text = "Şablon varsayılana döndü.";
    }

    // ---------- ev detay formu ----------

    Action<string>? _listingLog;

    void BtnHouseDetail_Click(object? sender, EventArgs e) => OpenHouseDetail(Selected);

    void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0) OpenHouseDetail(_grid.Rows[e.RowIndex].Tag as CountyResult);
    }

    void OpenHouseDetail(CountyResult? r)
    {
        if (r == null) return;
        if (!_cards.ContainsKey(r.County.Fips))
        {
            MessageBox.Show($"{r.County.Name} için ev kartı yok. Önce satırı seçip \"Ev kartlarını topla\".", "Ev detayları");
            return;
        }
        // Aynı ilçe zaten açıksa öne getir; farklı ilçeler için birden fazla form açılabilir
        var open = Application.OpenForms.OfType<HouseDetailForm>().FirstOrDefault(f => f.Fips == r.County.Fips && f.State == _snap.State);
        if (open != null) { open.Activate(); return; }
        new HouseDetailForm(this, r.County.Fips).Show(this);
    }

    // Ev detay formunun kullandığı üyeler
    internal bool Busy { get; private set; }
    internal string BaseDir => _baseDir;
    internal string OutDir => _outDir;
    internal string CardsState => _snap.State;
    internal Dictionary<string, HouseCard> Cards => _cards;
    internal bool UseOpenChrome => _chkCdp.Checked;

    /// Formdan uzun iş (tarayıcı, indirme) başlarken: ana formu meşgul yapar, "İptal" bu işi durdurur.
    internal CancellationToken BeginWork()
    {
        _cts = new CancellationTokenSource();
        SetBusy(true);
        return _cts.Token;
    }

    internal void EndWork() => SetBusy(false);

    /// Kartlar değişti: listings dosyaları, ranking csv ve sağ panel.
    internal void SaveCards()
    {
        RedfinListingPicker.SaveListings(_outDir, _snap.State, _cards.Values);
        if (_snap.Counties.Count > 0) SaveRanking();
        UpdatePlot();
    }

    internal void LogListings(string line)
    {
        if (_listingLog != null) { _listingLog(line); return; }
        try
        {
            Directory.CreateDirectory(_outDir);
            File.AppendAllText(Path.Combine(_outDir, "log_listings.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}\r\n", Encoding.UTF8);
        }
        catch (IOException) { }
    }

    /// "200-450" → 200.000-450.000 $. Boş → null (otomatik).
    static bool TryParseBand(string text, out (int Min, int Max)? band)
    {
        band = null;
        var t = new string(text.Where(ch => char.IsDigit(ch) || ch == '-').ToArray());
        if (t.Length == 0) return true;
        var p = t.Split('-');
        if (p.Length != 2 || !int.TryParse(p[0], out var min) || !int.TryParse(p[1], out var max) || min <= 0 || max <= min) return false;
        band = (min * 1000, max * 1000);
        return true;
    }

    static string CardSection(HouseCard c)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"EV KARTI — {c.Mode}, bant {c.MinPrice / 1000}k-{c.MaxPrice / 1000}k, {c.CreatedAt:dd.MM.yyyy}" +
                      (c.ListCount > 0 ? $", listede {c.ListCount} ilan" : ""));
        if (c.Chosen is { } h)
        {
            sb.AppendLine(Wrap(c.CardText, 110));
            sb.AppendLine(h.Url);
        }
        else sb.AppendLine(c.Note);

        if (c.Candidates.Count > 0)
        {
            sb.AppendLine("Adaylar:");
            foreach (var a in c.Candidates)
            {
                var line = $"{(ReferenceEquals(a, c.Chosen) || a.Url == c.Chosen?.Url ? "*" : " ")} {Trunc($"{a.Street}, {a.City}", 34),-34}" +
                           $"{RedfinListingPicker.Usd(a.CurrentPrice ?? a.Price),10} $ {(a.HasHistory ? a.Days : a.DaysOnRedfin),5} gün";
                if (a.HasHistory)
                {
                    line += $"  {a.Cuts.Count} indirim";
                    if (a.LastSalePrice.HasValue) line += $"  alış {a.LastSaleDate:yyyy-MM} {RedfinListingPicker.Usd(a.LastSalePrice)} $";
                    if (a.PreviouslyWithdrawn) line += "  (önce çekilmiş)";
                }
                else if (a.Error != null) line += $"  ({a.Error})";
                sb.AppendLine(line);
            }
        }
        return sb.ToString();
    }

    static string Trunc(string s, int n) => s.Length > n ? s[..(n - 1)] + "…" : s;

    // ---------- tablo ----------

    /// Sıralama: "Küçük taban" en alta, sonra skor azalan.
    IEnumerable<CountyResult> Ranked() =>
        _snap.Counties.OrderByDescending(r => r.Signal != "Küçük taban").ThenByDescending(r => r.Score).ThenBy(r => r.County.Name);

    void FillGrid()
    {
        _grid.SuspendLayout();
        _grid.Rows.Clear();
        foreach (var r in Ranked())
        {
            int i = _grid.Rows.Add(r.County.Name, r.Signal, r.Score, V(r.Active), V(r.ActiveVs2019), V(r.MonthsSupply),
                V(r.SoldYoY ?? r.PendingYoY), V(r.SalePriceFromPeak ?? r.ListPriceFromPeak), V(r.SaleToList), V(r.CutShareVsState));
            var row = _grid.Rows[i];
            row.Tag = r;
            if (r.Signal == "Küçük taban")
            {
                row.DefaultCellStyle.ForeColor = Color.Gray;
                row.DefaultCellStyle.BackColor = Color.White;
            }
            else if (r.Score >= 60) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 226, 226);
            else if (r.Score >= 40) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 241, 214);
            row.Cells[_colSignal.Index].Style.ForeColor = SignalColor(r.Signal);
            row.Cells[_colSignal.Index].Style.Font = new Font(_grid.Font, FontStyle.Bold);
        }
        _grid.ResumeLayout();
        if (_grid.Rows.Count > 0) { _grid.ClearSelection(); _grid.CurrentCell = _grid.Rows[0].Cells[0]; _grid.Rows[0].Selected = true; }
        UpdatePlot();
    }

    static Color SignalColor(string s) => s switch
    {
        "Küçük taban" => Color.Gray,
        "Alıcı çekildi" => Color.FromArgb(180, 20, 20),
        "Satıcı çekiliyor" => Color.FromArgb(160, 60, 0),
        "Fiyat kırılıyor" => Color.FromArgb(150, 90, 0),
        "Sıcak" => Color.FromArgb(0, 110, 60),
        "Zayıflıyor" => Color.FromArgb(120, 100, 0),
        _ => Color.FromArgb(90, 90, 90),
    };

    static object V(double? d) => d.HasValue ? (object)d.Value : DBNull.Value;

    // ---------- grafik + panel ----------

    /// Panelde gösterilen ilçe: odaktaki satır (çoklu seçimde son tıklanan).
    CountyResult? Selected => _grid.CurrentRow?.Tag as CountyResult;

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
        if (_cards.TryGetValue(r.County.Fips, out var card))
        {
            sb.Append(CardSection(card));
            sb.AppendLine();
        }
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
        SaveRanking();

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

    void SaveRanking()
    {
        Directory.CreateDirectory(_outDir);
        using var w = new StreamWriter(Path.Combine(_outDir, $"ranking_{_snap.State}.csv"), false, Encoding.UTF8);
        w.WriteLine("rank,score,signal,fips,county,month,redfin_month,active,active_yoy_pct,active_vs2019_pct,new_yoy_pct,pending_yoy_pct,dom,dom_yoy_days,cut_share_pct,cut_vs_state_pp,cut_vs_us_pp,list_price,list_price_yoy_pct,list_from_peak_pct,sold,sold_yoy_pct,sale_price,sale_price_yoy_pct,sale_from_peak_pct,sale_to_list_pct,months_supply,reading,house_card");
        int rank = 1;
        foreach (var r in Ranked())
            w.WriteLine(string.Join(",", rank++, r.Score, Q(r.Signal), r.County.Fips, Q(r.County.Name), r.Month, r.RedfinMonth,
                N(r.Active), N(r.ActiveYoY), N(r.ActiveVs2019), N(r.NewYoY), N(r.PendingYoY), N(r.Dom), N(r.DomYoY),
                N(r.CutShare), N(r.CutShareVsState), N(r.CutShareVsUs), N(r.ListPrice), N(r.ListPriceYoY), N(r.ListPriceFromPeak),
                N(r.Sold), N(r.SoldYoY), N(r.SalePrice), N(r.SalePriceYoY), N(r.SalePriceFromPeak), N(r.SaleToList), N(r.MonthsSupply), Q(r.Reading),
                Q(_cards.TryGetValue(r.County.Fips, out var card) ? card.CardText : "")));
    }

    void LoadCache(bool showMessage)
    {
        var path = CachePath(SelectedAbbr);
        _cards = RedfinListingPicker.LoadListings(_outDir, SelectedAbbr);
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
