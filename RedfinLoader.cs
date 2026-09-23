using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace FredPull;

/// Redfin Data Center dosyaları (ücretsiz, aylık güncellenir). Kaynak: https://www.redfin.com/news/data-center/
public static class RedfinLoader
{
    public const string CountyUrl = "https://redfin-public-data.s3.us-west-2.amazonaws.com/redfin_market_tracker/county_market_tracker.tsv000.gz";
    public const string StateUrl  = "https://redfin-public-data.s3.us-west-2.amazonaws.com/redfin_market_tracker/state_market_tracker.tsv000.gz";
    public const string UsUrl     = "https://redfin-public-data.s3.us-west-2.amazonaws.com/redfin_market_tracker/us_national_market_tracker.tsv000.gz";

    public static async Task DownloadAsync(string url, string dest, IProgress<string> progress, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        long total = resp.Content.Headers.ContentLength ?? -1;
        var name = Path.GetFileName(dest);
        var tmp = dest + ".part";

        await using (var src = await resp.Content.ReadAsStreamAsync(ct))
        await using (var dst = File.Create(tmp))
        {
            var buf = new byte[1 << 16];
            long done = 0, lastReport = 0;
            int n;
            while ((n = await src.ReadAsync(buf, ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, n), ct);
                done += n;
                if (done - lastReport > 2_000_000)
                {
                    lastReport = done;
                    progress.Report(total > 0
                        ? $"İndiriliyor {name}: {done / 1048576} / {total / 1048576} MB"
                        : $"İndiriliyor {name}: {done / 1048576} MB");
                }
            }
        }
        File.Move(tmp, dest, true);
    }

    /// Dosyayı satır satır okur. Sonuç: bölge adı -> metrik -> aylık seri.
    /// stateCode verilirse sadece o eyaletin satırları alınır. Sadece "All Residential" ve mevsimsel düzeltmesiz satırlar.
    public static Dictionary<string, Dictionary<string, List<Obs>>> Parse(string gzPath, string? stateCode, IProgress<string>? progress, CancellationToken ct)
    {
        using var fs = File.OpenRead(gzPath);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);
        using var sr = new StreamReader(gz, Encoding.UTF8);

        var headerLine = sr.ReadLine() ?? throw new InvalidOperationException("Redfin dosyası boş.");
        var header = headerLine.Split('\t').Select(Clean).ToArray();
        int Idx(string name) => Array.FindIndex(header, h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

        int iBegin = Idx("period_begin"), iRegion = Idx("region"), iState = Idx("state_code"),
            iType = Idx("property_type"), iDur = Idx("period_duration"), iSa = Idx("is_seasonally_adjusted");
        if (iBegin < 0 || iRegion < 0 || iType < 0)
            throw new InvalidOperationException("Redfin dosyasında beklenen sütunlar yok (period_begin / region / property_type).");

        var metricIdx = Analyzer.RedfinKeys
            .Select(k => (Key: k, Index: Idx(k)))
            .Where(t => t.Index >= 0)
            .ToArray();

        // bölge + süre -> metrik -> tarih -> değer
        var tmp = new Dictionary<string, Dictionary<string, Dictionary<DateOnly, double?>>>(StringComparer.OrdinalIgnoreCase);
        string? line;
        long lines = 0;
        while ((line = sr.ReadLine()) != null)
        {
            if ((++lines & 0x3FFFF) == 0)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"Redfin okunuyor: {lines / 1000}k satır");
            }
            var c = line.Split('\t');
            int needed = Math.Max(Math.Max(iBegin, iRegion), Math.Max(iType, Math.Max(iState, Math.Max(iDur, iSa))));
            if (c.Length <= needed) continue;

            if (stateCode != null && iState >= 0 && !string.Equals(Clean(c[iState]), stateCode, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.Equals(Clean(c[iType]), "All Residential", StringComparison.OrdinalIgnoreCase)) continue;
            if (iSa >= 0 && Clean(c[iSa]).StartsWith("t", StringComparison.OrdinalIgnoreCase)) continue;
            if (!DateOnly.TryParse(Clean(c[iBegin]), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) continue;
            d = new DateOnly(d.Year, d.Month, 1);

            var dur = iDur >= 0 ? Clean(c[iDur]) : "";
            var key = Clean(c[iRegion]) + "\u0001" + dur;
            if (!tmp.TryGetValue(key, out var byMetric)) tmp[key] = byMetric = new(StringComparer.OrdinalIgnoreCase);

            foreach (var (mk, mi) in metricIdx)
            {
                if (mi >= c.Length) continue;
                double? v = double.TryParse(Clean(c[mi]), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : null;
                if (!byMetric.TryGetValue(mk, out var byDate)) byMetric[mk] = byDate = new();
                byDate[d] = v;
            }
        }

        // Her bölge için aylık (30 günlük) seriyi tercih et; yoksa en kısa süreliyi al.
        var result = new Dictionary<string, Dictionary<string, List<Obs>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in tmp.GroupBy(kv => kv.Key.Split('\u0001')[0], StringComparer.OrdinalIgnoreCase))
        {
            var chosen = group.FirstOrDefault(kv => kv.Key.EndsWith("\u000130"));
            if (chosen.Value == null)
                chosen = group.OrderBy(kv => int.TryParse(kv.Key.Split('\u0001')[1], out var n) ? n : int.MaxValue).First();

            var dict = new Dictionary<string, List<Obs>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (mk, byDate) in chosen.Value)
                dict[mk] = byDate.OrderBy(p => p.Key).Select(p => new Obs(p.Key, p.Value)).ToList();
            result[group.Key] = dict;
        }
        return result;
    }

    static string Clean(string s) => s.Trim().Trim('"');

    /// "St. Johns County, FL" ve "St. Johns County" aynı anahtara insin: stjohns
    public static string NormalizeName(string s)
    {
        var comma = s.IndexOf(',');
        if (comma >= 0) s = s[..comma];
        var sb = new StringBuilder();
        foreach (var ch in s.ToLowerInvariant())
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        var t = sb.ToString();
        foreach (var suffix in new[] { "county", "parish", "borough", "censusarea", "municipality" })
            if (t.EndsWith(suffix, StringComparison.Ordinal)) { t = t[..^suffix.Length]; break; }
        return t;
    }
}
