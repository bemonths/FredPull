# FredPull ek görevi: seçili evlerin ayrıntılarını seçime dokunmadan okumak

Bağlam: Ham ek veri katmanı (commit 2bdff9f) ilan ayrıntılarını (`RedfinDetailsReader`: açıklama, vergi, aidat, sel bilgisi, iklim riski, emlakçı) yalnızca "Ev kartlarını topla" sırasında, aday toplanırken okuyor. Bu düğme adayları baştan topladığı için kullanıcının "Bu evi seç" ile elle yaptığı seçimleri değiştirebilir. Florida'da on evin beşi elle seçildi.

## 1. "Ek verileri çek" seçili evlerin ayrıntılarını da okusun

* Yüklü ev kartlarında her county'nin yalnızca seçili evinin (`Chosen`) ilan sayfası ziyaret edilir ve `RedfinDetailsReader` ile ayrıntılar okunur.
* Aday listesi, seçili ev, fiyat geçmişi ve kart metni değiştirilmez; yalnızca ayrıntı alanları ve eksikse koordinat doldurulur.
* Ziyaretler mevcut Playwright oturumuyla, aday toplamadaki bekleme süreleriyle yapılır.
* Sayfa açılmazsa ya da alanlar bulunamazsa ev atlanır, uyarı yazılır.
* Sonra `listings_{ST}.json` ve `ev_detaylari_{ST}.md` yeniden yazılır.

## 2. "Ev kartlarını topla" elle yapılan seçimi korusun

* Bir county yeniden toplanırken, önceki seçili evin adresi (`Url`) yeni aday listesinde de varsa o ev seçili kalır.
* Önceki seçili ev yeni listede yoksa (satılmış ya da ilandan kalkmış olabilir), program kendi kuralıyla seçer ve uyarı yazar: "{County}: önceki seçili ev ({adres}) artık aday listesinde yok; yeni ev seçildi."

## Test (Florida)

* "Ek verileri çek" sonrası on seçili evin hepsinde koordinat dolu; seçili evler değişmemiş (Walton 124 Conner Cir, Sumter 375 Simpson St, Pasco 13639 Frances Ave, Collier 883 Coconut Cir E, Highlands 4515 Starfish Ave).
* Pasco evinde açıklama metni "DIRECT GULF ACCESS" ile başlıyor; Redfin sel bölgesi AE, sel sigortası tahmini 1.737–8.500 $; FEMA sel bölgesi AE.
* `CLAUDE.md` güncellenir.
