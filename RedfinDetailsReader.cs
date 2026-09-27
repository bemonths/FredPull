using System.Text.Json;
using System.Text.RegularExpressions;

namespace FredPull;

/// İlan sayfasındaki ham ayrıntıları okur: ilan durumu, açıklama, emlak vergisi, aidat, Redfin'in FEMA bölgesi ve sel
/// sigortası tahmini, iklim riski puanları, ilanı veren emlakçı ve ofis. Değerler Redfin'in yazdığı gibi metin olarak
/// saklanır; hesap ve yorum yapılmaz. Her değerin bulunduğu yer Found'a yazılır. Yapı değişirse hata vermez, alan boş kalır.
///
/// Yollar 2026-09-27'de Florida'nın 10 gerçek ilan sayfasının gömülü bloklarından doğrulandı (out\redfin_raw\FL\):
///   payload.addressSectionInfo.status.displayValue                       → ilan durumu ("Active", "Sold")
///   payload.mainHouseInfo.mlsStatusDisplay.displayValue                   → ilan durumu (yedek)
///   payload.mainHouseInfo.marketingRemarks[0].marketingRemark             → açıklama
///   payload.mainHouseInfo.listingAgents[].agentInfo.agentName / brokerName → emlakçı / ofis
///   payload.publicRecordsInfo.taxInfo.taxesDue / rollYear                 → emlak vergisi / yılı
///   payload.publicRecordsInfo.mortgageCalculatorInfo.monthlyHoaDues       → Redfin hesaplayıcısının aylık aidatı
///   payload.amenitiesInfo.superGroups[].amenityGroups[] "HOA ..." grubu     → MLS aidat alanları (ASSOCIATION_FEE, _FREQUENCY)
///   payload.floodData.femaZones[] / lowInsurancePrice / highInsurancePrice / floodFactor → sel (riskFactor bloğu)
///   payload.fireData.fireFactor, heatData.heatFactor, windData.riskFactorScore, airData.riskFactorScore
/// "payload.homes" taşıyan bloklar (benzer/yakın evler) atlanır. Önceki sürümün anahtar adıyla gezinen okuyucusu kaldırıldı:
/// asıl bloklarda propertyId olmadığı için onları eliyor, bir sayfada da ortaklık reklamındaki emlak ofisini alıyordu.
public static class RedfinDetailsReader
{
    public static RedfinDetails Read(IReadOnlyList<string> bodies, string html, string url)
    {
        var d = new RedfinDetails { ReadAt = DateTime.Now };
        try
        {
            foreach (var b in bodies)
            {
                var t = b.TrimStart();
                if (t.StartsWith("{}&&", StringComparison.Ordinal)) t = t[4..];
                if (!t.StartsWith('{')) continue;
                JsonDocument doc;
                try { doc = JsonDocument.Parse(t); }
                catch (JsonException) { continue; }
                using (doc)
                {
                    if (!doc.RootElement.TryGetProperty("payload", out var p) || p.ValueKind != JsonValueKind.Object) continue;
                    if (p.TryGetProperty("homes", out _)) continue;           // benzer / yakın evler listesi
                    AddressSection(p, d);
                    MainHouse(p, d);
                    PublicRecords(p, d);
                    History(p, d);
                    Amenities(p, d);
                    RiskFactor(p, d);
                }
            }
            FromPageText(html, d);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }   // yapı değişirse boş kalır
        return d;
    }

    // ---------- gömülü bloklar ----------

