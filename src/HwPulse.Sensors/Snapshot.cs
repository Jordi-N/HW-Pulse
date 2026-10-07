namespace HwPulse.Sensors;

// Una lectura completa del equipo. Los valores que el hardware no expone llegan a null y la
// interfaz los pinta como «—» en vez de inventar un cero.

public sealed record Snapshot(
    MemoryStatus Memory,
    ChipStatus? Cpu,
    ChipStatus? Gpu,
    IReadOnlyList<DiskStatus> Disks,
    IReadOnlyList<FanStatus> Fans,
    bool HasLowLevelAccess);

public sealed record MemoryStatus(float? LoadPercent, float? UsedGb, float? TotalGb);

public sealed record ChipStatus(string Name, float? TemperatureC, float? LoadPercent, float? ClockMhz);

public sealed record DiskStatus(string Letter, string Model, bool IsSsd, float? TemperatureC, long UsedBytes, long TotalBytes);

public sealed record FanStatus(string Label, float Rpm, float Fraction);
