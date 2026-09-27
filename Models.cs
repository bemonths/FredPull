namespace FredPull;

public record County(string State, string Fips, string Name);

public record Obs(DateOnly Date, double? Value);

/// Bir birimin (ilçe, eyalet veya ABD) bütün serileri.
public class SeriesSet
{
    public Dictionary<string, List<Obs>> Fred { get; set; } = new();    // ACTLISCOU, NEWLISCOU, PENLISCOU, MEDDAYONMAR, PRIREDCOU, MEDLISPRI
    public Dictionary<string, List<Obs>> Redfin { get; set; } = new();  // homes_sold, median_sale_price, avg_sale_to_list, months_of_supply, ...

    public List<Obs>? F(string key) => Fred.TryGetValue(key, out var s) ? s : null;
    public List<Obs>? R(string key) => Redfin.TryGetValue(key, out var s) ? s : null;
}

public class CountyResult
{
    public County County { get; set; } = null!;
    public SeriesSet Data { get; set; } = new();
    public string Month { get; set; } = "";         // FRED veri ayı (yyyy-MM)
    public string RedfinMonth { get; set; } = "";   // Redfin veri ayı; boşsa Redfin verisi yok

    // İlan tarafı (FRED / Realtor.com)
    public double? Active { get; set; }
    public double? ActiveYoY { get; set; }
    public double? ActiveVs2019 { get; set; }
    public double? NewYoY { get; set; }
    public double? PendingYoY { get; set; }
    public double? Dom { get; set; }
    public double? DomYoY { get; set; }
    public double? CutShare { get; set; }
    public double? CutShareYoY { get; set; }
    public double? CutShareVsState { get; set; }
    public double? CutShareVsUs { get; set; }
    public double? ListPrice { get; set; }
    public double? ListPriceYoY { get; set; }
    public double? ListPriceFromPeak { get; set; }

    // Satış tarafı (Redfin)
    public double? Sold { get; set; }
    public double? SoldYoY { get; set; }
    public double? SalePrice { get; set; }
    public double? SalePriceYoY { get; set; }
    public double? SalePriceFromPeak { get; set; }
    public double? SaleToList { get; set; }
    public double? MonthsSupply { get; set; }
    public double? MonthsSupplyState { get; set; }
    public string? PeakLabel { get; set; }

    // Değerlendirme
    public int Score { get; set; }
    public string Signal { get; set; } = "";
    public string ScoreBreakdown { get; set; } = "";
    public string Reading { get; set; } = "";
}

public record PriceCut(DateOnly Date, int Price);

/// Redfin ilanı: liste sayfasındaki alanlar + ilan sayfasındaki fiyat geçmişinden hesaplananlar.
public class HouseCandidate
{
    public string Url { get; set; } = "";
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Zip { get; set; } = "";
    public int Price { get; set; }                  // liste sayfasındaki güncel fiyat
    public int DaysOnRedfin { get; set; }           // timeOnRedfin / 86 400 000
    public int PropertyType { get; set; }           // 6 müstakil, 3 daire, 13 townhouse
    public bool IsNewConstruction { get; set; }
    public string MlsStatus { get; set; } = "";
    public int? YearBuilt { get; set; }
    public int? SqFt { get; set; }
    public double? Beds { get; set; }
    public double? Baths { get; set; }

    // Fiyat geçmişi (ilan sayfası)
    public bool HasHistory { get; set; }
    public DateOnly? ListedDate { get; set; }
    public int? OriginalPrice { get; set; }
    public int? CurrentPrice { get; set; }
    public List<PriceCut> Cuts { get; set; } = new();   // eski → yeni
    public int Days { get; set; }                       // bugün − ListedDate
    public DateOnly? LastSaleDate { get; set; }
    public int? LastSalePrice { get; set; }
    public bool PreviouslyWithdrawn { get; set; }
    public bool RaisedAfterCuts { get; set; }           // indirimlerden sonra fiyat yeniden artırılmış
    public int? RaisedFrom { get; set; }                // artıştan hemen önceki (indirimlerle inilen) fiyat
    public int? RaisedTo { get; set; }                  // artırılan fiyat (son artış)
    public DateOnly? RaisedDate { get; set; }
    public string? Error { get; set; }
    public List<System.Text.Json.JsonElement>? RawHistory { get; set; }   // Redfin'in ham fiyat geçmişi (teşhis için)

