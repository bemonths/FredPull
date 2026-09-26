# Düzeltme görevi: yuvarlama ve mevsimsel sıçrama uyarısı

Grafik işinin denetiminde (26 Eylül) bulunan iki küçük sorun. İkisi de iki depoya dokunuyor; önce Stüdyo, sonra FredPull.

## 1. Yuvarlama hatası (doğruluk)

**Sorun:** Florida'nın fiyat kıran payı 30,54. FredPull bunu tek ondalığa yuvarlayıp 30,5 olarak gönderiyor; Stüdyo'nun `bar_list` sahnesi `round(30.5, 0)` ile yuvarlıyor. Python'un `round` fonksiyonu yarımları en yakın çift sayıya yuvarladığı için ekranda "30 OF 100" çıkıyor; doğrusu 31. C#'ta `Math.Round` da varsayılan olarak aynı davranır (ör. 2,5 → 2).

**Harita Stüdyosu:**
- Ekrana yazılan bütün sayılarda yarımlar yukarı yuvarlanmalı (0,5 → 1; 30,5 → 31). `engine` altında ortak bir yardımcı (ör. `round_half_up(v, dec)`, `decimal.Decimal` ve `ROUND_HALF_UP` ile) yazılır ve sahnelerdeki bütün `round(...)` çağrıları bununla değiştirilir (`bar_list`, `county_quiz`, `ring`, `thermometer`, `house_bars`, `line_trend`, `house_grid`, `question_board` ve varsa `price_ladder`).
- Test: `bar_list`'te 30,5 değeri 0 ondalıkla "31" yazmalı; 2,5 değeri "3" yazmalı.

**FredPull:**
- Stüdyo'ya giden ham değerler tek ondalığa değil en az iki ondalığa yuvarlanır (ya da hiç yuvarlanmaz). `ChartList.cs`'te değer üreten yerler kontrol edilir.
- C#'ta ekrana giden bir sayı yuvarlanıyorsa (ör. `cut_share_in10`: pay / 10) `Math.Round(x, MidpointRounding.AwayFromZero)` kullanılır.
- `grafik_degerleri_XX.csv` dökümündeki tanım metinleri tek dilde olmalı: `months_supply_rank` satırlarında "Mayıs 2026" yazıyor, diğerlerinde "MAY 2026". Hepsi İngilizce büyük harf ay adıyla.

## 2. Sıçrama uyarısının yanlış alarmları

**Sorun:** Florida verisiyle proje kurulurken üç uyarı çıktı. İkisi yanlış alarm: Pasco ve Polk için `months_of_supply` serisinde ocak ayında ani artış bildiriliyor. Bu mevsimseldir; ocakta satış azaldığı için aylık stok her yıl sıçrar, veri hatası değildir. Üçüncüsü (Osceola, 2020 → 2021, 2.278 → 816) gerçek bir düşüştür (pandemi), uyarının kontrol amaçlı çıkması kabul edilebilir.

**FredPull:**
- Aylık sıçrama kontrolü (son 24 ayda bir aydan diğerine %60'tan büyük değişim) yalnızca sayı serilerinde yapılır: `homes_sold`, `inventory`, `new_listings`, `pending_sales` ve `median_sale_price`.
- Oran serilerinde (`months_of_supply`, `avg_sale_to_list`) aylık kontrol yapılmaz. Bunların yerine aynı ayın geçen yılla kıyası yapılır: 2,5 katından büyük ya da 0,4 katından küçük değişimde uyarı.
- Beklenen sonuç: Florida'da bu grafik listesiyle yalnızca Osceola uyarısı kalır.

## 3. Stüdyo örnek projesi (küçük)

`projects/ornek_grafikler.json`'daki ikinci vaat kartının açıklaması "PULLED OFF / THE MARKET" yerine "FEWER HOMES FOR SALE / IN ONE YEAR" olmalı. Kanalın ölçüm kuralına göre satılık ev sayısındaki azalma ekrana "ilandan çekildi" diye yazılmaz. Referans kareleri gerekiyorsa güncellenir.

## Kontrol

Florida verisiyle proje yeniden kurulduğunda Pasco grafiğinde "44 OF 100", "31 OF 100", "35 OF 100" yazmalı ve uyarı listesinde yalnızca Osceola kalmalı.

---
Uygulandı: 2026-09-26, FredPull maddeleri (bf9a9b6) ve stüdyo maddeleri (stüdyo 938a5fb), Claude Code. Ayrıntı: CLAUDE.md → Grafik listesi.
