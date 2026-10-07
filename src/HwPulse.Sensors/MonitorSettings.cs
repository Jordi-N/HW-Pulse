using System.Text.Json;

namespace HwPulse.Sensors;

// settings.json junto al ejecutable. Opcional: sin él se muestra todo lo detectado con el nombre
// que da el hardware.
//
// {
//   "fans": [
//     { "sensor": "Fan #2", "label": "PUMP", "maxRpm": 3000 }
//   ]
// }
public sealed record MonitorSettings(IReadOnlyList<FanSetting> Fans)
{
    public static readonly MonitorSettings Empty = new([]);

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
        return parsed is null ? Empty : new MonitorSettings(parsed.Fans ?? []);
    }
}

public sealed record FanSetting(string Sensor, string Label, float MaxRpm = FanSetting.DefaultMaxRpm)
{
    public const float DefaultMaxRpm = 2000;
}