    // Konum ve medya (liste ya da ilan sayfasından; eski kayıtlarda boş)
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public List<PriceCut> PriceSteps { get; set; } = new();   // mevcut ilanın fiyat adımları: ilk fiyat + her değişiklik (animasyon)
    public List<string> PhotoUrls { get; set; } = new();      // ilan sayfasındaki fotoğraf adresleri
    public List<string> PhotoPaths { get; set; } = new();     // indirilen referans fotoğraflar, out klasörüne göre göreli
    public string? LatLngSource { get; set; }                 // "redfin", "census geocoder" ya da "openstreetmap nominatim"

    // Ham ek bilgiler (ek veri katmanı): FredPull yorumlamaz, bulunamayan alan boş kalır
    public RedfinDetails? Redfin { get; set; }                // ilan sayfasındaki ayrıntılar, sayfada yazdığı gibi
    public string? ListingStatus { get; set; }                // son ziyarette Redfin'in gösterdiği ilan durumu (Active, Pending, Sold...)
    public DateTime? ListingStatusAt { get; set; }            // durumun okunduğu (ya da okunamadığı) zaman
    public string? ListingStatusNote { get; set; }            // durum okunamadıysa sebebi: HTTP durumu, yönlendirme, engel, boş sayfa
    public string? FemaResult { get; set; }                   // "bölge bulundu" / "sorgu başarılı, bölge bulunamadı" / "sorgu başarısız"
    public string? FemaZone { get; set; }                     // FEMA NFHL nokta sorgusu: FLD_ZONE
    public string? FemaZoneSubtype { get; set; }              // ZONE_SUBTY
    public string? FemaSfha { get; set; }                     // SFHA_TF ("T" / "F")
    public DateTime? FemaQueriedAt { get; set; }
    public string? FemaSource { get; set; }                   // sorgulanan servis (FEMA'nın kendisi ya da Esri kopyası) ve veri tarihi
    public string? FemaVersion { get; set; }                  // verinin sürümü: resmî serviste sorgu tarihi, kopyada katmanın kendi ifadesi
    public string? FemaNote { get; set; }                     // sorgu başarılı, noktada poligon yok: kaynağın kapsamı
    public string? FemaError { get; set; }                    // servis hatası

    public int TotalCut => (OriginalPrice ?? 0) - (CurrentPrice ?? 0);
}

/// İlan sayfasından (yakalanan stingray yanıtları ve HTML'e gömülü bloklar) okunan ham ayrıntılar. Değerler Redfin'in
/// yazdığı gibi metin olarak saklanır; hesap ve yorum yapılmaz. Bulunamayan alan null. Found: alan → bulunduğu yer.
public class RedfinDetails
{
    public DateTime ReadAt { get; set; }
    public string? ListingStatus { get; set; }                // Redfin'in gösterdiği ilan durumu: Active, Pending, Sold...
    public string? Description { get; set; }                  // "About this home"
    public string? PropertyTax { get; set; }                  // yıllık emlak vergisi (kamu kayıtları)
    public string? PropertyTaxYear { get; set; }
    public string? Hoa { get; set; }                          // aidat tutarı (MLS alanı, yazıldığı gibi)
    public string? HoaPeriod { get; set; }                    // aidat dönemi (MLS alanı)
    public string? HoaMonthlyRedfin { get; set; }             // Redfin ödeme hesaplayıcısının aylık aidat alanı
    public string? HoaAmenities { get; set; }                 // ilanın "HOA Information" grubu olduğu gibi (ALAN=değer; ...)
    public string? FloodZone { get; set; }                    // Redfin'in gösterdiği FEMA bölgesi (tahmini, MassiveCert)
    public string? FloodInsuranceEstimate { get; set; }       // Redfin tahmini yıllık sel sigortası aralığı ($, alt–üst)
    public Dictionary<string, string> ClimateRisk { get; set; } = new();   // flood / fire / heat / wind / air → First Street puanı (1-10)
    public string? AgentName { get; set; }
    public string? OfficeName { get; set; }
    public string? SoldDate { get; set; }                     // addressSectionInfo.soldDate (yyyy-MM-dd, UTC günü)
    public string? PriceLabel { get; set; }                   // sayfanın ana fiyatının etiketi ("Price", "Last Sold Price"...)
    public string? PriceAmount { get; set; }                  // o fiyat
    public RawHistoryEvent? LastEvent { get; set; }           // fiyat geçmişindeki en yeni olay (ne olursa olsun)
    public RawHistoryEvent? LastSaleEvent { get; set; }       // en yeni "Sold ..." olayı
    public RawHistoryEvent? LastContractEvent { get; set; }   // en yeni "Pending" / "Contingent" / "Under Contract" olayı
    public Dictionary<string, string> Found { get; set; } = new();
}

