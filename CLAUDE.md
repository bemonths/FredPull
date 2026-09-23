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
- Çalışma dosyaları exe yanında (`bin\Debug\net8.0-windows\`): `fred_api_key.txt`, `counties_all.txt`, `redfin\`, `out\`, `pw-profile\`. Hepsi `.gitignore`'da; **API anahtarını asla commit'leme.**

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

## Arayüz
Üst çubuk iki satır:
1. Eyalet · ilçe sayısı · FRED API anahtarı · **Verileri çek** · İptal · Son sonucu yükle · out klasörü
2. Redfin verisini indir · Redfin tarihi · **Redfin'i mevcut sonuca ekle** · Ev kartı: [Müstakil ev / Daire] · Bant (bin $) · **Ev kartlarını topla**

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
Seçili satırlar için (seçim yoksa hepsini sorar) sırayla çalışır; tek görünür Chromium (`Headless = false`), kalıcı profil `pw-profile`, sayfalar arası 3-5 sn bekleme, 45 sn zaman aşımı. İlk kullanımda Chromium yoksa `playwright install chromium` çalıştırır.
1. **County adresi:** `stingray/do/location-autocomplete` → `/county/{id}/{ST}/{Ad}`; `out\redfin_regions.json`'da önbelleklenir.
2. **Liste:** `{countyUrl}/filter/property-type=house|condo,min-days-on-market=90,min-price=Xk,max-price=Yk`. Bant: Bant kutusu doluysa o (ör. `200-450`), değilse FRED medyan liste fiyatının 0,6-1,15 katı (5.000'e yuvarlı), o da yoksa 200k-450k. İlan verisi önce sayfanın `/stingray/api/gis?` yanıtından yakalanır, olmazsa HTML'e gömülü `"text":"{}&&{...}"` bloklarından okunur.
3. **Aday:** müstakil = propertyType 6 (daire 3), `timeOnRedfin` ≥ 90 gün, yeni inşaat değil, yearBuilt ≤ bu yıl − 2, en az 2 oda, bant içinde, mlsStatus "Active". Gün azalan ilk 5.
4. **Fiyat geçmişi:** ilan sayfasında `payload.propertyHistoryInfo.events`. Mevcut ilan = en yeni Listed/Relisted. İndirim = bir önceki fiyattan **düşük** Price Changed (artış sayılmaz). Son satış = mevcut ilandan önceki en yeni Sold kaydı (fiyatsızsa 90 gün içindeki fiyatlı kaydı). previouslyWithdrawn = son satıştan bu yana Removed/Delisted/Withdrawn/Expired.
5. **Seçim:** 2+ indirim (indirim sayısı, sonra gün azalan); yoksa 1 indirim + ≥ 120 gün; yoksa "uygun ev bulunamadı".
6. **cardText:** `Cape Coral: 3 Haziran'da 439.900 $'a çıktı, 2 indirimle 399.900 $, 112 gündür satılık; sahibi Ocak 2022'de 500.000 $'a almıştı.` Bu yıl değilse tarih yıllı yazılır (`1 Eylül 2025'te`). Bulunma ekleri ay adı ve yılın okunuşuna göre (`MonthSuffix`, `YearSuffix`); sayılar noktayla binlik ayrılır.
7. Engel ("Access Denied", HTTP 403/429, CloudFront "Request blocked"): 60 sn bekle, bir kez daha dene, olmazsa ilçeyi atla. Tarayıcı penceresi kapatılırsa toplama durur.

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

## Açık konular
- **Canlı Redfin çalıştırması hiç yapılmadı.** Kabul testi: Florida → Lee County satırını seç, "Müstakil ev", Bant `200-450`, "Ev kartlarını topla". Beklenen: 152 Nicholas Pkwy E, Cape Coral (listed 2026-06-03 / 439.900, indirimler 06-25 → 419.900, 08-04 → 399.900, son satış 2022-01-14 / 500.000). İlan kalkmışsa başka bir ev çıkması normal; cutCount ≥ 2 ve days ≥ 90 olmalı.
- Ayrıştırma md'deki Redfin alan adlarına dayanıyor; Redfin yapı değiştirirse ilk bakılacak yer `ReadHomes`, `ReadEvents`, `EmbeddedBlocks` ve `FindRegionAsync`.
- Redfin, Claude'un tarayıcısından istekleri CloudFront 403 ile engelledi; engel algılama bunu tanır ama engelin aşılmaya çalışılmaması gerekir.
- Okuma uyarısı md'deki gibi "az ilanlı county" diyor; arayüz geri kalanında "ilçe" kullanıyor (kullanıcı isterse değiştirilecek).
