using System.Text.Json;
using System.Text.RegularExpressions;

namespace FredPull;

/// İlan sayfasındaki ham ayrıntıları (açıklama, emlak vergisi, aidat, Redfin'in FEMA bölgesi ve sel sigortası tahmini,
/// iklim riski puanları, ilanı veren emlakçı ve ofis) bulur. Redfin'in yanıt yapısı belgelenmediği için alanlar anahtar
/// adıyla aranır (ör. "taxesDue", "hoaDues", "floodZone"); her değerin bulunduğu JSON yolu Found'a yazılır. Benzer ve yakın
/// evlerin bölümleri atlanır, önce bu evin kimliğini (propertyId) taşıyan gövdeler okunur. Değer sayfada/yanıtta yazdığı
/// gibi metin olarak alınır, tahmin edilmez; yapı değişirse hata vermez, alanlar boş kalır.
/// Not (2026-09-27): alan adları gerçek bir ilan sayfasıyla henüz doğrulanmadı. Toplama sırasında ham yanıtlar
/// out\redfin_raw\{ST}\ altına yazılır; ilk canlı toplamadan sonra bu dosyalara bakılıp kurallar sıkılaştırılacak.
public static class RedfinDetailsReader
{
    // bu parçaları içeren anahtarların altı başka evlere ait
    static readonly string[] OtherHomes = { "similar", "nearby", "comparable", "recentlysold", "comps", "otherhomes", "neighborhood" };

    static readonly HashSet<string> DescriptionKeys = new() { "marketingremark", "listingremarks", "publicremarks", "remarks", "propertydescription" };
    static readonly HashSet<string> TaxKeys = new() { "taxesdue", "propertytax", "propertytaxes", "annualtax", "taxannualamount", "taxamount" };
    static readonly HashSet<string> TaxYearKeys = new() { "rollyear", "taxyear", "taxesyear" };
    static readonly HashSet<string> HoaKeys = new() { "hoadues", "hoadue", "hoafee", "associationfee", "hoaamount" };
    static readonly HashSet<string> HoaPeriodKeys = new() { "hoaduesfrequency", "hoafeefrequency", "associationfeefrequency", "hoafrequency", "hoaperiod", "hoaduesperiod" };
    static readonly HashSet<string> FloodZoneKeys = new() { "femazone", "floodzone", "femafloodzone", "fldzone", "floodzonecode" };
    static readonly HashSet<string> OfficeKeys = new() { "brokername", "listingbrokername", "officename", "listingofficename", "brokeragename" };
    static readonly Regex ClimateKey = new(@"^(flood|fire|heat|wind)(factor)?(score|rating|risk|level)?$");
    static readonly Regex PropertyIdInUrl = new(@"/home/(\d+)");

    public static RedfinDetails Read(IReadOnlyList<string> bodies, string html, string url)
    {
        var d = new RedfinDetails { ReadAt = DateTime.Now };
        try
        {
            var docs = new List<(string Text, JsonDocument Doc)>();
            foreach (var b in bodies)
            {
                var t = b.TrimStart();
                if (t.StartsWith("{}&&", StringComparison.Ordinal)) t = t[4..];
                try { docs.Add((t, JsonDocument.Parse(t))); }
                catch (JsonException) { }
            }
            var pid = PropertyIdInUrl.Match(url) is { Success: true } m ? m.Groups[1].Value : null;
            var own = pid == null ? docs : docs.Where(x => x.Text.Contains($"\"propertyId\":{pid}") || x.Text.Contains($"\"propertyId\":\"{pid}\"")).ToList();
            foreach (var (_, doc) in own.Count > 0 ? own : docs) Walk(doc.RootElement, "", d);
            foreach (var (_, doc) in docs) doc.Dispose();
            FromPageText(html, d);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }   // yapı değişirse boş kalır
        return d;
    }

