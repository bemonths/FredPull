# FredPull görev eki — Ev detay formu ve materyal dışa aktarma

1) İlçe tablosunda satıra çift tık veya "Ev detayları" düğmesi → yeni form HouseDetailForm (modal değil, birden fazla açılabilir).
   Form düzeni:
   - Üst: ilçe adı, kart metni (RichTextBox, kopyalanabilir).
   - Sol: adaylar listesi (DataGridView: Seçildi ✓, Sokak, Şehir, Fiyat, İlk fiyat, Şimdiki, İndirim sayısı, Toplam indirim, Gün, Yapım, m², Oda, Son satış tarihi, Son satış fiyatı, Çekilmiş mi, Enlem, Boylam).
   - Sağ üst: seçili adayın fiyat geçmişi (tarih, olay, fiyat, fark) tablosu.
   - Sağ alt: fotoğraf alanı — FlowLayoutPanel içinde küçük resimler (PictureBox 160x120), tıklanınca aynı formda büyük önizleme.
   - Düğmeler: "Redfin'de aç" (varsayılan tarayıcı), "Bu evi seç" (Chosen değişir, CardText yeniden üretilir, listings dosyaları ve ranking csv kaydedilir, ana tablo güncellenir),
     "Fotoğrafları indir (referans)", "Klasörde göster" (seçili fotoğraf: explorer.exe /select,"yol"), "KML dışa aktar", "Animasyon CSV".

2) Enlem/boylam: gis yanıtındaki homes[] kaydında latLong.value.latitude / longitude alanları var; HouseCandidate'e Lat/Lng ekle, ReadHome'da oku. Eski kayıtlarda yoksa boş kalır.

3) Fotoğraf indirme: seçili adayın ilan sayfasını Playwright ile (mevcut RedfinListingPicker altyapısı, aynı profil ve engel yönetimi) açıp ssl.cdn-redfin.com/photo/... adreslerini toplar (img etiketleri + gömülü payload), en fazla 12 tanesini out\photos\{STATE}\{fips}_{sokak}\ altına kaydeder; form küçük resimleri oradan yükler.
   Klasöre REFERANS.txt yaz: "Emlakçı/MLS telifli; videoda kullanılmaz, yalnızca referans."
   Daha önce indirilmişse tekrar indirmez, doğrudan gösterir. İndirilen yollar listings_{STATE}.json'da aday başına PhotoPaths olarak saklanır.

4) "KML dışa aktar": seçilen evler için out\earthstudio_{STATE}.kml (Placemark: ad = "{İlçe} — {Şehir}", nokta = enlem/boylam). Koordinatı olmayan ev atlanır, log'a yazılır.

5) "Animasyon CSV": seçilen her ev için out\anim\{STATE}\{fips}.csv (date, price, cut) — ilk satır ilan tarihi ve ilk fiyat, sonraki satırlar her fiyat değişikliği; fiyat sayacı ve indirim etiketi animasyonları için.

6) Yeni kontroller HouseDetailForm.Designer.cs'e tasarımcı kuralıyla; veri alanları Models.cs'e; CLAUDE.md güncellenir. Kod stili mevcut dosyalarla aynı.

---
Uygulandı: 2026-09-24. Farklar ve eklemeler için CLAUDE.md → Ev detay formu ve Geçmiş.
