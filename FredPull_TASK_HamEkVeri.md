# FredPull görevi: ham ek veri katmanı

## Amaç ve ilke

FredPull'a, seçili eyalet için ek ham verileri indiren bir katman eklenir. **İlke: FredPull düşünmez, yorumlamaz, hesap yapmaz.** Sadece resmî kaynaklardan veriyi indirir ve olduğu gibi, kaynağı ve tarihiyle kaydeder. Karşılaştırma, sıralama, uzaklık hesabı, eşleştirme ve yorum yapay zeka projelerinde yapılır; bu yüzden çıktılar sade, düz ve kolay okunur tablolar olmalıdır.

Önce şunları oku: `CLAUDE.md`, `MainForm.cs` (mevcut "Verileri çek" akışı, uyarı alanı, ayarlar), `FredClient.cs`, `RedfinListingPicker.cs` (ilan sayfası ziyareti ve `stingray` yanıtlarının yakalanması), `Models.cs`. Kod düzeni mevcut dosyalarla aynı olmalı.

## 1. Arayüz

- Ana ekrana **"Ek verileri çek"** düğmesi. Seçili eyalet için aşağıdaki 2–6. maddeleri sırayla çalıştırır. Her adımın durumu ve satır sayısı günlük alanına yazılır.
- Bir adım başarısız olursa diğerleri yine çalışır; hata uyarı listesine yazılır.
- Ayarlara isteğe bağlı **Census API anahtarı** alanı (boşsa anahtarsız istek atılır).
- Mevcut **"Ev kartlarını topla"** akışına 7. ve 8. maddeler eklenir (yeni düğme gerekmez).

## 2. Census ACS (county düzeyi)

- Kaynak: Census Data API, 5 yıllık ACS. Önce en yeni yıl denenir (`/data/2024/acs/acs5`); yoksa bir önceki yıla düşülür. Kullanılan yıl dosyaya yazılır.
- Değişkenler:
  - `B25103_001E` (ödenen medyan emlak vergisi)
  - `B25077_001E` (medyan ev değeri)
  - `B01001_001E` (toplam nüfus)
  - 65 yaş ve üstü gruplar: erkek `B01001_020E`–`B01001_025E`, kadın `B01001_044E`–`B01001_049E`
