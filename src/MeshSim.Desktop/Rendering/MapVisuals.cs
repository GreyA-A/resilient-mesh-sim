using MeshSim.Engine;

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

/// <summary>
/// Маркер пакета на вузлі, де відбулась остання подія, із підписом і кольором за типом події.
/// </summary>
public readonly record struct PacketMarkerVisual(
    string PacketId,
    string Caption,
    ScreenPoint Position,
    double RadiusPx,
    SimulationEventType EventType,
    RgbaColor Color);

/// <summary>
/// Підсвічений сегмент, яким пакет щойно пройшов (від попереднього вузла до поточного).
/// </summary>
public readonly record struct ActiveSegmentVisual(
    ScreenPoint From,
    ScreenPoint To,
    RgbaColor Color,
    double Thickness);