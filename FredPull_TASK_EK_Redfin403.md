# FredPull görev eki — Redfin 403 (CloudFront "Request blocked")

1) Kendi Chromium yerine kurulu Google Chrome ile aç:
   LaunchPersistentContextAsync(userDir, new() {
     Channel = "chrome", Headless = false,
     IgnoreDefaultArgs = new[] { "--enable-automation" },
     Args = new[] { "--disable-blink-features=AutomationControlled", "--no-first-run", "--no-default-browser-check" },
     ViewportSize = null, Locale = "en-US"
   });
   Açılışta bir kez await ctx.AddInitScriptAsync("Object.defineProperty(navigator,'webdriver',{get:()=>undefined});");
   Chrome kurulu değilse eski Chromium'a düş ve log'a yaz.

2) Hiçbir stingray API adresine sayfa olarak gitme. Autocomplete için:
   - önce https://www.redfin.com/ sayfasını normal aç (NetworkIdle bekleme, 3-5 sn dur),
   - sonra page.EvaluateAsync ile site içinden çağır:
     fetch('/stingray/do/location-autocomplete?location=' + encodeURIComponent(q) + '&v=2', {credentials:'include'}).then(r => r.text())
   - dönen metni ParseRedfin ile çöz (aynı {}&& ön eki).
   Liste ve ilan sayfaları zaten normal sayfa; onları değiştirme.

3) Yedek mod "Açık Chrome'a bağlan": kullanıcı kendi Chrome'unu
   chrome.exe --remote-debugging-port=9222 --user-data-dir="C:\Users\1\pw-chrome"
   ile başlatır ve Redfin'i elle bir kez açar; program Playwright.Chromium.ConnectOverCDPAsync("http://localhost:9222")
   ile o tarayıcıya bağlanıp aynı akışı yürütür. Üst çubuğa onay kutusu: "Açık Chrome'a bağlan (9222)".
   Bağlanamazsa açık mesaj: "Chrome'u --remote-debugging-port=9222 ile başlat."

4) 403 alındığında 60 sn bekleyip tekrar denemeye devam; ikinci 403'te ilçeyi atla, log'a URL'yi yaz.

Kabul testi aynı: Florida / Lee County → 152 Nicholas Pkwy E, 2 indirim, 112 gün, son satış Ocak 2022 500.000.

---
Uygulandı: 2026-09-23. Farklar ve doğrulama için CLAUDE.md → Geçmiş.
