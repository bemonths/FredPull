using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FredPull;

/// Flow intro prompt'u: eyalet verisi (PromptData\states_intro.json), şablon (PromptData\intro_prompt_template.txt;
/// exe yanında user_intro_prompt_template.txt varsa o) ve kullanıcının eyalet bazındaki düzeltmeleri (exe yanında intro_overrides.json).
public static class IntroPrompt
{
    public const string StatesFile = "states_intro.json";
    public const string TemplateFile = "intro_prompt_template.txt";
    /// Kullanıcının düzenlediği şablon. PromptData her derlemede depodan yenilendiği için orada değil, exe yanında durur.
    public const string UserTemplateFile = "user_intro_prompt_template.txt";
    public const string OverridesFile = "intro_overrides.json";

    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public sealed record StateIntro(
        [property: JsonPropertyName("abbr")] string Abbr,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("neighbors")] string Neighbors,
        [property: JsonPropertyName("pin_city")] string PinCity);

    /// Kullanıcının bir eyalet için yazdığı değerler; ikisi de varsayılana dönerse kayıt silinir.
    public sealed class Override
    {
        public string Neighbors { get; set; } = "";
        public string PinCity { get; set; } = "";
    }

    public static string Dir(string baseDir) => Path.Combine(baseDir, "PromptData");

    /// Eyalet kısaltması (büyük harf) → veri.
    public static Dictionary<string, StateIntro> LoadStates(string path)
    {
        var dict = new Dictionary<string, StateIntro>();
        foreach (var s in JsonSerializer.Deserialize<List<StateIntro>>(File.ReadAllText(path, Encoding.UTF8)) ?? new())
            if (!string.IsNullOrWhiteSpace(s.Abbr)) dict[s.Abbr.Trim().ToUpperInvariant()] = s;
        return dict;
    }

    /// Satır sonları \r\n'e çevrilir: TextBox satırları doğru göstersin, kopyalanan metin önizlemeyle aynı olsun.
    public static string LoadTemplate(string path) => File.ReadAllText(path, Encoding.UTF8).ReplaceLineEndings("\r\n");

    /// Düz metin değiştirme, büyük/küçük harf duyarlı; sıra sabit: önce [STATE IN CAPITALS], sonra [STATE].
    public static string Fill(string template, string stateName, string neighbors, string pinCity) =>
        template.Replace("[STATE IN CAPITALS]", stateName.ToUpperInvariant())
                .Replace("[STATE]", stateName)
                .Replace("[NEIGHBORS]", neighbors)
                .Replace("[PIN CITY]", pinCity);

    static readonly Regex Placeholder = new(@"\[[^\[\]\r\n]{1,40}\]");

    /// Doldurulmadan kalan [YER TUTUCU]'lar.
    public static List<string> Leftovers(string text) => Placeholder.Matches(text).Select(m => m.Value).Distinct().ToList();

    public static Dictionary<string, Override> LoadOverrides(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<Dictionary<string, Override>>(File.ReadAllText(path, Encoding.UTF8)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException) { }
        return new();
    }

    public static void SaveOverrides(string path, Dictionary<string, Override> overrides) =>
        File.WriteAllText(path, JsonSerializer.Serialize(overrides, JsonOpts), new UTF8Encoding(false));
}