/// Redfin fiyat geçmişindeki bir olay, yazıldığı gibi.
public class RawHistoryEvent
{
    public string Date { get; set; } = "";                    // yyyy-MM-dd (UTC günü; Redfin gece yarısı ABD saatiyle yazıyor)
    public string Description { get; set; } = "";
    public string? Price { get; set; }
    public string? Source { get; set; }                       // MLS adı ya da "Public Records"
    public override string ToString() => $"{Date} {Description}{(Price != null ? $" {Price} $" : "")}{(Source != null ? $" ({Source})" : "")}";
}

/// FEMA National Flood Hazard Layer nokta sorgusunun sonucu (FLD_ZONE, ZONE_SUBTY, SFHA_TF). Source: kullanılan servis.
/// Evin kaydına HouseCandidate.Fema* alanları olarak yazılır.
public class FemaFlood
{
    public string? Zone { get; set; }
    public string? Subtype { get; set; }
    public string? Sfha { get; set; }
    public string Source { get; set; } = "";
    public DateTime QueriedAt { get; set; }
    public string? Error { get; set; }
    /// ExtraData.FemaFound / FemaNoZone / FemaFailed.
    public string? Result { get; set; }
    /// Verinin sürümü (resmî servis: sorgu tarihi; kopya: katmanın açıklamasındaki sürüm).
    public string? Version { get; set; }
    /// Bölge bulunamadıysa katmanın kendi açıklamasındaki kapsam cümleleri (Zone boş kalır).
    public string? Note { get; set; }
}

/// Bir ilçe için seçilen ev + adaylar; out\listings_XX.json içinde ilçe başına bir kayıt.
public class HouseCard
{
    public string Fips { get; set; } = "";
    public string County { get; set; } = "";
    public string Mode { get; set; } = "";          // "Müstakil ev" / "Daire"
    public DateTime CreatedAt { get; set; }
    public int MinPrice { get; set; }
    public int MaxPrice { get; set; }
    public string ListUrl { get; set; } = "";
    public int ListCount { get; set; }              // liste sayfasından gelen ilan sayısı
    public List<HouseCandidate> Candidates { get; set; } = new();
    public HouseCandidate? Chosen { get; set; }
    public string CardText { get; set; } = "";
    public string Note { get; set; } = "";          // seçim yoksa neden
    /// Bu toplamada önceki seçili ev korunamadıysa uyarı (dosyaya yazılmaz).
    [System.Text.Json.Serialization.JsonIgnore] public string? Warning { get; set; }
}

/// Bir eyaletin bütün çekilmiş verisi; out\cache_XX.json olarak saklanır.
public class Snapshot
{
    public string State { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool HasRedfin { get; set; }
    public SeriesSet StateData { get; set; } = new();
    public SeriesSet UsData { get; set; } = new();
    public List<CountyResult> Counties { get; set; } = new();
}
