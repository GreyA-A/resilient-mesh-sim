namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Палітра карти топології.
/// </summary>
public static class MapPalette
{
    public static readonly RgbaColor Background = new(24, 26, 31);
    public static readonly RgbaColor NodeOutline = new(236, 240, 241);
    public static readonly RgbaColor LabelText = new(236, 240, 241);
    public static readonly RgbaColor BatteryTrack = new(52, 58, 64);
    public static readonly RgbaColor InactiveNode = new(127, 140, 141);
    public static readonly RgbaColor BatteryLow = new(231, 76, 60);
    public static readonly RgbaColor BatteryMid = new(241, 196, 15);
    public static readonly RgbaColor BatteryFull = new(46, 204, 113);
    public static readonly RgbaColor LinkGood = new(46, 204, 113);
    public static readonly RgbaColor LinkPoor = new(230, 126, 34);
    public static readonly RgbaColor LinkBlocked = new(192, 57, 43);
    public static readonly RgbaColor RangeFill = new(52, 152, 219, 18);
    public static readonly RgbaColor RangeStroke = new(52, 152, 219, 70);
    public static readonly RgbaColor ZoneActive = new(231, 76, 60);
    public static readonly RgbaColor ZoneInactive = new(149, 165, 166);
    public static readonly RgbaColor MarkerQueued = new(52, 152, 219);
    public static readonly RgbaColor MarkerForwarded = new(241, 196, 15);
    public static readonly RgbaColor MarkerDelivered = new(46, 204, 113);
    public static readonly RgbaColor MarkerDropped = new(231, 76, 60);
    public static readonly RgbaColor SelectionRing = new(0, 200, 255);
}