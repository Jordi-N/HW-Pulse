using System.Text.Json;
using HwPulse.Sensors;

namespace HwPulse.Tests;

public class MonitorSettingsTests
{
    [Fact]
    public void ParseReadsFansInOrder()
    {
        var settings = MonitorSettings.Parse("""
            {
              "fans": [
                { "sensor": "Fan #2", "label": "PUMP", "maxRpm": 3000 },
                { "sensor": "Fan #1", "label": "CPU" }
              ]
            }
            """);

        Assert.Equal(
            [new FanSetting("Fan #2", "PUMP", 3000), new FanSetting("Fan #1", "CPU", FanSetting.DefaultMaxRpm)],
            settings.Fans);
    }

    [Fact]
    public void ParseAcceptsCommentsTrailingCommasAndAnyCase()
    {
        var settings = MonitorSettings.Parse("""
            {
              // Bomba del líquido
              "Fans": [ { "Sensor": "Fan #2", "LABEL": "PUMP", }, ],
            }
            """);

        Assert.Equal([new FanSetting("Fan #2", "PUMP")], settings.Fans);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "fans": null }""")]
    [InlineData("null")]
    public void ParseWithoutFansReturnsEmptyList(string json)
    {
        Assert.Empty(MonitorSettings.Parse(json).Fans);
    }

    [Fact]
    public void ParseInvalidJsonThrows()
    {
        Assert.ThrowsAny<JsonException>(() => MonitorSettings.Parse("{ fans: "));
    }

    [Fact]
    public void LoadMissingFileReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        Assert.Same(MonitorSettings.Empty, MonitorSettings.Load(path));
    }
}
