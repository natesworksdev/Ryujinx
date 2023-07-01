using System.Text.Json;
using System.Text.Json.Nodes;

var localesDir = "../Ryujinx.Ava/Assets/Locales";

var defaultLocale = "en_US.json";

var defaultLocaleDict = ReadLocaleFile(Path.Combine(localesDir, defaultLocale));


foreach (var file in Directory.EnumerateFiles(localesDir, "*.json")) {
    if (file.EndsWith(defaultLocale)) {
        continue;
    }
    var localeDict = ReadLocaleFile(file);
    var mergedDict = new Dictionary<string, string>(defaultLocaleDict);
    foreach (var key in mergedDict.Keys) {
        if (localeDict.ContainsKey(key)) {
            mergedDict[key] = localeDict[key];
        }
    }
    // keep outdated keys in json locales.
    foreach (var key in localeDict.Keys) {
        if (!defaultLocaleDict.ContainsKey(key)) {
            mergedDict[key] = localeDict[key];
        }
    }
    var newFile = file;
    WriteLocaleFile(newFile, mergedDict);
}

static void WriteLocaleFile(string filePath, Dictionary<string, string> dict) {
    var content = JsonSerializer.Serialize(
        dict,
        new JsonSerializerOptions {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }
    );
    File.WriteAllText(filePath, content);
}

static Dictionary<string, string> ReadLocaleFile(string filePath) {
    var jsonText = File.ReadAllText(filePath);
    JsonDocument jsonDoc = JsonDocument.Parse(jsonText);
    JsonObject jsonObj = JsonObject.Parse(jsonText) as JsonObject;

    var result = new Dictionary<string, string>();

    foreach (var entry in jsonObj) {
        result.Add(entry.Key, entry.Value.GetValue<string>());
    }

    return result;
}
