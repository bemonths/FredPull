using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace FredPull;

/// Ham ek veri katmanı ("Ek verileri çek"). İlke: FredPull düşünmez, yorumlamaz, hesap yapmaz; resmî kaynaklardan veriyi
/// indirir ve olduğu gibi, kaynağı ve tarihiyle kaydeder. Karşılaştırma, sıralama, uzaklık, eşleştirme ve yorum yapay zeka
/// projelerinde yapılır. Çıktılar out\ek_{ST}\ altında düz CSV'ler (UTF-8, BOM'suz) ve manifest.json.
public sealed class ExtraData : IDisposable
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
    public const string Principle = "FredPull ham veri indirir; hesap, karşılaştırma ve yorum yapmaz.";

    readonly HttpClient _http;
    readonly Action<string> _log;

    public ExtraData(Action<string> log)
    {
        _log = log;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        // tanımlayıcı User-Agent (OpenStreetMap Nominatim'in kullanım kuralı)
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("FredPull/1.0 (+https://github.com/bemonths/FredPull; county housing raw-data downloader)");
    }

    public void Dispose() => _http.Dispose();

    /// Bir çıktının manifest kaydı: kaynak, çekiliş zamanı, veri dönemi, satır sayısı, uyarılar.
    public sealed class Entry
    {
        public string File { get; set; } = "";
        public string Source { get; set; } = "";
        public DateTime FetchedAt { get; set; } = DateTime.Now;
        public string Period { get; set; } = "";
        public int Rows { get; set; }
        public List<string> Warnings { get; set; } = new();
        public string? Error { get; set; }
        public Dictionary<string, string> Details { get; set; } = new();
    }

    // ---------- ortak ----------

    static string Csv(string? s) => s == null ? "" : s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    static void WriteCsv(string path, IEnumerable<string> header, IEnumerable<IEnumerable<string?>> rows)
    {
        var sb = new StringBuilder(string.Join(",", header.Select(Csv))).Append("\r\n");
        foreach (var r in rows) sb.Append(string.Join(",", r.Select(Csv))).Append("\r\n");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    async Task<(int Code, string Body)> GetAsync(string url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync(ct));
    }

    static string Raw(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString() ?? "",
        JsonValueKind.Null or JsonValueKind.Undefined => "",
        _ => v.GetRawText(),
    };

    // ---------- 2. Census ACS 5 yıllık ----------

    /// Ödenen medyan emlak vergisi, medyan ev değeri, toplam nüfus, 65 yaş ve üstü (erkek 020–025, kadın 044–049).
    public static readonly string[] AcsVars =
        new[] { "B25103_001E", "B25077_001E", "B01001_001E" }
        .Concat(Enumerable.Range(20, 6).Select(i => $"B01001_{i:000}E"))
        .Concat(Enumerable.Range(44, 6).Select(i => $"B01001_{i:000}E")).ToArray();

    public const string CensusKeySignup = "https://api.census.gov/data/key_signup.html";

    static bool KeyProblem(string body) =>
        !body.TrimStart().StartsWith('[') && (body.Contains("Missing Key", StringComparison.OrdinalIgnoreCase)
                                             || body.Contains("Invalid Key", StringComparison.OrdinalIgnoreCase)
                                             || body.Contains("key_signup", StringComparison.OrdinalIgnoreCase));

    /// En yeni ACS 5 yıllık sürümü (geçen yıldan geriye 4 yıl denenir); county'ler, eyalet ve ABD ayrı satırlar. Değerler
    /// Census'un döndürdüğü gibi (özel negatif "veri yok" kodları dahil).
    public async Task<Entry> AcsAsync(string st, string stateFips, string? key, string dir, CancellationToken ct)
    {
        var e = new Entry { File = $"acs_{st}.csv" };
        var get = "NAME," + string.Join(",", AcsVars);
        var k = string.IsNullOrWhiteSpace(key) ? "" : "&key=" + Uri.EscapeDataString(key.Trim());
        int year = 0;
        for (int y = DateTime.Now.Year - 1; y >= DateTime.Now.Year - 4 && year == 0; y--)
        {
            var (code, body) = await GetAsync($"https://api.census.gov/data/{y}/acs/acs5?get={get}&for=us:1{k}", ct);
            if (code == 200 && body.TrimStart().StartsWith('[')) year = y;
            else if (KeyProblem(body))
                throw new InvalidOperationException(k.Length == 0
                    ? $"Census Data API anahtarsız isteği kabul etmiyor (\"Missing Key\"). Ücretsiz anahtar: {CensusKeySignup} — üst çubuktaki \"Census anahtarı\" kutusuna yaz."
                    : $"Census Data API anahtarı reddetti. Anahtarı kontrol et ({CensusKeySignup}).");
            else _log($"ACS {y}: yok (HTTP {code}), bir önceki yıl deneniyor");
        }
        if (year == 0) throw new InvalidOperationException("ACS 5 yıllık verisi son dört yılda bulunamadı.");

        var rows = new List<List<string?>>();
        foreach (var (geoType, forPart) in new[] { ("us", "for=us:1"), ("state", $"for=state:{stateFips}"), ("county", $"for=county:*&in=state:{stateFips}") })
        {
            var (code, body) = await GetAsync($"https://api.census.gov/data/{year}/acs/acs5?get={get}&{forPart}{k}", ct);
            if (code != 200 || !body.TrimStart().StartsWith('[')) throw new InvalidOperationException($"ACS {geoType}: HTTP {code}");
            using var doc = JsonDocument.Parse(body);
            var all = doc.RootElement.EnumerateArray().ToList();
            var head = all[0].EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            int iState = head.IndexOf("state"), iCounty = head.IndexOf("county"), iName = head.IndexOf("NAME");
            foreach (var r in all.Skip(1))
            {
                var c = r.EnumerateArray().Select(Raw).ToList();
                var fips = geoType switch { "us" => "US", "state" => c[iState], _ => c[iState] + c[iCounty] };
                rows.Add(new List<string?> { geoType, fips, c[iName] }.Concat(AcsVars.Select(v => (string?)c[head.IndexOf(v)])).Append(year.ToString(Inv)).ToList());
            }
        }
        rows = rows.OrderBy(r => r[0] == "us" ? 0 : r[0] == "state" ? 1 : 2).ThenBy(r => r[1], StringComparer.Ordinal).ToList();
        WriteCsv(Path.Combine(dir, e.File), new[] { "geo_type", "fips", "name" }.Concat(AcsVars).Append("acs_year"), rows);
        e.Source = $"https://api.census.gov/data/{year}/acs/acs5";
        e.Period = $"ACS 5 yıllık, {year - 4}–{year}";
        e.Rows = rows.Count;
        e.Details["değişkenler"] = string.Join(", ", AcsVars);
        _log($"ACS {year}: {rows.Count} satır ({rows.Count(r => r[0] == "county")} county + eyalet + ABD)");
        return e;
    }

    // ---------- 3. İnşaat izinleri (FRED BPPRIV0 + FIPS) ----------

    public async Task<Entry> PermitsAsync(FredClient fred, string st, List<County> counties, string dir, IProgress<string> status, CancellationToken ct)
    {
        var e = new Entry { File = $"izinler_{st}.csv", Source = "FRED, BPPRIV0{FIPS} (yıllık, özel konut izinleri): https://fred.stlouisfed.org/series/BPPRIV0" };
        int done = 0;
        var results = await Task.WhenAll(counties.Select(async c =>
        {
            var obs = await fred.GetObservationsAsync("BPPRIV0" + c.Fips, new DateOnly(1980, 1, 1), ct);
            status.Report($"İnşaat izinleri: {Interlocked.Increment(ref done)}/{counties.Count}");
            return (County: c, Obs: obs);
        }));
        var missing = results.Where(r => r.Obs == null).Select(r => r.County.Name).ToList();
        foreach (var m in missing) _log($"izinler: {m} için BPPRIV0 serisi yok, atlandı");
        if (missing.Count > 0) e.Warnings.Add($"{missing.Count} county'nin serisi yok (atlandı): {string.Join(", ", missing)}");
        var rows = results.Where(r => r.Obs != null)
            .SelectMany(r => r.Obs!.Where(o => o.Value.HasValue).Select(o => new List<string?>
                { r.County.Fips, r.County.Name, o.Date.Year.ToString(Inv), o.Value!.Value.ToString("0.####", Inv) }))
            .OrderBy(r => r[0], StringComparer.Ordinal).ThenBy(r => r[2], StringComparer.Ordinal).ToList();
        WriteCsv(Path.Combine(dir, e.File), new[] { "fips", "name", "year", "units" }, rows);
        e.Rows = rows.Count;
        if (rows.Count > 0) e.Period = $"{rows.Min(r => r[2])}–{rows.Max(r => r[2])}";
        _log($"izinler: {rows.Count} satır, {counties.Count - missing.Count} county");
        return e;
    }

    // ---------- 4. Hastaneler (CMS) ve koordinatları (Census Geocoder) ----------

    const string CmsQuery = "https://data.cms.gov/provider-data/api/1/datastore/query/xubh-q36u/0";
    const string CmsMeta = "https://data.cms.gov/provider-data/api/1/metastore/schemas/dataset/items/xubh-q36u";
    const int CmsPage = 1500;   // API sınırı 1500'ün üstünü "limit" doğrulamasıyla reddediyor (2026-09 denendi)

    public async Task<Entry> HospitalsAsync(string st, List<string> states, string dir, IProgress<string> status, CancellationToken ct)
    {
        var e = new Entry { File = $"hastaneler_{st}.csv", Source = $"CMS Provider Data API, Hospital General Information (xubh-q36u): {CmsQuery}" };
        var columns = new List<string>();
        var rows = new List<Dictionary<string, string>>();
        foreach (var s in states)
        {
            int before = rows.Count;
            for (int offset = 0; ; offset += CmsPage)
            {
                status.Report($"Hastaneler: {s} ({rows.Count - before})");
                var q = $"{Uri.EscapeDataString("conditions[0][property]")}=state&{Uri.EscapeDataString("conditions[0][value]")}={s}" +
                        $"&{Uri.EscapeDataString("conditions[0][operator]")}=%3D&limit={CmsPage}&offset={offset}&count=true&results=true&schema=false";
                var (code, body) = await GetAsync($"{CmsQuery}?{q}", ct);
                if (code != 200) throw new InvalidOperationException($"CMS {s}: HTTP {code} — {Short(body)}");
                using var doc = JsonDocument.Parse(body);
                var results = doc.RootElement.GetProperty("results");
                foreach (var r in results.EnumerateArray())
                {
                    var row = new Dictionary<string, string>();
                    foreach (var p in r.EnumerateObject())
                    {
                        if (!columns.Contains(p.Name)) columns.Add(p.Name);
                        row[p.Name] = Raw(p.Value);
                    }
                    rows.Add(row);
                }
                int count = doc.RootElement.TryGetProperty("count", out var cnt) && cnt.ValueKind == JsonValueKind.Number ? cnt.GetInt32() : -1;
                if (results.GetArrayLength() < CmsPage || (count >= 0 && offset + CmsPage >= count)) break;
            }
            _log($"hastaneler: {s} {rows.Count - before} satır");
            e.Details[$"satır_{s}"] = (rows.Count - before).ToString(Inv);
        }
        try
        {
            var (code, body) = await GetAsync(CmsMeta, ct);
            if (code == 200)
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var f in new[] { "modified", "released", "issued" })
                    if (doc.RootElement.TryGetProperty(f, out var v) && v.ValueKind == JsonValueKind.String) e.Details["cms_" + f] = v.GetString()!;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { e.Warnings.Add("CMS güncelleme tarihi okunamadı: " + ex.Message); }

        // Koordinat üç adımda; her satıra kaynağı yazılır (coord_source): census → census_retry (sadeleştirilmiş adres) → osm
        status.Report($"Hastaneler: {rows.Count} adres koordinata çevriliyor (Census Geocoder)");
        string V(int i, string col) => rows[i].GetValueOrDefault(col, "");
        var coord = new (string Lat, string Lng, string Match, string Source)[rows.Count];
        var geo = await GeocodeAsync(rows.Select((r, i) => (i.ToString(Inv), V(i, "address"), V(i, "citytown"), V(i, "state"), V(i, "zip_code"))).ToList(), ct);
        for (int i = 0; i < rows.Count; i++)
            coord[i] = geo.TryGetValue(i.ToString(Inv), out var g) ? (g.Lat, g.Lng, g.Match, g.Lat.Length > 0 ? "census" : "") : ("", "", "yanıt yok", "");
        int census = coord.Count(c => c.Source == "census");

        var retry = Enumerable.Range(0, rows.Count).Where(i => coord[i].Lat.Length == 0).ToList();
        status.Report($"Hastaneler: {retry.Count} adres sadeleştirilip yeniden deneniyor");
        var geo2 = await GeocodeAsync(retry.Select(i => (i.ToString(Inv), SimplifyAddress(V(i, "address")), V(i, "citytown"), V(i, "state"), V(i, "zip_code"))).ToList(), ct);
        foreach (var i in retry)
            if (geo2.TryGetValue(i.ToString(Inv), out var g) && g.Lat.Length > 0) coord[i] = (g.Lat, g.Lng, g.Match, "census_retry");
        int censusRetry = coord.Count(c => c.Source == "census_retry");

        var osmTodo = Enumerable.Range(0, rows.Count).Where(i => coord[i].Lat.Length == 0).ToList();
        int n = 0;
        foreach (var i in osmTodo)
        {
            status.Report($"Hastaneler: OpenStreetMap {++n}/{osmTodo.Count} — {V(i, "facility_name")}");
            if (await NominatimHospitalAsync(V(i, "facility_name"), V(i, "citytown"), V(i, "state"), ct) is { } o)
                coord[i] = (o.Lat.ToString(Inv), o.Lng.ToString(Inv), coord[i].Match, "osm");
        }
        int osm = coord.Count(c => c.Source == "osm");
        int matched = census + censusRetry + osm;
        var rate = rows.Count == 0 ? 0 : 100.0 * matched / rows.Count;
        _log($"hastaneler: koordinat {matched}/{rows.Count} (%{rate:0.0}) — census {census}, census_retry {censusRetry}, osm {osm}");
        foreach (var s in states)
        {
            var missing = Enumerable.Range(0, rows.Count).Where(i => V(i, "state") == s && coord[i].Lat.Length == 0).Select(i => V(i, "facility_name")).ToList();
            _log($"hastaneler: {s} koordinatsız {missing.Count}{(missing.Count > 0 ? ": " + string.Join(", ", missing) : "")}");
            e.Details[$"koordinatsız_{s}"] = missing.Count.ToString(Inv);
        }
        e.Details["koordinat_eşleşmesi"] = $"{matched}/{rows.Count}";
        e.Details["koordinat_kaynakları"] = $"census {census}, census_retry {censusRetry}, osm {osm}";
        e.Details["koordinat_servisleri"] = $"census / census_retry: {GeocoderBatch}; osm: {Nominatim}";

        WriteCsv(Path.Combine(dir, e.File), columns.Concat(new[] { "lat", "lng", "geocode_match", "coord_source" }),
            rows.Select((r, i) => columns.Select(c => (string?)r.GetValueOrDefault(c, ""))
                .Concat(new[] { coord[i].Lat, coord[i].Lng, coord[i].Match, coord[i].Source })));
        e.Rows = rows.Count;
        e.Period = e.Details.TryGetValue("cms_modified", out var mod) ? $"CMS veri kümesi güncellemesi {mod}" : "";
        e.Details["eyaletler"] = string.Join(", ", states);
        return e;
    }

    static string Short(string s) => s.Length > 200 ? s[..200] + "…" : s;

    static readonly System.Text.RegularExpressions.Regex PoBox = new(@"[,\s]+(?:P\.?\s*O\.?\s*BOX|POST OFFICE BOX|BOX)\s+\d[\w-]*.*$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    static readonly System.Text.RegularExpressions.Regex UnitPart = new(@"[,\s]+(?:(?:SUITE|STE|BLDG|BUILDING|FLOOR|FLR|UNIT|ROOM|RM|APT)\b|#)\s*[\w-]+",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    /// Census'un eşleştiremediği adres için ikinci deneme: posta kutusu, bina/süit/kat/oda bilgileri ve virgüller çıkarılır
    /// ("707 OLD DALTON ELLIJAY ROAD, PO BOX 1406" → "707 OLD DALTON ELLIJAY ROAD", "1364 CLIFTON ROAD, NE" → "1364 CLIFTON ROAD NE").
    public static string SimplifyAddress(string street)
    {
        var s = PoBox.Replace(street, "");
        s = UnitPart.Replace(s, "");
        return System.Text.RegularExpressions.Regex.Replace(s.Replace(",", " "), @"\s+", " ").Trim();
    }

    // ---------- Census Geocoder (toplu adres) ----------

    public const string GeocoderBatch = "https://geocoding.geo.census.gov/geocoder/locations/addressbatch";
    const int GeocodeChunk = 1000;   // servis dosya başına 10.000 adrese izin veriyor; yanıt süresi için 1.000'lik parçalar

    /// Adres → (Lat, Lng, Match). Match servisin yazdığı gibi: "Match/Exact", "Match/Non_Exact", "No_Match", "Tie".
    /// Eşleşmeyen adreste koordinat boş.
    public async Task<Dictionary<string, (string Lat, string Lng, string Match)>> GeocodeAsync(
        List<(string Id, string Street, string City, string State, string Zip)> rows, CancellationToken ct)
    {
        var result = new Dictionary<string, (string, string, string)>();
        for (int i = 0; i < rows.Count; i += GeocodeChunk)
        {
            var chunk = rows.Skip(i).Take(GeocodeChunk).ToList();
            var csv = string.Join("\n", chunk.Select(r => string.Join(",", new[] { r.Id, r.Street, r.City, r.State, r.Zip }.Select(x => Csv(x.Replace("\n", " "))))));
            using var form = new MultipartFormDataContent
            {
                { new StringContent("Public_AR_Current"), "benchmark" },
                { new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "addressFile", "adresler.csv" },
            };
            string body = "";
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var resp = await _http.PostAsync(GeocoderBatch, form, ct);
                    body = await resp.Content.ReadAsStringAsync(ct);
                    if (resp.IsSuccessStatusCode) break;
                    _log($"geocoder: HTTP {(int)resp.StatusCode}, {attempt}. deneme");
                }
                catch (HttpRequestException ex) { _log($"geocoder: {ex.Message}, {attempt}. deneme"); }
                await Task.Delay(3000 * attempt, ct);
            }
            foreach (var line in body.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
            {
                var c = StudioExport.SplitCsv(line);
                if (c.Count < 3) continue;
                string status = c[2], type = c.Count > 3 ? c[3] : "";
                string lat = "", lng = "";
                if (c.Count > 5 && c[5].Split(',') is { Length: 2 } xy) (lng, lat) = (xy[0], xy[1]);   // servis "boylam,enlem" yazıyor
                result[c[0]] = (lat, lng, type.Length > 0 && status == "Match" ? $"{status}/{type}" : status);
            }
        }
        return result;
    }

    const string Nominatim = "https://nominatim.openstreetmap.org/search";
    DateTime _lastNominatim = DateTime.MinValue;

    /// OpenStreetMap Nominatim, tek sorgu: ilk 5 sonuçtan koşulu sağlayan ilki (addressdetails ile; sıralama değişebildiği için
    /// yalnız ilk sonuca bakılmaz). Kullanım kuralı: saniyede en çok bir istek ve uygulamayı tanıtan User-Agent (HttpClient'ın
    /// UserAgent'ı); bu yüzden yalnızca Census'un bulamadıkları için kullanılır.
    async Task<JsonElement?> NominatimFirstAsync(string query, Func<JsonElement, bool> accept, CancellationToken ct)
    {
        var wait = _lastNominatim.AddMilliseconds(1100) - DateTime.Now;
        if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
        _lastNominatim = DateTime.Now;
        try
        {
            var (code, body) = await GetAsync($"{Nominatim}?format=jsonv2&countrycodes=us&limit=5&addressdetails=1&q={Uri.EscapeDataString(query)}", ct);
            if (code != 200) { _log($"nominatim: HTTP {code} ({query})"); return null; }
            using var doc = JsonDocument.Parse(body);
            foreach (var r in doc.RootElement.EnumerateArray())
                if (accept(r)) return r.Clone();
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            _log($"nominatim: {ex.Message} ({query})");
            return null;
        }
    }

    static (double Lat, double Lng)? LatLon(JsonElement r) =>
        r.TryGetProperty("lat", out var a) && r.TryGetProperty("lon", out var b)
        && double.TryParse(a.GetString(), NumberStyles.Float, Inv, out var la) && double.TryParse(b.GetString(), NumberStyles.Float, Inv, out var lo)
            ? (la, lo) : null;

    static string? Str(JsonElement e, params string[] path)
    {
        foreach (var k in path) if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(k, out e)) return null;
        return e.ValueKind == JsonValueKind.String ? e.GetString() : null;
    }

    /// Ev adresi. place_rank 30 = bina/adres noktası; daha kaba sonuç (sokak, mahalle) kabul edilmez.
    public async Task<(double Lat, double Lng)?> NominatimAsync(string street, string city, string state, string zip, CancellationToken ct)
    {
        var r = await NominatimFirstAsync($"{street}, {city}, {state}",
            x => x.TryGetProperty("place_rank", out var rank) && rank.GetInt32() >= 30 && Str(x, "address", "ISO3166-2-lvl4") == "US-" + state, ct);
        return r is { } found ? LatLon(found) : null;
    }

    /// Hastane adı + şehir + eyalet. Yalnızca sağlık tesisi (amenity=hospital/clinic, healthcare=*, building=hospital) ve aynı
    /// eyaletteki sonuç kabul edilir.
    public async Task<(double Lat, double Lng)?> NominatimHospitalAsync(string name, string city, string state, CancellationToken ct)
    {
        var r = await NominatimFirstAsync($"{name}, {city}, {state}", x =>
        {
            var (cat, type) = (Str(x, "category") ?? "", Str(x, "type") ?? "");
            bool health = cat == "healthcare" || (cat == "amenity" && type is "hospital" or "clinic") || (cat == "building" && type == "hospital");
            return health && Str(x, "address", "ISO3166-2-lvl4") == "US-" + state;
        }, ct);
        return r is { } found ? LatLon(found) : null;
    }

    // ---------- 5. Havalimanları (OurAirports) ----------

    const string AirportsUrl = "https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv";
    static readonly string[] AirportCols = { "ident", "iata_code", "name", "municipality", "iso_region", "type", "latitude_deg", "longitude_deg" };

    /// ABD'nin tamamında large/medium, tarifeli seferli havalimanları (sınır county'lerinde en yakını komşu eyalette olabilir).
    public async Task<Entry> AirportsAsync(string st, string dir, CancellationToken ct)
    {
        var e = new Entry { File = "havalimanlari.csv", Source = AirportsUrl };
        var (code, body) = await GetAsync(AirportsUrl, ct);
        if (code != 200) throw new InvalidOperationException($"OurAirports: HTTP {code}");
        var lines = body.Split('\n');
        var head = StudioExport.SplitCsv(lines[0].TrimEnd('\r'));
        int Col(string n) => head.IndexOf(n);
        int iCountry = Col("iso_country"), iRegion = Col("iso_region"), iType = Col("type"), iSched = Col("scheduled_service");
        var rows = new List<List<string?>>();
        foreach (var raw in lines.Skip(1))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            var c = StudioExport.SplitCsv(line);
            if (c.Count < head.Count) continue;
            if (c[iCountry] == "US" && c[iType] is "large_airport" or "medium_airport" && c[iSched] == "yes")
                rows.Add(AirportCols.Select(n => (string?)c[Col(n)]).ToList());
        }
        // sıra: eyalet (iso_region), sonra kod
        WriteCsv(Path.Combine(dir, e.File), AirportCols, rows.OrderBy(r => r[4], StringComparer.Ordinal).ThenBy(r => r[0], StringComparer.Ordinal));
        e.Rows = rows.Count;
        e.Period = "güncel (OurAirports her gün güncellenir)";
        e.Details["süzgeç"] = "iso_country=US, type=large_airport|medium_airport, scheduled_service=yes (bütün eyaletler)";
        _log($"havalimanları: {rows.Count} satır");
        return e;
    }

    // ---------- 8. FEMA sel bölgesi ----------

    const string FemaService = "https://hazards.fema.gov/arcgis/rest/services/public/NFHL/MapServer";
    const string FemaMirror = "https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/USA_Flood_Hazard_Reduced_Set_gdb/FeatureServer";
    string? _femaLayer;
    string _femaSource = "";
    string? _femaLayerStatement;     // katmanın kendi açıklamasından kapsam cümleleri (İngilizce, olduğu gibi)

    public const string FemaFound = "bölge bulundu";
    public const string FemaNoZone = "sorgu başarılı, bölge bulunamadı";
    public const string FemaFailed = "sorgu başarısız";

    /// Servis açıklamasındaki (HTML) kapsam cümleleri: hangi FEMA sürümünden türetildiği, yayın tarihi ve hangi bölgelerin
    /// çıkarıldığı. Yorum eklenmez; cümleler olduğu gibi aktarılır.
    static string? LayerStatement(string? descriptionHtml)
    {
        if (string.IsNullOrWhiteSpace(descriptionHtml)) return null;
        var text = RedfinDetailsReader.PageText(descriptionHtml.Replace("</div>", ". ").Replace("<br", ". <br"));
        var sentences = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[.])\s+")
            .Select(s => s.Trim(' ', '.')).Where(s => s.Length > 0)
            .Where(s => s.Contains("Publication Date", StringComparison.OrdinalIgnoreCase)
                        || s.Contains("derived from", StringComparison.OrdinalIgnoreCase)
                        || s.Contains("removed", StringComparison.OrdinalIgnoreCase))
            .Distinct().ToList();
        return sentences.Count > 0 ? string.Join(". ", sentences) + "." : null;
    }

    /// "Flood Hazard Zones" katmanı servisin katman listesinden adıyla bulunur. FEMA'nın servisine bağlanılamazsa
    /// (bu bilgisayardan hazards.fema.gov TLS el sıkışmasında kesiliyor) Esri Living Atlas'taki FEMA NFHL kopyasına düşülür;
    /// hangisinin kullanıldığı her evin kaydına (Fema.Source) yazılır.
    async Task<string?> FemaLayerAsync(List<string> warnings, CancellationToken ct)
    {
        if (_femaLayer != null) return _femaLayer;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(40));
            var (code, body) = await GetAsync(FemaService + "?f=json", cts.Token);
            if (code == 200)
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var l in doc.RootElement.GetProperty("layers").EnumerateArray())
                    if (string.Equals(l.GetProperty("name").GetString(), "Flood Hazard Zones", StringComparison.OrdinalIgnoreCase))
                    {
                        _femaLayer = $"{FemaService}/{l.GetProperty("id").GetInt32()}";
                        _femaSource = $"FEMA NFHL ({_femaLayer})";
                        try
                        {
                            var (lc, lb) = await GetAsync(_femaLayer + "?f=json", cts.Token);
                            using var ld = JsonDocument.Parse(lb);
                            _femaLayerStatement = lc == 200 && ld.RootElement.TryGetProperty("description", out var dsc) ? LayerStatement(dsc.GetString()) : null;
                        }
                        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { }
                        return _femaLayer;
                    }
                warnings.Add("FEMA servisinde \"Flood Hazard Zones\" katmanı bulunamadı.");
            }
            else warnings.Add($"FEMA servisi HTTP {code} döndürdü.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            warnings.Add($"FEMA servisine bağlanılamadı ({ex.Message.Split('\n')[0]}); Esri Living Atlas'taki FEMA NFHL kopyası kullanıldı.");
        }
        try
        {
            var (code, body) = await GetAsync(FemaMirror + "/0?f=json", ct);
            if (code != 200) return null;
            using var doc = JsonDocument.Parse(body);
            var date = doc.RootElement.TryGetProperty("editingInfo", out var ei) && ei.TryGetProperty("dataLastEditDate", out var dl) && dl.ValueKind == JsonValueKind.Number
                ? DateTimeOffset.FromUnixTimeMilliseconds(dl.GetInt64()).UtcDateTime.ToString("yyyy-MM-dd", Inv) : "?";
            _femaLayer = FemaMirror + "/0";
            _femaSource = $"Esri Living Atlas, USA Flood Hazard Reduced Set — FEMA NFHL'den türetilmiş kopya, katmanın son düzenleme tarihi {date} ({_femaLayer})";
            // kapsam cümleleri servisin açıklamasında (katmanın kendi açıklaması boş)
            try
            {
                var (sc, sb) = await GetAsync(FemaMirror + "?f=json", ct);
                using var sd = JsonDocument.Parse(sb);
                _femaLayerStatement = sc == 200 && sd.RootElement.TryGetProperty("description", out var dsc) ? LayerStatement(dsc.GetString()) : null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { }
            return _femaLayer;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            warnings.Add("FEMA kopyasına da bağlanılamadı: " + ex.Message);
            return null;
        }
    }

    public async Task<FemaFlood> FloodZoneAsync(double lat, double lng, List<string> warnings, CancellationToken ct)
    {
        var f = new FemaFlood { QueriedAt = DateTime.Now, Result = FemaFailed };
        var layer = await FemaLayerAsync(warnings, ct);
        if (layer == null) { f.Error = "FEMA servisi ve kopyası yanıt vermedi"; return f; }
        f.Source = _femaSource;
        var q = $"geometry={lng.ToString(Inv)},{lat.ToString(Inv)}&geometryType=esriGeometryPoint&inSR=4326&spatialRel=esriSpatialRelIntersects" +
                "&outFields=FLD_ZONE,ZONE_SUBTY,SFHA_TF&returnGeometry=false&f=json";
        try
        {
            var (code, body) = await GetAsync($"{layer}/query?{q}", ct);
            if (code != 200) { f.Error = $"HTTP {code}"; return f; }
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err)) { f.Error = err.GetRawText(); return f; }
            var feats = doc.RootElement.GetProperty("features");
            if (feats.GetArrayLength() == 0)
            {
                f.Result = FemaNoZone;
                f.Note = _femaLayerStatement != null ? "Katmanın kendi açıklaması: " + _femaLayerStatement : null;
                return f;
            }
            var a = feats[0].GetProperty("attributes");
            string? S(string n) => a.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            (f.Zone, f.Subtype, f.Sfha, f.Result) = (S("FLD_ZONE"), S("ZONE_SUBTY"), S("SFHA_TF"), FemaFound);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            f.Error = ex.Message.Split('\n')[0];
        }
        return f;
    }

    // ---------- 7–8. Ev kartları: koordinat ve FEMA ----------

    /// Kartlardaki bütün adaylar: koordinatı yoksa Census toplu adres servisiyle doldurulur (LatLngSource "census geocoder"),
    /// koordinatı olup FEMA sorgusu yapılmamış (ya da hata almış) her aday için FEMA sel bölgesi sorgulanır.
    public async Task<Entry> CompleteHousesAsync(string st, IEnumerable<HouseCard> cards, IProgress<string> status, CancellationToken ct)
    {
        var e = new Entry { File = $"listings_{st}.json (ev kartları)", Source = $"Koordinat: Redfin, yoksa {GeocoderBatch}, o da bulamazsa {Nominatim}; sel bölgesi: FEMA NFHL" };
        var all = cards.SelectMany(c => c.Candidates.Select(h => (Card: c, House: h))).ToList();
        var noCoord = all.Where(x => x.House.Lat == null || x.House.Lng == null).ToList();
        if (noCoord.Count > 0)
        {
            status.Report($"Ev kartları: {noCoord.Count} adayın koordinatı Census Geocoder ile aranıyor");
            var geo = await GeocodeAsync(noCoord.Select((x, i) => (i.ToString(Inv), x.House.Street, x.House.City,
                x.House.State.Length > 0 ? x.House.State : st, x.House.Zip)).ToList(), ct);
            int census = 0, osm = 0;
            for (int i = 0; i < noCoord.Count; i++)
            {
                var h = noCoord[i].House;
                if (geo.TryGetValue(i.ToString(Inv), out var g) && double.TryParse(g.Lat, NumberStyles.Float, Inv, out var la)
                    && double.TryParse(g.Lng, NumberStyles.Float, Inv, out var lo))
                {
                    (h.Lat, h.Lng, h.LatLngSource) = (la, lo, "census geocoder");
                    census++;
                    continue;
                }
                // Census bulamazsa OpenStreetMap Nominatim; yalnızca bina/adres düzeyindeki sonuç kabul edilir (sokak ortası değil)
                status.Report($"Ev kartları: {h.Street} OpenStreetMap'te aranıyor");
                var o = await NominatimAsync(h.Street, h.City, h.State.Length > 0 ? h.State : st, h.Zip, ct);
                if (o != null) { (h.Lat, h.Lng, h.LatLngSource) = (o.Value.Lat, o.Value.Lng, "openstreetmap nominatim"); osm++; }
                else e.Warnings.Add($"{noCoord[i].Card.County}: {h.Street}, {h.City} koordinatı bulunamadı (Census: {(geo.TryGetValue(i.ToString(Inv), out var g2) ? g2.Match : "yanıt yok")}, OpenStreetMap: adres düzeyinde sonuç yok)");
            }
            e.Details["koordinat_census"] = $"{census}/{noCoord.Count}";
            e.Details["koordinat_openstreetmap"] = osm.ToString(Inv);
            _log($"ev kartları: {noCoord.Count} koordinatsız aday → Census {census}, OpenStreetMap {osm}");
        }
        // hata alan ya da bölge bulamayan sorgular her çalıştırmada yenilenir (FEMA'nın kendi servisi açılırsa oradan gelir);
        // sonuç alanı (FemaResult) olmayan eski kayıtlar da
        var todo = all.Where(x => x.House.Lat != null && x.House.Lng != null && (x.House.FemaZone == null || x.House.FemaResult == null)).ToList();
        int n = 0;
        foreach (var (card, h) in todo)
        {
            status.Report($"Ev kartları: FEMA sel bölgesi {++n}/{todo.Count}");
            var f = await FloodZoneAsync(h.Lat!.Value, h.Lng!.Value, e.Warnings, ct);
            (h.FemaResult, h.FemaZone, h.FemaZoneSubtype, h.FemaSfha, h.FemaQueriedAt, h.FemaSource, h.FemaNote, h.FemaError) =
                (f.Result, f.Zone, f.Subtype, f.Sfha, f.QueriedAt, f.Source, f.Note, f.Error);
            if (f.Error != null) e.Warnings.Add($"{card.County}: {h.Street} FEMA sorgusu başarısız: {f.Error}");
        }
        e.Warnings = e.Warnings.Distinct().ToList();
        e.Rows = all.Count;
        e.Details["fema_sorgusu"] = todo.Count.ToString(Inv);
        if (_femaSource.Length > 0) e.Details["fema_kaynağı"] = _femaSource;
        if (_femaLayerStatement != null) e.Details["fema_katmanının_açıklaması"] = _femaLayerStatement;
        // seçili evlerin FEMA sonucu, üç durum ayrı: bölge bulundu / sorgu başarılı, bölge bulunamadı / sorgu başarısız
        foreach (var c in cards.Where(c => c.Chosen?.FemaResult != null))
        {
            var h = c.Chosen!;
            e.Details[$"fema: {c.County} ({h.Street})"] = h.FemaResult == FemaFound ? $"{FemaFound}: {h.FemaZone}"
                : h.FemaResult == FemaFailed ? $"{FemaFailed}: {h.FemaError}" : h.FemaResult!;
        }
        _log($"ev kartları: {all.Count} aday, {todo.Count} FEMA sorgusu");
        return e;
    }

    // ---------- 6. Manifest ----------

    /// out\ek_{ST}\manifest.json. Bu çalıştırmada hata veren adımın eski kaydı (dosya duruyorsa) korunur, hatası eklenir.
    public static void WriteManifest(string dir, string st, List<Entry> entries)
    {
        var path = Path.Combine(dir, "manifest.json");
        var old = new Dictionary<string, Entry>();
        try
        {
            if (File.Exists(path))
                using (var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8)))
                    foreach (var f in doc.RootElement.GetProperty("files").EnumerateArray())
                        if (f.Deserialize<Entry>(JsonOpts) is { } en) old[en.File] = en;
        }
        catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or InvalidOperationException) { }
        var merged = new List<Entry>();
        foreach (var en in entries)
        {
            if (en.Error != null && old.TryGetValue(en.File, out var prev) && prev.Error == null && File.Exists(Path.Combine(dir, en.File)))
            {
                prev.Warnings.Add($"Son deneme ({en.FetchedAt:yyyy-MM-dd HH:mm}) başarısız: {en.Error}. Dosya önceki çekilişten.");
                merged.Add(prev);
            }
            else merged.Add(en);
            old.Remove(en.File);
        }
        merged.AddRange(old.Values);   // bu çalıştırmada olmayan eski kayıtlar
        var manifest = new { state = st, written_at = DateTime.Now, principle = Principle, files = merged };
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, JsonOpts), new UTF8Encoding(false));
    }

    // ---------- 9. Okunabilir ev detayı dosyası ----------

    /// out\ev_detaylari_{ST}.md: seçili her ev için alan adı ve değer; yorum cümlesi yok.
    public static void WriteHouseDetails(string path, string st, IEnumerable<HouseCard> cards)
    {
        var sb = new StringBuilder($"# Ev detayları — {st}\n\nYazıldı: {DateTime.Now:yyyy-MM-dd HH:mm}. {Principle}\n");
        foreach (var card in cards.Where(c => c.Chosen != null).OrderBy(c => c.County))
        {
            var h = card.Chosen!;
            string V(object? v) => v switch { null => "—", string s when s.Length == 0 => "—", _ => Convert.ToString(v, Inv)! };
            void F(string name, object? v) => sb.Append($"- **{name}:** {V(v)}\n");
            sb.Append($"\n## {card.County} — {h.City}\n\n");
            F("Adres", $"{h.Street}, {h.City}, {h.State} {h.Zip}".Trim());
            F("İlan", h.Url);
            F("Mod", card.Mode);
            F("İlan durumu (Redfin)", h.ListingStatus ?? (h.ListingStatusAt != null ? "okunamadı" : null));
            F("Durumun okunduğu zaman", h.ListingStatusAt?.ToString("yyyy-MM-dd HH:mm", Inv));
            if (h.ListingStatusNote != null) F("Durum okunamama sebebi", h.ListingStatusNote);
            F("Kart metni", card.CardText);
            F("İlan tarihi", h.ListedDate?.ToString("yyyy-MM-dd", Inv));
            F("İlk fiyat ($)", h.OriginalPrice);
            F("Güncel fiyat ($)", h.CurrentPrice);
            F("İndirim sayısı", h.Cuts.Count);
            F("Gün", h.Days);
            F("Son satış", h.LastSaleDate is { } d ? $"{d:yyyy-MM-dd}, {h.LastSalePrice} $" : null);
            F("Yapım yılı", h.YearBuilt);
            F("Alan (sqft)", h.SqFt);
            F("Oda / banyo", h.Beds.HasValue || h.Baths.HasValue ? $"{h.Beds?.ToString("0.#", Inv) ?? "?"} / {h.Baths?.ToString("0.#", Inv) ?? "?"}" : null);
            F("Koordinat", h.Lat.HasValue ? $"{h.Lat.Value.ToString("0.######", Inv)}, {h.Lng!.Value.ToString("0.######", Inv)} ({h.LatLngSource ?? "redfin"})" : null);
            if (h.PriceSteps.Count > 0 || h.Cuts.Count > 0)
            {
                RedfinListingPicker.FillPriceSteps(h);
                sb.Append("- **Fiyat geçmişi:**\n");
                foreach (var s in h.PriceSteps) sb.Append($"  - {s.Date:yyyy-MM-dd}: {s.Price} $\n");
            }
            sb.Append("\n### FEMA sel bölgesi\n\n");
            F("Sonuç", h.FemaResult);
            F("FLD_ZONE", h.FemaZone);
            F("ZONE_SUBTY", h.FemaZoneSubtype);
            F("SFHA_TF", h.FemaSfha);
            F("Kaynak", h.FemaSource);
            F("Sorgu zamanı", h.FemaQueriedAt?.ToString("yyyy-MM-dd HH:mm", Inv));
            if (h.FemaNote != null) F("Kaynak katmanın kapsamı", h.FemaNote);
            if (h.FemaError != null) F("Sorgu hatası", h.FemaError);
            sb.Append("\n### Redfin ilan sayfası (ham)\n\n");
            var r = h.Redfin;
            if (r == null)
            {
                sb.Append("- Bu ev ilan sayfası okuyucusundan önce toplanmış; alanlar ilçe \"Ev kartlarını topla\" ile yeniden toplanınca dolar.\n");
                continue;
            }
            F("Okunma zamanı", r.ReadAt.ToString("yyyy-MM-dd HH:mm", Inv));
            F("Yıllık emlak vergisi", r.PropertyTax);
            F("Vergi yılı", r.PropertyTaxYear);
            F("Aidat (HOA, MLS)", r.Hoa);
            F("Aidat dönemi (MLS)", r.HoaPeriod);
            F("Aylık aidat (Redfin ödeme hesaplayıcısı)", r.HoaMonthlyRedfin);
            F("Aidat bilgileri (MLS, olduğu gibi)", r.HoaAmenities);
            F("FEMA bölgesi (Redfin'in gösterdiği, tahmini)", r.FloodZone);
            F("Sel sigortası (Redfin tahmini, yıllık $)", r.FloodInsuranceEstimate);
            foreach (var kind in new[] { "flood", "fire", "heat", "wind", "air" })
                F($"İklim riski: {kind} (First Street, 1-10)", r.ClimateRisk.GetValueOrDefault(kind));
            F("İlanı veren emlakçı", r.AgentName);
            F("Ofis", r.OfficeName);
            F("Açıklama", r.Description);
            F("Okunan alanlar (JSON yolu)", r.Found.Count > 0 ? string.Join("; ", r.Found.Select(kv => $"{kv.Key} ← {kv.Value}")) : null);
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    /// Eyaletin komşu eyaletleri: PromptData\states_intro.json'daki komşu metninden (ör. "Georgia, Alabama and the Gulf of
    /// Mexico") eyalet adları; eyalet olmayanlar (körfez, okyanus, ülke) atlanır.
    public static List<string> NeighborStates(string baseDir, string st)
    {
        try
        {
            var states = IntroPrompt.LoadStates(Path.Combine(IntroPrompt.Dir(baseDir), IntroPrompt.StatesFile));
            if (!states.TryGetValue(st, out var s)) return new();
            var parts = s.Neighbors.Replace(" and ", ",").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Select(p => CountyCatalog.States.FirstOrDefault(x => string.Equals(x.Name, p, StringComparison.OrdinalIgnoreCase)).Abbr)
                .Where(a => a != null && a != st).Distinct().ToList()!;
        }
        catch (Exception ex) when (ex is IOException or JsonException) { return new(); }
    }
}
