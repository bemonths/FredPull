using System.Globalization;
using System.Net;
using System.Text.Json;

namespace FredPull;

/// FRED API: https://fred.stlouisfed.org/docs/api/fred/series_observations.html
/// Limit 120 istek/dk. 2 paralel istek × 1100 ms bekleme ≈ 109/dk, limitin altında.
public sealed class FredClient : IDisposable
{
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    readonly SemaphoreSlim _gate = new(2);
    readonly string _apiKey;

    /// Her isteğin sonucu buraya yazılır (MainForm out\log.txt'ye bağlar).
    public Action<string>? Log { get; set; }

    public FredClient(string apiKey) => _apiKey = apiKey;

    /// Seri FRED'de yoksa null döner. Anahtar hatası ve beklenmeyen hatalar exception fırlatır.
    public async Task<List<Obs>?> GetObservationsAsync(string seriesId, DateOnly from, CancellationToken ct)
    {
        var url = $"https://api.stlouisfed.org/fred/series/observations?series_id={seriesId}&api_key={_apiKey}&file_type=json&observation_start={from:yyyy-MM-dd}";
        for (int attempt = 0; attempt < 5; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            await _gate.WaitAsync(ct);
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                int code = (int)resp.StatusCode;

                if (code == 400)
                {
                    var msg = ExtractError(body);
                    if (msg.Contains("api_key", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("FRED API anahtarı reddedildi. Anahtarı https://fredaccount.stlouisfed.org/apikeys adresinden kontrol et.\nFRED: " + msg);
                    if (msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                    {
                        Log?.Invoke($"{seriesId}: seri yok");
                        return null;
                    }
                    throw new InvalidOperationException($"{seriesId}: FRED 400 — {msg}");
                }
                if (code == 429 || code >= 500)
                {
                    Log?.Invoke($"{seriesId}: HTTP {code}, {attempt + 1}. deneme, bekleniyor");
                    await Task.Delay(1500 * (attempt + 1), ct);
                    continue;
                }
                if (code == 401 || code == 403)
                    throw new InvalidOperationException($"FRED erişimi reddedildi (HTTP {code}). Anahtarı kontrol et.");
                if (code != 200)
                    throw new InvalidOperationException($"{seriesId}: HTTP {code} — {Truncate(body)}");

                using var doc = JsonDocument.Parse(body);
                var list = new List<Obs>();
                foreach (var o in doc.RootElement.GetProperty("observations").EnumerateArray())
                {
                    var d = DateOnly.Parse(o.GetProperty("date").GetString()!, CultureInfo.InvariantCulture);
                    var vs = o.GetProperty("value").GetString();
                    double? v = (vs == null || vs == ".") ? null : double.Parse(vs, CultureInfo.InvariantCulture);
                    list.Add(new Obs(d, v));
                }
                Log?.Invoke($"{seriesId}: {list.Count} gözlem");
                return list;
            }
            catch (HttpRequestException ex)
            {
                Log?.Invoke($"{seriesId}: ağ hatası ({ex.Message}), {attempt + 1}. deneme");
                await Task.Delay(1500 * (attempt + 1), ct);
            }
            finally
            {
                _gate.Release();
                await Task.Delay(1100, ct);
            }
        }
        throw new InvalidOperationException($"{seriesId}: 5 denemede alınamadı (ağ/limit). Ayrıntı out\\log.txt");
    }

    static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error_message", out var m)) return m.GetString() ?? body;
        }
        catch { }
        return Truncate(body);
    }

    static string Truncate(string s) => s.Length > 200 ? s[..200] + "…" : s;

    public void Dispose()
    {
        _http.Dispose();
        _gate.Dispose();
    }
}
