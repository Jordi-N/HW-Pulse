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
    public void ParseReadsServices()
    {
        var settings = MonitorSettings.Parse("""
            { "services": [ { "name": "W3SVC", "label": "IIS" } ] }
            """);

        Assert.Equal([new ServiceSetting("W3SVC", "IIS")], settings.Services);
        Assert.Empty(settings.Fans);
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
    [InlineData("""{ "fans": null, "services": null }""")]
    [InlineData("null")]
    public void ParseWithoutListsReturnsEmptyLists(string json)
    {
        var settings = MonitorSettings.Parse(json);

        Assert.Empty(settings.Fans);
        Assert.Empty(settings.Services);
    }

    [Fact]
    public void ParseInvalidJsonThrows()
    {
        Assert.ThrowsAny<JsonException>(() => MonitorSettings.Parse("{ fans: "));
    }

    [Fact]
    public void ParseReadsDisplay()
    {
        var settings = MonitorSettings.Parse("""{ "display": { "x": -1280, "y": 0 } }""");

        Assert.Equal(new DisplayCorner(-1280, 0), settings.Display);
    }

    [Fact]
    public void ParseWithoutDisplayReturnsNull()
    {
        Assert.Null(MonitorSettings.Parse("{}").Display);
    }

    [Fact]
    public void WithDisplayKeepsTheOtherSettings()
    {
        var json = MonitorSettings.WithDisplay("""
            {
              // Bomba del líquido
              "fans": [ { "sensor": "Fan #2", "label": "PUMP", "maxRpm": 3000 }, ],
              "services": [ { "name": "W3SVC", "label": "IIS" } ]
            }
            """, new DisplayCorner(-1280, 0));

        var settings = MonitorSettings.Parse(json);
        Assert.Equal([new FanSetting("Fan #2", "PUMP", 3000)], settings.Fans);
        Assert.Equal([new ServiceSetting("W3SVC", "IIS")], settings.Services);
        Assert.Equal(new DisplayCorner(-1280, 0), settings.Display);
    }

    [Fact]
    public void WithDisplayReplacesThePreviousOneInAnyCase()
    {
        var json = MonitorSettings.WithDisplay("""{ "Display": { "x": 0, "y": 0 } }""", new DisplayCorner(1920, 0));

        Assert.Equal("""{"Display":{"x":1920,"y":0}}""", json.Replace(" ", "", StringComparison.Ordinal).ReplaceLineEndings(""));
    }

    [Fact]
    public void WithDisplayWithoutFileCreatesIt()
    {
        var json = MonitorSettings.WithDisplay(null, new DisplayCorner(-1280, 0));

        Assert.Equal(new DisplayCorner(-1280, 0), MonitorSettings.Parse(json).Display);
    }

    [Fact]
    public void LoadMissingFileReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        Assert.Same(MonitorSettings.Empty, MonitorSettings.Load(path));
    }
}
