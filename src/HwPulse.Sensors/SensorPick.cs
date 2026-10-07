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

    // Con ventiladores configurados se muestran esos, en ese orden y con ese nombre. Los sensores
    // con la misma etiqueta forman un solo marcador con la media de los que giran: «CHASIS» para
    // todos los de la caja, «CPU» para CPU_FAN y CPU_OPT. Sin configuración, uno por sensor que
    // gire: las placas reportan muchos conectores vacíos a 0 RPM.
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
            .GroupBy(s => s.Label)
            .Select(group => (
                Label: group.Key,
                group.First().MaxRpm,
                Rpms: group
                    .Select(s => fans.FirstOrDefault(f => f.Name == s.Sensor))
                    .Where(f => f.Name is not null)
                    .Select(f => f.Value!.Value)
                    .ToList()))
            .Where(group => group.Rpms.Count > 0)
            .Select(group => Fan(group.Label, SpinningAverage(group.Rpms), group.MaxRpm))
            .ToList();
    }

    // Los ventiladores de la gráfica, en un solo marcador. La barra sale del % de control que da la
    // tarjeta, porque el máximo de RPM cambia de un modelo a otro; sin control, contra el máximo
    // por defecto.
    public static FanStatus? GpuFan(IReadOnlyList<Reading> readings)
    {
        var rpms = Values(readings, SensorType.Fan);
        if (rpms.Count == 0) return null;

        var rpm = SpinningAverage(rpms);
        var controls = Values(readings, SensorType.Control);
        return controls.Count == 0
            ? Fan("GPU", rpm, FanSetting.DefaultMaxRpm)
            : new FanStatus("GPU", rpm, Math.Clamp(controls.Average() / 100, 0f, 1f));
    }

    // El adaptador principal es el que más datos ha movido desde el arranque: así no salen los
    // virtuales (vEthernet, Bluetooth) que existen pero apenas tienen tráfico.
    public static NetworkStatus? Network(IReadOnlyList<(string Name, IReadOnlyList<Reading> Readings)> adapters)
    {
        if (adapters.Count == 0) return null;
        var (name, readings) = adapters.MaxBy(a => Values(a.Readings, SensorType.Data).Sum());
        return new NetworkStatus(
            name,
            Named(readings, SensorType.Throughput, "Download Speed"),
            Named(readings, SensorType.Throughput, "Upload Speed"));
    }

    private static float SpinningAverage(List<float> rpms)
    {
        var spinning = rpms.Where(r => r > 0).ToList();
        return spinning.Count == 0 ? 0 : spinning.Average();
    }

    private static List<float> Values(IReadOnlyList<Reading> readings, SensorType type) =>
        readings.Where(r => r.Type == type && r.Value is not null).Select(r => r.Value!.Value).ToList();

    private static FanStatus Fan(string label, float rpm, float maxRpm) =>
        new(label, rpm, Math.Clamp(rpm / maxRpm, 0f, 1f));

    private static float? Named(IReadOnlyList<Reading> readings, SensorType type, string name) =>
        readings.FirstOrDefault(r => r.Type == type && r.Name == name && r.Value is not null).Value;

    private static float? First(IReadOnlyList<Reading> readings, SensorType type) =>
        readings.FirstOrDefault(r => r.Type == type && r.Value is not null).Value;

    private static float? Max(IReadOnlyList<Reading> readings, SensorType type) =>
        readings.Where(r => r.Type == type && r.Value is not null).Max(r => r.Value);
}
