using MeshSim.Desktop.Rendering;

namespace MeshSim.Tests;

public class RgbaColorTests
{
    private static readonly RgbaColor Black = new(0, 0, 0);
    private static readonly RgbaColor Target = new(100, 200, 50);

    [Fact]
    public void AlphaDefaultsToOpaque()
    {
        Assert.Equal(255, new RgbaColor(1, 2, 3).A);
    }

    [Fact]
    public void BlendReturnsEndpointsAtZeroAndOne()
    {
        Assert.Equal(Black, RgbaColor.Blend(Black, Target, 0.0));
        Assert.Equal(Target, RgbaColor.Blend(Black, Target, 1.0));
    }

    [Fact]
    public void BlendInterpolatesLinearlyAtMidpoint()
    {
        Assert.Equal(new RgbaColor(50, 100, 25), RgbaColor.Blend(Black, Target, 0.5));
    }

    [Theory]
    [InlineData(-3.0)]
    [InlineData(double.NaN)]
    public void BlendClampsBelowRangeAndTreatsNaNAsZero(double amount)
    {
        Assert.Equal(Black, RgbaColor.Blend(Black, Target, amount));
    }

    [Fact]
    public void BlendClampsAboveRange()
    {
        Assert.Equal(Target, RgbaColor.Blend(Black, Target, 5.0));
    }

    [Fact]
    public void BlendInterpolatesAlphaChannel()
    {
        RgbaColor result = RgbaColor.Blend(new RgbaColor(0, 0, 0, 0), new RgbaColor(0, 0, 0, 100), 0.5);

        Assert.Equal(50, result.A);
    }

    [Fact]
    public void WithAlphaReplacesOnlyAlpha()
    {
        Assert.Equal(new RgbaColor(1, 2, 3, 77), new RgbaColor(1, 2, 3).WithAlpha(77));
    }
}