- Coğrafya: eyaletin bütün county'leri (`for=county:*&in=state:{FIPS}`), ayrıca eyaletin kendisi ve ABD (ayrı satırlar).
- Çıktı: `out\ek_{ST}\acs_{ST}.csv`. Sütunlar: `geo_type, fips, name`, değişkenlerin ham değerleri (Census'un özel kodları, örneğin negatif "veri yok" değerleri olduğu gibi), `acs_year`.
- Toplama ya da oran hesabı yapılmaz; ham sayılar yazılır.

## 3. İnşaat izinleri (FRED)

- Seri adı: `BPPRIV0` + 5 haneli county FIPS (ör. Osceola `BPPRIV012097`, Pasco `BPPRIV012101`). Yıllık seri.
- Eyaletin bütün county'leri için indirilir; serisi olmayan county atlanır ve günlüğe yazılır.
- Çıktı: `out\ek_{ST}\izinler_{ST}.csv`. Sütunlar: `fips, name, year, units`.

## 4. Hastaneler (CMS) ve koordinatları (Census Geocoder)

- Kaynak: CMS Provider Data API, "Hospital General Information" veri kümesi (kimlik `xubh-q36u`). Uç nokta: `https://data.cms.gov/provider-data/api/1/datastore/query/xubh-q36u/0`, eyalete göre süzme (`conditions`), sayfalama (`limit`, `offset`). Sorgu biçimini `https://data.cms.gov/provider-data/docs` belgesinden doğrula.
- **Sabit bir CSV dosya bağlantısı kullanılmaz.** CMS dosya adını her sürümde değiştiriyor; eski bağlantı 404 veriyor.
- Seçili eyaletin yanında komşu eyaletler de çekilir; sınıra yakın county'lerde en yakın hastane komşu eyalette olabilir. Komşu listesi FredPull'daki `PromptData/states_intro.json` dosyasında zaten var.
- Adresler Census Geocoder'ın toplu adres servisiyle koordinata çevrilir (ücretsiz). Uç noktayı ve bir seferde gönderilebilecek satır sınırını Census'un geocoder belgesinden doğrula. Eşleşmeyen adresler boş koordinatla ve eşleşme durumuyla yazılır.
- Çıktı: `out\ek_{ST}\hastaneler_{ST}.csv`. Sütunlar: CMS'in bütün sütunları olduğu gibi, ardından `lat, lng, geocode_match`.
- CMS verisinin çekildiği tarih ve veri kümesinin kendi güncelleme tarihi (API döndürüyorsa) manifest'e yazılır. Listede kapanmış hastaneler bir süre kalabilir; FredPull bunu düzeltmeye çalışmaz, güncellik kontrolünü yapay zeka projesi yapar.

## 5. Havalimanları

- Kaynak: OurAirports açık verisi, `https://raw.githubusercontent.com/davidmegginson/ourairports-data/main/airports.csv`.
- ABD'deki `type` = `large_airport` ya da `medium_airport` ve `scheduled_service` = `yes` olan satırlar alınır.
- Çıktı: `out\ek_{ST}\havalimanlari.csv`. Sütunlar: `ident, iata_code, name, municipality, iso_region, type, latitude_deg, longitude_deg`. (Florida'da bu süzgeçle 24 satır çıkıyor; test için.)

## 6. Manifest

`out\ek_{ST}\manifest.json`. Her dosya için kaynak adresi, çekiliş zamanı, veri yılı ya da dönemi, satır sayısı ve uyarılar yazılır. Yapay zeka projesi hangi verinin ne kadar güncel olduğunu buradan okur.

## 7. Ev kartlarına: ilan sayfasındaki ham ayrıntılar

`RedfinListingPicker` her aday evin ilan sayfasını zaten açıyor ve `stingray` yanıtlarını yakalıyor. Aynı ziyarette, bulunabilirse şu bilgiler de okunur ve **ham metin ya da ham sayı olarak** evin kaydına (`listings_{ST}.json`) eklenir:
- İlanın açıklama metni ("About this home").
- Yıllık emlak vergisi ve hangi yıla ait olduğu (ilan sayfasındaki kamu kayıtları).
- Aidat (HOA) tutarı ve dönemi.
- Redfin'in gösterdiği FEMA sel bölgesi ve sel sigortası tahmini aralığı (sayfada yazdığı gibi, "Redfin tahmini" olarak işaretlenir).
- Redfin'in iklim riski puanları (sel, yangın, sıcaklık, rüzgâr; sayfada varsa).
- İlanı veren emlakçı ve ofis adı.
- **Koordinat (`Lat`, `Lng`) her aday ev için doldurulur.** Şu an "Bu evi seç" ile seçilen evlerde koordinat boş kalıyor (Florida'da Collier, Highlands, Pasco, Walton); bu hata düzeltilir.

Bulunamayan alan tahmin edilmez, boş bırakılır. Önce birkaç gerçek ilan sayfasının yakalanan yanıtlarını inceleyerek alanların nerede durduğunu bul. Redfin sayfa yapısı değişirse bu kısım hata vermeden boş dönmelidir.

## 8. Ev kartlarına: FEMA resmî sel bölgesi

- Kaynak: FEMA National Flood Hazard Layer, ArcGIS REST servisi `https://hazards.fema.gov/arcgis/rest/services/public/NFHL/MapServer`.
- "Flood Hazard Zones" katmanının numarası sabit yazılmaz; servisin katman listesinden (`?f=json`) adıyla bulunur.
- Her aday evin koordinatıyla nokta sorgusu yapılır (`geometryType=esriGeometryPoint`, `inSR=4326`, `spatialRel=esriSpatialRelIntersects`, `outFields=FLD_ZONE,ZONE_SUBTY,SFHA_TF`, `returnGeometry=false`, `f=json`).
- Sonuç evin kaydına `FemaZone`, `FemaZoneSubtype`, `FemaSfha` olarak eklenir; sorgu zamanı da yazılır. Sonuç boşsa ya da servis yanıt vermezse alan boş kalır ve uyarı yazılır.

## 9. Okunabilir ev detayı dosyası

`out\ev_detaylari_{ST}.md`. Seçili her ev için bir bölüm: county, şehir, fiyat geçmişi (mevcut kart bilgisi), 7. ve 8. maddelerin ham bilgileri, ilan adresi. Yorum cümlesi yazılmaz; sadece alan adı ve değer.

## 10. Test (Florida)

- `acs_FL.csv`: 67 county satırı, bir eyalet satırı, bir ABD satırı; `acs_year` yazılı.
- `izinler_FL.csv`: Pasco 2021 = 8.905, Osceola 2021 = 10.003 (Florida makalesinin araştırmasında bulunan değerler).
- `hastaneler_FL.csv`: Florida ve komşu eyaletlerin (Georgia, Alabama) hastaneleri; koordinat eşleşme oranı günlüğe yazılır.
- `havalimanlari.csv`: süzgeçle Florida'da 24 satır.
- Pasco'daki seçili ev (13639 Frances Ave, Hudson): FEMA sel bölgesi `AE` çıkmalı (ilan sayfasında da AE yazıyor). Aynı evde Redfin'in sel sigortası tahmini 1.737–8.500 $ olarak okunmalı.
- Florida'nın on seçili evinin hepsinde koordinat dolu olmalı.
- `CLAUDE.md`'ye yeni dosyaların biçimi ve ilke ("FredPull ham veri indirir, hesap ve yorum yapmaz") eklenir.
