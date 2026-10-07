using System.Globalization;
using System.Text.RegularExpressions;
using HwPulse.Sensors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HwPulse.App;

// Textos y proporciones de la pantalla. Un valor que el hardware no da se pinta «—», nunca 0.
internal static partial class Format
{
    private const string Missing = "—";
    private const double BytesPerGb = 1024d * 1024 * 1024;

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    public static string Celsius(double celsius) =>
        double.IsNaN(celsius) ? Missing : string.Create(Culture, $"{celsius:0}");

    public static string Percent(float? value) =>
        value is null ? Missing : string.Create(Culture, $"{value:0}%");

    public static string Mhz(float? mhz) =>
        mhz is null ? Missing : string.Create(Culture, $"{mhz:0} MHz");

    public static string Gb(long bytes) => string.Create(Culture, $"{bytes / BytesPerGb:0} GB");

    // En bits, como se anuncian las conexiones: 1 Gbps de fibra, no 125 MB/s.
    public static string Bitrate(float? bytesPerSecond) => (bytesPerSecond * 8) switch
    {
        null => Missing,
        < 1e6f and var bits => string.Create(Culture, $"{bits / 1e3f:0} kbps"),
        < 1e9f and var bits => string.Create(Culture, $"{bits / 1e6f:0.0} Mbps"),
        var bits => string.Create(Culture, $"{bits / 1e9f:0.00} Gbps"),
    };

    public static string Rpm(float rpm) => string.Create(Culture, $"{rpm:0} RPM");

    public static double Fraction(float? value, float max) =>
        value is null || max <= 0 ? 0 : Math.Clamp(value.Value / max, 0, 1);

    // El nombre del título, sin la marca ni la generación: «Intel Core Ultra 7 265K» → «Ultra 7 265K»,
    // «NVIDIA GeForce GTX 1070» → «GTX 1070».
    public static string ChipName(string? name) =>
        name is null ? Missing : ChipNoise().Replace(name, "").Trim();

    [GeneratedRegex(@"\d+(st|nd|rd|th) Gen |Intel\(R\)|Core\(TM\)|Intel Core |Intel |NVIDIA GeForce |NVIDIA |AMD Radeon |AMD | \d+-Core Processor|\(R\)|\(TM\)")]
    private static partial Regex ChipNoise();
}

// La barra cambia de color al llenarse: cian, ámbar desde el 80 % y rojo desde el 90 %.
internal sealed record DiskRow(string Title, double Celsius, string Free, string Total, double UsedFraction, Brush Fill, double RingSize)
{
    private const double WarnFraction = 0.8;
    private const double FullFraction = 0.9;

    // Dos discos caben con el anillo grande del SensorPanel; con más, se encoge para que quepan todos.
    public static DiskRow From(DiskStatus disk, int count)
    {
        var used = Format.Fraction(disk.UsedBytes, disk.TotalBytes);
        return new(
            $"{disk.Letter} · {disk.Model}",
            disk.TemperatureC ?? double.NaN,
            $"{Format.Gb(disk.TotalBytes - disk.UsedBytes)} libres",
            $"de {Format.Gb(disk.TotalBytes)}",
            used,
            Brush(used >= FullFraction ? "PulseError" : used >= WarnFraction ? "PulseWarn" : "PulseData"),
            Math.Min(100, 250d / Math.Max(count, 1) - 40));
    }

    public double BarHeight => RingSize - 18;

    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];
}

internal sealed record FanRow(string Label, string Rpm, double Fraction)
{
    public static FanRow From(FanStatus fan) => new(fan.Label, Format.Rpm(fan.Rpm), fan.Fraction);
}

// Activo en verde, ocupado (un runner con un trabajo) en cian, parado en rojo y no instalado en gris.
internal sealed record ServiceRow(string Label, string State, Brush Color)
{
    public static ServiceRow From(ServiceStatus service) => service.State switch
    {
        ServiceState.Running => new(service.Label, "Activo", Brush("PulseOk")),
        ServiceState.Busy => new(service.Label, "Ocupado", Brush("PulseData")),
        ServiceState.Stopped => new(service.Label, "Parado", Brush("PulseError")),
        _ => new(service.Label, "No instalado", Brush("PulseMuted")),
    };

    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];
}
