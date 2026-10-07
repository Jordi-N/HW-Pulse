using HwPulse.Sensors;

namespace HwPulse.Tests;

public class HistoryTests
{
    [Fact]
    public void AddKeepsValuesOldestFirst()
    {
        var history = new History(3);

        history.Add(1);
        history.Add(2);

        Assert.Equal([1f, 2f], history.Values);
    }

    [Fact]
    public void AddWhenFullDropsOldest()
    {
        var history = new History(3);

        foreach (var value in new float[] { 1, 2, 3, 4 }) history.Add(value);

        Assert.Equal([2f, 3f, 4f], history.Values);
    }

    [Fact]
    public void AddNullIsIgnored()
    {
        var history = new History(3);

        history.Add(1);
        history.Add(null);

        Assert.Equal([1f], history.Values);
    }
}
