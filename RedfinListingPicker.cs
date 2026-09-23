using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace FredPull;

/// Redfin'den her ilçe için "aylardır satılamayan tek bir ev" seçer (Playwright, görünür Chrome, kalıcı profil).
/// Akış: county adresi (autocomplete, önbellekli) → filtreli liste (≥90 gün) → en eski 5 aday → ilan sayfasında fiyat geçmişi → seçim.
public sealed class RedfinListingPicker : IAsyncDisposable
{
    const string Site = "https://www.redfin.com";
    const int TimeoutMs = 45_000;
    public const string CdpEndpoint = "http://localhost:9222";

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public sealed record RedfinRegion(string County, string RegionId, string Url);
    sealed record HistoryEvent(DateOnly Date, string Description, int? Price);
    sealed record PageData(string Html, List<string> Captured);

    /// Redfin iki denemede de engelledi: ilçe atlanır.
    public sealed class BlockedException(string message) : Exception(message);

    /// "Açık Chrome'a bağlan" seçiliyken 9222 portunda Chrome yok.
    public sealed class ChromeNotReachableException(string message) : Exception(message);

    readonly IPlaywright _pw;
    readonly IBrowserContext _ctx;
    readonly IBrowser? _cdpBrowser;       // açık Chrome'a bağlanıldıysa; kapatılmaz, yalnızca bağlantı kesilir
    IPage _page;
    readonly string _regionsFile;
    readonly Dictionary<string, RedfinRegion> _regions;
    readonly Action<string> _log;
    readonly Random _rnd = new();
    IProgress<string>? _status;
    string _label = "";
    bool _navigated;

    /// Kullanıcı tarayıcı penceresini kapattıysa true.
    public bool Closed { get; private set; }

    RedfinListingPicker(IPlaywright pw, IBrowserContext ctx, IPage page, IBrowser? cdpBrowser, string outDir, Action<string> log)
    {
        _pw = pw; _ctx = ctx; _page = page; _cdpBrowser = cdpBrowser; _log = log;
        _regionsFile = Path.Combine(outDir, "redfin_regions.json");
        _regions = LoadRegions(_regionsFile);
        if (cdpBrowser != null) cdpBrowser.Disconnected += (_, _) => Closed = true;
        else _ctx.Close += (_, _) => Closed = true;
    }

    // ---------- tarayıcı ----------

