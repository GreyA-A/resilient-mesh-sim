namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Вузол у екранних координатах: центр, радіуси, стиль і підпис заряду.
/// </summary>
public readonly record struct NodeVisual(
    string Id,
    ScreenPoint Center,
    double RadiusPx,
    double RangeRadiusPx,
    NodeStyle Style,
    string BatteryText);

/// <summary>
/// Радіоканал між двома вузлами в екранних координатах.
/// </summary>
public readonly record struct LinkVisual(
    string FromId,
    string ToId,
    ScreenPoint From,
    ScreenPoint To,
    double Quality,
    LinkStyle Style);

/// <summary>
/// Зона радіозавад у екранних координатах.
/// </summary>
public readonly record struct ZoneVisual(
    string Id,
    ScreenPoint Center,
    double RadiusPx,
    ZoneStyle Style);