    static JsonElement? Get(JsonElement e, params string[] path)
    {
        foreach (var k in path)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(k, out e)) return null;
        }
        return e;
    }

    static string? Scalar(JsonElement? v) => v?.ValueKind switch
    {
        JsonValueKind.String => v.Value.GetString()?.Trim() is { Length: > 0 } s ? s : null,
        JsonValueKind.Number => v.Value.GetRawText(),
        _ => null,
    };

    static void Set(RedfinDetails d, string field, string? value, string path, Func<string?> current, Action<string> set)
    {
        if (value == null || current() != null) return;
        set(value);
        d.Found[field] = path;
    }

    static string? UtcDay(JsonElement? ms) =>
        ms is { ValueKind: JsonValueKind.Number } v && v.TryGetInt64(out var t)
            ? DateTimeOffset.FromUnixTimeMilliseconds(t).UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : null;

    static void AddressSection(JsonElement p, RedfinDetails d)
    {
        if (Get(p, "addressSectionInfo") is not { ValueKind: JsonValueKind.Object } a) return;
        Set(d, "ListingStatus", Scalar(Get(a, "status", "displayValue")),
            "payload.addressSectionInfo.status.displayValue", () => d.ListingStatus, x => d.ListingStatus = x);
        Set(d, "SoldDate", UtcDay(Get(a, "soldDate")), "payload.addressSectionInfo.soldDate", () => d.SoldDate, x => d.SoldDate = x);
        Set(d, "PriceLabel", Scalar(Get(a, "latestPriceInfo", "label")), "payload.addressSectionInfo.latestPriceInfo.label", () => d.PriceLabel, x => d.PriceLabel = x);
        Set(d, "PriceAmount", Scalar(Get(a, "latestPriceInfo", "amount")), "payload.addressSectionInfo.latestPriceInfo.amount", () => d.PriceAmount, x => d.PriceAmount = x);
    }

    /// payload.propertyHistoryInfo.events: en yeni olay, en yeni satış ("Sold ...") ve en yeni sözleşme ("Pending",
    /// "Contingent", "Under Contract") olayı, yazıldığı gibi. Aday seçimindeki fiyat geçmişi hesabına dokunmaz.
    static void History(JsonElement p, RedfinDetails d)
    {
        if (d.LastEvent != null || Get(p, "propertyHistoryInfo", "events") is not { ValueKind: JsonValueKind.Array } events) return;
        var list = new List<(long T, RawHistoryEvent E)>();
        foreach (var e in events.EnumerateArray())
        {
            if (Get(e, "eventDate") is not { ValueKind: JsonValueKind.Number } t || !t.TryGetInt64(out var ms)) continue;
            list.Add((ms, new RawHistoryEvent
            {
                Date = UtcDay(t)!, Description = Scalar(Get(e, "eventDescription")) ?? "",
                Price = Scalar(Get(e, "price")), Source = Scalar(Get(e, "source")),
            }));
        }
        if (list.Count == 0) return;
        const string path = "payload.propertyHistoryInfo.events";
        RawHistoryEvent? Newest(Func<RawHistoryEvent, bool> f) =>
            list.Where(x => f(x.E)).OrderByDescending(x => x.T).Select(x => x.E).FirstOrDefault();   // eşitlikte dizideki ilk (Redfin sırası)
        d.LastEvent = Newest(_ => true);
        d.Found["LastEvent"] = path;
        if ((d.LastSaleEvent = Newest(x => x.Description.StartsWith("Sold", StringComparison.OrdinalIgnoreCase))) != null)
            d.Found["LastSaleEvent"] = path;
        if ((d.LastContractEvent = Newest(x => x.Description.Contains("Pending", StringComparison.OrdinalIgnoreCase)
                                              || x.Description.Contains("Contingent", StringComparison.OrdinalIgnoreCase)
                                              || x.Description.Contains("Under Contract", StringComparison.OrdinalIgnoreCase))) != null)
            d.Found["LastContractEvent"] = path;
    }

    static void MainHouse(JsonElement p, RedfinDetails d)
    {
        if (Get(p, "mainHouseInfo") is not { ValueKind: JsonValueKind.Object } m) return;
        Set(d, "ListingStatus", Scalar(Get(m, "mlsStatusDisplay", "displayValue")),
            "payload.mainHouseInfo.mlsStatusDisplay.displayValue", () => d.ListingStatus, x => d.ListingStatus = x);
        if (Get(m, "marketingRemarks") is { ValueKind: JsonValueKind.Array } rem && rem.GetArrayLength() > 0)
            Set(d, "Description", Scalar(Get(rem[0], "marketingRemark")),
                "payload.mainHouseInfo.marketingRemarks[0].marketingRemark", () => d.Description, x => d.Description = x);
        if (Get(m, "listingAgents") is { ValueKind: JsonValueKind.Array } agents)
        {
            var names = agents.EnumerateArray().Select(a => Scalar(Get(a, "agentInfo", "agentName"))).OfType<string>().Distinct().ToList();
            var offices = agents.EnumerateArray().Select(a => Scalar(Get(a, "brokerName"))).OfType<string>().Distinct().ToList();
            Set(d, "AgentName", names.Count > 0 ? string.Join("; ", names) : null,
                "payload.mainHouseInfo.listingAgents[].agentInfo.agentName", () => d.AgentName, x => d.AgentName = x);
            Set(d, "OfficeName", offices.Count > 0 ? string.Join("; ", offices) : null,
                "payload.mainHouseInfo.listingAgents[].brokerName", () => d.OfficeName, x => d.OfficeName = x);
        }
    }

    static void PublicRecords(JsonElement p, RedfinDetails d)
    {
        if (Get(p, "publicRecordsInfo") is not { ValueKind: JsonValueKind.Object } pr) return;
        Set(d, "PropertyTax", Scalar(Get(pr, "taxInfo", "taxesDue")),
            "payload.publicRecordsInfo.taxInfo.taxesDue", () => d.PropertyTax, x => d.PropertyTax = x);
        Set(d, "PropertyTaxYear", Scalar(Get(pr, "taxInfo", "rollYear")),
            "payload.publicRecordsInfo.taxInfo.rollYear", () => d.PropertyTaxYear, x => d.PropertyTaxYear = x);
        Set(d, "HoaMonthlyRedfin", Scalar(Get(pr, "mortgageCalculatorInfo", "monthlyHoaDues")),
            "payload.publicRecordsInfo.mortgageCalculatorInfo.monthlyHoaDues", () => d.HoaMonthlyRedfin, x => d.HoaMonthlyRedfin = x);
    }

    /// İlanın MLS alanlarındaki aidat grubu ("HOA Information", "Homeowners Association Information"). Grup olduğu gibi
    /// HoaAmenities'e yazılır; tutar ve dönem MLS'in kendi alanlarından: ASSOCIATION_FEE + ASSOCIATION_FEE_FREQUENCY, yoksa
    /// Stellar MLS'in MFR_MONTHLY_HOAAMOUNT'ı (dönem = alan adı).
    static void Amenities(JsonElement p, RedfinDetails d)
    {
        if (Get(p, "amenitiesInfo", "superGroups") is not { ValueKind: JsonValueKind.Array } supers) return;
        foreach (var sg in supers.EnumerateArray())
        {
            if (Get(sg, "amenityGroups") is not { ValueKind: JsonValueKind.Array } groups) continue;
            foreach (var g in groups.EnumerateArray())
            {
                var title = Scalar(Get(g, "groupTitle")) ?? "";
                if (!title.Contains("HOA", StringComparison.OrdinalIgnoreCase) && !title.Contains("Association", StringComparison.OrdinalIgnoreCase)) continue;
                if (Get(g, "amenityEntries") is not { ValueKind: JsonValueKind.Array } entries) continue;
                var fields = new List<(string Ref, string Value)>();
                foreach (var en in entries.EnumerateArray())
                {
                    var name = Scalar(Get(en, "referenceName")) ?? Scalar(Get(en, "amenityName"));
                    var vals = Get(en, "amenityValues") is { ValueKind: JsonValueKind.Array } va
                        ? string.Join(", ", va.EnumerateArray().Select(x => Scalar(x)).OfType<string>()) : "";
                    if (name != null) fields.Add((name, vals));
                }
                if (fields.Count == 0) continue;
                var path = $"payload.amenitiesInfo \"{title}\"";
                Set(d, "HoaAmenities", string.Join("; ", fields.Select(f => $"{f.Ref}={f.Value}")), path, () => d.HoaAmenities, x => d.HoaAmenities = x);
                string? F(string r) => fields.FirstOrDefault(f => f.Ref == r).Value is { Length: > 0 } v ? v : null;
                if (F("ASSOCIATION_FEE") is { } fee)
                {
                    Set(d, "Hoa", fee, path + " ASSOCIATION_FEE", () => d.Hoa, x => d.Hoa = x);
                    Set(d, "HoaPeriod", F("ASSOCIATION_FEE_FREQUENCY"), path + " ASSOCIATION_FEE_FREQUENCY", () => d.HoaPeriod, x => d.HoaPeriod = x);
                }
                else if (F("MFR_MONTHLY_HOAAMOUNT") is { } monthly)
                {
                    Set(d, "Hoa", monthly, path + " MFR_MONTHLY_HOAAMOUNT", () => d.Hoa, x => d.Hoa = x);
                    Set(d, "HoaPeriod", "MFR_MONTHLY_HOAAMOUNT (aylık)", path + " MFR_MONTHLY_HOAAMOUNT", () => d.HoaPeriod, x => d.HoaPeriod = x);
                }
            }
        }
    }

    /// First Street / Redfin iklim bloğu (riskFactor): FEMA bölgesi (Redfin'e göre MassiveCert tahmini), yıllık sel sigortası
    /// aralığı ve puanlar.
    static void RiskFactor(JsonElement p, RedfinDetails d)
    {
        if (Get(p, "floodData") is not { ValueKind: JsonValueKind.Object } fd) return;
        if (Get(fd, "femaZones") is { ValueKind: JsonValueKind.Array } zones)
        {
            var z = zones.EnumerateArray().Select(x => Scalar(x)).OfType<string>().ToList();
            Set(d, "FloodZone", z.Count > 0 ? string.Join(", ", z) : null, "payload.floodData.femaZones", () => d.FloodZone, x => d.FloodZone = x);
        }
        string? lo = Scalar(Get(fd, "lowInsurancePrice")), hi = Scalar(Get(fd, "highInsurancePrice"));
        Set(d, "FloodInsuranceEstimate", lo != null && hi != null ? $"{lo}–{hi}" : lo ?? hi,
            "payload.floodData.lowInsurancePrice–highInsurancePrice", () => d.FloodInsuranceEstimate, x => d.FloodInsuranceEstimate = x);
        foreach (var (kind, path) in new[]
                 {
                     ("flood", new[] { "floodData", "floodFactor" }), ("fire", new[] { "fireData", "fireFactor" }),
                     ("heat", new[] { "heatData", "heatFactor" }), ("wind", new[] { "windData", "riskFactorScore" }),
                     ("air", new[] { "airData", "riskFactorScore" }),
                 })
            if (!d.ClimateRisk.ContainsKey(kind) && Scalar(Get(p, path)) is { } v)
            {
                d.ClimateRisk[kind] = v;
                d.Found["ClimateRisk." + kind] = "payload." + string.Join(".", path);
            }
    }

    // ---------- sayfanın görünen metni (bloklar yoksa) ----------

    static readonly Regex Tags = new(@"<script[\s\S]*?</script>|<style[\s\S]*?</style>|<[^>]+>");
    static readonly Regex Spaces = new(@"\s+");
    static readonly Regex RemarksBlock = new(@"<div[^>]*\bid=""marketing-remarks-scroll""[^>]*>([\s\S]*?)</div>", RegexOptions.IgnoreCase);
    static readonly Regex FemaZoneText = new(@"FEMA\s+zone\s+([A-Z]{1,3}\d{0,2}(?:\s*\((?:un)?shaded\))?)", RegexOptions.IgnoreCase);
    static readonly Regex InsuranceText = new(@"Insurance for .{1,120}? ranges from \$([\d,]+) to \$([\d,]+) per year", RegexOptions.IgnoreCase);
    static readonly Regex ListedByText = new(@"Listed by (.+?) • (.+?) (?:Contact:|Listing updated:|$)");
    static readonly Regex TaxTableText = new(@"Year Property tax Land \+ Additions Assessment\*? (\d{4}) \$([\d,]+)");
    static readonly Regex FactorText = new(@"(\d{1,2})/10 (Flood|Fire|Heat|Wind|Air) Factor");

    /// Sayfanın görünen metni (script/style ve etiketler atılır, boşluklar teke iner).
    public static string PageText(string html) =>
        html.Length == 0 ? "" : Spaces.Replace(System.Net.WebUtility.HtmlDecode(Tags.Replace(html, " ")), " ").Trim();

    /// Gömülü bloklarda bulunamayanlar sayfada yazdığı gibi. Aidat için ödeme hesaplayıcısındaki "HOA dues $0" kullanılmaz:
    /// veri yokken de 0 gösteriyor.
    static void FromPageText(string html, RedfinDetails d)
    {
        if (html.Length == 0) return;
        if (d.Description == null && RemarksBlock.Match(html) is { Success: true } rb && PageText(rb.Groups[1].Value) is { Length: >= 30 } remarks)
            Set(d, "Description", remarks, "sayfa: #marketing-remarks-scroll", () => d.Description, x => d.Description = x);
        if (d.FloodZone != null && d.FloodInsuranceEstimate != null && d.AgentName != null && d.PropertyTax != null && d.ClimateRisk.Count > 0) return;
        var text = PageText(html);
        if (FemaZoneText.Match(text) is { Success: true } z)
            Set(d, "FloodZone", z.Groups[1].Value, "sayfa metni: " + z.Value, () => d.FloodZone, x => d.FloodZone = x);
        if (InsuranceText.Match(text) is { Success: true } ins)
            Set(d, "FloodInsuranceEstimate", $"{ins.Groups[1].Value.Replace(",", "")}–{ins.Groups[2].Value.Replace(",", "")}",
                "sayfa metni: " + ins.Value, () => d.FloodInsuranceEstimate, x => d.FloodInsuranceEstimate = x);
        if (ListedByText.Match(text) is { Success: true } lb)
        {
            Set(d, "AgentName", lb.Groups[1].Value.Trim(), "sayfa metni: Listed by", () => d.AgentName, x => d.AgentName = x);
            Set(d, "OfficeName", lb.Groups[2].Value.Trim(), "sayfa metni: Listed by", () => d.OfficeName, x => d.OfficeName = x);
        }
        if (TaxTableText.Match(text) is { Success: true } tax)
        {
            Set(d, "PropertyTax", tax.Groups[2].Value.Replace(",", ""), "sayfa metni: vergi tablosu", () => d.PropertyTax, x => d.PropertyTax = x);
            Set(d, "PropertyTaxYear", tax.Groups[1].Value, "sayfa metni: vergi tablosu", () => d.PropertyTaxYear, x => d.PropertyTaxYear = x);
        }
        foreach (Match f in FactorText.Matches(text))
        {
            var kind = f.Groups[2].Value.ToLowerInvariant();
            if (d.ClimateRisk.TryAdd(kind, f.Groups[1].Value)) d.Found["ClimateRisk." + kind] = "sayfa metni: " + f.Value;
        }
    }
}
