using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;

namespace MeshSim.Desktop.Controls;

[ExcludeFromCodeCoverage(Justification = "Кастомний контрол: лише малювання готової MapScene через DrawingContext, без обчислень. Уся логіка винесена в тестовані класи Rendering.")]
public sealed class MapControl : Control
{
    private const double BatteryBarWidth = 24.0;
    private const double BatteryBarHeight = 4.0;
    private const double LabelFontSize = 12.0;

    public static readonly StyledProperty<MapViewModel?> MapProperty =
        AvaloniaProperty.Register<MapControl, MapViewModel?>(nameof(Map));

    static MapControl()
    {
        AffectsRender<MapControl>(MapProperty);
    }

    public MapControl()
    {
        ClipToBounds = true;
    }

    public MapViewModel? Map
    {
        get => GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != MapProperty)
        {
            return;
        }

        if (change.OldValue is MapViewModel previous)
        {
            previous.PropertyChanged -= OnMapPropertyChanged;
        }

        if (change.NewValue is MapViewModel current)
        {
            current.PropertyChanged += OnMapPropertyChanged;
            current.Resize(Bounds.Width, Bounds.Height);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Size arranged = base.ArrangeOverride(finalSize);
        Map?.Resize(arranged.Width, arranged.Height);
        return arranged;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.DrawRectangle(CreateBrush(MapPalette.Background), null, new Rect(Bounds.Size));

        MapScene? scene = Map?.Scene;
        if (scene is null)
        {
            return;
        }

        foreach (ZoneVisual zone in scene.Zones)
        {
            DrawZone(context, zone);
        }

        foreach (NodeVisual node in scene.Nodes)
        {
            if (node.Style.IsActive)
            {
                DrawRange(context, node);
            }
        }

        foreach (LinkVisual link in scene.Links)
        {
            DrawLink(context, link);
        }

        foreach (NodeVisual node in scene.Nodes)
        {
            DrawNode(context, node);
        }
    }

    private void OnMapPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.Scene))
        {
            InvalidateVisual();
        }
    }

    private static void DrawZone(DrawingContext context, ZoneVisual zone)
    {
        var pen = new Pen(CreateBrush(zone.Style.Stroke), 1.5, zone.Style.IsDashed ? DashStyle.Dash : null);
        context.DrawEllipse(CreateBrush(zone.Style.Fill), pen, ToPoint(zone.Center), zone.RadiusPx, zone.RadiusPx);
    }

    private static void DrawRange(DrawingContext context, NodeVisual node)
    {
        var pen = new Pen(CreateBrush(MapPalette.RangeStroke), 1.0);
        context.DrawEllipse(CreateBrush(MapPalette.RangeFill), pen, ToPoint(node.Center), node.RangeRadiusPx, node.RangeRadiusPx);
    }

    private static void DrawLink(DrawingContext context, LinkVisual link)
    {
        var pen = new Pen(CreateBrush(link.Style.Color), link.Style.Thickness, link.Style.IsDashed ? DashStyle.Dash : null);
        context.DrawLine(pen, ToPoint(link.From), ToPoint(link.To));
    }

    private static void DrawNode(DrawingContext context, NodeVisual node)
    {
        Point center = ToPoint(node.Center);
        var outline = new Pen(CreateBrush(MapPalette.NodeOutline), 1.5);
        context.DrawEllipse(CreateBrush(node.Style.Fill), outline, center, node.RadiusPx, node.RadiusPx);

        double barLeft = center.X - (BatteryBarWidth / 2.0);
        double barTop = center.Y + node.RadiusPx + 4.0;
        context.DrawRectangle(CreateBrush(MapPalette.BatteryTrack), null, new Rect(barLeft, barTop, BatteryBarWidth, BatteryBarHeight));

        if (node.Style.BatteryFraction > 0.0)
        {
            context.DrawRectangle(
                CreateBrush(node.Style.BatteryColor),
                null,
                new Rect(barLeft, barTop, BatteryBarWidth * node.Style.BatteryFraction, BatteryBarHeight));
        }

        DrawLabel(context, node.Id, center.X, center.Y - node.RadiusPx - 4.0, anchorAbove: true);
        DrawLabel(context, node.BatteryText, center.X, barTop + BatteryBarHeight + 2.0, anchorAbove: false);
    }

    private static void DrawLabel(DrawingContext context, string text, double centerX, double anchorY, bool anchorAbove)
    {
        var label = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            LabelFontSize,
            CreateBrush(MapPalette.LabelText));

        double top = anchorAbove ? anchorY - label.Height : anchorY;
        context.DrawText(label, new Point(centerX - (label.Width / 2.0), top));
    }

    private static SolidColorBrush CreateBrush(RgbaColor color)
    {
        return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
    }

    private static Point ToPoint(ScreenPoint point)
    {
        return new Point(point.X, point.Y);
    }
}