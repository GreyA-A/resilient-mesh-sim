using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class StyleCalculatorTests
{
    // ---------- Лінки ----------

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void LinkStyleIsDashedRedForBlockedOrInvalidQuality(double quality)
    {
        LinkStyle style = LinkStyleCalculator.ForQuality(quality);

        Assert.Equal(MapPalette.LinkBlocked, style.Color);
        Assert.Equal(LinkStyleCalculator.MinThickness, style.Thickness);
        Assert.True(style.IsDashed);
    }

    [Fact]
    public void LinkStyleIsGreenAndThickForCleanLink()
    {
        LinkStyle style = LinkStyleCalculator.ForQuality(1.0);

        Assert.Equal(MapPalette.LinkGood, style.Color);
        Assert.Equal(LinkStyleCalculator.MaxThickness, style.Thickness);
        Assert.False(style.IsDashed);
    }

    [Fact]
    public void LinkStyleBlendsColorAndThicknessForPartialQuality()
    {
        LinkStyle style = LinkStyleCalculator.ForQuality(0.5);

        Assert.Equal(RgbaColor.Blend(MapPalette.LinkPoor, MapPalette.LinkGood, 0.5), style.Color);
        Assert.Equal(2.75, style.Thickness, precision: 9);
        Assert.False(style.IsDashed);
    }

    [Fact]
    public void LinkStyleClampsQualityAboveOne()
    {
        Assert.Equal(LinkStyleCalculator.ForQuality(1.0), LinkStyleCalculator.ForQuality(2.0));
    }

    // ---------- Вузли ----------

    [Fact]
    public void NodeStyleForFullBatteryIsGreenAndActive()
    {
        var node = new MeshNode("A", new Position(0, 0), 100.0);

        NodeStyle style = NodeStyleCalculator.For(node);

        Assert.Equal(MapPalette.BatteryFull, style.Fill);
        Assert.Equal(MapPalette.BatteryFull, style.BatteryColor);
        Assert.Equal(1.0, style.BatteryFraction);
        Assert.True(style.IsActive);
    }

    [Fact]
    public void NodeStyleForDepletedBatteryIsGrayAndInactive()
    {
        var node = new MeshNode("A", new Position(0, 0), 100.0, 0.0);

        NodeStyle style = NodeStyleCalculator.For(node);

        Assert.Equal(MapPalette.InactiveNode, style.Fill);
        Assert.Equal(MapPalette.BatteryLow, style.BatteryColor);
        Assert.Equal(0.0, style.BatteryFraction);
        Assert.False(style.IsActive);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void BatteryColorFollowsRedYellowGreenGradient(double fraction)
    {
        RgbaColor expected = fraction <= 0.5
            ? RgbaColor.Blend(MapPalette.BatteryLow, MapPalette.BatteryMid, fraction * 2.0)
            : RgbaColor.Blend(MapPalette.BatteryMid, MapPalette.BatteryFull, (fraction - 0.5) * 2.0);

        Assert.Equal(expected, NodeStyleCalculator.BatteryColor(fraction));
    }

    [Fact]
    public void BatteryColorHitsAnchorColorsAtEndpointsAndMiddle()
    {
        Assert.Equal(MapPalette.BatteryLow, NodeStyleCalculator.BatteryColor(0.0));
        Assert.Equal(MapPalette.BatteryMid, NodeStyleCalculator.BatteryColor(0.5));
        Assert.Equal(MapPalette.BatteryFull, NodeStyleCalculator.BatteryColor(1.0));
    }

    [Fact]
    public void NodeStyleThrowsArgumentNullExceptionForNull()
    {
        Assert.Throws<ArgumentNullException>(() => NodeStyleCalculator.For(null!));
    }

    // ---------- Зони ----------

    [Theory]
    [InlineData(0.0, 40)]
    [InlineData(0.5, 100)]
    [InlineData(1.0, 160)]
    public void ActiveZoneAlphaGrowsWithIntensity(double intensity, int expectedAlpha)
    {
        var zone = new JammingZone("z", new Position(0, 0), 10.0, intensity);

        ZoneStyle style = ZoneStyleCalculator.For(zone);

        Assert.Equal(expectedAlpha, style.Fill.A);
        Assert.Equal(MapPalette.ZoneActive.WithAlpha(ZoneStyleCalculator.ActiveStrokeAlpha), style.Stroke);
        Assert.False(style.IsDashed);
        Assert.True(style.IsActive);
    }

    [Fact]
    public void InactiveZoneIsGrayDashedAndFaint()
    {
        var zone = new JammingZone("z", new Position(0, 0), 10.0, 1.0, isActive: false);

        ZoneStyle style = ZoneStyleCalculator.For(zone);

        Assert.Equal(ZoneStyleCalculator.InactiveFillAlpha, style.Fill.A);
        Assert.Equal(ZoneStyleCalculator.InactiveStrokeAlpha, style.Stroke.A);
        Assert.True(style.IsDashed);
        Assert.False(style.IsActive);
    }

    [Fact]
    public void ZoneStyleThrowsArgumentNullExceptionForNull()
    {
        Assert.Throws<ArgumentNullException>(() => ZoneStyleCalculator.For(null!));
    }
}