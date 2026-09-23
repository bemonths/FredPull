using System.Text;

namespace FredPull;

/// ABD eyaletleri ve ilçe/FIPS kataloğu.
/// Katalog ABD Sayım Bürosu'nun resmî dosyasından bir kez indirilir, exe yanına counties_all.txt olarak kaydedilir.
public static class CountyCatalog
{
    public static readonly (string Abbr, string Name)[] States =
    {
        ("AL","Alabama"), ("AK","Alaska"), ("AZ","Arizona"), ("AR","Arkansas"), ("CA","California"),
        ("CO","Colorado"), ("CT","Connecticut"), ("DE","Delaware"), ("DC","District of Columbia"), ("FL","Florida"),
        ("GA","Georgia"), ("HI","Hawaii"), ("ID","Idaho"), ("IL","Illinois"), ("IN","Indiana"),
        ("IA","Iowa"), ("KS","Kansas"), ("KY","Kentucky"), ("LA","Louisiana"), ("ME","Maine"),
        ("MD","Maryland"), ("MA","Massachusetts"), ("MI","Michigan"), ("MN","Minnesota"), ("MS","Mississippi"),
        ("MO","Missouri"), ("MT","Montana"), ("NE","Nebraska"), ("NV","Nevada"), ("NH","New Hampshire"),
        ("NJ","New Jersey"), ("NM","New Mexico"), ("NY","New York"), ("NC","North Carolina"), ("ND","North Dakota"),
        ("OH","Ohio"), ("OK","Oklahoma"), ("OR","Oregon"), ("PA","Pennsylvania"), ("RI","Rhode Island"),
        ("SC","South Carolina"), ("SD","South Dakota"), ("TN","Tennessee"), ("TX","Texas"), ("UT","Utah"),
        ("VT","Vermont"), ("VA","Virginia"), ("WA","Washington"), ("WV","West Virginia"), ("WI","Wisconsin"),
        ("WY","Wyoming"),
    };

    static readonly string[] SourceUrls =
    {
        "https://www2.census.gov/geo/docs/reference/codes2020/national_county2020.txt",   // STATE|STATEFP|COUNTYFP|COUNTYNS|COUNTYNAME|CLASSFP|FUNCSTAT
        "https://www2.census.gov/geo/docs/reference/codes/files/national_county.txt",      // STATE,STATEFP,COUNTYFP,COUNTYNAME,CLASSFP
    };

    /// Eyalet kısaltması -> ilçe listesi. Önce yerel dosya, yoksa indirip kaydeder. Hiçbiri olmazsa null.
    public static async Task<Dictionary<string, List<County>>?> LoadAsync(string cacheFile, IProgress<string> status, CancellationToken ct)
    {
        string? text = null;

        if (File.Exists(cacheFile))
        {
            text = await File.ReadAllTextAsync(cacheFile, ct);
            status.Report($"İlçe kataloğu yerel dosyadan okundu: {Path.GetFileName(cacheFile)}");
        }
        else
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            foreach (var url in SourceUrls)
            {
                try
                {
                    status.Report($"İlçe kataloğu indiriliyor: {url}");
                    text = await http.GetStringAsync(url, ct);
                    if (text.Length > 10_000) { await File.WriteAllTextAsync(cacheFile, text, Encoding.UTF8, ct); break; }
                    text = null;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception) { text = null; }
            }
        }

        if (text == null) return null;
        var dict = Parse(text);
        return dict.Count > 0 ? dict : null;
    }

    static Dictionary<string, List<County>> Parse(string text)
    {
        var valid = States.Select(s => s.Abbr).ToHashSet();
        var dict = new Dictionary<string, List<County>>();

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            bool pipe = line.Contains('|');
            var p = pipe ? line.Split('|') : line.Split(',');
            if (p.Length < 4) continue;

            var state = p[0].Trim();
            if (!valid.Contains(state)) continue;                 // başlık satırı ve PR/GU gibi bölgeler elenir

            var fips = p[1].Trim() + p[2].Trim();
            if (fips.Length != 5 || !fips.All(char.IsDigit)) continue;

            var name = pipe ? (p.Length > 4 ? p[4].Trim() : "") : p[3].Trim();
            if (name.Length == 0) continue;

            if (!dict.TryGetValue(state, out var list)) dict[state] = list = new List<County>();
            list.Add(new County(state, fips, name));
        }

        foreach (var list in dict.Values) list.Sort((a, b) => string.CompareOrdinal(a.Fips, b.Fips));
        return dict;
    }

    /// Katalog indirilemezse en azından Florida çalışsın.
    public static List<County> FloridaFallback()
    {
        const string csv =
            "12001,Alachua County;12003,Baker County;12005,Bay County;12007,Bradford County;12009,Brevard County;12011,Broward County;" +
            "12013,Calhoun County;12015,Charlotte County;12017,Citrus County;12019,Clay County;12021,Collier County;12023,Columbia County;" +
            "12027,DeSoto County;12029,Dixie County;12031,Duval County;12033,Escambia County;12035,Flagler County;12037,Franklin County;" +
            "12039,Gadsden County;12041,Gilchrist County;12043,Glades County;12045,Gulf County;12047,Hamilton County;12049,Hardee County;" +
            "12051,Hendry County;12053,Hernando County;12055,Highlands County;12057,Hillsborough County;12059,Holmes County;12061,Indian River County;" +
            "12063,Jackson County;12065,Jefferson County;12067,Lafayette County;12069,Lake County;12071,Lee County;12073,Leon County;" +
            "12075,Levy County;12077,Liberty County;12079,Madison County;12081,Manatee County;12083,Marion County;12085,Martin County;" +
            "12086,Miami-Dade County;12087,Monroe County;12089,Nassau County;12091,Okaloosa County;12093,Okeechobee County;12095,Orange County;" +
            "12097,Osceola County;12099,Palm Beach County;12101,Pasco County;12103,Pinellas County;12105,Polk County;12107,Putnam County;" +
            "12109,St. Johns County;12111,St. Lucie County;12113,Santa Rosa County;12115,Sarasota County;12117,Seminole County;12119,Sumter County;" +
            "12121,Suwannee County;12123,Taylor County;12125,Union County;12127,Volusia County;12129,Wakulla County;12131,Walton County;12133,Washington County";
        return csv.Split(';').Select(x => x.Split(',')).Select(p => new County("FL", p[0], p[1])).ToList();
    }
}
