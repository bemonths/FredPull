using System.Globalization;
using System.Text;

namespace FredPull;

public record ChartMetric(string Key, string Name, string Unit, string Source, bool CompareState, bool CompareUs, bool Line2019, bool ShowPeak);

public static class Analyzer
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static readonly DateOnly HistoryStart = new(2016, 1, 1);

    /// FRED (Realtor.com) seri kökleri; ilçe için + FIPS, eyalet için + "FL", ülke için + "US"
    public static readonly string[] FredKeys = { "ACTLISCOU", "NEWLISCOU", "PENLISCOU", "MEDDAYONMAR", "PRIREDCOU", "MEDLISPRI" };

    /// Redfin market tracker sütunları
    public static readonly string[] RedfinKeys = { "homes_sold", "median_sale_price", "avg_sale_to_list", "months_of_supply", "inventory", "price_drops", "pending_sales", "median_dom", "new_listings" };

    public static readonly ChartMetric[] Metrics =
    {
        new("ACTLISCOU",        "Satılık ev sayısı",              "ilan",  "fred",   false, false, true,  false),
        new("NEWLISCOU",        "Yeni ilan sayısı",               "ilan",  "fred",   false, false, true,  false),
        new("PENLISCOU",        "Sözleşmeye bağlanan ev",         "ilan",  "fred",   false, false, true,  false),
        new("MEDDAYONMAR",      "İlan süresi",                    "gün",   "fred",   true,  true,  false, false),
        new("SHARE",            "Fiyat kıran satıcı payı",        "%",     "share",  true,  true,  false, false),
        new("MEDLISPRI",        "İstenen fiyat (medyan)",         "$",     "fred",   false, false, false, true),
        new("median_sale_price","Gerçekleşen satış fiyatı",       "$",     "redfin", false, false, false, true),
        new("homes_sold",       "Satılan ev sayısı",              "ev",    "redfin", false, false, true,  false),
        new("months_of_supply", "Aylık stok",                     "ay",    "redfin", true,  true,  false, false),
        new("SALETOLIST",       "Satış / liste fiyatı",           "%",     "redfin", true,  true,  false, false),
    };

    public const string Glossary =
        "TANIMLAR\n" +
        "Satılık ev sayısı: o ay ilanda olan ev sayısı (Realtor.com). 2019'a göre: pandemi öncesi aynı ayla kıyas, 'normal' ölçüsü.\n" +
        "Sözleşmeye bağlanan ev: satıcı ile alıcının anlaştığı, tapu bekleyen ev sayısı (Realtor.com pending). Satış verisi yoksa satış yerine bu kullanılır.\n" +
        "Fiyat kıran satıcı payı: o ay fiyatını indiren ilan / satılık ev. Eyalet ve ABD ile kıyaslanır; tek başına anlamsızdır.\n" +
        "İstenen fiyat: satıcının ilana yazdığı fiyatın ortancası. Gerçekleşen satış fiyatı: tapuda ödenen fiyatın ortancası (Redfin).\n" +
        "Zirveden: son 3 ayın ortalaması, geçmişteki en yüksek 3 aylık ortalamayla kıyaslanır.\n" +
        "Satış / liste fiyatı: ödenen fiyat, istenen fiyatın yüzde kaçı. 100 altı pazarlıkla düşmüş demek.\n" +
        "Aylık stok: satılık ev / aylık satış. 6 ay ve üstü alıcı piyasası, 3 ay altı satıcı piyasası (sektör standardı).\n\n" +
        "SKOR (0-100, yüksek = daha kırık piyasa)\n" +
        "Aylık stok 6+ → +25, 4-6 → +15, 3-4 → +5\n" +
        "Satış (yoksa sözleşme) geçen yıla göre -20% ve altı → +20, -10% ve altı → +10\n" +
        "Satış fiyatı (yoksa istenen) zirveden -15% ve altı → +20, -8% → +12, -3% → +5\n" +
        "Satılık ev 2019'a göre +50% ve üstü → +15, +20% → +8\n" +
        "Fiyat kıran satıcı payı eyaletin 8 puan üstü → +10, 3 puan → +5\n" +
        "Satış / liste %95 ve altı → +10, %97 ve altı → +5\n" +
        "Eşik aşılmayan ilçe 0 puan alır; 'Dengeli' sinyali verilir ve video konusu değildir.\n\n" +
        "SİNYAL\n" +
        "Alıcı çekildi: aylık stok 7+ ya da satış düşüyor ve stok birikiyor. Satıcı çekiliyor: satılık ev ve yeni ilan birlikte azalıyor, fiyat düşüyor. " +
        "Fiyat kırılıyor: satış sürüyor ama fiyat zirveden belirgin aşağıda. Sıcak: fiyat artıyor, stok az. Zayıflıyor: birkaç eşik aşılmış ama kırılma yok. Dengeli: ortalamaya yakın. " +
        "Küçük taban: satılık ev 300'ün ya da aylık satış 40'ın altında; yüzdeler güvenilmez, sıralamada en alta iner.";

    // ---------- seri yardımcıları ----------

    public static double? At(List<Obs>? s, DateOnly d) => s?.FirstOrDefault(o => o.Date == d)?.Value;
    public static DateOnly? Last(List<Obs>? s) => s?.LastOrDefault(o => o.Value.HasValue)?.Date;
    public static double? Pct(double? cur, double? prev) => (cur.HasValue && prev is > 0) ? (cur - prev) / prev * 100 : null;
    public static double? Yoy(List<Obs>? s, DateOnly d) => Pct(At(s, d), At(s, d.AddMonths(-12)));
    public static double? Diff(List<Obs>? s, DateOnly d)
    {
        var a = At(s, d); var b = At(s, d.AddMonths(-12));
        return (a.HasValue && b.HasValue) ? a - b : null;
    }
    public static double? Vs2019(List<Obs>? s, DateOnly d) => d.Year <= 2019 ? null : Pct(At(s, d), At(s, new DateOnly(2019, d.Month, 1)));

    /// Son 3 ayın ortalamasının, geçmişteki en yüksek 3 aylık ortalamaya göre % farkı ve zirve ayı.
    public static (double? Pct, DateOnly? PeakDate) FromPeak(List<Obs>? s, DateOnly d)
    {
        if (s == null) return (null, null);
        var v = s.Where(o => o.Value.HasValue && o.Date <= d).OrderBy(o => o.Date).ToList();
        if (v.Count < 6) return (null, null);
        double Avg3(int i) => (v[i].Value!.Value + v[i - 1].Value!.Value + v[i - 2].Value!.Value) / 3.0;
        double cur = Avg3(v.Count - 1);
        double peak = double.MinValue; int pi = -1;
        for (int i = 2; i < v.Count; i++)
        {
            var a = Avg3(i);
            if (a > peak) { peak = a; pi = i; }
        }
        if (peak <= 0 || pi < 0) return (null, null);
        return ((cur - peak) / peak * 100, v[pi].Date);
    }

    /// Fiyat kıran satıcı payı serisi (%): PRIREDCOU / ACTLISCOU × 100
    public static List<Obs> ShareSeries(SeriesSet set)
    {
        var list = new List<Obs>();
        var act = set.F("ACTLISCOU"); var red = set.F("PRIREDCOU");
        if (act == null || red == null) return list;
        var redByDate = red.Where(o => o.Value.HasValue).ToDictionary(o => o.Date, o => o.Value!.Value);
        foreach (var a in act)
            if (a.Value is > 0 && redByDate.TryGetValue(a.Date, out var r))
                list.Add(new Obs(a.Date, r / a.Value.Value * 100));
        return list;
    }

    /// Grafik için seri: metrik tanımına göre FRED, Redfin veya türetilmiş.
    public static List<Obs>? MetricSeries(SeriesSet set, ChartMetric m)
    {
        switch (m.Source)
        {
            case "share": return ShareSeries(set);
            case "fred": return set.F(m.Key);
            case "redfin":
                if (m.Key == "SALETOLIST")
                    return set.R("avg_sale_to_list")?.Select(o => new Obs(o.Date, o.Value.HasValue ? o.Value * 100 : null)).ToList();
                return set.R(m.Key);
        }
        return null;
    }

    // ---------- özetleme ----------

    public static void Summarize(CountyResult r, SeriesSet st, SeriesSet us)
    {
        var d = r.Data;
        var act = d.F("ACTLISCOU");
        var last = Last(act);
        if (last == null) return;
        var mo = last.Value;
        r.Month = mo.ToString("yyyy-MM", Inv);

        r.Active = At(act, mo);
        r.ActiveYoY = Yoy(act, mo);
        r.ActiveVs2019 = Vs2019(act, mo);
        r.NewYoY = Yoy(d.F("NEWLISCOU"), mo);
        r.PendingYoY = Yoy(d.F("PENLISCOU"), mo);
        r.Dom = At(d.F("MEDDAYONMAR"), mo);
        r.DomYoY = Diff(d.F("MEDDAYONMAR"), mo);

        var share = ShareSeries(d);
        r.CutShare = At(share, mo);
        r.CutShareYoY = Diff(share, mo);
        var stShare = At(ShareSeries(st), mo);
        var usShare = At(ShareSeries(us), mo);
        r.CutShareVsState = (r.CutShare.HasValue && stShare.HasValue) ? r.CutShare - stShare : null;
        r.CutShareVsUs = (r.CutShare.HasValue && usShare.HasValue) ? r.CutShare - usShare : null;

        var lp = d.F("MEDLISPRI");
        r.ListPrice = At(lp, mo);
        r.ListPriceYoY = Yoy(lp, mo);
        var (lpPeak, lpPeakDate) = FromPeak(lp, mo);
        r.ListPriceFromPeak = lpPeak;

        r.RedfinMonth = "";
        r.Sold = null; r.SoldYoY = null; r.SalePrice = null; r.SalePriceYoY = null;
        r.SalePriceFromPeak = null; r.SaleToList = null; r.MonthsSupply = null; r.MonthsSupplyState = null;

        var sold = d.R("homes_sold");
        var rlast = Last(sold);
        if (rlast != null)
        {
            var ro = rlast.Value;
            r.RedfinMonth = ro.ToString("yyyy-MM", Inv);
            r.Sold = At(sold, ro);
            r.SoldYoY = Yoy(sold, ro);
            var sp = d.R("median_sale_price");
            r.SalePrice = At(sp, ro);
            r.SalePriceYoY = Yoy(sp, ro);
            var (spPeak, spPeakDate) = FromPeak(sp, ro);
            r.SalePriceFromPeak = spPeak;
            r.PeakLabel = (spPeakDate ?? lpPeakDate)?.ToString("yyyy-MM", Inv);
            var stl = At(d.R("avg_sale_to_list"), ro);
            r.SaleToList = stl.HasValue ? stl * 100 : null;
            r.MonthsSupply = At(d.R("months_of_supply"), ro);
            r.MonthsSupplyState = At(st.R("months_of_supply"), ro);
        }
        else
        {
            r.PeakLabel = lpPeakDate?.ToString("yyyy-MM", Inv);
        }

        ComputeScore(r);
        ComputeSignal(r);
        r.Reading = BuildReading(r, stShare, usShare);

        // Az ilanlı ilçede yüzdeler birkaç evle oynar: skor kalır, sinyal ve sıralama düşer.
        if (r.Active is < 300 || r.Sold is < 40)
        {
            r.Signal = "Küçük taban";
            r.Reading += " Uyarı: az ilanlı county, yüzdeler güvenilmez.";
        }
    }

    static void ComputeScore(CountyResult r)
    {
        int s = 0;
        var parts = new List<string>();
        void Add(int pts, string why) { s += pts; parts.Add($"+{pts,-3} {why}"); }

        if (r.MonthsSupply is >= 6) Add(25, $"aylık stok {r.MonthsSupply:0.0} (6 ve üstü: alıcı piyasası)");
        else if (r.MonthsSupply is >= 4) Add(15, $"aylık stok {r.MonthsSupply:0.0} (4-6: alıcı lehine)");
        else if (r.MonthsSupply is >= 3) Add(5, $"aylık stok {r.MonthsSupply:0.0} (3-4: dengeli-gevşek)");

        var salesYoY = r.SoldYoY ?? r.PendingYoY;
        var salesLabel = r.SoldYoY.HasValue ? "satılan ev" : "sözleşmeye bağlanan ev";
        if (salesYoY is <= -20) Add(20, $"{salesLabel} geçen yıla göre {salesYoY:0}%");
        else if (salesYoY is <= -10) Add(10, $"{salesLabel} geçen yıla göre {salesYoY:0}%");

        var priceDrop = r.SalePriceFromPeak ?? r.ListPriceFromPeak;
        var priceLabel = r.SalePriceFromPeak.HasValue ? "satış fiyatı" : "istenen fiyat";
        if (priceDrop is <= -15) Add(20, $"{priceLabel} zirveden {priceDrop:0}%");
        else if (priceDrop is <= -8) Add(12, $"{priceLabel} zirveden {priceDrop:0}%");
        else if (priceDrop is <= -3) Add(5, $"{priceLabel} zirveden {priceDrop:0}%");

        if (r.ActiveVs2019 is >= 50) Add(15, $"satılık ev 2019'a göre +{r.ActiveVs2019:0}%");
        else if (r.ActiveVs2019 is >= 20) Add(8, $"satılık ev 2019'a göre +{r.ActiveVs2019:0}%");

        if (r.CutShareVsState is >= 8) Add(10, $"fiyat kıran satıcı payı eyaletin {r.CutShareVsState:0} puan üstünde");
        else if (r.CutShareVsState is >= 3) Add(5, $"fiyat kıran satıcı payı eyaletin {r.CutShareVsState:0} puan üstünde");

        if (r.SaleToList is <= 95) Add(10, $"satış fiyatı liste fiyatının %{r.SaleToList:0.0}'i");
        else if (r.SaleToList is <= 97) Add(5, $"satış fiyatı liste fiyatının %{r.SaleToList:0.0}'i");

        r.Score = Math.Min(100, s);
        r.ScoreBreakdown = parts.Count == 0 ? "Hiçbir eşik aşılmadı: 0 puan." : string.Join("\n", parts);
    }

    static void ComputeSignal(CountyResult r)
    {
        double salesYoY = r.SoldYoY ?? r.PendingYoY ?? 0;
        double priceYoY = r.SalePriceYoY ?? r.ListPriceYoY ?? 0;
        double priceDrop = r.SalePriceFromPeak ?? r.ListPriceFromPeak ?? 0;
        bool stockHigh = (r.MonthsSupply ?? 0) >= 5 || (r.ActiveVs2019 ?? 0) >= 20 || (r.ActiveYoY ?? 0) >= 10;

        if ((r.MonthsSupply ?? 0) >= 7) r.Signal = "Alıcı çekildi";
        else if (salesYoY <= -10 && stockHigh) r.Signal = "Alıcı çekildi";
        else if ((r.ActiveYoY ?? 0) <= -15 && (r.NewYoY ?? 0) <= -8 && priceYoY < 0) r.Signal = "Satıcı çekiliyor";
        else if (priceDrop <= -8 && salesYoY > -10) r.Signal = "Fiyat kırılıyor";
        else if (priceYoY >= 3 && (r.MonthsSupply ?? 0) < 3) r.Signal = "Sıcak";
        else if (r.Score >= 40) r.Signal = "Zayıflıyor";
        else r.Signal = "Dengeli";
    }

    static string BuildReading(CountyResult r, double? stShare, double? usShare)
    {
        var sb = new StringBuilder();

        if (r.Active.HasValue)
        {
            sb.Append($"{MonthName(r.Month)} ayında {r.Active.Value.ToString("N0", Inv)} ev satılıktı");
            if (r.ActiveVs2019.HasValue) sb.Append($"; pandemi öncesi 2019'un aynı ayına göre {Signed(r.ActiveVs2019)}%");
            if (r.ActiveYoY.HasValue) sb.Append($", geçen yıla göre {Signed(r.ActiveYoY)}%");
            sb.Append(". ");
        }

        if (r.Sold.HasValue)
            sb.Append($"{MonthName(r.RedfinMonth)} ayında {r.Sold.Value.ToString("N0", Inv)} ev satıldı, geçen yıla göre {Signed(r.SoldYoY)}%. ");
        else if (r.PendingYoY.HasValue)
            sb.Append($"Sözleşmeye bağlanan ev sayısı geçen yıla göre {Signed(r.PendingYoY)}% (satış verisi yok, sözleşme sayısı kullanıldı). ");

        if (r.SalePrice.HasValue)
        {
            sb.Append($"Gerçekleşen satış fiyatı (medyan) {r.SalePrice.Value.ToString("N0", Inv)} $, geçen yıla göre {Signed(r.SalePriceYoY)}%");
            if (r.SalePriceFromPeak.HasValue) sb.Append($", {r.PeakLabel} zirvesine göre {Signed(r.SalePriceFromPeak)}%");
            sb.Append(". ");
        }
        else if (r.ListPrice.HasValue)
        {
            sb.Append($"İstenen fiyat (medyan) {r.ListPrice.Value.ToString("N0", Inv)} $, geçen yıla göre {Signed(r.ListPriceYoY)}%");
            if (r.ListPriceFromPeak.HasValue) sb.Append($", {r.PeakLabel} zirvesine göre {Signed(r.ListPriceFromPeak)}%");
            sb.Append(". ");
        }

        if (r.CutShare.HasValue)
        {
            sb.Append($"Satıcıların yüzde {r.CutShare:0}'i bu ay fiyat kırdı");
            var cmp = new List<string>();
            if (stShare.HasValue) cmp.Add($"eyalet yüzde {stShare:0}");
            if (usShare.HasValue) cmp.Add($"ABD yüzde {usShare:0}");
            if (cmp.Count > 0) sb.Append($" ({string.Join(", ", cmp)})");
            sb.Append(". ");
        }

        if (r.MonthsSupply.HasValue)
        {
            sb.Append($"Mevcut stok {r.MonthsSupply:0.0} aylık satışa yetiyor");
            if (r.MonthsSupplyState.HasValue) sb.Append($" (eyalet {r.MonthsSupplyState:0.0})");
            sb.Append(". ");
        }

        sb.Append(SignalSentence(r.Signal, r.SoldYoY ?? r.PendingYoY ?? 0));
        return sb.ToString();
    }

    static string SignalSentence(string signal, double salesYoY) => signal switch
    {
        "Alıcı çekildi" when salesYoY > -10
                            => "Sonuç: stok 7 aylık satışın üstünde; alıcı çekilmiş, piyasa alıcının elinde.",
        "Alıcı çekildi"     => "Sonuç: satış düşerken stok birikiyor; bu, alıcının çekildiği bir piyasa.",
        "Satıcı çekiliyor"  => "Sonuç: satılık ev ve yeni ilan birlikte azalırken fiyat düşüyor; evler satıldığı için değil, satıcılar ilanı çektiği için azalıyor.",
        "Fiyat kırılıyor"   => "Sonuç: satış hızı korunuyor ama fiyat zirveden belirgin aşağıda; alıcı var, geçen yılın fiyatını ödemiyor.",
        "Sıcak"             => "Sonuç: fiyat artıyor ve stok az; satıcı piyasası.",
        "Zayıflıyor"        => "Sonuç: birden fazla gösterge kötüleşiyor ama kırılma eşiği aşılmadı; izlemeye değer, tek başına video konusu değil.",
        _                   => "Sonuç: göstergeler eyalet ve ülke ortalamasına yakın; burada anlatılacak bir olay yok."
    };

    public static string Signed(double? v) => v.HasValue ? v.Value.ToString("+0;-0;0", Inv) : "?";

    public static readonly string[] MonthNames ={ "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
    public static string MonthName(string yyyyMM)
    {
        if (yyyyMM.Length < 7 || !int.TryParse(yyyyMM.AsSpan(5, 2), out var m) || m < 1 || m > 12) return yyyyMM;
        return $"{MonthNames[m - 1]} {yyyyMM[..4]}";
    }
}
