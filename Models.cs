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
    public string? Error { get; set; }
    public List<System.Text.Json.JsonElement>? RawHistory { get; set; }   // Redfin'in ham fiyat geçmişi (teşhis için)

    // Konum ve medya (liste ya da ilan sayfasından; eski kayıtlarda boş)
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public List<PriceCut> PriceSteps { get; set; } = new();   // mevcut ilanın fiyat adımları: ilk fiyat + her değişiklik (animasyon)
    public List<string> PhotoUrls { get; set; } = new();      // ilan sayfasındaki fotoğraf adresleri
    public List<string> PhotoPaths { get; set; } = new();     // indirilen referans fotoğraflar, out klasörüne göre göreli

    public int TotalCut => (OriginalPrice ?? 0) - (CurrentPrice ?? 0);
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
