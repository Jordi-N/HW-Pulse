using System.Text.Json;
using System.Text.Json.Nodes;

namespace HwPulse.Sensors;

// settings.json junto al ejecutable. Opcional: sin él se muestra todo lo detectado con el nombre
// que da el hardware, y como servicios IIS, Jellyfin y los runners de GitHub que haya instalados.
// «display» no se escribe a mano: lo guarda el panel al elegir pantalla con el clic derecho.
//
// {
//   "fans": [
//     { "sensor": "Fan #7", "label": "PUMP", "maxRpm": 3000 },
//     { "sensor": "Fan #1", "label": "CHASIS" },
//     { "sensor": "Fan #4", "label": "CHASIS" }
//   ],
//   "services": [
//     { "name": "W3SVC", "label": "IIS" }
//   ],
//   "display": { "x": -1280, "y": 0 }
// }
public sealed record MonitorSettings(
    IReadOnlyList<FanSetting> Fans,
    IReadOnlyList<ServiceSetting> Services,
    DisplayCorner? Display = null)
{
    public static readonly MonitorSettings Empty = new([], []);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static MonitorSettings Load(string path) =>
        File.Exists(path) ? Parse(File.ReadAllText(path)) : Empty;

    public static MonitorSettings Parse(string json)
    {
        var parsed = JsonSerializer.Deserialize<MonitorSettings>(json, Options);
        return parsed is null ? Empty : new MonitorSettings(parsed.Fans ?? [], parsed.Services ?? [], parsed.Display);
    }

    public static void SaveDisplay(string path, DisplayCorner corner) =>
        File.WriteAllText(path, WithDisplay(File.Exists(path) ? File.ReadAllText(path) : null, corner));

    // Cambia solo «display» y deja el resto como estaba, salvo los comentarios: JSON no los guarda.
    public static string WithDisplay(string? json, DisplayCorner corner)
    {
        var root = json is null ? null : JsonNode.Parse(json, documentOptions: DocumentOptions);
        var settings = root as JsonObject ?? [];
        var display = new JsonObject { ["x"] = corner.X, ["y"] = corner.Y };
        var existing = settings.FirstOrDefault(p => p.Key.Equals("display", StringComparison.OrdinalIgnoreCase)).Key;
        settings[existing ?? "display"] = display;
        return settings.ToJsonString(WriteOptions);
    }
}

public sealed record FanSetting(string Sensor, string Label, float MaxRpm = FanSetting.DefaultMaxRpm)
{
    public const float DefaultMaxRpm = 2000;
}

public sealed record ServiceSetting(string Name, string Label);
