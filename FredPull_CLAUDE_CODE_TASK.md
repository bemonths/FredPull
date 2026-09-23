# FredPull — Görev: "Ev kartları" (Redfin ilan toplayıcı) + 3 düzeltme

Proje: FredPull, .NET 8 WinForms (MainForm.cs, Analyzer.cs, Models.cs, FredClient.cs, RedfinLoader.cs, CountyCatalog.cs).
Mevcut akış: FRED + Redfin market tracker verisiyle county tablosu, sinyal, skor, okuma metni. Bu görev iki parça:
A) Her county için "aylardır satılamayan tek bir ev" bulan otomatik toplayıcı (Playwright ile Redfin).
B) Üç bekleyen düzeltme.

Kod stili: mevcut dosyalarla aynı (tek Form, kod ile kurulan UI, System.Text.Json, InvariantCulture). Aşırı soyutlama yok.

---

## A) Ev kartları — RedfinListingPicker

### A1. NuGet
`Microsoft.Playwright` (son 1.x). İlk çalıştırmada Chromium yoksa kur:
`Microsoft.Playwright.Program.Main(new[] { "install", "chromium" })` — bunu "Ev kartlarını topla" düğmesine ilk basışta, kullanıcıya mesajla haber vererek çalıştır (1-2 dk sürer).

### A2. Redfin'de county adresi bulma
Redfin county sayfası: `https://www.redfin.com/county/{regionId}/{ST}/{County-Name}` (örn. `/county/471/FL/Lee-County`).
regionId'yi bulmak için otomatik tamamlama uç noktası:
`https://www.redfin.com/stingray/do/location-autocomplete?location={URL-encoded "Lee County, FL"}&v=2`
Yanıt `{}&&{...}` ön ekiyle gelen JSON; `payload.sections[].rows[]` içinde `url` alanı `/county/471/FL/Lee-County` biçiminde ve `type` county satırı. Bunu Playwright `page.GotoAsync` ile açıp `page.ContentAsync()`'ten ya da `APIRequestContext` ile al. `{}&&` ön ekini kes, JSON parse et. Bulunamazsa county için "Redfin bölgesi bulunamadı" yaz, atla.
regionId'yi `out/redfin_regions.json` içinde önbellekle (state+fips → regionId, url).

### A3. Filtreli liste sayfası
Adres kalıbı (doğrulandı):
`{countyUrl}/filter/property-type=house,min-days-on-market=90,min-price={min}k,max-price={max}k`
Daire modu için `property-type=condo`.
Fiyat bandı: county'nin FRED medyan liste fiyatı (CountyResult.ListPrice) varsa min = 0,6 × medyan, max = 1,15 × medyan; 5.000'e yuvarla, k cinsinden yaz. Yoksa 200k–450k.

