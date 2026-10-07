namespace HwPulse.Sensors;

// Una lectura completa del equipo. Los valores que el hardware no expone llegan a null y la
// interfaz los pinta como «—» en vez de inventar un cero.

public sealed record Snapshot(
    MemoryStatus Memory,
    ChipStatus? Cpu,
    ChipStatus? Gpu,
    IReadOnlyList<DiskStatus> Disks,
    IReadOnlyList<FanStatus> Fans,
    NetworkStatus? Network,
    IReadOnlyList<ServiceStatus> Services,
    bool HasLowLevelAccess);

public sealed record MemoryStatus(float? LoadPercent);

public sealed record ChipStatus(string Name, float? TemperatureC, float? LoadPercent, float? ClockMhz);

public sealed record DiskStatus(string Letter, string Model, bool IsSsd, float? TemperatureC, long UsedBytes, long TotalBytes);

public sealed record FanStatus(string Label, float Rpm, float Fraction);

public sealed record NetworkStatus(string Adapter, float? DownloadBytesPerSecond, float? UploadBytesPerSecond);

public sealed record ServiceStatus(string Label, ServiceState State);

public enum ServiceState
{
    Running,
    Busy,
    Stopped,
    Missing,
}
