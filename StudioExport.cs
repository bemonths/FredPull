using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FredPull;

/// Harita Stüdyosu (https://github.com/bemonths/harita-studyosu) projesi üretme, doğrulama ve render.
/// Şema: stüdyonun docs\ENTEGRASYON.md §5 (sürüm 1): state_map giriş + her county için county_focus ve price_ladder.
public static class StudioExport
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// Videodaki bir county: sıra, FIPS ve etiketin iki satırı (senaryodan).
    public sealed record TextRow(int Order, string Fips, string County, string FocusSub, string FocusStat);

    public sealed record Result(string Name, string Path, int Scenes, List<string> Warnings, List<string> Errors, double VideoSeconds);

    /// FredPull sinyali → stüdyo kategori anahtarı (brand.json). Küçük taban ve veri yok yazılmaz (none).
    static readonly Dictionary<string, string> SignalKeys = new()
    {
        ["Alıcı çekildi"] = "buyers",
        ["Satıcı çekiliyor"] = "sellers",
        ["Fiyat kırılıyor"] = "price",
        ["Zayıflıyor"] = "weak",
        ["Dengeli"] = "stable",
        ["Sıcak"] = "hot",
    };

    // ---------- stüdyo klasörü ----------

    public static string PythonPath(string studioDir) => Path.Combine(studioDir, ".venv", "Scripts", "python.exe");

    /// Geçerliyse null, değilse kullanıcıya gösterilecek açıklama.
    public static string? CheckStudio(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return "Harita Stüdyosu klasörü seçilmedi ya da yok.";
        var missing = new[] { Path.Combine(dir, "engine", "cli.py"), PythonPath(dir) }.Where(p => !File.Exists(p)).ToList();
        return missing.Count == 0 ? null
            : $"Seçilen klasör Harita Stüdyosu değil ya da kurulumu tamamlanmamış. Eksik:\n{string.Join("\n", missing)}\n\n" +
              "Stüdyonun kök klasörünü seç (içinde engine ve .venv olan); kurulum yapılmadıysa önce kurulum.bat.";
    }

    /// exe'den yukarı doğru "harita-studyosu" klasörü arar (ör. FredPull\harita-studyosu).
    public static string? FindStudio(string baseDir)
    {
        var d = new DirectoryInfo(baseDir);
        for (int i = 0; i < 6 && d != null; i++, d = d.Parent)
        {
            var c = Path.Combine(d.FullName, "harita-studyosu");
            if (CheckStudio(c) == null) return c;
        }
        return null;
    }

    // ---------- metin dosyası ----------

    /// out\metinler_{ST}.csv: order,fips,county,focus_sub,focus_stat (UTF-8, ilk satır başlık, tırnaklı alanlar).
    public static List<TextRow> ReadTexts(string path, List<string> warnings)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8).Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count == 0) return new();
        var head = SplitCsv(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList();
        int Col(string n) => head.IndexOf(n);
        int iOrder = Col("order"), iFips = Col("fips"), iCounty = Col("county"), iSub = Col("focus_sub"), iStat = Col("focus_stat");
        if (iFips < 0) throw new InvalidOperationException($"{Path.GetFileName(path)}: 'fips' sütunu yok.");

        var rows = new List<TextRow>();
        for (int n = 1; n < lines.Count; n++)
        {
            var c = SplitCsv(lines[n]);
            string Get(int i) => i >= 0 && i < c.Count ? c[i].Trim() : "";
            var fips = Get(iFips).PadLeft(5, '0');
            if (fips.Length != 5 || !fips.All(char.IsDigit)) { warnings.Add($"Metin dosyası satır {n + 1}: geçersiz FIPS '{Get(iFips)}', atlandı."); continue; }
            if (rows.Any(r => r.Fips == fips)) { warnings.Add($"Metin dosyası: {fips} iki kez var, ilki kullanıldı."); continue; }
            int order = int.TryParse(Get(iOrder), out var o) ? o : n;
            rows.Add(new TextRow(order, fips, Get(iCounty), Get(iSub), Get(iStat)));
        }
        return rows.OrderBy(r => r.Order).ToList();
    }

    static List<string> SplitCsv(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else sb.Append(ch);
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { fields.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        fields.Add(sb.ToString());
        return fields;
    }

    // ---------- proje ----------

    /// Projeyi kurar: state_map (bütün county'lerin sinyal boyaması) + her metin satırı için county_focus ve (ev varsa) price_ladder.
    public static (JsonObject Project, int Scenes, double VideoSeconds) Build(Snapshot snap, IReadOnlyDictionary<string, HouseCard> cards,
        List<TextRow> texts, string name, DateOnly today, List<string> warnings)
    {
        var state = snap.State;
        var stateFips = snap.Counties.FirstOrDefault()?.County.Fips[..2] ?? "";

        var assign = new JsonObject();
        foreach (var r in snap.Counties.OrderBy(r => r.County.Fips))
            if (SignalKeys.TryGetValue(r.Signal, out var key)) assign[r.County.Fips] = key;

        // FRED veri ayı (county'lerin en yenisi): "AUGUST 2026"
        var month = snap.Counties.Select(r => r.Month).Where(m => m.Length == 7).DefaultIfEmpty("").Max()!;
        var monthText = DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", Inv, DateTimeStyles.None, out var md)
            ? $"{En.DateTimeFormat.GetMonthName(md.Month).ToUpperInvariant()} {md.Year}" : "";

        texts = texts.Where(t =>
        {
            if (t.Fips.StartsWith(stateFips, StringComparison.Ordinal)) return true;
            warnings.Add($"{t.Fips} ({t.County}) {state} eyaletine ait değil, atlandı.");
            return false;
        }).ToList();

        var scenes = new JsonArray();
        double seconds = 13;
        scenes.Add(Scene("state_map", new JsonObject
        {
            ["state"] = state,
            ["subtitle"] = $"{texts.Count} COUNTIES  ·  {monthText} DATA",
            ["assign"] = assign.DeepClone(),
            ["focus"] = null,
        }));

        foreach (var t in texts)
        {
            scenes.Add(Scene("county_focus", new JsonObject
            {
                ["state"] = state,
                ["assign"] = assign.DeepClone(),
                ["focus"] = t.Fips,
                ["focus_sub"] = t.FocusSub,
                ["focus_stat"] = t.FocusStat,
            }));
            seconds += 5;

            var countyName = snap.Counties.FirstOrDefault(r => r.County.Fips == t.Fips)?.County.Name ?? t.County;
            if (!cards.TryGetValue(t.Fips, out var card) || card.Chosen is not { } h)
            {
                warnings.Add($"{countyName}: ev kartı yok, yalnızca county_focus üretildi (price_ladder atlandı).");
                continue;
            }
            var history = LadderHistory(h);
            if (history.Count < 2)
            {
                warnings.Add($"{countyName}: fiyat geçmişinde en az 2 adım yok, price_ladder atlandı.");
                continue;
            }
            var last = history[^1].Date;
            var ladderToday = today < last ? last : today;
            bool paid = h.LastSalePrice.HasValue;
            scenes.Add(Scene("price_ladder", new JsonObject
            {
                ["kicker"] = $"{countyName}  ·  {h.City}".ToUpperInvariant(),
                ["subtitle"] = Subtitle(h, card.Mode == "Daire"),
                ["history"] = new JsonArray(history.Select(s => (JsonNode)new JsonObject
                {
                    ["date"] = s.Date.ToString("yyyy-MM-dd", Inv),
                    ["price"] = s.Price,
                }).ToArray()),
                ["today"] = ladderToday.ToString("yyyy-MM-dd", Inv),
                ["paid"] = paid ? h.LastSalePrice : null,
                ["paid_year"] = paid ? h.LastSaleDate?.Year : null,
            }));
            seconds += 11.5;
        }

        var project = new JsonObject
        {
            ["version"] = 1,
            ["name"] = name,
            ["transition"] = 0.6,
            ["output"] = new JsonObject { ["separate"] = true, ["combined"] = true, ["transparent"] = false },
            ["scenes"] = scenes,
        };
        return (project, scenes.Count, seconds - 0.6 * (scenes.Count - 1));
    }

    static JsonObject Scene(string type, JsonObject p) => new() { ["type"] = type, ["enabled"] = true, ["params"] = p };

    /// Fiyat merdiveni: PriceSteps; aynı güne düşen adımlardan sonuncusu, ardışık eşit ya da 1.000 $'dan küçük farklı
    /// adımlar birleşir. Stüdyo her düşüşü indirim sayıyor; böylece videodaki indirim sayısı kart metniyle aynı kalır
    /// (430.000 → 429.999 gibi değişiklikler kartta da sayılmıyor).
    public static List<PriceCut> LadderHistory(HouseCandidate h)
    {
        RedfinListingPicker.FillPriceSteps(h);
        var byDay = new List<PriceCut>();
        foreach (var s in h.PriceSteps.OrderBy(s => s.Date))
            if (byDay.Count > 0 && byDay[^1].Date == s.Date) byDay[^1] = s;
            else byDay.Add(s);
        var steps = new List<PriceCut>();
        foreach (var s in byDay)
            if (steps.Count == 0 || Math.Abs(s.Price - steps[^1].Price) >= RedfinListingPicker.MinCut) steps.Add(s);
        return steps;
    }

    /// "4 bedrooms  ·  built 2000  ·  listed August 2024"; daire: "2-bedroom condo  ·  built ..."; eksik parça atlanır.
    static string Subtitle(HouseCandidate h, bool condo)
    {
        var parts = new List<string>();
        if (h.Beds is double b && b > 0)
        {
            var n = b.ToString("0.#", Inv);
            parts.Add(condo ? $"{n}-bedroom condo" : $"{n} {(b == 1 ? "bedroom" : "bedrooms")}");
        }
        else if (condo) parts.Add("condo");
        if (h.YearBuilt is int y) parts.Add($"built {y}");
        if (h.ListedDate is DateOnly d) parts.Add($"listed {En.DateTimeFormat.GetMonthName(d.Month)} {d.Year}");
        return string.Join("  ·  ", parts);
    }

    /// <stüdyo>\projects\{name}.json; aynı ad varsa _2, _3 (hem dosya adı hem name alanı).
    public static string UniqueName(string studioDir, string baseName)
    {
        var dir = Path.Combine(studioDir, "projects");
        var name = baseName;
        for (int k = 2; File.Exists(Path.Combine(dir, name + ".json")); k++) name = $"{baseName}_{k}";
        return name;
    }

    public static string Write(string studioDir, JsonObject project)
    {
        var dir = Path.Combine(studioDir, "projects");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, project["name"]!.GetValue<string>() + ".json");
        File.WriteAllText(path, project.ToJsonString(JsonOpts), new UTF8Encoding(false));   // BOM'suz: Python json.load BOM'u reddeder
        return path;
    }

    // ---------- stüdyo süreçleri ----------

    const string ValidateScript =
        "import json, os, sys\n" +
        "sys.path.insert(0, os.getcwd())\n" +
        "from engine import project\n" +
        "p = json.load(open(sys.argv[1], encoding='utf-8'))\n" +
        "try:\n" +
        "    _, e = project.validate(p)\n" +
        "    print(json.dumps(e, ensure_ascii=False))\n" +
        "except project.ProjectError as x:\n" +
        "    print(json.dumps([{'scene': None, 'param': 'project', 'message': str(x)}], ensure_ascii=False))\n";

    static ProcessStartInfo Python(string studioDir, params string[] args)
    {
        var psi = new ProcessStartInfo(PythonPath(studioDir))
        {
            WorkingDirectory = studioDir, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
        };
        psi.Environment["PYTHONUTF8"] = "1";
        foreach (var a in args) psi.ArgumentList.Add(a);
        return psi;
    }

    /// Stüdyonun kendi doğrulaması (engine.project.validate). Hatalar "2. sahne / focus: ..." biçiminde döner; boşsa temiz.
    public static async Task<List<string>> ValidateAsync(string studioDir, string projectPath, CancellationToken ct)
    {
        var script = Path.Combine(Path.GetTempPath(), "fredpull_studio_validate.py");
        await File.WriteAllTextAsync(script, ValidateScript, new UTF8Encoding(false), ct);
        using var p = Process.Start(Python(studioDir, script, projectPath))!;
        var errTask = p.StandardError.ReadToEndAsync(ct);
        var output = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        var last = output.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith('['));
        if (last == null)
            throw new InvalidOperationException($"Stüdyo doğrulaması çalışmadı (çıkış kodu {p.ExitCode}):\n{Tail(await errTask)}");
        using var doc = JsonDocument.Parse(last);
        return doc.RootElement.EnumerateArray().Select(e =>
        {
            var scene = e.TryGetProperty("scene", out var s) && s.ValueKind == JsonValueKind.Number ? $"{s.GetInt32() + 1}. sahne" : "proje";
            return $"{scene} / {e.GetProperty("param").GetString()}: {e.GetProperty("message").GetString()}";
        }).ToList();
    }

    /// python -m engine.cli render projects\{name}.json --progress-json. Her stdout satırı bir JSON olayı (progress, compose,
    /// output, done, error, log) olarak onEvent'e gelir; çağıran UI iş parçacığındaysa olaylar da orada gelir.
    /// İptalde süreç (ve alt süreçleri, ffmpeg) sonlandırılır. Üretilen dosyaların yollarını döndürür.
    public static async Task<List<string>> RenderAsync(string studioDir, string projectPath, Action<JsonElement> onEvent, CancellationToken ct)
    {
        var rel = Path.GetRelativePath(studioDir, projectPath);
        using var p = Process.Start(Python(studioDir, "-m", "engine.cli", "render", rel, "--progress-json"))!;
        var errTask = p.StandardError.ReadToEndAsync();
        using var reg = ct.Register(() => { try { if (!p.HasExited) p.Kill(true); } catch (InvalidOperationException) { } });

        var outputs = new List<string>();
        string? error = null;
        bool done = false;
        string? line;
        while ((line = await p.StandardOutput.ReadLineAsync()) != null)
        {
            JsonElement ev;
            try { using var doc = JsonDocument.Parse(line); ev = doc.RootElement.Clone(); }
            catch (JsonException) { continue; }
            switch (ev.TryGetProperty("event", out var k) ? k.GetString() : null)
            {
                case "output": outputs.Add(ev.GetProperty("path").GetString() ?? ""); break;
                case "done": done = true; break;
                case "error": error = ev.GetProperty("message").GetString(); break;
            }
            onEvent(ev);
        }
        await p.WaitForExitAsync();
        ct.ThrowIfCancellationRequested();
        if (error != null) throw new InvalidOperationException(error);
        if (!done || p.ExitCode != 0)
            throw new InvalidOperationException($"Render tamamlanmadı (çıkış kodu {p.ExitCode}).\n{Tail(await errTask)}");
        return outputs;
    }

    static string Tail(string s) => s.Length > 1500 ? "…" + s[^1500..] : s;
}