Sayfa yüklenince ilan verisi sayfanın kendi XHR isteğiyle gelir. İki yol, ikisini de uygula (önce 1, olmazsa 2):
1. **Yanıt yakalama (tercih):** `page.Response` olayında URL'si `/stingray/api/gis?` içeren yanıtı al, gövdesi `{}&&{ "payload": { "homes": [...] } }`. Ön eki kes, parse et.
2. **HTML ayrıştırma (yedek):** `page.ContentAsync()` içinde `"text":"{}&&{` ile başlayan JSON-string bloklarını bul (HTML'de string olarak kaçışlanmış: `\"`, `\\u002F`). Her bloğu JSON string olarak çöz (yani `"` + blok + `"` parse edilince düz metin çıkar), `{}&&` kes, parse et; `payload.homes` olan bloğu kullan.

Her `homes[]` kaydında kullanılacak alanlar (doğrulandı):
- `price.value` (int, $)
- `timeOnRedfin.value` (ms; gün = value / 86400000)
- `dom.value` (gün; bazen 1 döner, güvenme; timeOnRedfin'i kullan)
- `propertyType` (6 = müstakil ev, 3 = daire, 13 = townhouse, 8 = arsa/diğer)
- `isNewConstruction` (bool)
- `city`, `state`, `zip`
- `streetLine.value`
- `yearBuilt.value`, `sqFt.value`, `beds`, `baths`
- `url` (göreli, örn. `/FL/Cape-Coral/152-Nicholas-Pkwy-E-33990/home/61878048`; HTML yolunda `\u002F` kaçışlı gelir)
- `mlsStatus` ("Active")

Sayfa en fazla 350 ilan taşır (`num_homes=350`); yeterli, sayfalama yapma.

### A4. Aday seçme kuralları
Listeden aday filtresi:
- propertyType == 6 (daire modunda 3)
- gün ≥ 90
- isNewConstruction == false ve yearBuilt ≤ (bu yıl − 2)  → müteahhit stokunu ele
- beds ≥ 2
- fiyat bandın içinde (zaten filtreli)
Sırala: gün azalan. İlk 5 adayı al.

### A5. İlan sayfası ve fiyat geçmişi
Her aday için `https://www.redfin.com{url}` aç. Fiyat geçmişi sayfanın gömülü JSON'unda `payload.propertyHistoryInfo.events[]` (doğrulandı). Yakalama: `page.Response` içinde gövdesinde `"propertyHistoryInfo"` geçen yanıt; yedek: A3-2 ile aynı HTML ayrıştırma, `payload.propertyHistoryInfo` olan blok.

`events[]` alanları (doğrulandı):
- `eventDate` (ms epoch)
- `eventDescription`: "Listed", "Price Changed", "Pending", "Sold (MLS)", "Sold (Public Records)", "Listing Removed", "Relisted"
- `price` (int veya null)
- `mlsDescription` ("Active", "Pending", "Closed"...)
- `source`

Olaylar yeni → eski sıralı. Hesap:
- Mevcut ilan = en yeni "Listed" (veya "Relisted") olayı ve ondan sonra gelen (tarihçe olarak daha yeni) olaylar.
- originalPrice = o "Listed" olayının price'ı; listedDate = eventDate.
- cuts = mevcut ilan içindeki "Price Changed" olaylarının listesi (tarih, fiyat), yeni→eski değil eski→yeni sırayla sakla.
- currentPrice = en yeni "Price Changed" price'ı, yoksa originalPrice.
- days = bugün − listedDate.
- lastSale = mevcut ilandan önceki en yeni "Sold (MLS)" ya da "Sold (Public Records)" olayı (tarih, fiyat). Varsa "alış fiyatı" olarak kullan.
- Ek: mevcut ilandan önceki "Listed"/"Listing Removed" çiftleri = daha önce satılamayıp çekilmiş mi (bool, previouslyWithdrawn).

Seçim: cuts.Count ≥ 2 olan adaylar arasından önce cuts.Count azalan, sonra days azalan. Hiç ≥2 yoksa cuts.Count ≥ 1 ve days ≥ 120 olanı al; o da yoksa "uygun ev bulunamadı" yaz.

### A6. Çıktı
`out/listings_{STATE}.json`: county başına seçilen ev + tüm adayların özeti.
`out/listings_{STATE}.csv`: fips, county, city, street, url, yearBuilt, sqft, beds, listedDate, originalPrice, currentPrice, cutCount, totalCut, days, lastSaleDate, lastSalePrice, previouslyWithdrawn, cardText.

`cardText` (Türkçe, tek satır), örnek:
"Cape Coral: 3 Haziran'da 439.900 $'a çıktı, 2 indirimle 399.900 $, 112 gündür satılık; sahibi Ocak 2022'de 500.000 $'a almıştı."
(lastSale varsa son cümle eklenir; yoksa eklenmez.)

### A7. UI
- Üst çubuğa düğme: **"Ev kartlarını topla"**. Tabloda seçili satırlar için çalışır (DataGridView MultiSelect = true yap); hiç seçim yoksa tüm county'ler için sorar.
- Mod seçimi: satır bazında değil; düğmenin yanında küçük bir açılır liste: "Müstakil ev" / "Daire". (Miami-Dade için kullanıcı Daire seçip ayrı çalıştırır.)
- İlerleme durum çubuğunda: "Lee County — liste açılıyor / 5 aday / 152 Nicholas Pkwy E fiyat geçmişi...".
- Sonuç: sağ paneldeki bilgi metninin en üstüne, seçili county'nin "EV KARTI" bölümü olarak eklenir (cardText + adaylar). Ayrıca `ranking_{STATE}.csv`'ye `house_card` sütunu eklenir.
- Playwright: `Headless = false` (Redfin bot engeline daha az takılır), sayfalar arası 3–5 sn rastgele bekleme, `UserDataDir` ile kalıcı profil (`baseDir/pw-profile`) — `BrowserType.LaunchPersistentContextAsync`. Tek tarayıcı, sırayla sayfalar. `WaitUntil = NetworkIdle`, zaman aşımı 45 sn. Hata olursa county'yi atla, log'a yaz (`out/log_listings.txt`), devam et.

### A8. Kabul testi
Florida, Lee County, mod = Müstakil ev, band 200k–450k ile çalıştır. Beklenen: seçilen ev **152 Nicholas Pkwy E, Cape Coral**:
- listedDate 2026-06-03, originalPrice 439900
- cuts: 2026-06-25 → 419900, 2026-08-04 → 399900
- currentPrice 399900, cutCount 2, totalCut 40000
- lastSale 2022-01-14 / 500000
(Redfin bu ilanı kaldırdıysa, aynı kurallarla başka bir ev çıkması normal; o zaman cutCount ≥ 2 ve days ≥ 90 olduğunu doğrula.)

---

## B) Üç düzeltme

### B1. Küçük taban uyarısı
Analyzer.Summarize sonunda: `Active < 300` veya (Redfin varsa) son ay `homes_sold < 40` ise `r.Signal = "Küçük taban"` yap, skoru koru ama grid satırını gri (ForeColor gri, arka plan beyaz) göster, okuma metninin sonuna "Uyarı: az ilanlı county, yüzdeler güvenilmez." ekle. Sıralamada bu satırlar en alta gitsin (OrderByDescending(Score) yerine önce Signal != "Küçük taban").

### B2. "Redfin'i mevcut sonuca ekle" düğmesi
Üst çubuğa düğme. Mevcut `_snap` için `AttachRedfin` çalıştırır (FRED'i yeniden çekmez), `Summarize` tekrar, `FillGrid`, `SaveOutputs`, `HasRedfin = true`. Redfin dosyası yoksa mesaj.

### B3. Sinyal eşiği
ComputeSignal'da ilk kural: `MonthsSupply >= 7` ise satış düşüşü şartı aranmadan "Alıcı çekildi". (Miami-Dade 7,7 aylık stokla "Dengeli" çıkıyordu.)

---

## Notlar
- Redfin sayfalarındaki JSON'da `\u002F` kaçışları `/` demektir; JSON string çözümü bunu otomatik açar.
- Redfin bot engeli: sayfa "Access Denied" dönerse 60 sn bekleyip bir kez daha dene, sonra county'yi atla.
- Bu görev bitince kullanıcı `out/listings_FL.csv` dosyasını sohbete yükleyecek; oradaki `cardText` doğrudan senaryoya girecek.
