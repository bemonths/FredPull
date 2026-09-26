# FredPull görevi: grafik listesi (`grafikler_XX.csv`) ve grafik sahnelerinin otomatik doldurulması

Önkoşul: Harita Stüdyosu'nda sekiz yeni sahne tipi eklenmiş olmalı (görev: `STUDYO_TASK_grafik_sahneleri.md`; şema: stüdyonun `docs/ENTEGRASYON.md` §5, v1.3).

Önce şunları oku: `CLAUDE.md`, `StudioExport.cs` (özellikle `ReadTexts` ve `Build`), `Models.cs` (`Snapshot`, `CountyResult`, `SeriesSet`), `MainForm.cs`'teki "Video üretimi" sekmesi. Kod stili ve düzen mevcut dosyalarla aynı olmalı; yeni kontroller tasarımcı kuralıyla `MainForm.Designer.cs`'e.

## Amaç

Makale projesi her video için hangi grafiğin nerede kullanılacağını `out\grafikler_{ST}.csv` dosyasıyla verir. FredPull bu dosyayı okur, her grafiğin verisini bellekteki `Snapshot`'tan (FRED ve Redfin serileri) hesaplar ve Stüdyo projesine sahneleri ekler. Rakamlar elle girilmez.

## 1. Dosya biçimi: `out\grafikler_{ST}.csv`

UTF-8, ilk satır başlık, virgül içeren alanlar çift tırnaklı. Sütunlar:

| Sütun | Anlamı |
|---|---|
| `seq` | Sıra numarası (1'den başlar). Aynı `seq`'e sahip `question_card` satırları tek bir vaat ekranında birleşir. |
| `slot` | `intro` / `county` / `mid` / `closing` |
| `fips` | 5 haneli county kodu (`county` slotunda zorunlu; `question_card`'da `county` simgesi için) |
| `chart` | Tarif adı (Bölüm 2) |
| `metrics` | `county_quiz` için en fazla üç ölçü, noktalı virgülle (Bölüm 2); `question_card` için simge (`house` / `county` / `houses10` / `none`); diğerlerinde boş |
| `text` | `question_card` için kart değeri (ör. `$580K -> ?`); diğerlerinde isteğe bağlı: sağ üst büyük yazının elle değiştirilmiş hali |
| `caption` | `question_card` için kart açıklaması (iki satır, ` / ` ile ayrılır); diğerlerinde isteğe bağlı |

Örnek:
```
seq,slot,fips,chart,metrics,text,caption
1,intro,12071,question_card,house,$580K -> ?,ONE HOUSE IN / FORT MYERS
1,intro,12015,question_card,county,? HOMES,FEWER HOMES FOR SALE / IN ONE YEAR
1,intro,12101,question_card,houses10,? IN 10,SELLERS CUT / THEIR PRICE
1,intro,12055,question_card,county,#1 ?,AND IT'S NOT / CAPE CORAL
2,county,12071,county_quiz,homes_for_sale;cut_share_in10;sale_price_vs_peak,,
3,county,12071,price_years,,,
4,county,12015,stock_change_grid,,,
5,county,12101,cut_share_compare,,,
6,county,12097,stock_years_line,,,
7,county,12021,sale_to_list_ring,,,
8,county,12055,months_supply_thermo,,,
9,county,12105,stock_years_bars,,,
10,closing,,months_supply_rank,,,
```

Okuma kuralları: bilinmeyen `chart` ya da `metrics` değeri, başka eyalete ait `fips`, `county` slotunda boş `fips` → satır atlanır ve uyarı yazılır (mevcut `ReadTexts` uyarı düzeni).

## 2. Tarifler ve tanımlar

Tanımlar sabittir; makale projesi anlatıcının söyleyeceği rakamı aynı tanımla hesaplar. Bu tablo `CLAUDE.md`'ye de yazılır.

- **FRED ayı** = county'nin `Month` değeri (ilan verisi). **Redfin ayı** = `RedfinMonth` (satış verisi).
- **Aynı ay kuralı:** Yıllara göre serilerde her yılın aynı takvim ayı kullanılır (FRED serilerinde FRED ayı, Redfin serilerinde Redfin ayı). Mevsim etkisi böyle önlenir.

| `chart` | Stüdyo sahnesi | Veri ve tanım | Yazılar |
|---|---|---|---|
| `question_card` | `question_board` | Veri hesaplanmaz; `text` ve `caption` olduğu gibi aktarılır. Aynı `seq`'teki 2–4 satır tek sahne. `heading` = eyalet adı, `subtitle` = "`N` COUNTIES  ·  `K` QUESTIONS". | |
| `county_quiz` | `county_quiz` | `metrics` sırasıyla satırlar (en fazla 3). Ölçüler aşağıda. Açılış anları varsayılan (3,4 / 5,0 / 6,6 sn). | `heading` = county adı büyük harf; `subtitle` = `metinler_{ST}.csv`'deki `focus_sub` (yoksa boş). |
| `price_years` | `house_bars` | Redfin `median_sale_price`, 2019'dan son yıla her yılın Redfin ayı değeri. En yüksek değerli yıl `highlight`. Ok: zirve yılından son yıla. | `title` "WHAT A TYPICAL HOME SOLD FOR", `subtitle` "MEDIAN SALE PRICE  ·  `AY` OF EACH YEAR", `callout_value` = son − zirve ("−$59K"), `callout_label` "SINCE `AY` `ZİRVE YILI`", `source` "SOURCE: REDFIN" |
| `stock_years_line` | `line_trend` | FRED `ACTLISCOU`, 2016'dan (serinin ilk tam yılı) son yıla her yılın FRED ayı değeri. `ref_value` = 2019 değeri. `mark_min` = yes. | `title` "HOMES FOR SALE", `subtitle` "EVERY `AY`  ·  `focus_sub`", `ref_label` "2019 LEVEL: `değer`", `callout_value` oran ≥ 1,5 ise "`oran`×" (bir ondalık, ör. "2×", "1.6×") ve `callout_label` "THE 2019 LEVEL"; değilse "+`fark`" ve "MORE THAN `AY` 2019" (azalışta "−" ve "FEWER THAN"), `source` "SOURCE: REALTOR.COM VIA FRED" |
| `stock_years_bars` | `house_bars` | Aynı seri, 2019'dan son yıla. En düşük değerli yıl `highlight` (`highlight_color` = `neutral`, `after_color` = `accent`): dipten sonraki yıllar turuncu. Ok: en düşük yıldan son yıla (en düşük yıl son yılsa ok yok ve hepsi `neutral`). | `title` "HOMES FOR SALE", `callout_value` = son − 2019 ("+1,840"), `callout_label` "MORE THAN `AY` 2019" |
| `cut_share_compare` | `bar_list` | Fiyat kıran pay = `PRIREDCOU` / `ACTLISCOU` × 100, FRED ayı; county, eyalet (`StateData`) ve ABD (`UsData`). `max_value` 100, `value_suffix` " OF 100", county satırı `highlight`. | `title` "SELLERS WHO CUT THEIR PRICE", `subtitle` "OUT OF EVERY 100 HOMES FOR SALE  ·  `AY YIL`" |
| `sale_to_list_ring` | `ring` | Redfin `avg_sale_to_list`, Redfin ayı (county'nin `SaleToList` değeri). | `title` "WHAT BUYERS REALLY PAY", `subtitle` "`focus_sub`  ·  `AY YIL` SALES", `center_prefix` "$", `center_label` "OF EVERY $100 ASKED", `remainder_label` "AT THE TABLE", `source` "SOURCE: REDFIN" |
| `months_supply_thermo` | `thermometer` | County `MonthsSupply` ve eyalet `MonthsSupplyState`, Redfin ayı. | `title` "HOW LONG TO SELL EVERY HOME FOR SALE", `subtitle` "IF NO NEW HOME WERE LISTED  ·  `AY YIL`", tüp etiketleri county adı (kısa, "County" olmadan) ve eyalet adı |
| `months_supply_rank` | `bar_list` | `metinler_{ST}.csv`'deki video county'lerinin `MonthsSupply` değerleri, büyükten küçüğe. `max_value` 8 (en büyük değer 8'i aşıyorsa yukarı yuvarlanır), `threshold` 6, `decimals` 1. | `heading` "`EYALET`  ·  `N` COUNTIES", `title` "HOW MANY MONTHS TO SELL EVERY HOME", `threshold_label` "BUYER'S MARKET: 6+ MONTHS" |
| `stock_change_grid` | `house_grid` | FRED `ACTLISCOU`: geçen yılın FRED ayı (`before`) ve bu yılın FRED ayı (`after`). `unit` boş (Stüdyo seçer). | `title` "HOMES FOR SALE", `before_label` / `after_label` "`AY YIL`" |

**`county_quiz` ölçüleri:**

| Ölçü | Satır tipi | Tanım | Yazılar |
|---|---|---|---|
| `homes_for_sale` | `counter` | `Active` (FRED ayı) | "HOMES FOR SALE", not "`AY YIL`" |
| `cut_share_in10` | `in10` | Fiyat kıran pay / 10, en yakın tam sayı | "SELLERS WHO CUT THEIR PRICE" |
| `months_supply` | `counter` (bir ondalık) | `MonthsSupply` (Redfin ayı) | "MONTHS TO SELL EVERY HOME" |
| `sale_to_list` | `counter` | `SaleToList` yuvarlanmış, başında "$" | "BUYERS PAY PER $100 ASKED" |
| `sale_price_vs_peak` | `compare` | `price_years` ile aynı tanım: zirve yılının Redfin ayı değeri ile son değer | "WHAT BUYERS PAY", `value2_label` "`ZİRVE YILI` PEAK", `value_label` "TODAY", not "`AY YIL` SALES" |
| `stock_vs_2019` | `compare` | `stock_years_*` ile aynı tanım: 2019 FRED ayı değeri ile son değer | "HOMES FOR SALE", `value2_label` "2019", `value_label` "NOW" |

**Ortak alanlar:** County sahnelerinde `heading` = county adı büyük harf ("LEE COUNTY"), `fips` = county, `backdrop` = `county`. `intro`, `mid` ve `closing` sahnelerinde `backdrop` = `state`. `text` ve `caption` doluysa (question_card dışında) `callout_value` ve `callout_label` yerine onlar kullanılır. Ay adları İngilizce büyük harf ("MAY", "AUGUST").

## 3. Veri kontrolü (sıçrama uyarısı)

Seriye dayanan her tarifte, grafik kurulmadan önce kontrol edilir:
- Kullanılan yıllık değerlerde ardışık iki değer arasında 2,5 katından büyük ya da 0,4 katından küçük bir değişim varsa;
- ya da kullanılan Redfin serisinde son 24 ayda bir aydan diğerine %60'tan büyük bir değişim varsa (Broward'daki veri kaynağı değişikliği bu türdendi):
sahne yine eklenir ama uyarı listesine "`County`: `seri` serisinde olağan dışı sıçrama (`tarih`, `önceki` → `sonraki`), grafiği kontrol edin" yazılır. Eksik yıl varsa o yıl atlanır ve uyarı yazılır; üçten az nokta kalırsa sahne eklenmez.

## 4. Projeye yerleştirme sırası (`StudioExport.Build`)

1. `state_map`
2. `intro` slotundaki sahneler (`seq` sırasıyla)
3. Her county için (metin dosyasının sırasıyla): `county_focus` → o county'nin `county_quiz` sahnesi (varsa) → o county'nin diğer grafikleri (`seq` sırasıyla) → `price_ladder`
4. `mid` slotundaki sahneler, metin dosyasındaki 5. county'den sonra
5. `closing` slotundaki sahneler en sonda

`grafikler_{ST}.csv` yoksa proje bugünkü gibi kurulur (geriye uyumlu). Tahmini video süresi hesabına yeni sahnelerin temel süreleri eklenir.

## 5. Değer dökümü: `out\grafik_degerleri_{ST}.csv`

Proje kurulurken ekrana basılacak her rakam bu dosyaya yazılır: `seq, chart, fips, county, alan, değer, tanım` (ör. `3, price_years, 12071, Lee, 2022, 419000, Redfin median_sale_price MAY`). Amaç: makaledeki rakamların ekrandakilerle aynı olduğunu kontrol etmek.

## 6. Arayüz ("Video üretimi" sekmesi)

- Animasyon bölümüne **grafik listesi durumu**: `grafikler_{ST}.csv` bulundu mu, kaç satır, kaç uyarı. "Grafik dosyası seç…" (metin dosyası seçicisinin aynısı).
- **Grafik ekle** alanı (elle üretim için): county açılır listesi (metin dosyasındaki county'ler + "Eyalet geneli"), tarif açılır listesi (Bölüm 2), `county_quiz` seçilirse üç ölçü açılır listesi, `question_card` seçilirse simge, değer ve açıklama kutuları; "Ekle" düğmesi satırı `grafikler_{ST}.csv`'ye yazar (dosya yoksa başlıkla oluşturur, `seq` sonuncunun bir fazlası). Altında dosyadaki satırların listesi ve "Seçili satırı sil".
- **"Şeffaf arka plan (MOV)" onay kutusu:** işaretliyse projede `output.transparent = true` (şu an sabit `false`). Varsayılan kapalı; seçim ayarlarda saklanır.
- Uyarılar mevcut uyarı alanında gösterilir.

## 7. Testler ve kontrol

- Florida verisiyle (`out\` klasöründeki dosyalar) yukarıdaki örnek `grafikler_FL.csv` ile proje kurulur; Stüdyo doğrulaması hatasız geçer; render alınır.
- Hesaplanan değerler şu beklenenlerle karşılaştırılır (Florida, Ağustos/Mayıs 2026): Lee `price_years` Mayıs değerleri 2019–2026: 242.825, 243.450, 319.000, 419.000, 411.000, 399.000, 367.745, 360.000 (zirve 2022, fark −59K); Osceola Ağustos `ACTLISCOU` 2016–2026: 2.445, 2.380, 1.991, 2.075, 2.278, 816, 1.992, 2.316, 3.801, 4.282, 4.135; Polk Ağustos 2019–2026: 2.931, 2.078, 1.082, 2.564, 2.769, 4.543, 4.912, 4.771; Charlotte Ağustos 2025 → 2026: 3.625 → 2.705; fiyat kıran pay Pasco 43,6, Florida 30,5, ABD 35,3; Collier satış/liste 94,3; aylık stok Highlands 6,2, Florida 4,7.
- `CLAUDE.md`'ye: dosya biçimi, tarif tablosu, tanımlar, yerleştirme sırası.

---
Uygulandı: 2026-09-26 (Claude Code). Ayrıntı ve sapmalar: CLAUDE.md → Grafik listesi.
