# FredPull — proje notu (Claude oturumları için)

Bu dosya projeyi devralan Claude oturumu içindir: ne yapıldığı, nasıl çalıştığı, nerede ne olduğu. Claude Code bu dosyayı depoda otomatik okur.

## Ne yapar
ABD ilçelerinin (county) konut piyasasını iki kaynaktan okuyup her ilçe için tek kelimelik **sinyal**, 0-100 **skor** ve Türkçe **okuma metni** üretir; ayrıca her ilçe için Redfin'den "aylardır satılamayan tek bir ev" seçip **ev kartı** cümlesi yazar.
- FRED (Realtor.com ilan verisi): satılık ev, yeni ilan, sözleşme, ilan süresi, fiyat kıran ilan, medyan liste fiyatı.
- Redfin Data Center (market tracker .tsv.gz): satılan ev, satış fiyatı, satış/liste, aylık stok.
- Redfin sitesi (Playwright ile): tek tek ilanlar ve fiyat geçmişleri → ev kartları.

## İş akışı (kullanıcıyla)
1. Kullanıcı başka bir Claude sohbetinde görev dosyası yazdırır (ör. `FredPull_CLAUDE_CODE_TASK.md`).
2. Claude Code bu dosyayı projeye uygular. **Kullanıcının sohbetteki isteği md ile çelişirse kullanıcı kazanır.**
3. Kullanıcı uygulamayı çalıştırır; `out\listings_XX.csv` içindeki `cardText` ve okuma metinleri doğrudan **video senaryosuna** girer. Bu yüzden Türkçe metin doğruluğu (ekler, sayı biçimi) önemlidir.

