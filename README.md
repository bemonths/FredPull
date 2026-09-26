# FredPull v2 (WinForms, .NET 8)

ABD ilçelerinin konut piyasasını iki kaynaktan okur ve her ilçe için tek kelimelik sinyal, 0-100 skor ve düz Türkçe özet üretir.

- FRED (Realtor.com): satılık ev, yeni ilan, sözleşmeye bağlanan ev, ilan süresi, fiyat kıran ilan, istenen fiyat — 2016'dan bugüne, ilçe + eyalet + ABD.
- Redfin Data Center: satılan ev, gerçekleşen satış fiyatı, satış/liste oranı, aylık stok — ilçe + eyalet + ABD.

## Kurulum
1. Zip'i aç, FredPull.csproj'u Visual Studio'da aç, derle. ScottPlot.WinForms paketini kendisi indirir.
2. İlk açılışta ilçe kataloğunu (Sayım Bürosu) indirir; exe yanına counties_all.txt olarak kaydeder.
3. FRED API anahtarını yapıştır (ücretsiz: https://fred.stlouisfed.org/docs/api/api_key.html). fred_api_key.txt olarak kaydedilir.
4. "Redfin verisini indir": üç dosya, ilçe dosyası büyük (birkaç yüz MB), bir kez. exe yanındaki redfin klasörüne iner. Ayda bir yenile.
5. Eyaleti seç, "Verileri çek". Florida: 69 × 6 = 414 FRED isteği ≈ 4 dk, sonra Redfin okuması 1-2 dk.

Arayüz MainForm.Designer.cs'te; Visual Studio'da MainForm.cs'e çift tıkla (ya da Shift+F7) tasarımcı açılır. Davranış MainForm.cs'te.
Ana ekran iki sekme: "İlçeler" (tablo, grafik, okuma) ve "Video üretimi" (Harita Stüdyosu ve Flow intro prompt'u).

"Redfin'i mevcut sonuca ekle": FRED'i yeniden çekmeden, yüklü sonuca Redfin satış verisini ekler (önce "Redfin verisini indir").

## Tablo (10 sütun)
İlçe · Sinyal · Skor · Satılık ev · Stok 2019'a göre % · Aylık stok · Satış Y/Y % · Fiyat zirveden % · Satış/Liste % · İndirim payı vs eyalet
Skor 60+ kırmızı, 40-59 turuncu. Başlığa tıkla sıralanır, başlık üstünde bekle açıklama çıkar.
"Küçük taban" (satılık ev < 300 ya da aylık satış < 40) gri yazılır ve en alta iner; skor korunur.
Çoklu seçim açık: Ctrl/Shift ile birden fazla ilçe, Ctrl+A ile hepsi.

## Ev kartları (Redfin ilanları)
Her ilçe için "aylardır satılamayan tek bir ev" seçer. Tabloda ilçeleri seç, mod (Müstakil ev / Daire) ve isteğe bağlı bant (bin $, ör. 200-450; boş = ilçe medyan liste fiyatının %60-115'i) gir, "Ev kartlarını topla".
- Redfin kurulu Google Chrome'da, exe yanındaki ayrı bir profille (pw-profile) görünür pencerede açılır; bitene kadar kapatma. Chrome yoksa Playwright Chromium kullanılır (ilk kullanımda iner, 1-2 dk).
- Redfin yine 403 verirse "Açık Chrome'a bağlan (9222)" kutusunu işaretle: Chrome'u `chrome.exe --remote-debugging-port=9222 --user-data-dir="%USERPROFILE%\pw-chrome"` ile başlat (program bağlanamazsa bunu önerir ve başlatabilir), Redfin'i o pencerede bir kez elle aç, sonra "Ev kartlarını topla". Program yeni bir sekmede çalışır, bitince Chrome'un açık kalır.
- Kural: ≥90 gün ilanda, yeni inşaat değil, en az 2 oda; en eski 5 aday içinden fiyat geçmişinde 2+ indirim alan (yoksa 1 indirim + 120 gün) seçilir.
- Sonuç sağ panelin en üstünde "EV KARTI" olarak görünür; ranking_XX.csv'ye house_card sütunu eklenir.
- Redfin engellerse ("Access Denied" / 403) 60 sn bekleyip bir kez daha dener, olmazsa ilçeyi atlar ve adresi log_listings.txt'e yazar.

## Video üretimi sekmesi
### Animasyon — Harita Stüdyosu (video haritaları)
"Harita Stüdyosu klasörü" otomatik bulunur (FredPull\harita-studyosu); değilse "Seç…". Stüdyo yalnızca ana karadaki 48 eyaleti çizer; Alaska, Hawaii ve DC'de düğmeler pasiftir.
- out\metinler_XX.csv (senaryodan: order, fips, county, focus_sub, focus_stat) videodaki ilçe sırasını ve etiket metinlerini verir; içeriği sekmedeki tabloda görünür. "Metin dosyası seç…" başka yerdeki bir CSV'yi bu adla out\ klasörüne kopyalar (varsa üzerine yazmayı sorar). Dosya yoksa "İlçeler" tablosunda seçili ilçeler kullanılır.
- "Stüdyo projesi oluştur": giriş haritası + her ilçe için yakınlaşma ve fiyat merdiveni sahneleri olan projeyi stüdyonun projects klasörüne yazar ve stüdyoya doğrulatır. Stüdyo arayüzünde "Proje aç…" listesinde görünür.
- "Stüdyoda render al": projeyi oluşturur, render'ı başlatır (ilerleme alt çubukta, "İptal" durdurur), bitince çıktı klasörünü açar. "Çıktı klasörünü aç" son render'ın klasörünü sonradan da açar.

### Grafikler (grafik listesi)
Animasyon grubunun "Grafikler" alt sekmesi. out\grafikler_XX.csv hangi grafiğin videonun neresine gireceğini söyler (senaryo sohbetinden gelir ya da "Grafik ekle" ile burada yazılır); rakamlar elle girilmez, "Stüdyo projesi oluştur" onları yüklü veriden hesaplar.
- Biçim: `seq,slot,fips,chart,metrics,text,caption`. Grafikler: soru kartı (vaat ekranı), county soru kartı, satış fiyatı yılları, satılık ev (çizgi ya da sütun), fiyat kıran pay karşılaştırması, satış/liste halkası, aylık stok termometresi ve sıralaması, satılık ev ızgarası. Tanımlar CLAUDE.md'de.
- "Grafik ekle": county + grafik seç (county soru kartında üç ölçü, soru kartında simge/değer/açıklama), "Ekle". "Seçili satırı sil" satırı dosyadan çıkarır. "Grafik dosyası seç…" başka yerdeki listeyi out\ klasörüne kopyalar.
- Proje sırası: eyalet haritası → vaat ekranı → her county: yakınlaşma, soru kartı, grafikleri, fiyat merdiveni → aradaki grafikler 5. county'den sonra → kapanış grafikleri.
- Ekrana basılan her rakam out\grafik_degerleri_XX.csv'ye yazılır (makaledeki rakamlarla karşılaştırmak için). Veride olağan dışı sıçrama varsa proje özetinde uyarı çıkar.
- "Şeffaf arka plan (MOV)" işaretliyse stüdyo videoları şeffaf .mov olarak üretir.

### Intro prompt'u — Flow
Google Flow'a yapıştırılacak 10 saniyelik harita intro'su prompt'u, seçili eyalete göre hazır gelir.
- "Komşular" ve "Pin şehri" PromptData\states_intro.json'dan dolar; değiştirirsen o eyalet için hatırlanır (exe yanında intro_overrides.json). "Varsayılana dön" eski hâline getirir.
- Önizleme kutusu şablonun doldurulmuş hâlidir; "Kopyala" panoya alır.
- "Şablonu aç" şablonun sana ait kopyasını (exe yanında user_intro_prompt_template.txt; yoksa varsayılandan oluşturulur) düzenleyicide açar; kaydedince önizleme kendiliğinden yenilenir ve program kapanıp açılsa da kalır. "Şablonu varsayılana döndür" bu kopyayı siler.
- Doldurulmamış bir [YER TUTUCU] kalırsa ya da eyalet dosyada yoksa (Alaska, Hawaii, DC) kırmızı uyarı çıkar; kutuları elle doldur.

## Ev detayları
Tabloda satıra çift tıkla ya da "Ev detayları": adaylar, fiyat geçmişi ve grafiği, referans fotoğraflar.
- "Bu evi seç": kart metnine başka bir adayı koyar (listings dosyaları ve ranking güncellenir).
- "Fotoğrafları indir (referans)": en çok 12 fotoğraf out\photos\ altına iner. Emlakçı/MLS telifli; videoda kullanılmaz, yalnızca referans.
- "KML dışa aktar": seçilen evler out\earthstudio_XX.kml (Google Earth Studio). Koordinatı olmayanlar için ilan sayfasından tamamlamayı önerir.
- "Animasyon CSV": seçilen her ev için out\anim\XX\<fips>.csv (date, price, cut) — fiyat sayacı ve indirim etiketi için.
- Tarayıcı açan işler (fotoğraf, konum tamamlama) ev kartı toplama sürerken yapılamaz; toplama bitince dene.

## Sağ panel
Seçili ilçe için: okuma (3-5 cümle, sonunda "Sonuç"), ilçe / eyalet / ABD yan yana rakamlar, skor dökümü, tanımlar.
Grafik: seçili metrik; ilan süresi, indirim payı, aylık stok, satış/liste metriklerinde eyalet ve ABD çizgisi de çizilir; stok ve satış sayılarında 2019 çizgisi; fiyatlarda zirve çizgisi.

## Çıktılar (exe yanındaki out\)
- ranking_XX.csv — bütün sütunlar + okuma metni
- series\<fips>_<county>.csv — ilçe başına aylık FRED + Redfin serileri (grafik için)
- cache_XX.json — önbellek, "Son sonucu yükle"
- log.txt — FRED istek günlüğü
- listings_XX.json / listings_XX.csv — ev kartları (seçilen ev + adaylar; csv'de cardText senaryo cümlesi). Yeni çalıştırmalar ilçe bazında birleşir.
- redfin_regions.json — ilçe → Redfin county adresi önbelleği
- log_listings.txt — ev kartı günlüğü

## Sorun giderme
- Redfin eşleşen ilçe sayısı durum çubuğunda yazar; Redfin küçük ilçeleri yayınlamaz.
- Redfin URL değiştiyse RedfinLoader.cs başındaki adresleri https://www.redfin.com/news/data-center/ sayfasından güncelle.
- ScottPlot sürüm hatası: csproj'daki "5.0.*" yerine NuGet'teki son 5.0.x sürümünü yaz.
