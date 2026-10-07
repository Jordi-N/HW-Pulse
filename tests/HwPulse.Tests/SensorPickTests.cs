using HwPulse.Sensors;
using LibreHardwareMonitor.Hardware;

namespace HwPulse.Tests;

public class SensorPickTests
{
    private static Reading Temp(string name, float? value) => new(SensorType.Temperature, name, value);
    private static Reading Load(string name, float? value) => new(SensorType.Load, name, value);
    private static Reading Clock(string name, float? value) => new(SensorType.Clock, name, value);
    private static Reading Fan(string name, float? value) => new(SensorType.Fan, name, value);

    [Fact]
    public void CpuTemperaturePrefersPackage()
    {
        Reading[] readings = [Temp("Core Max", 70), Temp("CPU Package", 65), Temp("Core #1", 80)];

        Assert.Equal(65, SensorPick.CpuTemperature(readings));
    }

    [Fact]
    public void CpuTemperatureFallsBackToCoreMax()
    {
        Reading[] readings = [Temp("CPU Package", null), Temp("Core #1", 80), Temp("Core Max", 70)];

        Assert.Equal(70, SensorPick.CpuTemperature(readings));
    }

    [Fact]
    public void CpuTemperatureFallsBackToHottest()
    {
        Reading[] readings = [Temp("Tctl", 60), Temp("Tdie", 72), Load("CPU Total", 99)];

        Assert.Equal(72, SensorPick.CpuTemperature(readings));
    }

    [Fact]
    public void CpuTemperatureWithoutTemperaturesIsNull()
    {
        Assert.Null(SensorPick.CpuTemperature([Load("CPU Total", 10)]));
    }

    [Fact]
    public void CpuLoadReadsTotal()
    {
        Reading[] readings = [Load("CPU Core #1", 90), Load("CPU Total", 25)];

        Assert.Equal(25, SensorPick.CpuLoad(readings));
    }

    [Fact]
    public void CpuClockAveragesActiveCoresIncludingHybrid()
    {
        Reading[] readings =
        [
            Clock("P-Core #1", 5000),
            Clock("E-Core #1", 4000),
            Clock("Core #3", 0),
            Clock("Core #4", null),
            Clock("Bus Speed", 100),
        ];

        Assert.Equal(4500, SensorPick.CpuClock(readings));
    }

    [Fact]
    public void CpuClockWithoutCoresIsNull()
    {
        Assert.Null(SensorPick.CpuClock([Clock("Bus Speed", 100)]));
    }

    [Fact]
    public void GpuReadingsPreferCoreAndFallBackToFirst()
    {
        Reading[] named = [Temp("GPU Hot Spot", 80), Temp("GPU Core", 60), Load("GPU Memory", 40), Load("GPU Core", 30)];
        Reading[] unnamed = [Temp("GPU Hot Spot", 80), Load("GPU Memory", 40), Clock("GPU Memory", 4000)];

        Assert.Equal(60, SensorPick.GpuTemperature(named));
        Assert.Equal(30, SensorPick.GpuLoad(named));
        Assert.Equal(80, SensorPick.GpuTemperature(unnamed));
        Assert.Equal(40, SensorPick.GpuLoad(unnamed));
        Assert.Equal(4000, SensorPick.GpuClock(unnamed));
    }

    [Fact]
    public void GpuPrefersDedicatedOverIntel()
    {
        (string Name, HardwareType Type)[] gpus = [("Intel Graphics", HardwareType.GpuIntel), ("GTX 1070", HardwareType.GpuNvidia)];

        Assert.Equal("GTX 1070", SensorPick.Gpu(gpus.Select(g => g.Name), n => gpus.Single(g => g.Name == n).Type));
    }

    [Fact]
    public void GpuOnlyIntelReturnsIt()
    {
        Assert.Equal("Intel Graphics", SensorPick.Gpu(["Intel Graphics"], _ => HardwareType.GpuIntel));
    }

    [Fact]
    public void FansWithoutSettingsShowsSpinningOnes()
    {
        Reading[] readings = [Fan("Fan #1", 1000), Fan("Fan #2", 0), Fan("Fan #3", null)];

        Assert.Equal([new FanStatus("Fan #1", 1000, 0.5f)], SensorPick.Fans(readings, []));
    }

    [Fact]
    public void FansWithSettingsUsesTheirOrderLabelsAndMax()
    {
        Reading[] readings = [Fan("Fan #1", 1500), Fan("Fan #2", 6000), Fan("Fan #3", 0)];
        FanSetting[] settings =
        [
            new("Fan #2", "PUMP", 3000),
            new("Fan #9", "MISSING"),
            new("Fan #3", "REAR"),
            new("Fan #1", "CPU"),
        ];

        Assert.Equal(
            [new FanStatus("PUMP", 6000, 1f), new FanStatus("REAR", 0, 0f), new FanStatus("CPU", 1500, 0.75f)],
            SensorPick.Fans(readings, settings));
    }
}
