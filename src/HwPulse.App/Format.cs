using System.Globalization;
using System.Text.RegularExpressions;
using HwPulse.Sensors;

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

internal sealed record DiskRow(string Title, double Celsius, string Used, double UsedFraction, double RingSize)
{
    // Dos discos caben con el anillo grande del SensorPanel; con más, se encoge para que quepan todos.
    public static DiskRow From(DiskStatus disk, int count) => new(
        $"{disk.Letter} · {disk.Model}",
        disk.TemperatureC ?? double.NaN,
        Format.Gb(disk.UsedBytes),
        Format.Fraction(disk.UsedBytes, disk.TotalBytes),
        Math.Min(100, 250d / Math.Max(count, 1) - 40));
}

internal sealed record FanRow(string Label, string Rpm, double Fraction)
{
    public static FanRow From(FanStatus fan) => new(fan.Label, Format.Rpm(fan.Rpm), fan.Fraction);
}
