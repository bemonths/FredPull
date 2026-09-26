using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace FredPull;

/// Grafik listesi (out\grafikler_{ST}.csv) ve Harita Stüdyosu'nun grafik sahneleri (stüdyo v1.3, ENTEGRASYON.md §5.5–5.13).
/// Her satır bir tarif; veri bellekteki Snapshot'tan hesaplanır, rakamlar elle girilmez. Tanımlar CLAUDE.md'deki tabloyla
/// aynıdır (makale projesi anlatıcının söyleyeceği rakamı aynı tanımla hesaplar).
public static class ChartList
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");
    public const string Header = "seq,slot,fips,chart,metrics,text,caption";
    const string Minus = "−";   // stüdyonun farklarda kullandığı eksi işareti

    /// Line: dosyadaki satır numarası (1'den; başlık 1. satır). Silme bu satırı dosyadan çıkarır.
    public sealed record Row(int Seq, string Slot, string Fips, string Chart, string Metrics, string Text, string Caption, int Line = 0);

    /// Tarif: kimlik, stüdyo sahnesi, temel süre (sn), arayüz adı.
    public sealed record Recipe(string Id, string Scene, double Seconds, string Title);

    public static readonly Recipe[] Recipes =
    {
        new("question_card", "question_board", 6.0, "Soru kartı (vaat ekranı)"),
        new("county_quiz", "county_quiz", 9.5, "County soru kartı"),
        new("price_years", "house_bars", 7.0, "Satış fiyatı, 2019'dan (ev sütunları)"),
        new("stock_years_line", "line_trend", 7.0, "Satılık ev, 2016'dan (çizgi)"),
        new("stock_years_bars", "house_bars", 7.0, "Satılık ev, 2019'dan (ev sütunları)"),
        new("cut_share_compare", "bar_list", 6.5, "Fiyat kıran pay: county / eyalet / ABD"),
        new("sale_to_list_ring", "ring", 6.0, "Satış / liste fiyatı (halka)"),
        new("months_supply_thermo", "thermometer", 6.5, "Aylık stok: county / eyalet (termometre)"),
        new("months_supply_rank", "bar_list", 6.5, "Aylık stok sıralaması (eyalet geneli)"),
        new("stock_change_grid", "house_grid", 7.5, "Satılık ev, geçen yıl → bu yıl (ev ızgarası)"),
    };

    public static readonly (string Id, string Title)[] QuizMetrics =
    {
        ("homes_for_sale", "Satılık ev"),
        ("cut_share_in10", "Fiyat kıranlar (10 evde)"),
        ("months_supply", "Aylık stok"),
        ("sale_to_list", "Satış / liste ($100'da)"),
        ("sale_price_vs_peak", "Satış fiyatı / zirve"),
        ("stock_vs_2019", "Satılık ev / 2019"),
    };

    public static readonly string[] Icons = { "house", "county", "houses10", "none" };
    public static readonly string[] Slots = { "intro", "county", "mid", "closing" };

    public static Recipe? Find(string id) => Recipes.FirstOrDefault(r => r.Id == id);

    /// Eyalet geneli tarifler; diğerleri bir county ister.
    public static bool StateWide(string chart) => chart is "question_card" or "months_supply_rank";

    /// Satırda slot boşsa: soru kartı intro, sıralama closing, diğerleri county.
    public static string DefaultSlot(string chart) => chart switch
    {
        "question_card" => "intro",
        "months_supply_rank" => "closing",
        _ => "county",
    };

    // ---------- dosya ----------

    public static List<Row> Read(string path, string stateFips, List<string> warnings)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        var head = lines.Length > 0 ? StudioExport.SplitCsv(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList() : new();
        int Col(string n) => head.IndexOf(n);
        int iSeq = Col("seq"), iSlot = Col("slot"), iFips = Col("fips"), iChart = Col("chart"), iMetrics = Col("metrics"),
            iText = Col("text"), iCaption = Col("caption");
        if (lines.Length > 0 && iChart < 0) throw new InvalidOperationException($"{Path.GetFileName(path)}: 'chart' sütunu yok.");

        var rows = new List<Row>();
        for (int n = 1; n < lines.Length; n++)
        {
            if (lines[n].Trim().Length == 0) continue;
            var c = StudioExport.SplitCsv(lines[n]);
            string Get(int i) => i >= 0 && i < c.Count ? c[i].Trim() : "";
            var where = $"Grafik listesi satır {n + 1}";
            void Skip(string why) => warnings.Add($"{where}: {why}, atlandı.");

            var chart = Get(iChart).ToLowerInvariant();
            if (Find(chart) == null) { Skip($"bilinmeyen grafik '{Get(iChart)}'"); continue; }
            if (!int.TryParse(Get(iSeq), NumberStyles.Integer, Inv, out var seq) || seq < 1) { Skip($"seq '{Get(iSeq)}' 1 ya da büyük bir sayı olmalı"); continue; }
            var slot = Get(iSlot).ToLowerInvariant();
            if (slot.Length == 0) slot = DefaultSlot(chart);
            if (!Slots.Contains(slot)) { Skip($"bilinmeyen slot '{Get(iSlot)}' (intro / county / mid / closing)"); continue; }

            var fips = Get(iFips);
            if (fips.Length > 0)
            {
                fips = fips.PadLeft(5, '0');
                if (fips.Length != 5 || !fips.All(char.IsDigit)) { Skip($"geçersiz FIPS '{Get(iFips)}'"); continue; }
                if (!fips.StartsWith(stateFips, StringComparison.Ordinal)) { Skip($"{fips} bu eyalete ait değil"); continue; }
            }
            if (slot == "county" && fips.Length == 0) { Skip("county slotunda FIPS boş"); continue; }
            if (!StateWide(chart) && fips.Length == 0) { Skip($"{chart} bir county ister, FIPS boş"); continue; }

            var metrics = Get(iMetrics).ToLowerInvariant();
            if (chart == "county_quiz")
            {
                var parts = metrics.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                var bad = parts.FirstOrDefault(m => QuizMetrics.All(q => q.Id != m));
                if (parts.Count == 0) { Skip("county_quiz için en az bir ölçü gerekli"); continue; }
                if (bad != null) { Skip($"bilinmeyen ölçü '{bad}'"); continue; }
                if (parts.Count > 3) { warnings.Add($"{where}: en fazla üç ölçü kullanılır, fazlası atlandı."); parts = parts.Take(3).ToList(); }
                metrics = string.Join(";", parts);
            }
            else if (chart == "question_card")
            {
                if (metrics.Length == 0) metrics = "none";
                if (!Icons.Contains(metrics)) { Skip($"bilinmeyen simge '{Get(iMetrics)}' (house / county / houses10 / none)"); continue; }
            }
            else if (metrics.Length > 0)
            {
                warnings.Add($"{where}: {chart} ölçü almaz, '{Get(iMetrics)}' yok sayıldı.");
                metrics = "";
            }

            var text = Get(iText);
            var caption = Get(iCaption);
            if (text.Contains('%') || caption.Contains('%')) { Skip("ekranda yüzde işareti kullanılmaz ('44 OF 100', '3 IN 10' gibi yazın)"); continue; }
            if (chart == "question_card" && text.Length == 0) { Skip("soru kartının değeri (text) boş"); continue; }
            rows.Add(new Row(seq, slot, fips, chart, metrics, text, caption, n + 1));
        }
        return rows.OrderBy(r => r.Seq).ToList();   // OrderBy kararlı: aynı seq'te dosya sırası korunur
    }

    static string Quote(string s) => s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    public static string ToCsv(Row r) =>
        string.Join(",", new[] { r.Seq.ToString(Inv), r.Slot, r.Fips, r.Chart, r.Metrics, r.Text, r.Caption }.Select(Quote));

    /// Satırı dosyanın sonuna ekler; dosya yoksa başlıkla oluşturur (UTF-8, BOM'suz).
    public static void Append(string path, Row r)
    {
        var enc = new UTF8Encoding(false);
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            File.WriteAllText(path, Header + "\r\n" + ToCsv(r) + "\r\n", enc);
            return;
        }
        var existing = File.ReadAllText(path, Encoding.UTF8);
        var sep = existing.EndsWith('\n') ? "" : "\r\n";
        File.AppendAllText(path, sep + ToCsv(r) + "\r\n", enc);
    }

    /// Dosyadaki bir satırı (Row.Line) siler; diğer satırlara dokunmaz.
    public static void DeleteLine(string path, int line)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8).ToList();
        if (line < 2 || line > lines.Count) return;
        lines.RemoveAt(line - 1);
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n", new UTF8Encoding(false));
    }

    /// Sonraki seq; soru kartı eklenirken son soru kartının seq'i (kartı 4'ten azsa) tekrar kullanılır ki aynı ekranda birleşsin.
    public static int NextSeq(List<Row> rows, string chart)
    {
        if (chart == "question_card")
        {
            var lastCard = rows.LastOrDefault(r => r.Chart == "question_card");
            if (lastCard != null && rows.Count(r => r.Chart == "question_card" && r.Seq == lastCard.Seq) < 4) return lastCard.Seq;
        }
        return rows.Count == 0 ? 1 : rows.Max(r => r.Seq) + 1;
    }

    // ---------- hesap ----------

    /// Ekrana basılacak bir rakam: out\grafik_degerleri_{ST}.csv satırı.
    public sealed record Dump(int Seq, string Chart, string Fips, string County, string Field, string Value, string Definition);

    public sealed class Context
    {
        public Context(Snapshot snap, IReadOnlyList<StudioExport.TextRow> texts, IReadOnlyDictionary<string, string> colors, List<string> warnings)
        {
            Snap = snap; Texts = texts; Colors = colors; Warnings = warnings;
            StateName = (CountyCatalog.States.FirstOrDefault(s => s.Abbr == snap.State).Name ?? snap.State).ToUpperInvariant();
        }

        public Snapshot Snap { get; }
        public IReadOnlyList<StudioExport.TextRow> Texts { get; }
        public IReadOnlyDictionary<string, string> Colors { get; }   // neutral, accent, loss, price (stüdyonun brand.json'u)
        public List<string> Warnings { get; }
        public List<Dump> Dumps { get; } = new();
        public string StateName { get; }
        readonly HashSet<string> _seen = new();

        /// Aynı uyarı (ör. aynı serideki sıçrama iki grafikte) bir kez yazılır.
        public void Warn(string s) { if (_seen.Add(s)) Warnings.Add(s); }

        public CountyResult? County(string fips) => Snap.Counties.FirstOrDefault(c => c.County.Fips == fips);
        /// Metin dosyasındaki şehir satırı, grafik yazılarıyla aynı biçimde büyük harf ("CAPE CORAL  ·  FORT MYERS").
        public string FocusSub(string fips) => (Texts.FirstOrDefault(t => t.Fips == fips)?.FocusSub ?? "").ToUpperInvariant();
    }

    /// Soru kartı dışındaki bir satırın sahnesi; veri yetmezse null (uyarı yazılır).
    public static JsonObject? Scene(Row row, Context cx)
    {
        CountyResult? r = null;
        if (row.Fips.Length > 0)
        {
            r = cx.County(row.Fips);
            if (r == null) { cx.Warn($"{row.Chart} ({row.Fips}): bu county'nin verisi yok, grafik atlandı."); return null; }
        }
        var recipe = Find(row.Chart)!;
        JsonObject? p = row.Chart switch
        {
            "county_quiz" => Quiz(row, r!, cx),
            "price_years" => PriceYears(row, r!, cx),
            "stock_years_line" => StockLine(row, r!, cx),
            "stock_years_bars" => StockBars(row, r!, cx),
            "cut_share_compare" => CutShare(row, r!, cx),
            "sale_to_list_ring" => SaleToList(row, r!, cx),
            "months_supply_thermo" => Thermo(row, r!, cx),
            "months_supply_rank" => Rank(row, cx),
            "stock_change_grid" => Grid(row, r!, cx),
            _ => null,
        };
        if (p == null) return null;
        if (row.Chart == "stock_change_grid")
        {
            if (row.Text.Length > 0) p["result_text"] = row.Text;   // ızgarada sağ üst sayaca ayrılı; elle yazı sonuç satırına gider
        }
        else
        {
            if (row.Text.Length > 0) p["callout_value"] = row.Text;
            if (row.Caption.Length > 0) p["callout_label"] = row.Caption;
        }
        return StudioExport.Scene(recipe.Scene, p);
    }

    /// Aynı seq'teki soru kartları tek vaat ekranı (question_board). 2–4 kart.
    public static JsonObject? Board(List<Row> cards, Context cx)
    {
        var seq = cards[0].Seq;
        if (cards.Count > 4)
        {
            cx.Warn($"Soru kartları (seq {seq}): bir ekranda en fazla 4 kart olur, fazlası atlandı.");
            cards = cards.Take(4).ToList();
        }
        if (cards.Count < 2)
        {
            cx.Warn($"Soru kartları (seq {seq}): vaat ekranı için en az 2 kart gerekli, ekran atlandı.");
            return null;
        }
        var p = new JsonObject
        {
            ["state"] = cx.Snap.State,
            ["heading"] = cx.StateName,
            ["title"] = "",
            ["subtitle"] = $"{cx.Texts.Count} COUNTIES  ·  {cards.Count} QUESTIONS",
            ["source"] = "",
            ["callout_value"] = "",
            ["callout_label"] = "",
            ["backdrop"] = "state",
            ["fips"] = null,
        };
        for (int k = 1; k <= 4; k++)
        {
            var c = k <= cards.Count ? cards[k - 1] : null;
            var icon = c?.Metrics ?? "none";
            if (icon == "county" && (c!.Fips.Length == 0 || cx.County(c.Fips) == null))
            {
                cx.Warn($"Soru kartı (seq {seq}, {k}. kart): county simgesi için FIPS yok, simgesiz çizildi.");
                icon = "none";
            }
            p[$"card{k}_icon"] = icon;
            p[$"card{k}_fips"] = icon == "county" ? c!.Fips : null;
            p[$"card{k}_value"] = c?.Text ?? "";   // boş kart çizilmez (stüdyonun örnek kartları görünmesin)
            p[$"card{k}_caption"] = c?.Caption ?? "";
        }
        return StudioExport.Scene("question_board", p);
    }

    // ---- ortak ----

    /// Ortak ayarlar: bütün başlık yazıları önce boş (stüdyonun örnek metinleri sızmasın), county varsa başlık county adı;
    /// county slotunda zemin county, diğerlerinde eyalet.
    static JsonObject Base(Row row, CountyResult? r, Context cx)
    {
        var p = new JsonObject
        {
            ["state"] = cx.Snap.State,
            ["heading"] = r != null ? r.County.Name.ToUpperInvariant() : cx.StateName,
            ["title"] = "",
            ["subtitle"] = "",
            ["source"] = "",
            ["callout_value"] = "",
            ["callout_label"] = "",
            ["fips"] = r?.County.Fips,
            ["backdrop"] = r != null && row.Slot == "county" ? "county" : "state",
        };
        return p;
    }

    static string ShortName(string name)
    {
        foreach (var suffix in new[] { " County", " Parish", " Borough", " Census Area", " Municipality", " city" })
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return name[..^suffix.Length];
        return name;
    }

    /// Tablo etiketi (en fazla 24 karakter): tam ad sığıyorsa o, değilse "County"siz kısa ad.
    static string Label(CountyResult r, bool full)
    {
        var s = (full ? r.County.Name : ShortName(r.County.Name)).ToUpperInvariant();
        if (s.Length > 24) s = ShortName(r.County.Name).ToUpperInvariant();
        return s.Length > 24 ? s[..24].Trim() : s;
    }

    static bool TryMonth(string yyyyMM, out int month, out int year, out string name)
    {
        month = year = 0;
        name = "";
        if (yyyyMM.Length != 7 || !int.TryParse(yyyyMM[..4], out year) || !int.TryParse(yyyyMM[5..], out month) || month is < 1 or > 12) return false;
        name = En.DateTimeFormat.GetMonthName(month).ToUpperInvariant();
        return true;
    }

    /// "MAY 2026": döküm tanımlarında ay adı her yerde İngilizce büyük harf.
    static string MonthText(string yyyyMM) => TryMonth(yyyyMM, out _, out var y, out var n) ? $"{n} {y}" : yyyyMM;

    static bool FredMonth(Row row, CountyResult r, Context cx, out int m, out int y, out string name)
    {
        if (TryMonth(r.Month, out m, out y, out name)) return true;
        cx.Warn($"{r.County.Name}: FRED verisi yok, {row.Chart} atlandı.");
        return false;
    }

    static bool RedfinMonth(Row row, CountyResult r, Context cx, out int m, out int y, out string name)
    {
        if (TryMonth(r.RedfinMonth, out m, out y, out name)) return true;
        cx.Warn($"{r.County.Name}: Redfin verisi yok, {row.Chart} atlandı.");
        return false;
    }

    public static string Num(double v) => Math.Abs(v - Math.Round(v)) < 1e-9 ? v.ToString("N0", En) : v.ToString("#,0.0##", En);

    /// Ekrana giden tam sayı: yarımlar yukarı (2,5 → 3). Math.Round'un varsayılanı yarımı çifte yuvarlar (2,5 → 2).
    public static double Whole(double v) => Math.Round(v, MidpointRounding.AwayFromZero);

    /// Stüdyoya giden ondalıklı değer: iki ondalık, ekrandaki son yuvarlamayı stüdyo yapar. Tek ondalıkta 30,54 → 30,5
    /// oluyor, stüdyo da yarımı çifte yuvarlayınca ekranda "30 OF 100" çıkıyordu (doğrusu 31).
    public static double Two(double v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// Stüdyonun money_k biçimi: "$419K", "$1.2M"; yarımlar yukarı.
    public static string MoneyK(double v)
    {
        var a = Math.Abs(v);
        if (a >= 1_000_000) return "$" + Math.Round(a / 1e6, 1, MidpointRounding.AwayFromZero).ToString("0.0", Inv).Replace(".0", "") + "M";
        return "$" + Whole(a / 1000).ToString("N0", En) + "K";
    }

    static string Signed(double d, bool money) => (d < 0 ? Minus : "+") + (money ? MoneyK(Math.Abs(d)) : Num(Math.Abs(d)));

    static void D(Context cx, Row row, CountyResult? r, string field, double value, string definition) =>
        cx.Dumps.Add(new Dump(row.Seq, row.Chart, r?.County.Fips ?? "", r != null ? ShortName(r.County.Name) : cx.StateName, field,
            value.ToString("0.####", Inv), definition));

    /// Aynı ay kuralı: her yılın aynı takvim ayındaki değer. Serinin başlamadığı yıllar sessizce, aradaki eksik yıllar
    /// uyarıyla atlanır. Ardışık iki yıl arasında 2,5 katından büyük ya da 0,4 katından küçük değişim uyarı verir.
    static List<(int Year, double Value)> Yearly(List<Obs>? s, int month, int from, int to, string county, string series, Context cx)
    {
        var byDate = new Dictionary<DateOnly, double>();
        foreach (var o in s ?? new())
            if (o.Value.HasValue) byDate[o.Date] = o.Value.Value;
        var list = new List<(int, double)>();
        if (byDate.Count == 0) return list;
        var first = byDate.Keys.Min();
        var monthName = En.DateTimeFormat.GetMonthName(month).ToUpperInvariant();
        for (int y = from; y <= to; y++)
        {
            var d = new DateOnly(y, month, 1);
            if (byDate.TryGetValue(d, out var v)) list.Add((y, v));
            else if (d >= first) cx.Warn($"{county}: {series} {monthName} {y} değeri yok, o yıl atlandı.");
        }
        for (int i = 1; i < list.Count; i++)
        {
            var (y0, a) = list[i - 1];
            var (y1, b) = list[i];
            if (a > 0 && (b / a > 2.5 || b / a < 0.4))
                cx.Warn($"{county}: {series} serisinde olağan dışı sıçrama ({monthName} {y0} → {y1}, {Num(a)} → {Num(b)}), grafiği kontrol edin.");
        }
        return list;
    }

    /// Aylık sıçrama kontrolü yapılan Redfin sayı serileri. Oran serileri (months_of_supply, avg_sale_to_list) mevsimseldir:
    /// ocakta satış azaldığı için aylık stok her yıl sıçrar (Florida'da Pasco ve Polk yanlış alarm veriyordu).
    static readonly HashSet<string> CountSeries = new() { "homes_sold", "inventory", "new_listings", "pending_sales", "median_sale_price" };

    /// Sayı serisinde son 24 ayda bir aydan diğerine %60'tan büyük değişim (ör. veri kaynağı değişikliği) uyarı verir.
    /// Oran serisinde aylık kontrol yapılmaz; kullanılan ayın değeri geçen yılın aynı ayıyla kıyaslanır (2,5 katından büyük
    /// ya da 0,4 katından küçük değişim uyarı verir).
    static void CheckRedfin(List<Obs>? s, string yyyyMM, string county, string series, Context cx)
    {
        if (s == null || !TryMonth(yyyyMM, out var m, out var y, out var mn)) return;
        var end = new DateOnly(y, m, 1);
        if (!CountSeries.Contains(series))
        {
            if (Analyzer.At(s, end.AddMonths(-12)) is double a0 && a0 > 0 && Analyzer.At(s, end) is double b0 && (b0 / a0 > 2.5 || b0 / a0 < 0.4))
                cx.Warn($"{county}: {series} serisinde olağan dışı sıçrama ({mn} {y - 1} → {y}, {Num(a0)} → {Num(b0)}), grafiği kontrol edin.");
            return;
        }
        var pts = s.Where(o => o.Value.HasValue && o.Date <= end && o.Date > end.AddMonths(-24)).OrderBy(o => o.Date).ToList();
        for (int i = 1; i < pts.Count; i++)
        {
            double a = pts[i - 1].Value!.Value, b = pts[i].Value!.Value;
            if (pts[i].Date == pts[i - 1].Date.AddMonths(1) && a > 0 && Math.Abs(b / a - 1) > 0.6)
                cx.Warn($"{county}: {series} serisinde olağan dışı sıçrama ({pts[i].Date:yyyy-MM}, {Num(a)} → {Num(b)}), grafiği kontrol edin.");
        }
    }

    static JsonArray Table(IEnumerable<(string Label, double Value, bool Highlight)> rows) =>
        new(rows.Select(x => (JsonNode)new JsonObject { ["label"] = x.Label, ["value"] = x.Value, ["highlight"] = x.Highlight }).ToArray());

    // ---- tarifler ----

    /// Redfin median_sale_price, 2019'dan son yıla her yılın Redfin ayı. En yüksek yıl vurgulu, ok zirveden son yıla.
    static JsonObject? PriceYears(Row row, CountyResult r, Context cx)
    {
        if (!RedfinMonth(row, r, cx, out var m, out var y, out var mn)) return null;
        var name = ShortName(r.County.Name);
        var sp = r.Data.R("median_sale_price");
        CheckRedfin(sp, r.RedfinMonth, name, "median_sale_price", cx);
        var pts = Yearly(sp, m, 2019, y, name, "median_sale_price", cx);
        if (pts.Count < 3) { cx.Warn($"{name}: price_years için üçten az yıl var, grafik atlandı."); return null; }
        var peak = pts.First(p => p.Value == pts.Max(q => q.Value));
        var last = pts[^1];
        var p = Base(row, r, cx);
        p["title"] = "WHAT A TYPICAL HOME SOLD FOR";
        p["subtitle"] = $"MEDIAN SALE PRICE  ·  {mn} OF EACH YEAR";
        p["source"] = "SOURCE: REDFIN";
        p["bars"] = Table(pts.Select(x => (x.Year.ToString(Inv), Whole(x.Value), x.Year == peak.Year)));
        p["value_format"] = "money_k";
        foreach (var x in pts) D(cx, row, r, x.Year.ToString(Inv), x.Value, $"Redfin median_sale_price {mn}");
        if (peak.Year != last.Year)
        {
            p["arrow_from"] = peak.Year.ToString(Inv);
            p["arrow_to"] = last.Year.ToString(Inv);
            p["callout_value"] = Signed(last.Value - peak.Value, money: true);
            p["callout_label"] = $"SINCE {mn} {peak.Year}";
            D(cx, row, r, "callout", last.Value - peak.Value, $"son − zirve ({last.Year} − {peak.Year})");
        }
        else
        {
            p["arrow_from"] = "";
            p["arrow_to"] = "";
        }
        return p;
    }

    /// FRED ACTLISCOU, FRED ayı; 2016'dan (serinin ilk tam yılı) son yıla. Referans 2019.
    static JsonObject? StockLine(Row row, CountyResult r, Context cx)
    {
        if (!FredMonth(row, r, cx, out var m, out var y, out var mn)) return null;
        var name = ShortName(r.County.Name);
        var pts = Yearly(r.Data.F("ACTLISCOU"), m, 2016, y, name, "ACTLISCOU", cx);
        if (pts.Count < 3) { cx.Warn($"{name}: stock_years_line için üçten az yıl var, grafik atlandı."); return null; }
        var sub = cx.FocusSub(r.County.Fips);
        var p = Base(row, r, cx);
        p["title"] = "HOMES FOR SALE";
        p["subtitle"] = sub.Length > 0 ? $"EVERY {mn}  ·  {sub}" : $"EVERY {mn}";
        p["source"] = "SOURCE: REALTOR.COM VIA FRED";
        p["points"] = Table(pts.Select(x => (x.Year.ToString(Inv), Whole(x.Value), false)));
        p["value_format"] = "count";
        p["mark_min"] = "yes";
        p["axis_from_zero"] = "yes";
        foreach (var x in pts) D(cx, row, r, x.Year.ToString(Inv), x.Value, $"FRED ACTLISCOU {mn}");
        var last = pts[^1].Value;
        var ref2019 = pts.Where(x => x.Year == 2019).Select(x => (double?)x.Value).FirstOrDefault();
        if (ref2019 is double v19 && v19 > 0)
        {
            p["ref_value"] = Whole(v19);
            p["ref_label"] = $"2019 LEVEL: {Num(Whole(v19))}";
            var ratio = last / v19;
            if (ratio >= 1.5)
            {
                p["callout_value"] = ratio.ToString("0.#", Inv) + "×";
                p["callout_label"] = "THE 2019 LEVEL";
                p["callout_color"] = cx.Colors["accent"];
                D(cx, row, r, "callout", Math.Round(ratio, 1, MidpointRounding.AwayFromZero), "son / 2019");
            }
            else
            {
                var diff = last - v19;
                p["callout_value"] = Signed(diff, money: false);
                p["callout_label"] = $"{(diff >= 0 ? "MORE" : "FEWER")} THAN {mn} 2019";
                p["callout_color"] = diff >= 0 ? cx.Colors["accent"] : cx.Colors["loss"];
                D(cx, row, r, "callout", diff, "son − 2019");
            }
        }
        else
        {
            p["ref_value"] = null;
            p["ref_label"] = "";
            cx.Warn($"{name}: ACTLISCOU {mn} 2019 değeri yok; referans çizgisi ve büyük rakam çizilmedi.");
        }
        return p;
    }

    /// Aynı seri, 2019'dan son yıla. En düşük yıl vurgulu (nötr), sonrası turuncu; ok dipten son yıla.
    static JsonObject? StockBars(Row row, CountyResult r, Context cx)
    {
        if (!FredMonth(row, r, cx, out var m, out var y, out var mn)) return null;
        var name = ShortName(r.County.Name);
        var pts = Yearly(r.Data.F("ACTLISCOU"), m, 2019, y, name, "ACTLISCOU", cx);
        if (pts.Count < 3) { cx.Warn($"{name}: stock_years_bars için üçten az yıl var, grafik atlandı."); return null; }
        var low = pts.First(p => p.Value == pts.Min(q => q.Value));
        var last = pts[^1];
        bool lowIsLast = low.Year == last.Year;
        var sub = cx.FocusSub(r.County.Fips);
        var p = Base(row, r, cx);
        p["title"] = "HOMES FOR SALE";
        p["subtitle"] = sub.Length > 0 ? $"EVERY {mn}  ·  {sub}" : $"EVERY {mn}";
        p["source"] = "SOURCE: REALTOR.COM VIA FRED";
        // dip son yılsa ok yok ve hepsi nötr: son satır işaretlenir, işaret rengi nötr (öncekiler zaten nötr)
        var mark = lowIsLast ? last.Year : low.Year;
        p["bars"] = Table(pts.Select(x => (x.Year.ToString(Inv), Whole(x.Value), x.Year == mark)));
        p["value_format"] = "count";
        p["highlight_color"] = cx.Colors["neutral"];
        p["after_color"] = cx.Colors["accent"];
        p["arrow_color"] = cx.Colors["price"];
        p["arrow_from"] = lowIsLast ? "" : low.Year.ToString(Inv);
        p["arrow_to"] = lowIsLast ? "" : last.Year.ToString(Inv);
        foreach (var x in pts) D(cx, row, r, x.Year.ToString(Inv), x.Value, $"FRED ACTLISCOU {mn}");
        var v19 = pts.Where(x => x.Year == 2019).Select(x => (double?)x.Value).FirstOrDefault();
        if (v19 is double b)
        {
            var diff = last.Value - b;
            p["callout_value"] = Signed(diff, money: false);
            p["callout_label"] = $"{(diff >= 0 ? "MORE" : "FEWER")} THAN {mn} 2019";
            p["callout_color"] = diff >= 0 ? cx.Colors["accent"] : cx.Colors["loss"];
            D(cx, row, r, "callout", diff, "son − 2019");
        }
        return p;
    }

    /// Fiyat kıran pay = PRIREDCOU / ACTLISCOU × 100, FRED ayı: county, eyalet, ABD.
    static JsonObject? CutShare(Row row, CountyResult r, Context cx)
    {
        if (!FredMonth(row, r, cx, out var m, out var y, out var mn)) return null;
        var mo = new DateOnly(y, m, 1);
        var def = $"PRIREDCOU / ACTLISCOU × 100, {mn} {y}";
        var rows = new List<(string, double, bool)>();
        if (r.CutShare is double c) { rows.Add((Label(r, full: true), Two(c), true)); D(cx, row, r, "county", c, def); }
        var st = Analyzer.At(Analyzer.ShareSeries(cx.Snap.StateData), mo);
        if (st is double s) { rows.Add((cx.StateName, Two(s), false)); D(cx, row, r, "eyalet", s, def); }
        else cx.Warn($"{cx.StateName}: {mn} {y} eyalet fiyat kıran payı yok.");
        var us = Analyzer.At(Analyzer.ShareSeries(cx.Snap.UsData), mo);
        if (us is double u) { rows.Add(("UNITED STATES", Two(u), false)); D(cx, row, r, "ABD", u, def); }
        else cx.Warn($"ABD: {mn} {y} fiyat kıran payı yok.");
        if (rows.Count < 2 || r.CutShare == null) { cx.Warn($"{ShortName(r.County.Name)}: cut_share_compare için veri yetmedi, grafik atlandı."); return null; }
        var p = Base(row, r, cx);
        p["title"] = "SELLERS WHO CUT THEIR PRICE";
        p["subtitle"] = $"OUT OF EVERY 100 HOMES FOR SALE  ·  {mn} {y}";
        p["source"] = "SOURCE: REALTOR.COM VIA FRED";
        p["rows"] = Table(rows);
        p["max_value"] = 100;
        p["value_suffix"] = " OF 100";
        p["decimals"] = 0;
        p["threshold"] = null;
        p["threshold_label"] = "";
        return p;
    }

    /// Redfin avg_sale_to_list × 100, Redfin ayı (county'nin SaleToList değeri).
    static JsonObject? SaleToList(Row row, CountyResult r, Context cx)
    {
        if (!RedfinMonth(row, r, cx, out _, out var y, out var mn)) return null;
        var name = ShortName(r.County.Name);
        if (r.SaleToList is not double v) { cx.Warn($"{name}: satış/liste değeri yok, sale_to_list_ring atlandı."); return null; }
        if (v > 100) { cx.Warn($"{name}: satış/liste {v:0.0} (100'ün üstünde, istenen fiyattan fazla ödeniyor); halka çizilemez, grafik atlandı."); return null; }
        CheckRedfin(r.Data.R("avg_sale_to_list"), r.RedfinMonth, name, "avg_sale_to_list", cx);
        var sub = cx.FocusSub(r.County.Fips);
        var p = Base(row, r, cx);
        p["title"] = "WHAT BUYERS REALLY PAY";
        p["subtitle"] = sub.Length > 0 ? $"{sub}  ·  {mn} {y} SALES" : $"{mn} {y} SALES";
        p["source"] = "SOURCE: REDFIN";
        p["value"] = Two(v);
        p["max_value"] = 100;
        p["center_prefix"] = "$";
        p["center_label"] = "OF EVERY $100 ASKED";
        p["remainder_value"] = "";
        p["remainder_label"] = "AT THE TABLE";
        D(cx, row, r, "value", v, $"Redfin avg_sale_to_list × 100, {mn} {y}");
        return p;
    }

    static int ThermoMax(double top) => Math.Max(8, (int)Math.Ceiling(top));

    /// County MonthsSupply ve eyalet MonthsSupplyState, Redfin ayı.
    static JsonObject? Thermo(Row row, CountyResult r, Context cx)
    {
        if (!RedfinMonth(row, r, cx, out _, out var y, out var mn)) return null;
        var name = ShortName(r.County.Name);
        if (r.MonthsSupply is not double ms) { cx.Warn($"{name}: aylık stok yok, months_supply_thermo atlandı."); return null; }
        CheckRedfin(r.Data.R("months_of_supply"), r.RedfinMonth, name, "months_of_supply", cx);
        CheckRedfin(cx.Snap.StateData.R("months_of_supply"), r.RedfinMonth, cx.StateName, "months_of_supply", cx);
        var def = $"Redfin months_of_supply, {mn} {y}";
        var tubes = new List<(string, double, bool)> { (Label(r, full: false), Two(ms), false) };
        D(cx, row, r, "county", ms, def);
        if (r.MonthsSupplyState is double st) { tubes.Add((cx.StateName, Two(st), false)); D(cx, row, r, "eyalet", st, def); }
        else cx.Warn($"{cx.StateName}: {mn} {y} eyalet aylık stoku yok, termometrede yalnız county var.");
        var p = Base(row, r, cx);
        p["title"] = "HOW LONG TO SELL EVERY HOME FOR SALE";
        p["subtitle"] = $"IF NO NEW HOME WERE LISTED  ·  {mn} {y}";
        p["source"] = "SOURCE: REDFIN";
        p["tubes"] = Table(tubes);
        p["max_value"] = ThermoMax(tubes.Max(t => t.Item2));
        p["unit_label"] = "MONTHS";
        p["decimals"] = 1;
        return p;
    }

    /// Metin dosyasındaki video county'lerinin MonthsSupply'ı, büyükten küçüğe. Eşik 6.
    static JsonObject? Rank(Row row, Context cx)
    {
        var rows = new List<(string Label, double Value, bool)>();
        string month = "";
        foreach (var t in cx.Texts)
        {
            var r = cx.County(t.Fips);
            if (r == null) continue;
            if (r.MonthsSupply is not double ms) { cx.Warn($"{ShortName(r.County.Name)}: aylık stok yok, sıralamaya girmedi."); continue; }
            CheckRedfin(r.Data.R("months_of_supply"), r.RedfinMonth, ShortName(r.County.Name), "months_of_supply", cx);
            rows.Add((Label(r, full: false), Two(ms), false));
            if (string.CompareOrdinal(r.RedfinMonth, month) > 0) month = r.RedfinMonth;
            D(cx, row, r, "value", ms, $"Redfin months_of_supply, {MonthText(r.RedfinMonth)}");
        }
        if (rows.Count < 2) { cx.Warn("months_supply_rank: en az iki county'nin aylık stoku gerekli, grafik atlandı."); return null; }
        rows = rows.OrderByDescending(x => x.Value).ToList();
        if (rows.Count > 10) { cx.Warn("months_supply_rank: en fazla 10 county çizilir, en yüksek 10'u alındı."); rows = rows.Take(10).ToList(); }
        TryMonth(month, out _, out var y, out var mn);
        var p = Base(row, null, cx);
        p["heading"] = $"{cx.StateName}  ·  {rows.Count} COUNTIES";
        p["title"] = "HOW MANY MONTHS TO SELL EVERY HOME";
        p["subtitle"] = $"IF NO NEW HOME WERE LISTED  ·  {mn} {y}";
        p["source"] = "SOURCE: REDFIN";
        p["rows"] = Table(rows);
        p["max_value"] = ThermoMax(rows.Max(x => x.Value));
        p["value_suffix"] = "";
        p["decimals"] = 1;
        p["threshold"] = 6;
        p["threshold_label"] = "BUYER'S MARKET: 6+ MONTHS";
        return p;
    }

    /// FRED ACTLISCOU: geçen yılın ve bu yılın FRED ayı.
    static JsonObject? Grid(Row row, CountyResult r, Context cx)
    {
        if (!FredMonth(row, r, cx, out var m, out var y, out var mn)) return null;
        var name = ShortName(r.County.Name);
        var pts = Yearly(r.Data.F("ACTLISCOU"), m, y - 1, y, name, "ACTLISCOU", cx);
        if (pts.Count < 2 || pts[0].Year != y - 1) { cx.Warn($"{name}: ACTLISCOU {mn} {y - 1} ya da {y} yok, stock_change_grid atlandı."); return null; }
        var p = Base(row, r, cx);
        p["title"] = "HOMES FOR SALE";
        p["subtitle"] = cx.FocusSub(r.County.Fips);
        p["source"] = $"SOURCE: REALTOR.COM VIA FRED  ·  {mn} {y - 1} AND {mn} {y}";
        p["before"] = (int)Whole(pts[0].Value);
        p["after"] = (int)Whole(pts[1].Value);
        p["unit"] = null;
        p["before_label"] = $"{mn} {y - 1}";
        p["after_label"] = $"{mn} {y}";
        p["result_text"] = "";
        D(cx, row, r, "before", pts[0].Value, $"FRED ACTLISCOU {mn} {y - 1}");
        D(cx, row, r, "after", pts[1].Value, $"FRED ACTLISCOU {mn} {y}");
        return p;
    }

    /// County soru kartı: metrics sırasıyla en fazla üç satır; açılış anları stüdyonun varsayılanı (3,4 / 5,0 / 6,6 sn).
    static JsonObject? Quiz(Row row, CountyResult r, Context cx)
    {
        var name = ShortName(r.County.Name);
        var p = Base(row, r, cx);
        p["subtitle"] = cx.FocusSub(r.County.Fips);
        TryMonth(r.Month, out var fm, out var fy, out var fmn);
        TryMonth(r.RedfinMonth, out var rm, out var ry, out var rmn);
        var metrics = row.Metrics.Split(';', StringSplitOptions.RemoveEmptyEntries);
        int k = 0;
        foreach (var metric in metrics)
        {
            var q = QuizRow(metric, row, r, cx, name, fm, fy, fmn, rm, ry, rmn);
            if (q == null) continue;
            k++;
            foreach (var (key, value) in q) p[$"row{k}_{key}"] = value?.DeepClone();
        }
        if (k == 0) { cx.Warn($"{name}: county_quiz ölçülerinin hiçbirinde veri yok, grafik atlandı."); return null; }
        for (int j = k + 1; j <= 3; j++) p[$"row{j}_kind"] = "none";   // stüdyonun örnek satırları görünmesin
        return p;
    }

    static JsonObject? QuizRow(string metric, Row row, CountyResult r, Context cx, string name,
        int fm, int fy, string fmn, int rm, int ry, string rmn)
    {
        JsonObject Counter(double value, string label, string note, int decimals = 0, string prefix = "") => new()
        {
            ["kind"] = "counter", ["label"] = label, ["note"] = note, ["value"] = value, ["value2"] = null,
            ["value2_label"] = "", ["value_label"] = "", ["format"] = "count", ["decimals"] = decimals, ["prefix"] = prefix,
        };
        JsonObject? Missing(string what) { cx.Warn($"{name}: county_quiz '{metric}' için {what} yok, satır atlandı."); return null; }
        bool fred = fy > 0, redfin = ry > 0;

        switch (metric)
        {
            case "homes_for_sale":
                if (!fred || r.Active is not double a) return Missing("FRED satılık ev sayısı");
                D(cx, row, r, metric, a, $"FRED ACTLISCOU {fmn} {fy}");
                return Counter(Whole(a), "HOMES FOR SALE", $"{fmn} {fy}");
            case "cut_share_in10":
                if (!fred || r.CutShare is not double c) return Missing("fiyat kıran payı");
                var n10 = (int)Math.Clamp(Math.Round(c / 10, MidpointRounding.AwayFromZero), 0, 10);
                D(cx, row, r, metric, n10, $"PRIREDCOU / ACTLISCOU × 100 / 10, {fmn} {fy} (pay {c.ToString("0.0", Inv)})");
                return new JsonObject
                {
                    ["kind"] = "in10", ["label"] = "SELLERS WHO CUT THEIR PRICE", ["note"] = $"{fmn} {fy}", ["value"] = n10,
                    ["value2"] = null, ["value2_label"] = "", ["value_label"] = "", ["format"] = "count",
                };
            case "months_supply":
                if (!redfin || r.MonthsSupply is not double ms) return Missing("aylık stok");
                CheckRedfin(r.Data.R("months_of_supply"), r.RedfinMonth, name, "months_of_supply", cx);
                D(cx, row, r, metric, ms, $"Redfin months_of_supply, {rmn} {ry}");
                return Counter(Two(ms), "MONTHS TO SELL EVERY HOME", $"{rmn} {ry}", decimals: 1);
            case "sale_to_list":
                if (!redfin || r.SaleToList is not double stl) return Missing("satış/liste oranı");
                CheckRedfin(r.Data.R("avg_sale_to_list"), r.RedfinMonth, name, "avg_sale_to_list", cx);
                D(cx, row, r, metric, Whole(stl), $"Redfin avg_sale_to_list × 100, {rmn} {ry} (değer {stl.ToString("0.0", Inv)})");
                return Counter(Whole(stl), "BUYERS PAY PER $100 ASKED", $"{rmn} {ry} SALES", prefix: "$");
            case "sale_price_vs_peak":
            {
                if (!redfin) return Missing("Redfin satış fiyatı");
                var sp = r.Data.R("median_sale_price");
                CheckRedfin(sp, r.RedfinMonth, name, "median_sale_price", cx);
                var pts = Yearly(sp, rm, 2019, ry, name, "median_sale_price", cx);
                if (pts.Count < 2) return Missing("yıllık satış fiyatı");
                var peak = pts.First(x => x.Value == pts.Max(z => z.Value));
                var last = pts[^1];
                D(cx, row, r, $"{metric} zirve {peak.Year}", peak.Value, $"Redfin median_sale_price {rmn}");
                D(cx, row, r, $"{metric} {last.Year}", last.Value, $"Redfin median_sale_price {rmn}");
                return new JsonObject
                {
                    ["kind"] = "compare", ["label"] = "WHAT BUYERS PAY", ["note"] = $"{rmn} {ry} SALES", ["value"] = Whole(last.Value),
                    ["value2"] = Whole(peak.Value), ["value2_label"] = $"{peak.Year} PEAK", ["value_label"] = "TODAY", ["format"] = "money_k",
                };
            }
            case "stock_vs_2019":
            {
                if (!fred) return Missing("FRED satılık ev serisi");
                var pts = Yearly(r.Data.F("ACTLISCOU"), fm, 2019, fy, name, "ACTLISCOU", cx);
                if (pts.Count < 2 || pts[0].Year != 2019) return Missing($"ACTLISCOU {fmn} 2019 değeri");
                D(cx, row, r, $"{metric} 2019", pts[0].Value, $"FRED ACTLISCOU {fmn}");
                D(cx, row, r, $"{metric} {pts[^1].Year}", pts[^1].Value, $"FRED ACTLISCOU {fmn}");
                return new JsonObject
                {
                    ["kind"] = "compare", ["label"] = "HOMES FOR SALE", ["note"] = $"{fmn} {fy}", ["value"] = Whole(pts[^1].Value),
                    ["value2"] = Whole(pts[0].Value), ["value2_label"] = "2019", ["value_label"] = "NOW", ["format"] = "count",
                };
            }
        }
        return null;
    }

    /// out\grafik_degerleri_{ST}.csv: seq, chart, fips, county, alan, değer, tanım.
    public static void WriteDump(string path, List<Dump> dumps)
    {
        var sb = new StringBuilder("seq,chart,fips,county,alan,değer,tanım\r\n");
        foreach (var d in dumps)
            sb.Append(string.Join(",", new[] { d.Seq.ToString(Inv), d.Chart, d.Fips, d.County, d.Field, d.Value, d.Definition }.Select(Quote))).Append("\r\n");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));   // Excel Türkçe karakterleri doğru açsın
    }
}
