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
