using System.Text.Json;

namespace HwPulse.Sensors;

// settings.json junto al ejecutable. Opcional: sin él se muestra todo lo detectado con el nombre
// que da el hardware, y como servicios IIS, Jellyfin y los runners de GitHub que haya instalados.
//
// {
//   "fans": [
//     { "sensor": "Fan #7", "label": "PUMP", "maxRpm": 3000 },
//     { "sensor": "Fan #1", "label": "CHASIS" },
//     { "sensor": "Fan #4", "label": "CHASIS" }
//   ],
//   "services": [
//     { "name": "W3SVC", "label": "IIS" }
//   ]
// }
public sealed record MonitorSettings(IReadOnlyList<FanSetting> Fans, IReadOnlyList<ServiceSetting> Services)
{
    public static readonly MonitorSettings Empty = new([], []);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static MonitorSettings Load(string path) =>
        File.Exists(path) ? Parse(File.ReadAllText(path)) : Empty;

    public static MonitorSettings Parse(string json)
    {
        var parsed = JsonSerializer.Deserialize<MonitorSettings>(json, Options);
        return parsed is null ? Empty : new MonitorSettings(parsed.Fans ?? [], parsed.Services ?? []);
    }
}

public sealed record FanSetting(string Sensor, string Label, float MaxRpm = FanSetting.DefaultMaxRpm)
{
    public const float DefaultMaxRpm = 2000;
}

public sealed record ServiceSetting(string Name, string Label);
