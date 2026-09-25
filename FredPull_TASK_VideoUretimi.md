# FredPull — Görev: "Video üretimi" sekmesi (animasyon + intro prompt'u)

Depo kuralları geçerli (CLAUDE.md): WinForms, kontroller Designer dosyasında, Türkçe arayüz, mevcut kod stili.

Bu görevle ana pencerede iki sekme olacak:
1. **İlçeler:** Şu anki ekranın kendisi (tablo, grafik, sağ panel). Hiçbir davranışı değişmez.
2. **Video üretimi:** Yeni sekme. İki ayrı bölüm: **Animasyon** ve **Intro prompt'u**.

Kapsam dışı (bu görevde yapma): kapak ve canlı ev promptları, senaryo metinleri.

---

## 1. Sekme yapısı

- Ana pencerenin gövdesini bir `TabControl` içine al. Birinci sekme "İlçeler": mevcut tablo ve sağ panel olduğu gibi taşınır. İkinci sekme "Video üretimi".
- Üst çubuk ortak kalır: eyalet seçimi, FRED anahtarı, veri çekme, Redfin, ev kartı düğmeleri. Sadece "Harita Stüdyosu" satırı (klasör kutusu, Seç…, Stüdyo projesi oluştur, Stüdyoda render al) üst çubuktan kaldırılıp yeni sekmeye taşınır.
- Durum çubuğu ve ilerleme çubuğu ortak kalır.

## 2. Bölüm: Animasyon (Harita Stüdyosu)

Bir `GroupBox` ("Animasyon — Harita Stüdyosu"). İçinde:

1. **Harita Stüdyosu klasörü:** Mevcut kutu ve "Seç…" düğmesi, aynı ayar dosyasıyla.
2. **Metin dosyası:** `out\metinler_{EYALET}.csv` için durum satırı:
   - Bulunduysa: "Metin dosyası: 10 county" ve altında salt okunur küçük bir tablo (sıra, county, şehirler, istatistik satırı).
   - Bulunmadıysa: "Metin dosyası yok — seçili satırlar ekran sırasıyla kullanılacak".
   - "Metin dosyası seç…" düğmesi: seçilen CSV'yi `out\metinler_{EYALET}.csv` adıyla kopyalar (üzerine yazmadan önce sorar) ve tabloyu yeniler.
   - Eyalet değişince durum ve tablo yeniden okunur.
3. **Düğmeler:** "Stüdyo projesi oluştur" ve "Stüdyoda render al" (mevcut davranış aynen), yanına "Çıktı klasörünü aç" (son render'ın klasörü; yoksa pasif).

Mevcut dışa aktarma ve render kodunun mantığı değişmez, sadece kontrollerin yeri değişir.

## 3. Bölüm: Intro prompt'u (Flow)

Bir `GroupBox` ("Intro prompt'u — Flow"). İçinde:

1. **İki düzenlenebilir kutu:**
   - "Komşular" (`TextBox`, tek satır).
   - "Pin şehri" (`TextBox`, tek satır).
   
   Eyalet seçilince/değişince bu iki kutu `PromptData\states_intro.json` dosyasındaki değerlerle dolar.
2. **Kullanıcı değişikliği:** Kutularda yapılan değişiklik eyalet bazında `intro_overrides.json` dosyasına (exe yanında) kaydedilir. O eyalet tekrar seçildiğinde tablodaki değer yerine kaydedilen değer gelir. "Varsayılana dön" düğmesi o eyaletin kaydını siler.
3. **Prompt önizlemesi:** Çok satırlı, salt okunur `TextBox` (kaydırma çubuğu açık, satır sonları korunur). İçerik şablondan üretilir ve kutular değiştikçe anında güncellenir.
4. **"Kopyala" düğmesi:** Önizlemedeki metnin tamamını panoya kopyalar (`Clipboard.SetText`, satır sonları `\r\n` olmadan da olur, olduğu gibi). Durum çubuğunda "Intro prompt'u kopyalandı (Florida)" yazar.
5. **"Şablonu aç" düğmesi:** `PromptData\intro_prompt_template.txt` dosyasını varsayılan metin düzenleyicide açar. Sekme her etkinleştiğinde ya da dosya değiştiğinde (FileSystemWatcher) şablon yeniden okunur ve önizleme yenilenir.

**Şablon doldurma kuralı** (düz metin değiştirme, büyük/küçük harf duyarlı):

| Şablondaki yer tutucu | Değer |
|---|---|
| `[STATE]` | Eyaletin adı, ör. `Florida` (`states_intro.json` → `name`) |
| `[STATE IN CAPITALS]` | Adın büyük harfli hali, ör. `FLORIDA` (`ToUpperInvariant`) |
| `[NEIGHBORS]` | "Komşular" kutusu |
| `[PIN CITY]` | "Pin şehri" kutusu |

Önce `[STATE IN CAPITALS]`, sonra `[STATE]` değiştirilsin (biri diğerinin parçası değil ama sıra yine de sabit olsun). Doldurma sonrası metinde `[` ile başlayan bir yer tutucu kalırsa önizlemenin üstünde kırmızı uyarı göster.

**Veri dosyaları:** Bu görevle gelen iki dosya projeye `PromptData` klasöründe eklenir ve derleme çıktısına kopyalanır (`CopyToOutputDirectory = PreserveNewest`):
- `PromptData\intro_prompt_template.txt`: intro prompt'unun şablon metni (UTF-8).
- `PromptData\states_intro.json`: 48 eyalet için `abbr`, `name`, `neighbors`, `pin_city`.

Dosya bulunamazsa bölümde açık bir hata mesajı göster; program çökmesin. `states_intro.json` içinde seçili eyalet yoksa (ör. Alaska, Hawaii, DC) kutular boş gelir ve uyarı gösterilir.

## 4. Kabul testi

- Florida seçiliyken "Video üretimi" sekmesinde:
  - Komşular: `Georgia, Alabama and the Gulf of Mexico`
  - Pin şehri: `Orlando`
  - Önizlemenin 5. satırı (boş satırlar dahil): `0:00 – 0:02`
  - Önizlemede şu cümle geçmeli: `drops onto land at the location of Orlando and casts a small shadow`
  - Önizlemede `The label must read exactly FLORIDA.` geçmeli; hiçbir `[` yer tutucusu kalmamalı.
- "Kopyala" sonrası panodaki metin önizlemeyle birebir aynı.
- Texas seçilince komşular `Oklahoma, New Mexico, Arkansas, Louisiana, Mexico and the Gulf of Mexico`, pin şehri `Austin`.
- Pin şehrini Florida için `Tampa` yapıp programı kapatıp açınca Florida'da `Tampa` gelmeli; "Varsayılana dön" ile `Orlando`ya dönmeli.
- "Stüdyoda render al" yeni sekmeden eskisi gibi çalışmalı.

## Bitince bildir

Değişen dosyalar, derlenen exe'nin tam yolu ve kabul testinin sonucu.

---
Uygulandı: 2026-09-25. Ayrıntı için CLAUDE.md → Video üretimi sekmesi ve Geçmiş.
