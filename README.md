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

## Tablo (10 sütun)
İlçe · Sinyal · Skor · Satılık ev · Stok 2019'a göre % · Aylık stok · Satış Y/Y % · Fiyat zirveden % · Satış/Liste % · İndirim payı vs eyalet
Skor 60+ kırmızı, 40-59 turuncu. Başlığa tıkla sıralanır, başlık üstünde bekle açıklama çıkar.

## Sağ panel
Seçili ilçe için: okuma (3-5 cümle, sonunda "Sonuç"), ilçe / eyalet / ABD yan yana rakamlar, skor dökümü, tanımlar.
Grafik: seçili metrik; ilan süresi, indirim payı, aylık stok, satış/liste metriklerinde eyalet ve ABD çizgisi de çizilir; stok ve satış sayılarında 2019 çizgisi; fiyatlarda zirve çizgisi.

## Çıktılar (exe yanındaki out\)
- ranking_XX.csv — bütün sütunlar + okuma metni
- series\<fips>_<county>.csv — ilçe başına aylık FRED + Redfin serileri (grafik için)
- cache_XX.json — önbellek, "Son sonucu yükle"
- log.txt — FRED istek günlüğü

## Sorun giderme
- Redfin eşleşen ilçe sayısı durum çubuğunda yazar; Redfin küçük ilçeleri yayınlamaz.
- Redfin URL değiştiyse RedfinLoader.cs başındaki adresleri https://www.redfin.com/news/data-center/ sayfasından güncelle.
- ScottPlot sürüm hatası: csproj'daki "5.0.*" yerine NuGet'teki son 5.0.x sürümünü yaz.