    static void Walk(JsonElement e, string path, RedfinDetails d)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in e.EnumerateObject())
                {
                    var k = p.Name.ToLowerInvariant();
                    if (OtherHomes.Any(o => k.Contains(o))) continue;
                    var sub = path.Length == 0 ? p.Name : path + "." + p.Name;
                    if (p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) Walk(p.Value, sub, d);
                    else Leaf(k, p.Value, sub, d);
                }
                break;
            case JsonValueKind.Array:
                int i = 0;
                foreach (var x in e.EnumerateArray()) Walk(x, $"{path}[{i++}]", d);
                break;
        }
    }

    static string? Scalar(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString()?.Trim(),
        JsonValueKind.Number => v.GetRawText(),
        _ => null,
    };

    static bool HasDigit(string s) => s.Any(char.IsDigit);

    static void Leaf(string k, JsonElement v, string path, RedfinDetails d)
    {
        var s = Scalar(v);
        if (string.IsNullOrEmpty(s)) return;
        var lpath = path.ToLowerInvariant();

        void Set(string field, Func<string?> get, Action<string> set)
        {
            if (get() != null) return;
            set(s);
            d.Found[field] = path;
        }

        if (DescriptionKeys.Contains(k) && v.ValueKind == JsonValueKind.String && s.Length >= 30)
            Set("Description", () => d.Description, x => d.Description = x);
        else if (TaxKeys.Contains(k) && HasDigit(s))
            Set("PropertyTax", () => d.PropertyTax, x => d.PropertyTax = x);
        else if (TaxYearKeys.Contains(k) && int.TryParse(s, out var y) && y is >= 1990 and <= 2100)
            Set("PropertyTaxYear", () => d.PropertyTaxYear, x => d.PropertyTaxYear = x);
        else if (HoaKeys.Contains(k) && HasDigit(s))
            Set("Hoa", () => d.Hoa, x => d.Hoa = x);
        else if (HoaPeriodKeys.Contains(k))
            Set("HoaPeriod", () => d.HoaPeriod, x => d.HoaPeriod = x);
        else if (FloodZoneKeys.Contains(k) && s.Length <= 12)
            Set("FloodZone", () => d.FloodZone, x => d.FloodZone = x);
        else if (k.Contains("floodinsurance") && (v.ValueKind == JsonValueKind.String || HasDigit(s)))
        {
            if (d.FloodInsuranceEstimate == null) { d.FloodInsuranceEstimate = s; d.Found["FloodInsuranceEstimate"] = path; }
            else if (d.Found.TryGetValue("FloodInsuranceEstimate", out var first) && ParentOf(first) == ParentOf(path)
                     && !d.FloodInsuranceEstimate.Contains('–'))
            {
                d.FloodInsuranceEstimate += "–" + s;   // aynı nesnedeki alt/üst sınır: "1737–8500"
                d.Found["FloodInsuranceEstimate"] += " + " + path;
            }
        }
        else if (ClimateKey.Match(k) is { Success: true } cm && (cm.Groups[2].Success || cm.Groups[3].Success))
        {
            var kind = cm.Groups[1].Value;
            if (!d.ClimateRisk.ContainsKey(kind)) { d.ClimateRisk[kind] = s; d.Found["ClimateRisk." + kind] = path; }
        }
        else if (lpath.Contains("listingagent") && k is "agentname" or "name" or "fullname" && v.ValueKind == JsonValueKind.String)
            Set("AgentName", () => d.AgentName, x => d.AgentName = x);
        else if (OfficeKeys.Contains(k) && v.ValueKind == JsonValueKind.String)
            Set("OfficeName", () => d.OfficeName, x => d.OfficeName = x);
    }

    static string ParentOf(string path) => path.Contains('.') ? path[..path.LastIndexOf('.')] : "";

    static readonly Regex Tags = new(@"<script[\s\S]*?</script>|<style[\s\S]*?</style>|<[^>]+>");
    static readonly Regex FemaZoneText = new(@"FEMA\s*(?:Flood\s*)?Zone[:\s]+([A-Z]{1,3}\d{0,2})\b", RegexOptions.IgnoreCase);
    static readonly Regex InsuranceText = new(@"flood insurance[\s\S]{0,300}?(\$[\d,]+\s*[–-]\s*\$[\d,]+)", RegexOptions.IgnoreCase);
    static readonly Regex HoaText = new(@"HOA\s*Dues[:\s]*(\$[\d,]+)(?:\s*/\s*(month|mo|year|yr|quarter|qtr))?", RegexOptions.IgnoreCase);

    static readonly Regex Spaces = new(@"\s+");
    static readonly Regex RemarksBlock = new(@"<div[^>]*\bid=""marketing-remarks-scroll""[^>]*>([\s\S]*?)</div>", RegexOptions.IgnoreCase);

    /// Sayfanın görünen metni (script/style ve etiketler atılır, boşluklar teke iner).
    public static string PageText(string html) =>
        html.Length == 0 ? "" : Spaces.Replace(System.Net.WebUtility.HtmlDecode(Tags.Replace(html, " ")), " ").Trim();

    /// JSON'da bulunamayanlar için sayfanın görünen metni (sayfada yazdığı gibi).
    static void FromPageText(string html, RedfinDetails d)
    {
        if (d.Description == null && RemarksBlock.Match(html) is { Success: true } rb && PageText(rb.Groups[1].Value) is { Length: >= 30 } remarks)
        {
            d.Description = remarks;
            d.Found["Description"] = "sayfa: #marketing-remarks-scroll";
        }
        if (html.Length == 0 || (d.FloodZone != null && d.FloodInsuranceEstimate != null && d.Hoa != null)) return;
        var text = PageText(html);
        if (d.FloodZone == null && FemaZoneText.Match(text) is { Success: true } z)
        {
            d.FloodZone = z.Groups[1].Value;
            d.Found["FloodZone"] = "sayfa metni: " + z.Value.Trim();
        }
        if (d.FloodInsuranceEstimate == null && InsuranceText.Match(text) is { Success: true } ins)
        {
            d.FloodInsuranceEstimate = ins.Groups[1].Value;
            d.Found["FloodInsuranceEstimate"] = "sayfa metni";
        }
        if (d.Hoa == null && HoaText.Match(text) is { Success: true } h)
        {
            d.Hoa = h.Groups[1].Value;
            if (h.Groups[2].Success && d.HoaPeriod == null) d.HoaPeriod = h.Groups[2].Value;
            d.Found["Hoa"] = "sayfa metni: " + h.Value.Trim();
        }
    }
}
