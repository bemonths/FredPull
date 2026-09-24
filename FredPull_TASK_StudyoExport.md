# FredPull — Görev: Harita Stüdyosu projesi dışa aktarma ve tek tıkla render

Depo: https://github.com/bemonths/FredPull (CLAUDE.md'deki kurallar geçerli: WinForms, kontroller Designer dosyasında, Türkçe arayüz).
Hedef araç: Harita Stüdyosu (https://github.com/bemonths/harita-studyosu). Proje dosyası şeması o deponun `docs/ENTEGRASYON.md` §5 bölümünde; bu görev şema sürüm 1'i ve yeni `county_focus` sahnesini kullanır. **Önce Harita Stüdyosu'ndaki "marka dosyası ve county_focus" görevi bitmiş olmalı.**

## Amaç

FredPull'daki county sinyallerini ve ev kartlarını tek düğmeyle bir Harita Stüdyosu projesine çevirmek ve isteğe bağlı olarak render'ı da FredPull içinden başlatmak. Kullanıcı hiçbir alanı elle doldurmaz.

## 1. Ayar: Harita Stüdyosu klasörü

- Üst çubuğa ya da bir ayar penceresine "Harita Stüdyosu klasörü" seçimi. Değer exe yanındaki ayar dosyasında saklansın.
- Geçerlilik kontrolü: klasörde `engine\cli.py` ve `.venv\Scripts\python.exe` olmalı; yoksa açık bir mesaj.

## 2. Video metin dosyası (isteğe bağlı ama önerilen)

Senaryodan gelen iki satır metin ve county sırası bu dosyadan okunur: `out\metinler_{STATE}.csv` (UTF-8, ilk satır başlık).

```
order,fips,county,focus_sub,focus_stat
1,12071,Lee,Cape Coral  ·  Fort Myers,9351 HOMES FOR SALE
...
```

- `order`: videodaki sıra. `fips` esas alınır; `county` sadece okunabilirlik için.
- Dosya yoksa: tabloda seçili satırlar ekrandaki sırayla alınır, `focus_sub` seçilen evin şehri, `focus_stat` boş bırakılır; kullanıcıya bu durum bildirilir.
- CSV'deki bir FIPS'in ev kartı yoksa o county için sadece `county_focus` üretilir, `price_ladder` atlanır ve uyarı listesine yazılır.

## 3. Dışa aktarma: proje JSON'u

Düğme: **"Stüdyo projesi oluştur"**. Mevcut eyaletin `cache_{STATE}.json` ve `listings_{STATE}.json` verisinden şu projeyi üretir:

```json
{
  "version": 1,
  "name": "FL_20260924",
  "transition": 0.6,
  "output": {"separate": true, "combined": true, "transparent": false},
  "scenes": [ ... ]
}
```

- `name`: `{STATE}_{yyyyMMdd}` (şema: `^[A-Za-z0-9_-]{1,60}$`).
- Dosya `<Harita Stüdyosu klasörü>\projects\{name}.json` olarak yazılır. Stüdyo kuralı: dosya adı `name` alanıyla birebir aynı olmalı. Aynı ad varsa hem `name` alanına hem dosya adına `_2`, `_3` eklenir (ör. `FL_20260924_2`).

**Sahne sırası:**

1. Bir `state_map` (giriş):
   - `state`: eyalet kodu.
   - `subtitle`: `"{N} COUNTIES  ·  {MONTH} {YYYY} DATA"`. N videodaki county sayısı; ay, FRED veri ayının İngilizce adı büyük harfle (ör. `AUGUST 2026`).
   - `assign`: eyaletin **bütün** county'leri için sinyal eşlemesi (aşağıdaki tablo). `none` olanları yazma.
   - `focus`: `null`. `categories` ve `accent` yazma; stüdyonun marka varsayılanları kullanılsın.
2. Metin dosyasındaki her county için sırayla iki sahne:
   - `county_focus`: `state`, `assign` (giriştekiyle aynı), `focus` = FIPS, `focus_sub` ve `focus_stat` metin dosyasından. `focus_name` boş (stüdyo "LEE COUNTY" gibi kendisi yazar).
   - `price_ladder` (seçilen ev varsa, `HouseCard.Chosen`):
     - `kicker`: `"{COUNTY NAME} COUNTY  ·  {CITY}"` büyük harf (ör. `LEE COUNTY  ·  FORT MYERS`). Louisiana gibi "Parish" kullanan eyaletler için county adını olduğu gibi al.
     - `title`: boş (stüdyo `ONE HOUSE. N PRICE CUTS.` yazar).
     - `subtitle`: `"{beds} bedrooms  ·  built {YearBuilt}  ·  listed {Month YYYY}"` (İngilizce ay adı). Daire modunda `"{beds}-bedroom condo  ·  built ..."`. Eksik alan varsa o parçayı atla.
     - `history`: `Chosen.PriceSteps` → `[{"date": "YYYY-MM-DD", "price": int}]`. Stüdyo kuralları: tarihler kesin artan, ardışık iki fiyat farklı. Aynı güne düşen adımlardan sonuncusunu tut; ardışık eşit fiyatları birleştir. En az 2 adım kalmıyorsa `price_ladder` atlanır ve uyarı yazılır.
     - `today`: dışa aktarma günü; son adım tarihinden önce olamaz.
     - `paid`, `paid_year`: `LastSalePrice` ve `LastSaleDate.Year`; yoksa `null`.

**Sinyal → kategori eşlemesi** (stüdyodaki `brand.json` anahtarları):

| FredPull `Signal` | Stüdyo `key` |
|---|---|
| Alıcı çekildi | `buyers` |
| Satıcı çekiliyor | `sellers` |
| Fiyat kırılıyor | `price` |
| Zayıflıyor | `weak` |
| Dengeli | `stable` |
| Sıcak | `hot` |
| Küçük taban / veri yok | (yazılmaz, `none` sayılır) |

**Doğrulama:** Dosyayı yazdıktan sonra stüdyonun kendi doğrulamasını çalıştır. `engine.project.validate(proje)` bir `(temiz_proje, hatalar)` ikilisi döndürür; yapısal bir sorunda `ProjectError` fırlatır. Çalışma klasörü stüdyo kökü, ortam değişkeni `PYTHONUTF8=1`:

```
<stüdyo>\.venv\Scripts\python.exe -c "import json,sys; from engine import project; p=json.load(open(sys.argv[1],encoding='utf-8'));\ntry:\n    _,e=project.validate(p); print(json.dumps(e,ensure_ascii=False))\nexcept project.ProjectError as x:\n    print(json.dumps([{'scene':None,'param':'project','message':str(x)}],ensure_ascii=False))" <proje yolu>
```

(Tek satırlık komut yerine küçük bir geçici .py dosyası yazıp çalıştırmak daha güvenli; mantık aynı.) Çıktı `[]` değilse hataları kullanıcıya listele: her hata `{"scene", "param", "message"}` biçiminde. `ProjectError` sınıfının adı ya da yeri farklıysa `docs/ENTEGRASYON.md` §5.4'e göre uyarla.

## 4. Tek tıkla render

Düğme: **"Stüdyoda render al"**. Önce 3. adımı çalıştırır, sonra stüdyonun komut satırını başlatır:

```
<stüdyo>\.venv\Scripts\python.exe -m engine.cli render projects\{name}.json --progress-json
```

- Çalışma klasörü stüdyo kökü, ortam değişkeni `PYTHONUTF8=1`.
- stdout satırlarını JSON olarak oku: `progress` olaylarıyla ilerleme çubuğunu güncelle (genel ilerleme `(scene + frame/total) / scenes`), `output` olaylarındaki yolları topla, `done` gelince çıktı klasörünü Explorer'da aç, `error` gelince mesajı göster.
- İptal düğmesi süreci sonlandırsın.

## 5. Kabul testi

- Florida, `out\metinler_FL.csv` (kullanıcı verecek) ile: proje 1 + 10 + 10 = 21 sahne içerir, stüdyo doğrulaması boş hata listesi döner.
- Lee County `price_ladder` sahnesinde `history` 15 Ağustos 2024, 580000 ile başlar, 410000 ile biter; `paid` 310000, `paid_year` 2017.
- "Stüdyoda render al" çalışır ve çıktı klasörü açılır.

## Bitince bildir

Değişen dosyalar, üretilen proje dosyasının yolu, stüdyo doğrulamasının çıktısı ve render süresi.

---
Uygulandı: 2026-09-24. Farklar, doğrulama ve render süresi için CLAUDE.md → Harita Stüdyosu ve Geçmiş.
