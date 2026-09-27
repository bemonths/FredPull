# FredPull düzeltme görevi: ek veri katmanının Florida denetimi

Florida çalıştırması (27 Eylül 2026, `out\ek_FL\`, `ev_detaylari_FL.md`) denetlendi. Doğru çalışanlar: ACS değerleri (county vergi değerleri bağımsız bir kaynakla birebir aynı), izinler (Pasco 2021 = 8.905, Osceola 2021 = 10.003), manifest, havalimanları, on evin koordinatı, FEMA sorgusu (Pasco AE). Aşağıdaki beş sorun düzeltilecek. İlke aynı: FredPull ham veri indirir, yorum yapmaz.

## 1. İlan ayrıntılarının çoğu okunamıyor (en önemlisi)

On seçili evin hiçbirinde şu alanlar okunmadı: yıllık emlak vergisi, Redfin'in gösterdiği FEMA bölgesi, sel sigortası tahmini, iklim riski puanları, ilanı veren emlakçı ve ofis. Yalnızca açıklama metni ve aidat okundu.

Bu bilgiler sayfada var. Kullanıcı Pasco evinin sayfasında (13639 Frances Ave, Hudson) şu metni gördü ve kopyaladı: "About FEMA Zone AE — FEMA designates Zone AE as a high-risk flood area … Insurance for 13639 Frances Ave ranges from $1737 to $8500 per year."

- `out\redfin_raw\FL\` altındaki ham kayıtları (yakalanan yanıtlar ve `page_text`) incele; bu alanların hangi yanıtta ya da sayfanın hangi metninde durduğunu bul.
- Yanıtlarda bulunamazsa sayfanın görünen metninden (`page_text`) oku: "FEMA Zone …", "Insurance for … ranges from $… to $… per year", emlak vergisi satırı, "Listed by …" gibi ifadeler.
- Test: Pasco evinde Redfin FEMA bölgesi "AE", sel sigortası tahmini 1.737–8.500 $ okunmalı.

## 2. İlan durumu kaydedilmiyor

Charlotte'taki seçili evin (1307 Tidy Ln, Punta Gorda) sayfasında hiçbir ayrıntı bulunamadı. Ev satılmış ya da ilandan kalkmış olabilir; bu, makaleyi doğrudan etkiler.

- Her seçili ev için ilan durumu ham olarak okunup kaydedilir (Redfin'in gösterdiği biçimiyle: Active, Pending, Sold, Off market vb.) ve okunduğu zaman yazılır.
- Sayfa açılmazsa ya da durum okunamazsa bunun sebebi yazılır (HTTP durumu, yönlendirme, engelleme).
- `ev_detaylari_{ST}.md` ve manifest'te her evin durumu görünür.

## 3. FEMA sonucu boş olduğunda anlamı belirsiz

Collier, Highlands, Lee, Miami-Dade ve Pasco evlerinde bölge döndü; Osceola, Sumter, St. Lucie ve Walton'da alan boş. Boşluk iki farklı anlama gelebilir: ya sorgu başarısız oldu, ya da ev kullanılan katmanda bir sel bölgesinin içinde değil.

- Bu iki durum ayrı yazılır: sorgu başarısız (hata mesajıyla) ya da "sorgu başarılı, bölge bulunamadı".
- Kullanılan Esri kopyasının ("USA Flood Hazard Reduced Set") hangi bölgeleri içerdiği, katmanın kendi açıklamasından okunup manifest'e yazılır (ör. yalnızca yüksek ve orta risk bölgeleri mi). Yorum yapılmaz, katmanın kendi ifadesi aktarılır.

## 4. Hastanelerin yüzde 14'ünün koordinatı yok

Florida'daki 221 hastanenin 32'sinde koordinat yok (toplamda 469'un 78'inde). Aralarında Walton'daki evin en yakın hastanesi olan "SACRED HEART HOSPITAL ON THE EMERALD COAST" (Miramar Beach), "TAMPA GENERAL HOSPITAL" ve "Adventhealth Zephyrhills" var. Bu haliyle en yakın hastane hesabı bazı evlerde yanlış hastaneyi gösterir.

- Eşleşmeyen adresler için ikinci deneme: adresi sadeleştirerek (bina, süit, kat bilgileri çıkarılarak) Census Geocoder'a yeniden gönder.
- Yine bulunamazsa, evlerde zaten kullanılan OpenStreetMap servisine hastane adı + şehir + eyalet ile sor. Servisin kullanım kurallarına uy: saniyede en fazla bir istek ve tanımlayıcı bir User-Agent.
- Koordinatın kaynağı her satıra yazılır (`census`, `census_retry`, `osm`).
- Test: yukarıdaki üç hastane koordinatlı olmalı; Florida'da koordinatsız hastane sayısı günlüğe yazılır.

## 5. Havalimanları yalnızca eyaletle sınırlı

Görevde ABD'nin tamamı istenmişti; çıktı yalnızca Florida'yı içeriyor. Sınır county'lerinde en yakın havalimanı komşu eyalette olabilir (ör. Washington–Oregon, Kentucky–Ohio).

- Süzgeçten eyalet koşulu kaldırılır; ABD'deki bütün tarifeli büyük ve orta havalimanları yazılır (birkaç yüz satır).

## Test (Florida)

Pasco evinde Redfin FEMA "AE" ve sel sigortası 1.737–8.500 $; Charlotte evinin ilan durumu yazılı; FEMA'da "bölge bulunamadı" ile "sorgu başarısız" ayrı; üç hastane koordinatlı; `havalimanlari.csv` ABD'nin tamamını içeriyor. `CLAUDE.md` güncellenir.
