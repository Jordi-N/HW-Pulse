using HwPulse.Sensors;

namespace HwPulse.Tests;

public class DisplayPickTests
{
    private static readonly DisplayCorner Main = new(0, 0);
    private static readonly DisplayCorner Left = new(-1280, 0);
    private static readonly DisplayCorner Right = new(1920, 0);

    [Fact]
    public void ChooseSavedDisplayWhenConnected()
    {
        Assert.Equal(0, DisplayPick.Choose([Main, Left, Right], 0, Main));
    }

    [Fact]
    public void ChooseFirstNonPrimaryWhenSavedIsGone()
    {
        Assert.Equal(1, DisplayPick.Choose([Main, Left], 0, Right));
    }

    [Fact]
    public void ChooseFirstNonPrimaryWithoutSaved()
    {
        Assert.Equal(0, DisplayPick.Choose([Left, Main], 1, null));
    }

    [Fact]
    public void ChoosePrimaryWhenItIsTheOnlyDisplay()
    {
        Assert.Equal(0, DisplayPick.Choose([Main], 0, null));
    }
}