## Teknik
- .NET 8 WinForms (`net8.0-windows`), tek form. Paketler: `ScottPlot.WinForms 5.0.*`, `Microsoft.Playwright 1.62.0`.
- Derleme: `dotnet build` ya da Visual Studio 2026 (`FredPull.sln`). Uyarı olarak yalnızca OpenTK NU1701 (ScottPlot bağımlılığı, zararsız) çıkar.
- Çalışma dosyaları exe yanında (`bin\Debug\net8.0-windows\`): `fred_api_key.txt`, `counties_all.txt`, `redfin\`, `out\`, `pw-profile\`, `pw-profile-chromium\`. Hepsi `.gitignore`'da; **API anahtarını asla commit'leme.**

## Dosyalar
| Dosya | Görev |
|---|---|
| `MainForm.Designer.cs` | Bütün arayüz (`InitializeComponent`). VS tasarımcısı bunu okur. |
| `MainForm.cs` | Davranış: olaylar, veri çekme, tablo, sağ panel, kayıt/yükleme, ev kartı akışı. |
| `Analyzer.cs` | Seri yardımcıları, `Summarize` (metrikler), skor, sinyal, okuma metni, sözlük (`Glossary`). |
| `Models.cs` | `County`, `Obs`, `SeriesSet`, `CountyResult`, `Snapshot`, `HouseCandidate`, `HouseCard`, `PriceCut`. |
| `FredClient.cs` | FRED API (2 paralel istek, ~109/dk, 5 deneme). |
| `RedfinLoader.cs` | Redfin market tracker dosyalarını indirir ve ayrıştırır; ilçe adı normalleştirme (`NormalizeName`). |
| `RedfinListingPicker.cs` | Playwright ile Redfin ilan toplayıcısı (ev kartları), listings dosyaları. |
| `CountyCatalog.cs` | Eyalet listesi, Sayım Bürosu ilçe/FIPS kataloğu, Florida yedeği. |
| `FredPull_CLAUDE_CODE_TASK.md` | 2026-09-23'te uygulanan görev tanımı (tarihsel kayıt). |
| `FredPull_TASK_EK_Redfin403.md` | Aynı gün uygulanan ek: Redfin 403'e karşı tarayıcı ayarları (tarihsel kayıt). |

## Arayüz
Üst çubuk iki satır:
1. Eyalet · ilçe sayısı · FRED API anahtarı · **Verileri çek** · İptal · Son sonucu yükle · out klasörü
2. Redfin verisini indir · Redfin tarihi · **Redfin'i mevcut sonuca ekle** · Ev kartı: [Müstakil ev / Daire] · Bant (bin $) · ☐ Açık Chrome'a bağlan (9222) · **Ev kartlarını topla**

Sol: 10 sütunlu tablo (çoklu seçim açık; Ctrl/Shift, Ctrl+A). Sağ: metrik grafiği (ScottPlot) + bilgi metni. Bilgi metni odaktaki satırı (`CurrentRow`) gösterir; ilçenin ev kartı varsa en üstte "EV KARTI" bölümü çıkar.

**Arayüz kuralı:** yeni kontrol `MainForm.Designer.cs`'e tasarımcının okuyabileceği biçimde eklenir (lambda yok, olay → isimli metot, `ISupportInitialize` Begin/EndInit). Constructor'da yalnızca veriyle doldurma kalır (`Items`, `SelectedIndex`). `_cbState.SelectedIndexChanged` bilerek constructor'da bağlanır (tasarımcıda bağlanırsa açılışta önbellek iki kez yüklenir).

## Veri akışı
1. Açılış: katalog (`counties_all.txt`) → seçili eyaletin `out\cache_XX.json` önbelleği ve `out\listings_XX.json` ev kartları yüklenir.
2. **Verileri çek:** eyalet + ABD + her ilçe için 6 FRED serisi (Florida ≈ 414 istek ≈ 4 dk). Redfin dosyası varsa `AttachRedfin` satış serilerini ilçe adıyla eşler.
3. `Analyzer.Summarize` her ilçe için metrikleri, skoru, sinyali ve okuma metnini hesaplar (önbellekten yüklerken de yeniden çalışır, yani kural değişiklikleri eski veriye de uygulanır).
4. `FillGrid` → tablo; `SaveOutputs` → ranking csv, seri csv'leri, önbellek.
5. **Redfin'i mevcut sonuca ekle:** FRED'i yeniden çekmeden `_snap`'e `AttachRedfin`, sonra Summarize + FillGrid + SaveOutputs. Eşleşmeyen ilçelerin eski Redfin serisi temizlenir.

## Sinyal ve skor
- Skor (0-100): aylık stok, satış Y/Y, fiyatın zirveden düşüşü, 2019'a göre stok, fiyat kıran payı (eyalete göre), satış/liste eşiklerinden toplanır. Ayrıntı `Analyzer.Glossary`.
- Sinyal sırası (`ComputeSignal`): **aylık stok ≥ 7 → "Alıcı çekildi"** · satış ≤ -10% ve stok yüksek → "Alıcı çekildi" · "Satıcı çekiliyor" · "Fiyat kırılıyor" · "Sıcak" · skor ≥ 40 → "Zayıflıyor" · "Dengeli".
- **"Küçük taban"**: satılık ev < 300 ya da (Redfin varsa) son ay satılan < 40. Skor korunur, sinyal "Küçük taban" olur, satır gri/beyaz, okuma sonuna "Uyarı: az ilanlı county, yüzdeler güvenilmez." eklenir; tabloda ve `ranking_XX.csv`'de en alta iner (`Ranked()`).

## Ev kartları (`RedfinListingPicker`)
Seçili satırlar için (seçim yoksa hepsini sorar) sırayla çalışır; tek görünür tarayıcı, sayfalar arası 3-5 sn bekleme, 45 sn gezinme zaman aşımı. Sayfalar `DOMContentLoaded` ile açılır, aranan veri (yakalanan yanıt ya da HTML'deki blok) gelene kadar en çok 20 sn saniyede bir yoklanır. `NetworkIdle` **kullanılmaz**: Redfin arka planda hiç susmadığı için ilk canlı çalıştırmada her sayfa 45 sn zaman aşımına düşüyordu (ilçe başına ~5 dk).

**Tarayıcı** (Redfin, Playwright'ın kendi Chromium'unu CloudFront 403 "Request blocked" ile engellediği için):
- Varsayılan: kurulu **Google Chrome** (`Channel = "chrome"`), profil `pw-profile`, `--enable-automation` kapalı, `--disable-blink-features=AutomationControlled`, `ViewportSize.NoViewport`, `Locale en-US`, açılışta `navigator.webdriver` → undefined (init script). Chrome açılamazsa log'a yazıp Playwright Chromium'a düşer (ayrı profil `pw-profile-chromium`; yoksa `playwright install chromium`).
- **"Açık Chrome'a bağlan (9222)"** onay kutusu: kullanıcının `chrome.exe --remote-debugging-port=9222 --user-data-dir="%USERPROFILE%\pw-chrome"` ile açtığı Chrome'a `ConnectOverCDPAsync` ile bağlanır, yeni bir sekmede çalışır; bitince yalnızca o sekmeyi kapatıp bağlantıyı keser (kullanıcının Chrome'u açık kalır). Bağlanamazsa mesaj komutu gösterir ve Chrome'u bu ayarla başlatmayı önerir.

1. **County adresi:** stingray adresine sayfa olarak **gidilmez**. Sayfa redfin.com'da değilse önce `https://www.redfin.com/` normal açılır, 3-5 sn beklenir, sonra `page.EvaluateAsync` ile site içinden `fetch('/stingray/do/location-autocomplete?...', {credentials:'include'})` çağrılır → `/county/{id}/{ST}/{Ad}`; `out\redfin_regions.json`'da önbelleklenir.
2. **Liste:** `{countyUrl}/filter/property-type=house|condo,min-days-on-market=90,min-price=Xk,max-price=Yk`. Bant: Bant kutusu doluysa o (ör. `200-450`), değilse FRED medyan liste fiyatının 0,6-1,15 katı (5.000'e yuvarlı), o da yoksa 200k-450k. İlan verisi önce sayfanın `/stingray/api/gis?` yanıtından yakalanır, olmazsa HTML'e gömülü `"text":"{}&&{...}"` bloklarından okunur (canlıda Lee County listesi HTML'de geldi). Redfin sayfa başına en çok **350** ilan verir ve sınırdayken her seferinde farklı bir 350 döndürür; liste 350'ye dayanırsa fiyat bandı ikiye bölünüp parçalar ayrı açılır, gerekirse tekrar bölünür (`LoadBandAsync`, ilçe başına en çok 40 liste sayfası; kullanıcı için kapsam süreden önemli).
3. **Aday:** müstakil = propertyType 6 (daire 3), `timeOnRedfin` ≥ 90 gün, yeni inşaat değil, yearBuilt ≤ bu yıl − 2, en az 2 oda, bant içinde, mlsStatus "Active". Gün azalan ilk 5.
4. **Fiyat geçmişi:** ilan sayfasında `payload.propertyHistoryInfo.events`. Mevcut ilan = en yeni Listed/Relisted; bu kayıt **fiyatsızsa** (çekilip yeniden çıkan evlerde Redfin fiyat yazmayabiliyor) son satıştan bu yana fiyatlı en yeni Listed/Relisted başlangıç sayılır, ondan sonra fiyat değişikliği yoksa güncel fiyat liste sayfasından alınır. **Kira kayıtları** yok sayılır: Redfin'de `historyEventType` 1 = satış ilanı (Listed, Price Changed, Listing Removed, Relisted, Pending), 2 = satış (Sold (MLS), Sold (Public Records)), **3 = kira** ("Listed for Rent", "Rental Removed") — canlı veriden doğrulandı. Yedek olarak metinde kira sözcüğü ve 25.000 $ altı ilan/fiyat değişikliği kayıtları da ayıklanır; 25.000 $ altı satış fiyatı (sembolik devir) alış fiyatı sayılmaz. Her adayın ham tarihçesi `RawHistory` olarak listings JSON'una yazılır. İndirim = bir önceki fiyattan **en az 1.000 $ düşük** Price Changed (artış ve 430.000 → 429.999 gibi kozmetik düşüşler sayılmaz; `MinCut`; toplam düşüş yine ilk ve son fiyattan). Son satış = mevcut ilandan önceki en yeni Sold kaydı (fiyatsızsa 90 gün içindeki fiyatlı kaydı). previouslyWithdrawn = son satıştan bu yana Removed/Delisted/Withdrawn/Expired.
5. **Seçim:** 2+ indirim (indirim sayısı, sonra gün azalan); yoksa 1 indirim + ≥ 120 gün; yoksa "uygun ev bulunamadı".
6. **cardText:** `Cape Coral: 3 Haziran'da 439.900 $'a çıktı, 2 indirimle 399.900 $, 112 gündür satılık; sahibi Ocak 2022'de 500.000 $'a almıştı.` Bu yıl değilse tarih yıllı yazılır (`1 Eylül 2025'te`). Bulunma ekleri ay adı ve yılın okunuşuna göre (`MonthSuffix`, `YearSuffix`); sayılar noktayla binlik ayrılır.
7. Engel ("Access Denied", HTTP 403/429, CloudFront "Request blocked"; sayfa, autocomplete ya da ilan sayfası): 60 sn bekle, bir kez daha dene; ikinci engelde `BlockedException` → **ilçe atlanır**, URL `log_listings.txt`'e yazılır. Tarayıcı penceresi kapatılırsa (ya da bağlı Chrome kapanırsa) toplama durur.

Sonuç dosyaları çalıştırmalar arasında **ilçe bazında birleşir** (Miami-Dade'i ayrıca Daire modunda çalıştırmak diğerlerini silmez). Hata alan çalıştırma, önceki başarılı kartın üstüne yazmaz.

## Çıktılar (`out\`)
- `ranking_XX.csv` — tüm sütunlar + `reading` + `house_card`
- `series\<fips>_<ilçe>.csv` — ilçe başına aylık seriler
- `cache_XX.json` — önbellek ("Son sonucu yükle")
- `listings_XX.json` — ilçe başına seçilen ev + tüm adaylar
- `listings_XX.csv` — fips, county, city, street, url, yearBuilt, sqft, beds, listedDate, originalPrice, currentPrice, cutCount, totalCut, days, lastSaleDate, lastSalePrice, previouslyWithdrawn, cardText (yalnızca ev seçilen ilçeler)
- `redfin_regions.json`, `log.txt` (FRED), `log_listings.txt` (ev kartları)

## Kod stili
Mevcut dosyalarla aynı: tek Form, `System.Text.Json`, `InvariantCulture`, aşırı soyutlama yok, Türkçe yorumlar ve arayüz metinleri. Arayüzde "ilçe" denir.

## Geçmiş
### 2026-09-23 (Claude Code oturumu)
Commit'ler: `5a181d8` orijinal hâl · `16d94b8` aşağıdaki değişiklikler · ardından bu dosya.
- Arayüz koddan `MainForm.Designer.cs`'e taşındı; önceden VS tasarımcısında form boş görünüyordu. VS 2026 tasarımcısında açıldığı doğrulandı (`MainForm.resx` VS'nin boş şablonu).
- `FredPull_CLAUDE_CODE_TASK.md` uygulandı: A (ev kartları), B1 (küçük taban), B2 (Redfin'i mevcut sonuca ekle), B3 (7+ ay stok).
- md'ye göre farklar/eklemeler: Bant (bin $) kutusu (kabul testi 200-450 ile çalıştırılabilsin diye; Lee County'nin FRED medyanı olduğundan otomatik bant farklı çıkar), fiyat artışı indirim sayılmaz, 7+ stokla tetiklenen "Alıcı çekildi"de sonuç cümlesi "satış düşerken" demez, mlsStatus = Active şartı, previouslyWithdrawn yalnız son satıştan sonrası, listings dosyaları birleşir.
- Doğrulama: derleme temiz; ev kartı mantığı md'deki kabul testi verisiyle (152 Nicholas Pkwy E) geçici bir test projesinde sınandı, `cardText` md örneğiyle birebir aynı; uygulama açılıp B1/B3 görsel olarak kontrol edildi (Miami-Dade "Alıcı çekildi", Columbia ve Putnam "Küçük taban").

### 2026-09-23 — görev eki: Redfin 403
Redfin, Playwright Chromium'u CloudFront 403 ile engelledi. Ek uygulandı: kurulu Chrome + otomasyon işaretleri kapalı, autocomplete site içinden `fetch`, "Açık Chrome'a bağlan (9222)" modu, ikinci 403'te ilçeyi atla (ayrıntı yukarıda). Ekteki `ViewportSize = null` .NET'te sabit 1280×720 demek olduğundan niyete uygun `ViewportSize.NoViewport` kullanıldı.
Doğrulama (Redfin'e hiç istek atılmadan): Chrome 153 açılıyor ve `navigator.webdriver` undefined; 9222 kapalıyken doğru mesaj; 9222 modunda yeni sekme açılıp kapanıyor, kullanıcının Chrome'u açık kalıyor; Playwright route ile redfin.com istekleri sahte yanıtlarla karşılanarak tüm akış uçtan uca çalıştı (ana sayfa → autocomplete `fetch` → liste, gis sayfanın kendi `fetch`'inden yakalandı → 5 aday → fiyat geçmişi → 152 Nicholas Pkwy E, 2 indirim, 40.000, son satış 2022-01-14 / 500.000).
Test projesi depoda değil (geçiciydi); gerekirse ana csproj'a `<Compile Remove="tests/**" />` ekleyip `tests/` altına alınabilir.

### 2026-09-23 — ilk canlı çalıştırma ve iki düzeltme
Kullanıcı Chrome modunda (9222 değil) çalıştırdı; ilk açılışta Redfin doğrulama istedi, kullanıcı elle geçti (kalıcı profilde kalır). Lee County, bant otomatik 230k-435k: liste 350 ilan (Redfin üst sınırı), 5 aday. Seçilen **3300 Hampton Blvd, Alva**: 2 Haziran 459.900 → 7 indirimle 369.900, 113 gün, sahibi Ekim 2025'te 430.000'e almış. Kart: `Alva: 2 Haziran'da 459.900 $'a çıktı, 7 indirimle 369.900 $, 113 gündür satılık; sahibi Ekim 2025'te 430.000 $'a almıştı.` md'deki 152 Nicholas Pkwy E çıkmadı çünkü bant 200-450 değildi ve 112 günle ilk 5'e girmedi (md bu durumda başka ev çıkmasını kabul ediyor).
Bu çalıştırmadan çıkan düzeltmeler: (1) NetworkIdle yerine veri gelene kadar yoklama (yukarıda); (2) fiyatsız yeniden ilan: 210 günlük 5521 Longleaf Dr, son Relisted kaydı fiyatsız olduğu için eleniyordu. İkisi de sahte yanıtlı uçtan uca testte ve birim testlerde doğrulandı.
İkinci canlı çalıştırma (bant yine otomatik): ilçe 49 sn'de bitti (önce ~5,5 dk); Longleaf artık doğru okunuyor (13 Şubat 289.900 → 1 indirimle 284.900, 222 gün, önce çekilmiş) ama tek indirimi olduğu için yine Hampton Blvd seçildi. Liste sayfasında gis isteği hiç gelmedi, ilanlar HTML'de tam (350) vardı; bu yüzden liste için "hazır" koşuluna gömülü veri de eklendi (20 sn boş bekleme kalktı, ilçe başına ~30 sn beklenir).
Üçüncü canlı çalıştırma = md kabul testi (bant 200-450): 26 sn. Seçilen **13651 Willow Bridge Dr, North Fort Myers** (4 Haziran 450.000 → 13 Temmuz 425.000 → 7 Eylül 399.000, 111 gün, sahibi Mayıs 2023'te 360.000) → md'nin yedek şartı (cutCount ≥ 2, days ≥ 90) sağlandı. İki sorun görüldü ve düzeltildi: (1) 325 Paulcrest Ave'de 2.250 $'lık aylık kira kaydı güncel fiyat sanıldı → kira/küçük fiyat filtresi; (2) önceki çalıştırmada seçilen Hampton Blvd (369.900, 113 gün) bu bantta olmasına rağmen listede yoktu → 350 sınırı, bant bölme. Nicholas'ın çıkmamasının da büyük ihtimalle nedeni buydu. Sahte yanıtlı testte 800 ilanlık ilçe 7 sayfada tamamen okundu.
Dördüncü canlı çalıştırma (bant 200-450, bant bölme ile): 77 sn, 9 liste sayfası, **1129 ilan**; 325k-450k bandı 8 sayfalık bütçe dolduğu için 350'de kaldı → bütçe 16'ya çıkarıldı (bölmeden önce 2 sayfa yer var mı bakılıyor). Adaylar artık gerçekten eski: 911, 742, 728, 723, 661 gün. Seçilen **1208 Barnsdale St, Lehigh Acres** (1969 yapımı, 1.323 sqft): 11 Eylül 2024'te 349.900 → 9 indirimle 229.900, 742 gün, Ağustos 2025'te çekilip Ekim'de fiyatsız yeniden ilana çıkmış, sahibi Haziran 2010'da 63.500'e almış. Kart: `Lehigh Acres: 11 Eylül 2024'te 349.900 $'a çıktı, 9 indirimle 229.900 $, 742 gündür satılık; sahibi Haziran 2010'da 63.500 $'a almıştı.` Ham tarihçeyle satır satır karşılaştırıldı, doğru. Ham tarihçelerde kira kayıtları görüldü (`historyEventType` 3) ve kural buna göre sıkılaştırıldı; 5 adayın canlı tarihçesi yeni kuralla yeniden hesaplandı, sonuçlar aynı.
Beşinci canlı çalıştırma (bütçe 16): 1788 ilan, 15 sayfa, 1 dk 46 sn; 325k-385k aralığından ilçenin en eski ilanı çıktı (1825 Tomaso Ave, Temmuz 2022'den beri, 1530 gün, 1 indirim) ama 385k-450k yine 350'de kaldı. Kullanıcı "süre önemli değil, bekleriz" dedi → bütçe 40.
Altıncı canlı çalıştırma (bütçe 40): 1897 ilan, 17 sayfa, ~2 dk, bütçe uyarısı yok. 385k-450k bandından gelen **16525 Wellington Lakes Cir, Fort Myers** (2000 yapımı, 2.264 sqft, 4 oda) seçildi: 15 Ağustos 2024 580.000 → 410.000, 769 gün, Ekim-Aralık 2024 ilandan kalkmış, sahibi Şubat 2017'de 310.000'e almış. İndirimlerden biri 430.000 → 429.999 (1 $) olduğu için 1.000 $ eşiği eklendi (kullanıcı: "düşük indirimleri atlayalım ama listeyi kaybetmeyelim"); 5 adayın canlı tarihçesinde yalnızca bu ev 10 → 9 oldu. Kart: `Fort Myers: 15 Ağustos 2024'te 580.000 $'a çıktı, 9 indirimle 410.000 $, 769 gündür satılık; sahibi Şubat 2017'de 310.000 $'a almıştı.` Kullanıcının yanlışlıkla çalıştırdığı Highlands County kaydı çıktı dosyalarından silindi.

## Açık konular
- Claude ilan toplayıcıyı Redfin'e karşı kendisi çalıştırmaz; canlı çalıştırmaları kullanıcı yapar, Claude `out\log_listings.txt` ve `out\listings_XX.json`'u okuyup değerlendirir. md'deki kabul testi (Bant `200-450`) henüz çalıştırılmadı: Florida → Lee County, "Müstakil ev", Bant `200-450`, "Ev kartlarını topla" (403 sürerse "Açık Chrome'a bağlan (9222)" ile).
- Log'da "sayfa bütçesi doldu" satırı çıkarsa o bantta eski ilanlar kaçmış olabilir; `MaxListPages` (40) artırılabilir. Büyük ilçelerde (Lee, Miami-Dade, Broward, Palm Beach, Hillsborough...) ilçe başına 2-3 dk sürebilir; kullanıcı beklemeyi kabul ediyor.
- Seçim kuralı (en çok indirim) uzun süredir ilanda olan, arada çekilip yeniden çıkan evleri öne çıkarıyor; kartta "742 gündür satılık" derken arada ~2 ay ilandan kalkmış olabilir (`previouslyWithdrawn` sütununa bakılmalı). Beklenen: 152 Nicholas Pkwy E, Cape Coral (listed 2026-06-03 / 439.900, indirimler 06-25 → 419.900, 08-04 → 399.900, son satış 2022-01-14 / 500.000). İlan kalkmışsa başka bir ev çıkması normal; cutCount ≥ 2 ve days ≥ 90 olmalı.
- Ayrıştırma md'deki Redfin alan adlarına dayanıyor; Redfin yapı değiştirirse ilk bakılacak yer `ReadHomes`, `ReadEvents`, `EmbeddedBlocks` ve `FindRegionAsync`.
- Okuma uyarısı md'deki gibi "az ilanlı county" diyor; arayüz geri kalanında "ilçe" kullanıyor (kullanıcı isterse değiştirilecek).
