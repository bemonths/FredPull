# FredPull kısa görev: yalnızca FEMA istekleri için proxy

Bağlam: FEMA'nın resmî servisi (`hazards.fema.gov`) kullanıcının ağından (Türk Telekom) gelen TLS bağlantısını kesiyor; teşhis önceki görevde yapıldı. Kullanıcı ABD'den çıkış veren bir proxy ayarlayacak. Proxy yalnızca FEMA isteklerinde kullanılacak. Census, CMS, OpenStreetMap ve Redfin (Playwright) doğrudan bağlantıyla çalışmaya devam eder.

## 1. Ayarlar

* Ayarlara "FEMA proxy" bölümü: adres (`http://host:port` ya da `socks5://host:port`), isteğe bağlı kullanıcı adı ve parola.
* Değerler yalnızca yerel ayar dosyasında (`fredpull_settings.json`, exe'nin yanında) saklanır; depoya girmez. Parola günlüğe ve manifest'e yazılmaz; günlükte yalnızca proxy'nin şeması ve sunucu adı görünür.
* .NET 8 `HttpClient` SOCKS5 proxy'yi `WebProxy` ile destekliyor; iki tür de desteklenir.

## 2. "Proxy'yi dene" düğmesi

* FEMA servisinin katman listesini (`...NFHL/MapServer?f=json`) proxy üzerinden ister.
* Sonucu gösterir: başarılı mı, HTTP durumu, yanıt süresi; başarısızsa hata mesajı.
* Başarılıysa ayrıca `https://api.ipify.org` üzerinden proxy'nin çıkış IP'sini gösterir (yalnızca bilgi için).

## 3. Kaynak sırası (FEMA sorgusu)

1. Proxy tanımlıysa: resmî servis, proxy üzerinden.
2. Proxy yoksa ya da başarısızsa: resmî servis, doğrudan.
3. İkisi de olmazsa: Esri kopyası (mevcut davranış).

Hangi yolun kullanıldığı her evin kaydına, `ev_detaylari_{ST}.md`'ye ve manifest'e yazılır. Resmî servis çalıştığında, daha önce Esri kopyasından gelmiş sonuçlar resmî servisten yeniden sorgulanır (bu zaten mevcut davranış; proxy yolu için de geçerli olmalı).

## Test

* "Proxy'yi dene" başarılı sonuç verir.
* Florida'da "Ek verileri çek" sonrası on evin FEMA sonucu resmî servisten gelir; `ev_detaylari_FL.md`'de "Verinin sürümü" satırı resmî servisi ve sorgu tarihini gösterir.
* Lee evinin (16525 Wellington Lakes Cir, Fort Myers) resmî servisten gelen bölgesi, Esri kopyasının verdiği bölgeyle karşılaştırılıp günlüğe yazılır (farklıysa not düşülür).
* `CLAUDE.md` güncellenir.
