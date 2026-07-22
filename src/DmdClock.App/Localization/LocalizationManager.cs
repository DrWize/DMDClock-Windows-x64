using System.Text.Json;

namespace DmdClock.App.Localization;

public static class LocalizationManager
{
    private static Dictionary<string, string> _strings = [];

    public static void Load(string language)
    {
        _strings = Read("en");
        if (language != "en")
            foreach (var pair in Read(language)) _strings[pair.Key] = pair.Value;
    }

    public static string Get(string key) => _strings.GetValueOrDefault(key, key);

    public static string? FindKey(string value) =>
        _strings.FirstOrDefault(pair => pair.Value == value).Key;

    private static Dictionary<string, string> Read(string language)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "i18n", language + ".json");
        if (!File.Exists(path)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];
        }
        catch (JsonException) { return []; }
        catch (IOException) { return []; }
    }
}
