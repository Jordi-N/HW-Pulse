using LibreHardwareMonitor.Hardware;

namespace HwPulse.Sensors;

internal readonly record struct Reading(SensorType Type, string Name, float? Value);

// Qué sensor de LibreHardwareMonitor representa cada dato de la pantalla. Los nombres cambian entre
// fabricantes y generaciones, así que cada elección tiene un nombre preferido y una alternativa.
internal static class SensorPick
{
    public static float? CpuTemperature(IReadOnlyList<Reading> readings) =>
        Named(readings, SensorType.Temperature, "CPU Package")
        ?? Named(readings, SensorType.Temperature, "Core Max")
        ?? Max(readings, SensorType.Temperature);

    public static float? CpuLoad(IReadOnlyList<Reading> readings) =>
        Named(readings, SensorType.Load, "CPU Total");

    // Media de los núcleos: «Core #1», y en los híbridos de Intel «P-Core #1» y «E-Core #1».
    public static float? CpuClock(IReadOnlyList<Reading> readings)
    {
        var cores = readings
            .Where(r => r.Type == SensorType.Clock && r.Value is > 0 && r.Name.Contains("Core #", StringComparison.Ordinal))
            .Select(r => r.Value!.Value)
            .ToList();
        return cores.Count == 0 ? null : cores.Average();
    }

    public static float? GpuTemperature(IReadOnlyList<Reading> readings) =>
        Named(readings, SensorType.Temperature, "GPU Core") ?? First(readings, SensorType.Temperature);

    public static float? GpuLoad(IReadOnlyList<Reading> readings) =>
        Named(readings, SensorType.Load, "GPU Core") ?? First(readings, SensorType.Load);

    public static float? GpuClock(IReadOnlyList<Reading> readings) =>
        Named(readings, SensorType.Clock, "GPU Core") ?? First(readings, SensorType.Clock);

    public static float? Temperature(IReadOnlyList<Reading> readings) => First(readings, SensorType.Temperature);

    // Una gráfica dedicada antes que la integrada: si el equipo tiene las dos, la que importa es la dedicada.
    public static T? Gpu<T>(IEnumerable<T> gpus, Func<T, HardwareType> typeOf) where T : class =>
        gpus.OrderBy(g => typeOf(g) == HardwareType.GpuIntel ? 1 : 0).FirstOrDefault();

    // Con ventiladores configurados se muestran esos, en ese orden y con ese nombre. Sin
    // configuración, los que giran: las placas reportan muchos conectores vacíos a 0 RPM.
    public static IReadOnlyList<FanStatus> Fans(IReadOnlyList<Reading> readings, IReadOnlyList<FanSetting> settings)
    {
        var fans = readings.Where(r => r.Type == SensorType.Fan && r.Value is not null).ToList();
        if (settings.Count == 0)
        {
            return fans
                .Where(f => f.Value > 0)
                .Select(f => Fan(f.Name, f.Value!.Value, FanSetting.DefaultMaxRpm))
                .ToList();
        }

        return settings
            .Select(s => (Setting: s, Reading: fans.FirstOrDefault(f => f.Name == s.Sensor)))
            .Where(p => p.Reading.Name is not null)
            .Select(p => Fan(p.Setting.Label, p.Reading.Value!.Value, p.Setting.MaxRpm))
            .ToList();
    }

    private static FanStatus Fan(string label, float rpm, float maxRpm) =>
        new(label, rpm, Math.Clamp(rpm / maxRpm, 0f, 1f));

    private static float? Named(IReadOnlyList<Reading> readings, SensorType type, string name) =>
        readings.FirstOrDefault(r => r.Type == type && r.Name == name && r.Value is not null).Value;

    private static float? First(IReadOnlyList<Reading> readings, SensorType type) =>
        readings.FirstOrDefault(r => r.Type == type && r.Value is not null).Value;

    private static float? Max(IReadOnlyList<Reading> readings, SensorType type) =>
        readings.Where(r => r.Type == type && r.Value is not null).Max(r => r.Value);
}
