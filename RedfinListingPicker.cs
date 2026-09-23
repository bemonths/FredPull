using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace FredPull;

/// Redfin'den her ilçe için "aylardır satılamayan tek bir ev" seçer (Playwright, görünür Chromium, kalıcı profil).
/// Akış: county adresi (autocomplete, önbellekli) → filtreli liste (≥90 gün) → en eski 5 aday → ilan sayfasında fiyat geçmişi → seçim.
public sealed class RedfinListingPicker : IAsyncDisposable
{
    const string Site = "https://www.redfin.com";
    const int TimeoutMs = 45_000;

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public sealed record RedfinRegion(string County, string RegionId, string Url);
    sealed record HistoryEvent(DateOnly Date, string Description, int? Price);
    sealed record PageData(string Html, List<string> Captured, string? MainBody);

    readonly IPlaywright _pw;
    readonly IBrowserContext _ctx;
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

    RedfinListingPicker(IPlaywright pw, IBrowserContext ctx, IPage page, string outDir, Action<string> log)
    {
        _pw = pw; _ctx = ctx; _page = page; _log = log;
        _regionsFile = Path.Combine(outDir, "redfin_regions.json");
        _regions = LoadRegions(_regionsFile);
        _ctx.Close += (_, _) => Closed = true;
    }

    // ---------- tarayıcı ----------

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

    public static async Task<RedfinListingPicker> StartAsync(string baseDir, string outDir, Action<string> log, IProgress<string> status)
    {
        var pw = await Playwright.CreateAsync();
        try
        {
            var userDir = Path.Combine(baseDir, "pw-profile");
            var opts = new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            };
            IBrowserContext ctx;
            try { ctx = await pw.Chromium.LaunchPersistentContextAsync(userDir, opts); }
            catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
            {
                // Chromium yok ya da bu Playwright sürümüne ait değil: kur, bir kez daha dene.
                status.Report("Chromium kuruluyor (ilk kullanım, 1-2 dk)...");
                log("Chromium bulunamadı, kuruluyor.");
                await Task.Run(InstallChromium);
                ctx = await pw.Chromium.LaunchPersistentContextAsync(userDir, opts);
            }
            ctx.SetDefaultTimeout(TimeoutMs);
            ctx.SetDefaultNavigationTimeout(TimeoutMs);
            var page = ctx.Pages.FirstOrDefault() ?? await ctx.NewPageAsync();
            return new RedfinListingPicker(pw, ctx, page, outDir, log);
        }
        catch
        {
            pw.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { await _ctx.CloseAsync(); } catch (PlaywrightException) { }
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
            catch (Exception ex) when (!Closed)
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
        var url = $"{Site}/stingray/do/location-autocomplete?location={Uri.EscapeDataString($"{county.Name}, {county.State}")}&v=2";
        var page = await OpenAsync(url, WaitUntilState.Load, null, ct);
        var text = page.MainBody ?? await _page.InnerTextAsync("body");

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
        var page = await OpenAsync(url, WaitUntilState.NetworkIdle, u => u.Contains("/stingray/api/gis?"), ct);

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
        var page = await OpenAsync(url, WaitUntilState.NetworkIdle, u => u.Contains("/stingray/"), ct);

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

        var listed = ev[li];
        c.HasHistory = true;
        c.ListedDate = listed.Date;
        c.OriginalPrice = listed.Price;
        c.Days = DateOnly.FromDateTime(DateTime.Today).DayNumber - listed.Date.DayNumber;

        // Mevcut ilan içindeki fiyat değişiklikleri, eski → yeni. İndirim = bir öncekinden düşük fiyat.
        var changes = ev.Take(li).Where(e => e.Description == "Price Changed" && e.Price.HasValue).Reverse().ToList();
        c.CurrentPrice = changes.Count > 0 ? changes[^1].Price : listed.Price;
        c.Cuts = new();
        int? prev = listed.Price;
        foreach (var e in changes)
        {
            if (prev.HasValue && e.Price < prev) c.Cuts.Add(new PriceCut(e.Date, e.Price!.Value));
            prev = e.Price;
        }

        // Mevcut ilandan önceki en yeni satış; fiyatsız kayıtsa aynı satışın 90 gün içindeki fiyatlı kaydı
        var older = ev.Skip(li + 1).ToList();
        var sale = older.FirstOrDefault(e => e.Description.StartsWith("Sold", StringComparison.OrdinalIgnoreCase));
        if (sale != null)
        {
            var priced = sale.Price.HasValue ? sale : older.FirstOrDefault(e =>
                e.Description.StartsWith("Sold", StringComparison.OrdinalIgnoreCase) && e.Price.HasValue
                && Math.Abs(e.Date.DayNumber - sale.Date.DayNumber) <= 90);
            c.LastSaleDate = (priced ?? sale).Date;
            c.LastSalePrice = priced?.Price;
        }

        // Son satıştan bu yana ilana çıkıp satılamadan çekilmiş mi
        var sinceSale = sale == null ? older : older.TakeWhile(e => !ReferenceEquals(e, sale));
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

    /// Sayfayı açar; capture'a uyan yanıtların gövdelerini toplar. Engel sayfasında 60 sn bekleyip bir kez daha dener.
    async Task<PageData> OpenAsync(string url, WaitUntilState waitUntil, Func<string, bool>? capture, CancellationToken ct)
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

            IResponse? main = null;
            _page.Response += OnResponse;
            try { main = await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = waitUntil, Timeout = TimeoutMs }); }
            catch (TimeoutException)
            {
                // Redfin'de arka plan istekleri bitmeyebilir; yüklenen kadarıyla devam
                _log($"{_label}: {TimeoutMs / 1000} sn zaman aşımı, sayfa yüklendiği kadarıyla okunuyor — {url}");
            }
            finally { _page.Response -= OnResponse; }

            var html = await SafeAsync(() => _page.ContentAsync());
            var title = await SafeAsync(() => _page.TitleAsync());
            if (IsBlocked(main?.Status, title, html))
            {
                _log($"{_label}: Redfin engeli (HTTP {main?.Status}, \"{title}\") — {url}");
                if (attempt >= 2) throw new InvalidOperationException("Redfin erişimi engelledi (Access Denied)");
                _status?.Report($"{_label} — Redfin engeli, 60 sn bekleniyor...");
                await Task.Delay(60_000, ct);
                continue;
            }

            Task<string?>[] tasks;
            lock (pending) tasks = pending.ToArray();
            var bodies = (await Task.WhenAll(tasks)).OfType<string>().ToList();
            var mainBody = capture == null && main != null ? await BodyOrNull(main) : null;
            return new PageData(html, bodies, mainBody);
        }
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
