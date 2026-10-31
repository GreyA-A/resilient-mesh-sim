using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public class SceneEditorFormTests
{
    private static void FillNode(SceneEditorViewModel editor, string? id = "D")
    {
        editor.NodeId = id;
        editor.NodeX = "40.5";
        editor.NodeY = "-10";
        editor.NodeRange = "150";
        editor.NodeBattery = "75";
    }

    private static void FillZone(SceneEditorViewModel editor, string? id = "z2")
    {
        editor.ZoneId = id;
        editor.ZoneX = "30";
        editor.ZoneY = "-20";
        editor.ZoneRadius = "15";
        editor.ZoneIntensity = "0.8";
    }

    private static void FillPacket(SceneEditorViewModel editor, string? id = "p2")
    {
        editor.PacketId = id;
        editor.PacketSource = "A";
        editor.PacketDestination = "B";
        editor.PacketTtl = "6";
        editor.PacketPayloadBytes = "32";
        editor.PacketScheduledTimeMs = "5";
    }

    // ---------- Початковий стан і властивості ----------

    [Fact]
    public void NewEditorExposesDependenciesCommandsAndDefaults()
    {
        EditorFixture f = EditorFixture.Create();
        SceneEditorViewModel editor = f.Editor;

        Assert.Same(f.Simulation, editor.Simulation);
        Assert.Same(f.Map, editor.Map);
        Assert.NotNull(editor.AddNodeCommand);
        Assert.NotNull(editor.RemoveNodeCommand);
        Assert.NotNull(editor.AddZoneCommand);
        Assert.NotNull(editor.RemoveZoneCommand);
        Assert.NotNull(editor.ToggleZoneCommand);
        Assert.NotNull(editor.AddPacketCommand);
        Assert.NotNull(editor.RemovePacketCommand);
        Assert.True(editor.IsEditingEnabled);
        Assert.False(editor.IsDragging);
        Assert.False(editor.HasError);
        Assert.Null(editor.ErrorMessage);
        Assert.Equal("Нічого не вибрано.", editor.SelectionText);
        Assert.Equal("140", editor.NodeRange);
        Assert.Equal("100", editor.NodeBattery);
        Assert.Equal("30", editor.ZoneRadius);
        Assert.Equal("0.5", editor.ZoneIntensity);
        Assert.Equal("8", editor.PacketTtl);
        Assert.Equal("64", editor.PacketPayloadBytes);
        Assert.Equal("0", editor.PacketScheduledTimeMs);
    }

    [Fact]
    public void EveryFormFieldStoresValueAndRaisesPropertyChanged()
    {
        SceneEditorViewModel editor = EditorFixture.Create().Editor;
        (string Name, Action Set, Func<string?> Get)[] fields =
        [
            (nameof(SceneEditorViewModel.NodeId), () => editor.NodeId = "n1", () => editor.NodeId),
            (nameof(SceneEditorViewModel.NodeX), () => editor.NodeX = "n2", () => editor.NodeX),
            (nameof(SceneEditorViewModel.NodeY), () => editor.NodeY = "n3", () => editor.NodeY),
            (nameof(SceneEditorViewModel.NodeRange), () => editor.NodeRange = "n4", () => editor.NodeRange),
            (nameof(SceneEditorViewModel.NodeBattery), () => editor.NodeBattery = "n5", () => editor.NodeBattery),
            (nameof(SceneEditorViewModel.ZoneId), () => editor.ZoneId = "z1", () => editor.ZoneId),
            (nameof(SceneEditorViewModel.ZoneX), () => editor.ZoneX = "z2", () => editor.ZoneX),
            (nameof(SceneEditorViewModel.ZoneY), () => editor.ZoneY = "z3", () => editor.ZoneY),
            (nameof(SceneEditorViewModel.ZoneRadius), () => editor.ZoneRadius = "z4", () => editor.ZoneRadius),
            (nameof(SceneEditorViewModel.ZoneIntensity), () => editor.ZoneIntensity = "z5", () => editor.ZoneIntensity),
            (nameof(SceneEditorViewModel.PacketId), () => editor.PacketId = "p1x", () => editor.PacketId),
            (nameof(SceneEditorViewModel.PacketSource), () => editor.PacketSource = "p2x", () => editor.PacketSource),
            (nameof(SceneEditorViewModel.PacketDestination), () => editor.PacketDestination = "p3x", () => editor.PacketDestination),
            (nameof(SceneEditorViewModel.PacketTtl), () => editor.PacketTtl = "p4x", () => editor.PacketTtl),
            (nameof(SceneEditorViewModel.PacketPayloadBytes), () => editor.PacketPayloadBytes = "p5x", () => editor.PacketPayloadBytes),
            (nameof(SceneEditorViewModel.PacketScheduledTimeMs), () => editor.PacketScheduledTimeMs = "p6x", () => editor.PacketScheduledTimeMs)
        ];

        foreach ((string name, Action set, Func<string?> get) in fields)
        {
            Assert.PropertyChanged(editor, name, set);
            Assert.NotNull(get());
        }
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionForNullArguments()
    {
        EditorFixture f = EditorFixture.Create();

        Assert.Throws<ArgumentNullException>(() => new SceneEditorViewModel(null!, f.Logger));
        Assert.Throws<ArgumentNullException>(() => new SceneEditorViewModel(f.Simulation, null!));
    }

    [Fact]
    public void InitialPacketListMirrorsScenario()
    {
        SceneEditorViewModel editor = EditorFixture.Create().Editor;

        PacketRow row = Assert.Single(editor.Packets);
        Assert.Equal("p1", row.Id);
        Assert.Equal("p1: A → C, TTL 5, t = 0 мс", row.Description);
        Assert.Null(editor.SelectedPacket);
    }

    // ---------- Додавання вузла ----------

    [Fact]
    public void AddNodeAppliesValidNodeAndClearsError()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.NodeId = "A"; // спершу помилка (дублікат)
        f.Editor.AddNodeCommand.Execute(null);
        Assert.True(f.Editor.HasError);

        FillNode(f.Editor);
        f.Editor.AddNodeCommand.Execute(null);

        Assert.False(f.Editor.HasError);
        Assert.Null(f.Editor.ErrorMessage);
        Assert.Equal(4, f.Simulation.Scenario.Nodes.Count);
        NodeDefinition node = f.NodeDefinitionOf("D");
        Assert.Equal(40.5, node.X);
        Assert.Equal(-10.0, node.Y);
        Assert.Equal(150.0, node.TransmissionRangeMeters);
        Assert.Equal(75.0, node.BatteryLevelPercent);
        Assert.True(f.Map.Topology.Nodes.ContainsKey("D"));
        Assert.Equal(SimulationState.Ready, f.Simulation.State);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Додано вузол D.");
    }

    [Fact]
    public void AddNodeTrimsIdentifier()
    {
        EditorFixture f = EditorFixture.Create();
        FillNode(f.Editor, "  D  ");

        f.Editor.AddNodeCommand.Execute(null);

        Assert.True(f.Map.Topology.Nodes.ContainsKey("D"));
    }

    [Theory]
    [InlineData("x", "abc", "X вузла")]
    [InlineData("y", "", "Y вузла")]
    [InlineData("range", "1,5", "Радіус передачі")]
    [InlineData("battery", "   ", "Заряд батареї")]
    public void AddNodeReportsUnparsableFieldWithoutChangingScenario(string field, string value, string expectedLabel)
    {
        EditorFixture f = EditorFixture.Create();
        FillNode(f.Editor);
        switch (field)
        {
            case "x":
                f.Editor.NodeX = value;
                break;
            case "y":
                f.Editor.NodeY = null;
                f.Editor.NodeY = value;
                break;
            case "range":
                f.Editor.NodeRange = value;
                break;
            default:
                f.Editor.NodeBattery = value;
                break;
        }

        ScenarioDefinition before = f.Simulation.Scenario;
        f.Editor.AddNodeCommand.Execute(null);

        Assert.True(f.Editor.HasError);
        Assert.Contains(expectedLabel, f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);
    }

    [Fact]
    public void AddNodeTreatsNullFieldAsUnparsable()
    {
        EditorFixture f = EditorFixture.Create();
        FillNode(f.Editor);
        f.Editor.NodeX = null;

        f.Editor.AddNodeCommand.Execute(null);

        Assert.Contains("X вузла", f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("A", "дублюється")]
    [InlineData("", "id")]
    [InlineData(null, "id")]
    public void AddNodeRejectsDuplicateOrBlankIdentifierViaValidator(string? id, string expectedFragment)
    {
        EditorFixture f = EditorFixture.Create();
        FillNode(f.Editor, id);
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Editor.AddNodeCommand.Execute(null);

        Assert.True(f.Editor.HasError);
        Assert.Contains("Зміну не застосовано", f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains(expectedFragment, f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);
    }

    [Theory]
    [InlineData("NaN", "0", "150", "75", "координати")]
    [InlineData("0", "0", "-10", "75", "transmissionRangeMeters")]
    [InlineData("0", "0", "150", "101", "batteryLevelPercent")]
    public void AddNodeRejectsOutOfRangeValuesViaValidator(string x, string y, string range, string battery, string expectedFragment)
    {
        EditorFixture f = EditorFixture.Create();
        FillNode(f.Editor);
        f.Editor.NodeX = x;
        f.Editor.NodeY = y;
        f.Editor.NodeRange = range;
        f.Editor.NodeBattery = battery;

        f.Editor.AddNodeCommand.Execute(null);

        Assert.Contains(expectedFragment, f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(3, f.Simulation.Scenario.Nodes.Count);
    }

    [Fact]
    public void RepeatingTheSameErrorKeepsErrorState()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.NodeId = "A";
        f.Editor.AddNodeCommand.Execute(null);
        string? first = f.Editor.ErrorMessage;

        f.Editor.AddNodeCommand.Execute(null);

        Assert.Equal(first, f.Editor.ErrorMessage);
        Assert.True(f.Editor.HasError);
    }

    [Fact]
    public void ErrorMessageRaisesHasErrorNotification()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.NodeId = "A";

        Assert.PropertyChanged(f.Editor, nameof(SceneEditorViewModel.HasError), () => f.Editor.AddNodeCommand.Execute(null));
    }

    // ---------- Додавання зони ----------

    [Fact]
    public void AddZoneAppliesValidZoneAsActive()
    {
        EditorFixture f = EditorFixture.Create();
        FillZone(f.Editor);

        f.Editor.AddZoneCommand.Execute(null);

        Assert.False(f.Editor.HasError);
        Assert.Equal(2, f.Simulation.Scenario.JammingZones.Count);
        JammingZoneDefinition zone = f.ZoneDefinitionOf("z2");
        Assert.Equal(30.0, zone.X);
        Assert.Equal(-20.0, zone.Y);
        Assert.Equal(15.0, zone.RadiusMeters);
        Assert.Equal(0.8, zone.JammingIntensity);
        Assert.True(zone.IsActive);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Додано зону завад z2.");
    }

    [Theory]
    [InlineData("x", "abc", "X зони")]
    [InlineData("y", "", "Y зони")]
    [InlineData("radius", "1,5", "Радіус зони")]
    [InlineData("intensity", "x", "Інтенсивність завад")]
    public void AddZoneReportsUnparsableFieldWithoutChangingScenario(string field, string value, string expectedLabel)
    {
        EditorFixture f = EditorFixture.Create();
        FillZone(f.Editor);
        switch (field)
        {
            case "x":
                f.Editor.ZoneX = value;
                break;
            case "y":
                f.Editor.ZoneY = value;
                break;
            case "radius":
                f.Editor.ZoneRadius = value;
                break;
            default:
                f.Editor.ZoneIntensity = value;
                break;
        }

        ScenarioDefinition before = f.Simulation.Scenario;
        f.Editor.AddZoneCommand.Execute(null);

        Assert.Contains(expectedLabel, f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);
    }

    [Theory]
    [InlineData("1.5", "jammingIntensity")]
    [InlineData("-0.1", "jammingIntensity")]
    public void AddZoneRejectsIntensityOutsideRangeViaValidator(string intensity, string expectedFragment)
    {
        EditorFixture f = EditorFixture.Create();
        FillZone(f.Editor);
        f.Editor.ZoneIntensity = intensity;

        f.Editor.AddZoneCommand.Execute(null);

        Assert.Contains(expectedFragment, f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Single(f.Simulation.Scenario.JammingZones);
    }

    [Fact]
    public void AddZoneRejectsDuplicateIdentifier()
    {
        EditorFixture f = EditorFixture.Create();
        FillZone(f.Editor, "z1");

        f.Editor.AddZoneCommand.Execute(null);

        Assert.Contains("дублюється", f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    // ---------- Додавання пакета ----------

    [Fact]
    public void AddPacketAppliesValidPacketAndRefreshesList()
    {
        EditorFixture f = EditorFixture.Create();
        FillPacket(f.Editor);

        f.Editor.AddPacketCommand.Execute(null);

        Assert.False(f.Editor.HasError);
        Assert.Equal(2, f.Simulation.Scenario.Packets.Count);
        Assert.Equal(2, f.Editor.Packets.Count);
        Assert.Equal("p2: A → B, TTL 6, t = 5 мс", f.Editor.Packets[1].Description);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Додано пакет p2.");
    }

    [Theory]
    [InlineData("ttl", "abc", "TTL")]
    [InlineData("ttl", "1.5", "TTL")]
    [InlineData("payload", "", "Розмір навантаження")]
    [InlineData("time", "x", "Час відправлення")]
    public void AddPacketReportsUnparsableFieldWithoutChangingScenario(string field, string value, string expectedLabel)
    {
        EditorFixture f = EditorFixture.Create();
        FillPacket(f.Editor);
        switch (field)
        {
            case "ttl":
                f.Editor.PacketTtl = value;
                break;
            case "payload":
                f.Editor.PacketPayloadBytes = value;
                break;
            default:
                f.Editor.PacketScheduledTimeMs = value;
                break;
        }

        ScenarioDefinition before = f.Simulation.Scenario;
        f.Editor.AddPacketCommand.Execute(null);

        Assert.Contains(expectedLabel, f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);
    }

    [Theory]
    [InlineData("GHOST", "B", "не існує")]
    [InlineData("A", "GHOST", "не існує")]
    [InlineData("", "B", "sourceNodeId")]
    public void AddPacketRejectsUnknownOrBlankEndpointsViaValidator(string source, string destination, string expectedFragment)
    {
        EditorFixture f = EditorFixture.Create();
        FillPacket(f.Editor);
        f.Editor.PacketSource = source;
        f.Editor.PacketDestination = destination;

        f.Editor.AddPacketCommand.Execute(null);

        Assert.Contains(expectedFragment, f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Single(f.Simulation.Scenario.Packets);
    }

    [Theory]
    [InlineData("0", "64", "ttl")]
    [InlineData("-3", "64", "ttl")]
    [InlineData("5", "0", "payloadBytes")]
    public void AddPacketRejectsNonPositiveNumbersViaValidator(string ttl, string payload, string expectedFragment)
    {
        EditorFixture f = EditorFixture.Create();
        FillPacket(f.Editor);
        f.Editor.PacketTtl = ttl;
        f.Editor.PacketPayloadBytes = payload;

        f.Editor.AddPacketCommand.Execute(null);

        Assert.Contains(expectedFragment, f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPacketRejectsDuplicateIdentifier()
    {
        EditorFixture f = EditorFixture.Create();
        FillPacket(f.Editor, "p1");

        f.Editor.AddPacketCommand.Execute(null);

        Assert.Contains("дублюється", f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    // ---------- Видалення ----------

    [Fact]
    public void RemoveNodeWithoutSelectionReportsError()
    {
        EditorFixture f = EditorFixture.Create();

        f.Editor.RemoveNodeCommand.Execute(null);

        Assert.Contains("виберіть вузол", f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(3, f.Simulation.Scenario.Nodes.Count);
    }

    [Fact]
    public void RemoveNodeReferencedByPacketIsRejectedByValidator()
    {
        EditorFixture f = EditorFixture.Create();
        f.Map.SetSelection(new MapTarget(MapTargetKind.Node, "A"));
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Editor.RemoveNodeCommand.Execute(null);

        Assert.Contains("не існує", f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);
        Assert.NotNull(f.Map.Selection);
    }

    [Fact]
    public void RemoveNodeDeletesUnreferencedNodeAndClearsSelection()
    {
        EditorFixture f = EditorFixture.Create();
        f.Map.SetSelection(new MapTarget(MapTargetKind.Node, "B"));

        f.Editor.RemoveNodeCommand.Execute(null);

        Assert.False(f.Editor.HasError);
        Assert.Equal(2, f.Simulation.Scenario.Nodes.Count);
        Assert.False(f.Map.Topology.Nodes.ContainsKey("B"));
        Assert.Null(f.Map.Selection);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Видалено вузол B.");
    }

    [Fact]
    public void RemoveZoneWithoutSelectionReportsError()
    {
        EditorFixture f = EditorFixture.Create();

        f.Editor.RemoveZoneCommand.Execute(null);

        Assert.Contains("виберіть зону", f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void RemoveZoneDeletesSelectedZoneAndClearsSelection()
    {
        EditorFixture f = EditorFixture.Create();
        f.Map.SetSelection(new MapTarget(MapTargetKind.Zone, "z1"));

        f.Editor.RemoveZoneCommand.Execute(null);

        Assert.Empty(f.Simulation.Scenario.JammingZones);
        Assert.Null(f.Map.Selection);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Видалено зону завад z1.");
    }

    [Fact]
    public void ToggleZoneWithoutSelectionReportsError()
    {
        EditorFixture f = EditorFixture.Create();

        f.Editor.ToggleZoneCommand.Execute(null);

        Assert.Contains("виберіть зону", f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ToggleZoneFlipsActivityAndKeepsSelection()
    {
        EditorFixture f = EditorFixture.Create();
        f.Map.SetSelection(new MapTarget(MapTargetKind.Zone, "z1"));

        f.Editor.ToggleZoneCommand.Execute(null);
        Assert.False(f.ZoneDefinitionOf("z1").IsActive);
        Assert.False(f.Map.Topology.JammingZones[0].IsActive);
        Assert.NotNull(f.Map.Selection);

        f.Editor.ToggleZoneCommand.Execute(null);
        Assert.True(f.ZoneDefinitionOf("z1").IsActive);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Перемкнуто активність зони завад z1.");
    }

    [Fact]
    public void RemovePacketWithoutSelectionReportsError()
    {
        EditorFixture f = EditorFixture.Create();

        f.Editor.RemovePacketCommand.Execute(null);

        Assert.Contains("виберіть пакет", f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovePacketDeletesSelectedPacketAndRefreshesList()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.SelectedPacket = f.Editor.Packets[0];

        f.Editor.RemovePacketCommand.Execute(null);

        Assert.Empty(f.Simulation.Scenario.Packets);
        Assert.Empty(f.Editor.Packets);
        Assert.Null(f.Editor.SelectedPacket);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Видалено пакет p1.");
    }
}