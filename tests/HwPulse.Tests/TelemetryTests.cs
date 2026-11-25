using System.ComponentModel;
using System.Diagnostics;
using HwPulse.Sensors;
using LibreHardwareMonitor.Hardware;

namespace HwPulse.Tests;

public sealed class TelemetryTests
{
    [Fact]
    public void CpuTelemetrySeparatesPackageFromPhysicalCoreAndThermalMargin()
    {
        Reading[] readings =
        [
            new(SensorType.Temperature, "CPU Package", 94),
            new(SensorType.Temperature, "P-Core #1", 100),
            new(SensorType.Temperature, "E-Core #2", 85),
            new(SensorType.Temperature, "Core #1 Distance to TjMax", 110),
            new(SensorType.Power, "CPU Package", 235),
        ];

        Assert.Equal(94, SensorPick.CpuPackageTemperature(readings));
        Assert.Equal(100, SensorPick.HottestPhysicalCore(readings));
        Assert.Equal(235, SensorPick.PackagePower(readings));
        Assert.Null(SensorPick.HottestPhysicalCore([]));
    }

    [Fact]
    public void MissingProcessCounterStaysNull()
    {
        Assert.Null(ProcessTelemetry.ReadCounter<long?>(() => throw new Win32Exception()));
        Assert.Equal(0, ProcessTelemetry.ReadCounter<long?>(() => 0));
    }

    [Fact]
    public void ReadsCurrentWindowsProcess()
    {
        using var current = Process.GetCurrentProcess();
        var sample = Assert.Single(ProcessTelemetry.Read(), item => item.Pid == current.Id);
        Assert.NotNull(sample.StartedAt);
        Assert.NotNull(sample.CpuMs);
        Assert.True(sample.WorkingSetBytes > 0);
        Assert.NotNull(sample.ReadBytes);
    }
}