    /// Kurulu Google Chrome'un yolu; yoksa null.
    public static string? ChromePath() =>
        new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.LocalApplicationData }
            .Select(f => Path.Combine(Environment.GetFolderPath(f), "Google", "Chrome", "Application", "chrome.exe"))
            .FirstOrDefault(File.Exists);

    static string DebugProfileDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "pw-chrome");

    /// "Açık Chrome'a bağlan" için kullanıcının çalıştıracağı komut.
    public static string DebugChromeCommand => $"chrome.exe --remote-debugging-port=9222 --user-data-dir=\"{DebugProfileDir}\"";

    /// Chrome'u 9222 hata ayıklama portuyla, ayrı profille ve Redfin açık başlatır. Chrome yoksa false.
    public static bool StartDebugChrome()
    {
        var chrome = ChromePath();
        if (chrome == null) return false;
        var psi = new System.Diagnostics.ProcessStartInfo(chrome) { UseShellExecute = false };
        psi.ArgumentList.Add("--remote-debugging-port=9222");
        psi.ArgumentList.Add($"--user-data-dir={DebugProfileDir}");
        psi.ArgumentList.Add(Site + "/");
        System.Diagnostics.Process.Start(psi);
        return true;
    }

    static string BrowsersDir()
    {
        var dir = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        return string.IsNullOrEmpty(dir)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright")
            : dir;
    }

    /// Kaba kontrol: herhangi bir Chromium kurulu mu. Sürüm uymazsa StartAsync yine de kurar.
    public static bool ChromiumInstalled()
    {
        var dir = BrowsersDir();
        return Directory.Exists(dir) && Directory.EnumerateDirectories(dir, "chromium-*").Any();
    }

    static void InstallChromium()
    {
        int code = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (code != 0) throw new InvalidOperationException($"Chromium kurulamadı (playwright install çıkış kodu {code}).");
    }

    /// connectToChrome: kullanıcının 9222 portuyla açtığı Chrome'a bağlan; değilse kurulu Chrome'u (yoksa Playwright Chromium'u) aç.
    public static async Task<RedfinListingPicker> StartAsync(string baseDir, string outDir, bool connectToChrome, Action<string> log, IProgress<string> status)
    {
        var pw = await Playwright.CreateAsync();
        try
        {
            IBrowserContext ctx;
            IBrowser? cdp = null;
            IPage page;
            if (connectToChrome)
            {
                try { cdp = await pw.Chromium.ConnectOverCDPAsync(CdpEndpoint); }
                catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
                {
                    log($"{CdpEndpoint} bağlantısı kurulamadı: {FirstLine(ex.Message)}");
                    throw new ChromeNotReachableException($"Chrome'u --remote-debugging-port=9222 ile başlat.\n\n{DebugChromeCommand}");
                }
                ctx = cdp.Contexts.FirstOrDefault() ?? await cdp.NewContextAsync();
                page = await ctx.NewPageAsync();         // kullanıcının sekmelerine dokunma, yeni sekmede çalış
                log($"Açık Chrome'a bağlanıldı ({CdpEndpoint}).");
            }
            else
            {
                ctx = await LaunchAsync(pw, baseDir, log, status);
                await ctx.AddInitScriptAsync("Object.defineProperty(navigator,'webdriver',{get:()=>undefined});");
                page = ctx.Pages.FirstOrDefault() ?? await ctx.NewPageAsync();
            }
            ctx.SetDefaultTimeout(TimeoutMs);
            ctx.SetDefaultNavigationTimeout(TimeoutMs);
            return new RedfinListingPicker(pw, ctx, page, cdp, outDir, log);
        }
        catch
        {
            pw.Dispose();
            throw;
        }
    }

    /// Kurulu Google Chrome (otomasyon işaretleri kapalı); açılamazsa Playwright'ın kendi Chromium'u.
    static async Task<IBrowserContext> LaunchAsync(IPlaywright pw, string baseDir, Action<string> log, IProgress<string> status)
    {
        var opts = new BrowserTypeLaunchPersistentContextOptions
        {
            Channel = "chrome",
            Headless = false,
            IgnoreDefaultArgs = new[] { "--enable-automation" },
            Args = new[] { "--disable-blink-features=AutomationControlled", "--no-first-run", "--no-default-browser-check" },
            ViewportSize = ViewportSize.NoViewport,     // sabit görünüm yok, pencere boyutu
            Locale = "en-US",
        };
        try { return await pw.Chromium.LaunchPersistentContextAsync(Path.Combine(baseDir, "pw-profile"), opts); }
        catch (PlaywrightException ex)
        {
            log($"Google Chrome açılamadı, Playwright Chromium kullanılıyor: {FirstLine(ex.Message)}");
        }

        // Chromium profili ayrı: Chrome profilini eski sürüm tarayıcıyla açmak onu bozabilir.
        opts.Channel = null;
        var dir = Path.Combine(baseDir, "pw-profile-chromium");
        try { return await pw.Chromium.LaunchPersistentContextAsync(dir, opts); }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            // Chromium yok ya da bu Playwright sürümüne ait değil: kur, bir kez daha dene.
            status.Report("Chromium kuruluyor (ilk kullanım, 1-2 dk)...");
            log("Chromium bulunamadı, kuruluyor.");
            await Task.Run(InstallChromium);
            return await pw.Chromium.LaunchPersistentContextAsync(dir, opts);
        }
    }

    static string FirstLine(string s) => s.Split('\n')[0].Trim();

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_cdpBrowser != null)
            {
                // Kullanıcının Chrome'u açık kalır: yalnızca bizim sekmeyi kapat, bağlantıyı kes.
                if (!_page.IsClosed) await _page.CloseAsync();
                await _cdpBrowser.CloseAsync();
            }
            else await _ctx.CloseAsync();
        }
        catch (PlaywrightException) { }
        _pw.Dispose();
    }

    // ---------- ilçe başına akış ----------

    public async Task<HouseCard> CollectAsync(County county, double? medianListPrice, bool condo, (int Min, int Max)? band,
        IProgress<string> status, CancellationToken ct)
    {
        _status = status;
        _label = county.Name;
        var card = new HouseCard { Fips = county.Fips, County = county.Name, Mode = condo ? "Daire" : "Müstakil ev", CreatedAt = DateTime.Now };

        var region = await FindRegionAsync(county, ct);
        if (region == null)
        {
            card.Note = "Redfin bölgesi bulunamadı";
            _log($"{county.Name}: {card.Note}");
            return card;
        }

        (card.MinPrice, card.MaxPrice) = band ?? PriceBand(medianListPrice);
        card.ListUrl = $"{Site}{region.Url}/filter/property-type={(condo ? "condo" : "house")},min-days-on-market=90," +
                       $"min-price={card.MinPrice / 1000}k,max-price={card.MaxPrice / 1000}k";

        status.Report($"{county.Name} — liste açılıyor");
        var homes = await LoadHomesAsync(card.ListUrl, ct);
        card.ListCount = homes.Count;
        card.Candidates = PickCandidates(homes, condo, card.MinPrice, card.MaxPrice);
        status.Report($"{county.Name} — {card.Candidates.Count} aday");
        _log($"{county.Name}: liste {homes.Count} ilan, {card.Candidates.Count} aday — {card.ListUrl}");

        foreach (var c in card.Candidates)
        {
            ct.ThrowIfCancellationRequested();
            status.Report($"{county.Name} — {c.Street} fiyat geçmişi...");
            try { ApplyHistory(c, await LoadHistoryAsync(c.Url, ct)); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (!Closed && ex is not BlockedException)     // engel: ilçeyi atla
            {
                c.Error = ex.Message;
                _log($"{county.Name}: {c.Url} fiyat geçmişi alınamadı — {ex.Message}");
            }
        }

        card.Chosen = Choose(card.Candidates);
        if (card.Chosen == null)
            card.Note = card.Candidates.Count == 0
                ? $"uygun ev bulunamadı (listede {homes.Count} ilan, kurala uyan aday yok)"
                : "uygun ev bulunamadı (adaylarda 2+ indirim ya da 1 indirim + 120 gün yok)";
        else
            card.CardText = CardText(card.Chosen);
        return card;
    }

    /// FRED medyan liste fiyatının 0,6-1,15 katı, 5.000'e yuvarlı. Medyan yoksa 200k-450k.
    public static (int Min, int Max) PriceBand(double? median)
    {
        if (median is not > 0) return (200_000, 450_000);
        static int Round5k(double v) => (int)Math.Round(v / 5000, MidpointRounding.AwayFromZero) * 5000;
        return (Round5k(median.Value * 0.6), Round5k(median.Value * 1.15));
    }

    // ---------- A2: county adresi ----------

    async Task<RedfinRegion?> FindRegionAsync(County county, CancellationToken ct)
    {
        var key = $"{county.State}-{county.Fips}";
        if (_regions.TryGetValue(key, out var cached)) return cached;

        _status?.Report($"{county.Name} — Redfin bölgesi aranıyor");
        var text = await AutocompleteAsync($"{county.Name}, {county.State}", ct);

        using var doc = ParseRedfin(text);
        if (doc == null || !doc.RootElement.TryGetProperty("payload", out var payload)) return null;

        var rows = new List<JsonElement>();
        if (payload.TryGetProperty("exactMatch", out var exact) && exact.ValueKind == JsonValueKind.Object) rows.Add(exact);
        if (payload.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
            foreach (var s in sections.EnumerateArray())
                if (s.TryGetProperty("rows", out var rs) && rs.ValueKind == JsonValueKind.Array)
                    rows.AddRange(rs.EnumerateArray());

        var target = RedfinLoader.NormalizeName(county.Name);
        RedfinRegion? loose = null;
        foreach (var row in rows)
        {
            var u = Str(row, "url") ?? "";
            var m = Regex.Match(u, @"^/county/(\d+)/([A-Z]{2})/([^/?]+)");
            if (!m.Success || m.Groups[2].Value != county.State) continue;
            var region = new RedfinRegion(county.Name, m.Groups[1].Value, m.Value);
            if (RedfinLoader.NormalizeName(Str(row, "name") ?? "") == target || RedfinLoader.NormalizeName(m.Groups[3].Value) == target)
                return Remember(key, region);
            loose ??= region;
        }
        if (loose != null) _log($"{county.Name}: ad tam eşleşmedi, eyaletteki ilk county sonucu alındı: {loose.Url}");
        return loose == null ? null : Remember(key, loose);
    }

    /// Stingray adresine sayfa olarak gitmez: redfin.com içindeyken sitenin kendi fetch'iyle çağırır.
    async Task<string> AutocompleteAsync(string query, CancellationToken ct)
    {
        const string js = "q => fetch('/stingray/do/location-autocomplete?location=' + encodeURIComponent(q) + '&v=2', { credentials: 'include' })" +
                          ".then(async r => r.status + '|' + await r.text())";
        for (int attempt = 1; ; attempt++)
        {
            if (!_page.Url.StartsWith(Site, StringComparison.OrdinalIgnoreCase))
                await OpenAsync(Site + "/", null, null, ct);
            await Task.Delay(_rnd.Next(3000, 5001), ct);

            var result = await _page.EvaluateAsync<string>(js, query);
            int bar = result.IndexOf('|');
            int.TryParse(result[..Math.Max(0, bar)], out var httpStatus);
            var body = result[(bar + 1)..];
            if (httpStatus is not (403 or 429) && !IsBlocked(null, "", body)) return body;

            var url = $"{Site}/stingray/do/location-autocomplete?location={Uri.EscapeDataString(query)}&v=2";
            _log($"{_label}: Redfin engeli (HTTP {httpStatus}, autocomplete) — {url}");
            if (attempt >= 2) throw new BlockedException($"Redfin erişimi engelledi (HTTP {httpStatus}): {url}");
            _status?.Report($"{_label} — Redfin engeli, 60 sn bekleniyor...");
            await Task.Delay(60_000, ct);
        }
    }

    RedfinRegion Remember(string key, RedfinRegion region)
    {
        _regions[key] = region;
        try { File.WriteAllText(_regionsFile, JsonSerializer.Serialize(_regions, JsonOpts)); }
        catch (IOException ex) { _log("redfin_regions.json yazılamadı: " + ex.Message); }
        return region;
    }

    static Dictionary<string, RedfinRegion> LoadRegions(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Dictionary<string, RedfinRegion>>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException) { }
        return new();
    }

    // ---------- A3-A4: liste ve adaylar ----------

    async Task<List<HouseCandidate>> LoadHomesAsync(string url, CancellationToken ct)
    {
        // Hazır: sayfanın kendi gis yanıtı geldi. Gelmezse 20 sn sonunda HTML'deki bloklara düşülür.
        var page = await OpenAsync(url, u => u.Contains("/stingray/api/gis?"), d => ReadHomes(d.Captured) != null, ct);

        // 1) sayfanın kendi gis yanıtları; 2) olmazsa HTML'e gömülü bloklar
        return ReadHomes(page.Captured) ?? ReadHomes(EmbeddedBlocks(page.Html))
            ?? throw new InvalidOperationException("ilan verisi bulunamadı (gis yanıtı yakalanmadı, sayfada gömülü veri yok)");
    }

    /// payload.homes içeren bütün gövdeleri birleştirir (url'ye göre tekil). Hiçbirinde yoksa null.
    static List<HouseCandidate>? ReadHomes(IEnumerable<string> bodies)
    {
        Dictionary<string, HouseCandidate>? all = null;
        foreach (var body in bodies)
        {
            using var doc = ParseRedfin(body);
            if (doc == null || Get(doc.RootElement, "payload", "homes") is not { ValueKind: JsonValueKind.Array } homes) continue;
            all ??= new();
            foreach (var h in homes.EnumerateArray())
                if (ReadHome(h) is { } c) all[c.Url] = c;
        }
        return all?.Values.ToList();
    }

    static HouseCandidate? ReadHome(JsonElement h)
    {
        var url = Str(h, "url");
        if (string.IsNullOrEmpty(url)) return null;
        // dom.value bazen 1 döner; timeOnRedfin (ms) esas
        var ton = Num(h, "timeOnRedfin", "value");
        return new HouseCandidate
        {
            Url = Site + url,
            Street = Str(h, "streetLine", "value") ?? "",
            City = Str(h, "city") ?? "",
            State = Str(h, "state") ?? "",
            Zip = Str(h, "zip") ?? "",
            Price = (int)(Num(h, "price", "value") ?? 0),
            DaysOnRedfin = (int)(ton.HasValue ? ton.Value / 86_400_000 : Num(h, "dom", "value") ?? 0),
            PropertyType = (int)(Num(h, "propertyType") ?? 0),
            IsNewConstruction = Get(h, "isNewConstruction") is { ValueKind: JsonValueKind.True },
            MlsStatus = Str(h, "mlsStatus") ?? "",
            YearBuilt = (int?)Num(h, "yearBuilt", "value"),
            SqFt = (int?)Num(h, "sqFt", "value"),
            Beds = Num(h, "beds"),
            Baths = Num(h, "baths"),
        };
    }

    static List<HouseCandidate> PickCandidates(IEnumerable<HouseCandidate> homes, bool condo, int min, int max)
    {
        int type = condo ? 3 : 6;
        int maxYear = DateTime.Today.Year - 2;     // müteahhit stoku elensin
        return homes
            .Where(h => h.PropertyType == type && h.DaysOnRedfin >= 90 && h.Beds is >= 2
                        && !h.IsNewConstruction && h.YearBuilt is int y && y <= maxYear
                        && h.Price >= min && h.Price <= max
                        && (h.MlsStatus.Length == 0 || h.MlsStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(h => h.DaysOnRedfin)
            .Take(5)
            .ToList();
    }

    // ---------- A5: fiyat geçmişi ve seçim ----------

    async Task<List<HistoryEvent>> LoadHistoryAsync(string url, CancellationToken ct)
    {
        // Hazır: fiyat geçmişi bir yanıtta ya da HTML'e gömülü blokta var (genelde ilk HTML'de gelir)
        var page = await OpenAsync(url, u => u.Contains("/stingray/"),
            d => ReadEvents(d.Captured) != null || ReadEvents(EmbeddedBlocks(d.Html)) != null, ct);

        // 1) sayfanın kendi yanıtları; 2) olmazsa HTML'e gömülü bloklar
        return ReadEvents(page.Captured) ?? ReadEvents(EmbeddedBlocks(page.Html))
            ?? throw new InvalidOperationException("fiyat geçmişi bulunamadı (propertyHistoryInfo yok)");
    }

    /// payload.propertyHistoryInfo.events olan ilk gövde. Hiçbirinde yoksa null.
    static List<HistoryEvent>? ReadEvents(IEnumerable<string> bodies)
    {
        foreach (var body in bodies.Where(b => b.Contains("propertyHistoryInfo")))
        {
            using var doc = ParseRedfin(body);
            if (doc == null || Get(doc.RootElement, "payload", "propertyHistoryInfo", "events") is not { ValueKind: JsonValueKind.Array } events) continue;
            var list = new List<HistoryEvent>();
            foreach (var e in events.EnumerateArray())
            {
                var ms = Num(e, "eventDate");
                if (!ms.HasValue) continue;
                // eventDate ABD'de gece yarısı (UTC 04-10); UTC tarihi aynı gün
                var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds((long)ms.Value).UtcDateTime);
                list.Add(new HistoryEvent(date, Str(e, "eventDescription") ?? "", (int?)Num(e, "price")));
            }
            return list;
        }
        return null;
    }

    static void ApplyHistory(HouseCandidate c, List<HistoryEvent> events)
    {
        var ev = events.OrderByDescending(e => e.Date).ToList();     // yeni → eski (aynı gün sırası korunur)
        int li = ev.FindIndex(e => e.Description is "Listed" or "Relisted");
        if (li < 0) { c.Error = "tarihçede Listed olayı yok"; return; }
        int saleIndex = ev.FindIndex(li + 1, e => e.Description.StartsWith("Sold", StringComparison.OrdinalIgnoreCase));

        // Fiyatsız yeniden ilan (çekilip tekrar çıkan evde Redfin fiyat yazmayabiliyor): son satıştan bu yana
        // fiyatlı en yeni Listed/Relisted başlangıç sayılır; gün sayısı Redfin'in timeOnRedfin'iyle uyuşur.
        var relist = ev[li];
        if (relist.Price == null)
        {
            int end = saleIndex < 0 ? ev.Count : saleIndex;
            int pi = ev.FindIndex(li + 1, end - li - 1, e => e.Description is "Listed" or "Relisted" && e.Price.HasValue);
            if (pi >= 0) li = pi;
        }

        var listed = ev[li];
        c.HasHistory = true;
        c.ListedDate = listed.Date;
        c.OriginalPrice = listed.Price;
        c.Days = DateOnly.FromDateTime(DateTime.Today).DayNumber - listed.Date.DayNumber;

        // Mevcut ilan içindeki fiyat değişiklikleri, eski → yeni. İndirim = bir öncekinden düşük fiyat.
        var changes = ev.Take(li).Where(e => e.Description == "Price Changed" && e.Price.HasValue).Reverse().ToList();
        c.CurrentPrice = changes.Count > 0 ? changes[^1].Price : listed.Price;
        // Fiyatsız yeniden ilandan sonra değişiklik kaydı yoksa güncel fiyat liste sayfasından
        if (relist.Price == null && !ReferenceEquals(relist, listed) && c.Price > 0 && !changes.Any(e => e.Date >= relist.Date))
            c.CurrentPrice = c.Price;
        c.Cuts = new();
        int? prev = listed.Price;
        foreach (var e in changes)
        {
            if (prev.HasValue && e.Price < prev) c.Cuts.Add(new PriceCut(e.Date, e.Price!.Value));
            prev = e.Price;
        }

        // Mevcut ilandan önceki en yeni satış; fiyatsız kayıtsa aynı satışın 90 gün içindeki fiyatlı kaydı
        var sale = saleIndex >= 0 ? ev[saleIndex] : null;
        if (sale != null)
        {
            var priced = sale.Price.HasValue ? sale : ev.Skip(saleIndex).FirstOrDefault(e =>
                e.Description.StartsWith("Sold", StringComparison.OrdinalIgnoreCase) && e.Price.HasValue
                && Math.Abs(e.Date.DayNumber - sale.Date.DayNumber) <= 90);
            c.LastSaleDate = (priced ?? sale).Date;
            c.LastSalePrice = priced?.Price;
        }

        // Son satıştan bu yana ilana çıkıp satılamadan çekilmiş mi (fiyatsız yeniden ilan öncesindeki çekilme dahil)
        var sinceSale = saleIndex < 0 ? ev : ev.Take(saleIndex);
        c.PreviouslyWithdrawn = sinceSale.Any(e =>
            e.Description.Contains("Removed", StringComparison.OrdinalIgnoreCase) ||
            e.Description.Contains("Delisted", StringComparison.OrdinalIgnoreCase) ||
            e.Description.Contains("Withdrawn", StringComparison.OrdinalIgnoreCase) ||
            e.Description.Contains("Expired", StringComparison.OrdinalIgnoreCase));
    }

    static HouseCandidate? Choose(List<HouseCandidate> candidates)
    {
        var ok = candidates.Where(c => c.HasHistory && c.OriginalPrice.HasValue && c.CurrentPrice.HasValue).ToList();
        return ok.Where(c => c.Cuts.Count >= 2).OrderByDescending(c => c.Cuts.Count).ThenByDescending(c => c.Days).FirstOrDefault()
            ?? ok.Where(c => c.Cuts.Count >= 1 && c.Days >= 120).OrderByDescending(c => c.Days).FirstOrDefault();
    }

    // ---------- A6: kart metni ----------

    /// "Cape Coral: 3 Haziran'da 439.900 $'a çıktı, 2 indirimle 399.900 $, 112 gündür satılık; sahibi Ocak 2022'de 500.000 $'a almıştı."
    public static string CardText(HouseCandidate c)
    {
        var d = c.ListedDate!.Value;
        var when = d.Year == DateTime.Today.Year
            ? $"{d.Day} {Analyzer.MonthNames[d.Month - 1]}'{MonthSuffix[d.Month - 1]}"
            : $"{d.Day} {Analyzer.MonthNames[d.Month - 1]} {d.Year}'{YearSuffix(d.Year)}";
        var sb = new StringBuilder($"{c.City}: {when} {Usd(c.OriginalPrice)} $'a çıktı, {c.Cuts.Count} indirimle {Usd(c.CurrentPrice)} $, {c.Days} gündür satılık");
        if (c.LastSaleDate is DateOnly s && c.LastSalePrice.HasValue)
            sb.Append($"; sahibi {Analyzer.MonthNames[s.Month - 1]} {s.Year}'{YearSuffix(s.Year)} {Usd(c.LastSalePrice)} $'a almıştı");
        return sb.Append('.').ToString();
    }

    public static string Usd(int? v) => v.HasValue ? v.Value.ToString("N0", Inv).Replace(',', '.') : "?";

    /// Bulunma eki: Ocak'ta, Haziran'da, Eylül'de...
    static readonly string[] MonthSuffix = { "ta", "ta", "ta", "da", "ta", "da", "da", "ta", "de", "de", "da", "ta" };

    /// Yılın okunuşuna göre bulunma eki: 2022'de, 2023'te, 2026'da, 2010'da, 2000'de.
    static string YearSuffix(int y)
    {
        string[] ones = { "", "de", "de", "te", "te", "te", "da", "de", "de", "da" };   // bir iki üç dört beş altı yedi sekiz dokuz
        string[] tens = { "", "da", "de", "da", "ta", "de", "ta", "te", "de", "da" };   // on yirmi otuz kırk elli altmış yetmiş seksen doksan
        if (y % 10 != 0) return ones[y % 10];
        if (y / 10 % 10 != 0) return tens[y / 10 % 10];
        return "de";                                                                  // yüz, bin
    }

    // ---------- A6: dosyalar ----------

    public static Dictionary<string, HouseCard> LoadListings(string outDir, string state)
    {
        var dict = new Dictionary<string, HouseCard>();
        var path = Path.Combine(outDir, $"listings_{state}.json");
        try
        {
            if (File.Exists(path))
                foreach (var c in JsonSerializer.Deserialize<List<HouseCard>>(File.ReadAllText(path)) ?? new())
                    dict[c.Fips] = c;
        }
        catch (Exception ex) when (ex is IOException or JsonException) { }
        return dict;
    }

    public static void SaveListings(string outDir, string state, IEnumerable<HouseCard> cards)
    {
        Directory.CreateDirectory(outDir);
        var list = cards.OrderBy(c => c.County).ToList();
        File.WriteAllText(Path.Combine(outDir, $"listings_{state}.json"), JsonSerializer.Serialize(list, JsonOpts), Encoding.UTF8);

        using var w = new StreamWriter(Path.Combine(outDir, $"listings_{state}.csv"), false, Encoding.UTF8);
        w.WriteLine("fips,county,city,street,url,yearBuilt,sqft,beds,listedDate,originalPrice,currentPrice,cutCount,totalCut,days,lastSaleDate,lastSalePrice,previouslyWithdrawn,cardText");
        foreach (var card in list)
        {
            if (card.Chosen is not { } h) continue;
            w.WriteLine(string.Join(",", card.Fips, Q(card.County), Q(h.City), Q(h.Street), h.Url, h.YearBuilt, h.SqFt,
                h.Beds?.ToString("0.#", Inv), h.ListedDate?.ToString("yyyy-MM-dd", Inv), h.OriginalPrice, h.CurrentPrice,
                h.Cuts.Count, h.TotalCut, h.Days, h.LastSaleDate?.ToString("yyyy-MM-dd", Inv), h.LastSalePrice,
                h.PreviouslyWithdrawn ? "true" : "false", Q(card.CardText)));
        }
    }

    static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

    // ---------- sayfa açma ----------

    /// Sayfayı açar (DOMContentLoaded); capture'a uyan yanıtların gövdelerini toplar ve ready sağlanana kadar en çok DataWaitMs bekler.
    /// Redfin arka planda hiç susmadığı için NetworkIdle beklenmez (her sayfada 45 sn boşa giderdi).
    /// Engel sayfasında 60 sn bekleyip bir kez daha dener, sonra BlockedException.
    async Task<PageData> OpenAsync(string url, Func<string, bool>? capture, Func<PageData, bool>? ready, CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            if (Closed) throw new InvalidOperationException("Tarayıcı penceresi kapatıldı.");
            if (_navigated) await Task.Delay(_rnd.Next(3000, 5001), ct);    // sayfalar arası 3-5 sn
            _navigated = true;
            if (_page.IsClosed) _page = await _ctx.NewPageAsync();

            var pending = new List<Task<string?>>();
            void OnResponse(object? sender, IResponse r)
            {
                if (capture != null && capture(r.Url)) lock (pending) pending.Add(BodyOrNull(r));
            }

            _page.Response += OnResponse;
            try
            {
                IResponse? main = null;
                try { main = await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = TimeoutMs }); }
                catch (TimeoutException) { _log($"{_label}: {TimeoutMs / 1000} sn zaman aşımı, sayfa yüklendiği kadarıyla okunuyor — {url}"); }

                var html = await SafeAsync(() => _page.ContentAsync());
                var title = await SafeAsync(() => _page.TitleAsync());
                if (IsBlocked(main?.Status, title, html))
                {
                    _log($"{_label}: Redfin engeli (HTTP {main?.Status}, \"{title}\") — {url}");
                    if (attempt >= 2) throw new BlockedException($"Redfin erişimi engelledi (HTTP {main?.Status}): {url}");
                    _status?.Report($"{_label} — Redfin engeli, 60 sn bekleniyor...");
                    await Task.Delay(60_000, ct);
                    continue;
                }

                // Aranan veri (yakalanan yanıt ya da HTML'deki blok) gelene kadar saniyede bir yokla
                var data = Snapshot(html, pending);
                var deadline = DateTime.UtcNow.AddMilliseconds(DataWaitMs);
                while (ready != null && !ready(data) && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(1000, ct);
                    data = Snapshot(await SafeAsync(() => _page.ContentAsync()), pending);
                }
                if (ready != null && !ready(data))
                    _log($"{_label}: {DataWaitMs / 1000} sn içinde beklenen veri gelmedi, eldekiyle devam — {url}");
                return data;
            }
            finally { _page.Response -= OnResponse; }
        }
    }

    const int DataWaitMs = 20_000;

    /// O ana kadar gövdesi okunmuş yanıtlar + sayfanın HTML'i.
    static PageData Snapshot(string html, List<Task<string?>> pending)
    {
        lock (pending)
            return new PageData(html, pending.Where(t => t.IsCompletedSuccessfully).Select(t => t.Result).OfType<string>().ToList());
    }

    static async Task<string?> BodyOrNull(IResponse r)
    {
        try { return await r.TextAsync().WaitAsync(TimeSpan.FromSeconds(20)); }
        catch (Exception) { return null; }   // yönlendirme, iptal edilmiş istek, gövdesiz yanıt
    }

    static async Task<string> SafeAsync(Func<Task<string>> f)
    {
        try { return await f(); }
        catch (PlaywrightException) { return ""; }
    }

    static bool IsBlocked(int? status, string title, string html) =>
        status is 403 or 429
        || title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
        || title.Contains("could not be satisfied", StringComparison.OrdinalIgnoreCase)
        || (html.Length < 20_000 && (html.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
                                     || html.Contains("Request blocked", StringComparison.OrdinalIgnoreCase)));

    // ---------- Redfin JSON ----------

    /// "{}&&{...}" ön ekini keser. Geçersizse null.
    static JsonDocument? ParseRedfin(string text)
    {
        var t = text.TrimStart();
        if (t.StartsWith("{}&&", StringComparison.Ordinal)) t = t[4..];
        try { return JsonDocument.Parse(t); }
        catch (JsonException) { return null; }
    }

    /// HTML'deki "text":"{}&&{...}" JSON-string bloklarını çözülmüş metin olarak verir (\" ve \\u002F kaçışları açılır).
    static IEnumerable<string> EmbeddedBlocks(string html)
    {
        const string marker = "\"text\":\"{}&&";
        int i = html.IndexOf(marker, StringComparison.Ordinal);
        while (i >= 0)
        {
            int start = i + 7;                                  // değerin açılış tırnağı
            int j = start + 1;
            while (j < html.Length && html[j] != '"') j += html[j] == '\\' ? 2 : 1;
            if (j >= html.Length) yield break;

            string? text = null;
            try { text = JsonSerializer.Deserialize<string>(html.Substring(start, j - start + 1)); }
            catch (JsonException) { }
            if (text != null) yield return text;
            i = html.IndexOf(marker, j + 1, StringComparison.Ordinal);
        }
    }

    static JsonElement? Get(JsonElement e, params string[] path)
    {
        var cur = e;
        foreach (var p in path)
        {
            if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(p, out var next)) return null;
            cur = next;
        }
        return cur;
    }

    static double? Num(JsonElement e, params string[] path) =>
        Get(e, path) is { ValueKind: JsonValueKind.Number } v ? v.GetDouble() : null;

    static string? Str(JsonElement e, params string[] path) =>
        Get(e, path) is { ValueKind: JsonValueKind.String } v ? v.GetString() : null;
}
