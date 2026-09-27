using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace FredPull;

/// FEMA'nın resmî NFHL sunucusuna bağlanılamadığında nedenini ayırt etmek için aynı makineden farklı yollarla dener ve
/// sonuçları satır satır döndürür (günlüğe yazılır): DNS, her IP'ye TCP 443/80, TLS 1.2 ve 1.3 el sıkışması (SNI'li ve
/// SNI'siz), el sıkışma olursa sertifika zinciri (iptal denetimi dahil), .NET HttpClient (TLS 1.2 / 1.3 açıkça),
/// Windows curl.exe ve PowerShell Invoke-RestMethod. Sertifika doğrulaması hiçbir denemede kapatılmaz.
public static class FemaDiagnostics
{
    public sealed record Result(List<string> Lines, string Summary);

    public static async Task<Result> RunAsync(IReadOnlyList<string> urls, CancellationToken ct)
    {
        var lines = new List<string>();
        void L(string s) => lines.Add(s);
        var host = new Uri(urls[0]).Host;
        bool anyTcp = false, anyHandshake = false, anyHttp = false, allResetAfterTcp = true, any80 = false, tls13Local = true;

        // 1) DNS
        IPAddress[] ips = Array.Empty<IPAddress>();
        try
        {
            ips = (await Dns.GetHostAddressesAsync(host, ct)).Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
            L($"DNS {host}: {string.Join(", ", ips.Select(i => i.ToString()))}");
        }
        catch (SocketException ex) { L($"DNS {host}: hata — {ex.Message}"); }

        // 2) TCP ve TLS (IP başına)
        foreach (var ip in ips)
        {
            foreach (var port in new[] { 443, 80 })
            {
                var ok = await TcpAsync(ip, port, ct);
                L($"TCP {ip}:{port}: {(ok == null ? "bağlandı" : ok)}");
                if (port == 443 && ok == null) anyTcp = true;
                if (port == 80 && ok == null) any80 = true;
            }
            foreach (var (proto, name) in new[] { (SslProtocols.Tls12, "TLS 1.2"), (SslProtocols.Tls13, "TLS 1.3") })
                foreach (var sni in new[] { host, null })
                {
                    var (ok, text) = await HandshakeAsync(ip, host, sni, proto, ct);
                    L($"TLS {ip} {name} {(sni == null ? "SNI'siz" : "SNI=" + sni)}: {text}");
                    if (ok) anyHandshake = true;
                    else if (proto == SslProtocols.Tls12 && !IsReset(text)) allResetAfterTcp = false;
                    if (proto == SslProtocols.Tls13 && !text.Contains("Win32Exception", StringComparison.Ordinal)) tls13Local = false;
                }
        }

        // 3) HttpClient (TLS sürümü açıkça), curl.exe, Invoke-RestMethod — her iki adres
        foreach (var url in urls)
        {
            foreach (var (proto, name) in new[] { (SslProtocols.Tls12, "TLS 1.2"), (SslProtocols.Tls13, "TLS 1.3") })
            {
                var text = await HttpAsync(url, proto, ct);
                L($"HttpClient {name} {url}: {text}");
                if (text.StartsWith("HTTP 2", StringComparison.Ordinal)) anyHttp = true;
            }
            var curl = await ProcessAsync(Path.Combine(Environment.SystemDirectory, "curl.exe"),
                $"-s -S -o NUL -m 20 -w \"HTTP %{{http_code}}\" \"{url}\"", ct);
            L($"curl.exe {url}: {curl}");
            if (curl.Contains("HTTP 200")) anyHttp = true;
            var ps = await ProcessAsync(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                "-NoProfile -NonInteractive -Command \"[Console]::OutputEncoding = [Text.Encoding]::UTF8; try { $r = Invoke-RestMethod '" + url + "' -TimeoutSec 20; 'OK' } catch { $_.Exception.Message + ' | ' + $_.Exception.InnerException.Message }\"", ct);
            L($"Invoke-RestMethod {url}: {ps}");
            if (ps.Trim() == "OK") anyHttp = true;
        }

        string summary = anyHttp ? "Resmî sunucuya en az bir yolla ulaşıldı (ayrıntı günlükte)."
            : anyHandshake ? "TLS el sıkışması oldu ama HTTP isteği başarısız (ayrıntı günlükte)."
            : anyTcp && allResetAfterTcp
                ? "TCP bağlantısı kuruluyor ama sunucu tarafı TLS'nin ilk mesajında bağlantıyı sıfırlıyor (bütün IP'lerde, SNI'li ve SNI'siz, TLS 1.2); " +
                  "HttpClient, curl.exe ve Invoke-RestMethod da bağlanamadı. Sunucu sertifika göndermeden kestiği için istemci tarafında düzeltilecek " +
                  "bir sertifika zinciri sorunu görülmedi. " + (any80 ? "80 portuna TCP bağlantısı kuruluyor." : "80 portu yanıt vermiyor (zaman aşımı).")
                  + (tls13Local ? " TLS 1.3 bu Windows sürümünde istemci tarafında kullanılamıyor (Windows 10); denenebilen sürüm TLS 1.2." : "")
            : anyTcp ? "TCP bağlantısı kuruluyor, TLS el sıkışması başarısız (ayrıntı günlükte)."
            : "Sunucuya TCP bağlantısı kurulamadı (ayrıntı günlükte).";
        return new Result(lines, summary);
    }

    static bool IsReset(string text) =>
        text.Contains("reset", StringComparison.OrdinalIgnoreCase) || text.Contains("zorla kapatıldı", StringComparison.OrdinalIgnoreCase)
        || text.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase) || text.Contains("10054");

