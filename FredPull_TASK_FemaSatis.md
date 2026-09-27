# FredPull kısa görev: FEMA bağlantısı ve satılan evlerin kaydı

## 1. FEMA'nın resmî sunucusuna bağlanma sorununu teşhis et

Bağlam: "Ek verileri çek" FEMA'nın resmî servisine (`hazards.fema.gov`) her seferinde "The SSL connection could not be established" hatasıyla bağlanamıyor ve Esri Living Atlas'taki kopyaya düşüyor. Kopyanın kendi açıklamasına göre veri FEMA'nın 5 Ekim 2022 sürümünden türetilmiş. Sel haritaları o tarihten beri değişti (örneğin Lee County'nin yeni haritaları Kasım 2022'de yürürlüğe girdi); yani kopya bazı evler için eski bölgeyi gösterebilir.

* Aynı makinede bağlantıyı farklı yollarla dene ve sonuçları günlüğe yaz: PowerShell `Invoke-RestMethod`, Windows'un `curl.exe` aracı ve .NET `HttpClient` (TLS 1.2 ve TLS 1.3 açıkça seçilerek).
* İki resmî adresi de dene: `https://hazards.fema.gov/arcgis/rest/services/public/NFHL/MapServer?f=json` ve `https://hazards.fema.gov/gis/nfhl/rest/services/public/NFHL/MapServer?f=json`.
* Sertifika zincirini incele (eksik ara sertifika, süresi dolmuş kök, iptal kontrolü zaman aşımı gibi). Sorunu bulursan uygulama içinde güvenli bir biçimde çöz. Sertifika doğrulamasını kapatmak kabul edilmez.
* Resmî sunucu çalışırsa birincil kaynak o olur; Esri kopyası yalnızca yedek kalır.
* Hangi kaynak kullanılırsa kullanılsın, her evin kaydına ve `ev_detaylari_{ST}.md`'ye verinin sürümü yazılır: resmî servis için sorgu tarihi, kopya için "FEMA 5 Ekim 2022 sürümünden türetilmiş" ifadesi (katmanın kendi açıklamasından).

## 2. Satılan ya da satışı bekleyen evlerin ham bilgisi

Bağlam: Collier'daki seçili ev (883 Coconut Cir E) Redfin'de "Sold" görünüyor. Bir evin satılmış olması makale için önemli bir bilgi; satış fiyatı ve tarihi hikâyenin sonu olabilir.

* İlan durumu "Sold" ya da "Pending" olan evlerde, ilan sayfasındaki satış (ya da sözleşme) tarihi ve fiyatı ham olarak okunup kaydedilir; fiyat geçmişindeki son olay da (ör. "Sold (MLS)" ya da "Pending") yazılır.
* Yorum yapılmaz; sadece sayfadaki değerler aktarılır.

## Test

Florida'da "Ek verileri çek" çalıştırılır:

* FEMA teşhis sonucu günlükte yazılı; resmî servis çalışıyorsa Pasco ve Lee evlerinin bölgesi oradan gelir.
* Collier evinin satış tarihi ve fiyatı kaydedilmiş.
* Charlotte evinin ilan durumu ya da okunamama sebebi yazılı.
