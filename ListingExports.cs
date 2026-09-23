using System.Globalization;
using System.Security;
using System.Text;

namespace FredPull;

/// Ev kartlarından dışa aktarımlar: referans fotoğraflar, Google Earth Studio KML'i, animasyon CSV'leri.
public static class ListingExports
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    const int MaxPhotos = 12;

    public static string PhotoDir(string outDir, string state, string fips, HouseCandidate c) =>
        Path.Combine(outDir, "photos", state, $"{fips}_{Safe(c.Street)}");

    static string Safe(string s)
    {
        var t = new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray()).Trim('_');
        return t.Length > 40 ? t[..40] : t;
    }

    /// Daha önce indirilmiş ve dosyaları yerinde mi.
    public static bool HasPhotos(string outDir, HouseCandidate c) =>
        c.PhotoPaths.Count > 0 && c.PhotoPaths.All(p => File.Exists(Path.Combine(outDir, p)));

    /// PhotoUrls'deki ilk 12 fotoğrafı out\photos\{ST}\{fips}_{sokak}\ altına indirir, yanına REFERANS.txt yazar;
    /// yolları c.PhotoPaths'e (out klasörüne göre göreli) koyar. Görüntüler CDN'den doğrudan iner, tarayıcı gerekmez.
    public static async Task<int> DownloadPhotosAsync(string outDir, string state, string fips, HouseCandidate c,
        IProgress<string> status, CancellationToken ct)
    {
        var dir = PhotoDir(outDir, state, fips, c);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "REFERANS.txt"),
            "Emlakçı/MLS telifli; videoda kullanılmaz, yalnızca referans.\r\n" +
            $"İlan: {c.Url}\r\nİndirme: {DateTime.Now:yyyy-MM-dd HH:mm}\r\n", Encoding.UTF8, ct);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/153.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Referrer = new Uri("https://www.redfin.com/");

        var urls = c.PhotoUrls.Take(MaxPhotos).ToList();
        var paths = new List<string>();
        string? firstError = null;
        for (int i = 0; i < urls.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            status.Report($"{c.Street} — fotoğraf {i + 1}/{urls.Count}");
            var ext = Path.GetExtension(new Uri(urls[i]).AbsolutePath).ToLowerInvariant();
            var file = Path.Combine(dir, $"{i + 1:00}{(ext is ".jpg" or ".jpeg" or ".png" or ".webp" ? ext : ".jpg")}");
            try
            {
                await File.WriteAllBytesAsync(file, await http.GetByteArrayAsync(urls[i], ct), ct);
                paths.Add(Path.GetRelativePath(outDir, file));
            }
            catch (HttpRequestException ex) { firstError ??= ex.Message; }
            await Task.Delay(300, ct);
        }
        if (paths.Count == 0 && urls.Count > 0)
            throw new InvalidOperationException("Fotoğraflar indirilemedi: " + firstError);
        c.PhotoPaths = paths;
        return paths.Count;
    }

    /// Seçilen evler için Google Earth Studio'ya aktarılacak out\earthstudio_{ST}.kml.
    /// Placemark adı "{İlçe} — {Şehir}"; koordinatı olmayan ev atlanır ve Skipped'de döner.
    public static (string Path, int Written, List<string> Skipped) WriteKml(string outDir, string state, IEnumerable<HouseCard> cards)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
        sb.AppendLine("<Document>");
        sb.AppendLine($"  <name>{X($"FredPull ev kartları — {state}")}</name>");
        int written = 0;
        var skipped = new List<string>();
        foreach (var card in cards.OrderBy(c => c.County))
        {
            if (card.Chosen is not { } h) continue;
            if (h.Lat is not double lat || h.Lng is not double lng) { skipped.Add($"{card.County} — {h.Street}"); continue; }
            sb.AppendLine("  <Placemark>");
            sb.AppendLine($"    <name>{X($"{card.County} — {h.City}")}</name>");
            sb.AppendLine($"    <description>{X($"{card.CardText}\n{h.Street}\n{h.Url}")}</description>");
            sb.AppendLine($"    <Point><coordinates>{lng.ToString("0.######", Inv)},{lat.ToString("0.######", Inv)},0</coordinates></Point>");
            sb.AppendLine("  </Placemark>");
            written++;
        }
        sb.AppendLine("</Document>");
        sb.AppendLine("</kml>");
        var path = Path.Combine(outDir, $"earthstudio_{state}.kml");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return (path, written, skipped);
    }

    static string X(string s) => SecurityElement.Escape(s) ?? "";

    /// Seçilen her ev için out\anim\{ST}\{fips}.csv: date, price, cut. İlk satır ilan tarihi ve ilk fiyat (cut 0),
    /// sonrakiler her fiyat değişikliği; cut = bir önceki fiyattan düşüş (artışta eksi). Fiyat sayacı ve indirim etiketi için.
    public static (string Dir, int Written, List<string> Skipped) WriteAnimCsv(string outDir, string state, IEnumerable<HouseCard> cards)
    {
        var dir = Path.Combine(outDir, "anim", state);
        Directory.CreateDirectory(dir);
        int written = 0;
        var skipped = new List<string>();
        foreach (var card in cards)
        {
            if (card.Chosen is not { } h) continue;
            RedfinListingPicker.FillPriceSteps(h);
            if (h.PriceSteps.Count == 0) { skipped.Add($"{card.County} — {h.Street}"); continue; }
            using var w = new StreamWriter(Path.Combine(dir, $"{card.Fips}.csv"), false, new UTF8Encoding(false));
            w.WriteLine("date,price,cut");
            int? prev = null;
            foreach (var s in h.PriceSteps)
            {
                w.WriteLine($"{s.Date.ToString("yyyy-MM-dd", Inv)},{s.Price},{(prev.HasValue ? prev.Value - s.Price : 0)}");
                prev = s.Price;
            }
            written++;
        }
        return (dir, written, skipped);
    }
}