    static string Flat(Exception ex)
    {
        var parts = new List<string>();
        for (var e = ex; e != null; e = e.InnerException) parts.Add($"{e.GetType().Name}: {e.Message.Split('\n')[0].Trim()}");
        return string.Join(" → ", parts.Distinct());
    }

    static async Task<string?> TcpAsync(IPAddress ip, int port, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(8));
        using var client = new TcpClient();
        try { await client.ConnectAsync(ip, port, cts.Token); return null; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return "8 sn zaman aşımı"; }
        catch (SocketException ex) { return $"hata — {ex.SocketErrorCode}: {ex.Message}"; }
    }

    /// TLS el sıkışması; sni null ise SNI gönderilmez (hedef adı olarak IP verilir). Sertifika her durumda denetlenir:
    /// ad eşleşmesi SNI'li denemede, zincir (iptal denetimi dahil) ayrıca X509Chain ile raporlanır.
    static async Task<(bool Ok, string Text)> HandshakeAsync(IPAddress ip, string host, string? sni, SslProtocols proto, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(15));
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(ip, 443, cts.Token);
            X509Certificate2? cert = null;
            SslPolicyErrors policy = SslPolicyErrors.None;
            using var ssl = new SslStream(client.GetStream(), false, (_, c, _, errors) =>
            {
                if (c != null) cert = new X509Certificate2(c);
                policy = errors;
                return errors == SslPolicyErrors.None;    // doğrulama kapatılmaz
            });
            try
            {
                await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = sni ?? ip.ToString(), EnabledSslProtocols = proto,
                    CertificateRevocationCheckMode = X509RevocationMode.Online,
                }, cts.Token);
            }
            catch (AuthenticationException) when (cert != null)
            {
                return (false, $"sertifika kabul edilmedi ({policy}) — {CertText(cert)}");
            }
            return (true, $"el sıkışması tamam ({ssl.SslProtocol}) — {CertText(cert)}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return (false, "15 sn zaman aşımı"); }
        // Win32Exception: TLS 1.3 Windows 10'da desteklenmeyince bu türle gelebiliyor
        catch (Exception ex) when (ex is IOException or SocketException or AuthenticationException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
        {
            return (false, Flat(ex));
        }
    }

    static string CertText(X509Certificate2? cert)
    {
        if (cert == null) return "sertifika yok";
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(15);
        bool ok = chain.Build(cert);
        var status = chain.ChainStatus.Select(s => s.Status.ToString()).Distinct().ToList();
        return $"konu {cert.Subject}; veren {cert.Issuer}; geçerlilik {cert.NotBefore:yyyy-MM-dd} – {cert.NotAfter:yyyy-MM-dd}; " +
               $"zincir {(ok ? "geçerli" : "geçersiz")} ({chain.ChainElements.Count} halka{(status.Count > 0 ? ", " + string.Join(", ", status) : "")})";
    }

    static async Task<string> HttpAsync(string url, SslProtocols proto, CancellationToken ct)
    {
        using var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions { EnabledSslProtocols = proto, CertificateRevocationCheckMode = X509RevocationMode.Online },
            ConnectTimeout = TimeSpan.FromSeconds(15),
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("FredPull/1.0 (+https://github.com/bemonths/FredPull; county housing raw-data downloader)");
        try
        {
            using var resp = await http.GetAsync(url, ct);
            return $"HTTP {(int)resp.StatusCode}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or AuthenticationException or PlatformNotSupportedException)
        {
            return Flat(ex);
        }
    }

    static async Task<string> ProcessAsync(string exe, string args, CancellationToken ct)
    {
        if (!File.Exists(exe)) return $"{Path.GetFileName(exe)} bulunamadı";
        try
        {
            using var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8, StandardErrorEncoding = System.Text.Encoding.UTF8,
            })!;
            var so = p.StandardOutput.ReadToEndAsync(ct);
            var se = p.StandardError.ReadToEndAsync(ct);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(40));
            try { await p.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { try { p.Kill(true); } catch (InvalidOperationException) { } return "40 sn zaman aşımı"; }
            var text = ((await so).Trim() + " " + (await se).Trim()).Trim();
            return $"{text} (çıkış kodu {p.ExitCode})".Replace("\r", " ").Replace("\n", " ");
        }
        catch (System.ComponentModel.Win32Exception ex) { return "başlatılamadı — " + ex.Message; }
    }
}
